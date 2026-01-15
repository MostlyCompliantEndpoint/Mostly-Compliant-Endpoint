using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AssignedAccessDesigner.Models
{
    public enum KioskMode { SingleApp, MultiApp }

    public class AssignedAccessPolicy
    {
        public KioskMode Mode { get; set; } = KioskMode.SingleApp;

        public List<SingleApp> SingleApp { get; set; } = new();

        public int numProfiles { get; set; } = 1;

        public List<AllowedApp> AllowedApps { get; set; } = new();
        public List<StartMenuPin> StartMenuPins { get; set; } = new();

        public List<TaskbarPin> TaskbarPins { get; set; } = new();

        public bool TaskbarEnabled { get; set; } = true;

        public List<int> TaskbarProfilesEnabled { get; set; } = new();

        public List<TaskbarEnabled> TaskbarEnabledList { get; set; } = new();

        public List<ExplorerRestriction> ExplorerRestrictions { get; set; } = new();
        public List<LogonAccount> LogonAccounts { get; set; } = new();

        public Guid ProfileGuid { get; set; }

        public List<string> CurrentErrors { get; set; } = new();

        public List<Profile> Profiles { get; set; } = new();

        public List<MergePolicy> PoliciesToMerge { get; set; } = new();

        public bool BlockMerge { get; set; } = false;   

        public Namespaces namespaces { get; set; } = new();
    }

    public class Namespaces
    {
        public bool v3 { get; set; } = false;

        public bool v4 { get; set; } = false;

        public bool v5 { get; set; } = false;

        public bool rs5 { get; set; } = false;  
    }

    public class AllowedApp
    {
        public bool AutoLaunch { get; set; }
        public int ProfileId { get; set; } = 1; // e.g., "C:\Program Files\app\app.exe"
        public string? AppPath { get; set; }   // e.g., "Publisher.App_123abc!App"

        public string? AutoLaunchArgs { get; set; }

        public List<int> ProfileListIds { get; set; } = new();
    }

    public class Profile
    {
        public string? ProfileGuid { get; set; }
        public int ProfileId { get; set; }

        public string? ProfileName { get; set; }
    }

    public class StartMenuPin
    {
        public string? UwpAumid { get; set; }
        public string? DesktopPath { get; set; }

        public string? Path { get; set; }

        public int ProfileId { get; set; } = 1;

        public List<int> ProfileListIds { get; set; } = new();
    }

    public class SingleApp
    {
        public List<int> ProfileListIds { get; set; } = new();
        public int ProfileId { get; set; } = 1;
        public string? AppPath { get; set; }
        public string? AppArguments { get; set; }
        public string? BreakoutSequence { get; set; }
    }

    public class TaskbarPin
    {

        public int ProfileId { get; set; } = 1;

        public string? Path { get; set; }

        public List<int> ProfileListIds { get; set; } = new();
    }

    public class TaskbarEnabled
    {

        public int ProfileId { get; set; } = 1;

        public bool IsTaskbarEnabled { get; set; } = true;
    }

    public class ExplorerRestriction
    {
        public bool AllowDownloads { get; set; } = false;
        public bool AllowRemovableMedia { get; set; }  = false;

        public bool NoRestrictions { get; set; }  = false;
        public bool RestrictAll { get; set; } = false;

        public int ProfileId { get; set; } = 1;
    }

    public class LogonAccount
    {
        public string AccountName { get; set; } = string.Empty; // local group/user or domain group

        public int ProfileId { get; set; } = 1;

        public string Type { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public List<int> ProfileListIds { get; set; } = new();

    }

    public class MergePolicy
    {
        public XDocument policy = new();
        public string fileName = "";
    }
}