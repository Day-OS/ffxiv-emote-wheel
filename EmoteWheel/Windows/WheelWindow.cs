using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Emote = Lumina.Excel.Sheets.Emote;

namespace EmoteWheel.Windows;

/// <summary>
/// Invisible full-screen overlay (so clicks never fall through to the game) with the wheel drawn
/// around the point where it was opened. Favorites appear directly in saved order; left-click an
/// emote to perform it. The center, right-click or Esc closes the wheel.
/// </summary>
/// <remarks>
/// Animations: the wheel pops open from its center with a slight overshoot and shrinks/fades away when
/// closed; each new ring (level or page change) scales/fades in; hovered slices grow outward with an
/// outline and a bigger icon; a clicked slice flashes and pulses briefly before its action runs (red
/// for a locked emote, which does nothing).
/// </remarks>
public class WheelWindow : Window
{
    private const float InnerRatio = 0.35f;
    private const float IconSize = 40f;
    private const float SliceGap = 4f;
    private const int SlicesPerPage = 16;

    private const float OpenSeconds = 0.25f;
    private const float IntroSeconds = 0.18f;
    private const float CloseSeconds = 0.15f;
    private const float PressSeconds = 0.14f;
    private const float HoverSpeed = 14f;

    private static readonly Vector4 EmoteColor = new(0.17f, 0.15f, 0.12f, 0.95f);
    private static readonly Vector4 FlashColor = new(1f, 1f, 0.9f, 1f);
    private static readonly Vector4 DenyColor = new(0.85f, 0.2f, 0.2f, 0.95f);

    private readonly Plugin plugin;
    private List<Emote> favorites = [];
    private HashSet<uint> locked = [];
    private int page;
    private Vector2 center;
    private bool needsAnchor;

    // Animation state for the ring currently shown.
    private bool firstRing;
    private bool closing;
    private float outro;
    private float visScale = 1f;
    private float visAlpha = 1f;
    private float intro;
    private float[] hoverAnim = [];
    private float hubAnim;
    private int? pressedSlice;
    private float pressTimer;
    private uint? pendingEmote;

    public WheelWindow(Plugin plugin)
        : base("Emote Wheel##overlay",
               ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
               ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDocking)
    {
        this.plugin = plugin;
        // Esc is handled in Draw so it can play the close animation.
        RespectCloseHotkey = false;
        PositionCondition = ImGuiCond.Always;
        SizeCondition = ImGuiCond.Always;
    }

    public override void OnOpen()
    {
        // Rebuilt on every open: unlocks change while playing and differ per character.
        favorites = Emotes.GetFavorites();
        locked = favorites.Where(e => !Emotes.IsUnlocked(e)).Select(e => e.RowId).ToHashSet();
        page = 0;
        needsAnchor = true;
        ShowNewRing();
        firstRing = true;
        closing = false;
        outro = 0f;
    }

    /// <summary>Toggles Favorites at the cursor, playing the opening or closing animation.</summary>
    public void ToggleAnimated()
    {
        if (!IsOpen || closing)
        {
            if (IsOpen)
                OnOpen();
            else
                IsOpen = true;
        }
        else
            CloseAnimated();
    }

    /// <summary>Shrinks and fades the wheel out, then closes it. Input is ignored meanwhile.</summary>
    private void CloseAnimated()
    {
        if (!IsOpen || closing)
            return;
        closing = true;
        outro = 0f;
        pendingEmote = null;
        pressedSlice = null;
    }

    public override void PreDraw()
    {
        var viewport = ImGui.GetMainViewport();
        Position = viewport.Pos;
        Size = viewport.Size;

        if (!needsAnchor)
            return;
        needsAnchor = false;

        // Center on the cursor; near an edge, slide inward just enough for the wheel to sit flush
        // against it. On a screen too small for the wheel, center it on that axis.
        var extent = new Vector2(MaxExtent);
        var min = viewport.Pos + extent;
        var max = viewport.Pos + viewport.Size - extent;
        var mouse = ImGui.GetIO().MousePos;
        var middle = viewport.Pos + viewport.Size / 2;
        center = new Vector2(
            min.X <= max.X ? Math.Clamp(mouse.X, min.X, max.X) : middle.X,
            min.Y <= max.Y ? Math.Clamp(mouse.Y, min.Y, max.Y) : middle.Y);
    }

