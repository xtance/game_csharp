using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;

namespace TrainingPractice_02
{
    public partial class MainWindow : Window
    {
        public int _row = 4;                  // кол-во плиток в ряду
        public int _max = 16;                 // всего плиток
        public int _side = 150;               // пикселей на плитку
        public int _status = 0;               // 0 - игра не запущена, 1 - игра идёт, 2 - игра выиграна
        public int _timestamp = 0;            // время, когда началась игра (unixtime)
        public int _steps = 0;                // кол-во шагов
        public Grid _grid;                    // контейнер плиток
        public DispatcherTimer _timer;        // таймер
        public TextBlock _text = null;        // блок с текстoм
        public Random _random = new Random(); // рандом

        // Хранение рекордов
        public static string _path = @"records.txt";
        public static List<(int ts, int steps)> _records = new List<(int, int)> { };

        // Основное окно
        public MainWindow()
        {
            InitializeComponent();
            _text = this.Info;
            _grid = this.GridTiles;
            this.Play.Click += OnPlayClicked;
            this.Help.Click += OnHelpClicked;
            this.Records.Click += OnRecordsClicked;

            // Создаем файл для рекордов, если он отсутствует
            if (!File.Exists(_path)) File.Create(_path).Close();
            StreamReader file = new StreamReader(_path);

            // Считываем рекорды
            string line;
            while ((line = file.ReadLine()) != null)
            {
                string[] split = line.Split(' ');
                if (split.Length != 2) continue;
                int time = Convert.ToInt32(split[0]);
                int steps = Convert.ToInt32(split[1]);
                if (time <= 0 || steps <= 0) continue;
                _records.Add((time, steps));
            }
            file.Close();

            // Сортируем по скорости (ts). Также можно по количеству шагов (steps)
            _records.Sort((y, x) => y.ts.CompareTo(x.ts));
        }

        // Нажатие на кнопку - Об игре
        public void OnHelpClicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Цель игры 'Пятнашки' - собрать таблицу из идущих подряд чисел от 1 до 15. Пустая ячейка должна оказаться последней. Нажмите на плитку, граничащую с пустой ячейкой, чтобы передвинуть её на свободное место. \n\nИгра создана в рамках учебной практики по C#", "Об игре");
        }

        // Нажатие на кнопку - Рекорды

        public void OnRecordsClicked(object sender, RoutedEventArgs e)
        {
            string temp = "";
            _records.ForEach(one =>
            {
                int min = one.ts / 60;
                int sec = one.ts - min * 60;
                temp += $"{min}:{sec.ToString("00")} | {one.steps} шагов \n";
            });

            if (temp != "") MessageBox.Show(temp, $"Рекордов: {_records.Count}");
            else MessageBox.Show("Рекордов ещё нет.", "Ошибка");
        }

        // Нажатие на кнопку - Новая игра
        public void OnPlayClicked(object sender, RoutedEventArgs e)
        {
            if (_status == 1)
            {
                MessageBoxResult res = MessageBox.Show("Сейчас запущена игра. Вы уверены, что хотите начать сначала?", "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes) Game();
            }
            else Game();
        }


        // Запуск новой игры

        public void Game()
        {
            // Удаляем предыдущие кнопки - вдруг они там есть
            _grid.Children.Clear();

            // Создаём массив
            int[] arr = new int[_max];
            for (int i = 0; i < _max; i++) arr[i] = i;

            // Перемешиваем, пока не найдется решаемый вариант
            do Shuffle(arr, _max);
            while (!IsPossible(arr, _max));

            // Создаём плитки
            for (int i = 0; i < _max; i++) new Tile(arr[i], i, this);

            _steps = 0;     // шаги
            _status = 1;    // статус 1 - сейчас идёт игра
            _timestamp = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Таймер
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += OnTimerTick;
            }
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
            OnTimerTick(null, null);
        }

        // Таймер, показывает время и шаги
        public void OnTimerTick(object sender, EventArgs e)
        {
            int diff = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _timestamp;
            _text.Text = $"{GetReadableTime(diff)},  шагов: {_steps}";
        }

        // Возвращает читаемое время - из 105 сек в 1:45
        public string GetReadableTime(int ts)
        {
            int min = ts / 60;
            int sec = ts - min * 60;
            return $"{min}:{sec.ToString("00")}";
        }

