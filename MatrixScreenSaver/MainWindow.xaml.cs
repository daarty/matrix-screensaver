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
    public class Coordinate
    {
        public int Column { get; set; }
        public int Row { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const int CharacterSize = 16;

        private static readonly SolidColorBrush[] Brushes = new SolidColorBrush[]
           {
                //new SolidColorBrush(Colors.Black),
                //new SolidColorBrush(Colors.DimGray),
                new SolidColorBrush(CalculateColor(Colors.Black, Colors.DarkGreen, 75)),
                new SolidColorBrush(CalculateColor(Colors.Black, Colors.DarkGreen, 50)),
                new SolidColorBrush(CalculateColor(Colors.Black, Colors.DarkGreen, 25)),
                new SolidColorBrush(Colors.DarkGreen),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.DarkGreen, 66)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.DarkGreen, 33)),
                new SolidColorBrush(Colors.Green),
                //new SolidColorBrush(Colors.LightGreen),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.White, 70)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.White, 50)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.White, 20)),
                new SolidColorBrush(CalculateColor(Colors.Green, Colors.White, 10)),
                //new SolidColorBrush(Colors.WhiteSmoke),
                new SolidColorBrush(Colors.White)
           };

        private static Action EmptyDelegate = delegate () { };
        private static object locker = new object();
        private int columns;

        private Random random = new Random();
        private int rows;
        private TimeSpan timeSpan;
        private TimeSpan timeSpanExpected = new TimeSpan(0, 0, 0, 0, 66);
        private DateTime timeStampFirst;
        private DateTime timeStampSecond;

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
                (byte)(((int)firstColor.A * percentOfFirstColor + (int)secondColor.A * (100 - percentOfFirstColor)) / 100),
                (byte)(((int)firstColor.R * percentOfFirstColor + (int)secondColor.R * (100 - percentOfFirstColor)) / 100),
                (byte)(((int)firstColor.G * percentOfFirstColor + (int)secondColor.G * (100 - percentOfFirstColor)) / 100),
                (byte)(((int)firstColor.B * percentOfFirstColor + (int)secondColor.B * (100 - percentOfFirstColor)) / 100));
        }

        private void CalculateNewCharacters(int column, int row, List<Coordinate> changedValues)
        {
            var thisCharacter = MatrixGrid[column, row];

            for (int brush = 0; brush < Brushes.Length; brush++)
            {
                if (random.Next(100) > 0 && brush > 0 && thisCharacter.Brush == brush)
                {
                    thisCharacter.Brush--;

                    if (thisCharacter.Brush > 0 &&
                        ((thisCharacter.Brush < Brushes.Length - 2 && random.Next(10) == 0) ||
                        random.Next(500) == 0))
                    {
                        thisCharacter.Brush--;
                    }

                    //thisCharacter.Foreground = Brushes[brush - 1];
                    //InvokeUiAction(() => thisCharacter.Foreground = Brushes[thisCharacter.Brush]);
                    //thisCharacter.HasChanged = true;
                    if (random.Next(3) > 0)
                    {
                        // DEBUG ChangedValues
                        //ChangedValues.Add(new Coordinate { Column = column, Row = row });
                        changedValues.Add(new Coordinate { Column = column, Row = row });
                    }
                }

                // if white -> create next letter row + 1 if not last row
                if (row > 0 && MatrixGrid[column, row - 1].Brush == Brushes.Length - 2)
                {
                    thisCharacter.Brush = Brushes.Length - 1;
                    //InvokeUiAction(() =>
                    //    {
                    //thisCharacter.Foreground = Brushes[thisCharacter.Brush];
                    //thisCharacter.Brush = brush - 1;
                    thisCharacter.Character = MatrixCharacter.PoolOfCharacters[random.Next(MatrixCharacter.PoolOfCharacters.Length - 1)];

                    // DEBUG Binding
                    //TextGrid[column, row].Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                    //((TextBox)sender).GetBindingExpression(ComboBox.TextProperty).UpdateSource();
                    //InvokeUiAction(() => TextGrid[column, row].GetBindingExpression(TextBlock.TextProperty).UpdateTarget());
                    //OnPropertyChanged($"MatrixGridColumn{column}Row{row}");
                    OnPropertyChanged(nameof(thisCharacter));

                    //});
                    //thisCharacter.HasChanged = true;
                    // DEBUG ChangedValues
                    changedValues.Add(new Coordinate { Column = column, Row = row });
                }

                //////thisCharacter.Dispatcher.Invoke(() =>
                //////{
                //////    if (thisCharacter.Foreground == Brushes[brush])
                //////    {
                //////        thisCharacter.Foreground = Brushes[brush - 1];
                //////    }

                //////    // if white -> create next letter row + 1 if not last row
                //////});
            }
        }

        private void CreateScene()
        {
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

            // DEBUG
            var timeStampCreationFirst = DateTime.Now;

            var random = new Random();

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    // MatrixCharacter
                    var thisCharacter = new MatrixCharacter();
                    MatrixGrid[i, j] = thisCharacter;

                    // disable init values
                    //thisCharacter.Brush = 0;
                    //var newCharacter = MatrixCharacter.PoolOfCharacters[random.Next(MatrixCharacter.PoolOfCharacters.Length - 1)];
                    //thisCharacter.Character = newCharacter;
                    thisCharacter.Name = $"MatrixGridColumn{i}Row{j}";

                    // TextBlock
                    var thisTextBlock = new TextBlock();
                    TextGrid[i, j] = thisTextBlock;

                    thisTextBlock.FontSize = CharacterSize * 0.75;
                    //thisTextBlock.Margin = new Thickness(0);
                    //thisTextBlock.Foreground = new SolidColorBrush(Colors.White);
                    thisTextBlock.Foreground = Brushes[thisCharacter.Brush]; //new SolidColorBrush(Colors.White); //MatrixCharacter.FontBrushOld;

                    Grid.SetColumn(thisTextBlock, i);
                    Grid.SetRow(thisTextBlock, j);

                    MainGrid.Children.Add(thisTextBlock);

                    //new Binding()
                    //thisTextBlock.Text.DataBindings

                    //make a new source
                    //MyData myDataObject = new MyData(DateTime.Now);
                    //Binding myBinding = new Binding("MyDataProperty");
                    //myBinding.Source = myDataObject;
                    //myText.SetBinding(TextBlock.TextProperty, myBinding);
                    //

                    // DEBUG Binding
                    thisTextBlock.Name = $"TextGridColumn{i}Row{j}";

                    Binding thisBinding = new Binding(); // new Binding($"MatrixGridColumn{i}Row{j}"); // new Binding($"MatrixGrid[{i},{j}]");
                    thisBinding.Source = MatrixGrid[i, j].Character;
                    thisTextBlock.SetBinding(TextBlock.TextProperty, thisBinding);
                }
            }

            // Timer
            var timeStampCreationSecond = DateTime.Now;
            var timeSpanCreation = (timeStampCreationSecond.Subtract(timeStampCreationFirst));

            // DEBUG
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
            catch (Exception ex)
            //catch (TaskCancelledException ex)
            {
                Console.WriteLine("Caught Exception: " + ex.Message);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // DEBUG
            CreateScene();
        }

        private void RunAnimation(List<Coordinate> changedValues)
        {
            while (true)
            {
                // DEBUG
                timeStampFirst = DateTime.Now;

                for (int column = 0; column < columns; column++)
                {
                    //    Parallel.For(0, columns - 1, column =>
                    //{
                    //MainGrid.Dispatcher.Invoke(() =>
                    //{
                    //lock (locker)
                    //{
                    for (int row = 0; row < rows; row++)
                    {
                        CalculateNewCharacters(column, row, changedValues);

                        //thisCharacter.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                    }
                    //});
                    //}
                    //});
                }

                // create new running word
                var newWordColumn = random.Next(columns - 1);
                var newCharacter = MatrixGrid[newWordColumn, 0];
                newCharacter.Brush = Brushes.Length - 1;
                newCharacter.Character = MatrixCharacter.PoolOfCharacters[random.Next(MatrixCharacter.PoolOfCharacters.Length - 1)];
                //newCharacter.HasChanged = true;
                // DEBUG ChangedValues
                changedValues.Add(new Coordinate { Column = newWordColumn, Row = 0 });

                // Then set the TexBlocks in the UI thread
                //var changedValuesCopy = new Coordinate[changedValues.Count];
                //changedValues.CopyTo(changedValuesCopy);
                //changedValues.Clear();

                InvokeUiAction(() =>
                {
                    Console.WriteLine("ChangedValues: " + changedValues.Count);

                    //foreach (var coordinate in changedValuesCopy)
                    foreach (var coordinate in changedValues)
                    {
                        TextGrid[coordinate.Column, coordinate.Row].Text = MatrixGrid[coordinate.Column, coordinate.Row].Character.ToString();
                        TextGrid[coordinate.Column, coordinate.Row].Foreground = Brushes[MatrixGrid[coordinate.Column, coordinate.Row].Brush];
                        //MatrixGrid[coordinate.Column, coordinate.Row].HasChanged = false;
                        //ChangedValues.Add(new Coordinate { Column = column, Row = row });
                    }

                    //ChangedValues.Clear();
                    changedValues.Clear();

                    //for (int column = 0; column < columns; column++)
                    //{
                    //    for (int row = 0; row < rows; row++)
                    //    {
                    //        if (MatrixGrid[column, row].HasChanged)
                    //        {
                    //            textGrid[column, row].Text = MatrixGrid[column, row].Character.ToString();
                    //            textGrid[column, row].Foreground = Brushes[MatrixGrid[column, row].Brush];
                    //            MatrixGrid[column, row].HasChanged = false;
                    //        }
                    //    }
                    //}

                    //newCharacter.Foreground = Brushes[newCharacter.Brush];
                });

                // Timer
                timeStampSecond = DateTime.Now;
                timeSpan = (timeStampSecond.Subtract(timeStampFirst));

                // DEBUG
                Console.WriteLine($"Run took {timeSpan} ms");

                // DEBUG ChangedValues
                //MainGrid.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

                //InvokeUiAction(() =>
                //{
                //    this.DataContext = null;
                //    MainGrid.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                //    this.DataContext = this;
                //    MainGrid.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                //});
                //MainGrid.CommandBindings.up
                //OnPropertyChanged(nameof(MainGrid));

                //Thread.Sleep(10);
                Thread.Sleep(Math.Max(0, (int)timeSpanExpected.Subtract(timeSpan).TotalMilliseconds));
            }

            //var random = new Random();
            //int thisExecutionColumn = random.Next(width - 1);

            //while (true)
            //{
            //    bool createdNewThisRun = false;

            //    for (int i = 0; i < height; i++)
            //    {
            //        var thisCharacter = MatrixGrid[thisExecutionColumn, i];

            //        if (thisCharacter.Character != MatrixCharacter.EmptyChar)
            //        {
            //            thisCharacter.Brush = MatrixCharacter.CalculateBrush(thisCharacter);

            //            if (thisCharacter.Brush == MatrixCharacter.FontBrushOld)
            //            {
            //                thisCharacter.Character = MatrixCharacter.EmptyChar;
            //            }
            //        }
            //        else if (!createdNewThisRun)
            //        {
            //            if (random.Next(height) < 3)
            //            {
            //                thisCharacter.Character = MatrixCharacter.PoolOfCharacters[random.Next(MatrixCharacter.PoolOfCharacters.Length - 1)];
            //            }
            //        }
            //    }

            //    Thread.Sleep(100);
            //}
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