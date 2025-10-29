using System;
using System.IO;

public static class Logger
{
    // Путь к папке "CoverFlow Settings" в "Документах"
    private static string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoverFlow Settings");

    // Создаем папку, если она не существует
    static Logger()
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    // Путь для логирования
    private static string logFilePath = Path.Combine(folderPath, "app_log.txt");

    // Метод для записи в лог
    public static void WriteLog(string message)
    {
        try
        {
            using (StreamWriter sw = new StreamWriter(logFilePath, true))  // true для добавления в конец файла
            {
                sw.WriteLine($"{DateTime.Now}: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при записи в лог: " + ex.Message);
        }
    }
}
