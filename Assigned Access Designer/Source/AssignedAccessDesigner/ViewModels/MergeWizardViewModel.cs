using AssignedAccessDesigner.Models;
using System;
using System.Collections.Generic;

namespace AssignedAccessDesigner.ViewModels
{
    public class MergeWizardViewModel
    {
        public List<AssignedAccessPolicy> PoliciesToMerge { get; set; } = new();
        public AssignedAccessPolicy Merged { get; set; } = new();

    }
}