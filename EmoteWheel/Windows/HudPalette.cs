using System.Numerics;

namespace EmoteWheel.Windows;

internal static class HudPalette
{
    public static readonly Vector4 Panel = new(0.09f, 0.085f, 0.075f, 0.97f);
    public static readonly Vector4 Brass = new(0.59f, 0.48f, 0.28f, 1f);
    public static readonly Vector4 Gold = new(0.92f, 0.79f, 0.53f, 1f);
    public static readonly Vector4 Text = new(0.96f, 0.93f, 0.86f, 1f);
    public static readonly Vector4 MutedText = new(0.72f, 0.68f, 0.59f, 1f);
    public static readonly Vector4 Hover = new(0.29f, 0.24f, 0.16f, 0.96f);
    public static readonly Vector4 Active = new(0.37f, 0.30f, 0.19f, 1f);
}