    private float Radius => plugin.Configuration.Radius * ImGuiHelpers.GlobalScale;

    /// <summary>
    /// Farthest the wheel ever draws from its center: the open animation's overshoot (~5%), plus a
    /// hovered slice growing out (10px), the click pulse (8px), its outline, and a little breathing room.
    /// </summary>
    private float MaxExtent => Radius * 1.05f + 24f * ImGuiHelpers.GlobalScale;

    public override void Draw()
    {
        var dt = ImGui.GetIO().DeltaTime;
        if (closing)
        {
            outro = MathF.Min(1f, outro + dt / CloseSeconds);
            if (outro >= 1f)
            {
                IsOpen = false;
                return;
            }
        }

        intro = MathF.Min(1f, intro + dt / (firstRing ? OpenSeconds : IntroSeconds));
        RunPendingPress(dt);
        if (!closing && ImGui.IsKeyPressed(ImGuiKey.Escape))
            CloseAnimated();
        UpdateVisibility();

        var drawList = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var frameRadius = (Radius + 6f * scale) * visScale;
        drawList.AddCircleFilled(center, frameRadius,
            ImGui.GetColorU32(HudPalette.Panel with { W = HudPalette.Panel.W * visAlpha }), 96);
        drawList.AddCircle(center, frameRadius, ImGui.GetColorU32(HudPalette.Brass with { W = 0.8f * visAlpha }), 96, scale);
        drawList.AddCircle(center, frameRadius - 3f * scale * visScale,
            ImGui.GetColorU32(HudPalette.Brass with { W = 0.3f * visAlpha }), 96, scale);

        if (favorites.Count == 0)
        {
            DrawHubText(drawList, !Plugin.ClientState.IsLoggedIn
                ? "No emotes available.\nLog in to a character."
                : "No favorites yet.\nAdd them in the\ngame's Emote menu.");
            if (!closing && (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right)))
                CloseAnimated();
            return;
        }

