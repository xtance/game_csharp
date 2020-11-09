using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TrainingPractice_02
{
    public partial class MainWindow : Window
    {
        public static int _row = 4;     // кол-во плиток в ряду
        public static int _max = 15;    // всего плиток
        public static int _side = 150;  // пикселей на плитку
        public static Grid _grid;       // контейнер плиток

        public MainWindow()
        {
            InitializeComponent();
            this.Text1.Text = "тут будет таймер и какая-то инфа";
            TextBlock text1 = (TextBlock)this.FindName("Text1");
            _grid = this.GridTiles;

            // Создаём 16 кнопок
            for (int i = 0; i < _row * _row; i++) new Tile(i, i);

            Swap((Tile) this.GridTiles.Children[6], (Tile) this.GridTiles.Children[15]);
        }

        public static void Swap(Tile t1, Tile t2)
        {
            int cur = t1.Now;
            t1.Now = t2.Now;
            t2.Now = cur;
            t1.SetMargin(true);
            t2.SetMargin(true);
        }
    }

    public class Tile : Button
    {

        public Tile(int original, int now)
        {
            this.Original = original;
            this.Now = now;
            this.Height = this.Width = MainWindow._side;
            this.VerticalAlignment = VerticalAlignment.Top;
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.Click += OnClick;
            this.Content = (this.Original + 1).ToString();
            this.SetMargin(false);
            if (original == 15) this.Opacity = 0;
            MainWindow._grid.Children.Add(this);
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"Сейчас: {this.Now}, адрес: {this.Original}");

            foreach (Tile t in MainWindow._grid.Children)
            {
                if (t.Original == MainWindow._max)
                {
                    if  (   (this.Now - t.Now == MainWindow._row)
                        ||  (t.Now - this.Now == MainWindow._row)
                        ||  (t.Now - this.Now == 1 && this.Now / MainWindow._side == t.Now / MainWindow._side)
                        ||  (this.Now - t.Now == 1 && this.Now / MainWindow._side == t.Now / MainWindow._side)
                        )
                    {
                        MainWindow.Swap(this, t);
                    }
                    else
                    {
                        Debug.WriteLine("Увы, никак!");
                    }
                    break;
                }
            }
        }

        public void SetMargin(bool animate)
        {
            if (animate)
            {
                ThicknessAnimation ta = new ThicknessAnimation();
                ta.From = this.Margin;
                ta.To = new Thickness((this.Now % MainWindow._row) * MainWindow._side, (this.Now / MainWindow._row) * MainWindow._side, 0, 0);
                ta.Duration = new Duration(TimeSpan.FromMilliseconds(500));
                this.BeginAnimation(Tile.MarginProperty, ta);
            }
            else
            {
                this.Margin = new Thickness((this.Now % MainWindow._row) * MainWindow._side, (this.Now / MainWindow._row) * MainWindow._side, 0, 0);
            }
        }

        public int Now
        {
            get { return (int) GetValue(NowProperty); }
            set { SetValue(NowProperty, value); }
        }
        public int Original
        {
            get { return (int) GetValue(OriginalProperty); }
            set { SetValue(OriginalProperty, value); }
        }

        public static readonly DependencyProperty NowProperty = DependencyProperty.Register("Now", typeof(int), typeof(Tile), new UIPropertyMetadata(0));
        public static readonly DependencyProperty OriginalProperty = DependencyProperty.Register("Original", typeof(int), typeof(Tile), new UIPropertyMetadata(0));
    }
}
