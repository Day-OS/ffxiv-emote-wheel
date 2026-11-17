using System;
using System.Numerics;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace EmoteWheel.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration configuration;
    private string? shortcutError;

    public ConfigWindow(Plugin plugin) : base("Emote Wheel Settings###EmoteWheelConfig")
    {
        configuration = plugin.Configuration;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(360, 0) };
    }

    public override void Draw()
    {
        DrawShortcut("Wheel key", configuration.WheelHotkey, configuration.CatalogHotkey);
        DrawShortcut("Catalog key", configuration.CatalogHotkey, configuration.WheelHotkey);
        if (shortcutError is { } error)
            ImGui.TextWrapped(error);

        var radius = configuration.Radius;
        if (ImGui.SliderFloat("Wheel size", ref radius, 100f, 400f, "%.0f"))
        {
            configuration.Radius = radius;
            configuration.Save();
        }

        var showLocked = configuration.ShowLockedEmotes;
        if (ImGui.Checkbox("Show locked emotes", ref showLocked))
        {
            configuration.ShowLockedEmotes = showLocked;
            configuration.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Also list emotes this character hasn't unlocked yet. They appear dimmed and can't be used.");
    }

    private void DrawShortcut(string label, KeyBinding shortcut, KeyBinding other)
    {
        using var id = ImRaii.PushId(label);
        var key = shortcut.Key;
        var (ctrl, shift, alt) = shortcut.Modifiers;
        if (ImGui.BeginCombo(label, key == VirtualKey.NO_KEY ? "None (disabled)" : key.GetFancyName()))
        {
            if (ImGui.Selectable("None (disabled)", key == VirtualKey.NO_KEY))
                key = VirtualKey.NO_KEY;
            foreach (var option in ShortcutKeys.Value)
            {
                using var optionId = ImRaii.PushId((int)option);
                if (ImGui.Selectable(option.GetFancyName(), key == option))
                    key = option;
            }
            ImGui.EndCombo();
        }

        ImGui.BeginDisabled(key == VirtualKey.NO_KEY);
        ImGui.Checkbox("Ctrl", ref ctrl);
        ImGui.SameLine();
        ImGui.Checkbox("Shift", ref shift);
        ImGui.SameLine();
        ImGui.Checkbox("Alt", ref alt);
        ImGui.EndDisabled();

        var modifiers = (ctrl, shift, alt);
        if (key == shortcut.Key && modifiers == shortcut.Modifiers)
            return;
        if (key != VirtualKey.NO_KEY && key == other.Key && modifiers == other.Modifiers)
        {
            shortcutError = "Wheel and catalog shortcuts must use different key combinations.";
            return;
        }

        shortcutError = null;
        shortcut.Key = key;
        (shortcut.Ctrl, shortcut.Shift, shortcut.Alt) = modifiers;
        configuration.Save();
    }

    private static readonly Lazy<VirtualKey[]> ShortcutKeys = new(() => Plugin.KeyState.GetValidVirtualKeys()
        .Where(key => key is not (VirtualKey.NO_KEY or VirtualKey.ESCAPE or
            VirtualKey.LBUTTON or VirtualKey.RBUTTON or VirtualKey.MBUTTON or VirtualKey.XBUTTON1 or VirtualKey.XBUTTON2 or
            VirtualKey.SHIFT or VirtualKey.LSHIFT or VirtualKey.RSHIFT or VirtualKey.CONTROL or VirtualKey.LCONTROL or
            VirtualKey.RCONTROL or VirtualKey.MENU or VirtualKey.LMENU or VirtualKey.RMENU or VirtualKey.LWIN or VirtualKey.RWIN))
        .Distinct().ToArray());
}
