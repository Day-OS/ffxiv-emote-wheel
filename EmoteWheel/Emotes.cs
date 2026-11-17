using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace EmoteWheel;

internal static class Emotes
{
    public static List<Emote> Get(bool includeLocked)
    {
        if (!Plugin.ClientState.IsLoggedIn)
            return [];

        return Plugin.DataManager.GetExcelSheet<Emote>()
            .Where(e => e.RowId != 0 && e.Icon != 0 && e.TextCommand.RowId != 0 && !e.Name.IsEmpty &&
                        (includeLocked || IsUnlocked(e)))
            .ToList();
    }

    public static bool IsUnlocked(Emote emote) => Plugin.UnlockState.IsEmoteUnlocked(emote);

    public static string Name(Emote emote) => emote.Name.ExtractText();

    public static unsafe List<Emote> GetFavorites()
    {
        if (!Plugin.ClientState.IsLoggedIn)
            return [];

        var ui = UIModule.Instance();
        if (ui == null)
            return [];
        var history = ui->GetEmoteHistoryModule();
        if (history == null)
            return [];

        var sheet = Plugin.DataManager.GetExcelSheet<Emote>();
        var favorites = new List<Emote>();
        foreach (var id in history->Favorites)
        {
            if (id != 0 && sheet.TryGetRow(id, out var emote) && emote.Icon != 0 && !emote.Name.IsEmpty)
                favorites.Add(emote);
        }
        return favorites;
    }

    public static (string Command, string Description) GetHint(Emote emote)
    {
        var command = string.Empty;
        var description = string.Empty;
        if (Plugin.DataManager.GetExcelSheet<TextCommand>().TryGetRow(emote.TextCommand.RowId, out var textCommand))
        {
            command = textCommand.Command.ExtractText().Trim();
            if (command.Length == 0)
                command = textCommand.ShortCommand.ExtractText().Trim();
            description = textCommand.Description.ExtractText().Trim();
        }

        var logId = emote.LogMessageUntargeted.RowId;
        if (description.Length == 0 && logId != 0 &&
            Plugin.DataManager.GetExcelSheet<LogMessage>().TryGetRow(logId, out var log))
            description = log.Text.ExtractText().Trim();

        return (command, description);
    }

    /// <summary>Performs the emote the same way the game's emote menu does (unlock and state checks included).</summary>
    public static void Execute(uint emoteId)
    {
        Plugin.Framework.RunOnFrameworkThread(() =>
        {
            unsafe
            {
                var agent = AgentEmote.Instance();
                if (agent != null)
                    agent->ExecuteEmote((ushort)emoteId, null, true, true);
            }
        });
    }
}
