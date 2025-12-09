using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssignedAccessDesigner.Helpers
{
    internal class FileHelper
    {
        public static string CleanPath(string path)
        {
            List<char> invalidChars = new (System.IO.Path.GetInvalidPathChars());
            List<char> invalidXmlChars = new List<char> { '<', '>', '/', '|', '?', '*', '&' };

            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            // Trim whitespace
            path = path.Trim();

            // Remove surrounding quotes if present
            if (path.StartsWith("\"") && path.EndsWith("\""))
            {
                path = path[1..^1];
            }
            else if (path.StartsWith("“") && path.EndsWith("”"))
            {
                path = path[1..^1];
            }
            else if (path.StartsWith("‘") && path.EndsWith("’"))
            {
                path = path[1..^1];
            }
            else if (path.StartsWith("'") && path.EndsWith("'"))
            {
                path = path[1..^1];
            }

            // Look for single quotes as well just in case one was left by accident
            if (path.StartsWith('"'))
            {
                path = path[1..];
            }
            else if (path.EndsWith('"'))
            {
                path = path[..^1];
            }
            else if (path.StartsWith("'"))
            {
                path = path[1..];

            }
            else if (path.EndsWith("'"))
            {
                path = path[..^1];
            }

            // Remove invalid characters
            foreach (char invalidChar in invalidChars)
            {
                path = path.Replace(invalidChar.ToString(), string.Empty);
            }

            foreach (char invalidChar in invalidXmlChars)
            {
                path = path.Replace(invalidChar.ToString(), string.Empty);
            }

            return path;
        }
    }
}
