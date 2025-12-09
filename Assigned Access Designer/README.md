# The Mostly Compliant Endpoint – Assigned Access Designer  

## Quick Start 
Follow these steps to get started with Assigned Access Designer:  

1. **Install & Launch**  
   - Ensure you have the latest version of Windows 10/11.  
   - Download and open Assigned Access Designer.  

2. **Create or Edit a Policy**  
   - On the **Welcome Page**, choose to create a new policy, edit an existing one, or merge policies.  
   - Single App and Multi App policies cannot be merged.  

3. **Select Mode**  
   - Choose **Single App** (one app per profile) or **Multi App** (restricted environment with multiple apps).  
   - Add profiles and give them names for easy identification.  
   - Single App supports ONLY MS Edge (Desktop App) or UWP applications.

4. **Add Apps**  
   - Use the UWP or Exe pickers to select apps already installed on your machine.  
   - Remember: apps must be deployed before enabling the profile.  

5. **Customize Experience**  
   - Configure Start Menu pins (Multi App only).  
   - Enable/disable the taskbar and add taskbar pins.  
   - Apply restrictions (USB, Downloads folder, or full lockdown).  

6. **Assign Accounts & Groups**  
   - Map accounts and groups to profiles.  
   - Add AutoLogon accounts for kiosk experiences.  
   - Use Global profiles for all standard users.  

7. **Save & Deploy**  
   - Preview the XML before saving.  
   - Save the XML file and deploy it via **Intune**.  
   - Final validation ensures schema and app configuration are correct.  

---

## Overview
Managing kiosk and restricted user experiences in Windows often requires creating complex **Assigned Access XML files**. This tool simplifies the process by allowing IT admins to **create, edit, and merge XML configurations** without the headaches of manual coding.

With support for **single‑app** and **multi‑app kiosks**, multiple profiles, and customization options like Start menu pins and Taskbar settings, this tool helps organizations streamline endpoint administration and deploy consistent, secure experiences across devices.

---

## Features
- **Create** – Generate new Assigned Access XML files from scratch.
- **Edit** – Modify existing XML files safely and easily.
- **Merge** – Combine multiple XML files into one unified configuration.
- **Single‑App Kiosk Support**  
  - Lock devices to a single UWP app or Microsoft Edge.  
  - Configure launch arguments and breakout sequences.  
  - Select UWP applications from your current device.
- **Multi‑App Kiosk Support**  
  - Deploy curated sets of UWP and desktop apps.  
  - Configure autolaunch (one per profile) with arguments.  
  - Customize Start menu pins and Taskbar layouts.  
  - Select UWP applications from your current device.
  - Select Win32 Desktop applications from your current device.
- **Multiple Profiles** – Assign different apps and restrictions based on users or groups.  
- **File Explorer Restrictions** – Block or limit access to Downloads, removable storage, or specific folders.  

---

## Supported Accounts & Groups
Profiles can be assigned to:
- Local user accounts
- Active Directory users and groups
- Microsoft Entra (Azure AD) users and groups
- Global profiles (applied to all non‑admin accounts)

---

## Applications & Deployment Features
| Feature                 | Single‑App Kiosk                                  | Multi‑App Kiosk                                      |
|-------------------------|---------------------------------------------------|------------------------------------------------------|
| **Supported Apps**      | UWP apps or Microsoft Edge (full screen)          | UWP apps and traditional desktop (Win32) apps        |
| **Autolaunch**          | Not applicable                                    | One app per profile can be set to auto‑launch        |
| **Autolaunch Arguments**| Not applicable                                    | Startup arguments supported for the auto‑launch app  |
| **Breakout Sequence**   | Supported                                         | Not Applicable                                       |
| **Start Menu Pins**     | Not applicable                                    | Supported                                            |
| **Taskbar Settings**    | Not applicable                                    | Supported                                            |
| **Multiple Profiles**   | Supported                                         | Supported                                            |

---

## Full Walkthrough  
This guide walks you through using **Assigned Access Designer** to create, edit, and manage kiosk policies.  

---

### Welcome Page  
Choose one of the following options:  
- **Create a new policy**  
- **Edit an existing policy**  
- **Merge existing policies**  

Note: You cannot merge Single App and Multi App policies together.  

---

### Mode Page  
Select the type of experience you want to configure:  
- **Single App** – Restricts the user to one application.  
- **Multi App** – Provides a restricted environment with multiple applications.  

You can also:  
- Add multiple profiles to your policy.  
- Assign names to profiles for easier identification.  

---

### Apps Page  
Define which applications are available in each profile:  
- Use the **UWP picker** to select Universal Windows Platform apps.  
- Use the **Exe picker** to select desktop applications already installed on your machine.  

Important rules:  
- Apps **must be deployed to the device** before enabling the profile.  
- **Single App policies**: exactly one application per profile (required).  
- **Multi App policies**: at least one application per profile.  

---

### Start Menu (Multi App Policies Only)  
Configure Start Menu pins:  
- Pins must be **UWP applications** or **.LNK files** for desktop apps.  

---

### Taskbar  
Decide whether the taskbar is enabled or disabled for each profile.  
You can also add Taskbar pins:  
- Pins must be **UWP applications** or **.LNK files** for desktop apps.  

---

### Restrictions  
Apply restrictions to control device usage:  
- Limit access to **USB devices**  
- Restrict access to the **Downloads folder**  
- **Restrict everything** or **restrict nothing**  

Restrictions can be configured **per profile**.  

---

### Accounts and Groups  
Assign accounts and groups to profiles for a customized experience:  
- Multiple accounts and groups can be mapped to different profiles.  
- **AutoLogon accounts** automatically sign in to the kiosk experience.  
- **Global profiles** apply to all standard users on the machine.  

Use the **Help** button in the wizard to see examples of required formats.  

---

### Save  
Before saving, you can preview the generated XML.  
- Save the XML to your device for deployment via **Intune**.  
- During save, **final app validation** and **schema validation** ensure the configuration is correct.  

---

## Deploying with Intune
1. Export your XML file from the tool.  
2. In the Intune admin center, navigate to:  
   **Devices > Configuration profiles > Create profile**  
3. Select **Windows 10 and later** as the platform, and **Custom** as the profile type.  
4. Add a custom OMA‑URI setting pointing to:  
   `./Device/Vendor/MSFT/AssignedAccess/Configuration`  
5. Paste your XML content as the value.  
6. Assign the profile to the appropriate devices or groups.  

Intune will push the configuration to devices, enforcing the kiosk or restricted experience automatically.

---

## License
This project is released under the MIT License. See [LICENSE](LICENSE) for details.

---
