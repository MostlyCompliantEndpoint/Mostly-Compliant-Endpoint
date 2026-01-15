---
title: "Intune Policy Search: Find Any Intune Setting in Seconds"
tags: [Intune, PowerShell, Microsoft Graph, Endpoint Management, Admin Tools]
description: |
  A PowerShell-based WPF tool for searching and exporting Intune policy settings across Settings Catalog, Templates, Compliance, and ADMX. Includes setup, usage, troubleshooting.
---

# Intune Policy Search: Find Any Intune Setting in Seconds

If you’ve ever tried to answer *“Where is this setting configured?”* across a sprawl of Intune policies—Settings Catalog, Templates, Compliance, and ADMX—you know it can mean a lot of clicking, exporting, and guesswork. **Intune Policy Search** eliminates that pain.

This WPF‑based PowerShell tool connects to Microsoft Graph, pulls your Intune policies, and lets you **search across policy types** from a single search box. It returns **Policy Type, Platform, Policy Name, Policy GUID, Setting Found, and Value**—and you can export results to CSV with one click.

---

## How can it help you?

- **One search, all policy types**: Settings Catalog, *legacy* Device Configuration templates, Device Compliance, and ADMX‑backed policies—all at once.
- **Fast scoping**: Instantly pinpoint *where* a setting is configured and *what* value it uses.
- **Zero Graph scripting required**: Under the hood it uses Graph; on the surface it’s a clean UI.
- **Works in Commercial & GCC High**: Includes support for a large number of environments.
- **Runs almost anywhere**: Supported on **PowerShell 7+** and **Windows PowerShell 5.1**.
- **Wildcard support**: Enter `*` to return **all configured settings** for quick auditing sweeps.

---

## What the Tool Looks For (and How)

- **Settings Catalog**: Searches CSP definition IDs (you’ll see the full CSP name in the *Setting* column).
- **Configuration Templates (legacy)**: Parses the template object for configured (and sometimes default-stored) values as Graph returns them.
- **Compliance Policies**: Surfaces which compliance conditions are active and how they’re set.
- **ADMX (Administrative Templates)**: Shows imported ADMX settings and their configured values.

> Note: The current release targets setting names/identifiers for matching (not policy display names or arbitrary value text). That makes it perfect for “find the CSP / GPO setting” workloads.

---

## Prerequisites

- **PowerShell**: Windows PowerShell 5.1 or PowerShell 7+.
- **Modules**: Microsoft Graph PowerShell SDK (Authentication module at minimum).

```Powershell
Install-Module Microsoft.Graph -Scope CurrentUser

```

- **Permissions**: Read access to Intune configuration via Microsoft Graph: 
    - **Delegated**: `DeviceManagementConfiguration.Read.All`
    - **Application (app-only)**: `DeviceManagementConfiguration.Read.All` (admin consent required)

---

## Getting Started (Two Authentication Options)

You can connect **interactively** (delegated) or via **app-only** (client secret). Both are built into the UI.

### Option 1 — App registration (App‑only) authentication

1. **Register an app**
    - Go to **Microsoft Entra admin center** → *App registrations* → **New registration**.
    - **Name**: e.g., `GraphAutomationApp`
    - **Supported account types**: *Single tenant* (recommended)
    - **Redirect URI**: Not required for client credentials (you can leave blank).
2. **Capture IDs**
    - **Application (client) ID** and **Directory (tenant) ID**.
3. **Create a client secret**
    - *Certificates & secrets* → **New client secret** → copy the **Value** immediately.
4. **Grant Microsoft Graph permissions**
    - *API permissions* → **Add a permission** → Microsoft Graph → **Application permissions**
    - Add **DeviceManagementConfiguration.Read.All** → **Grant admin consent**.
5. **Populate the script parameters**
    - Open `IntunePolicySearch.ps1` and set `TenantId`, `ClientId`, and `ClientSecret` (or read secret from an env var like `$env:GraphKey`).

> Security tip: Prefer storing the client secret in a secret store or environment variable over hardcoding. Rotate the secret on a regular schedule.

### Option 2 — Interactive sign‑in (Delegated)

1. Launch the script.

```Powershell
# Run
.\IntunePolicySearch.ps1
```

1. If you’re in **GCC High**, check the **GCC High** box in the UI. 
    - **Pro tip:** Enter your GCC‑H **Tenant ID** in the script header before running to prevent WAM from loading a Commercial context on a Commercial device.
2. Click **Connect to Graph** in the UI.
3. After you see **Connected: …**, you’re ready to search.

> If sign‑in fails inside the UI, try connecting in a standalone PowerShell session using Connect-MgGraph. If it doesn’t work there, it won’t work in the tool either—fix the base Graph connection first.

