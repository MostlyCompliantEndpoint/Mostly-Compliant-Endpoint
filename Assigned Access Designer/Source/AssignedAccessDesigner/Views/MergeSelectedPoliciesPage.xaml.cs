using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.Services;
using AssignedAccessDesigner.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AssignedAccessDesigner.Views
{
    public sealed partial class MergeSelectPoliciesPage : Page
    {
        private MergeWizardViewModel _vm = new();
        private PolicyWizardViewModel _pvm = new();
        private List<XDocument> policiesToMerge = new();
        private bool IsSingleAppMerge = false;
        private bool IsMultiAppMerge = false;

        public MergeSelectPoliciesPage() { InitializeComponent(); }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is MergeWizardViewModel mvm) _vm = mvm;
            if (e.Parameter is PolicyWizardViewModel pvm) _pvm = pvm;

            if(_pvm.Policy.PoliciesToMerge.Count > 0)
            {
                MergeList.ItemsSource = null;
                MergeList.ItemsSource = _pvm.Policy.PoliciesToMerge;
            }

            base.OnNavigatedTo(e);
        }

        private async void Add_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (App.MainWindow == null)
                return;

            bool addFileCanceled = false;
            var paths = await FilePickerService.PickMultipleXmlAsync(App.MainWindow);

            if(paths == null || paths.Count == 0) return;

            foreach (var p in paths)
            {

                bool skipFile = false;
                var doc = XDocument.Load(p);
                // For simplicity, parse only fields we write (you can expand parsing to a full read)
                if(doc.Root != null)
                {
                    XNamespace ns = "http://schemas.microsoft.com/AssignedAccess/2017/config";
                    var profiles = doc.Root?.Element(XName.Get("Profiles", ns.NamespaceName))?
                        .Elements(XName.Get("Profile", ns.NamespaceName)) ?? Enumerable.Empty<XElement>();
                    try
                    {
                        // Verify that this file has not already been uploaded
                        if (_pvm.Policy.PoliciesToMerge.Any(x => x.fileName.Equals(System.IO.Path.GetFileName(p), StringComparison.OrdinalIgnoreCase)))
                        {
                            skipFile = true;
                        }
                        else
                        {
                            foreach (var pro in profiles)
                            {
                                var singleAppElem = pro.Element(XName.Get("KioskModeApp", ns.NamespaceName));
                                var applications = doc.Root?.Element(XName.Get("AllowedApps", ns.NamespaceName))?.Elements(XName.Get("App", ns.NamespaceName)) ?? Enumerable.Empty<XElement>();

                                if (singleAppElem != null)
                                {
                                    if (IsMultiAppMerge)
                                    {
                                        _pvm.Policy.BlockMerge = true;
                                        throw new("Cannot merge Single App policy into Multi App merge set.");
                                    }
                                    else
                                    {
                                        IsSingleAppMerge = true;
                                    }
                                }
                                else if (applications != null)
                                {
                                    if (IsSingleAppMerge)
                                    {
                                        _pvm.Policy.BlockMerge = true;
                                        throw new("Cannot merge Single App policy into Multi App merge set.");
                                    }
                                    else
                                    {
                                        IsMultiAppMerge = true;
                                    }
                                }
                            }
                        }
                    }
                    catch {

                        if (App.MainWindow is MainWindow mainWindow2)
                        {
                            mainWindow2.UpdatePolicyVm(_pvm,10);
                            mainWindow2.EnableWizardNav(7);
                        }

                        addFileCanceled = true;
                        break;
                    }


                    if (!skipFile)
                    {
                        _pvm.Policy.PoliciesToMerge.Add(new MergePolicy
                        {
                            policy = doc,
                            fileName = System.IO.Path.GetFileName(p)
                        });

                        MergeList.ItemsSource = null;
                        MergeList.ItemsSource = _pvm.Policy.PoliciesToMerge;
                    }
                }
            }

            if (App.MainWindow is MainWindow mainWindow && !addFileCanceled)
            {
                mainWindow.UpdatePolicyVm(_pvm, 10);
                mainWindow.EnableWizardNav(7);
            }
            else if (addFileCanceled)
            {
                _pvm.Policy.BlockMerge = false;
            }

        }


        private void Remove_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (MergeList.SelectedItem is MergePolicy selectedPolicy)
            {
                _pvm.Policy.PoliciesToMerge.Remove(selectedPolicy);
                MergeList.ItemsSource = null;
                MergeList.ItemsSource = _pvm.Policy.PoliciesToMerge;
            }
        }
    }
}