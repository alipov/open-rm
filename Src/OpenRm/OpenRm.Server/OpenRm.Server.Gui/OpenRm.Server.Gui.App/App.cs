using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace OpenRm.Server.Gui
{
    public class App : Application
    {
        private const int MinimumSplashTime = 1500; // Miliseconds
        private const int SplashFadeTime = 500;     // Miliseconds

        // with help of: http://www.wpfsharp.com/2012/02/14/how-to-display-a-splash-screen-for-at-specific-minimum-length-of-time-in-wpf/
        // deleting app.xaml: http://social.msdn.microsoft.com/Forums/pl/wpf/thread/0fdb3189-f51f-4a13-8217-1410335e5cd6
        //                    http://www.codingconvention.com/tutorials/WPF

        [STAThread()]
        static void Main()
        {
            // Step 1 - Load the splash screen
            //var splash = new SplashScreen("OpenRM.ico");
            //splash.Show(false, true);

            // Step 2 - Start a stop watch
            var timer = new Stopwatch();
            timer.Start();

            // Step 3 - Load your windows but don't show it yet
            TypeResolving.RegisterTypeResolving();
            var app = new App();
            app.Run();

            // Step 4 - Make sure that the splash screen lasts at least two seconds
            //timer.Stop();
            //int remainingTimeToShowSplash = MinimumSplashTime - (int)timer.ElapsedMilliseconds;
            //if (remainingTimeToShowSplash > 0)
            //    Thread.Sleep(remainingTimeToShowSplash);

            //// Step 5 - show the page
            //splash.Close(TimeSpan.FromMilliseconds(SplashFadeTime));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // create the bootstrapper
            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }
    }
}