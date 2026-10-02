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
        private const int MaxSpeed = 20;

        private const double FlashDropProbability = 0.02;

        private const int RainbowHues = 24;

        private const double ColorCycleSeconds = 60;

        // Recomputing the palette means redrawing every visible character, so it happens every few frames only.
        private const int ColorCycleFrames = 8;

        // Hue of Colors.Green, where the color cycle starts.
        private const double GreenHue = 120;

        // In screen pixels.
        private const double MouseMoveTolerance = 10;

        private readonly ColorMode colorMode;

        // Pixel value for each palette, level and glyph coverage (0-255), blended over the black background.
        private int[] colorTable;

        private readonly int characterSize;
        private readonly char[] characterPool;

        // Chance per column and frame to start a new drop.
        private readonly double newDropProbability;

        private Point? initialMousePosition;

        // Size of a character cell in physical pixels.
        private int cellPixels;

        private WriteableBitmap bitmap;

        // Coverage of every glyph of the pool, cellPixels * cellPixels bytes each.
        private byte[] glyphMasks;
        private Dictionary<char, int> glyphIndices;

        private int columns;

        private Random random = new Random();
        private int rows;
        private TimeSpan timeSpanExpected = new TimeSpan(0, 0, 0, 0, 66);

        public MainWindow(ScreenSaverSettings settings)
        {
            characterSize = settings.CharacterSize;
            characterPool = MatrixCharacter.CreatePool(settings.CharacterSets);
            colorMode = settings.ColorMode;
            colorTable = colorMode switch
            {
                ColorMode.ColorCycle => CreateColorTable(ColorPalette.Create(ColorPalette.FromHue(GreenHue))),
                ColorMode.RainbowDrops => CreateColorTable(Enumerable.Range(0, RainbowHues)
                    .Select(i => ColorPalette.Create(ColorPalette.FromHue(GreenHue + i * 360.0 / RainbowHues)))
                    .ToArray()),
                _ => CreateColorTable(ColorPalette.Create(settings.BaseColorValue)),
            };
            newDropProbability = settings.Density * timeSpanExpected.TotalMilliseconds / TimeSpan.FromMinutes(1).TotalMilliseconds;

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
                            (color.R * coverage / 255) << 16 | (color.G * coverage / 255) << 8 | (color.B * coverage / 255);
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

            // No update if the minimal brush is already applied.
            // Sometimes just don't update the current one, depending on the speed. So it gets "stuck" more often.
            if (thisCharacter.Brush != 0 && random.Next(thisCharacter.Speed) != 0)
            {
                thisCharacter.Brush--;

                // If the character is still not on minimum colour, sometimes turn down one more.
                if (thisCharacter.Brush != 0 && thisCharacter.Brush < ColorPalette.Size - 2 && random.Next(10) == 0)
                {
                    thisCharacter.Brush--;
                }

                // Sometimes simply don't update the color so it gets stuck on the screen.
                if (random.Next(3) > 0)
                {
                    changedValues.Add(new Coordinate { Column = column, Row = row });
                }
            }

            // If not first row and
            // if the letter above is one less than white -> create next letter in the current row
            else if (row > 0 && MatrixGrid[column, row - 1].Brush == ColorPalette.Size - 2)
            {
                var dropAbove = MatrixGrid[column, row - 1];

                // If not in last row and at high speed, sometimes jump two blocks
                if (row < rows - 1 && dropAbove.Speed > MaxSpeed / 2 && random.Next(MaxSpeed - dropAbove.Speed) == 0)
                {
                    var nextCharacter = MatrixGrid[column, row + 1];
                    nextCharacter.Brush = ColorPalette.Size - 1;
                    nextCharacter.Character = RandomCharacter();
                    nextCharacter.Speed = dropAbove.Speed;
                    nextCharacter.IsFlash = dropAbove.IsFlash;
                    nextCharacter.Palette = dropAbove.Palette;

                    changedValues.Add(new Coordinate { Column = column, Row = row + 1 });

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
                thisCharacter.IsFlash = dropAbove.IsFlash;
                thisCharacter.Palette = dropAbove.Palette;

                changedValues.Add(new Coordinate { Column = column, Row = row });
            }

            return createdNextCharacter;
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

            bitmap = new WriteableBitmap(columns * cellPixels, rows * cellPixels, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Bgr32, null);
            MatrixImage.Source = bitmap;

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
            }
        }

        private unsafe void DrawCell(byte* backBuffer, int stride, int column, int row)
        {
            MatrixCharacter character = MatrixGrid[column, row];
            int colorOffset = (character.Palette * ColorPalette.Size + character.Brush) * 256;
            int maskOffset = glyphIndices.TryGetValue(character.Character, out int glyph) ? glyph * cellPixels * cellPixels : -1;

            for (int y = 0; y < cellPixels; y++)
            {
                int* line = (int*)(backBuffer + (row * cellPixels + y) * stride) + column * cellPixels;

                for (int x = 0; x < cellPixels; x++)
                {
                    line[x] = maskOffset < 0 ? 0 : colorTable[colorOffset + glyphMasks[maskOffset + y * cellPixels + x]];
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

            try
            {
                byte* backBuffer = (byte*)bitmap.BackBuffer;

                if (redrawAll)
                {
                    // Also the black cells: some faded out without being drawn again and still show an old color.
                    for (int column = 0; column < columns; column++)
                    {
                        for (int row = 0; row < rows; row++)
                        {
                            DrawCell(backBuffer, bitmap.BackBufferStride, column, row);
                        }
                    }

                    firstColumn = firstRow = 0;
                    lastColumn = columns - 1;
                    lastRow = rows - 1;
                }

                foreach (var coordinate in changedValues)
                {
                    DrawCell(backBuffer, bitmap.BackBufferStride, coordinate.Column, coordinate.Row);

                    firstColumn = Math.Min(firstColumn, coordinate.Column);
                    lastColumn = Math.Max(lastColumn, coordinate.Column);
                    firstRow = Math.Min(firstRow, coordinate.Row);
                    lastRow = Math.Max(lastRow, coordinate.Row);
                }

                // WPF merges many small dirty rects into their union anyway, one rect saves the calls.
                bitmap.AddDirtyRect(new Int32Rect(
                    firstColumn * cellPixels, firstRow * cellPixels,
                    (lastColumn - firstColumn + 1) * cellPixels, (lastRow - firstRow + 1) * cellPixels));
            }
            finally
            {
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
                    // Half of the drops start at the top. Each row below starts one with 1 / (rows - 1) of that
                    // chance, which gives the other half.
                    if (random.NextDouble() < newDropProbability / 2)
                    {
                        StartDrop(column, 0, changedValues);
                    }

                    if (rows > 1 && random.NextDouble() < newDropProbability / 2)
                    {
                        StartDrop(column, random.Next(1, rows), changedValues);
                    }
                }

                bool colorsChanged = false;

                if (colorMode == ColorMode.ColorCycle && ++frame % ColorCycleFrames == 0)
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

                Thread.Sleep(Math.Max(1, (int)timeSpanExpected.Subtract(timeSpan).TotalMilliseconds));
            }
        }

        private void StartDrop(int column, int row, List<Coordinate> changedValues)
        {
            var newCharacter = MatrixGrid[column, row];
            newCharacter.Brush = ColorPalette.Size - 1;
            newCharacter.Character = RandomCharacter();
            newCharacter.IsFlash = random.NextDouble() < FlashDropProbability;
            newCharacter.Speed = newCharacter.IsFlash ? MaxSpeed : random.Next(MaxSpeed) + 1;
            newCharacter.Palette = colorMode == ColorMode.RainbowDrops ? random.Next(RainbowHues) : 0;

            changedValues.Add(new Coordinate { Column = column, Row = row });
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