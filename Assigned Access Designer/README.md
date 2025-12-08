# The Mostly Compliant Endpoint – Assigned Access XML Tool

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
- **Multi‑App Kiosk Support**  
  - Deploy curated sets of UWP and desktop apps.  
  - Configure autolaunch (one per profile) with arguments.  
  - Customize Start menu pins and Taskbar layouts.  
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