        DrawFavorites(drawList);
    }

    /// <summary>
    /// Favorites in saved order, paged only if they outgrow one ring.
    /// </summary>
    private void DrawFavorites(ImDrawListPtr drawList)
    {
        var pageCount = (favorites.Count + SlicesPerPage - 1) / SlicesPerPage;
        var wheel = ImGui.GetIO().MouseWheel;
        if (pageCount > 1 && wheel != 0 && pressedSlice is null)
        {
            page = (page + (wheel < 0 ? 1 : -1) + pageCount) % pageCount;
            ShowNewRing();
        }

        var first = page * SlicesPerPage;
        var count = Math.Min(SlicesPerPage, favorites.Count - first);
        var hovered = DrawRing(drawList, first, count);
        var label = "Close";
        if (hovered is { } h)
        {
            var emote = favorites[first + h];
            label = Emotes.Name(emote) + (locked.Contains(emote.RowId) ? "\n(locked)" : string.Empty);
        }
        label = $"Favorites\n{label}";
        if (pageCount > 1)
            label += $"\nPage {page + 1}/{pageCount}\n(scroll)";
        DrawHubText(drawList, label);

        if (TryGetClick(hovered, out var index))
        {
            if (index is { } i)
            {
                // Locked emotes are shown for browsing only: they flash red and do nothing.
                var id = favorites[first + i].RowId;
                pressedSlice = i;
                pressTimer = PressSeconds;
                pendingEmote = locked.Contains(id) ? null : id;
            }
            else
            {
                CloseAnimated();
            }
        }
    }

    private void ShowNewRing()
    {
        intro = 0f;
        firstRing = false;
        hoverAnim = [];
        hubAnim = 0f;
        pressedSlice = null;
        pendingEmote = null;
    }

    private void RunPendingPress(float dt)
    {
        if (pressedSlice is null)
            return;
        pressTimer -= dt;
        if (pressTimer > 0f)
            return;

        var emote = pendingEmote;
        pendingEmote = null;
        pressedSlice = null;
        if (emote is { } id)
        {
            Emotes.Execute(id);
            CloseAnimated();
        }
    }

    /// <summary>
    /// Left-click reports the hovered slice (null = the hub). Right-click acts like clicking the hub,
    /// which closes the wheel. Ignored while a click animation is playing.
    /// </summary>
    private bool TryGetClick(int? hovered, out int? index)
    {
        index = null;
        if (pressedSlice is not null || closing)
            return false;

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            index = hovered;
            return true;
        }
        return ImGui.IsMouseClicked(ImGuiMouseButton.Right);
    }

    /// <summary>
    /// Draws the slices clockwise from the top with hover/press animations, and returns the hovered slice.
    /// </summary>
    private int? DrawRing(ImDrawListPtr drawList, int first, int count)
    {
        var io = ImGui.GetIO();
        var scale = ImGuiHelpers.GlobalScale;
        var outer = Radius * visScale;
        var inner = outer * InnerRatio;
        var offset = io.MousePos - center;
        var step = MathF.Tau / count;

        // The whole area outside the hub is selectable by angle, so imprecise flicks still land.
        // While a click animation plays, the pressed slice stays highlighted.
        int? hovered = null;
        if (closing)
        {
            hovered = null;
        }
        else if (pressedSlice is not null)
        {
            hovered = pressedSlice;
        }
        else if (offset.Length() > Radius * InnerRatio)
        {
            var angle = MathF.Atan2(offset.Y, offset.X) + MathF.PI / 2 + step / 2; // 0 = top, clockwise
            hovered = (int)(((angle % MathF.Tau) + MathF.Tau) % MathF.Tau / step) % count;
        }

        if (hoverAnim.Length != count)
            hoverAnim = new float[count];
        var follow = MathF.Min(1f, io.DeltaTime * HoverSpeed);

        // 0 → 1 over the click animation; flash fades out, pulse goes out and back.
        var pressProgress = pressedSlice is null ? 0f : 1f - MathF.Max(0f, pressTimer) / PressSeconds;
        var flash = 1f - pressProgress;
        var pulse = MathF.Sin(pressProgress * MathF.PI);

        var halfGap = count > 1 ? SliceGap * scale * visScale / 2f : 0f;
        for (var i = 0; i < count; i++)
        {
            hoverAnim[i] += ((hovered == i ? 1f : 0f) - hoverAnim[i]) * follow;
            var h = hoverAnim[i];
            var isPressed = pressedSlice == i;
            var emote = favorites[first + i];
            var dimmed = locked.Contains(emote.RowId);

            // Move both edges together so hover and press keep the slice thickness constant.
            var lift = (10f * h + (isPressed ? 8f * pulse : 0f)) * scale;
            var sliceOuter = outer + lift;
            var sliceInner = inner + lift;
            var sliceMid = (sliceInner + sliceOuter) / 2;

            var color = Vector4.Lerp(EmoteColor, HudPalette.Hover, h);
            if (dimmed)
                color = Vector4.Lerp(color, HudPalette.Panel, 0.65f);
            if (isPressed)
                color = Vector4.Lerp(color, dimmed ? DenyColor : FlashColor, flash * 0.8f);
            color.W *= visAlpha;

            var mid = -MathF.PI / 2 + i * step;
            var from = mid - step / 2;
            var to = mid + step / 2;
            DrawSliceFill(drawList, sliceInner, sliceOuter, from, to, halfGap, ImGui.GetColorU32(color));

            if (h > 0.01f)
            {
                var rimColor = isPressed && dimmed ? DenyColor : HudPalette.Gold;
                rimColor.W = (dimmed ? 0.2f : 0.65f) * h * visAlpha;
                var rim = sliceOuter + 1.5f * scale;
                var inset = MathF.Asin(halfGap / rim);
                drawList.PathArcTo(center, rim, from + inset, to - inset, 48);
                drawList.PathStroke(ImGui.GetColorU32(rimColor), ImDrawFlags.None, 1.5f * scale);
            }

            var iconScale = 1f + 0.2f * h + (isPressed ? 0.15f * pulse : 0f);
            var iconSize = new Vector2(IconSize * scale * iconScale * visScale);
            var sliceCenter = PointOnRing(sliceMid, mid);
            var alpha = (dimmed ? 0.3f : 1f) * visAlpha;
            var icon = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(emote.Icon)).GetWrapOrEmpty();
            drawList.AddImage(icon.Handle, sliceCenter - iconSize / 2, sliceCenter + iconSize / 2,
                              Vector2.Zero, Vector2.One, ImGui.GetColorU32(new Vector4(1, 1, 1, alpha)));
        }

        // The hub lights up when hovered, since clicking it closes the wheel.
        var hubHovered = pressedSlice is null && !closing && offset.Length() <= Radius * InnerRatio;
        hubAnim += ((hubHovered ? 1f : 0f) - hubAnim) * follow;
        var hubColor = Vector4.Lerp(HudPalette.Panel, HudPalette.Hover, hubAnim);
        hubColor.W *= visAlpha;
        drawList.AddCircleFilled(center, inner - 4f, ImGui.GetColorU32(hubColor), 48);
        var hubRim = Vector4.Lerp(HudPalette.Brass, HudPalette.Gold, hubAnim);
        hubRim.W = (0.45f + 0.45f * hubAnim) * visAlpha;
        drawList.AddCircle(center, inner - 4f, ImGui.GetColorU32(hubRim), 48, scale);

        return hovered;
    }

    private void DrawSliceFill(ImDrawListPtr drawList, float inner, float outer, float from, float to,
                               float halfGap, uint color)
    {
        // Offset each edge by a fixed distance from the divider at both radii.
        // The resulting sides are parallel across the gap instead of opening like a wedge.
        var innerInset = MathF.Asin(halfGap / inner);
        var outerInset = MathF.Asin(halfGap / outer);
        var segments = Math.Max(2, (int)MathF.Ceiling((to - from) * 96f / MathF.Tau));
        var uv = ImGui.GetFontTexUvWhitePixel();
        drawList.PrimReserve(segments * 6, (segments + 1) * 2);
        var start = (ushort)drawList.VtxCurrentIdx;
        for (var i = 0; i <= segments; i++)
        {
            var t = (float)i / segments;
            var angle = from + (to - from) * t;
            var inset = 1f - 2f * t;
            drawList.PrimWriteVtx(PointOnRing(inner, angle + innerInset * inset), uv, color);
            drawList.PrimWriteVtx(PointOnRing(outer, angle + outerInset * inset), uv, color);
        }
        for (var i = 0; i < segments; i++)
        {
            var index = start + i * 2;
            drawList.PrimWriteIdx((ushort)index);
            drawList.PrimWriteIdx((ushort)(index + 1));
            drawList.PrimWriteIdx((ushort)(index + 3));
            drawList.PrimWriteIdx((ushort)index);
            drawList.PrimWriteIdx((ushort)(index + 3));
            drawList.PrimWriteIdx((ushort)(index + 2));
        }
    }

    private Vector2 PointOnRing(float radius, float angle) =>
        center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

    private void DrawHubText(ImDrawListPtr drawList, string text)
    {
        // Center each line on its own so multi-line labels stay symmetric in the hub.
        var lines = text.Split('\n');
        var lineHeight = ImGui.GetTextLineHeight();
        var color = ImGui.GetColorU32(HudPalette.Text with { W = visAlpha });
        var y = center.Y - lines.Length * lineHeight / 2;
        foreach (var line in lines)
        {
            var width = ImGui.CalcTextSize(line).X;
            drawList.AddText(new Vector2(center.X - width / 2, y), color, line);
            y += lineHeight;
        }
    }

    /// <summary>
    /// Size and opacity for this frame. Opening grows from 50% with a slight overshoot, a new ring grows
    /// from 80%, and closing shrinks to 70% while fading out.
    /// </summary>
    private void UpdateVisibility()
    {
        var grow = firstRing ? 0.5f + 0.5f * EaseOutBack(intro) : 0.8f + 0.2f * EaseOutCubic(intro);
        var shrink = 1f - 0.3f * EaseOutCubic(outro);
        visScale = grow * shrink;
        visAlpha = EaseOutCubic(intro) * (1f - EaseOutCubic(outro));
    }

    private static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * MathF.Pow(t - 1f, 3f) + c1 * MathF.Pow(t - 1f, 2f);
    }
}
