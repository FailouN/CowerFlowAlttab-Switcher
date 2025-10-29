// Scene3D.cs
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Windows.Media; // для работы с цветами и материалами
using System.Windows.Media.Media3D; // для 3D-моделей и мешей
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging; // для работы с изображениями

namespace CoverflowAltTab
{
    public class Scene3D
    {
        private readonly Model3DGroup _modelsGroup;  // Группа 3D-моделей для сцены
        private const double CENTER_MAX_WIDTH = 800;  // Максимальная ширина для центральной модели
        private const double CENTER_MAX_HEIGHT = 600; // Максимальная высота для центральной модели

        // Конструктор класса, инициализирует группу моделей
        public Scene3D(Model3DGroup modelsGroup)
        {
            _modelsGroup = modelsGroup;
        }

        // Метод для построения моделей с использованием списка элементов и DPI
        public void BuildModels(List<CoverflowItem> items, double dpi)
        {
            _modelsGroup.Children.Clear(); // Очищаем текущие модели из группы

            // Создание источников света
            var directionalLight = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1)); // Направленный свет
            var ambientLight = new AmbientLight(Colors.White); // Дополнительный мягкий свет

            // Группа света
            var lights = new Model3DGroup();
            lights.Children.Add(directionalLight); // Добавляем направленный свет
            lights.Children.Add(ambientLight);     // Добавляем амбиентный свет

            // Добавление света в 3D-сцену
            _modelsGroup.Children.Add(lights);

            // Перебор каждого элемента в списке и создание для него 3D-модели
            foreach (var item in items)
            {
                var mesh = new MeshGeometry3D(); // Создание геометрической сетки для модели
                double halfW = CENTER_MAX_WIDTH / 2; // Половина ширины для вычислений
                double halfH = CENTER_MAX_HEIGHT / 2; // Половина высоты для вычислений

                // Задаём вершины (координаты) для 3D-объекта
                mesh.Positions = new Point3DCollection(new[]
                {
                    new Point3D(-halfW, halfH, 0),  // Верхний левый угол
                    new Point3D(halfW, halfH, 0),   // Верхний правый угол
                    new Point3D(halfW, -halfH, 0),  // Нижний правый угол
                    new Point3D(-halfW, -halfH, 0), // Нижний левый угол
                });

                // Задаём индексы для треугольников (где 0, 1, 2 — это индексы точек)
                mesh.TriangleIndices = new Int32Collection(new[] { 0, 1, 2, 2, 3, 0 });

                // Задаём текстурные координаты для наложения изображения
                mesh.TextureCoordinates = new PointCollection(new[]
                {
                    new Point(0, 0),
                    new Point(1, 0),
                    new Point(1, 1),
                    new Point(0, 1),
                });

                // Берём превью элемента или создаём плейсхолдер с иконкой и заголовком
                var brush = new ImageBrush(item.Preview ?? CreatePlaceholderWithIcon(item.Icon, item.Title, CENTER_MAX_WIDTH, CENTER_MAX_HEIGHT, dpi))
                {
                    Stretch = Stretch.Uniform // Растягиваем изображение по центру
                };

                var material = new DiffuseMaterial(brush); // Материал, который будет использоваться для модели

                // Создаём 3D-модель с геометрией и материалом
                var geom = new GeometryModel3D(mesh, material)
                {
                    BackMaterial = material // Назначаем материал для задней стороны
                };

                // Добавляем модель в 3D-группу
                _modelsGroup.Children.Add(geom);  

                // Устанавливаем созданную модель для элемента
                item.Model = geom;
            }
        }

        // Метод для создания плейсхолдера с иконкой и текстом
        private ImageSource CreatePlaceholderWithIcon(ImageSource? icon, string title, double w, double h, double dpi)
        {
            var dv = new DrawingVisual(); // Создание визуала для рисования
            using (var dc = dv.RenderOpen()) // Открытие контекста рисования
            {
                dc.DrawRectangle(Brushes.DimGray, null, new Rect(0, 0, w, h)); // Рисуем прямоугольник
                if (icon != null) dc.DrawImage(icon, new Rect(10, 10, 64, 64)); // Если есть иконка, рисуем её
                if (!string.IsNullOrEmpty(title)) // Если заголовок не пустой
                {
                    var ft = new FormattedText(title,
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 20, Brushes.White,
                        dpi); // Форматируем текст
                    dc.DrawText(ft, new Point(12, h - 40)); // Рисуем текст
                }
            }
            var bmp = new RenderTargetBitmap((int)w, (int)h, 96, 96, PixelFormats.Pbgra32); // Создание битмапа для вывода изображения
            bmp.Render(dv); // Рендерим изображение в битмап
            bmp.Freeze(); // Замораживаем битмап для использования в UI
            return bmp; // Возвращаем готовый битмап
        }
    }
}
