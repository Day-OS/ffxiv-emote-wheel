using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Emote = Lumina.Excel.Sheets.Emote;
using static EmoteWheel.EmoteId;

namespace EmoteWheel;

internal static class EmoteGroups
{
    private const string PropsName = "Props";

    private sealed record MenuItem(EmoteId IconEmote, string Name = "", MenuItem[]? Children = null)
    {
        public static implicit operator MenuItem(EmoteId emote) => new(emote);
    }

    private static MenuItem Menu(string name, EmoteId icon, params MenuItem[] children) => new(icon, name, children);

    private static readonly MenuItem[] Definitions =
    [
        Menu("Social", Wave,
            Yes, ThumbsUp, No, Deny,
            Menu("Greetings", Wave,
                Bow, EasternBow, Welcome, Greeting, Goodbye, Wave, EasternGreeting, LaliHo, Ohokaliy),
            Menu("Salutes", Salute,
                Kneel, Salute, StormSalute, SerpentSalute, FlameSalute, ImperialSalute),
            Menu("Comfort", Comfort, Rally, Haurchefant, Comfort, Soothe),
            Menu("Affection", Hug, BlowKiss, Dote, Hug, Embrace, Pet, LoveHeart, FistBump, HighFive, Bouquet),
            Menu("Requests & Apologies", PrettyPlease, Grovel, PrettyPlease),
            Menu("Teasing & Threats", Deride, Slap, Poke, Deride, Frighten, Malevolence),
            Menu("Respect & Prayer", Pray, Pray, RitualPrayer, PayRespects, HandToHeart),
            Menu("Conversation", Converse, Converse, Elucidate, Insist, AirQuotes, Linkpearl, Scheme),
            Menu("Scold & Shush", Reprimand, Reprimand, Shush),
            Menu("Point & Signal", Point, Me, Point, Beckon, Snap)),
        Menu("Reactions", Joy,
            Menu("Laugh", Laugh, Chuckle, Laugh, Cackle),
            Menu("Happy & Excited", Joy, Joy, Happy, FistPump, Delighted,
                JumpForJoy1, JumpForJoy2, JumpForJoy3, JumpForJoy4, JumpForJoy5),
            Menu("Shy & Smitten", Charmed, Blush, Charmed),
            Menu("Sad", Cry, Cry, Disappointed, Sulk),
            Menu("Angry & Frustrated", Angry, Angry, Furious, Fume, Stomp, Vexed, Rage, Upset, Facepalm, ClutchHead),
            Menu("Surprised", Shocked, Shocked, Surprised, Aback, Overreact, Wow),
            Menu("Scared", Tremble, Panic, Tremble),
            Menu("Think & Question", Think, Shrug, Think, Huh, Doubt, Consider, Eureka),
            Menu("Observe & Check", Lookout, Lookout, Confirm),
            Menu("Pain & Discomfort", Headache, Sweat, Shiver, Headache),
            Menu("Impact & Falls", BlownAway, Stagger, Dazed, BlownAway)),
        Menu("Faces", Smile,
            PuckerUp,
            // Face-only emotes stay separate from full-body reactions.
            Menu("Smiles", Grin,
                Smile, Grin, Beam, BigGrin, Content, Simper, FakeSmile),
            Menu("Eyes", WinkRight, WinkRight, WinkLeft, ShutEyes),
            Menu("Sad & Afraid", Worried, Sad, Scared, Worried),
            Menu("Frowns", Annoyed, Annoyed, Furrow),
            Menu("Smirks & Sneers", Smirk, Smirk, Taunt, Sneer, Scoff),
            Menu("Pain", Ouch, Ouch, Endure),
            Menu("Thoughtful", Ponder, Concentrate, Confused, Reflect, Ponder),
            Menu("Surprised & Alert", Amazed, Alert, Amazed),
            Menu("Neutral", StraightFace, StraightFace)),
        Menu("Poses", Pose,
            Pose,
            Menu("Show Off", Flex, Flex, PoseOfTheUnbound, HumbleTriumph, LookBack, MostGentlemanly, VictoryReveal),
            Menu("Presentation", ShowRight, RunwayWalk, ExamineSelf, Twirl, ShowRight, ShowLeft),
            Menu("Stances", Lean, Lean, Guard, Attention, AtEase, Attend, WringHands),
            Menu("Ranger Poses", RedRangerPoseA,
                RedRangerPoseA, RedRangerPoseB, BlackRangerPoseA, BlackRangerPoseB, YellowRangerPoseA,
                YellowRangerPoseB),
            Menu("Weapons", DrawWeapon, DrawWeapon, SheatheWeapon, BattleStance, Victory),
            Menu("Rest & Recovery", Sit, Sit, SitOnGround, Doze, PlayDead, WaterFloat, Slump, Winded)),
        Menu("Movement", Backflip,
            Menu("Exercise", Squats, Squats, PushUps, SitUps, BreathControl, LimberUp, EasternStretch, Stretch),
            Menu("Martial Arts", Box, Box, CrimsonLotus),
            Menu("Jumps & Flips", Backflip, Backflip, WaterFlip, AdventOfLight),
            Menu("Get Ready", Determined, Psych, Determined, PowerUp)),
        Menu("Dance & Music", Dance,
            Dance,
            Menu("Traditional", HarvestDance,
                HarvestDance, BombDance, ThavnairianDance, SundropDance, MoonliftDance, EasternDance, YolDance,
                LittleLadiesDance),
            Menu("Steps & Hops", BoxStep,
                StepDance, BoxStep, SideStep, GetFantasy, PopotoStep, HeelToe, GoobbueDo, Breaking, LaliHop, LopHop),
            Menu("Party & Show", BeesKnees,
                BallDance, GoldDance, MandervilleDance, MandervilleMambo, MoogleDance, FlameDance,
                BeesKnees, Songbird, SenorSabotender),
            Menu("Music", Hum, Hum, Conduct)),
        Menu("Cheering", Cheer,
            Menu("Applause", Clap, Clap, Congratulate, Cheer, Huzzah, Spirit),
            Menu("Flowers & Coins", FlowerShower, FlowerShower, Gratuity),
            Menu("Fans", Wasshoi, Wasshoi, Uchiwasshoi),
            Menu("Glowsticks", CheerOnBlue,
                Menu("On", CheerOnBlue, CheerOnBlue, CheerOnBright, CheerOnOrange),
                Menu("Wave", CheerWaveYellow, CheerWaveYellow, CheerWaveViolet, CheerWavePink),
                Menu("Jump", CheerJumpRed, CheerJumpRed, CheerJumpGreen, CheerJumpIndigo),
                Menu("Rhythm", CheerRhythmRed, CheerRhythmRed, CheerRhythmViolet, CheerRhythmBright),
                Menu("Light", CheerLightYellow, CheerLightYellow, CheerLightBlue, CheerLightGreen))),
        Menu(PropsName, Read,
            Menu("Food & Sweets", EatApple,
                BreakFast, EatRiceBall, EatApple, EatPizza, EatEgg, EatChicken, EatTaco, DevourTaco,
                EatPumpkinCookie, EatChocolate),
            Menu("Drinks & Toasts", DrinkTea, Toast, DrinkTea, DrinkGreenTea, SavorTea,
                LominsanSip, GridanianSip, UldahnSip, LominsanGulp, GridanianGulp, UldahnGulp),
            Menu("Cooking & Mixing", FryEgg, FryEgg, ShakeDrink),
            Menu("Books & Writing", Read,
                Read, Reference, Study, CarryBook, Pen),
            Menu("Devices", Tomestone, Tomestone, Tomescroll, Magitype),
            Menu("Camera & Map", Photograph, Photograph, VisageCapture, Navigate),
            Menu("Chores", SweepUp, SweepUp, Water),
            Menu("Objects & Toys", BlowBubbles, HandOver, Throw, BlowBubbles),
            Menu("Paint", PaintItRed, PaintItBlack, PaintItRed, PaintItYellow, PaintItBlue),
            Menu("Visor & Glasses", Visor, Visor, Spectacles)),
        Menu("Magic & Play", MagicTrick,
            Menu("Spells & Summons", Megaflare,
                Zantetsuken, DiamondDust, Megaflare, Ultima, SimulationM, SimulationF, Sundering),
            Menu("Light Effects", DivineArm, DivineArm, DivineDisk, DivineTiara),
            Menu("Tricks & Play", MagicTrick, MagicTrick, AllSaintsCharm, EarWiggle, Pantomime, Splash)),
    ];

