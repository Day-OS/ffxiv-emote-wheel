using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Emote = Lumina.Excel.Sheets.Emote;

namespace EmoteWheel.Windows;

/// <summary>A native hover catalog that softens menus covered by the frontmost popup.</summary>
public sealed class CatalogWindow : Window
{
    private const string PopupId = "Emote catalog##menu";
    private const float IconSize = 32f;
    private const float MenuWidth = 280f;
    private const float ScreenMargin = 16f;
    private const float MenuSidePadding = 8f;
    private const float IntroSeconds = 0.16f;

    private sealed class LevelState
    {
        public int LastFrame = -2;
        public float Intro;
        public float Blur;
    }

    private readonly Plugin plugin;
    private List<CatalogEntry> entries = [];
    private readonly List<LevelState> levels = [];
    private (int Depth, Vector2 Min, Vector2 Max)? previousFrontMenu;
    private (int Depth, Vector2 Min, Vector2 Max)? frontMenu;
    private bool needsPopup;
    private bool closeRequested;

    public CatalogWindow(Plugin plugin)
        : base("Emote Catalog##host",
               ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
               ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDocking)
    {
        this.plugin = plugin;
        RespectCloseHotkey = false;
        PositionCondition = ImGuiCond.Always;
        SizeCondition = ImGuiCond.Always;
    }

    public override void OnOpen()
    {
        entries = EmoteGroups.Build(plugin.Configuration.ShowLockedEmotes);
        levels.Clear();
        previousFrontMenu = null;
        needsPopup = true;
    }

    public void RequestClose()
    {
        if (IsOpen)
            closeRequested = true;
    }

    public void ToggleCatalog()
    {
        if (IsOpen)
            RequestClose();
        else
        {
            closeRequested = false;
            IsOpen = true;
        }
    }

    public override void PreDraw()
    {
        var viewport = ImGui.GetMainViewport();
        Position = viewport.Pos;
        Size = viewport.Size;
    }

    public override void Draw()
    {
        var style = ImGui.GetStyle();
        var previousPadding = style.DisplaySafeAreaPadding;
        style.DisplaySafeAreaPadding = Vector2.Max(previousPadding, new Vector2(ScreenMargin * ImGuiHelpers.GlobalScale));
        try
        {
            DrawCatalog();
        }
        finally
        {
            style.DisplaySafeAreaPadding = previousPadding;
        }
    }

