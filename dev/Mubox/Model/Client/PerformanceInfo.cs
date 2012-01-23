using System.Windows;

namespace Mubox.Model.Client
{
    public class PerformanceInfo : DependencyObject
    {
        #region MainWindowTitle

        /// <summary>
        /// MainWindowTitle Dependency Property
        /// </summary>
        public static readonly DependencyProperty MainWindowTitleProperty =
            DependencyProperty.Register("MainWindowTitle", typeof(string), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((string)"Untitled"));

        /// <summary>
        /// Gets or sets the MainWindowTitle property.  This dependency property
        /// indicates Process WindowText.
        /// </summary>
        public string MainWindowTitle
        {
            get { return (string)GetValue(MainWindowTitleProperty); }
            set { SetValue(MainWindowTitleProperty, value); }
        }

        #endregion

        #region ProcessId

        /// <summary>
        /// ProcessId Dependency Property
        /// </summary>
        public static readonly DependencyProperty ProcessIdProperty =
            DependencyProperty.Register("ProcessId", typeof(int), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((int)0));

        /// <summary>
        /// Gets or sets the ProcessId property.  This dependency property
        /// indicates Process ID.
        /// </summary>
        public int ProcessId
        {
            get { return (int)GetValue(ProcessIdProperty); }
            set { SetValue(ProcessIdProperty, value); }
        }

        #endregion

        #region ProcessName

        /// <summary>
        /// ProcessName Dependency Property
        /// </summary>
        public static readonly DependencyProperty ProcessNameProperty =
            DependencyProperty.Register("ProcessName", typeof(string), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((string)""));

        /// <summary>
        /// Gets or sets the ProcessName property.  This dependency property
        /// indicates Process Name.
        /// </summary>
        public string ProcessName
        {
            get { return (string)GetValue(ProcessNameProperty); }
            set { SetValue(ProcessNameProperty, value); }
        }

        #endregion

        #region IsWindowResponding

        /// <summary>
        /// IsWindowResponding Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsWindowRespondingProperty =
            DependencyProperty.Register("IsWindowResponding", typeof(string), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((string)""));

        /// <summary>
        /// Gets or sets the IsWindowResponding property.  This dependency property
        /// indicates Responding.
        /// </summary>
        public string IsWindowResponding
        {
            get { return (string)GetValue(IsWindowRespondingProperty); }
            set { SetValue(IsWindowRespondingProperty, value); }
        }

        #endregion

        #region WorkingSet

        /// <summary>
        /// WorkingSet Dependency Property
        /// </summary>
        public static readonly DependencyProperty WorkingSetProperty =
            DependencyProperty.Register("WorkingSet", typeof(long), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((long)0));

        /// <summary>
        /// Gets or sets the WorkingSet property.  This dependency property
        /// indicates WorkingSet64.
        /// </summary>
        public long WorkingSet
        {
            get { return (long)GetValue(WorkingSetProperty); }
            set { SetValue(WorkingSetProperty, value); }
        }

        #endregion

        #region PeakWorkingSet

        /// <summary>
        /// PeakWorkingSet Dependency Property
        /// </summary>
        public static readonly DependencyProperty PeakWorkingSetProperty =
            DependencyProperty.Register("PeakWorkingSet", typeof(long), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((long)0));

        /// <summary>
        /// Gets or sets the PeakWorkingSet property.  This dependency property
        /// indicates PeakWorkingSet64.
        /// </summary>
        public long PeakWorkingSet
        {
            get { return (long)GetValue(PeakWorkingSetProperty); }
            set { SetValue(PeakWorkingSetProperty, value); }
        }

        #endregion

        #region VirtualMemorySize

        /// <summary>
        /// VirtualMemorySize Dependency Property
        /// </summary>
        public static readonly DependencyProperty VirtualMemorySizeProperty =
            DependencyProperty.Register("VirtualMemorySize", typeof(long), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((long)0));

        /// <summary>
        /// Gets or sets the VirtualMemorySize property.  This dependency property
        /// indicates VirtualMemorySize64.
        /// </summary>
        public long VirtualMemorySize
        {
            get { return (long)GetValue(VirtualMemorySizeProperty); }
            set { SetValue(VirtualMemorySizeProperty, value); }
        }

        #endregion

        #region PeakVirtualMemorySize

        /// <summary>
        /// PeakVirtualMemorySize Dependency Property
        /// </summary>
        public static readonly DependencyProperty PeakVirtualMemorySizeProperty =
            DependencyProperty.Register("PeakVirtualMemorySize", typeof(long), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((long)0));

        /// <summary>
        /// Gets or sets the PeakVirtualMemorySize property.  This dependency property
        /// indicates PeakVirtualMemorySize64.
        /// </summary>
        public long PeakVirtualMemorySize
        {
            get { return (long)GetValue(PeakVirtualMemorySizeProperty); }
            set { SetValue(PeakVirtualMemorySizeProperty, value); }
        }

        #endregion

        #region NetworkSendTime

        /// <summary>
        /// NetworkSendTime Dependency Property
        /// </summary>
        public static readonly DependencyProperty NetworkSendTimeProperty =
            DependencyProperty.Register("NetworkSendTime", typeof(long), typeof(PerformanceInfo),
                new FrameworkPropertyMetadata((long)0));

        /// <summary>
        /// Gets or sets the NetworkSendTime property.  This dependency property
        /// indicates sendCommandTimeSpent.
        /// </summary>
        public long NetworkSendTime
        {
            get { return (long)GetValue(NetworkSendTimeProperty); }
            set { SetValue(NetworkSendTimeProperty, value); }
        }

        #endregion
    }
}