using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

public class SceneSettings : INotifyPropertyChanged
{
    private static readonly SceneSettings _instance = new SceneSettings();  // Экземпляр класса (синглтон)
    public static SceneSettings Instance => _instance;  // Статический доступ к экземпляру

    public event PropertyChangedEventHandler? PropertyChanged;  // Событие для изменения свойства
    public event EventHandler? SettingsChanged;  // Событие для изменения настроек

    // Конструктор без параметров, необходим для десериализации
    public SceneSettings() { }

    // Метод для уведомления об изменении свойства
    private void OnPropertyChanged(string propName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));  // Уведомление об изменении свойства
        SettingsChanged?.Invoke(this, EventArgs.Empty);  // Уведомление об изменении настроек
    }

    // Параметры расположения элементов на сцене

    private double _spacingX = 550;  // Расстояние по оси X между элементами
    public double SpacingX { get => _spacingX; set { if (_spacingX != value) { _spacingX = value; OnPropertyChanged(nameof(SpacingX)); } } }

    private double _sideAngle = -80;  // Угол наклона элементов с боковых сторон
    public double SideAngle { get => _sideAngle; set { if (_sideAngle != value) { _sideAngle = value; OnPropertyChanged(nameof(SideAngle)); } } }

    private double _sideScale1 = 0.7;  // Масштаб для первого бокового элемента
    public double SideScale1 { get => _sideScale1; set { if (_sideScale1 != value) { _sideScale1 = value; OnPropertyChanged(nameof(SideScale1)); } } }

    private double _sideScale2 = 0.5;  // Масштаб для второго бокового элемента
    public double SideScale2 { get => _sideScale2; set { if (_sideScale2 != value) { _sideScale2 = value; OnPropertyChanged(nameof(SideScale2)); } } }

    private double _stackSpacing = 50;  // Расстояние между элементами в стопке
    public double StackSpacing { get => _stackSpacing; set { if (_stackSpacing != value) { _stackSpacing = value; OnPropertyChanged(nameof(StackSpacing)); } } }

    private double _stackDepth = -60;  // Глубина элементов в стопке
    public double StackDepth { get => _stackDepth; set { if (_stackDepth != value) { _stackDepth = value; OnPropertyChanged(nameof(StackDepth)); } } }

    private double _sceneDepth = -1250;  // Общая глубина сцены
    public double SceneDepth { get => _sceneDepth; set { if (_sceneDepth != value) { _sceneDepth = value; OnPropertyChanged(nameof(SceneDepth)); } } }

    private double _stackMinScale = 0.9;  // Минимальный масштаб элементов в стопке
    public double StackMinScale { get => _stackMinScale; set { if (_stackMinScale != value) { _stackMinScale = value; OnPropertyChanged(nameof(StackMinScale)); } } }

    private double _stackMaxScale = 1.8;  // Максимальный масштаб элементов в стопке
    public double StackMaxScale { get => _stackMaxScale; set { if (_stackMaxScale != value) { _stackMaxScale = value; OnPropertyChanged(nameof(StackMaxScale)); } } }

    private double _centerOpacity = 0.0;  // Прозрачность центрального элемента
    public double CenterOpacity { get => _centerOpacity; set { if (_centerOpacity != value) { _centerOpacity = value; OnPropertyChanged(nameof(CenterOpacity)); } } }

    private double _sideOpacity = 1.0;  // Прозрачность боковых элементов
    public double SideOpacity { get => _sideOpacity; set { if (_sideOpacity != value) { _sideOpacity = value; OnPropertyChanged(nameof(SideOpacity)); } } }

    private double _fadeStep = 0.0;  // Шаг плавного исчезновения боковых элементов
    public double FadeStep { get => _fadeStep; set { if (_fadeStep != value) { _fadeStep = value; OnPropertyChanged(nameof(FadeStep)); } } }

    // Путь для конфигурационного файла
    private static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CoverFlow Settings", "scene_settings.json");

    // Новый параметр для сохранения выбранной горячей клавиши
    private string _selectedHotkey = "CTRL+ALT+Q";  // Значение по умолчанию для горячей клавиши
    public string SelectedHotkey
    {
        get => _selectedHotkey;
        set
        {
            if (_selectedHotkey != value)
            {
                _selectedHotkey = value;  // Обновляем горячую клавишу
                OnPropertyChanged(nameof(SelectedHotkey));  // Уведомляем об изменении
            }
        }
    }

    // Метод для сохранения настроек в файл
