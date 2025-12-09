using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace AssignedAccessDesigner.ViewModels
{
    public class AppSettings : INotifyPropertyChanged
    {
        //private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        private string _defaultDirectory;
        private bool _isDarkTheme;

        public AppSettings()
        {
          //  _defaultDirectory = localSettings.Values["DefaultDirectory"] as string
          //      ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
          //  _isDarkTheme = (localSettings.Values["IsDarkTheme"] as bool?) ?? false;
        }

        public string DefaultDirectory
        {
            get => _defaultDirectory;
            set
            {
                _defaultDirectory = value;
            //    localSettings.Values["DefaultDirectory"] = value;
                OnPropertyChanged();
            }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                _isDarkTheme = value;
            //    localSettings.Values["IsDarkTheme"] = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}