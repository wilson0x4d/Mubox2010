using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace Mubox.QuickLaunch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(
                        XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine("CoerceCultureInfo Failed for Mubox.QuickLaunch.App");
            }

            try
            {
                string muboxLogFilename = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MUBOX_" + DateTime.Now.Ticks.ToString() + ".log");
                Stream clientStream = File.Open(muboxLogFilename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                Mubox.Diagnostics.TraceListenerStreamWriter traceListenerStreamWriter = new Mubox.Diagnostics.TraceListenerStreamWriter(clientStream);
                System.Diagnostics.Trace.Listeners.Add(traceListenerStreamWriter);
                Debug.WriteLine(new string('*', 0x4d));
                Debug.WriteLine(new string('*', 0x4d));
                Debug.WriteLine(new string('*', 0x4d));
                Debug.WriteLine("Logging \"" + muboxLogFilename + "\" for Mubox.QuickLaunch.App");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                Debug.WriteLine("Logging Failed for Mubox.QuickLaunch.App");
            }

            try
            {
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                currentProcess.PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}