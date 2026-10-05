using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace MatrixScreenSaver
{
    public enum ColorMode
    {
        // All drops in the base color.
        SingleColor,

        // The whole palette moves through the color wheel.
        ColorCycle,

        // Every drop gets its own color.
        RainbowDrops,
    }

    public class ScreenSaverSettings
    {
        public const int MinCharacterSize = 8;
        public const int MaxCharacterSize = 64;
        public const int MinDensity = 1;
        public const int MaxDensity = 20;
        public const int SlowestSpeed = 1;
        public const int FastestSpeed = 20;
        public const int MinFramesPerSecond = 1;
        public const int MaxFramesPerSecond = 60;

        private const CharacterSets DefaultCharacterSets = CharacterSets.Latin | CharacterSets.Katakana | CharacterSets.Digits | CharacterSets.Symbols;

        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MatrixScreenSaver", "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        public int CharacterSize { get; set; } = 16;

        public CharacterSets CharacterSets { get; set; } = DefaultCharacterSets;

        /// <summary>
        /// New drops per column and minute.
        /// </summary>
        public int Density { get; set; } = 2;

        public ColorMode ColorMode { get; set; } = ColorMode.SingleColor;

        /// <summary>
        /// Color of the drops in <see cref="ColorMode.SingleColor"/>, as #RRGGBB.
        /// </summary>
        public string BaseColor { get; set; } = ToHex(ColorPalette.DefaultBaseColor);

        [JsonIgnore]
        public Color BaseColorValue => TryParseColor(BaseColor, out Color color) ? color : ColorPalette.DefaultBaseColor;

        /// <summary>
        /// Share of the new drops that start below the top row, in percent.
        /// </summary>
        public int MidStartPercent { get; set; } = 50;

        public int MinSpeed { get; set; } = SlowestSpeed;

        public int MaxSpeed { get; set; } = FastestSpeed;

        /// <summary>
        /// Share of the drops that flash down the whole screen, in percent.
        /// </summary>
        public int FlashDropPercent { get; set; } = 2;

        /// <summary>
        /// Chance per row that a drop ends before it reaches the bottom, in percent.
        /// </summary>
        public int DropStopPercent { get; set; } = 0;

        /// <summary>
        /// Chance per frame that a fading character keeps its color on the screen, in percent.
        /// </summary>
        public int StuckPercent { get; set; } = 33;

        /// <summary>
        /// Share of the drops below the top row that stay in place and flicker, in percent.
        /// </summary>
        public int FlickerDropPercent { get; set; } = 3;

        /// <summary>
        /// Chance per flicker that such a drop stops flickering, in percent.
        /// </summary>
        public int FlickerStopPercent { get; set; } = 5;

        /// <summary>
        /// Share of the stopping flicker drops that run down from there instead of fading out, in percent.
        /// </summary>
        public int FlickerMovePercent { get; set; } = 25;

        public int FramesPerSecond { get; set; } = 15;

        public ScreenSaverSettings Clone()
        {
            return (ScreenSaverSettings)MemberwiseClone();
        }

        public static ScreenSaverSettings Load()
        {
            ScreenSaverSettings settings = null;

            try
            {
                if (File.Exists(FilePath))
                {
                    settings = JsonSerializer.Deserialize<ScreenSaverSettings>(File.ReadAllText(FilePath), JsonOptions);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
            {
                // A screensaver must start anyway, so a broken file just means defaults.
            }

            settings ??= new ScreenSaverSettings();
            settings.Normalize();
            return settings;
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }

        private void Normalize()
        {
            CharacterSize = Math.Clamp(CharacterSize, MinCharacterSize, MaxCharacterSize);
            Density = Math.Clamp(Density, MinDensity, MaxDensity);
            MidStartPercent = Math.Clamp(MidStartPercent, 0, 100);
            MinSpeed = Math.Clamp(MinSpeed, SlowestSpeed, FastestSpeed);
            MaxSpeed = Math.Clamp(MaxSpeed, MinSpeed, FastestSpeed);
            FlashDropPercent = Math.Clamp(FlashDropPercent, 0, 100);
            DropStopPercent = Math.Clamp(DropStopPercent, 0, 100);
            StuckPercent = Math.Clamp(StuckPercent, 0, 100);
            FlickerDropPercent = Math.Clamp(FlickerDropPercent, 0, 100);
            FlickerStopPercent = Math.Clamp(FlickerStopPercent, 0, 100);
            FlickerMovePercent = Math.Clamp(FlickerMovePercent, 0, 100);
            FramesPerSecond = Math.Clamp(FramesPerSecond, MinFramesPerSecond, MaxFramesPerSecond);

            if (MatrixCharacter.CreatePool(CharacterSets).Length == 0)
            {
                CharacterSets = DefaultCharacterSets;
            }

            if (!Enum.IsDefined(ColorMode))
            {
                ColorMode = ColorMode.SingleColor;
            }

            BaseColor = ToHex(BaseColorValue);
        }

        public static string ToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static bool TryParseColor(string text, out Color color)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(text) && ColorConverter.ConvertFromString(text) is Color parsed)
                {
                    color = parsed;
                    return true;
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException)
            {
                // A hand-edited file may contain anything, the color then falls back to the default.
            }

            color = default;
            return false;
        }
    }
}
