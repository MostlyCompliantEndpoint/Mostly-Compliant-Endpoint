using AssignedAccessDesigner.Services;
using AssignedAccessDesigner.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;

namespace AssignedAccessDesigner.Views
{
    public sealed partial class WizardStep7SavePage : Page
    {
        private bool _xmlVisible = false;
        private PolicyWizardViewModel _vm = new();
        public static Window? MainWindow { get; private set; }

        public WizardStep7SavePage() { 
            InitializeComponent();

            MainWindow = App.MainWindow;

            if (MainWindow != null)
            {
                MainWindow.SizeChanged += OnWindowSizeChanged;

                double height = MainWindow.Bounds.Height;
                double desiredHeight = Math.Max(100, height * 0.6);

                double width = MainWindow.Bounds.Width;
                double desiredWidth = Math.Max(100, width * 0.8);

                if (desiredHeight > 0)
                {
                    XmlPreviewBox.Height = desiredHeight;
                    XmlPreviewBox.MaxHeight = desiredHeight;
                }
                if(desiredWidth > 0)
                {
                    StatusText.Width = desiredWidth;
                    StatusText.MaxWidth = desiredWidth;
                }

            }
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is PolicyWizardViewModel pvm) _vm = pvm;
            base.OnNavigatedTo(e);
        }
        private void ShowXmlButton_Click(object sender, RoutedEventArgs e)
        {
            _xmlVisible = !_xmlVisible;

            if (_xmlVisible)
            {
                // Generate or fetch the XML you are about to save
                var xml = XmlPolicyBuilder.Build(_vm.Policy);

                // Populate the preview and show it
                XmlPreviewBox.Text = xml.ToString();
                XmlPreviewBox.Visibility = Visibility.Visible;
                ShowXmlButton.Content = "Hide XML";
            }
            else
            {
                // Hide the preview
                XmlPreviewBox.Visibility = Visibility.Collapsed;
                ShowXmlButton.Content = "Show XML";
            }
        }


        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.IsValid)
                {
                    if (App.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.UpdatePolicyVm(_vm, 1337);

                        if (!_vm.IsValid)
                        {
                            return;
                        }
                    }
                    else if (App.MainWindow == null)
                        return;


                    var doc = XmlPolicyBuilder.Build(_vm.Policy);
                    // Validate before save (use local XSD if available)
                    XmlSchemaValidator.Validate(doc);

                    var settings = new XmlWriterSettings
                    {
                        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), // UTF-8, no BOM (common for configs)
                        Indent = true,
                        OmitXmlDeclaration = false                                          // <-- do not omit
                    };

                    var suggested = string.IsNullOrWhiteSpace(FileNameBox.Text) ? "policy.xml" : FileNameBox.Text.Trim();
                    var path = await Services.FilePickerService.SaveXmlAsync(App.MainWindow, App.Settings.DefaultDirectory, suggested);
                    if (path == null) return;

                    using var fs = File.Create(path);
                    using var writer = XmlWriter.Create(fs, settings);
                    doc.Save(fs);
                    StatusText.Text = "Saved and validated.";
                }
                else
                {
                    throw new("Policy is not valid. Please go through the wizard again and correct any errors before saving.");
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"{ex.Message}";
            }
        }

        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            double height = e.Size.Height;

            double desiredHeight = Math.Max(100, height * 0.6);

            double width = e.Size.Width;
            double desiredWidth = Math.Max(100, width * 0.8);

            if (desiredHeight > 0)
            {
                XmlPreviewBox.Height = desiredHeight;
                XmlPreviewBox.MaxHeight = desiredHeight;
            }

            if (desiredWidth > 0)
            {
                StatusText.Width = desiredWidth;
                StatusText.MaxWidth = desiredWidth;
            }
        }
    }
}