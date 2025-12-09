using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssignedAccessDesigner.ViewModels
{
    public sealed class AppxPackage
    {
        public string DisplayName { get; set; }
        public string FullName { get; set; }
        public string PublisherDisplayName { get; set; }
        public string Architecture { get; set; }
        public string Version { get; set; }

    }
}
