using AssignedAccessDesigner.Helpers;
using AssignedAccessDesigner.Services;
using AssignedAccessDesigner.ViewModels;
using AssignedAccessDesigner.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Windows.Graphics;
using WinRT.Interop;


namespace AssignedAccessDesigner
{
    public sealed partial class MainWindow : Window, INavigationBarController
    {
        private readonly NavigationStore _navStore = new();
        private readonly PolicyWizardViewModel _policyVm = new();
        private readonly MergeWizardViewModel _mergeVm = new();

        public MainWindow()
        {
            InitializeComponent();


            // Set your icon at runtime (change the path as needed)
            IconHelper.ApplyIcon(this, "Assets\\AssignedAccessDesigner-256.ico");

            // Wire up NavigationView events
            NavView.SelectionChanged += NavView_SelectionChanged;

            // Handle Frame navigation events
            ContentFrame.Navigated += NavViewFrame_Navigated;

            // Listen for validation state changes
            _policyVm.PropertyChanged += PolicyVm_PropertyChanged;


            // Set initial navigation
            NavigateTo("Welcome");

            // Set Window State
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            //appWindow.Resize(new Windows.Graphics.SizeInt32(1920, 1080));
            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            var overlapped = (OverlappedPresenter)appWindow.Presenter;

        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {

            var tag = (args.SelectedItem as NavigationViewItem)?.Tag?.ToString();
            if (!string.IsNullOrEmpty(tag))
            {
                // Verify NavStore current step is updated to reflect the actual step we are on. If the user clicks on the navigation menu directly, we need to update the step accordingly.
                switch(tag)
                {
                    case "Step1":
                        _navStore.CurrentStep = 1;
                        break;
                    case "Step2":
                        _navStore.CurrentStep = 2;
                        break;
                    case "Step3":
                        _navStore.CurrentStep = 3;
                        break;
                    case "Step4":
                        _navStore.CurrentStep = 4;
                        break;
                    case "Step5":
                        _navStore.CurrentStep = 5;
                        break;
                    case "Step6":
                        _navStore.CurrentStep = 6;
                        break;
                    case "Step7":
                        _navStore.CurrentStep = 7;
                        break;
                    default:
                        _navStore.CurrentStep = 1;
                        break;
                }

                NavigateTo(tag);
            }
        }


        private void NavViewFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            // Find the matching NavViewItem by Tag == page type name
            var pageKey = e.SourcePageType.Name;
            var tag = "";

            switch (pageKey)
            {
                case "WelcomePage":
                    tag = "Welcome";
                    break;
                case "SettingsPage":
                    tag = "Settings";
                    break;
                case "WizardStep1ModePage":
                    tag = "Step1";
                    break;
                case "WizardStep2AppsPage":
                    tag = "Step2";
                    break;
                case "WizardStep3PinsPage":
                    tag = "Step3";
                    break;
                case "WizardStep4TaskbarPinsPage":
                    tag = "Step4";
                    break;
                case "WizardStep5RestrictionsPage":
                    tag = "Step5";
                    break;
                case "WizardStep6AccountsPage":
                    tag = "Step6";
                    break;
                case "WizardStep7SavePage":
                    tag = "Step7";
                    break;
                case "MergeSelectPoliciesPage":
                    tag = "MergeSelect";
                    break;
                case "HelpWindow":
                    tag = "Help";
                    break;
                default:
                    tag = "Welcome";
                    break;
            }

            foreach (var item in NavView.MenuItems)
            {
                if (item is NavigationViewItem navItem && (string)navItem.Tag == tag)
                {
                    var match = navItem;
                    NavView.SelectedItem = match;          // This updates the visual selection
                    NavView.IsPaneOpen = NavView.IsPaneOpen; // force layout refresh if you’ve seen stale visuals
                    return;
                }
            }
        }



