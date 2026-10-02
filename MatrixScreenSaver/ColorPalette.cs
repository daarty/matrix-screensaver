using System.Windows.Media;

namespace MatrixScreenSaver
{
    public static class ColorPalette
    {
        // Brightness levels of a character, from the black background up to the white head of a drop.
        public const int Size = 15;

        public static Color[] Create(Color baseColor)
        {
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
