using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AssignedAccessDesigner.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HelpWindow : Window
    {
        public static Window? MainWindow { get; private set; }

        public HelpWindow()
        {
            InitializeComponent();
        }

        /// <summary>Populate header + description.</summary>
        public void Initialize(string pageName, string sectionHeader, string description)
        {
            //HeaderText.Text = $"{pageName}";
            SectionHeader.Text = sectionHeader;
            DescriptionText.Text = description;
        }

        /// <summary>Center the window on the current display.</summary>
        public void CenterOnScreen()
        {
            try
            {
                var aw = AppWindow;

                // Resize based on work area
                double height = aw.Size.Height;
                double desiredHeight = Math.Max(100, height * 0.65); // your rule
                double width = aw.Size.Width;
                double desiredwidth = Math.Max(100, width * 0.6); // your rule

                aw.Resize(new SizeInt32((int)desiredwidth, (int)desiredHeight));

                // Center on screen
                var display = DisplayArea.GetFromWindowId(aw.Id, DisplayAreaFallback.Primary);
                var work = display.WorkArea;

                int w = aw.Size.Width, h = aw.Size.Height;
                int x = work.X + (work.Width - w) / 2;
                int y = work.Y + (work.Height - h) / 2;

                aw.Move(new PointInt32(x, y));
            }
            catch { /* ignore on older SDKs */ }
        }
    }
}





    
