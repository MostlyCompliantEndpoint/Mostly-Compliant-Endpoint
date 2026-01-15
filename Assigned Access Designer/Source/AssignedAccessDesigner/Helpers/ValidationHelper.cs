using System.Text.RegularExpressions;
using System.IO;

namespace AssignedAccessDesigner.Helpers
{
    public static class ValidationHelper
    {
        // AUMID format: Publisher.App[_GUID]!App
        private static readonly Regex AumidRegex = new(@"^[A-Za-z0-9\.]+_[A-Za-z0-9]+![A-Za-z0-9\.]+$", RegexOptions.Compiled);

        public static bool IsValidAumid(string? aumid)
        {
            if (string.IsNullOrWhiteSpace(aumid)) return false;
            return AumidRegex.IsMatch(aumid);
        }

        public static bool IsValidPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                var full = Path.GetFullPath(path);
                return File.Exists(full) || Directory.Exists(full);
            }
            catch { return false; }
        }
    }
}