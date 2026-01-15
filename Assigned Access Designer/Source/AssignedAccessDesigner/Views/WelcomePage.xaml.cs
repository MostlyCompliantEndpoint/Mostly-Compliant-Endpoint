using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.Services;
using AssignedAccessDesigner.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace AssignedAccessDesigner.Views
{
    public sealed partial class WelcomePage : Page
    {
        private PolicyWizardViewModel _vm = new();
        public static Window? MainWindow { get; private set; }

        public WelcomePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is PolicyWizardViewModel pvm) _vm = pvm;
            base.OnNavigatedTo(e);
        }

        private void CreateBtn_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WizardStep1ModePage), _vm);
            ShowNavigationBar();
            SetNavigationButtonEnable(false, true);
        }

        private async void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if(App.MainWindow == null) return;

            var path = await FilePickerService.PickXmlAsync(App.MainWindow);
            if (path != null)
            {
                _vm.Policy = new AssignedAccessPolicy();
                var doc = XDocument.Load(path);
                AssignedAccessPolicyBuilder.AddXmlToUi(doc, _vm);
                _vm.IsEdit = true;

                Frame.Navigate(typeof(WizardStep1ModePage), _vm);
                ShowNavigationBar();
                SetNavigationButtonEnable(false, true);
            }
        }

        private void MergeBtn_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MergeSelectPoliciesPage), null);
            ShowNavigationBar();
            SetNavigationButtonEnable(false, true);

        }

        private void ShowNavigationBar()
        {
            var navController = App.NavController;

            if(navController == null) return;

            navController.ConfigureNavigationBar(true,false,true);
        }

        private void SetNavigationButtonEnable(bool enablePrevious, bool enableNext)
        {
            var navController = App.NavController;

            if(navController == null) return;

            navController.ConfigureNavigationButtons(enablePrevious, enableNext);
        }

    }
}