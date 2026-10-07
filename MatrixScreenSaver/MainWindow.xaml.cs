// Copyright (c) 2026 daarty
// Copyright (c) 2015 Wm. Barrett Simms wbsimms.com
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MatrixScreenSaver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Changes per second of a flicker drop at the slowest and at the fastest speed.
        private const double MinFlickersPerSecond = 1;
        private const double MaxFlickersPerSecond = 4;

        private const int RainbowHues = 24;

        private const double ColorCycleSeconds = 60;

        // Recomputing the palette means redrawing every visible character, so it happens every few frames only.
        private const int ColorCycleFrames = 8;

        // Hue of Colors.Green, where the color cycle starts.
        private const double GreenHue = 120;

        // In screen pixels.
        private const double MouseMoveTolerance = 10;

        // The glow source is the glyph widened by this many pixels, a thin stroke alone blurs to almost nothing.
        private const int GlowDilation = 2;

        private readonly ScreenSaverSettings settings;

        // Premultiplied pixel value for each palette, level and glyph coverage (0-255).
        private int[] colorTable;

        private readonly int characterSize;
        private readonly char[] characterPool;

        // Chance per column and frame to start a new drop in the top row or in one of the rows below.
        private readonly double newTopDropProbability;
        private readonly double newMidDropProbability;

        private Point? initialMousePosition;

        // Size of a character cell in physical pixels.
        private int cellPixels;

        private WriteableBitmap bitmap;

        // Only the bright characters, blurred by the effect on GlowImage; null without glow.
        private WriteableBitmap glowBitmap;

        // Coverage of every glyph of the pool, cellPixels * cellPixels bytes each, and the widened coverage for the glow.
        private byte[] glyphMasks;
        private byte[] glowMasks;
        private Dictionary<char, int> glyphIndices;

        private int columns;

        private Random random = new Random();
        private int rows;
        private readonly TimeSpan frameTime;

        public MainWindow(ScreenSaverSettings settings)
        {
            this.settings = settings;
            characterSize = settings.CharacterSize;
            characterPool = MatrixCharacter.CreatePool(settings.CharacterSets);
            colorTable = settings.ColorMode switch
            {
                ColorMode.ColorCycle => CreateColorTable(ColorPalette.Create(ColorPalette.FromHue(GreenHue))),
                ColorMode.RainbowDrops => CreateColorTable(Enumerable.Range(0, RainbowHues)
                    .Select(i => ColorPalette.Create(ColorPalette.FromHue(GreenHue + i * 360.0 / RainbowHues)))
                    .ToArray()),
                _ => CreateColorTable(ColorPalette.Create(settings.BaseColorValue)),
            };
            frameTime = TimeSpan.FromSeconds(1.0 / settings.FramesPerSecond);

            double newDropProbability = settings.Density * frameTime.TotalMilliseconds / TimeSpan.FromMinutes(1).TotalMilliseconds;
            newMidDropProbability = newDropProbability * settings.MidStartPercent / 100;
            newTopDropProbability = newDropProbability - newMidDropProbability;

            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            // The preview shows only the grid, without the window, so the grid starts the animation.
            MainGrid.Loaded += MainGrid_Loaded;
        }

        public MatrixCharacter[,] MatrixGrid { get; private set; }

        private static int[] CreateColorTable(params Color[][] palettes)
        {
            var table = new int[palettes.Length * ColorPalette.Size * 256];

            for (int palette = 0; palette < palettes.Length; palette++)
            {
                for (int level = 0; level < ColorPalette.Size; level++)
                {
                    Color color = palettes[palette][level];
                    int offset = (palette * ColorPalette.Size + level) * 256;

                    for (int coverage = 0; coverage < 256; coverage++)
                    {
                        table[offset + coverage] =
                            coverage << 24 | (color.R * coverage / 255) << 16 | (color.G * coverage / 255) << 8 | (color.B * coverage / 255);
                    }
                }
            }

            return table;
        }

        /// <returns>True if the character below was just created and must not move on in this frame.</returns>
        private bool CalculateNewCharacters(int column, int row, List<Coordinate> changedValues)
        {
            var thisCharacter = MatrixGrid[column, row];
            bool createdNextCharacter = false;
            bool headAbove = row > 0 && MatrixGrid[column, row - 1].Brush == ColorPalette.Size - 2;

            if (thisCharacter.IsFlicker)
            {
                if (!headAbove)
                {
                    Flicker(column, row, changedValues);
                    return false;
                }

                // A drop from above runs over the flickering character.
                thisCharacter.IsFlicker = false;
                thisCharacter.Brush = 0;
                QueueRedraw(column, row, changedValues);
            }

            int fade = thisCharacter.Brush == 0 ? 0 : FadeLevels(thisCharacter);

            if (fade > 0)
            {
                thisCharacter.Brush = Math.Max(0, thisCharacter.Brush - fade);

                // Sometimes simply don't update the color so it gets stuck on the screen.
                // The chance counts per level, otherwise a short word fading several levels per frame leaves brighter residues.
                if (random.NextDouble() >= Math.Pow(settings.StuckPercent / 100.0, fade))
                {
                    QueueRedraw(column, row, changedValues);
                }
            }

            // If the letter above is one less than white -> create next letter in the current row.
            // A flash drop always runs to the bottom, any other may end here.
            else if (headAbove && (MatrixGrid[column, row - 1].IsFlash || random.Next(100) >= settings.DropStopPercent))
            {
                var dropAbove = MatrixGrid[column, row - 1];

                // If not in last row and at high speed, sometimes jump two blocks
                if (row < rows - 1 && dropAbove.Speed > ScreenSaverSettings.FastestSpeed / 2
                    && random.Next(ScreenSaverSettings.FastestSpeed - dropAbove.Speed) == 0)
                {
                    var nextCharacter = MatrixGrid[column, row + 1];
                    nextCharacter.Brush = ColorPalette.Size - 1;
                    nextCharacter.Character = RandomCharacter();
                    nextCharacter.Speed = dropAbove.Speed;
                    nextCharacter.WordLength = dropAbove.WordLength;
                    nextCharacter.IsFlash = dropAbove.IsFlash;
                    nextCharacter.IsFlicker = false;
                    nextCharacter.Palette = dropAbove.Palette;

                    QueueRedraw(column, row + 1, changedValues);

                    thisCharacter.Brush = ColorPalette.Size - 2;

                    // Processing the new character in the same frame would let the drop run on and on.
                    createdNextCharacter = !nextCharacter.IsFlash;
                }
                else
                {
                    thisCharacter.Brush = ColorPalette.Size - 1;
                }

                thisCharacter.Character = RandomCharacter();
                thisCharacter.Speed = dropAbove.Speed;
                thisCharacter.WordLength = dropAbove.WordLength;
                thisCharacter.IsFlash = dropAbove.IsFlash;
                thisCharacter.Palette = dropAbove.Palette;

                QueueRedraw(column, row, changedValues);
            }

            return createdNextCharacter;
        }

        /// <returns>Levels the character darkens this frame.</returns>
        private int FadeLevels(MatrixCharacter character)
        {
            // The head moves on with the drop's speed, speed 1 never moves.
            if (character.Brush == ColorPalette.Size - 1)
            {
                return random.Next(character.Speed) != 0 ? 1 : 0;
            }

            // The trail fades out in the frames the head needs for WordLength rows, so the visible length does not depend on the speed.
            int levelsPerFrame = (ColorPalette.Size - 2) * (character.Speed - 1) * 256 / (character.Speed * character.WordLength);

            return levelsPerFrame / 256 + (random.Next(256) < levelsPerFrame % 256 ? 1 : 0);
        }

        private void QueueRedraw(int column, int row, List<Coordinate> changedValues)
        {
            MatrixCharacter character = MatrixGrid[column, row];
            character.DisplayedBrush = character.Brush;

            changedValues.Add(new Coordinate { Column = column, Row = row });
        }

        private void Flicker(int column, int row, List<Coordinate> changedValues)
        {
            MatrixCharacter character = MatrixGrid[column, row];

            if (--character.FlickerCountdown > 0)
            {
                return;
            }

            character.FlickerCountdown = FlickerInterval(character.Speed);

            if (random.Next(100) < settings.FlickerStopPercent)
            {
                character.IsFlicker = false;

                // At the head level the row below takes it up and it runs down from here, one level lower it only fades.
                if (random.Next(100) >= settings.FlickerMovePercent)
                {
                    character.Brush = ColorPalette.Size - 3;
                }
            }
            else
            {
                character.Character = RandomCharacter();
            }

            QueueRedraw(column, row, changedValues);
        }

        /// <returns>Frames between two changes; the speed maps linearly onto the flicker rate.</returns>
        private int FlickerInterval(int speed)
        {
            double flickersPerSecond = MinFlickersPerSecond + (MaxFlickersPerSecond - MinFlickersPerSecond)
                * (speed - ScreenSaverSettings.SlowestSpeed) / (ScreenSaverSettings.FastestSpeed - ScreenSaverSettings.SlowestSpeed);

            return Math.Max(1, (int)Math.Round(settings.FramesPerSecond / flickersPerSecond));
        }

        private void CreateScene()
        {
            // Timer
            var timeStampCreationFirst = DateTime.Now;

            // Drawing in physical pixels keeps the characters sharp at any display scaling.
            DpiScale dpi = VisualTreeHelper.GetDpi(MainGrid);
            cellPixels = Math.Max(1, (int)Math.Round(characterSize * dpi.DpiScaleX));

            columns = (int)Math.Ceiling(MainGrid.RenderSize.Width * dpi.DpiScaleX / cellPixels);
            rows = (int)Math.Ceiling(MainGrid.RenderSize.Height * dpi.DpiScaleY / cellPixels);

            CreateGlyphMasks(dpi);

            bitmap = new WriteableBitmap(columns * cellPixels, rows * cellPixels, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32, null);
            MatrixImage.Source = bitmap;

            // Without hardware acceleration (remote desktop, some VMs) a full-screen blur per frame stutters badly.
            bool hardwareRendering = RenderCapability.Tier >> 16 > 0;

            if (settings.Glow && settings.GlowIntensityPercent > 0 && hardwareRendering)
            {
                glowBitmap = new WriteableBitmap(columns * cellPixels, rows * cellPixels, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32, null);
                GlowImage.Source = glowBitmap;
                GlowImage.Opacity = settings.GlowIntensityPercent / 100.0;
                GlowBlur.Radius = settings.GlowRadius;
            }
            else
            {
                GlowImage.Visibility = Visibility.Collapsed;
            }

            MatrixGrid = new MatrixCharacter[columns, rows];

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    MatrixGrid[i, j] = new MatrixCharacter();
                }
            }

            // Timer
            var timeStampCreationSecond = DateTime.Now;
            var timeSpanCreation = (timeStampCreationSecond.Subtract(timeStampCreationFirst));

            Console.WriteLine($"Creation took {timeSpanCreation} ms");

            // Every screen needs its own list
            var changedValues = new List<Coordinate>();
            Task.Run(() => RunAnimation(changedValues));
        }

        private void CreateGlyphMasks(DpiScale dpi)
        {
            // Render every glyph once into an atlas and keep only its coverage.
            var typeface = new Typeface(SystemFonts.MessageFontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            int atlasColumns = (int)Math.Ceiling(Math.Sqrt(characterPool.Length));
            int atlasRows = (int)Math.Ceiling((double)characterPool.Length / atlasColumns);
            double cellSize = cellPixels / dpi.DpiScaleX;

            var visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                for (int i = 0; i < characterPool.Length; i++)
                {
                    var origin = new Point(i % atlasColumns * cellSize, i / atlasColumns * cellSize);
                    var text = new FormattedText(
                        characterPool[i].ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        typeface, characterSize * 0.75, Brushes.White, dpi.PixelsPerDip);

                    context.PushClip(new RectangleGeometry(new Rect(origin, new Size(cellSize, cellSize))));
                    context.DrawText(text, origin);
                    context.Pop();
                }
            }

            int atlasWidth = atlasColumns * cellPixels;
            var atlas = new RenderTargetBitmap(atlasWidth, atlasRows * cellPixels, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            atlas.Render(visual);

            var atlasPixels = new int[atlasWidth * atlasRows * cellPixels];
            atlas.CopyPixels(atlasPixels, atlasWidth * 4, 0);

            glyphMasks = new byte[characterPool.Length * cellPixels * cellPixels];
            glowMasks = new byte[glyphMasks.Length];
            glyphIndices = new Dictionary<char, int>();

            for (int i = 0; i < characterPool.Length; i++)
            {
                glyphIndices[characterPool[i]] = i;

                int left = i % atlasColumns * cellPixels;
                int top = i / atlasColumns * cellPixels;

                for (int y = 0; y < cellPixels; y++)
                {
                    for (int x = 0; x < cellPixels; x++)
                    {
                        // The text is white, so the alpha channel is the coverage.
                        glyphMasks[(i * cellPixels + y) * cellPixels + x] = (byte)(atlasPixels[(top + y) * atlasWidth + left + x] >> 24);
                    }
                }

                for (int y = 0; y < cellPixels; y++)
                {
                    for (int x = 0; x < cellPixels; x++)
                    {
                        byte widened = 0;

                        for (int dy = Math.Max(0, y - GlowDilation); dy <= Math.Min(cellPixels - 1, y + GlowDilation); dy++)
                        {
                            for (int dx = Math.Max(0, x - GlowDilation); dx <= Math.Min(cellPixels - 1, x + GlowDilation); dx++)
                            {
                                widened = Math.Max(widened, glyphMasks[(i * cellPixels + dy) * cellPixels + dx]);
                            }
                        }

                        glowMasks[(i * cellPixels + y) * cellPixels + x] = widened;
                    }
                }
            }
        }

        private unsafe void DrawCell(byte* backBuffer, byte* glowBuffer, int stride, int column, int row)
        {
            MatrixCharacter character = MatrixGrid[column, row];
            int colorOffset = (character.Palette * ColorPalette.Size + character.DisplayedBrush) * 256;
            int maskOffset = glyphIndices.TryGetValue(character.Character, out int glyph) ? glyph * cellPixels * cellPixels : -1;

            // The glow grows with the level from faint at GlowLevel to full at the head, a hard threshold shows as a visible step in the drop.
            int glowScale = glowBuffer != null && maskOffset >= 0 && character.DisplayedBrush >= settings.GlowLevel
                ? (character.DisplayedBrush - settings.GlowLevel + 1) * 256 / (ColorPalette.Size - settings.GlowLevel)
                : 0;

            // The near-white levels would glow white, the base color keeps the halo in the drop's hue.
            int glowOffset = (character.Palette * ColorPalette.Size + ColorPalette.BaseLevel) * 256;

            for (int y = 0; y < cellPixels; y++)
            {
                int lineOffset = (row * cellPixels + y) * stride + column * cellPixels * 4;
                int* line = (int*)(backBuffer + lineOffset);

                for (int x = 0; x < cellPixels; x++)
                {
                    line[x] = maskOffset < 0 ? 0 : colorTable[colorOffset + glyphMasks[maskOffset + y * cellPixels + x]];
                }

                if (glowBuffer != null)
                {
                    int* glowLine = (int*)(glowBuffer + lineOffset);

                    for (int x = 0; x < cellPixels; x++)
                    {
                        glowLine[x] = glowScale == 0 ? 0 : colorTable[glowOffset + glowMasks[maskOffset + y * cellPixels + x] * glowScale / 256];
                    }
                }
            }
        }

        /// <param name="redrawAll">Draw every visible character again, because the colors changed.</param>
        private unsafe void DrawChangedCells(List<Coordinate> changedValues, bool redrawAll)
        {
            if (changedValues.Count == 0 && !redrawAll)
            {
                return;
            }

            int firstColumn = columns, lastColumn = 0, firstRow = rows, lastRow = 0;

            bitmap.Lock();
            glowBitmap?.Lock();

            try
            {
                byte* backBuffer = (byte*)bitmap.BackBuffer;
                byte* glowBuffer = glowBitmap == null ? null : (byte*)glowBitmap.BackBuffer;

                if (redrawAll)
                {
                    // Black is black in every palette, and most cells are black.
                    for (int column = 0; column < columns; column++)
                    {
                        for (int row = 0; row < rows; row++)
                        {
                            if (MatrixGrid[column, row].DisplayedBrush != 0)
                            {
                                DrawCell(backBuffer, glowBuffer, bitmap.BackBufferStride, column, row);
                            }
                        }
                    }

                    firstColumn = firstRow = 0;
                    lastColumn = columns - 1;
                    lastRow = rows - 1;
                }

                foreach (var coordinate in changedValues)
                {
                    DrawCell(backBuffer, glowBuffer, bitmap.BackBufferStride, coordinate.Column, coordinate.Row);

                    firstColumn = Math.Min(firstColumn, coordinate.Column);
                    lastColumn = Math.Max(lastColumn, coordinate.Column);
                    firstRow = Math.Min(firstRow, coordinate.Row);
                    lastRow = Math.Max(lastRow, coordinate.Row);
                }

                // WPF merges many small dirty rects into their union anyway, one rect saves the calls.
                var dirtyRect = new Int32Rect(
                    firstColumn * cellPixels, firstRow * cellPixels,
                    (lastColumn - firstColumn + 1) * cellPixels, (lastRow - firstRow + 1) * cellPixels);
                bitmap.AddDirtyRect(dirtyRect);
                glowBitmap?.AddDirtyRect(dirtyRect);
            }
            finally
            {
                glowBitmap?.Unlock();
                bitmap.Unlock();
            }
        }

        private void InvokeUiAction(Action action)
        {
            try
            {
                // Below input and render priority, so a slow frame cannot block exiting or drawing.
                MainGrid.Dispatcher.Invoke(action, DispatcherPriority.Background);
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Takes the keyboard focus where Windows allows it, e.g. when Windows starts the screensaver itself.
            Activate();
        }

        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Loaded comes again whenever the grid re-enters a visual tree, the scene must be created only once.
            MainGrid.Loaded -= MainGrid_Loaded;
            CreateScene();
        }

        private void RunAnimation(List<Coordinate> changedValues)
        {
            DateTime timeStampFirst;
            DateTime timeStampSecond;
            TimeSpan timeSpan;
            Stopwatch colorCycleClock = Stopwatch.StartNew();
            int frame = 0;

            while (true)
            {
                // Timer
                timeStampFirst = DateTime.Now;

                for (int column = 0; column < columns; column++)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        if (CalculateNewCharacters(column, row, changedValues))
                        {
                            row++;
                        }
                    }
                }

                for (int column = 0; column < columns; column++)
                {
                    if (random.NextDouble() < newTopDropProbability)
                    {
                        StartDrop(column, 0, changedValues);
                    }

                    // One drop for all rows below, so each of them gets 1 / (rows - 1) of the chance.
                    if (rows > 1 && random.NextDouble() < newMidDropProbability)
                    {
                        StartDrop(column, random.Next(1, rows), changedValues);
                    }
                }

                bool colorsChanged = false;

                if (settings.ColorMode == ColorMode.ColorCycle && ++frame % ColorCycleFrames == 0)
                {
                    double hue = GreenHue + colorCycleClock.Elapsed.TotalSeconds * 360 / ColorCycleSeconds;
                    colorTable = CreateColorTable(ColorPalette.Create(ColorPalette.FromHue(hue)));
                    colorsChanged = true;
                }

                InvokeUiAction(() =>
                {
                    Console.WriteLine("ChangedValues: " + changedValues.Count);

                    DrawChangedCells(changedValues, colorsChanged);

                    changedValues.Clear();
                });

                // Timer
                timeStampSecond = DateTime.Now;
                timeSpan = timeStampSecond.Subtract(timeStampFirst);

                Console.WriteLine($"Run took {timeSpan} ms");

                Thread.Sleep(Math.Max(1, (int)frameTime.Subtract(timeSpan).TotalMilliseconds));
            }
        }

        private void StartDrop(int column, int row, List<Coordinate> changedValues)
        {
            var newCharacter = MatrixGrid[column, row];
            newCharacter.Brush = ColorPalette.Size - 1;
            newCharacter.Character = RandomCharacter();
            newCharacter.IsFlash = random.Next(100) < settings.FlashDropPercent;
            newCharacter.IsFlicker = row > 0 && !newCharacter.IsFlash && random.Next(100) < settings.FlickerDropPercent;
            newCharacter.Speed = newCharacter.IsFlash ? ScreenSaverSettings.FastestSpeed : RandomSpeed(row, newCharacter.IsFlicker);
            newCharacter.FlickerCountdown = newCharacter.IsFlicker ? FlickerInterval(newCharacter.Speed) : 0;
            newCharacter.Palette = settings.ColorMode == ColorMode.RainbowDrops ? random.Next(RainbowHues) : 0;

            // A flash drop fades one level per frame, its jumps already stretch the trail.
            newCharacter.WordLength = newCharacter.IsFlash ? ColorPalette.Size - 2 : RandomWordLength();

            QueueRedraw(column, row, changedValues);
        }

        private int RandomSpeed(int row, bool isFlicker)
        {
            int slowest = settings.MinSpeed;
            int fastest = settings.MaxSpeed;

            // Speed 1 never fades: from the top row it would pile up white characters, a stopped flicker drop would never go.
            if (row == 0 || isFlicker)
            {
                slowest = Math.Max(slowest, ScreenSaverSettings.SlowestSpeed + 1);
                fastest = Math.Max(fastest, slowest);
            }

            return random.Next(slowest, fastest + 1);
        }

        private int RandomWordLength()
        {
            // Log-uniform, so short words are as common as long ones.
            double logMin = Math.Log(settings.MinWordLength);
            double logMax = Math.Log(settings.MaxWordLength + 1);

            return Math.Clamp((int)Math.Exp(logMin + random.NextDouble() * (logMax - logMin)), settings.MinWordLength, settings.MaxWordLength);
        }

        private char RandomCharacter()
        {
            return characterPool[random.Next(characterPool.Length)];
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // WPF also raises MouseMove without a real movement, e.g. when the window appears under the cursor.
            // Screen pixels stay stable even if the window gets rescaled on a monitor with a different DPI.
            Point position = PointToScreen(e.GetPosition(this));

            if (initialMousePosition == null)
            {
                initialMousePosition = position;
            }
            else if ((position - initialMousePosition.Value).Length > MouseMoveTolerance)
            {
                Application.Current.Shutdown();
            }
        }
    }
}