        public void ConfigureNavigationBar(
            bool showBar,
            bool showPrevious,
            bool showNext
            )
        {
            NavigationStack.Visibility = showBar ? Visibility.Visible : Visibility.Collapsed;
            PrevButton.Visibility = showPrevious ? Visibility.Visible : Visibility.Collapsed;
            NextButton.Visibility = showNext ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ConfigureNavigationButtons(bool enablePrevious, bool enableNext)
        {
            NextButton.IsEnabled = enableNext;
            PrevButton.IsEnabled = enablePrevious;
        }

        public void NavigateTo(string tag)
        {
            // Default: disable Next/Prev on non-wizard pages
            ConfigureNavigationButtons(false, false);
            var candidates = NavView.MenuItems.OfType<NavigationViewItem>();
            var match = candidates.FirstOrDefault(i => (string)i.Tag == tag);

            switch (tag)
            {
                case "Welcome":
                    ContentFrame.Navigate(typeof(Views.WelcomePage), _policyVm);
                    ConfigureNavigationBar(false, false, false);
                    NavView.SelectedItem = match;
                    break;
                case "Step1":
                    ContentFrame.Navigate(typeof(Views.WizardStep1ModePage), _policyVm);
                    ConfigureNavigationBar(true, false, true);
                    EnableWizardNav(1);
                    NavView.SelectedItem = match;
                    break;
                case "Step2":
                    ContentFrame.Navigate(typeof(Views.WizardStep2AppsPage), _policyVm);
                    ConfigureNavigationBar(true, true, true);
                    EnableWizardNav(2);
                    NavView.SelectedItem = match;
                    break;
                case "Step3":
                    ContentFrame.Navigate(typeof(Views.WizardStep3PinsPage), _policyVm);
                    ConfigureNavigationBar(true, true, true);
                    EnableWizardNav(3);
                    NavView.SelectedItem = match;
                    break;
                case "Step4":
                    ContentFrame.Navigate(typeof(Views.WizardStep4TaskbarPinsPage), _policyVm);
                    ConfigureNavigationBar(true, true, true);
                    EnableWizardNav(4);
                    NavView.SelectedItem = match;
                    break;
                case "Step5":
                    ContentFrame.Navigate(typeof(Views.WizardStep5RestrictionsPage), _policyVm);
                    ConfigureNavigationBar(true, true, true);
                    EnableWizardNav(5);
                    NavView.SelectedItem = match;
                    break;
                case "Step6":
                    ContentFrame.Navigate(typeof(Views.WizardStep6AccountsPage), _policyVm);
                    ConfigureNavigationBar(true, true, true);
                    EnableWizardNav(6);
                    NavView.SelectedItem = match;
                    break;
                case "Step7":
                    ContentFrame.Navigate(typeof(Views.WizardStep7SavePage), _policyVm);
                    ConfigureNavigationBar(true, true, false);
                    EnableWizardNav(7);
                    NavView.SelectedItem = match;
                    break;
                case "MergeSelect":
                    ContentFrame.Navigate(typeof(Views.MergeSelectPoliciesPage), _mergeVm);
                    PrevButton.IsEnabled = false; NextButton.IsEnabled = true;
                    NavView.SelectedItem = match;
                    break;
                case "Help":
                    ContentFrame.Navigate(typeof(Views.HelpWindow));
                    //ConfigureNavigationBar(false, false, false);
                    //NavView.SelectedItem = match;
                    break;
                default:
                    ContentFrame.Navigate(typeof(Views.WelcomePage), _policyVm);
                    ConfigureNavigationBar(false, false, false);
                    break;
            }
        }

        public void EnableWizardNav(int step)
        {
            PrevButton.IsEnabled = step > 1;

            // Update Next button based on validation state
            NextButton.IsEnabled = step < 8;
            GlobalValidationInfoBar.IsOpen = !_policyVm.IsValid;
            GlobalValidationInfoBar.Message = _policyVm.ValidationMessage;

        }


        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            HelpInfo helpInfo = new HelpInfo();

            // Determine the active view hosted in the frame.
            var activeView = ContentFrame?.Content;


            // Fallback: show generic help with the view’s type name
            var pageName = activeView?.GetType().Name;
            var sectionTitle = $"About {pageName}";
            var description = "No contextual help defined for this view.";

            switch (pageName)
            {
                case "WelcomePage":
                    pageName = "Welcome";
                    sectionTitle = "Welcome to Assigned Access Designer";
                    description = helpInfo.AboutHelpDescription;
                    break;
                case "WizardStep1ModePage":
                    pageName = "Kiosk Mode and Profiles";
                    sectionTitle = "Kiosk Mode and Profiles";
                    description = helpInfo.ModeHelpDescription;
                    break;
                case "WizardStep2AppsPage":
                    pageName = "Applications";
                    sectionTitle = "Configuring Applications";
                    description = helpInfo.AppsHelpDescription;
                    break;
                case "WizardStep3PinsPage":
                    pageName = "Start Menu Pins";
                    sectionTitle = "Start Menu";
                    description = helpInfo.StartHelpDescription;
                    break;
                case "WizardStep4TaskbarPinsPage":
                    pageName = "Taskbar Pins";
                    sectionTitle = "Taskbar";
                    description = helpInfo.TaskHelpDescription;
                    break;
                case "WizardStep5RestrictionsPage":
                    pageName = "Device Restrictions";
                    sectionTitle = "Device Restrictions";
                    description = helpInfo.RestrictionsHelpDescription;
                    break;
                case "WizardStep6AccountsPage":
                    pageName = "Login Accounts / Groups";
                    sectionTitle = "Accounts & Groups";
                    description = helpInfo.AccountsHelpDescription;
                    break;
                case "WizardStep7SavePage":
                    pageName ="Deploying XML Files";
                    sectionTitle = "Deploying XML Files Using Microsoft Intune";
                    description = helpInfo.SaveHelpDescription;
                    break;
                case "SettingsPage":
                    pageName = "About Assigned Access Designer";
                    sectionTitle = "About Assigned Access Designer";
                    description = "Assigned Access Designer is a tool to help you create and manage Assigned Access (Kiosk) policies for Windows devices.";
                    break;
                default:
                    pageName = "Assigned Access Designer";
                    sectionTitle = "Help";
                    description = helpInfo.AboutHelpDescription;
                    break;
            }

            var help = new HelpWindow();
            help.Initialize(pageName, sectionTitle, description);
            help.CenterOnScreen(); // optional
            help.Activate();       // NON-MODAL
        }


        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_navStore.CurrentStep > 1)
            {
                if ((_policyVm.Policy.Mode == Models.KioskMode.SingleApp) && _navStore.CurrentStep == 6)
                {
                    _navStore.CurrentStep = 2;
                    NavigateTo($"Step{_navStore.CurrentStep}");
                }
                else
                {
                    _navStore.CurrentStep--;
                    NavigateTo($"Step{_navStore.CurrentStep}");
                }

            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_policyVm.Policy.PoliciesToMerge.Count > 0 && _policyVm.Policy.Profiles.Count == 0)
            {
                MergePolicies();
                _policyVm.Validate(10);
                _navStore.CurrentStep = 0;
            }

