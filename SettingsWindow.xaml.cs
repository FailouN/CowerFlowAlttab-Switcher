using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Для работы с ComboBox и ComboBoxItem
using System.Windows.Media.Imaging;

namespace CoverflowAltTab
{
    public partial class SettingsWindow : Window
    {
        public Process? _ahkProcess; // Ссылка на процесс, чтобы можно было его завершить
        private AutostartManager _autostartManager = new AutostartManager();  // Инициализация менеджера автозапуска
        public string SceneIconPath { get; set; } = string.Empty;  // Путь к иконке сцены
        public string HotkeyIconPath { get; set; } = string.Empty;  // Путь к иконке горячих клавиш
        public string LoadIconPath { get; set; } = string.Empty;  // Путь к иконке загрузки

        // Конструктор окна настроек
        public SettingsWindow()
        {
            try
            {
                Logger.WriteLog("Открытие окна настроек.");

                InitializeComponent();  // Инициализация компонентов окна
                LoadHotkeyFromConfig();  // Загружаем горячую клавишу из конфигурации
                _autostartManager = new AutostartManager();  // Инициализация менеджера автозапуска
                UpdateAutoStartButton();  // Обновляем состояние кнопки автозапуска

                // Устанавливаем контекст данных для настроек (все свойства из SceneSettings будут привязаны)
                this.DataContext = SceneSettings.Instance;
                Logger.WriteLog("DataContext настроен.");

                // Устанавливаем путь для каждой иконки
                string iconPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoverFlowAltTab", "Icons");

                // Путь к иконкам
                SceneIconPath = Path.Combine(iconPath, "scene.png");
                HotkeyIconPath = Path.Combine(iconPath, "key.png");
                LoadIconPath = Path.Combine(iconPath, "Load.png");

                // Устанавливаем контекст данных для иконок
                this.IconDataContext = this;

                // Настройка видимости панелей
                SceneSettingsPanel.Visibility = Visibility.Visible;
                HotkeysPanel.Visibility = Visibility.Collapsed;
                SaveLoadPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке окна: " + ex.Message);
            }
        }

        // Контекст данных для иконок
        public object IconDataContext { get; set; } = new object();  

        // Метод для загрузки горячей клавиши из конфигурации
        private void LoadHotkeyFromConfig()
        {
            // Загружаем конфиг
            SceneSettings.Instance.Load();

            // Устанавливаем выбранную горячую клавишу в ComboBox
            if (SceneSettings.Instance.SelectedHotkey == "ALT+TAB")
            {
                HotkeyComboBox.SelectedItem = HotkeyComboBox.Items[0];  // Выбираем ALT+TAB
            }
            else if (SceneSettings.Instance.SelectedHotkey == "CTRL+ALT+Q")
            {
                HotkeyComboBox.SelectedItem = HotkeyComboBox.Items[1];  // Выбираем CTRL+ALT+Q
            }
        }

        // Обработчик для кнопки "Включить/Отключить автозапуск"
        private void AutoStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_autostartManager.IsInStartup())
            {
                _autostartManager.RemoveFromStartup();  // Удаляем из автозагрузки
            }
            else
            {
                _autostartManager.AddToStartup();  // Добавляем в автозагрузку
            }

