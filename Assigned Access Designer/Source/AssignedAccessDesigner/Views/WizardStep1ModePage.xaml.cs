using AssignedAccessDesigner.Helpers;
using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT;

namespace AssignedAccessDesigner.Views
{
    public sealed partial class WizardStep1ModePage : Page
    {
        private PolicyWizardViewModel _vm = new();
        private SingleApp singleApp = new SingleApp();
        public static Window? MainWindow { get; private set; }
        private int numProfiles = 0;

        public WizardStep1ModePage()
        {
            this.InitializeComponent();

            MainWindow = App.MainWindow;

            if (MainWindow != null)
            {
                MainWindow.SizeChanged += OnWindowSizeChanged;

                double height = MainWindow.Bounds.Height;
                double width = MainWindow.Bounds.Width;
                double desiredHeight = Math.Max(100, height * 0.5); // your rule
                double desiredWidth = Math.Max(100, width * 0.8);

                if (desiredHeight > 0)
                {
                    ProfilesList.Height = desiredHeight;
                    ProfilesList.MaxHeight = desiredHeight;
                }
                if(desiredWidth > 0)
                {
                    ProfilesList.Width = desiredWidth;
                    ProfilesList.MaxWidth = desiredWidth;
                    //ProfileNameTextbox.MinWidth = (desiredWidth / 2);
                }
            }
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is PolicyWizardViewModel pvm) _vm = pvm;

            if (_vm.Policy.Mode == KioskMode.SingleApp)
            {
                SingleRadio.IsChecked = true;
            }
            else
            {
                MultiRadio.IsChecked = true;
            }

            // Intiialize 1 default policy if not present as this is needed for both single app and multi
            if (_vm.Policy.Profiles.Count == 0)
            {
                _vm.Policy.Profiles.Add(new Profile
                {
                    ProfileGuid = "{" + (Guid.NewGuid()).ToString() + "}",
                    ProfileId = 1
                });

                ProfilesList.ItemsSource = _vm.Policy.Profiles;
                RemoveButton.IsEnabled = false;
            }

            ProfilesList.ItemsSource = _vm.Policy.Profiles;

            base.OnNavigatedTo(e);
        }

        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            var tag = (sender as RadioButton)?.Tag?.ToString();
            _vm.Policy.Mode = tag == "MultiApp" ? KioskMode.MultiApp : KioskMode.SingleApp;
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var profileId = 1;

            foreach (var tempId in _vm.Policy.Profiles)
            {
                if (tempId.ProfileId >= profileId)
                    profileId = tempId.ProfileId + 1;
            }

            var guid = (Guid.NewGuid().ToString());

            var profile = new Profile {
                ProfileGuid = "{" + guid + "}",
                ProfileId = profileId,
                ProfileName = ""
            };

            _vm.Policy.Profiles.Add(profile);
            ProfilesList.ItemsSource = null;
            ProfilesList.ItemsSource = _vm.Policy.Profiles;

            if (_vm.Policy.Profiles.Count > 1)
            {
                RemoveButton.IsEnabled = true;
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (ProfilesList.SelectedItem is Profile profile)
            {
                _vm.Policy.Profiles.Remove(profile);
                 ProfilesList.ItemsSource = null;
                ProfilesList.ItemsSource = _vm.Policy.Profiles;
            }

            if (_vm.Policy.Profiles.Count == 1)
            {
                RemoveButton.IsEnabled = false;
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            double width = e.Size.Width;
            double height = e.Size.Height;


            double desiredHeight = Math.Max(100, height * 0.5); 
            double desiredWidth = Math.Max(100, width * 0.8);

            if (desiredHeight > 0)
            {
                ProfilesList.Height = desiredHeight;
                ProfilesList.MaxHeight = desiredHeight;
                ProfilesList.Width = desiredWidth;
                ProfilesList.MaxWidth = desiredWidth;
            }
        }
    }
}