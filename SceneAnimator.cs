// SceneAnimator.cs
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System;
using System.Windows.Media.Media3D; // для 3D-материалов
using System.Windows.Media.Animation; // для анимации

namespace CoverflowAltTab
{
    public class SceneAnimator
    {
        private const int ANIMATION_DURATION_MS = 450; // Длительность анимации в миллисекундах

        // Метод для анимации трансформации элемента (масштаб, вращение, сдвиг)
        public void AnimateTransform(CoverflowItem item, int offset, bool isSelected)
        {
            if (item == null || item.Model == null) return;  // Проверка на null для элемента и его модели

            // Создаём группу трансформаций (масштаб, поворот, сдвиг)
            var tg = new Transform3DGroup();
            var scale = new ScaleTransform3D(1, 1, 1);  // Масштаб (по всем осям изначально равен 1)
            var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0); // Поворот (ось Y, угол 0)
            var rotate = new RotateTransform3D(rotation); // Преобразование для поворота
            var translate = new TranslateTransform3D(0, 0, 0); // Сдвиг по осям X, Y, Z (изначально равен 0)

            tg.Children.Add(scale);   // Добавляем масштаб в группу трансформаций
            tg.Children.Add(rotate);  // Добавляем поворот в группу трансформаций
            tg.Children.Add(translate); // Добавляем сдвиг в группу трансформаций
            item.Model.Transform = tg;  // Применяем трансформацию к модели элемента

            var cfg = SceneSettings.Instance; // Получаем текущие настройки сцены

            // --- Масштаб внутри стопки: от StackMinScale (центр) к StackMaxScale (далее)
            double baseScale;
            if (isSelected)
            {
                baseScale = cfg.StackMinScale; // Центральный элемент — самый маленький
            }
            else
            {
                int depth = Math.Abs(offset);  // Глубина (относительное положение в стопке)
                double t = Math.Min(1.0, depth / 5.0); // Подгонка скорости нарастания масштаба
                baseScale = cfg.StackMinScale + (cfg.StackMaxScale - cfg.StackMinScale) * t;
            }

            // --- Позиция по X: стопка (плотно сгруппирована рядом со SpacingX)
            double targetX = 0; // Исходное значение для позиции по X
            if (offset < 0)
                targetX = -cfg.SpacingX - (Math.Abs(offset) - 1) * cfg.StackSpacing; // Смещение влево
            else if (offset > 0)
                targetX = cfg.SpacingX + (Math.Abs(offset) - 1) * cfg.StackSpacing; // Смещение вправо

            // --- Позиция по Z: общая глубина + шаг глубины внутри стопки
            double targetZ = cfg.SceneDepth + (Math.Abs(offset) - 1) * cfg.StackDepth; // Глубина сцены и шаг глубины

            // --- Угол и ось поворота
            double targetAngle = 0;  // Исходный угол поворота
            Vector3D rotationAxis = new Vector3D(0, 1, 0); // Ось вращения (по оси Y)

            if (offset < 0)
            {
                // Левое окно: поворачиваем вправо (к центру)
                targetAngle = cfg.SideAngle;
                rotationAxis = new Vector3D(0, -1, 0); // Задаем ось вращения для левого окна
            }
            else if (offset > 0)
            {
                // Правое окно: поворачиваем зеркально
                targetAngle = -cfg.SideAngle;
                rotationAxis = new Vector3D(0, -1, 0); // Зеркальный эффект для правого окна
            }

            // --- Масштабирование с учетом перспективы
            double perspectiveFix = 1.0;
            if (Math.Abs(targetZ) > 1e-6 && Math.Abs(cfg.SceneDepth) > 1e-6)
                perspectiveFix = Math.Abs(cfg.SceneDepth) / Math.Abs(targetZ); // Коррекция перспективы

            double targetScale = baseScale * perspectiveFix; // Итоговый масштаб

            var animDuration = TimeSpan.FromMilliseconds(ANIMATION_DURATION_MS); // Длительность анимации
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut }; // Функция плавности (EaseOut для замедления в конце)

            // анимация масштабирования
            scale.BeginAnimation(ScaleTransform3D.ScaleXProperty, new DoubleAnimation(targetScale, animDuration) { EasingFunction = easing });
            scale.BeginAnimation(ScaleTransform3D.ScaleYProperty, new DoubleAnimation(targetScale, animDuration) { EasingFunction = easing });

            // анимация поворота
            var newRotation = new AxisAngleRotation3D(rotationAxis, 0); // Новый поворот
            var newRotateTransform = new RotateTransform3D(newRotation);
            if (tg.Children.Count >= 2) tg.Children[1] = newRotateTransform; // Заменяем старую трансформацию
            newRotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, new DoubleAnimation(targetAngle, animDuration) { EasingFunction = easing });

            // анимация сдвига
            translate.BeginAnimation(TranslateTransform3D.OffsetXProperty, new DoubleAnimation(targetX, animDuration) { EasingFunction = easing });
            translate.BeginAnimation(TranslateTransform3D.OffsetZProperty, new DoubleAnimation(targetZ, animDuration) { EasingFunction = easing });

            // --- Прозрачность
            if (item.Model.Material is DiffuseMaterial mat && mat.Brush is ImageBrush ib)
            {
                double targetOpacity;
                if (isSelected)
                {
                    targetOpacity = cfg.CenterOpacity; // Прозрачность для центрального элемента
                }
                else
                {
                    // Прозрачность для боковых элементов, уменьшающаяся с расстоянием
                    targetOpacity = cfg.SideOpacity - Math.Min(cfg.FadeStep * (Math.Abs(offset) - 1), cfg.SideOpacity);
                    if (targetOpacity < 0) targetOpacity = 0; // Не допускаем отрицательную прозрачность
                }

                // Анимация изменения прозрачности
                ib.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(targetOpacity, animDuration) { EasingFunction = easing });
            }
        }
    }
}
