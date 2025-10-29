using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Interop;

namespace CoverflowAltTab
{
    public partial class SwitcherWindow : Window
    {
        private readonly Scene3D _scene3D;  // Объект для создания и управления 3D-сценой
        private readonly SceneAnimator _sceneAnimator;  // Объект для анимации 3D-элементов сцены

        // Вложенные типы и данные для элементов сцены
        private readonly List<CoverflowItem> _items = new();  // Список всех окон для отображения
        private readonly Model3DGroup _modelsGroup = new();  // Группа 3D-моделей, которая будет добавлена в сцену
        private int _selectedIndex = 0;  // Индекс выбранного окна

        private const double CENTER_MAX_WIDTH = 800;  // Максимальная ширина центрального окна
        private const double CENTER_MAX_HEIGHT = 600; // Максимальная высота центрального окна

        private IntPtr _hostHwnd;  // Хэндл главного окна
        private IntPtr _currentDwmThumb = IntPtr.Zero; // Хэндл превью для DWM
        private int _lastSelectedIndex = -1;  // Индекс последнего выбранного окна

        private const int ANIMATION_DURATION_MS = 200; // Длительность анимации в миллисекундах

        // Конструктор окна
        public SwitcherWindow()
        {
            InitializeComponent();  // Инициализация компонента (UI)

            _scene3D = new Scene3D(_modelsGroup);  // Инициализация 3D-сцены
            _sceneAnimator = new SceneAnimator();  // Инициализация аниматора сцены

            // Добавляем 3D-модельную группу в Viewport для отображения на экране
            var visual = new ModelVisual3D { Content = _modelsGroup };
            MainViewport.Children.Add(visual);  // Добавляем 3D-визуализацию в основной Viewport

            // Подписка на изменения настроек сцены (обновление при изменениях)
            SceneSettings.Instance.SettingsChanged += (s, e) =>
            {
                if (IsLoaded) UpdateTransforms();  // Обновляем трансформации при изменении настроек
            };
        }

        // Инициализация окна при запуске
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _hostHwnd = new WindowInteropHelper(this).Handle;  // Получаем хэндл окна
        }

        // Обработчик закрытия окна
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Отписываемся от события изменения настроек сцены
            SceneSettings.Instance.SettingsChanged -= (s, e) =>
            {
                if (IsLoaded) UpdateTransforms();
            };