            UpdateAutoStartButton();  // Обновляем текст на кнопке
        }

        // Обновление текста на кнопке в зависимости от того, включена ли автозагрузка
        private void UpdateAutoStartButton()
        {
            if (_autostartManager.IsInStartup())
            {
                AutoStartButton.Content = "Disable Autostart";  // Если в автозагрузке, показываем "Отключить автозагрузку"
            }
            else
            {
                AutoStartButton.Content = "Enable Autostart";  // Если не в автозагрузке, показываем "Включить автозагрузку"
            }
        }

        // Обработчик для кнопки закрытия окна
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.WriteLog("Закрытие окна.");
            this.Close();  // Закрываем окно
        }

        // Обработчик для перетаскивания окна
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
          if (e.ChangedButton == MouseButton.Left)
          {
             this.DragMove();  // Перетаскивание окна
          }
         }

        // Обработчик для кнопки сворачивания окна
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;  // Сворачиваем окно
        }

        // Обработчик для кнопки разворачивания окна
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;  // Разворачиваем окно
            }
            else
            {
                this.WindowState = WindowState.Normal;  // Возвращаем окно в нормальный размер
            }
        }

        // Обработчик для кнопки "Сохранить настройки"
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SceneSettings.Instance.Save();  // Сохраняем настройки
            MessageBox.Show("Настройки успешно сохранены.");  // Информируем пользователя о сохранении
        }

        // Обработчик для кнопки "Загрузить настройки"
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            SceneSettings.Instance.Load();  // Загружаем настройки
            MessageBox.Show("Настройки успешно загружены.");  // Информируем пользователя о загрузке
        }

        // Обработчик для кнопки "Сбросить настройки"
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SceneSettings.Instance.ResetDefaults();  // Сбрасываем настройки в значения по умолчанию
        }

        // Обработчик для кнопки "Открыть папку конфигурации"
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем путь к папке конфигов в "Документах"
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string configFolderPath = Path.Combine(documentsPath, "CoverFlow Settings");

            // Если папка существует, открываем её в проводнике
            if (Directory.Exists(configFolderPath))
            {
                Process.Start("explorer.exe", configFolderPath);
            }
            else
            {
                MessageBox.Show("Папка с конфигом не найдена!");  // Если папка не существует, сообщаем пользователю
            }
        }

        // Обработчик для кнопки "Сохранить горячие клавиши"
       private void SaveHotkeyButton_Click(object sender, RoutedEventArgs e)
{
    var selectedItem = HotkeyComboBox.SelectedItem as ComboBoxItem;
    
    if (selectedItem == null) return;  // Если элемент не выбран, выходим

    // Получаем выбранную горячую клавишу
    string selectedHotkey = selectedItem.Content?.ToString() ?? string.Empty;  // Если Content может быть null, заменяем на пустую строку

    // Сохраняем в настройки
    SceneSettings.Instance.SelectedHotkey = selectedHotkey;
    SceneSettings.Instance.Save();  // Сохраняем настройки

    // Проверка и запуск/остановка скрипта в зависимости от выбранной горячей клавиши
    if (selectedHotkey == "ALT+TAB")
    {
        CreateAndRunAltTabScript();  // Если выбран ALT+TAB, запускаем скрипт
    }
    else if (selectedHotkey == "CTRL+ALT+Q")
    {
        StopAltTabScript();  // Если выбран CTRL+ALT+Q, останавливаем скрипт
    }
}


        // Обработчик для кнопки "Настройки сцены"
        private void SceneSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SceneSettingsPanel.Visibility = Visibility.Visible;  // Показываем панель настроек сцены
            HotkeysPanel.Visibility = Visibility.Collapsed;  // Скрываем панель горячих клавиш
            SaveLoadPanel.Visibility = Visibility.Collapsed;  // Скрываем панель сохранения/загрузки
        }

        // Обработчик для кнопки "Горячие клавиши"
        private void HotkeysButton_Click(object sender, RoutedEventArgs e)
        {
            SceneSettingsPanel.Visibility = Visibility.Collapsed;  // Скрываем панель настроек сцены
            HotkeysPanel.Visibility = Visibility.Visible;  // Показываем панель горячих клавиш
            SaveLoadPanel.Visibility = Visibility.Collapsed;  // Скрываем панель сохранения/загрузки
        }

        // Обработчик для кнопки "Сохранение и загрузка"
        private void SaveLoadButton_Click(object sender, RoutedEventArgs e)
        {
            SceneSettingsPanel.Visibility = Visibility.Collapsed;  // Скрываем панель настроек сцены
            HotkeysPanel.Visibility = Visibility.Collapsed;  // Скрываем панель горячих клавиш
            SaveLoadPanel.Visibility = Visibility.Visible;  // Показываем панель сохранения/загрузки
        }

        // Метод для создания и запуска AHK скрипта для ALT+TAB
        public void CreateAndRunAltTabScript()
        {
            string selectedHotkey = SceneSettings.Instance.SelectedHotkey;

            // Если выбран не ALT+TAB, не запускаем скрипт
            if (selectedHotkey != "ALT+TAB")
            {
                return;
            }

            // Если процесс уже запущен, не запускаем новый
            if (_ahkProcess != null && !_ahkProcess.HasExited)
            {
                return;
            }

            string ahkPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoverFlowAltTab", "Icons", "AutoHotkey.exe");  // Путь к AHK
            string scriptPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoverFlowAltTab", "Icons", "BlockAltTab.ahk");  // Путь к AHK скрипту

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ahkPath,
                Arguments = scriptPath + " /silent",  // Путь к AHK скрипту с флагом для скрытия
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas"
            };

            try
            {
                _ahkProcess = Process.Start(startInfo);  // Сохраняем процесс для возможного завершения
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске AHK скрипта: {ex.Message}");
            }
        }

        // Метод для остановки процесса перехвата ALT+TAB
        public void StopAltTabScript()
        {
            if (_ahkProcess != null && !_ahkProcess.HasExited)
            {
                _ahkProcess.Kill();  // Завершаем процесс, который перехватывает ALT+TAB
                _ahkProcess = null;  // Обнуляем ссылку на процесс
            }
        }
    }
}
