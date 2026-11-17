# Emote Wheel

A Dalamud plugin for FINAL FANTASY XIV that opens a radial wheel of your favorite emotes at the cursor.

## Building

1. Install XIVLauncher and run the game with Dalamud once, so the development files exist in `%AppData%\XIVLauncher\addon\Hooks\dev`.
2. Run `dotnet build -c Release`, or build `EmoteWheel.slnx` in your IDE.
3. In game, open `/xlsettings` → Experimental and add the full path to `EmoteWheel\bin\x64\Release\EmoteWheel.dll` under Dev Plugin Locations.
4. Enable the plugin under Dev Tools > Installed Dev Plugins in `/xlplugins`.
