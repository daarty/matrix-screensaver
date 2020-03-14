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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace MatrixScreenSaver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int CharacterSize = 16;

        private const int MaxSpeed = 20;

        private static readonly SolidColorBrush[] Brushes = new SolidColorBrush[]
           {
                new SolidColorBrush(Colors.Black),
                new SolidColorBrush(CalculateColor(Colors.Black, Colors.DarkGreen, 80)),
                new SolidColorBrush(CalculateColor(Colors.Black, Colors.DarkGreen, 60)),
                new SolidColorBrush(CalculateColor(Colors.Black, Colors.DarkGreen, 40)),
                new SolidColorBrush(CalculateColor(Colors.Black, Colors.DarkGreen, 20)),
                new SolidColorBrush(Colors.DarkGreen),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.DarkGreen, 75)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.DarkGreen, 50)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.DarkGreen, 25)),
                new SolidColorBrush(Colors.Green),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.White, 70)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.White, 50)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.White, 20)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.White, 10)),
                new SolidColorBrush(Colors.White)
           };

        private int columns;

        private Random random = new Random();
        private int rows;
        private TimeSpan timeSpanExpected = new TimeSpan(0, 0, 0, 0, 66);

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Brush DebugGridBackgroundBrush { get; private set; } = new SolidColorBrush(Colors.Yellow);
        public MatrixCharacter[,] MatrixGrid { get; private set; }
        public TextBlock[,] TextGrid { get; private set; }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static Color CalculateColor(Color firstColor, Color secondColor, int percentOfFirstColor)
        {
            return Color.FromArgb(
                (byte)((firstColor.A * percentOfFirstColor + secondColor.A * (100 - percentOfFirstColor)) / 100),
                (byte)((firstColor.R * percentOfFirstColor + secondColor.R * (100 - percentOfFirstColor)) / 100),
                (byte)((firstColor.G * percentOfFirstColor + secondColor.G * (100 - percentOfFirstColor)) / 100),
                (byte)((firstColor.B * percentOfFirstColor + secondColor.B * (100 - percentOfFirstColor)) / 100));
        }

        private void CalculateNewCharacters(int column, int row, List<Coordinate> changedValues)
        {
            var thisCharacter = MatrixGrid[column, row];

            // No update if the minimal brush is already applied.
            // Sometimes just don't update the current one, depending on the speed. So it gets "stuck" more often.
            if (thisCharacter.Brush != 0 && random.Next(thisCharacter.Speed) != 0)
            {
                thisCharacter.Brush--;

                // If the character is still not on minimum colour, sometimes turn down one more.
                if (thisCharacter.Brush != 0 && thisCharacter.Brush < Brushes.Length - 2 && random.Next(10) == 0)
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
            else if (row > 0 && MatrixGrid[column, row - 1].Brush == Brushes.Length - 2)
            {
                // If not in last row and at high speed, sometimes jump two blocks
                if (row < rows - 1 && thisCharacter.Speed > MaxSpeed / 2 && random.Next(MaxSpeed - thisCharacter.Speed) == 0)
                {
                    var nextCharacter = MatrixGrid[column, row + 1];
                    nextCharacter.Brush = Brushes.Length - 1;
                    nextCharacter.Character = MatrixCharacter.PoolOfCharacters[random.Next(MatrixCharacter.PoolOfCharacters.Length - 1)];
                    nextCharacter.Speed = MatrixGrid[column, row - 1].Speed;

                    changedValues.Add(new Coordinate { Column = column, Row = row + 1 });

                    thisCharacter.Brush = Brushes.Length - 2;
                }
                else
                {
                    thisCharacter.Brush = Brushes.Length - 1;
                }

                thisCharacter.Character = MatrixCharacter.PoolOfCharacters[random.Next(MatrixCharacter.PoolOfCharacters.Length - 1)];
                thisCharacter.Speed = MatrixGrid[column, row - 1].Speed;

                changedValues.Add(new Coordinate { Column = column, Row = row });
            }
        }

        private void CreateScene()
        {
            // Timer
            var timeStampCreationFirst = DateTime.Now;

            columns = (int)Math.Ceiling(MainGrid.RenderSize.Width / CharacterSize);
            rows = (int)Math.Ceiling(MainGrid.RenderSize.Height / CharacterSize);

            for (int i = 0; i < columns; i++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CharacterSize) });
            }

            for (int i = 0; i < rows; i++)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CharacterSize) });
            }

            MatrixGrid = new MatrixCharacter[columns, rows];
            TextGrid = new TextBlock[columns, rows];

            var random = new Random();

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    // MatrixCharacter
                    var thisCharacter = new MatrixCharacter();
                    MatrixGrid[i, j] = thisCharacter;

                    thisCharacter.Name = $"MatrixGridColumn{i}Row{j}";

                    // TextBlock
                    var thisTextBlock = new TextBlock();
                    TextGrid[i, j] = thisTextBlock;

                    thisTextBlock.FontSize = CharacterSize * 0.75;
                    thisTextBlock.Foreground = new SolidColorBrush(Colors.Black);

                    Grid.SetColumn(thisTextBlock, i);
                    Grid.SetRow(thisTextBlock, j);

                    MainGrid.Children.Add(thisTextBlock);

                    var thisBinding = new Binding();
                    thisBinding.Source = MatrixGrid[i, j].Character;
                    thisTextBlock.SetBinding(TextBlock.TextProperty, thisBinding);
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

        private void InvokeUiAction(Action action)
        {
            try
            {
                MainGrid.Dispatcher.Invoke(action);
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CreateScene();
        }

        private void RunAnimation(List<Coordinate> changedValues)
        {
            DateTime timeStampFirst;
            DateTime timeStampSecond;
            TimeSpan timeSpan;

            while (true)
            {
                // Timer
                timeStampFirst = DateTime.Now;

                for (int column = 0; column < columns; column++)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        CalculateNewCharacters(column, row, changedValues);
                    }
                }

                //Parallel.For(0, columns - 1, column =>
                //{
                //    for (int row = 0; row < rows; row++)
                //    {
                //        CalculateNewCharacters(column, row, changedValues);
                //    }
                //});

                // Make new words not appear every time.
                if (random.Next(3) == 0)
                {
                    // create new running word
                    var newWordColumn = random.Next(columns - 1);
                    var newCharacter = MatrixGrid[newWordColumn, 0];
                    newCharacter.Brush = Brushes.Length - 1;
                    newCharacter.Character = MatrixCharacter.PoolOfCharacters[random.Next(MatrixCharacter.PoolOfCharacters.Length - 1)];
                    newCharacter.Speed = random.Next(MaxSpeed) + 1;

                    changedValues.Add(new Coordinate { Column = newWordColumn, Row = 0 });
                }

                InvokeUiAction(() =>
                {
                    Console.WriteLine("ChangedValues: " + changedValues.Count);

                    foreach (var coordinate in changedValues)
                    {
                        TextGrid[coordinate.Column, coordinate.Row].Text = MatrixGrid[coordinate.Column, coordinate.Row].Character.ToString();
                        TextGrid[coordinate.Column, coordinate.Row].Foreground = Brushes[MatrixGrid[coordinate.Column, coordinate.Row].Brush];
                    }

                    changedValues.Clear();
                });

                // Timer
                timeStampSecond = DateTime.Now;
                timeSpan = timeStampSecond.Subtract(timeStampFirst);

                Console.WriteLine($"Run took {timeSpan} ms");

                Thread.Sleep(Math.Max(1, (int)timeSpanExpected.Subtract(timeSpan).TotalMilliseconds));
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}