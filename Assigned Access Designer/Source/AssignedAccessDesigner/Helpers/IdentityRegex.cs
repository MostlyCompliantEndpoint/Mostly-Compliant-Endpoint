
using System;
using System.Text.RegularExpressions;

namespace AssignedAccessDesigner.Helpers;

public static class IdentityRegex
{
    // MACHINE\user or DOMAIN\user – generic prefix\name
    private static readonly Regex PrefixSamRegex =
        new(@"^(?<prefix>[A-Za-z0-9._-]+)\\(?<name>[A-Za-z0-9._$-]{1,20})$",
            RegexOptions.Compiled);

    private static readonly Regex LocalNameRegex =
        new(@"(?<name>[A-Za-z0-9._$-]{1,20})$",
                        RegexOptions.Compiled);

    // UPN: user@domain.tld (basic)
    private static readonly Regex UpnRegex =
        new(@"^(?<user>[A-Za-z0-9._%+\-]+)@(?<domain>[A-Za-z0-9.-]+\.[A-Za-z]{2,})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // onmicrosoft UPN (typical Entra default domain)
    private static readonly Regex OnMicrosoftUpnRegex =
        new(@"^[A-Za-z0-9._%+\-]+@(?<tenant>[A-Za-z0-9-]+)\.onmicrosoft\.com$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // AzureAd Prefix 
    private static readonly Regex AzureAdRegex =
        new(@"AzureAd\\",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Entra objectId (GUID)
    private static readonly Regex GuidRegex =
        new(@"^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$",
            RegexOptions.Compiled);

    // SID (generic)
    private static readonly Regex SidRegex =
        new(@"^S-\d+(-\d+)+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Well-known local authorities
    private static readonly Regex LocalAuthorityRegex =
        new(@"^(NT AUTHORITY|BUILTIN)\\.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Returns true if the string *format* suggests a local (SAM) identity:
    /// - MACHINE\user where MACHINE == Environment.MachineName
    /// - NT AUTHORITY\... or BUILTIN\...
    /// - SID that looks local/system (heuristic only)
    /// </summary>
    public static bool IsLocalByFormat(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) return false;

        // NT AUTHORITY\... or BUILTIN\...
        if (LocalAuthorityRegex.IsMatch(identity)) return true;

        // MACHINE\user form: treat as local only if prefix == actual machine name (case-insensitive)
        var m = PrefixSamRegex.Match(identity);
        if (m.Success)
        {
            var prefix = m.Groups["prefix"].Value;
            // Compare against current machine name
            if (prefix.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) || prefix == ".")
                return true;
        }

        var n = LocalNameRegex.Match(identity);
        if (n.Success)
        {
            return true;
        }

        // SIDs cannot be reliably classified as local vs domain by regex alone.
        // If you want to treat any SID as "local-like" format, you could return true here.
        // Keeping it conservative:
        // return SidRegex.IsMatch(identity);

        return false;
    }

    /// <summary>
    /// Returns true if the string *format* suggests on-prem AD style:
    /// - DOMAIN\samAccountName (prefix not equal to machine name)
    /// - UPN with a non-onmicrosoft domain (heuristic; custom domains may be Entra too)
    /// </summary>
    public static bool IsActiveDirectoryByFormat(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) return false;

        // DOMAIN\samAccountName where prefix != machine name
        var m = PrefixSamRegex.Match(identity);
        if (m.Success)
        {
            var prefix = m.Groups["prefix"].Value;
            if (!prefix.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // UPN with custom (non-onmicrosoft) domain → heuristic for AD
        // Note: Verified custom domains are often used by Entra tenants too.
        var upn = UpnRegex.Match(identity);
        if (upn.Success && !OnMicrosoftUpnRegex.IsMatch(identity))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the string *format* suggests Entra:
    /// - UPN ending with *.onmicrosoft.com
    /// - GUID matching Entra objectId pattern
    /// </summary>
    public static bool IsEntraByFormat(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity)) return false;

        if (OnMicrosoftUpnRegex.IsMatch(identity)) return true;
        if (GuidRegex.IsMatch(identity)) return true;
        if(AzureAdRegex.IsMatch(identity)) return true;

        // You could also treat UPNs with verified custom domains as Entra-like,
        // but that overlaps with on-prem AD; best left to actual directory checks.

        return false;
    }
}