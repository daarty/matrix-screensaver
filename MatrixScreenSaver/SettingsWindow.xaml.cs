using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MatrixScreenSaver
{
    public partial class SettingsWindow : Window
    {
        private readonly Dictionary<CharacterSets, CheckBox> characterSetBoxes = new Dictionary<CharacterSets, CheckBox>();

        public SettingsWindow(ScreenSaverSettings settings)
        {
            InitializeComponent();

            CharacterSizeSlider.Minimum = ScreenSaverSettings.MinCharacterSize;
            CharacterSizeSlider.Maximum = ScreenSaverSettings.MaxCharacterSize;
            DensitySlider.Minimum = ScreenSaverSettings.MinDensity;
            DensitySlider.Maximum = ScreenSaverSettings.MaxDensity;

            foreach (CharacterSets set in Enum.GetValues<CharacterSets>())
            {
                if (set == CharacterSets.None)
                {
                    continue;
                }

                var box = new CheckBox { Content = set.ToString(), Margin = new Thickness(0, 0, 16, 4) };
                characterSetBoxes[set] = box;
                CharacterSetPanel.Children.Add(box);
            }

            DisplaySettings(settings);
        }

        private void DisplaySettings(ScreenSaverSettings settings)
        {
            CharacterSizeSlider.Value = settings.CharacterSize;
            DensitySlider.Value = settings.Density;

            foreach (var (set, box) in characterSetBoxes)
            {
                box.IsChecked = settings.CharacterSets.HasFlag(set);
            }
        }

        private void DefaultsClick(object sender, RoutedEventArgs e)
        {
            DisplaySettings(new ScreenSaverSettings());
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            var settings = new ScreenSaverSettings
            {
                CharacterSize = (int)CharacterSizeSlider.Value,
                Density = (int)DensitySlider.Value,
                CharacterSets = CharacterSets.None,
            };

            foreach (var (set, box) in characterSetBoxes)
            {
                if (box.IsChecked == true)
                {
                    settings.CharacterSets |= set;
                }
            }

            if (settings.CharacterSets == CharacterSets.None)
            {
                MessageBox.Show(this, "Select at least one character set.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                settings.Save();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, $"The settings could not be saved: {ex.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
        }
    }
}
