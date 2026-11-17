using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace EmoteWheel;

internal static class FashionAccessories
{
    public static List<Ornament> Get()
    {
        if (!Plugin.ClientState.IsLoggedIn)
            return [];

        return Plugin.DataManager.GetExcelSheet<Ornament>()
            .Where(o => o.RowId != 0 && o.Icon != 0 && !o.Singular.IsEmpty)
            .OrderBy(o => o.Order).ThenBy(o => o.RowId)
            .ToList();
    }

    public static void Execute(uint ornamentId)
    {
        Plugin.Framework.RunOnFrameworkThread(() =>
        {
            if (!Plugin.ClientState.IsLoggedIn ||
                !Plugin.DataManager.GetExcelSheet<Ornament>().TryGetRow(ornamentId, out var ornament) ||
                !Plugin.UnlockState.IsOrnamentUnlocked(ornament))
                return;

            unsafe
            {
                var manager = ActionManager.Instance();
                if (manager != null)
                    manager->UseAction(ActionType.Ornament, ornamentId);
            }
        });
    }
}