---

## How to Use Intune Policy Search

1. **Open the tool** and **Connect to Graph** (choose GCC‑H if applicable).
2. **Enter a search string** (e.g., `RequireDeviceLock`, `EdgeHomePage`, `Defender`, etc.). 
    - **Wildcard sweep**: Use `*` to return **all configured settings** across selected policy types—handy for quick audits or environment baselining.
3. **Select policy types** to include (Device Configurations, Device Compliance, Administrative Templates).
4. (Optional) Toggle **Refresh Policies** → **On** if you want to re‑query Graph (instead of using cached results) during the session. This is only recommended if you are making changes to your policies in-between searching.
5. Click **Search**. Results populate with: 
    - **Type** (Settings Catalog, Configuration Template, Compliance, ADMX)
    - **Platform** (Windows, iOS/iPadOS, Android, etc.)
    - **Policy Name** and **Policy GUID**
    - **Setting** (CSP/ADMX name) and **Value** (normalized where possible)
6. **Export** with **Save results** (CSV).

---

## Tips, Tricks & Power‑User Patterns

- **Start broad, then narrow**: Use `Defender*` to sweep all Defender‑related CSPs, then refine with specific CSP IDs.
- **Audit mode with **`*`: Running a `*` search across all policy types gives you an instant snapshot of **everything set**—great for **merger/migration** assessments, **MSP onboarding**, and **control inventory**.
- **Cache smartly**: Leave **Refresh Policies** off to search quickly across cached policy data. Toggle **On** only when you’ve just created/edited policies and need a fresh snapshot.
- **Know your platforms**: The tool attempts to infer **Platform** per policy/result; when in doubt, cross‑check the CSP in Microsoft’s CSP docs to confirm OS scope and user/device targeting.
- **ADMX visibility**: Use the ADMX search to verify imported templates and confirm whether the expected ADMX setting/value was actually applied.
- **Compliance triage**: Filter on compliance results to quickly confirm whether key controls (like password policies, encryption, device threat level) are enabled and how they’re configured.
- **CSV for diffing**: Export today’s results, then re‑run later and diff the CSVs in Git or Power BI for **configuration drift** detection.

---

## Troubleshooting

- **Unauthorized / Forbidden when connecting**
    - Your account/app likely lacks `DeviceManagementConfiguration.Read.All`. For app‑only, ensure **admin consent** was granted.
- **Interactive auth on Commercial device with GCC‑H tenant**
    - Add **Tenant ID** in the script header and select **GCC High** in the UI to avoid WAM using a cached Commercial account context.
- **No results**
    - Verify you selected at least one policy type.
    - Remember the current release matches on **setting identifiers/names** (e.g., CSP names), not policy display names. Try broader strings or the `*` wildcard.
- **Slow on large tenants**
    - Keep **Refresh Policies = Off** and refine your search term. Only refresh when you’ve changed policies or need a re‑pull.
- **Run errors on first launch**
    - Ensure the **Microsoft Graph PowerShell SDK** is installed and you’re running **PowerShell 7 or Windows PowerShell 5.1**.

---

## Installation & Running

```Powershell
# 1) Install Microsoft Graph SDK (if needed)
Install-Module Microsoft.Graph -Scope CurrentUser

# 2) Unblock the script
Unblock-File .\IntunePolicySearch.ps1

# 3) Run
.\IntunePolicySearch.ps1

```

For **app-only** runs, pre-populate these at the top of the script:

```Powershell
$ClientId     = '<your app id>'
$TenantId     = '<your tenant id>'
$ClientSecret = '<your client secret or pull from $env:GraphKey>'

```

---

## FAQ

**Q: Does it change anything in my tenant?**

A: No. It’s **read‑only** when you grant `DeviceManagementConfiguration.Read.All`.

**Q: Is PowerShell 7 required?**

A: No. It works on **PowerShell 7+** and **Windows PowerShell 5.1**.

**Q: Does **`*`** overload the tenant?**

A: It will query all selected policy types and return all configured settings—on very large tenants this can take time. Use **Refresh Policies = Off** to leverage caching for iterative sweeps.

**Q: Can I run this in GCC High?**

A: Yes. Toggle **GCC High** in the UI and set your **Tenant ID** in the script header for the cleanest sign‑in experience.

---

## Wrap‑up

**Intune Policy Search** gives admins a single pane of glass to discover where settings live across Intune—fast. Whether you’re doing a one‑off hunt for a CSP, validating an ADMX rollout, or auditing an entire tenant with `*`, this tool saves time and reduces error.

If you have feature ideas or run into edge cases, drop an issue on GitHub or reach out via my site’s contact form. Happy searching!