            if(_policyVm.Policy.PoliciesToMerge.Count == 0 && _policyVm.Policy.Profiles.Count == 0)
            {
                _navStore.CurrentStep = 0;
            }

            if (_navStore.CurrentStep < 7 && _navStore.CurrentStep > 0)
            {
                // Validate
                _policyVm.Validate(_navStore.CurrentStep);

                if (_policyVm.IsValid)
                {
                    if ((_policyVm.Policy.Mode == Models.KioskMode.SingleApp) && _navStore.CurrentStep == 2)
                    {
                        _navStore.CurrentStep = 6;
                        NavigateTo($"Step{_navStore.CurrentStep}");
                    }
                    else
                    {
                        _navStore.CurrentStep++;
                        NavigateTo($"Step{_navStore.CurrentStep}");
                    }

                }
                else
                {
                    NextButton.IsEnabled = true;
                }

            }
            else
            {
                _navStore.CurrentStep++;
                NavigateTo($"Step{_navStore.CurrentStep}");
            }

        }

        private void PolicyVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PolicyWizardViewModel.IsValid))
            {
                NextButton.IsEnabled = _policyVm.IsValid && _navStore.CurrentStep < 8;
                GlobalValidationInfoBar.IsOpen = !_policyVm.IsValid;
            }
            else if (e.PropertyName == nameof(PolicyWizardViewModel.ValidationMessage))
            {
                GlobalValidationInfoBar.Message = _policyVm.ValidationMessage;
            }
        }

        public void UpdatePolicyVm(PolicyWizardViewModel pvm, int step)
        {
            // Used when merging policies to update the main policy VM
            _policyVm.Policy = pvm.Policy;
            _policyVm.Validate(step);
        }

        private void MergePolicies()
        {
            foreach (var pol in _policyVm.Policy.PoliciesToMerge)
            {
                AssignedAccessPolicyBuilder.AddXmlToUi(pol.policy, _policyVm);
            }

           NavigateTo("Step1");
        }
    }
}