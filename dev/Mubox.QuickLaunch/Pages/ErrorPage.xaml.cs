using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Mubox.QuickLaunch.Pages
{
    /// <summary>
    /// Interaction logic for ErrorPage.xaml
    /// </summary>
    public partial class ErrorPage : Page
    {
        public ErrorPage()
        {
            InitializeComponent();
            retryTimer = new DispatcherTimer(DispatcherPriority.Normal);
            retryTimer.Interval = TimeSpan.FromSeconds(30);
            retryTimer.Tick += (sender, e) =>
                {
                    Page_MouseDown(null, null);
                };
            //retryTimer.Start();
            linkTryAgain.Click += (sender, e) =>
                {
                    Page_MouseDown(null, null);
                };
        }

        private DispatcherTimer retryTimer;

        private void Page_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (AppWindow.TryAgainSource != null)
                {
                    if (retryTimer.IsEnabled)
                    {
                        retryTimer.Stop();
                        NavigationService.Navigate(AppWindow.TryAgainSource);
                    }
                }
            }
            catch
            {
            }
        }
    }
}