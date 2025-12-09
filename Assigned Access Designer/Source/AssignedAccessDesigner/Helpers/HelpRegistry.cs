using System;
using System.Collections.Generic;

namespace AssignedAccessDesigner.Helpers
{
    


    public sealed class HelpInfo
    {
        public string Header { get; init; } = "Help";
        public string Description { get; init; } = "No help content available.";

        public string ModeHelpDescription = @"
Define kiosk mode behavior for Windows devices using Assigned Access.

Single App Mode:
- Locks the device to one application running full screen.
- What you can do:
  • Use UWP apps (e.g., Calculator, Photos).
  • Use Microsoft Edge in kiosk mode for web-based experiences.
- Ideal for point-of-sale systems or information kiosks.

Multi App Mode:
- Allows multiple approved apps in a controlled environment.
- What you can do:
  • Include UWP apps and desktop (Win32) apps.
  • Configure auto-launch for a primary app when the device starts.
  • Customize Start Menu and Taskbar for easy navigation.
- Suitable for shared devices or specialized workflows.

Profiles:
- Create multiple profiles with different app sets and restrictions.

Limitations:
- Single App mode does not support Start Menu or Taskbar customization.
- Multi App mode requires Windows 10/11 Enterprise or Education editions.
";

        public string AppsHelpDescription = @"
Specify which apps are available in kiosk mode.

What you can do:
- Single App:
  • Choose one UWP app or Microsoft Edge.
  • App runs full screen and cannot be closed by the user.

- Multi App:
  • Define a list of UWP apps and desktop apps.
  • Configure auto-launch for a primary app.
  • Provide a curated experience for multiple workflows.

Limitations:
- Only one Autolaunch app allowed in multi-app mode.
- Some system apps may not be available for kiosk mode.
";
        public string StartHelpDescription = @"
Control which apps appear pinned on the Start Menu in multi-app kiosk mode.

What you can do:
- Group apps into categories for easier navigation.
- Define the order of pinned apps for a consistent user experience.
- Create a simplified Start Menu tailored to kiosk tasks.

Limitations:
- Available only in multi-app mode.
- Requires a valid Start layout configuration.

Formats Supported:
- UWP Apps: Package Family Name (PFN) (e.g., Microsoft.MicrosoftEdge_8wekyb3d8bbwe)
- Desktop Apps: .LNK file path (e.g., C:\Path\To\App.lnk)
";
        public string TaskHelpDescription = @"
Define if the Taskbar is enabled and which apps are pinned to the taskbar for quick access.

What you can do:
- Pin frequently used apps for faster access.
- Combine Start Menu and Taskbar customization for a streamlined interface.

Limitations:
- Ignored in single-app mode.
- Some system apps cannot be pinned due to OS restrictions.

Formats Supported:
- UWP Apps: Package Family Name (PFN) (e.g., Microsoft.MicrosoftEdge_8wekyb3d8bbwe)
- Desktop Apps: .LNK file path (e.g., C:\Path\To\App.lnk)
";
        public string RestrictionsHelpDescription = @"
Applies security and usage restrictions to the kiosk device.

What you can do:
- Block printing or USB access for security.
- Restrict access to system settings and control panels.
- Enforce policies for a locked-down environment.

Limitations:
- Restrictions vary by Windows edition.
- Some advanced restrictions require MDM or Group Policy.
";
        public string AccountsHelpDescription = @"
Manage which accounts and groups can use kiosk mode and supports autologon.

What you can do:
- Assign a single local, domain, or Entra user account for kiosk mode.
- Configure local, domain, or Entra groups for multi-user scenarios.
- Enable autologon for unattended kiosks.
- Use Global profiles to apply to every non-administrator account

Limitations:
- Nested groups are not supported.
- Azure AD groups require internet connectivity.
- Autologon must be configured securely to avoid credential exposure.

Formats Supported:
- Azure AD/Entra ID: GUID (e.g., 12345678-90ab-cdef-1234-567890abcdef)
- Domain accounts: DOMAIN\Username
- Local accounts: .\Username or Username
";

        public string AboutHelpDescription = @"
The Assigned Access Designer is a powerful yet easy-to-use tool designed for IT administrators who need to configure Windows kiosk mode settings without manually editing XML files. This application provides a graphical interface that simplifies the creation, editing, and merging of Assigned Access configuration files.

One-Stop Configuration: Manage all kiosk settings in one place, including:
• Single App vs Multi App profiles
• Application lists
• Start Menu and Taskbar pins
• Device restrictions
• Accounts and groups (including autologon)

Multiple Profiles Support: Create and maintain multiple profiles for different scenarios or devices.

Preview changes instantly and export ready-to-use configuration files for deployment.";

        public string SaveHelpDescription = @"
Once you’ve saved your XML file, you can deploy it to Windows devices through Intune:

1) Sign in to Microsoft Intune Admin Center:
Navigate to Devices > Configuration profiles.

2) Create a New Profile:
Platform: Windows 10 and later
Profile type: Templates > Custom

3) Add OMA-URI Settings:
Name: Assigned Access Configuration
OMA-URI: ./Device/Vendor/MSFT/AssignedAccess/Configuration
Data type: String
Value: Paste the entire XML configuration file content.

4) Assign the Profile:
Target the appropriate device groups or users.

5) Monitor Deployment
Check compliance and deployment status in the Intune portal.

*Note
If the machine was already on and signed in, you will need to reboot or sign out and back in to see the changes.";


    }

    /// <summary>
    /// Central registry that maps a Page/UserControl type to contextual help text.
    /// </summary>
    public static class HelpRegistry
    {
        private static readonly Dictionary<Type, HelpInfo> _byType = new();

        /// <summary>Register help for a view type.</summary>
        public static void Register<TView>(string header, string description)
            => _byType[typeof(TView)] = new HelpInfo { Header = header, Description = description };

        /// <summary>Get help by view instance or type; null if none.</summary>
        public static HelpInfo? Get(object? viewOrType)
        {
            var t = viewOrType switch
            {
                null => null,
                Type type => type,
                var obj => obj.GetType()
            };
            if (t is null) return null;

            return _byType.TryGetValue(t, out var info) ? info : null;
        }
    }

    /// <summary>
    /// Optional interface: a view can provide its own context text dynamically.
    /// If implemented, this overrides registry text.
    /// </summary    /// </summary>
    public interface IHelpContextProvider
    {
        HelpInfo GetHelp();
    }

}
