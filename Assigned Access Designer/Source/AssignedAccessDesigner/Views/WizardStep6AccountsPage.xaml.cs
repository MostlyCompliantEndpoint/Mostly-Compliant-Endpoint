using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace AssignedAccessDesigner.Views
{
    public sealed partial class WizardStep6AccountsPage : Page
    {
        private PolicyWizardViewModel _vm = new();
        public List<int>? profileIdList { get; private set; }

        public static Window? MainWindow { get; private set; }
        public WizardStep6AccountsPage() { 

            InitializeComponent();

            MainWindow = App.MainWindow;

            if (MainWindow != null)
            {
                MainWindow.SizeChanged += OnWindowSizeChanged;

                double height = MainWindow.Bounds.Height;
                double desiredHeight = Math.Max(100, height * 0.68);

                double width = MainWindow.Bounds.Width;
                double desiredWidth = Math.Max(100, width * 0.8);

                if (desiredHeight > 0)
                {
                    AccountsList.Height = desiredHeight;
                    AccountsList.MaxHeight = desiredHeight;
                }
                if(desiredWidth > 0)
                {
                    AccountsList.Width = desiredWidth;
                    AccountsList.MaxWidth = desiredWidth;
                }
            }
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is PolicyWizardViewModel pvm) _vm = pvm;
            AccountsList.ItemsSource = _vm.Policy.LogonAccounts;

            // Populat combo box
            if (_vm.Policy.Profiles.Count > 0)
            {
                var proList = new List<int> { };
                foreach (var profile in _vm.Policy.Profiles)
                {
                    proList.Add(profile.ProfileId);

                }

                List<LogonAccount> removeAccs = new List<LogonAccount> { };
                foreach (var acc in _vm.Policy.LogonAccounts)
                {
                    acc.ProfileListIds = proList;
                    if (!proList.Contains(acc.ProfileId))
                    {
                        removeAccs.Add(acc);
                    }
                }

                foreach (var remacc in removeAccs)
                {
                    _vm.Policy.LogonAccounts.Remove(remacc);
                }

                profileIdList = proList;
           
            }

            AccountsList.ItemsSource = null;
            AccountsList.ItemsSource = _vm.Policy.LogonAccounts;

            base.OnNavigatedTo(e);
        }

        private void Add_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _vm.Policy.LogonAccounts.Add(new LogonAccount { 
                AccountName = "", 
                ProfileListIds = profileIdList == null ? new List<int>() : profileIdList, 
                Location = "Local" });
            AccountsList.ItemsSource = null; AccountsList.ItemsSource = _vm.Policy.LogonAccounts;
        }

        private void Remove_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (AccountsList.SelectedItem is LogonAccount acc)
            {
                _vm.Policy.LogonAccounts.Remove(acc);
                AccountsList.ItemsSource = null; AccountsList.ItemsSource = _vm.Policy.LogonAccounts;
            }
        }

        private void typeChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var type in _vm.Policy.LogonAccounts)
            {
                if (type.Type == "AutoLogon")
                {
                    _vm.Policy.namespaces.rs5 = true;
                }
                else if (type.Type == "Global")
                {
                    _vm.Policy.namespaces.v3 = true;
                }
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            double width = e.Size.Width;
            double height = e.Size.Height;


            double desiredHeight = Math.Max(100, height * 0.68);
            double desiredWidth = Math.Max(100, width * 0.8);

            if (desiredHeight > 0)
            {
                AccountsList.Height = desiredHeight;
                AccountsList.MaxHeight = desiredHeight;
            }
            if (desiredWidth > 0)
            {
                AccountsList.Width = desiredWidth;
                AccountsList.MaxWidth = desiredWidth;
            }
        }
    }
}