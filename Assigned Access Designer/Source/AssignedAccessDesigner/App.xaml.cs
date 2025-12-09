using Microsoft.UI.Xaml;

namespace AssignedAccessDesigner
{
    public partial class App : Application
    {

        public static INavigationBarController? NavController { get; private set; }

        public static Window? MainWindow { get; private set; }

        public static ViewModels.AppSettings? Settings { get; private set; }
        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Settings = new ViewModels.AppSettings();

            MainWindow = new MainWindow();
            NavController = (INavigationBarController)MainWindow;
            MainWindow.Activate();
        }
    }
}