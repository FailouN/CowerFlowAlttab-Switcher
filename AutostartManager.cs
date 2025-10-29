using System;
using System.IO;
using Microsoft.Win32;

namespace CoverflowAltTab
{
    public class AutostartManager
    {
        private string _appPath;

        public AutostartManager()
        {
            _appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverflowAltTab.exe");
        }

        // Добавление в автозагрузку через реестр
        public void AddToStartup()
        {
            string keyName = "CoverflowAltTab"; // Имя записи в реестре
            string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run"; // Путь к разделу реестра

            using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true))  // Открываем ключ для изменения
            {
                if (key != null && key.GetValue(keyName) == null)  // Проверяем, есть ли запись
                {
                    // Добавляем запись, которая указывает на путь к приложению
                    key.SetValue(keyName, _appPath);
                }
            }
        }

        // Удаление из автозагрузки через реестр
        public void RemoveFromStartup()
        {
            string keyName = "CoverflowAltTab"; // Имя записи в реестре
            string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run"; // Путь к разделу реестра

            using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true))  // Открываем ключ для изменения
            {
                if (key != null && key.GetValue(keyName) != null)  // Проверяем, существует ли запись
                {
                    // Удаляем запись
                    key.DeleteValue(keyName);
                }
            }
        }

        // Проверка, добавлен ли ярлык в автозагрузку
        public bool IsInStartup()
        {
            string keyName = "CoverflowAltTab"; // Имя записи в реестре
            string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run"; // Путь к разделу реестра

            using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                return key != null && key.GetValue(keyName) != null;  // Проверяем, существует ли запись
            }
        }
    }
}
