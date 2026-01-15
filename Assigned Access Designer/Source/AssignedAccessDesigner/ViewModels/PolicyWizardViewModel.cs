using AssignedAccessDesigner.Helpers;
using AssignedAccessDesigner.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AssignedAccessDesigner.ViewModels
{
    public class PolicyWizardViewModel : INotifyPropertyChanged
    {
        public AssignedAccessPolicy Policy { get; set; } = new();


        public enum AccountFormatType
        {
            LocalFormat,
            ActiveDirectoryFormat,
            EntraFormat,
            UnknownFormat
        }

        public static class IdentityFormatClassifier
        {
            public static AccountFormatType Classify(string identity)
            {
                if (IdentityRegex.IsEntraByFormat(identity)) return AccountFormatType.EntraFormat;
                if (IdentityRegex.IsActiveDirectoryByFormat(identity)) return AccountFormatType.ActiveDirectoryFormat;
                if (IdentityRegex.IsLocalByFormat(identity)) return AccountFormatType.LocalFormat;
                return AccountFormatType.UnknownFormat;
            }
        }



        private bool _isEdit = false;
        public bool IsEdit
        {
            get => _isEdit;
            set { _isEdit = value; OnPropertyChanged(); }
        }


        private bool _isValid = true;
        public bool IsValid
        {
            get => _isValid;
            private set { _isValid = value; OnPropertyChanged(); }
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            private set { _validationMessage = value; OnPropertyChanged(); }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            private set {  _isEnabled = value; OnPropertyChanged();  }
        }

        static readonly Regex PathSimple = new(
    @"^(?:[A-Za-z]:\\|\\\\[^\\\/]+\\[^\\\/]+).*$",
    RegexOptions.Compiled);

        static readonly Regex AumidSimple = new(@"^[^!\s]+!|\..+\.[^!]+$", RegexOptions.Compiled);

        static readonly Regex LnkRegex = new(@"\.lnk", RegexOptions.Compiled);

        bool IsAumid(string? s) =>
            !string.IsNullOrWhiteSpace(s) && AumidSimple.IsMatch(s.Trim());

        bool IsAbsoluteWindowsPath(string? s) =>
            !string.IsNullOrWhiteSpace(s) && PathSimple.IsMatch(s.Trim());

        public void Validate(int page)
        {
            IsValid = true;

            try
            {
                if (page == 1 || page == 1337)// Mode Page
                {
                    try
                    {
                        if (Policy.Profiles.Count == 0)
                        {
                            throw new("You need at least one profile");
                        }
                        else if (Policy.Profiles.Count > 0)
                        {
                           foreach (var profile in Policy.Profiles)
                            {
                                if (string.IsNullOrWhiteSpace(profile.ProfileGuid))
                                {
                                    throw new("You have one or more profiles missing a GUID. Please remove that profile.");
                                }
                                else if (profile.ProfileId == 0 || profile.ProfileId.ToString() == null)
                                {
                                    throw new("You have one or more profiles missing a Profile ID. Please remove that profile.");
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        IsValid = false;
                        if(page == 1337)
                        {
                            ValidationMessage = $"\nPage: Mode Selection\nError: {ex.Message}";
                        }
                        else
                        {
                            ValidationMessage = $"\nError: {ex.Message}";
                        }
                            return;
                    }

                }
                
                if (page == 2 || page == 1337) // Apps Page
                {
                    try
                    {
                        if (Policy.Mode == KioskMode.SingleApp)
                        {
                            if (Policy.SingleApp.Count == 0)
                            {
                                throw new("You must enter an App Path or AUMID. Single App Kiosks support UWP applications or MS Edge.");
                            }

                            int UniqueProfilesFound = 0;
                            List<int> profilesFound = new();

                            foreach (var AppSettings in Policy.SingleApp)
                            {

                                if (string.IsNullOrWhiteSpace(AppSettings.AppPath))
                                {
                                    throw new("You need to specify an AUMID or App Path to be launched");
                                }

                                if (!profilesFound.Contains(AppSettings.ProfileId))
                                {
                                    profilesFound.Add(AppSettings.ProfileId);
                                    UniqueProfilesFound++;
                                }

                                bool validAumid = IsAumid(AppSettings.AppPath);
                                bool validPath = IsAbsoluteWindowsPath(AppSettings.AppPath);

                                var expanded = Environment.ExpandEnvironmentVariables(AppSettings.AppPath);
                                bool expandedPath = IsAbsoluteWindowsPath(expanded);


                                if (!(validAumid || validPath || expandedPath))
                                {
                                    if (!validAumid && !validPath && !expandedPath)
                                    {
                                        throw new(AppSettings.AppPath + " is not a valid AUMID or App Path.");
                                    }
                                    else if (!validAumid)
                                    {
                                        throw new(AppSettings.AppPath + " is not a valid AUMID.");
                                    }
                                    else if (!validPath)
                                    {
                                        throw new(AppSettings.AppPath + " is not a valid App Path.");
                                    }
                                }


                            } // End of Foreach

                            if (Policy.Profiles.Count != UniqueProfilesFound)
                            {
                                foreach (var profile in Policy.Profiles)
                                {
                                    if (!profilesFound.Contains(profile.ProfileId))
                                    {
                                        throw new("Profile ID " + profile.ProfileId + " does not have an app assigned to it.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            List<int> AutoLaunchProfiles = new();

                            int UniqueProfilesFound = 0;
                            List<int> profilesFound = new();

                            foreach (var app in Policy.AllowedApps)
                            {
                                if (string.IsNullOrEmpty(app.AppPath))
                                {
                                    throw new("You have app(s) that are missing their AUMID or App Path");
                                }

                                bool validAumid = IsAumid(app.AppPath);
                                bool validPath = IsAbsoluteWindowsPath(app.AppPath);

                                var expanded = Environment.ExpandEnvironmentVariables(app.AppPath);
                                bool expandedPath = IsAbsoluteWindowsPath(expanded);

                                if (!profilesFound.Contains(app.ProfileId))
                                {
                                    profilesFound.Add(app.ProfileId);
                                    UniqueProfilesFound++;
                                }


                                if (!(validAumid || validPath || expandedPath))
                                {
                                    if (!validAumid && !validPath && !expandedPath)
                                    {
                                        throw new(app.AppPath + " is not a valid AUMID or App Path.");
                                    }
                                    else if (!validAumid)
                                    {
                                        throw new(app.AppPath + " is not a valid AUMID.");
                                    }
                                    else if (!validPath)
                                    {
                                        throw new(app.AppPath + " is not a valid App Path.");
                                    }
                                }
                                else if (app.ProfileId == 0)
                                {
                                    throw new(app.AppPath + " does not have a valid ProfileId");
                                }
                                else if (app.AutoLaunch)
                                {
                                    if (!AutoLaunchProfiles.Contains(app.ProfileId))
                                    {
                                        AutoLaunchProfiles.Add(app.ProfileId);
                                    }
                                    else
                                    {
                                        throw new("You can only have one AutoLaunch app per profile. Profile ID " + app.ProfileId + " has multiple AutoLaunch apps.");
                                    }
                                }
                            } // End of Foreach Loop

                            if (Policy.Profiles.Count != UniqueProfilesFound)
                            {
                                foreach (var profile in Policy.Profiles)
                                {
                                    if (!profilesFound.Contains(profile.ProfileId))
                                    {
                                        throw new("Profile ID " + profile.ProfileId + " does not have any apps assigned to it.");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IsValid = false;
                        if (page == 1337)
                        {
                            ValidationMessage = $"\nPage: Applications\nError: {ex.Message}";
                        }
                        else
                        {
                            ValidationMessage = $"\nError: {ex.Message}";
                        }
                        return;
                    }
                }
                
                if (page == 3 || page == 1337)// Validate Start Pins
                {
                    try
                    {
                        // Validate accounts (example: must have a name)
                        foreach (var pin in Policy.StartMenuPins)
                        {
                            if (string.IsNullOrWhiteSpace(pin.Path))
                            {
                                throw new("You have a Start Pin(s) that is missing an AppID or Link Path");
                            }

                            bool validAumid = IsAumid(pin.Path);
                            bool validPath = IsAbsoluteWindowsPath(pin.Path);

                            var expanded = Environment.ExpandEnvironmentVariables(pin.Path);
                            bool expandedPath = IsAbsoluteWindowsPath(expanded);

                            if (!(validPath || validAumid || expandedPath))
                            {
                                throw new(pin.Path + " - Invalid AppId or Link Path");
                            }
                            else if (pin.ProfileId == 0)
                            {
                                throw new(pin.Path + " does not have a valid ProfileId");
                            }
                            else if ((validPath || expandedPath) && !LnkRegex.IsMatch(pin.Path))
                            {
                                throw new(pin.Path + " - All Desktop Link Paths must be a .lnk file");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IsValid = false;

                        if (page == 1337)
                        {
                            ValidationMessage = $"\nPage: Start Menu\nError: {ex.Message}";
                        }
                        else
                        {
                            ValidationMessage = $"\nError: {ex.Message}";
                        }

                       
                        return;
                    }
                }
                
                if (page == 4 || page == 1337) // Validate Task Pins
                {
                    try
                    {
                        // Validate accounts (example: must have a name)
                        foreach (var pin in Policy.TaskbarPins)
                        {

                            if (string.IsNullOrWhiteSpace(pin.Path))
                            {
                                throw new("You have Taskbar Pin(s) with a missing AppId or Path");
                            }

                            bool validAumid = IsAumid(pin.Path);
                            bool validPath = IsAbsoluteWindowsPath(pin.Path);
                            var expanded = Environment.ExpandEnvironmentVariables(pin.Path);
                            bool expandedPath = IsAbsoluteWindowsPath(expanded);


                            if (!(validPath || validAumid || expandedPath))
                            {
                                throw new(pin.Path + " - Invalid AppId or Path");
                            }
                            else if (pin.ProfileId == 0)
                            {
                                throw new(pin.Path + " does not have a valid ProfileId");
                            }
                            else if ( (validPath || expandedPath) && !LnkRegex.IsMatch(pin.Path))
                            {
                                throw new(pin.Path + " - All Taskbar Pins using Desktop Link Paths must be a .lnk file");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IsValid = false;
                        if (page == 1337)
                        {
                            ValidationMessage = $"\nPage: Taskbar\nError: {ex.Message}";
                        }
                        else
                        {
                            ValidationMessage = $"\nError: {ex.Message}";
                        }
                        
                        return;
                    }
                }
                
                if (page == 5 || page == 1337)// Validate Restrictions
                {
                    try
                    {
                        // Validate accounts (example: must have a name)
                        foreach (var restriction in Policy.ExplorerRestrictions)
                        {
                            if(restriction.ProfileId == 0)
                            {
                                throw new("You have one or more restrictions missing a Profile ID");
                            }

                            if (restriction.RestrictAll && (restriction.NoRestrictions || restriction.AllowDownloads || restriction.AllowRemovableMedia))
                            {
                                throw new("Profile" + restriction.ProfileId + ": You can't have Restrict All selected with any other restrictions.");
                            }

                            if (restriction.NoRestrictions && (restriction.RestrictAll || restriction.AllowDownloads || restriction.AllowRemovableMedia))
                            {
                                throw new("Profile" + restriction.ProfileId + ": You can't have No Restrictions selected with any other restrictions.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IsValid = false;
                        if (page == 1337)
                        {
                            ValidationMessage = $"\nPage: Restrictions\nError: {ex.Message}";
                        }
                        else
                        {
                            ValidationMessage = $"\nError: {ex.Message}";
                        }
                        
                        return;
                    }

                }
                
                if (page == 6 || page == 1337) // Validate Accounts
                {
                    try
                    {
                        // Validate accounts (example: must have a name)
                        foreach (var acc in Policy.LogonAccounts)
                        {
                            if (acc.Type == null && acc.Type == null && acc.Location == null)
                            {
                                throw new("You have one or more blank rows");
                            }
                            else if (acc.Type == null)
                            {
                                throw new("Every User/Group must have a type");
                            }
                            else if (Policy.Profiles.Count == 0)
                            {
                                throw new("User/Group is missing a Profile Id. Make sure you added one on the Mode blade.");
                            }
                            else if ((acc.Type == "Group" || acc.Type == "User") && (acc.AccountName == null || acc.Location == null))
                            {
                                throw new("One or more Users/Groups are missing their Identifier and Location");
                            }
                            else if (string.IsNullOrWhiteSpace(acc.AccountName) && acc.Type.ToLower() != "global" && acc.Type.ToLower() != "autologon")
                            {
                                throw new("Account or Group Identifier missing. All Accounts and Groups (except for Global or AutoLogon) must have an identifier.");
                            }

                            var identityType = IdentityFormatClassifier.Classify(acc.AccountName);
                            /*
                             * 
LocalFormat,
    ActiveDirectoryFormat,
    EntraFormat,
    UnknownFormat

                             * 
                             */

                            // Further User/Group validation
                            if(acc.Type == "User" || acc.Type == "Group")
                            {
                                    // Local Account/Group
                                    if (identityType != AccountFormatType.EntraFormat && acc.Location == "Entra ID")
                                    {
                                        throw new(acc.AccountName + " was not in the proper format for an Entra ID " + acc.Type );
                                    }
                                    else if (identityType != AccountFormatType.LocalFormat && acc.Location == "Local")
                                    {
                                        throw new(acc.AccountName + " was not in the proper format for a Local " + acc.Type );
                                    }
                                    else if (identityType != AccountFormatType.ActiveDirectoryFormat && acc.Location == "Active Directory")
                                    {
                                        throw new(acc.AccountName + " was not in the proper format for an AD" + acc.Type );
                                    }
                                    else if (identityType == AccountFormatType.UnknownFormat)
                                    {
                                        throw new("Account (" + acc.AccountName + ") was not detected as a valid format.");
                                    }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        IsValid = false;
                        if (page == 1337)
                        {
                            ValidationMessage = $"\nPage: Accounts\nError: {ex.Message}";
                        }
                        else
                        {
                            ValidationMessage = $"\nError: {ex.Message}";
                        }
                        
                        return;
                    }
                }
                
                if (page == 10 ) // Merge Page
                {
                    try
                    {
                        if (Policy.BlockMerge)
                        {
                            throw new("Cannot merge Single App and Multi App policies together.");
                        }
                    }
                    catch(Exception ex)
                    {
                        IsValid = false;
                        ValidationMessage = $"\nPage: Merge\nError: {ex.Message}";
                        return;
                    }

                }
            }
            catch
            {
                IsValid = false;
            }
            
        }

        private string? _selectedProfileName;
        public string? SelectedProfileName
        {
            get => _selectedProfileName;
            set
            {
                _selectedProfileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedProfileName)));
            }
        }

        public class ProfileInfo
        {
            public string ProfileGuid { get; set; } = string.Empty;
            public int ProfileId { get; set; }
        }

        public ObservableCollection<ProfileInfo> MyProfiles { get; } = new(){
            new ProfileInfo
            {
                ProfileId = 1,
                ProfileGuid = ""
            }
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}