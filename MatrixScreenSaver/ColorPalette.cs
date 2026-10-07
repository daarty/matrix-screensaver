// Copyright (c) 2026 daarty
using System;
using System.Windows.Media;

namespace MatrixScreenSaver
{
    public static class ColorPalette
    {
        // Brightness levels of a character, from the black background up to the white head of a drop.
        public const int Size = 15;

        // Level of the base color itself, above it the levels blend towards white.
        public const int BaseLevel = 9;

        public static readonly Color DefaultBaseColor = Colors.Green;

        /// <param name="baseColor">The color a drop shows most of its way, between its white head and the dark trail.</param>
        public static Color[] Create(Color baseColor)
        {
            // Black would give a black palette, nothing would be visible.
            if (baseColor.R == 0 && baseColor.G == 0 && baseColor.B == 0)
            {
                baseColor = DefaultBaseColor;
            }

            // Darker by the ratio of Colors.DarkGreen (0, 100, 0) to Colors.Green (0, 128, 0).
            Color darkColor = Color.FromRgb(
                (byte)(baseColor.R * 100 / 128),
                (byte)(baseColor.G * 100 / 128),
                (byte)(baseColor.B * 100 / 128));

            return new[]
            {
                Colors.Black,
                Mix(Colors.Black, darkColor, 80),
                Mix(Colors.Black, darkColor, 60),
                Mix(Colors.Black, darkColor, 40),
                Mix(Colors.Black, darkColor, 20),
                darkColor,
                Mix(baseColor, darkColor, 25),
                Mix(baseColor, darkColor, 50),
                Mix(baseColor, darkColor, 75),
                baseColor,
                Mix(baseColor, Colors.White, 70),
                Mix(baseColor, Colors.White, 50),
                Mix(baseColor, Colors.White, 20),
                Mix(baseColor, Colors.White, 10),
                Colors.White,
            };
        }

        /// <param name="hue">Degrees on the color wheel, 0 is red, 120 green, 240 blue.</param>
        public static Color FromHue(double hue)
        {
            hue = (hue % 360 + 360) % 360;
            double x = 1 - Math.Abs(hue / 60 % 2 - 1);

            (double r, double g, double b) = (int)(hue / 60) switch
            {
                0 => (1d, x, 0d),
                1 => (x, 1d, 0d),
                2 => (0d, 1d, x),
                3 => (0d, x, 1d),
                4 => (x, 0d, 1d),
                _ => (1d, 0d, x),
            };

            return Color.FromRgb((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
        }

        private static Color Mix(Color firstColor, Color secondColor, int percentOfFirstColor)
        {
            return Color.FromArgb(
                (byte)((firstColor.A * percentOfFirstColor + secondColor.A * (100 - percentOfFirstColor)) / 100),
                (byte)((firstColor.R * percentOfFirstColor + secondColor.R * (100 - percentOfFirstColor)) / 100),
                (byte)((firstColor.G * percentOfFirstColor + secondColor.G * (100 - percentOfFirstColor)) / 100),
                (byte)((firstColor.B * percentOfFirstColor + secondColor.B * (100 - percentOfFirstColor)) / 100));
        }
    }
}
