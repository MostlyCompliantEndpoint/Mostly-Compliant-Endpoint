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
    public sealed partial class WizardStep4TaskbarPinsPage : Page
    {
        private PolicyWizardViewModel _vm = new();
        public List<int>? profileIdList { get; private set; }

        public static Window? MainWindow { get; private set; }

        public WizardStep4TaskbarPinsPage()
        {
            this.InitializeComponent();


            MainWindow = App.MainWindow;

            if (MainWindow != null)
            {
                MainWindow.SizeChanged += OnWindowSizeChanged;

                double height = MainWindow.Bounds.Height;
                double desiredHeight = Math.Max(100, height * 0.62);
                double desiredWidth = Math.Max(100, MainWindow.Bounds.Width * 0.8);

                if (desiredHeight > 0)
                {
                    tbPinsList.Height = desiredHeight;
                    tbPinsList.MaxHeight = desiredHeight;
                }

                if (desiredWidth > 0)
                {
                    ScrollViewerCB.MaxWidth = desiredWidth;
                    tbPinsList.Width = desiredWidth;
                    tbPinsList.MaxWidth = desiredWidth;
                }
            }
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is PolicyWizardViewModel pvm) _vm = pvm;

            _vm.Policy.TaskbarEnabled = false;
            ;
            if (_vm.Policy.Profiles.Count > 1)
            {

                if (_vm.Policy.Profiles.Count != _vm.Policy.TaskbarEnabledList.Count)
                {

                    List<int> ProfileIndex = new List<int> { };
                    List<int> TaskbarIndex = new List<int> { };

                    foreach (var taskbar in _vm.Policy.TaskbarEnabledList)
                    {
                        TaskbarIndex.Add(taskbar.ProfileId);
                    }

                    foreach (var profile in _vm.Policy.Profiles)
                    {
                        ProfileIndex.Add(profile.ProfileId);

                        if (!TaskbarIndex.Contains(profile.ProfileId))
                        {
                            _vm.Policy.TaskbarEnabledList.Add(new TaskbarEnabled
                            {
                                ProfileId = profile.ProfileId,
                                IsTaskbarEnabled = false,
                            });
                        }
                    }

                    var index = 0;
                    foreach (var taskbar in _vm.Policy.TaskbarEnabledList)
                    {
                        if (!ProfileIndex.Contains(taskbar.ProfileId))
                        {
                            _vm.Policy.TaskbarEnabledList.RemoveAt(index);
                            break;
                        }
                        else if (taskbar.IsTaskbarEnabled == true)
                        {
                            _vm.Policy.TaskbarEnabled = true;
                        }
                        index++;
                    }
                }
                else
                {
                    List<int> ProfileIndex = new List<int> { };
                    // Sync TaskbarEnabledList with Profiles
                    foreach (var profile in _vm.Policy.Profiles)
                    {
                        bool exists = false;
                        int removalIndex = 0;
                        ProfileIndex.Add(profile.ProfileId);

                        foreach (var taskbar in _vm.Policy.TaskbarEnabledList)
                        {
                            if (taskbar.IsTaskbarEnabled == true)
                            {
                                _vm.Policy.TaskbarEnabled = true;
                            }

                            if (taskbar.ProfileId == profile.ProfileId)
                            {
                                exists = true;
                                break;
                            }
                            else if (!(ProfileIndex.Contains(taskbar.ProfileId)))
                            {
                                _vm.Policy.TaskbarEnabledList.RemoveAt(removalIndex);
                            }
                            removalIndex++;
                        }
                        if (!exists)
                        {
                            _vm.Policy.TaskbarEnabledList.Add(new TaskbarEnabled
                            {
                                ProfileId = profile.ProfileId,
                                IsTaskbarEnabled = false,
                            });
                        }
                    }
                }

                TaskbarEnabledListCheckboxes.ItemsSource = null;
                TaskbarEnabledListCheckboxes.ItemsSource = _vm.Policy.TaskbarEnabledList;

            }
            else
            {
                if (_vm.Policy.TaskbarEnabledList.Count == 0 && _vm.Policy.Profiles.Count == 1)
                {
                    _vm.Policy.TaskbarEnabledList.Add(new TaskbarEnabled
                    {
                        ProfileId = _vm.Policy.Profiles[0].ProfileId,
                        IsTaskbarEnabled = _vm.Policy.TaskbarEnabled,
                    });
                }

                TaskbarEnabledListCheckboxes.ItemsSource = null;
                TaskbarEnabledListCheckboxes.ItemsSource = _vm.Policy.TaskbarEnabledList;
            }


            if (_vm.Policy.TaskbarEnabled == true)
            {
                tbPinsList.Visibility = Visibility.Visible;
                AddRemoveButtons.Visibility = Visibility.Visible;
            }

            // Populat combo box
            if (_vm.Policy.Profiles.Count > 0)
            {
                var proList = new List<int> { };
                foreach (var profile in _vm.Policy.Profiles)
                {
                    proList.Add(profile.ProfileId);

                }

                List<TaskbarPin> removePins = new List<TaskbarPin> { };
                foreach (var pin in _vm.Policy.TaskbarPins)
                {
                    pin.ProfileListIds = proList;
                    if (!proList.Contains(pin.ProfileId))
                    {
                        removePins.Add(pin);
                    }
                }

                foreach (var remPin in removePins)
                {
                    _vm.Policy.TaskbarPins.Remove(remPin);
                }

                profileIdList = proList;
            }

            tbPinsList.ItemsSource = null;
            tbPinsList.ItemsSource = _vm.Policy.TaskbarPins;

            base.OnNavigatedTo(e);
        }

        private void Add_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _vm.Policy.TaskbarPins.Add(new TaskbarPin
            {
                ProfileListIds = profileIdList == null ? new List<int>() : profileIdList,
            });
            tbPinsList.ItemsSource = null; tbPinsList.ItemsSource = _vm.Policy.TaskbarPins;
        }

        private void Remove_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (tbPinsList.SelectedItem is TaskbarPin pin)
            {
                _vm.Policy.TaskbarPins.Remove(pin);
                tbPinsList.ItemsSource = null; tbPinsList.ItemsSource = _vm.Policy.TaskbarPins;
            }
        }

        private void Taskbar_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;

            if (cb == null)
                return;

            _vm.Policy.TaskbarEnabled = false;

            foreach (var cbox in _vm.Policy.TaskbarEnabledList)
            {
                if (cbox.IsTaskbarEnabled)
                {
                    if ((cbox.ProfileId.ToString() == cb.Content.ToString()) && (cb.IsChecked != true))
                    {
                        _vm.Policy.TaskbarEnabled = false;
                    }
                    else
                    {
                        _vm.Policy.TaskbarEnabled = true;
                        break;
                    }

                }
            }

            if (cb.IsChecked == true)
                _vm.Policy.TaskbarEnabled = true;

            if (_vm.Policy.TaskbarEnabled)
            {
                _vm.Policy.TaskbarEnabled = true;
                tbPinsList.Visibility = Visibility.Visible;
                AddRemoveButtons.Visibility = Visibility.Visible;
            }
            else
            {
                _vm.Policy.TaskbarEnabled = false;
                tbPinsList.Visibility = Visibility.Collapsed;
                AddRemoveButtons.Visibility = Visibility.Collapsed;
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            double width = e.Size.Width;
            double height = e.Size.Height;


            double desiredHeight = Math.Max(100, height * 0.62);
            double desiredWidth = Math.Max(100, width * 0.8);

            if (desiredHeight > 0)
            {
                tbPinsList.Height = desiredHeight;
                tbPinsList.MaxHeight = desiredHeight;
            }

            if (desiredWidth > 0)
            {
                ScrollViewerCB.MaxWidth = desiredWidth;
                tbPinsList.Width = desiredWidth;
                tbPinsList.MaxWidth = desiredWidth;
            }
        }

        private void Path_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null)
                return;
            tb.Text = Helpers.FileHelper.CleanPath(tb.Text);
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
                    var newApp = new TaskbarPin
                    {
                        Path = app.FullName,
                        ProfileId = 1, // Default to first profile, user can change later
                    };

                    if (profileIdList != null)
                        newApp.ProfileListIds = profileIdList;

                    _vm.Policy.TaskbarPins.Add(newApp);
                }

                tbPinsList.ItemsSource = null;
                tbPinsList.ItemsSource = _vm.Policy.TaskbarPins;

            }
        }

        private async void AddFromExplorer_Click(object sender, RoutedEventArgs e)
        {

            if (App.MainWindow == null) return;

            var path = await FilePickerService.PickLnkAsync(App.MainWindow);

            if (path != null)
            {

                var newApp = new TaskbarPin
                {
                    Path = path,
                    ProfileId = 1
                };

                if (profileIdList != null)
                    newApp.ProfileListIds = profileIdList;

                _vm.Policy.TaskbarPins.Add(newApp);

                tbPinsList.ItemsSource = null;
                tbPinsList.ItemsSource = _vm.Policy.TaskbarPins;

            }
        }
    }
}