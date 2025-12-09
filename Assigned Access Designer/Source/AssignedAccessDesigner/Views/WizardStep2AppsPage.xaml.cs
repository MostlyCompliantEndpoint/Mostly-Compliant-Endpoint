using AssignedAccessDesigner;
using AssignedAccessDesigner.Helpers;
using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.Services;
using AssignedAccessDesigner.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;

namespace AssignedAccessDesigner.Views
{
    public sealed partial class WizardStep2AppsPage : Page
    {
        private PolicyWizardViewModel _vm = new();
        public static Window? MainWindow { get; private set; }

        public List<int>? profileIdList { get; private set; }

        public WizardStep2AppsPage()
        {
            InitializeComponent();

            MainWindow = App.MainWindow;

            if (MainWindow != null)
            {
                MainWindow.SizeChanged += OnWindowSizeChanged;

                double height = MainWindow.Bounds.Height;
                double width = MainWindow.Bounds.Width;
                double desiredHeight = Math.Max(100, height * 0.73); // your rule
                double desiredWidth = Math.Max(100, width * 0.8);

                if (desiredHeight > 0)
                {
                    AppsList.Height = desiredHeight;
                    AppsList.MaxHeight = desiredHeight;
                    AppsList.Width = desiredWidth;
                    AppsList.MaxWidth = desiredWidth;
                    SingleAppsList.Height = desiredHeight;
                    SingleAppsList.MaxHeight = desiredHeight;
                    SingleAppsList.Width = desiredWidth;
                    SingleAppsList.MaxWidth = desiredWidth;
                }
            }
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is PolicyWizardViewModel pvm) _vm = pvm;

            // Check for Policy type (single vs multi), if nothing is added yet add one item for Single and disable the add/remove button
            if (_vm.Policy.Mode == KioskMode.SingleApp)
            {
                AddButton.Visibility = Visibility.Collapsed;
                RemoveButton.Visibility = Visibility.Collapsed;

                SingleAppsList.Visibility = Visibility.Visible;
            }
            else
            {
                AppsList.Visibility = Visibility.Visible;
            }

            // Create Single Apps for Profiles if not present already
            if (_vm.Policy.Profiles.Count != _vm.Policy.SingleApp.Count && _vm.Policy.Mode == KioskMode.SingleApp)
            {
                foreach (var profile in _vm.Policy.Profiles)
                {
                    bool found = false;
                    foreach (var app in _vm.Policy.SingleApp)
                    {
                        if (app.ProfileId == profile.ProfileId)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var newApp = new SingleApp();
                        newApp.ProfileId = profile.ProfileId;
                        _vm.Policy.SingleApp.Add(newApp);
                    }
                }
            }

            // Populat combo box
            if (_vm.Policy.Profiles.Count > 0)
            {
                var proList = new List<int> { };
                foreach (var profile in _vm.Policy.Profiles)
                {
                    proList.Add(profile.ProfileId);

                }


                if (_vm.Policy.Mode == KioskMode.SingleApp)
                {
                    List<SingleApp> removeApps = new List<SingleApp> { };
                    foreach (var app in _vm.Policy.SingleApp)
                    {
                        app.ProfileListIds = proList;
                        if (!proList.Contains(app.ProfileId))
                        {
                            removeApps.Add(app);
                        }
                    }

                    foreach (var app in removeApps)
                    {
                        _vm.Policy.SingleApp.Remove(app);
                    }

                    SingleAppsList.ItemsSource = null;
                    SingleAppsList.ItemsSource = _vm.Policy.SingleApp;
                }
                else
                {
                    List<AllowedApp> removeApps = new List<AllowedApp> { };
                    foreach (var app in _vm.Policy.AllowedApps)
                    {
                        app.ProfileListIds = proList;
                        if (!proList.Contains(app.ProfileId))
                        {
                            removeApps.Add(app);
                        }
                    }

                    foreach (var app in removeApps)
                    {
                        _vm.Policy.AllowedApps.Remove(app);
                    }

                    AppsList.ItemsSource = null;
                    AppsList.ItemsSource = _vm.Policy.AllowedApps;
                }

                profileIdList = proList;
            }

