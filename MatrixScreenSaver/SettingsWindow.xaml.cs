using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MatrixScreenSaver
{
    public partial class SettingsWindow : Window
    {
        private readonly Dictionary<CharacterSets, CheckBox> characterSetBoxes = new Dictionary<CharacterSets, CheckBox>();

        private Color baseColor;

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

            baseColor = settings.BaseColorValue;
            SingleColorRadio.IsChecked = settings.ColorMode == ColorMode.SingleColor;
            ColorCycleRadio.IsChecked = settings.ColorMode == ColorMode.ColorCycle;
            RainbowDropsRadio.IsChecked = settings.ColorMode == ColorMode.RainbowDrops;
            UpdateColorPreview();
        }

        private ColorMode SelectedColorMode =>
            ColorCycleRadio.IsChecked == true ? ColorMode.ColorCycle
            : RainbowDropsRadio.IsChecked == true ? ColorMode.RainbowDrops
            : ColorMode.SingleColor;

        private void UpdateColorPreview()
        {
            Color[] palette = ColorPalette.Create(baseColor);
            Color buttonColor = palette[9];
            ColorButton.Background = new SolidColorBrush(buttonColor);
            ColorButton.Foreground = buttonColor.R * 0.2126 + buttonColor.G * 0.7152 + buttonColor.B * 0.0722 < 128 ? Brushes.White : Brushes.Black;

            // One color shows its brightness levels, the other modes the hues they run through.
            IEnumerable<Color> colors = SelectedColorMode == ColorMode.SingleColor
                ? palette
                : Enumerable.Range(0, ColorPalette.Size).Select(i => ColorPalette.Create(ColorPalette.FromHue(120 + i * 360.0 / ColorPalette.Size))[9]);

            ColorPreview.Children.Clear();

            foreach (Color color in colors)
            {
                ColorPreview.Children.Add(new Rectangle { Fill = new SolidColorBrush(color) });
            }
        }

        private void ColorModeChecked(object sender, RoutedEventArgs e)
        {
            UpdateColorPreview();
        }

        private void ColorButtonClick(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(baseColor.R, baseColor.G, baseColor.B),
                FullOpen = true,
            };

            var owner = new System.Windows.Forms.NativeWindow();
            owner.AssignHandle(new WindowInteropHelper(this).Handle);

            try
            {
                if (dialog.ShowDialog(owner) != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
            }
            finally
            {
                owner.ReleaseHandle();
            }

            baseColor = Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
            SingleColorRadio.IsChecked = true;
            UpdateColorPreview();
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
                ColorMode = SelectedColorMode,
                BaseColor = ScreenSaverSettings.ToHex(baseColor),
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
