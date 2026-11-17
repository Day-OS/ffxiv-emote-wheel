using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace EmoteWheel;

internal sealed record CatalogEntry(string Label, uint Icon, List<CatalogEntry>? Children = null,
                                    Emote? Emote = null, Ornament? Accessory = null, bool SeparatorBefore = false)
{
    public static CatalogEntry FromEmote(Emote emote) => new(Emotes.Name(emote), emote.Icon, Emote: emote);

    public bool IsEnabled => Emote is { } emote ? Emotes.IsUnlocked(emote)
        : Accessory is not { } accessory || Plugin.UnlockState.IsOrnamentUnlocked(accessory);

    public void Execute()
    {
        if (Emote is { } emote)
            Emotes.Execute(emote.RowId);
        else if (Accessory is { } accessory)
            FashionAccessories.Execute(accessory.RowId);
    }
}
