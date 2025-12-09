using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssignedAccessDesigner
{
    public interface INavigationBarController
    {
        // Controls if the Nav Bar is visible
       void ConfigureNavigationBar(bool showBar, bool showPrevious, bool showNext);

        // Control if the Nav Bar buttons are enabled
        void ConfigureNavigationButtons(bool enablePrevious, bool enableNext);
    }

    internal class NavigationInterface
    {
    }
}
