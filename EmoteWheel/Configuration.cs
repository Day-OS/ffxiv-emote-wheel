using System;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;

namespace EmoteWheel;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    /// <summary>Outer radius of the wheel, before HUD scaling.</summary>
    public float Radius { get; set; } = 180f;

    public KeyBinding WheelHotkey { get; set; } = new() { Key = VirtualKey.Y };
    public KeyBinding CatalogHotkey { get; set; } = new() { Key = VirtualKey.Y, Shift = true };

    /// <summary>Also show emotes the character hasn't unlocked (dimmed, not usable).</summary>
    public bool ShowLockedEmotes { get; set; }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
