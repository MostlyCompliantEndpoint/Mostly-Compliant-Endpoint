using AssignedAccessDesigner.Models;
using AssignedAccessDesigner.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AssignedAccessDesigner.Services
{
    public static class AssignedAccessPolicyBuilder
    {
        public static void AddXmlToUi(XDocument xml, PolicyWizardViewModel _vm)
        {
            // Set up all fields in UI based on opened polkcy
            bool AllowRemovable = false;
            bool AllowDownloads = false;
            bool RestrictAll = false;
            bool NoRestrictions = false;

            int currentProfileId = 0;

            if (_vm.Policy.Profiles.Count > 0)
            {
                currentProfileId = _vm.Policy.Profiles.Count;
            }

            Dictionary<int, string> ProfileDict = new();

            // Query elements by name
            foreach (var node in xml.Descendants())
            {
                // ProfileId
                if (node.Name.LocalName == "Profile")
                {
                    var name = ""; var guid = "";
                    foreach (var attr in node.Attributes())
                    {
                        if (attr.Name != null)
                        {
                            if (attr.Name.LocalName == "Id")
                            {
                                guid = attr.Value;

                                if(!guid.StartsWith("{"))
                                {
                                    guid = "{" + guid;
                                }
                                if (!guid.EndsWith("}"))
                                {
                                    guid = guid + "}";
                                }
                            }
                            if (attr.Name.LocalName == "Name")
                            {
                                name = attr.Value;
                            }
                        }
                    }

                    // If default 999 is set, set index properly. Each time this block get hit indicates a new profile.
                    if (currentProfileId == 0)
                    {
                        currentProfileId = 1;
                        ProfileDict.Add(currentProfileId, guid);
                    }
                    else
                    {
                        currentProfileId++;
                        ProfileDict.Add(currentProfileId, guid);
                    }


                    _vm.Policy.Profiles.Add(new Profile
                    {
                        ProfileGuid = guid,
                        ProfileName = name,
                        ProfileId = (_vm.Policy.Profiles.Count + 1)
                    });

                } // End Profile ID

                // App List
                else if (node.Name.LocalName == "App")
                {
                    foreach (var attr in node.Attributes())
                    {
                        var attribName = attr.Name.LocalName;
                        var attribValue = attr.Value;
                        var autoLaunch = false;
                        var path = "";
                        var autoLaunchArgs = "";

                        if (attribName == "AppUserModelId")
                        {
                            // UWP
                            if (attr.NextAttribute != null && attr.NextAttribute.Name.LocalName.ToLower() == "autolaunch")
                            {
                                if (attr.NextAttribute.NextAttribute != null && attr.NextAttribute.NextAttribute.Name.LocalName.ToLower() == "autolauncharguments")
                                {
                                    autoLaunchArgs = attr.NextAttribute.NextAttribute.Value;
                                }

                                autoLaunch = true;
                                path = attribValue;
                            }
                            else
                                path = attribValue;

                            _vm.Policy.AllowedApps.Add(new AllowedApp
                            {
                                ProfileId = currentProfileId,
                                AppPath = path,
                                AutoLaunch = autoLaunch,
                                AutoLaunchArgs = autoLaunchArgs
                            });
                        }
                        else if (attribName == "DesktopAppPath")
                        {
                            // EXE
                            if (attr.NextAttribute != null && attr.NextAttribute.Name.LocalName.ToLower() == "autolaunch")
                            {
                                if (attr.NextAttribute.NextAttribute != null && attr.NextAttribute.NextAttribute.Name.LocalName.ToLower() == "autolauncharguments")
                                {
                                    autoLaunchArgs = attr.NextAttribute.NextAttribute.Value;
                                }
                                autoLaunch = true;
                                path = attribValue;
                            }
                            else
                                path = attribValue;

                            _vm.Policy.AllowedApps.Add(new AllowedApp
                            {
                                ProfileId = currentProfileId,
                                AppPath = path,
                                AutoLaunch = autoLaunch,
                                AutoLaunchArgs = autoLaunchArgs
                            });
                        }
                    }
                } // End of Apps

                // Start of Single App Detection
                /*
                 *       <KioskModeApp 
                 *          v4:ClassicAppPath="%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe" 
                 *          v4:ClassicAppArguments="--kiosk https://www.contoso.com/ --edge-kiosk-type=fullscreen --kiosk-idle-timeout-minutes=2" />
      <v4:BreakoutSequence 
            Key="Ctrl+A" />
    </Profile>
                 * 
                 */
                else if (node.Name.LocalName == "KioskModeApp")
                {
                    _vm.Policy.Mode = Models.KioskMode.SingleApp;
                    var path = "";
                    var args = "";

                    if (node.FirstAttribute != null && node.FirstAttribute.Name.LocalName.ToLower() == "classicapppath")
                    {
                        path = node.FirstAttribute.Value;
                        foreach (var attributes in node.Attributes())
                        {
                            if (attributes.Name.LocalName.ToLower() == "classicapparguments")
                            {
                                args = attributes.Value;
                            }
                        }
                    }
                    else if (node.FirstAttribute != null && node.FirstAttribute.Name.LocalName.ToLower() == "appusermodelid")
                    {
                        path = node.FirstAttribute.Value;
                    }

                        bool found = false;

                    foreach (var sApp in _vm.Policy.SingleApp)
                    {
                        if (sApp.ProfileId == currentProfileId)
                        {
                            // Already exists, update
                            sApp.AppPath = path;
                            sApp.AppArguments = args;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        _vm.Policy.SingleApp.Add(new SingleApp
                        {
                            AppPath = path,
                            AppArguments = args,
                            ProfileId = currentProfileId
                        });
                    }

                }
                else if (node.Name.LocalName.ToLower() == "breakoutsequence")
                {
                    var key = "";
                    _vm.Policy.Mode = Models.KioskMode.SingleApp;

                    if (node.FirstAttribute != null && node.FirstAttribute.Name.LocalName.ToLower() == "key")
                    {
                        key = node.FirstAttribute.Value;
                    }

                    if (key != null)
                    {
                        bool found = false;

                        foreach (var sApp in _vm.Policy.SingleApp)
                        {
                            if (sApp.ProfileId == currentProfileId)
                            {
                                sApp.BreakoutSequence = key;
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            _vm.Policy.SingleApp.Add(new SingleApp
                            {
                                BreakoutSequence = key,
                                ProfileId = currentProfileId
                            });

                        }
                    }
                }


                // Start Menu
                else if (node.Name.LocalName.ToLower() == "startpins")
                {

                  if(node.Value != null)
                    {
                        var rawJson = Regex.Replace(node.Value, @"<!\[CDATA\[(.*?)\]\]>", "$1");
                        using JsonDocument jdoc = JsonDocument.Parse(rawJson);
                        JsonElement root = jdoc.RootElement;

                        foreach (JsonProperty prop in root.EnumerateObject())
                        {
                            if (prop.Name.ToLower() == "pinnedlist")
                            {
                                var Values = prop.Value;

                                foreach (var Value in Values.EnumerateArray())
                                {
                                    if (Value.TryGetProperty("packagedAppId", out var valUWP))
                                    {
                                        if (valUWP.ToString() != null)
                                        {
                                            _vm.Policy.StartMenuPins.Add(new Models.StartMenuPin
                                            {
                                                Path = valUWP.ToString(),
                                                ProfileId = currentProfileId
                                            });
                                        }
                                    }
                                    else if (Value.TryGetProperty("desktopAppLink", out var valDesktop))
                                    {
                                        if (valDesktop.ToString() != null)
                                        {
                                            _vm.Policy.StartMenuPins.Add(new Models.StartMenuPin
                                            {
                                                Path = valDesktop.ToString(),
                                                ProfileId = currentProfileId
                                            });
                                        }
                                    }
                                }
                            }

                        }
                    }  

                }

                // Restrictions
                else if (node.Name.LocalName.ToLower() == "allowednamespace")
                    AllowDownloads = true;
                else if (node.Name.LocalName.ToLower() == "allowremovabledrives")
                    AllowRemovable = true;
                else if (node.Name.LocalName.ToLower() == "norestriction")
                    NoRestrictions = true;
                else if (node.Name.LocalName.ToLower() == "fileexplorernamespacerestrictions")
                    RestrictAll = true;

                // Show Task bar
                else if (node.Name.LocalName.ToLower() == "taskbar")
                {
                    if (node.FirstAttribute != null && node.FirstAttribute.Name.LocalName.ToLower() == "showtaskbar")
                    {
                        if (node.FirstAttribute.Value == "true")
                        {
                            _vm.Policy.TaskbarEnabledList.Add(new TaskbarEnabled
                            {
                                ProfileId = currentProfileId,
                                IsTaskbarEnabled = true
                            });
                        }
                        else if (node.FirstAttribute.Value == "false")
                        {
                            _vm.Policy.TaskbarEnabledList.Add(new TaskbarEnabled
                            {
                                ProfileId = currentProfileId,
                                IsTaskbarEnabled = false
                            });
                        }
                    }
                }

                // Task bar layout
                else if (node.Name.LocalName.ToLower() == "taskbarlayout")
                { 
                    var taskbarXml = Regex.Replace((node.Value), @"<!\[CDATA\[(.*?)\]\]>", "$1").Trim();
                    var taskXml = XDocument.Parse(taskbarXml);

                    foreach (var tbElement in taskXml.Descendants())
                    {
                        if (tbElement.Name.LocalName.ToLower() == "desktopapp")
                        {
                            if (tbElement.FirstAttribute != null && tbElement.FirstAttribute.Name.LocalName.ToLower() == "desktopapplicationid")
                                _vm.Policy.TaskbarPins.Add(new Models.TaskbarPin
                                {
                                    Path = tbElement.FirstAttribute.Value,
                                    ProfileId = currentProfileId
                                });
                            else if (tbElement.FirstAttribute != null && tbElement.FirstAttribute.Name.LocalName == "DesktopApplicationLinkPath")
                                _vm.Policy.TaskbarPins.Add(new Models.TaskbarPin
                                {
                                    Path = tbElement.FirstAttribute.Value,
                                    ProfileId = currentProfileId
                                });
                        }
                    }
                }

                // Accounts and Groups
                else if (node.Name.LocalName.ToLower() == "autologonaccount")
                {
                    var DisplayName = "";
                    var profileGuid = "";
                    var profileId = 1;

                    if (node.FirstAttribute != null && node.FirstAttribute.Name.LocalName.ToLower() == "displayname")
                    {
                        DisplayName = node.FirstAttribute.Value;

                        if (node.NextNode != null)
                        {
                            XElement nextNode = (XElement)node.NextNode;
                            if (nextNode.FirstAttribute.Name.LocalName == "Id")
                            {
                                profileGuid = nextNode.FirstAttribute.Value;
                                if (ProfileDict.ContainsValue(profileGuid))
                                {
                                    foreach (var key in ProfileDict.Keys)
                                    {
                                        string val = "";
                                        ProfileDict.TryGetValue(key, out val);
                                        if (val == profileGuid)
                                        {
                                            profileId = key;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _vm.Policy.LogonAccounts.Add(new Models.LogonAccount
                    {
                        AccountName = DisplayName,
                        Type = "AutoLogon",
                        Location = "Local",
                        ProfileId = profileId
                    });
                }

                // Global
                else if (node.Name.LocalName.ToLower() == "globalprofile")
                {
                    var profileId = 1;
                    if (node.FirstAttribute != null && node.FirstAttribute.Name.LocalName.ToLower() == "id")
                    {
                        var profileGuid = node.FirstAttribute.Value;
                        if (ProfileDict.ContainsValue(profileGuid))
                        {
                            foreach (var key in ProfileDict.Keys)
                            {
                                string val = "";
                                ProfileDict.TryGetValue(key, out val);
                                if (val == profileGuid)
                                {
                                    profileId = key;
                                    break;
                                }
                            }
                        }
                    }

                    _vm.Policy.LogonAccounts.Add(new Models.LogonAccount
                    {
                        AccountName = "",
                        Type = "Global",
                        Location = "Local",
                        ProfileId = profileId
                    });
                }
                // Accounts
                else if (node.Name.LocalName.ToLower() == "account")
                {
                    var accountName = node.Value.ToString();

                    // Need regex match to determine on prem, vs local, vs entrid
                    Regex ActiveDirectory = new(@"^(?<domain>[A-Za-z0-9._-]{1,15})\\(?<user>[^\s\\/:*?""<>|]+)$", RegexOptions.IgnoreCase);
                    Regex EntraUpn = new(@"AzureAD\\", RegexOptions.IgnoreCase);

                    var AD = ActiveDirectory.Match(accountName);
                    var Entra = EntraUpn.Match(accountName);
                    var profileId = 1;
                    var profileGuid = "";

                    if (node.NextNode != null)
                    {
                        XElement nextNode = (XElement)node.NextNode;
                        if (nextNode.FirstAttribute.Name.LocalName == "Id")
                        {
                            profileGuid = nextNode.FirstAttribute.Value;
                            if (ProfileDict.ContainsValue(profileGuid))
                            {
                                foreach (var key in ProfileDict.Keys)
                                {
                                    string val = "";
                                    ProfileDict.TryGetValue(key, out val);
                                    if (val == profileGuid)
                                    {
                                        profileId = key;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (Entra.Success)
                    {
                        _vm.Policy.LogonAccounts.Add(new Models.LogonAccount
                        {
                            AccountName = accountName,
                            Type = "User",
                            Location = "Entra ID",
                            ProfileId = profileId
                        });
                    }
                    else if (AD.Success)
                    {
                        _vm.Policy.LogonAccounts.Add(new Models.LogonAccount
                        {
                            AccountName = accountName,
                            Type = "User",
                            Location = "Active Directory",
                            ProfileId = profileId
                        });
                    }
                    else
                    {
                        _vm.Policy.LogonAccounts.Add(new Models.LogonAccount
                        {
                            AccountName = accountName,
                            Type = "User",
                            Location = "Local",
                            ProfileId = profileId
                        });
                    }
                }
                // Groups
                else if (node.Name.LocalName.ToLower() == "usergroup")
                {
                    var Location = "";
                    var Name = "";

                    var profileId = 1;
                    var profileGuid = "";

                    if (node.NextNode != null)
                    {
                        XElement nextNode = (XElement)node.NextNode;
                        if (nextNode.FirstAttribute != null && nextNode.FirstAttribute.Name.LocalName.ToLower() == "id")
                        {
                            profileGuid = nextNode.FirstAttribute.Value;
                            if (ProfileDict.ContainsValue(profileGuid))
                            {
                                foreach (var key in ProfileDict.Keys)
                                {
                                    string val = "";
                                    ProfileDict.TryGetValue(key, out val);
                                    if (val == profileGuid)
                                    {
                                        profileId = key;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    foreach (var attr in node.Attributes())
                    {
                        if (attr.Name.LocalName.ToLower() == "type")
                        {
                            if (attr.Value == "ActiveDirectoryGroup")
                                Location = "Active Directory";
                            else if (attr.Value == "AzureActiveDirectoryGroup")
                                Location = "Entra ID";
                            else if (attr.Value == "LocalGroup")
                                Location = "Local";
                        }
                        else if (attr.Name.LocalName.ToLower() == "name")
                        {
                            Name = attr.Value;
                        }
                    }

                    _vm.Policy.LogonAccounts.Add(new Models.LogonAccount
                    {
                        AccountName = Name,
                        Type = "Group",
                        Location = Location,
                        ProfileId = profileId
                    });
                }

            } // End of ForEach

            // Set Multi vs Single based on number of apps added. Single will need additional logic as well to verify its UWP or Edge
            if (_vm.Policy.AllowedApps.Count > 1)
            {
                _vm.Policy.Mode = Models.KioskMode.MultiApp;
            }
            else
            {
                _vm.Policy.Mode = Models.KioskMode.SingleApp;
            } // End of Mode

            // Set Restrictions
            if (NoRestrictions && (AllowDownloads || AllowRemovable))
            {
                // Invalid XML, defaulting no restrictions since it's present
                AllowRemovable = false;
                AllowDownloads = false;
                RestrictAll = false;
            }
            else if (RestrictAll && (AllowRemovable || AllowDownloads || NoRestrictions))
            {
                RestrictAll = false;
            }

            _vm.Policy.ExplorerRestrictions.Add(new Models.ExplorerRestriction
            {
                AllowDownloads = AllowDownloads,
                AllowRemovableMedia = AllowRemovable,
                NoRestrictions = NoRestrictions,
                RestrictAll = RestrictAll
            });


        }
    }
}
