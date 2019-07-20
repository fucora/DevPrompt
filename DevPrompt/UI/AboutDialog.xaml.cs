﻿using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.ComponentModel;


namespace DevPrompt.UI
{
    internal partial class AboutDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string AppVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private Api.IWindow window;
        private string appLatestVersion;
        private HttpClient httpClient;

        public AboutDialog(Api.IWindow window)
        {
            this.window = window;
            this.appLatestVersion = "checking...";
            this.InitializeComponent();
        }

        public string AppLatestVersion
        {
            get => this.appLatestVersion;

            set
            {
                this.appLatestVersion = value ?? string.Empty;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.AppLatestVersion)));
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs args)
        {
            using (HttpClient client = new HttpClient())
            using (this.window.BeginLoading(client.CancelPendingRequests, "Checking latest version"))
            {
                try
                {
                    this.httpClient = client;
                    string versionString = await client.GetStringAsync(@"http://peterspada.com/DevPrompt/GetLatestVersion");
                    this.AppLatestVersion = versionString;
                }
                catch
                {
                    // doesn't matter
                    this.AppLatestVersion = "failed";
                }
                finally
                {
                    this.httpClient = null;
                }
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (this.httpClient != null)
            {
                this.httpClient.CancelPendingRequests();
            }
        }

        public string CopyrightYear
        {
            get
            {
                AssemblyCopyrightAttribute copyright = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>();
                return copyright?.Copyright ?? string.Empty;
            }
        }

        private void OnHyperlink(object sender, RequestNavigateEventArgs args)
        {
            if (sender is Hyperlink hyperlink && hyperlink.NavigateUri != null)
            {
                this.window.RunExternalProcess(hyperlink.NavigateUri.ToString());
            }
        }
    }
}
