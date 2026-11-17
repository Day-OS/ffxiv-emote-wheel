using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EmoteWheel.Windows;

namespace EmoteWheel;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IUnlockState UnlockState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;

    private const string CommandName = "/emotewheel";

    public Configuration Configuration { get; }

    private readonly WindowSystem windowSystem = new("EmoteWheel");
    private readonly ConfigWindow configWindow;
    private readonly WheelWindow wheelWindow;
    private readonly CatalogWindow catalogWindow;
    private readonly WheelHotkey hotkeys;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        configWindow = new ConfigWindow(this);
        wheelWindow = new WheelWindow(this);
        catalogWindow = new CatalogWindow(this);
        windowSystem.AddWindow(configWindow);
        windowSystem.AddWindow(catalogWindow);
        windowSystem.AddWindow(wheelWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle Favorites at the cursor. Use \"/emotewheel catalog\" for all emotes or \"/emotewheel config\" for settings.",
        });

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleWheel;
        hotkeys = new WheelHotkey(this);
    }

    public void Dispose()
    {
        hotkeys.Dispose();
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleWheel;
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        if (args.Trim().Equals("config", System.StringComparison.OrdinalIgnoreCase))
            ToggleConfigUi();
        else if (args.Trim().Equals("catalog", System.StringComparison.OrdinalIgnoreCase))
            ToggleCatalog();
        else
            ToggleWheel();
    }

    public void ToggleConfigUi() => configWindow.Toggle();
    public void ToggleWheel()
    {
        catalogWindow.RequestClose();
        wheelWindow.ToggleAnimated();
    }

    public void ToggleCatalog()
    {
        wheelWindow.IsOpen = false;
        catalogWindow.ToggleCatalog();
    }
}
