using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MatrixScreenSaver
{
    public partial class SettingsWindow : Window
    {
        private readonly Dictionary<CharacterSets, CheckBox> characterSetBoxes = new Dictionary<CharacterSets, CheckBox>();

        private readonly List<(Slider Slider, Func<ScreenSaverSettings, int> Get, Action<ScreenSaverSettings, int> Set)> advancedSliders = new();

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

            AddAdvancedSlider("Drops starting below the top row (%)", 0, 100, s => s.MidStartPercent, (s, v) => s.MidStartPercent = v);
            Slider minSpeedSlider = AddAdvancedSlider("Slowest drop speed", ScreenSaverSettings.SlowestSpeed, ScreenSaverSettings.FastestSpeed, s => s.MinSpeed, (s, v) => s.MinSpeed = v);
            Slider maxSpeedSlider = AddAdvancedSlider("Fastest drop speed", ScreenSaverSettings.SlowestSpeed, ScreenSaverSettings.FastestSpeed, s => s.MaxSpeed, (s, v) => s.MaxSpeed = v);
            AddAdvancedSlider("Drops flashing down the whole screen (%)", 0, 100, s => s.FlashDropPercent, (s, v) => s.FlashDropPercent = v);
            AddAdvancedSlider("Chance per row that a drop ends (%)", 0, 100, s => s.DropStopPercent, (s, v) => s.DropStopPercent = v);
            AddAdvancedSlider("Chance per frame that a fading character stays (%)", 0, 100, s => s.StuckPercent, (s, v) => s.StuckPercent = v);
            AddAdvancedSlider("Drops below the top row that stay and flicker (%)", 0, 100, s => s.FlickerDropPercent, (s, v) => s.FlickerDropPercent = v);
            AddAdvancedSlider("Chance per flicker that such a drop stops (%)", 0, 100, s => s.FlickerStopPercent, (s, v) => s.FlickerStopPercent = v);
            AddAdvancedSlider("Stopping flicker drops that run down instead of fading (%)", 0, 100, s => s.FlickerMovePercent, (s, v) => s.FlickerMovePercent = v);
            AddAdvancedSlider("Frames per second", ScreenSaverSettings.MinFramesPerSecond, ScreenSaverSettings.MaxFramesPerSecond, s => s.FramesPerSecond, (s, v) => s.FramesPerSecond = v);

            // Moving one speed slider past the other takes the other along.
            minSpeedSlider.ValueChanged += (o, args) => maxSpeedSlider.Value = Math.Max(maxSpeedSlider.Value, args.NewValue);
            maxSpeedSlider.ValueChanged += (o, args) => minSpeedSlider.Value = Math.Min(minSpeedSlider.Value, args.NewValue);

            DisplaySettings(settings);
        }

        private Slider AddAdvancedSlider(string label, int minimum, int maximum, Func<ScreenSaverSettings, int> get, Action<ScreenSaverSettings, int> set)
        {
            var slider = new Slider { Minimum = minimum, Maximum = maximum, IsSnapToTickEnabled = true, TickFrequency = 1 };
            var value = new TextBlock { Width = 40, TextAlignment = TextAlignment.Right };
            value.SetBinding(TextBlock.TextProperty, new Binding(nameof(Slider.Value)) { Source = slider });
            DockPanel.SetDock(value, Dock.Right);

            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
            row.Children.Add(value);
            row.Children.Add(slider);

            AdvancedPanel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, advancedSliders.Count == 0 ? 0 : 12, 0, 0) });
            AdvancedPanel.Children.Add(row);
            advancedSliders.Add((slider, get, set));

            return slider;
        }

        private void DisplaySettings(ScreenSaverSettings settings)
        {
            CharacterSizeSlider.Value = settings.CharacterSize;
            DensitySlider.Value = settings.Density;
            DisplayAdvancedSettings(settings);

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

        private void DisplayAdvancedSettings(ScreenSaverSettings settings)
        {
            foreach (var (slider, get, _) in advancedSliders)
            {
                slider.Value = get(settings);
            }
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
                : Enumerable.Range(0, ColorPalette.Size).Select(i => ColorPalette.FromHue(120 + i * 360.0 / ColorPalette.Size));

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

        private void AdvancedDefaultsClick(object sender, RoutedEventArgs e)
        {
            DisplayAdvancedSettings(new ScreenSaverSettings());
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

            foreach (var (slider, _, set) in advancedSliders)
            {
                set(settings, (int)slider.Value);
            }

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