    private void DrawCatalog()
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(MenuSidePadding, 16) * scale);
        using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing,
            new Vector2(14f * scale, MathF.Max(8f * scale, (IconSize + 10f) * scale - ImGui.GetTextLineHeight())));
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.PopupRounding, 6f * scale);
        using var border = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, scale);
        using var background = ImRaii.PushColor(ImGuiCol.PopupBg, HudPalette.Panel);
        using var borderColor = ImRaii.PushColor(ImGuiCol.Border, HudPalette.Brass);
        using var separator = ImRaii.PushColor(ImGuiCol.Separator, HudPalette.Brass with { W = 0.5f });
        using var text = ImRaii.PushColor(ImGuiCol.Text, HudPalette.Text);
        using var muted = ImRaii.PushColor(ImGuiCol.TextDisabled, HudPalette.MutedText);
        using var hover = ImRaii.PushColor(ImGuiCol.HeaderHovered, HudPalette.Hover);
        using var active = ImRaii.PushColor(ImGuiCol.HeaderActive, HudPalette.Active);
        if (needsPopup)
        {
            needsPopup = false;
            if (closeRequested)
            {
                IsOpen = false;
                return;
            }
            // Native placement follows the cursor and chooses either side at screen edges.
            ImGui.OpenPopup(PopupId);
        }
        SetMenuBounds();
        if (!ImGui.BeginPopup(PopupId))
        {
            IsOpen = false;
            return;
        }
        if (closeRequested || ImGui.IsKeyPressed(ImGuiKey.Escape) || ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ImGui.CloseCurrentPopup();
            IsOpen = false;
        }
        else
        {
            frontMenu = null;
            DrawLevel(entries, 0);
            previousFrontMenu = frontMenu;
        }
        ImGui.EndPopup();
    }

    private LevelState StateFor(int depth, Vector2 min, Vector2 max)
    {
        while (levels.Count <= depth)
            levels.Add(new LevelState());
        var state = levels[depth];
        var frame = ImGui.GetFrameCount();
        if (state.LastFrame < frame - 1)
        {
            state.Intro = 0;
            state.Blur = 0;
        }
        state.LastFrame = frame;
        var dt = ImGui.GetIO().DeltaTime;
        state.Intro = MathF.Min(1, state.Intro + dt / IntroSeconds);
        // Parents draw first, before the frontmost popup's bounds are available this frame.
        var target = 0f;
        if (previousFrontMenu is { } menu && depth < menu.Depth && min.Y < menu.Max.Y && max.Y > menu.Min.Y)
        {
            // The native edge overlap can also include the parent's scrollbar gutter.
            var overlap = MathF.Min(max.X, menu.Max.X) - MathF.Max(min.X, menu.Min.X);
            var edge = MenuSidePadding * ImGuiHelpers.GlobalScale + ImGui.GetStyle().PopupBorderSize;
            if (ImGui.GetScrollMaxY() > 0f)
                edge += ImGui.GetStyle().ScrollbarSize;
            if (overlap > edge)
                target = MathF.Min((menu.Depth - depth) * 0.6f, 1f);
        }
        state.Blur = target > 0 ? state.Blur + (target - state.Blur) * MathF.Min(1, dt * 12f) : 0f;
        return state;
    }

    private void DrawLevel(List<CatalogEntry> list, int depth)
    {
        var min = ImGui.GetWindowPos();
        var max = min + ImGui.GetWindowSize();
        if (frontMenu is not { } front || depth >= front.Depth)
            frontMenu = (depth, min, max);
        var state = StateFor(depth, min, max);
        var drawList = ImGui.GetWindowDrawList();
        if (!Plugin.ClientState.IsLoggedIn)
            ImGui.TextDisabled("Log in to a character.");
        else if (list.Count == 0)
            ImGui.TextDisabled("No entries available.");
        else
        {
            for (var i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                using var id = ImRaii.PushId(i);
                if (entry.SeparatorBefore)
                    ImGui.Separator();
                if (entry.Children is { } children)
                {
                    if (!BeginIconMenu(entry, state.Blur))
                        continue;
                    DrawLevel(children, depth + 1);
                    ImGui.EndMenu();
                }
                else if (IconMenuItem(entry, state.Blur))
                {
                    entry.Execute();
                    IsOpen = false;
                }
            }
        }
        // Each popup has its own draw list, so fading an ancestor never fades its active submenu.
        var fade = (1f - MathF.Pow(1f - state.Intro, 3f)) * (1f - 0.45f * state.Blur);
        if (fade >= 1f)
            return;

        var vertices = drawList.VtxBuffer;
        for (var i = 0; i < vertices.Size; i++)
        {
            var vertex = vertices[i];
            var alpha = (uint)((vertex.Col >> 24) * fade);
            vertex.Col = (vertex.Col & 0x00FFFFFF) | (alpha << 24);
            vertices[i] = vertex;
        }
    }

    private static bool BeginIconMenu(CatalogEntry entry, float blur)
    {
        var drawList = ImGui.GetWindowDrawList();
        var at = ImGui.GetCursorScreenPos();
        SetMenuBounds();
        bool opened;
        var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        using (ImRaii.PushColor(ImGuiCol.Text, color with { W = color.W * (1f - blur) }))
            opened = ImGui.BeginMenu(IconLabel(entry.Label));
        DrawSoftContent(drawList, at, entry, blur, true);
        return opened;
    }

    private static bool IconMenuItem(CatalogEntry entry, float blur)
    {
        var drawList = ImGui.GetWindowDrawList();
        var at = ImGui.GetCursorScreenPos();
        var enabled = entry.IsEnabled;
        bool clicked;
        var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        using (ImRaii.PushColor(ImGuiCol.Text, color with { W = color.W * (1f - blur) }))
            clicked = ImGui.MenuItem(IconLabel(entry.Label), enabled ? string.Empty : "Locked", false, enabled);
        DrawSoftContent(drawList, at, entry, blur, enabled);
        if (!clicked && entry.Emote is { } emote &&
            ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            DrawEmoteHint(emote);
        return clicked;
    }

    private static void DrawEmoteHint(Emote emote)
    {
        var (command, description) = Emotes.GetHint(emote);
        if (command.Length == 0 && description.Length == 0)
            return;

        ImGui.BeginTooltip();
        if (command.Length > 0)
        {
            using var text = ImRaii.PushColor(ImGuiCol.Text, HudPalette.Gold);
            ImGui.TextUnformatted(command);
        }
        if (description.Length > 0)
        {
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 360f * ImGuiHelpers.GlobalScale);
            ImGui.TextUnformatted(description);
            ImGui.PopTextWrapPos();
        }
        ImGui.EndTooltip();
    }

    private static string IconLabel(string label)
    {
        var width = (IconSize + 12f) * ImGuiHelpers.GlobalScale;
        return new string(' ', (int)MathF.Ceiling(width / ImGui.CalcTextSize(" ").X)) + label;
    }

    private static void SetMenuBounds()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var margin = Vector2.Max(ImGui.GetStyle().DisplaySafeAreaPadding, new Vector2(ScreenMargin * scale));
        var available = Vector2.Max(Vector2.One, ImGui.GetMainViewport().WorkSize - margin * 2);
        ImGui.SetNextWindowSizeConstraints(new Vector2(MathF.Min(MenuWidth * scale, available.X), 0), available);
    }

    private static readonly Vector2[] BlurOffsets =
    [
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
        new(-0.7f, -0.7f), new(0.7f, -0.7f), new(-0.7f, 0.7f), new(0.7f, 0.7f),
    ];

    private static void DrawSoftContent(ImDrawListPtr drawList, Vector2 at, CatalogEntry entry, float blur, bool enabled)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(IconSize * scale);
        var iconAt = at + new Vector2(0, (ImGui.GetTextLineHeight() - size.Y) / 2);
        var alpha = enabled ? 1f : ImGui.GetStyle().DisabledAlpha;
        var radius = 2f * scale * blur;
        if (entry.Icon != 0)
        {
            var icon = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(entry.Icon)).GetWrapOrEmpty();
            if (blur < 0.01f)
                drawList.AddImage(icon.Handle, iconAt, iconAt + size, Vector2.Zero, Vector2.One,
                                  ImGui.GetColorU32(new Vector4(1, 1, 1, alpha)));
            else
            {
                // Small multi-tap blur of our own icons; no game-screen capture or renderer hooks.
                var tint = ImGui.GetColorU32(new Vector4(1, 1, 1, alpha / BlurOffsets.Length));
                foreach (var direction in BlurOffsets)
                {
                    var offset = direction * radius;
                    drawList.AddImage(icon.Handle, iconAt + offset, iconAt + size + offset, Vector2.Zero, Vector2.One, tint);
                }
            }
        }
        if (blur < 0.01f)
            return;
        var textAt = at + new Vector2(ImGui.CalcTextSize(IconLabel(string.Empty)).X, 0);
        var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
        var textColor = ImGui.GetColorU32(color with { W = color.W * alpha * blur / BlurOffsets.Length });
        foreach (var direction in BlurOffsets)
            drawList.AddText(textAt + direction * radius, textColor, entry.Label);
    }
}