            base.OnNavigatedTo(e);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.Policy.Mode == KioskMode.SingleApp)
            {
                var newApp = new SingleApp();

                if (profileIdList != null)
                    newApp.ProfileListIds = profileIdList;

                _vm.Policy.SingleApp.Add(newApp);
                SingleAppsList.ItemsSource = null;
                SingleAppsList.ItemsSource = _vm.Policy.SingleApp;
            }
            else
            {
                var newApp = new AllowedApp();

                if (profileIdList != null)
                    newApp.ProfileListIds = profileIdList;

                _vm.Policy.AllowedApps.Add(newApp);
                AppsList.ItemsSource = null;
                AppsList.ItemsSource = _vm.Policy.AllowedApps;
            }


        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {

            if (_vm.Policy.Mode == KioskMode.SingleApp)
            {
                if (SingleAppsList.SelectedItem is SingleApp app)
                {
                    _vm.Policy.SingleApp.Remove(app);
                    SingleAppsList.ItemsSource = null;
                    SingleAppsList.ItemsSource = _vm.Policy.SingleApp;
                }
            }
            else
            {
                if (AppsList.SelectedItem is AllowedApp app)
                {
                    _vm.Policy.AllowedApps.Remove(app);
                    AppsList.ItemsSource = null;
                    AppsList.ItemsSource = _vm.Policy.AllowedApps;
                }
            }


        }

        private void autoLaunchFocus(object sender, RoutedEventArgs e)
        {
            foreach (var app in _vm.Policy.AllowedApps)
            {
                if (app.AutoLaunch)
                {
                    _vm.Policy.namespaces.rs5 = true;
                }
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            double width = e.Size.Width;
            double height = e.Size.Height;


            double desiredHeight = Math.Max(100, height * 0.73); // your rule
            double desiredWidth = Math.Max(100, width * 0.8);

            if (desiredHeight > 0)
            {
                AppsList.Height = desiredHeight;
                AppsList.MaxHeight = desiredHeight;
                AppsList.Width = desiredWidth;
                AppsList.MaxWidth = desiredWidth;
                SingleAppsList.Height = desiredHeight;
                SingleAppsList.MaxHeight = desiredHeight;
                SingleAppsList.Width = desiredWidth;
                SingleAppsList.MaxWidth = desiredWidth;

            }
        }

        private void Path_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                tb.Text = FileHelper.CleanPath(tb.Text);
            }
        }

        private void SinglePath_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                tb.Text = FileHelper.CleanPath(tb.Text);
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
                if (_vm.Policy.Mode == KioskMode.SingleApp)
                {
                    int defaultProfile = 1;
                    var currentSelection = SingleAppsList.SelectedItem as SingleApp;

                    if (currentSelection != null)
                    {
                        defaultProfile = currentSelection.ProfileId;
                    }

                    foreach (var app in _vm.Policy.SingleApp)
                    {
                        if (app.ProfileId == defaultProfile)
                            app.AppPath = SelectedApps.First().FullName;
                    }

                    SingleAppsList.ItemsSource = null;
                    SingleAppsList.ItemsSource = _vm.Policy.SingleApp;
                }
                else
                {
                    foreach (var app in SelectedApps)
                    {
                        var newApp = new AllowedApp
                        {
                            AppPath = app.FullName,
                            ProfileId = 1, // Default to first profile, user can change later
                        };

                        if (profileIdList != null)
                            newApp.ProfileListIds = profileIdList;

                        _vm.Policy.AllowedApps.Add(newApp);
                    }

                    AppsList.ItemsSource = null;
                    AppsList.ItemsSource = _vm.Policy.AllowedApps;
                }
            }

        }

        private async void AddFromExplorer_Click(object sender, RoutedEventArgs e)
        {

            if (App.MainWindow == null) return;

            var path = await FilePickerService.PickExeAsync(App.MainWindow);

            if (path != null)
            {
                if (_vm.Policy.Mode == KioskMode.SingleApp)
                {
                    int defaultProfile = 1;
                    var currentSelection = SingleAppsList.SelectedItem as SingleApp;

                    if (currentSelection != null)
                    {
                        defaultProfile = currentSelection.ProfileId;
                    }

                    foreach (var app in _vm.Policy.SingleApp)
                    {
                        if (app.ProfileId == defaultProfile)
                            app.AppPath = path;
                    }

                    SingleAppsList.ItemsSource = null;
                    SingleAppsList.ItemsSource = _vm.Policy.SingleApp;

                }
                else
                {
                    var newApp = new AllowedApp
                    {
                        AppPath = path,
                        ProfileId = 1
                    };

                    if (profileIdList != null)
                        newApp.ProfileListIds = profileIdList;

                    _vm.Policy.AllowedApps.Add(newApp);

                    AppsList.ItemsSource = null;
                    AppsList.ItemsSource = _vm.Policy.AllowedApps;
                }
            }
        }
    }
}