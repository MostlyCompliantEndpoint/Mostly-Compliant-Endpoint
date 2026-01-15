using AssignedAccessDesigner.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssignedAccessDesigner.Services
{
    public static class AppxPackageService
    {
        public static IEnumerable<AppxPackage> GetInstalledUwpApps()
        {
            var pm = new Windows.Management.Deployment.PackageManager();
            var packages = pm.FindPackagesForUser(string.Empty);

            foreach (var pkg in packages)
            {
                if (pkg.IsFramework || pkg.IsResourcePackage || pkg.IsBundle)
                    continue;

                yield return new AppxPackage
                {
                    DisplayName = string.IsNullOrWhiteSpace(pkg.DisplayName) ? pkg.Id.Name : pkg.DisplayName,
                    FullName = pkg.Id.FullName,
                    PublisherDisplayName = pkg.PublisherDisplayName,
                    Architecture = pkg.Id.Architecture.ToString(),
                    Version = pkg.Id.Version.ToString()
                };
            }
        }

        // Helper: filter and update the observable collection in-place
        public static void FilterApps(
        IReadOnlyList<AppxPackage> allApps,
        ObservableCollection<AppxPackage> filteredApps,
        string query)
        {
            // Compute new filtered set
            IEnumerable<AppxPackage> newSet =
                string.IsNullOrWhiteSpace(query)
                ? allApps
                : allApps.Where(a =>
                      a.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                      a.PublisherDisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                      a.FullName.Contains(query, StringComparison.CurrentCultureIgnoreCase));

            // Apply minimal-diff updates for smoother UI (remove not present, add missing)
            var newList = newSet.ToList();

            // Remove
            for (int i = filteredApps.Count - 1; i >= 0; i--)
            {
                if (!newList.Any(a => a.FullName == filteredApps[i].FullName))
                    filteredApps.RemoveAt(i);
            }

            // Add (keep order consistent with newList)
            foreach (var item in newList)
            {
                if (!filteredApps.Any(a => a.FullName == item.FullName))
                {
                    // Insert by order
                    int insertIndex = Enumerable.Range(0, filteredApps.Count)
                        .FirstOrDefault(idx =>
                            StringComparer.CurrentCultureIgnoreCase.Compare(
                                item.DisplayName, filteredApps[idx].DisplayName) < 0);
                    if (insertIndex >= 0 && insertIndex < filteredApps.Count)
                        filteredApps.Insert(insertIndex, item);
                    else
                        filteredApps.Add(item);
                }
            }
        }
    }
}
