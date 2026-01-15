using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace AssignedAccessDesigner.Views
{
    public sealed partial class WizardStep5RestrictionsPage : Page
    {
        private PolicyWizardViewModel _vm = new();
        public WizardStep5RestrictionsPage() { InitializeComponent(); }

        public static Window? MainWindow { get; private set; }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is PolicyWizardViewModel pvm) _vm = pvm;
            
            if (_vm.Policy.ExplorerRestrictions.Count < 1 && _vm.Policy.Profiles.Count > 0)
            {
                foreach (var profile in _vm.Policy.Profiles)
                {
                    _vm.Policy.ExplorerRestrictions.Add(new ExplorerRestriction
                    {
                        ProfileId = profile.ProfileId,
                        NoRestrictions = false,
                        AllowDownloads = false,
                        AllowRemovableMedia = false,
                        RestrictAll = false
                    });
                }

                _vm.Policy.namespaces.rs5 = true;

                RestrictList.ItemsSource = null; 
                RestrictList.ItemsSource = _vm.Policy.ExplorerRestrictions;
            }
            else if (_vm.Policy.ExplorerRestrictions.Count != _vm.Policy.Profiles.Count && _vm.Policy.Profiles.Count > 0)
            {
                List<int> existingProfileIds = new();
                List<int> existingRestrictionIds = new();

                // Ensure each profile has a corresponding restriction

                foreach (var restriction in _vm.Policy.ExplorerRestrictions)
                {
                    existingRestrictionIds.Add(restriction.ProfileId);
                }

                // Add any missing profile restrictions
                foreach (var profile in _vm.Policy.Profiles)
                {
                    existingProfileIds.Add(profile.ProfileId);

                    if (!(existingRestrictionIds.Contains(profile.ProfileId)))
                    {
                        _vm.Policy.ExplorerRestrictions.Add(new ExplorerRestriction
                        {
                            ProfileId = profile.ProfileId,
                            NoRestrictions = false,
                            AllowDownloads = false,
                            AllowRemovableMedia = false,
                            RestrictAll = false
                        });
                    }
                }

                List<ExplorerRestriction> restrictionRemovals = new(); 

                // Remove any deleted profile restrictions
                foreach (var restriction in _vm.Policy.ExplorerRestrictions)
                {
                    if (!(existingProfileIds.Contains(restriction.ProfileId)))
                    {
                       restrictionRemovals.Add(restriction);
                    }
                }

                if (restrictionRemovals.Count > 0)
                {
                    foreach (var removal in restrictionRemovals)
                    {
                        _vm.Policy.ExplorerRestrictions.Remove(removal);
                    }
                }

                RestrictList.ItemsSource = null; 
                RestrictList.ItemsSource = _vm.Policy.ExplorerRestrictions;  
            }
            else
            {
                _vm.Policy.namespaces.rs5 = true;
                RestrictList.ItemsSource = _vm.Policy.ExplorerRestrictions;
            }

            MainWindow = App.MainWindow;

            if (MainWindow != null)
            {
                MainWindow.SizeChanged += OnWindowSizeChanged;
                double height = MainWindow.Bounds.Height;
                double width = MainWindow.Bounds.Width;
                double desiredHeight = Math.Max(100, height * 0.72);
                double desiredWidth = Math.Max(100, width * 0.83);

                if (desiredHeight > 0)
                {
                    RestrictList.Width = desiredWidth;
                    ScrollViewerRestrict.Height = desiredHeight;
                   RestrictList.MaxWidth = desiredWidth;
                    ScrollViewerRestrict.MaxHeight = desiredHeight;
                }
            }

            base.OnNavigatedTo(e);
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            double width = e.Size.Width;
            double height = e.Size.Height;


            double desiredHeight = Math.Max(100, height * 0.72); 
            double desiredWidth = Math.Max(100, width * 0.83);

            if (desiredHeight > 0)
            {
                RestrictList.Width = desiredWidth;
                ScrollViewerRestrict.Height = desiredHeight;
                RestrictList.MaxWidth = desiredWidth;
                ScrollViewerRestrict.MaxHeight = desiredHeight;
            }
        }
    }
}