public void Save()
{
    try
    {
        var dir = Path.GetDirectoryName(ConfigPath);  // Получаем директорию для сохранения

        // Если dir равно null, устанавливаем пустую строку или логируем ошибку
        if (dir == null)
        {
            Logger.WriteLog("Ошибка: путь к директории не может быть определён.");
            return;  // Выходим из метода, если директория не может быть определена
        }

        // Логируем путь для сохранения
        Logger.WriteLog($"Путь для сохранения файла настроек: {ConfigPath}");

        // Если директория не существует, создаём её
        if (!Directory.Exists(dir))
        {
            Logger.WriteLog($"Директория не существует, создаем: {dir}");
            Directory.CreateDirectory(dir);
        }

        // Сериализуем объект SceneSettings в JSON
        var json = JsonSerializer.Serialize(this, GetType(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);  // Сохраняем в файл

        // Логируем успешное сохранение
        Logger.WriteLog("Настройки успешно сохранены.");
    }
    catch (Exception ex)
    {
        // Логируем ошибку
        Logger.WriteLog("Ошибка при сохранении настроек: " + ex.Message);
    }
}


    // Метод для загрузки настроек из файла
    public void Load()
    {
        try
        {
            Logger.WriteLog($"Путь для загрузки файла настроек: {ConfigPath}");

            // Если файл настроек существует, загружаем его
            if (File.Exists(ConfigPath))
            {
                Logger.WriteLog("Файл настроек найден.");

                // Читаем содержимое файла
                var json = File.ReadAllText(ConfigPath);
                Logger.WriteLog("Содержимое файла настроек: " + json);  // Логируем содержимое

                // Десериализуем JSON в объект SceneSettings
                var loaded = JsonSerializer.Deserialize<SceneSettings>(json);

                if (loaded != null)
                {
                    // Обновляем параметры из загруженных настроек
                    SpacingX = loaded.SpacingX;
                    SideAngle = loaded.SideAngle;
                    SideScale1 = loaded.SideScale1;
                    SideScale2 = loaded.SideScale2;
                    StackSpacing = loaded.StackSpacing;
                    StackDepth = loaded.StackDepth;
                    SceneDepth = loaded.SceneDepth;
                    StackMinScale = loaded.StackMinScale;
                    StackMaxScale = loaded.StackMaxScale;
                    CenterOpacity = loaded.CenterOpacity;
                    SideOpacity = loaded.SideOpacity;
                    FadeStep = loaded.FadeStep;
                    SelectedHotkey = loaded.SelectedHotkey;

                    // Обновляем привязку данных для интерфейса
                    OnPropertyChanged(nameof(SpacingX));
                    OnPropertyChanged(nameof(SideAngle));
                    OnPropertyChanged(nameof(SideScale1));
                    OnPropertyChanged(nameof(SideScale2));
                    OnPropertyChanged(nameof(StackSpacing));
                    OnPropertyChanged(nameof(StackDepth));
                    OnPropertyChanged(nameof(SceneDepth));
                    OnPropertyChanged(nameof(StackMinScale));
                    OnPropertyChanged(nameof(StackMaxScale));
                    OnPropertyChanged(nameof(CenterOpacity));
                    OnPropertyChanged(nameof(SideOpacity));
                    OnPropertyChanged(nameof(FadeStep));
                    OnPropertyChanged(nameof(SelectedHotkey));

                    Logger.WriteLog("Настройки успешно загружены.");
                }
                else
                {
                    Logger.WriteLog("Ошибка: не удалось десериализовать настройки.");
                }
            }
            else
            {
                Logger.WriteLog("Файл настроек не найден.");
            }
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            Logger.WriteLog("Ошибка при загрузке настроек: " + ex.Message);
        }
    }

    // Метод для сброса настроек в значения по умолчанию
    public void ResetDefaults()
    {
        SpacingX = 600;
        SideAngle = -60;
        SideScale1 = 0.7;
        SideScale2 = 0.5;
        StackSpacing = 50;
        StackDepth = -30;
        SceneDepth = -1300;
        StackMinScale = 1.1;
        StackMaxScale = 1.6;
        CenterOpacity = 0;
        SideOpacity = 1;
        FadeStep = 0;
        SelectedHotkey = "CTRL\u002BALT\u002BQ";  // Устанавливаем значение по умолчанию для горячей клавиши

        Logger.WriteLog("Настройки сброшены в значения по умолчанию.");
    }
}