        // Проверка на победу
        public void CheckWin()
        {
            int count = 0;
            for (int i = 0; i < _max; i++) if (((Tile)_grid.Children[i]).Original == ((Tile)_grid.Children[i]).Now) count++;
            if (count == _max)
            {
                _timer.Stop();
                _status = 2;
                int diff = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _timestamp;

                // Пишем рекорд в файл
                StreamWriter writer = new StreamWriter(_path, true);
                writer.WriteLine($"{diff} {_steps}");
                writer.Close();

                // Пишем рекорд в список рекордов
                _records.Add((diff, _steps));
                _records.Sort((y, x) => y.ts.CompareTo(x.ts));

                _text.Text = "Игра завершена.";
                MessageBoxResult res = MessageBox.Show($"Вы собрали пятнашки за {GetReadableTime(diff)}, {_steps} шагов. Сыграть ещё раз?", "Поздравляем!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (res == MessageBoxResult.Yes) Game();
            }
        }

        // Меняет две плитки местами
        public void Swap(Tile tile1, Tile tile2)
        {
            int cur = tile1.Now;
            tile1.Now = tile2.Now;
            tile2.Now = cur;
            tile1.SetMargin(true);
            tile2.SetMargin(false);
        }

        // Перемешивает массив
        public void Shuffle(int[] arr, int size)
        {
            for (int i = size - 1; i > 0; i--)
            {
                int j = _random.Next(0, i + 1);
                int temp = arr[j];
                arr[j] = arr[i];
                arr[i] = temp;
            }

            // Чтобы пустая плитка всегда была на последнем месте
            for (int i = 0; i < size; i++)
            {
                if (arr[i] == size - 1)
                {
                    int temp = arr[size - 1];
                    arr[size - 1] = arr[i];
                    arr[i] = temp;
                }
            }
        }

        // Проверяет возможность данной комбинации
        // https://technocrator.livejournal.com/45348.html
        public static bool IsPossible(int[] arr, int size)
        {
            int empty = -1; // позиция "пустой" клетки. всегда должна быть равна size-1. но мало ли что...
            int sum = 0;    // сумма
            for (int i = 0; i < size; ++i)
            {
                if (arr[i] == size - 1) empty = i;
                for (int j = 0; j < i; ++j) if (arr[j] > arr[i]) ++sum;
            }
            sum += 1 + (empty / 4);
            if (sum % 2 == 0) Debug.WriteLine($"\nВозможно! Пустая клеточка: {empty}");
            else Debug.WriteLine($"\nНевозможно! Пустая клеточка: {empty}");
            return (sum % 2 == 0);
        }
    }

    // Класс плитки является расширением обычной кнопки
    public class Tile : Button
    {
        private MainWindow window;
        // Конструктор
        public Tile(int original, int now, MainWindow window)
        {
            this.window = window;
            this.Original = original;
            this.Now = now;
            this.Height = this.Width = window._side;
            this.VerticalAlignment = VerticalAlignment.Top;
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.Click += OnClick;
            this.Content = (this.Original + 1).ToString();
            this.FontSize = 24;
            this.FontWeight = FontWeights.SemiBold;
            this.Background = new SolidColorBrush(Color.FromRgb(179, 255, 230));
            this.Cursor = Cursors.Hand;
            this.SetMargin(false);

            if (original == (window._max-1)) this.Opacity = 0;
            window._grid.Children.Add(this);
        }

        // Нажатие на плитку
        private void OnClick(object sender, RoutedEventArgs e)
        {
            // Если игра еще не началась или уже окончена, пропускаем действие
            if (window._status != 1) return;

            foreach (Tile t in window._grid.Children)
            {
                if (t.Original == (window._max - 1))
                {
                    if ((this.Now - t.Now == window._row)   // Вверх
                        || (t.Now - this.Now == window._row)   // Вниз
                        || (t.Now - this.Now == 1 && this.Now / window._row == t.Now / window._row)  // Вправо
                        || (this.Now - t.Now == 1 && this.Now / window._row == t.Now / window._row)  // Влево
                        )
                    {
                        window.Swap(this, t);
                        window._steps++;
                        window.CheckWin();
                    }
                    break;
                }
            }
        }

        // Устанавливает позицию плитки при помощи свойства margin
        public void SetMargin(bool animate)
        {
            if (animate)
            {
                ThicknessAnimation ta = new ThicknessAnimation();
                ta.From = this.Margin;
                ta.To = new Thickness((this.Now % window._row) * window._side, (this.Now / window._row) * window._side, 0, 0);
                ta.Duration = new Duration(TimeSpan.FromMilliseconds(250));
                this.BeginAnimation(Tile.MarginProperty, ta);
            }
            else
            {
                this.Margin = new Thickness((this.Now % window._row) * window._side, (this.Now / window._row) * window._side, 0, 0);
            }
        }

        // Текущая позиция плитки
        public int Now
        {
            get { return (int)GetValue(NowProperty); }
            set { SetValue(NowProperty, value); }
        }

        // Изначальная позиция плитки, если совпадает с now - плитка стоит правильно
        public int Original
        {
            get { return (int)GetValue(OriginalProperty); }
            set { SetValue(OriginalProperty, value); }
        }

        public static readonly DependencyProperty NowProperty = DependencyProperty.Register("Now", typeof(int), typeof(Tile), new UIPropertyMetadata(0));
        public static readonly DependencyProperty OriginalProperty = DependencyProperty.Register("Original", typeof(int), typeof(Tile), new UIPropertyMetadata(0));
    }
}