    public static List<CatalogEntry> Build(bool includeLocked)
    {
        if (!Plugin.ClientState.IsLoggedIn)
            return [];

        var remaining = Emotes.Get(includeLocked).ToDictionary(e => (EmoteId)e.RowId);
        var entries = new List<CatalogEntry>();
        if (remaining.Remove(ChangePose, out var pose))
            entries.Add(CatalogEntry.FromEmote(pose));

        var favorites = Emotes.GetFavorites().Select(CatalogEntry.FromEmote).ToList();
        entries.Add(CountedMenu("Favorites", favorites.FirstOrDefault()?.Icon ?? 0u, favorites, favorites.Count));

        var firstCategory = entries.Count;
        entries.AddRange(BuildCategories(remaining, includeLocked));
        if (entries.Count > firstCategory)
            entries[firstCategory] = entries[firstCategory] with { SeparatorBefore = true };
        return entries;
    }

    private static IEnumerable<CatalogEntry> BuildCategories(Dictionary<EmoteId, Emote> remaining, bool includeLocked)
    {
        var sheet = Plugin.DataManager.GetExcelSheet<Emote>();
        var (accessories, accessoryCount) = BuildAccessories(includeLocked);
        var hasProps = false;

        foreach (var definition in Definitions)
        {
            var (children, count, _) = ResolveMenu(definition.Children!, remaining, sheet, category: true);
            if (count == 0)
                continue;

            if (definition.Name == PropsName)
            {
                children.Add(accessories with { SeparatorBefore = true });
                count += accessoryCount;
                hasProps = true;
            }
            yield return CountedMenu(definition.Name, IconOf(sheet, definition.IconEmote), children, count);
        }

        if (!hasProps && accessoryCount > 0)
            yield return CountedMenu(PropsName, accessories.Icon, [accessories], accessoryCount);

        // Unclassified emotes from the live game sheet always go in the last category.
        if (remaining.Count > 0)
        {
            var other = InGameOrder(remaining.Values);
            yield return CountedMenu("Other", other[0].Icon, other.Select(CatalogEntry.FromEmote).ToList(), other.Count);
        }
    }

