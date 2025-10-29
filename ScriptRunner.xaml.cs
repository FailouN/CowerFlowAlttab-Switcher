using System.IO;
using System;
using System.Diagnostics;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Windows.Controls;

namespace CoverflowAltTab
{
    public class ScriptRunner : Window
    {
        private readonly SettingsWindow _settingsWindow;
        public Process? _ahkProcess;


        public ScriptRunner()
 
        {
          _settingsWindow = new SettingsWindow();
            this.Visibility = Visibility.Hidden; // Делаем окно невидимым
            this.Loaded += OnWindowLoaded;
         }

           private void OnWindowLoaded(object sender, RoutedEventArgs e)        
           {
            // После загрузки окна запускаем скрипт AHK
            _settingsWindow.CreateAndRunAltTabScript();
           }

           public void CloseScriptAndApp()
           {
            _settingsWindow.StopAltTabScript();

            // Закрытие приложения
            Application.Current.Shutdown();
           }
  }
}