            // Если DWM-превью активно, удаляем его
            if (_currentDwmThumb != IntPtr.Zero)
            {
                Native.DwmUnregisterThumbnail(_currentDwmThumb);
                _currentDwmThumb = IntPtr.Zero;
            }
        }

        // Обработчик загрузки окна
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Получаем список открытых окон с помощью WindowEnumerator
            var windows = WindowEnumerator.GetOpenWindows();
            foreach (var w in windows)
            {
                _items.Add(new CoverflowItem
                {
                    Hwnd = w.Handle,  // Дескриптор окна
                    Title = w.Title,  // Заголовок окна
                    Preview = w.Preview,  // Превью окна
                    Icon = w.Icon  // Иконка окна
                });
            }

            // Строим модели для окон
            BuildModels();
            // Обновляем трансформации (анимируем)
            UpdateTransforms();
            // Обновляем информацию поверх центрального окна
            UpdateOverlay();
            // Обновляем превью DWM
            UpdateDwmCenterPreview();
        }

        // Метод для построения 3D-моделей окон
        private void BuildModels()
        {
            _scene3D.BuildModels(_items, VisualTreeHelper.GetDpi(this).PixelsPerDip);  // Строим 3D-модели с учётом DPI
        }

        // Обработчик нажатия кнопки закрытия
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                // Получаем дескриптор окна, которое выбрано
                IntPtr hwnd = _items[_selectedIndex].Hwnd;

                // Закрываем выбранное окно
                WindowActivator.CloseWindow(hwnd);

                // Удаляем окно из списка
                _items.RemoveAt(_selectedIndex);

                // Если закрыто окно, выбираем следующее
                if (_items.Count > 0)
                {
                    _selectedIndex = Math.Min(_selectedIndex, _items.Count - 1);  // Если индекс выходит за пределы, выбираем последний элемент
                }
                else
                {
                    _selectedIndex = -1;  // Если все окна закрыты, сбрасываем индекс
                }

                // Перестроить сцену с актуальными окнами
                UpdateTransforms();
                UpdateOverlay();

                // Если все окна закрыты, скрываем центральное окно
                if (_items.Count == 0)
                {
                    CenterPlaceholder.Visibility = Visibility.Collapsed;
                    OverlayTitle.Text = "";
                    OverlayIcon.Source = null;
                }
            }
        }

        // Метод для создания плейсхолдера с иконкой и текстом
        private ImageSource CreatePlaceholderWithIcon(ImageSource? icon, string title, double w, double h)
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.DimGray, null, new Rect(0, 0, w, h));  // Рисуем прямоугольник
                if (icon != null) dc.DrawImage(icon, new Rect(10, 10, 64, 64));  // Если есть иконка, рисуем её
                if (!string.IsNullOrEmpty(title))  // Если заголовок не пустой
                {
                    var ft = new FormattedText(title,
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 20, Brushes.White,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);  // Форматируем текст
                    dc.DrawText(ft, new Point(12, h - 40));  // Рисуем текст
                }
            }
            var bmp = new RenderTargetBitmap((int)w, (int)h, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);  // Рендерим изображение
            bmp.Freeze();  // Замораживаем битмап для дальнейшего использования
            return bmp;  // Возвращаем изображение
        }

        // Метод для отображения центрального плейсхолдера
        private void ShowCenterPlaceholder(ImageSource src)
        {
            CenterPlaceholder.Source = src;  // Устанавливаем изображение
            CenterPlaceholder.Width = CENTER_MAX_WIDTH;  // Устанавливаем размеры
            CenterPlaceholder.Height = CENTER_MAX_HEIGHT;
            CenterPlaceholder.Visibility = Visibility.Visible;  // Делаем видимым

            var animDuration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS);  // Длительность анимации
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };  // Плавное изменение

            var scale = new ScaleTransform(0.7, 0.7);  // Начальный масштаб
            CenterPlaceholder.RenderTransformOrigin = new Point(0.5, 0.5);  // Точка масштабирования
            CenterPlaceholder.RenderTransform = scale;  // Применяем трансформацию

            // Анимация масштабирования
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, animDuration) { EasingFunction = easing });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, animDuration) { EasingFunction = easing });
        }

        // Метод для анимации трансформации выбранного элемента
        private void AnimateTransform(CoverflowItem item, int offset, bool isSelected)
        {
            _sceneAnimator.AnimateTransform(item, offset, isSelected);  // Анимация с использованием SceneAnimator
        }

        // Метод для обновления трансформаций всех элементов
        private void UpdateTransforms()
        {
            // Пересоздаём меши (чтобы учесть новое количество/порядок) и применяем анимации
            BuildModels();
            for (int i = 0; i < _items.Count; i++)
                AnimateTransform(_items[i], i - _selectedIndex, i == _selectedIndex);  // Анимируем каждый элемент

            // Скрываем DWM-превью пока перестраиваем
            SetDwmOpacity(0);

            // Отображаем превью центрального окна
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                var src = _items[_selectedIndex].Preview ?? CreatePlaceholderWithIcon(_items[_selectedIndex].Icon, _items[_selectedIndex].Title, CENTER_MAX_WIDTH, CENTER_MAX_HEIGHT);
                ShowCenterPlaceholder(src);  // Отображаем центральный плейсхолдер
            }

            // Плавно отображаем DWM-превью
            CenterPlaceholder.Dispatcher.InvokeAsync(() =>
            {
                CenterPlaceholder.Visibility = Visibility.Collapsed;
                UpdateDwmCenterPreview();  // Обновляем DWM-превью

                int steps = 15;
                int step = 0;
                var fadeTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS / (double)steps)
                };
                fadeTimer.Tick += (fs, fe) =>
                {
                    step++;
                    byte value = (byte)(255 * step / steps);  // Плавное изменение прозрачности
                    SetDwmOpacity(value);
                    if (step >= steps) fadeTimer.Stop();
                };
                fadeTimer.Start();
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        // Метод для обновления наложения информации поверх центрального окна
        private void UpdateOverlay()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                OverlayTitle.Text = _items[_selectedIndex].Title ?? "";  // Устанавливаем заголовок
                OverlayIcon.Source = _items[_selectedIndex].Icon;  // Устанавливаем иконку
            }
            else
            {
                OverlayTitle.Text = "";  // Если нет выбранного окна
                OverlayIcon.Source = null;
            }
        }

        // Метод для обновления превью DWM центра
        private void UpdateDwmCenterPreview()
        {
            if (_items.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _items.Count) return;

            var center = _items[_selectedIndex];  // Получаем центральный элемент
            var srcHwnd = center.Hwnd;  // Получаем дескриптор окна центрального элемента

            double w = CENTER_MAX_WIDTH, h = CENTER_MAX_HEIGHT;
            double x = (ActualWidth - w) / 2, y = (ActualHeight - h) / 2;  // Позиция для DWM-превью

            if (_lastSelectedIndex != _selectedIndex || _currentDwmThumb == IntPtr.Zero)
            {
                _lastSelectedIndex = _selectedIndex;
                if (_currentDwmThumb != IntPtr.Zero)
                {
                    Native.DwmUnregisterThumbnail(_currentDwmThumb);  // Убираем старое DWM-превью
                    _currentDwmThumb = IntPtr.Zero;
                }

                Native.DwmRegisterThumbnail(_hostHwnd, srcHwnd, out _currentDwmThumb);  // Регистрируем новое DWM-превью
            }

            if (_currentDwmThumb == IntPtr.Zero) return;
            if (Native.DwmQueryThumbnailSourceSize(_currentDwmThumb, out var size) != 0 || size.x <= 0 || size.y <= 0) return;

            double scale = Math.Min(w / size.x, h / size.y);
            double dw = size.x * scale, dh = size.y * scale;
            double dx = x + (w - dw) / 2.0, dy = y + (h - dh) / 2.0;

            // Правильный rcDestination — исправлено (dx+dw, dy+dh)
            var props = new Native.DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = Native.DWM_TNP_VISIBLE | Native.DWM_TNP_RECTDESTINATION | Native.DWM_TNP_OPACITY,
                fVisible = true,
                opacity = 0,
                fSourceClientAreaOnly = false,
                rcDestination = new Native.RECT
                {
                    Left = (int)Math.Round(dx),
                    Top = (int)Math.Round(dy),
                    Right = (int)Math.Round(dx + dw),
                    Bottom = (int)Math.Round(dy + dh)
                }
            };
            Native.DwmUpdateThumbnailProperties(_currentDwmThumb, ref props);  // Обновляем DWM-превью

            // Расставляем glow overlay
            GlowOverlay.Width = dw + 80;
            GlowOverlay.Height = dh + 80;
            GlowOverlay.HorizontalAlignment = HorizontalAlignment.Left;
            GlowOverlay.VerticalAlignment = VerticalAlignment.Top;
            GlowOverlay.Margin = new Thickness(dx - 40, dy - 40, 0, 0);
        }

        // Метод для установки прозрачности DWM-превью
        private void SetDwmOpacity(byte alpha)
        {
            if (_currentDwmThumb == IntPtr.Zero) return;
            var props = new Native.DWM_THUMBNAIL_PROPERTIES
            {
                dwFlags = Native.DWM_TNP_VISIBLE | Native.DWM_TNP_OPACITY,
                fVisible = true,
                opacity = alpha,
                fSourceClientAreaOnly = false
            };
            Native.DwmUpdateThumbnailProperties(_currentDwmThumb, ref props);  // Обновляем свойства DWM-превью
        }

        // Обработчик нажатия клавиш
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right) { NextWindow(); e.Handled = true; }
            else if (e.Key == Key.Left) { PreviousWindow(); e.Handled = true; }
            else if (e.Key == Key.Enter) { ConfirmAndClose(); }
            else if (e.Key == Key.Escape) { Close(); }
        }

        // Обработчик отпускания клавиш
        private void Window_KeyUp(object sender, KeyEventArgs e) { }

        // Метод активации выбранного окна
        private void ActivateSelected()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
                WindowActivator.ActivateWindow(_items[_selectedIndex].Hwnd);
        }

        // Метод для подтверждения и закрытия окна
        public void ConfirmAndClose() { ActivateSelected(); Close(); }

        // Переход к следующему окну
        public void NextWindow()
        {
            if (_items.Count == 0) return;
            _selectedIndex = (_selectedIndex + 1) % _items.Count;  // Следующее окно
            UpdateTransforms();  // Обновляем трансформации
            UpdateOverlay();  // Обновляем overlay
        }

        // Переход к предыдущему окну
        public void PreviousWindow()
        {
            if (_items.Count == 0) return;
            _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;  // Предыдущее окно
            UpdateTransforms();  // Обновляем трансформации
            UpdateOverlay();  // Обновляем overlay
        }
    }
}