    private static (CatalogEntry Entry, int Count) BuildAccessories(bool includeLocked)
    {
        var accessories = FashionAccessories.Get();
        var icon = accessories.Count > 0 ? accessories[0].Icon : 0u;
        var visible = accessories.Where(o => includeLocked || Plugin.UnlockState.IsOrnamentUnlocked(o))
            .Select(o => new CatalogEntry(o.Singular.ExtractText(), o.Icon, Accessory: o)).ToList();
        return (CountedMenu("Fashion Accessories", icon, visible, visible.Count), visible.Count);
    }

    private static (List<CatalogEntry> Entries, int Count, int Order) ResolveMenu(
        MenuItem[] definitions, Dictionary<EmoteId, Emote> remaining, ExcelSheet<Emote> sheet, bool category = false)
    {
        var emotes = new List<Emote>();
        var submenus = new List<(MenuItem Definition, (List<CatalogEntry> Entries, int Count, int Order) Menu)>();
        foreach (var item in definitions)
        {
            if (item.Children is { } children)
            {
                var menu = ResolveMenu(children, remaining, sheet);
                if (menu.Count > 0)
                    submenus.Add((item, menu));
            }
            else if (remaining.Remove(item.IconEmote, out var emote))
            {
                emotes.Add(emote);
            }
        }

        // Categories with just one visible submenu can show its contents directly.
        if (category && emotes.Count == 0 && submenus is [var only])
            return only.Menu;

        var items = InGameOrder(emotes).Select(emote => (Entry: CatalogEntry.FromEmote(emote), Order: (int)emote.Order)).ToList();
        var count = emotes.Count;
        foreach (var (child, menu) in submenus)
        {
            count += menu.Count;
            var entry = menu.Entries.Count == 1 ? menu.Entries[0]
                : CountedMenu(child.Name, IconOf(sheet, child.IconEmote), menu.Entries, menu.Count);
            items.Add((entry, menu.Order));
        }
        var order = items.Count > 0 ? items.Min(item => item.Order) : int.MaxValue;
        var ordered = category ? items.AsEnumerable() : items.OrderBy(item => item.Order);
        return (ordered.Select(item => item.Entry).ToList(), count, order);
    }

    private static CatalogEntry CountedMenu(string name, uint icon, List<CatalogEntry> children, int count) =>
        new($"{name} ({count})", icon, children);

    private static uint IconOf(ExcelSheet<Emote> sheet, EmoteId id) =>
        sheet.TryGetRow((uint)id, out var row) ? row.Icon : 0u;

    private static List<Emote> InGameOrder(IEnumerable<Emote> emotes) =>
        emotes.OrderBy(e => e.Order).ThenBy(e => e.RowId).ToList();
}
