﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CefBrowserControl;

namespace CefBrowser
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 2 && e.Args[1] == "--debug" && Debugger.IsAttached == false)
                Debugger.Launch();
           
            var window = new WebBrowserEx();
            window.Visibility = Options.WindowsNormallyVisible ? Visibility.Visible : Visibility.Hidden;
                window.Show();
        }

    }
}
