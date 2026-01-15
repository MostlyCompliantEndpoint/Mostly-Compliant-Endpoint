using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.Services;
using AssignedAccessDesigner.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AssignedAccessDesigner.Views
{
    public sealed partial class WizardStep3PinsPage : Page
    {
        private PolicyWizardViewModel _vm = new();
        public List<int>? profileIdList { get; private set; }

        public static Window? MainWindow { get; private set; }

        public WizardStep3PinsPage()
        {
            this.InitializeComponent();

            MainWindow = App.MainWindow;

            if (MainWindow != null)
            {
                MainWindow.SizeChanged += OnWindowSizeChanged;

                double height = MainWindow.Bounds.Height;
                double desiredHeight = Math.Max(100, height * 0.71); // your rule
                double width = MainWindow.Bounds.Width;
                double desiredWidth = Math.Max(100, width * 0.8);

                if (desiredHeight > 0)
                {
                    PinsList.Height = desiredHeight;
                    PinsList.MaxHeight = desiredHeight;
                }
                if (desiredWidth > 0)
                {
                    PinsList.Width = desiredWidth;
                    PinsList.MaxWidth = desiredWidth;
                }
                }
            }
        

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is PolicyWizardViewModel pvm) _vm = pvm;
            PinsList.ItemsSource = _vm.Policy.StartMenuPins;

            // Populat combo box
            if (_vm.Policy.Profiles.Count > 0)
            {
                var proList = new List<int> { };
                foreach (var profile in _vm.Policy.Profiles)
                {
                    proList.Add(profile.ProfileId);

                }

                List<StartMenuPin> removeStart = new List<StartMenuPin> { };
                foreach (var pin in _vm.Policy.StartMenuPins)
                {
                   pin.ProfileListIds = proList;
                    if (!proList.Contains(pin.ProfileId))
                    {
                        removeStart.Add(pin);
                    }
                }

                foreach (var pin in removeStart)
                {
                    _vm.Policy.StartMenuPins.Remove(pin);
                }

                profileIdList = proList;
            }

            PinsList.ItemsSource = null;
            PinsList.ItemsSource = _vm.Policy.StartMenuPins;

            base.OnNavigatedTo(e);
        }

        private void Add_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _vm.Policy.StartMenuPins.Add(new StartMenuPin{
                ProfileListIds = profileIdList == null ? new List<int>() : profileIdList
            });
            PinsList.ItemsSource = null; 
            PinsList.ItemsSource = _vm.Policy.StartMenuPins;
        }

        private void Remove_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (PinsList.SelectedItem is StartMenuPin pin)
            {
                _vm.Policy.StartMenuPins.Remove(pin);
                PinsList.ItemsSource = null; PinsList.ItemsSource = _vm.Policy.StartMenuPins;
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            double width = e.Size.Width;
            double height = e.Size.Height;


            double desiredHeight = Math.Max(100, height * 0.71);
            double desiredWidth = Math.Max(100, width * 0.8);

            if (desiredHeight > 0)
            {
                PinsList.Height = desiredHeight;
                PinsList.MaxHeight = desiredHeight;
            }
            if (desiredWidth > 0)
            {
                PinsList.Width = desiredWidth;
                PinsList.MaxWidth = desiredWidth;
            }
        }

        private void Path_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is StartMenuPin pin)
            {
                pin.Path = Helpers.FileHelper.CleanPath(tb.Text);
                tb.Text = pin.Path;
            }
        }

        private async void AddUwpApps_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<AppxPackage> SelectedApps = new();
            var allApps = AppxPackageService.GetInstalledUwpApps()
            .OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

            var filteredApps = new ObservableCollection<AppxPackage>(allApps);

            var searchBox = new AutoSuggestBox
            {
                PlaceholderText = "Search apps by Name or Publisher",
                QueryIcon = new SymbolIcon(Symbol.Find),
                Margin = new Thickness(0, 0, 0, 8)
            };


            searchBox.TextChanged += (s, args) =>
            {
                if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                    return;

                var q = searchBox.Text?.Trim() ?? string.Empty;

                // Suggestions (top 10)
                var suggestions = string.IsNullOrEmpty(q)
                    ? allApps.Take(10).Select(a => a.DisplayName).ToList()
                    : allApps.Where(a =>
                            a.DisplayName.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                            a.PublisherDisplayName.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                            a.FullName.Contains(q, StringComparison.CurrentCultureIgnoreCase))
                        .Take(10)
                        .Select(a => a.DisplayName)
                        .ToList();

                searchBox.ItemsSource = suggestions;

                // Filter the ListView’s backing collection
                AppxPackageService.FilterApps(allApps, filteredApps, q);
            };



            // Accept suggestions to set the query text
            searchBox.SuggestionChosen += (s, args) =>
            {
                searchBox.Text = args.SelectedItem?.ToString() ?? "";
                AppxPackageService.FilterApps(allApps, filteredApps, searchBox.Text);
            };

            // Submit search with Enter
            searchBox.QuerySubmitted += (s, args) =>
            {
                var q = args.QueryText?.Trim() ?? string.Empty;
                AppxPackageService.FilterApps(allApps, filteredApps, q);
            };




            // Build a ListView for selection
            var pickerListView = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single,
                ItemsSource = filteredApps,
                ItemTemplate = (DataTemplate)Resources["PickerItemTemplate"],
                MaxHeight = 600
            };


            ScrollViewer.SetVerticalScrollBarVisibility(pickerListView, ScrollBarVisibility.Auto);
            ScrollViewer.SetVerticalScrollMode(pickerListView, ScrollMode.Enabled);

            var layoutRoot = new Grid();
            layoutRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // search
            layoutRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // list

            layoutRoot.Children.Add(searchBox);
            Grid.SetRow(pickerListView, 1);
            layoutRoot.Children.Add(pickerListView);


            var dialog = new ContentDialog
            {
                Title = "Select installed UWP apps",
                Content = layoutRoot,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var selected = pickerListView.SelectedItems.Cast<AppxPackage>().ToList();
                foreach (var app in selected)
                {
                    // Prevent duplicates in your existing ListView backing collection
                    if (!SelectedApps.Any(a => a.FullName == app.FullName))
                    {
                        SelectedApps.Add(app);
                    }
                }

                // Add selected Apps to the AllowedApps or SingleApp based on mode
                
                    foreach (var app in SelectedApps)
                    {
                        var newApp = new StartMenuPin
                        {
                            Path = app.FullName,
                            ProfileId = 1, // Default to first profile, user can change later
                        };

                        if (profileIdList != null)
                            newApp.ProfileListIds = profileIdList;

                        _vm.Policy.StartMenuPins.Add(newApp);
                    }

                    PinsList.ItemsSource = null;
                    PinsList.ItemsSource = _vm.Policy.StartMenuPins;

            }

        }

        private async void AddFromExplorer_Click(object sender, RoutedEventArgs e)
        {

            if (App.MainWindow == null) return;

            var path = await FilePickerService.PickLnkAsync(App.MainWindow);

            if (path != null)
            {
                
                    var newApp = new StartMenuPin
                    {
                        Path = path,
                        ProfileId = 1
                    };

                    if (profileIdList != null)
                        newApp.ProfileListIds = profileIdList;

                    _vm.Policy.StartMenuPins.Add(newApp);

                    PinsList.ItemsSource = null;
                    PinsList.ItemsSource = _vm.Policy.StartMenuPins;

            }
        }
    }
}