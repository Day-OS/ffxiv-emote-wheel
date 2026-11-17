using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace EmoteWheel;

[Serializable]
public class KeyBinding
{
    public VirtualKey Key { get; set; } = VirtualKey.NO_KEY;
    public bool Ctrl { get; set; }
    public bool Shift { get; set; }
    public bool Alt { get; set; }

    public bool IsSet => Key != VirtualKey.NO_KEY;

    internal (bool Ctrl, bool Shift, bool Alt) Modifiers => (Ctrl, Shift, Alt);
}

internal sealed class WheelHotkey : IDisposable
{
    private const int VkShift = 0x10, VkControl = 0x11, VkMenu = 0x12;

    private readonly Plugin plugin;
    private bool wheelWasDown;
    private bool catalogWasDown;
    private readonly HashSet<VirtualKey> swallowing = [];

    public WheelHotkey(Plugin plugin)
    {
        this.plugin = plugin;
        Plugin.Framework.Update += OnUpdate;
    }

    public void Dispose() => Plugin.Framework.Update -= OnUpdate;

    private void OnUpdate(IFramework framework)
    {
        var wheel = plugin.Configuration.WheelHotkey;
        var catalog = plugin.Configuration.CatalogHotkey;
        var focused = GameHasFocus();
        var wheelDown = focused && wheel.IsSet && IsPhysicallyDown((int)wheel.Key);
        var catalogDown = focused && catalog.IsSet && IsPhysicallyDown((int)catalog.Key);
        var wheelPressed = wheelDown && !wheelWasDown && HeldModifiers() == wheel.Modifiers;
        var catalogPressed = catalogDown && !catalogWasDown && HeldModifiers() == catalog.Modifiers;
        wheelWasDown = wheelDown;
        catalogWasDown = catalogDown;

        if ((wheelPressed || catalogPressed) && Plugin.ClientState.IsLoggedIn && !IsTyping())
        {
            if (catalogPressed)
                plugin.ToggleCatalog();
            else
                plugin.ToggleWheel();
            swallowing.Add(catalogPressed ? catalog.Key : wheel.Key);
        }

        // A held shortcut is consumed until release without letting auto-repeat toggle the menu.
        swallowing.RemoveWhere(key => !focused || !IsPhysicallyDown((int)key));
        foreach (var key in swallowing)
            if (Plugin.KeyState.IsVirtualKeyValid(key))
                Plugin.KeyState[key] = false;
    }

    private static (bool Ctrl, bool Shift, bool Alt) HeldModifiers() =>
        (IsPhysicallyDown(VkControl), IsPhysicallyDown(VkShift), IsPhysicallyDown(VkMenu));

    private static bool IsPhysicallyDown(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    private static unsafe bool IsTyping()
    {
        if (ImGui.GetIO().WantTextInput)
            return true;
        var ui = UIModule.Instance();
        if (ui == null)
            return false;
        var atk = ui->GetRaptureAtkModule();
        return atk != null && atk->IsTextInputActive();
    }

    private static bool GameHasFocus()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
            return false;
        GetWindowThreadProcessId(foreground, out var processId);
        return processId == Environment.ProcessId;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);
}
