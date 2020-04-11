﻿using System.IO;
using BannerLordLauncher.ViewModels;
using Newtonsoft.Json;

namespace BannerLordLauncher.Views
{
    using System;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        internal AppConfig Configuration { get; }
        private readonly string _configurationFilePath;

        public MainWindow()
        {
            this._configurationFilePath = Path.Combine(GetApplicationRoot(), "configuration.json");
            try
            {
                if (File.Exists(this._configurationFilePath))
                {
                    this.Configuration =
                        JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(this._configurationFilePath));
                }
            }
            catch
            {
                this.Configuration = null;
            }
            if (this.Configuration == null) this.Configuration = new AppConfig();

            InitializeComponent();

            var model = new MainWindowViewModel(this);
            this.DataContext = model;
            model.Initialize();
        }

        private static string GetApplicationRoot()
        {
            return Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }

        protected override void OnClosed(EventArgs e)
        {
            var settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented };
            File.WriteAllText(this._configurationFilePath, JsonConvert.SerializeObject(this.Configuration, settings));

            base.OnClosed(e);
        }
    }
}
