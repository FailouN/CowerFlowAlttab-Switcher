using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;  // Используем TaskbarIcon для работы с системным треем
using System.Windows.Media.Imaging;  // Для работы с изображениями
using System.IO;
using System.Reflection;

namespace CoverflowAltTab
{
    public partial class App : Application
    {
        private ScriptRunner? _scriptRunner;
        private HotkeyWindow? _hotkeyWindow;
        private TaskbarIcon? _trayIcon;  // Используем TaskbarIcon вместо WinForms.NotifyIcon
        private static string DocumentsIconsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "CoverFlowAltTab", "Icons");
          
        private static void ExtractIconsIfNeeded()
        {
            try
            {
                // Проверяем, существует ли папка для иконок в "Документах"
                if (!Directory.Exists(DocumentsIconsPath))
                {
                    Directory.CreateDirectory(DocumentsIconsPath); // Создаём папку, если её нет
                    Console.WriteLine($"Создана папка для иконок: {DocumentsIconsPath}");
                }

                // Путь к иконкам в выходной папке (из директории с исполняемым файлом)
                var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                Console.WriteLine($"Путь к выходной папке ресурсов: {outputPath}");

                // Имена иконок
                var iconNames = new string[] { "AutoHotkey.exe", "BlockAltTab.ahk", "key.png", "scene.png", "Load.png" };

                foreach (var iconName in iconNames)
                {
                    string sourceFilePath = Path.Combine(outputPath, iconName);
                    string destinationFilePath = Path.Combine(DocumentsIconsPath, iconName);

                    // Логируем пути
                    Console.WriteLine($"Пытаемся скопировать {iconName} из {sourceFilePath} в {destinationFilePath}");

                    if (File.Exists(sourceFilePath))
                    {
                        if (!File.Exists(destinationFilePath))
                        {
                            // Копируем файл из выходной папки в Documents
                            File.Copy(sourceFilePath, destinationFilePath);
                            Console.WriteLine($"Иконка {iconName} успешно скопирована в {destinationFilePath}");
                        }
                        else
                        {
                            Console.WriteLine($"Иконка {iconName} уже существует в {destinationFilePath}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка: файл {iconName} не найден в выходной папке: {sourceFilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, если произошла ошибка распаковки
                Console.WriteLine($"Ошибка при распаковке иконок: {ex.Message}");
            }
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Загружаем настройки при старте
            Console.WriteLine("Загрузка настроек...");
            SceneSettings.Instance.Load(); // Логирование загрузки
            ExtractIconsIfNeeded();

            _hotkeyWindow = new HotkeyWindow();
            _hotkeyWindow.Show();
            _hotkeyWindow.Hide();

            // Создаем иконку для системного трея
            _trayIcon = new TaskbarIcon
            {
                IconSource = new BitmapImage(new Uri("pack://application:,,,/resources/app_icon.ico")),  // Используем IconSource
                ToolTipText = "CoverflowAltTab",
                Visibility = System.Windows.Visibility.Visible  // Используем Visibility вместо Visible
            };

            // Устанавливаем иконку для главного окна
            this.MainWindow.Icon = new BitmapImage(new Uri("pack://application:,,,/resources/app_icon.ico"));

            // Контекстное меню для иконки в системном трее
            var menu = new System.Windows.Controls.ContextMenu();

            var settingsItem = new System.Windows.Controls.MenuItem { Header = "Настройки 3D" };
            settingsItem.Click += Tray_OpenSettings_Click;
            menu.Items.Add(settingsItem);

            var exitItem = new System.Windows.Controls.MenuItem { Header = "Выход" };
            exitItem.Click += Tray_Exit_Click;
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenu = menu;
            _scriptRunner = new ScriptRunner();
            _scriptRunner.Show(); // Окно запускает AHK скрипт
        }

        private void Tray_OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            // Открытие окна настроек при клике на пункт меню в трее
            var win = new SettingsWindow();
            win.Show();
        }

        private void Tray_Exit_Click(object sender, RoutedEventArgs e)
        {
            // Закрытие приложения при клике на пункт "Выход" в контекстном меню
            _scriptRunner?.CloseScriptAndApp(); // Теперь вызываем этот метод из ScriptRunner
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Сохраняем настройки при выходе
            Console.WriteLine("Сохранение настроек...");
            SceneSettings.Instance.Save(); // Логирование сохранения

            base.OnExit(e);
            _hotkeyWindow?.Close();

            // Убираем иконку из трея при выходе
            _trayIcon?.Dispose();
        }
    }
}
