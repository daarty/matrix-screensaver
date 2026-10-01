using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MatrixScreenSaver
{
    public class ScreenSaverSettings
    {
        public const int MinCharacterSize = 8;
        public const int MaxCharacterSize = 64;
        public const int MinDensity = 1;
        public const int MaxDensity = 20;

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

            if (MatrixCharacter.CreatePool(CharacterSets).Length == 0)
            {
                CharacterSets = DefaultCharacterSets;
            }
        }
    }
}
