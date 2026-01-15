using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AssignedAccessDesigner.Services
{
    public static class XmlPolicyBuilder
    {


        // Assigned Access root namespace per MS docs
        private static readonly XNamespace ns = "http://schemas.microsoft.com/AssignedAccess/2017/config";
        private static readonly XNamespace xs = "http://www.w3.org/2001/XMLSchema";
        private static readonly XNamespace rs5 = "http://schemas.microsoft.com/AssignedAccess/201810/config";
        private static readonly XNamespace v3 = "http://schemas.microsoft.com/AssignedAccess/2020/config";
        private static readonly XNamespace v4 = "http://schemas.microsoft.com/AssignedAccess/2021/config";
        private static readonly XNamespace v5 = "http://schemas.microsoft.com/AssignedAccess/2022/config";

        // Tasbar namespaces uses for Taskbar Pin List
        private static readonly XNamespace tbns = "http://schemas.microsoft.com/Start/2014/LayoutModification";
        private static readonly XNamespace tbdefaultlayout = "http://schemas.microsoft.com/Start/2014/FullDefaultLayout";
        private static readonly XNamespace tbstart = "http://schemas.microsoft.com/Start/2014/StartLayout";
        private static readonly XNamespace tbtaskbar = "http://schemas.microsoft.com/Start/2014/TaskbarLayout";


        // Regex to determine what type of path was provided from the user
        static readonly Regex PathSimple = new(
            @"^(?:[A-Za-z]:\\|\\\\[^\\\/]+\\[^\\\/]+).*$",
            RegexOptions.Compiled);
        static readonly Regex AumidSimple = new(@"^[^!\s]+!|\..+\.[^!]+$", RegexOptions.Compiled);

        public static XDocument Build(AssignedAccessPolicy policy)
        {
            bool IsAumid(string? s) =>
            !string.IsNullOrWhiteSpace(s) && AumidSimple.IsMatch(s.Trim());

            bool IsAbsoluteWindowsPath(string? s) =>
                !string.IsNullOrWhiteSpace(s) && PathSimple.IsMatch(s.Trim());


            var root = new XElement(ns + "AssignedAccessConfiguration",
                    new XAttribute(XNamespace.Xmlns + "xs", xs.NamespaceName));

            bool v3restrictions = false;
            bool v4restrictions = false;
            bool v5restrictions = false;
            bool rs5restrictions = false;

            // V3 Namespace checks
            foreach (var restrict in policy.ExplorerRestrictions)
            {
                rs5restrictions = true; // RS5 is required for any Explorer restrictions

                if (restrict.AllowRemovableMedia || restrict.NoRestrictions)
                {
                    v3restrictions = true;
                }
                else if (restrict.AllowDownloads)
                    v5restrictions = true;
            }

            foreach (var acc in policy.LogonAccounts)
            {
                if (acc.Type == "Global")
                {
                    v3restrictions = true;
                }
                else if (acc.Type == "AutoLogon")
                    rs5restrictions = true;
            }

            if (v3restrictions)
            {
                root.Add(new XAttribute(XNamespace.Xmlns + "v3", v3.NamespaceName));
            }
            // V3 Namespace checks

            // V4 Namespace checks
            foreach (var app in policy.SingleApp)
            {
                if (!string.IsNullOrWhiteSpace(app.AppPath))
                {
                    if (!IsAumid(app.AppPath))
                    {
                        v4restrictions = true;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(app.BreakoutSequence))
                    v4restrictions = true;
            }

            if (v4restrictions)
            {
                root.Add(new XAttribute(XNamespace.Xmlns + "v4", v4.NamespaceName));
            }
            // V4 Namespace checks

            // V5 Namespace checks
            if (policy.StartMenuPins.Count > 0)
            {
                v5restrictions = true;
            }

            if (policy.TaskbarPins.Count > 0)
                v5restrictions = true;


            if (v5restrictions)
            {
                root.Add(new XAttribute(XNamespace.Xmlns + "v5", v5.NamespaceName));
            }
            // V5 Namespace checks

            // rs5 Namespace check
            foreach (var app in policy.AllowedApps)
            {
                if (app.AutoLaunch)
                {
                    rs5restrictions = true;
                }
                else if (!string.IsNullOrEmpty(app.AutoLaunchArgs))
                    rs5restrictions = true;
            }

            if (rs5restrictions)
            {
                root.Add(new XAttribute(XNamespace.Xmlns + "rs5", rs5.NamespaceName));
            }
            // rs5 Namespace check


            // Setup Profile array that will be used to store the existing profiles.
            // This can be referneced to dynamically add settings to each profile without an expensive loop
            List<XElement> buildProfiles = new();
            List<int> buildProfileIds = new();
            List<string> buildProfileGuids = new();

            if (policy.Profiles != null && policy.Profiles.Count > 0)
            {
                foreach (Profile profile in policy.Profiles)
                {
                    if (profile.ProfileGuid != null && profile.ProfileId != 0)
                    {
                        var ProfileXml = new XElement(ns + "Profile",
                            new XAttribute("Id", profile.ProfileGuid),
                            new XAttribute("Name", profile.ProfileName == null ? "" : profile.ProfileName));

                        buildProfiles.Add(ProfileXml);
                        buildProfileIds.Add(profile.ProfileId);
                        buildProfileGuids.Add(profile.ProfileGuid);
                    }
                }
            }

            // TODO:: Set up for loop based on ProfileDict.Keys and iterate through the profiles to create the respective sections below
            // while dynamically setting up the IDs to the respective IDs

            var Profiles = new XElement(ns + "Profiles");

            // Single App 
            if(policy.Mode == KioskMode.SingleApp)
            {
                List<XElement> appLists = new();
                foreach (var build in buildProfiles)
                {
                    var apps = new XElement(ns + "KioskModeApp");
                    appLists.Add(apps);
                }

                foreach (var a in policy.SingleApp)
                {

                    var appProfileId = a.ProfileId;
                    var profileIndex = 0;

                    if (appProfileId != 0)
                    {
                        profileIndex = buildProfileIds.IndexOf(appProfileId);
                    }

                    var appArgs = string.IsNullOrWhiteSpace(a.AppArguments) ? "" : a.AppArguments;
                    var breakSeq = string.IsNullOrWhiteSpace(a.BreakoutSequence) ? "" : a.BreakoutSequence;

                    if (!string.IsNullOrWhiteSpace(a.AppPath))
                    {
                        if (IsAumid(a.AppPath))
                        {
                            var KioskModeApp = new XElement(ns + "KioskModeApp",
                                new XAttribute("AppUserModelId", a.AppPath));

                            buildProfiles[profileIndex].Add(KioskModeApp);
                        }
                        else
                        {
                            var KioskModeApp = new XElement(ns + "KioskModeApp",
                                new XAttribute(v4 + "ClassicAppPath", a.AppPath),
                                new XAttribute(v4 + "ClassicAppArguments", string.IsNullOrWhiteSpace(appArgs) ? "" : appArgs));

                            buildProfiles[profileIndex].Add(KioskModeApp);
                        }

                        if (!(string.IsNullOrWhiteSpace(a.BreakoutSequence)))
                        {
                            var BreakoutSequence = new XElement(v4 + "BreakoutSequence",
                                new XAttribute("Key", a.BreakoutSequence));

                            buildProfiles[profileIndex].Add(BreakoutSequence);
                        }
                    }
                } // End of ForEach
            }
            else
            {
                // Allowed apps (Multi)
                List<XElement> appLists = new();

                foreach (var build in buildProfiles)
                {
                    var apps = new XElement(ns + "AllowedApps");
                    appLists.Add(apps);
                }

                foreach (var a in policy.AllowedApps)
                {

                    var appProfileId = a.ProfileId;
                    var profileIndex = 0;

                    if (appProfileId != 0)
                    {
                        profileIndex = buildProfileIds.IndexOf(appProfileId);
                    }

                    if (a.AutoLaunch)
                    {
                        if (!string.IsNullOrWhiteSpace(a.AppPath))
                        {
                            var expanded = Environment.ExpandEnvironmentVariables(a.AppPath);

                            if (IsAumid(a.AppPath))
                            {
                                var appEl = new XElement(ns + "App",
                                new XAttribute("AppUserModelId", a.AppPath),
                                new XAttribute(rs5 + "AutoLaunch", "true"),
                                new XAttribute(rs5 + "AutoLaunchArguments", a.AutoLaunchArgs == null ? "" : a.AutoLaunchArgs)
                                );

                                appLists[profileIndex].Add(appEl);
                            }
                            else if ( IsAbsoluteWindowsPath(a.AppPath) || IsAbsoluteWindowsPath(expanded) )
                            {
                                var appEl = new XElement(ns + "App",
                                new XAttribute("DesktopAppPath", a.AppPath),
                                new XAttribute(rs5 + "AutoLaunch", "true"),
                                new XAttribute(rs5 + "AutoLaunchArguments", string.IsNullOrWhiteSpace(a.AutoLaunchArgs) ? "" : a.AutoLaunchArgs)
                                );
                                appLists[profileIndex].Add(appEl);
                            }

                        }

                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(a.AppPath))
                        {

                            var expanded = Environment.ExpandEnvironmentVariables(a.AppPath);

                            if (IsAumid(a.AppPath))
                            {
                                var appEl = new XElement(ns + "App",
                                new XAttribute("AppUserModelId", a.AppPath)
                                );

                                appLists[profileIndex].Add(appEl);
                            }
                            else if (IsAbsoluteWindowsPath(a.AppPath) || IsAbsoluteWindowsPath(expanded))
                            {
                                var appEl = new XElement(ns + "App",
                                new XAttribute("DesktopAppPath", a.AppPath)
                                );
                                appLists[profileIndex].Add(appEl);
                            }

                        }

                    }
                } // End of ForEach

                int buildcounter = 0;
                foreach (var appl in appLists)
                {
                    var allappslist = new XElement(ns + "AllAppsList");
                    allappslist.Add(appl);
                    buildProfiles[buildcounter].Add(allappslist);
                    buildcounter++;
                }
                
            }



            // Explorer restrictions 
            foreach (var restrict in policy.ExplorerRestrictions)
            {
                var restrictions = new XElement(rs5 + "FileExplorerNamespaceRestrictions");
                var appProfileId = restrict.ProfileId;
                var profileIndex = 0;

                if (appProfileId > 0)
                {
                    profileIndex = buildProfileIds.IndexOf(appProfileId);
                }

                // If none are selected do not apend anything and just leave restrictions off the Profile
                if (restrict.AllowDownloads || restrict.NoRestrictions || restrict.RestrictAll || restrict.AllowRemovableMedia)
                {
                    // Check for all or nothing first
                    if (restrict.RestrictAll)
                    {
                        // Add FileExplorerNamespaceResitrction with nothing in it
                        buildProfiles[profileIndex].Add(restrictions);
                    }
                    else if (restrict.NoRestrictions)
                    {
                        restrictions.Add(new XElement(v3 + "NoRestriction"));
                        buildProfiles[profileIndex].Add(restrictions);
                    }
                    else if (restrict.AllowDownloads || restrict.AllowRemovableMedia)
                    {
                        if (restrict.AllowDownloads)
                        {
                            restrictions.Add(new XElement(rs5 + "AllowedNamespace",
                                new XAttribute("Name", "Downloads")));
                        }

                        if (restrict.AllowRemovableMedia)
                        {
                            restrictions.Add(new XElement(v3 + "AllowRemovableDrives"));
                        }

                        buildProfiles[profileIndex].Add(restrictions);
                    }
                }
            }

            if (policy.Mode != KioskMode.SingleApp)
            {
                // Start menu pins
                foreach (var id in buildProfileIds)
                {
                    var appProfileId = id;
                    var profileIndex = 0;

                    if (appProfileId > 0)
                    {
                        profileIndex = buildProfileIds.IndexOf(appProfileId);
                    }

                    var pinnedList = new List<Dictionary<string, string>>();
                    foreach (var pin in policy.StartMenuPins)
                    {
                        if (!string.IsNullOrWhiteSpace(pin.Path) && pin.ProfileId == id)
                        {
                            var expanded = Environment.ExpandEnvironmentVariables(pin.Path);

                            if (IsAumid(pin.Path))
                            {
                                pinnedList.Add(new Dictionary<string, string>
                                {
                                    ["packagedAppId"] = pin.Path.Trim()
                                });
                            }
                            else if (IsAbsoluteWindowsPath(pin.Path) || IsAbsoluteWindowsPath(expanded))
                            {
                                pinnedList.Add(new Dictionary<string, string>
                                {
                                    ["desktopAppLink"] = pin.Path.Trim()
                                });
                            }
                        }
                    } // End of ForEach Create Pins

                    // Add all the pins to get ready to append to the XML if there were entries
                    if (pinnedList.Count > 0)
                    {
                        var payload = new Dictionary<string, List<Dictionary<string,string>>>
                        {
                            ["pinnedList"] = pinnedList
                        };

                        var options = new JsonSerializerOptions { 
                            WriteIndented = true,
                            TypeInfoResolver = AppJsonContext.Default
                        };
                        var pinJson = JsonSerializer.Serialize(payload, options);

                        // Wrap in CDATA
                        var cdata = new XCData(pinJson);

                        var pins = new XElement(v5 + "StartPins", cdata);
                        buildProfiles[profileIndex].Add(pins);
                    }

                } // End of For Each
            } // End of Start Menu Pins

            // Add Taskbar show hide 
            if (policy.Mode != KioskMode.SingleApp)
            {
                var profileIndex = 0;

                foreach (var tb in policy.TaskbarEnabledList)
                {
                    if (tb.ProfileId > 0 && buildProfileIds.Contains(tb.ProfileId))
                    {
                        profileIndex = buildProfileIds.IndexOf(tb.ProfileId);
                        var taskbar = new XElement(ns + "Taskbar",
                            new XAttribute("ShowTaskbar", tb.IsTaskbarEnabled));
                        buildProfiles[profileIndex].Add(taskbar);
                    }
                }
            }

            // Generate layout from Pins
            var taskbarPins = policy.TaskbarPins;

            if (taskbarPins != null)
            {
                List<XElement> tbPinLists = new();

                foreach (var build in buildProfiles)
                {
                    tbPinLists.Add(new XElement(tbtaskbar + "TaskbarPinList"));
                }

                    var validPins = false;
                    foreach (var pin in taskbarPins)
                    {
                        if (!string.IsNullOrWhiteSpace(pin.Path))
                        {
                            var index = buildProfileIds.IndexOf(pin.ProfileId);
                            var expanded = Environment.ExpandEnvironmentVariables(pin.Path);

                        if (IsAumid(pin.Path))
                            {
                                tbPinLists[index].Add(new XElement(tbtaskbar + "DesktopApp",
                                    new XAttribute("DesktopApplicationID", pin.Path)
                                       ));
                                validPins = true;
                            }
                            else if (IsAbsoluteWindowsPath(pin.Path) || IsAbsoluteWindowsPath(expanded) )
                            {
                                tbPinLists[index].Add(new XElement(tbtaskbar + "DesktopApp",
                                    new XAttribute("DesktopApplicationLinkPath", pin.Path)
                                       ));
                                validPins = true;
                            }
                        }
                    }

                    // Pins may have been added on the UI, but if no pins actually contain data don't append this section
                    if (validPins)
                    {
                        foreach (var id in buildProfileIds)
                        {

                            var idIndex = buildProfileIds.IndexOf(id);
                            bool append = false;
                           foreach (var tb in policy.TaskbarEnabledList)
                           {
                               if (tb.ProfileId == id)
                               {
                                if (tb.IsTaskbarEnabled)
                                    append = true;
                                else
                                    append = false;
                                }
                           }

                            if (tbPinLists[idIndex].HasElements && append)
                            {
                                var tbroot = new XElement(tbns + "LayoutModificationTemplate",
                               new XAttribute(XNamespace.Xmlns + "defaultlayout", tbdefaultlayout.NamespaceName),
                               new XAttribute(XNamespace.Xmlns + "start", tbstart.NamespaceName),
                               new XAttribute(XNamespace.Xmlns + "taskbar", tbtaskbar.NamespaceName));

                                var customTaskbarLayout = new XElement(tbns + "CustomTaskbarLayoutCollection");
                                var tbLayout = new XElement(tbdefaultlayout + "TaskbarLayout");
                                var tbPinList = new XElement(tbtaskbar + "TaskbarPinList");

                                tbLayout.Add(tbPinLists[idIndex]);
                                customTaskbarLayout.Add(tbLayout);
                                tbroot.Add(customTaskbarLayout);
                                var tbdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), tbroot);
                                var tbcdata = new XCData(tbdoc.ToString());

                                var formattedPinList = new XElement(v5 + "TaskbarLayout", tbcdata);
                                buildProfiles[idIndex].Add(formattedPinList);
                            }
                        }
                    }

            } 

            // Add the completed Profile to Root
            foreach (var Profile in buildProfiles)
            {
                Profiles.Add(Profile);
            }

            root.Add(Profiles);


            // Add Configs Section
            var configs = new XElement(ns + "Configs");
            
            bool globalAdded = false;

            // First loop for just global profiles since they are not under the config element
            foreach (var acc in policy.LogonAccounts)
            {
                if (acc.Type == "Global")
                {
                    var curProfile = acc.ProfileId;
                    var index = buildProfileIds.IndexOf(acc.ProfileId);
                    var profileGuid = buildProfileGuids[index];
                    if (profileGuid != null)
                    {
                        var globalconfig = new XElement(v3 + "GlobalProfile",
                            new XAttribute("Id", profileGuid));
                        configs.Add(globalconfig);
                    }
                }
            }

            // Logon accounts/groups
            foreach (var acc in policy.LogonAccounts)
            {
                bool added = false;

                var config = new XElement(ns + "Config");

                if (acc.Type == "AutoLogon")
                {
                    config.Add(new XElement(ns + "AutoLogonAccount",
                          new XAttribute(rs5 + "DisplayName", acc.AccountName)
                          ));
                    added = true;
                }
                else if (acc.Type == "Group")
                {
                    var Type = "";

                    if (acc.Location == "Active Directory")
                        Type = "ActiveDirectoryGroup";
                    else if (acc.Location == "Entra ID")
                        Type = "AzureActiveDirectoryGroup";
                    else
                    {
                        Type = "LocalGroup";
                    }

                    config.Add(new XElement(ns + "UserGroup",
                       new XAttribute("Name", acc.AccountName),
                       new XAttribute("Type", Type)
                       ));

                    added = true;
                }
                else if (acc.Type != "Global")
                {
                    config.Add(new XElement(ns + "Account", acc.AccountName));
                    added = true;
                }

                // Global doesn't get the default profile key
                if (acc.Type != "Global")
                {
                    var curProfile = acc.ProfileId;
                    var index = buildProfileIds.IndexOf(acc.ProfileId);
                    var profileGuid = buildProfileGuids[index];

                    if (profileGuid != null)
                    {
                        config.Add(new XElement(ns + "DefaultProfile",
                        new XAttribute("Id", profileGuid)));
                    }
                }

                if(added)
                configs.Add(config);

            } // End of ForEach

            root.Add(configs);

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

            return doc;

        } // End of Build XML
    }
}