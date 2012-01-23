using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Threading;

namespace Mubox
{
    #region INotifyPropertyChanged

    public static class NotifyPropertyChangedExtensions
    {
        public static Dispatcher UIDispatcher { get; set; }

        public static void OnPropertyChanged<T>(this T obj)
            where T : INotifyPropertyChanged
        {
            System.Diagnostics.StackFrame frame = new System.Diagnostics.StackFrame(1, false);
            var methodName = frame.GetMethod().Name;
            var propertyName = methodName.Replace("get_", "").Replace("set_", "");

            OnPropertyChanged(obj, propertyName);
        }

        public static void OnPropertyChanged<T, TResult>(this T obj, Expression<Func<T, TResult>> expr)
            where T : INotifyPropertyChanged
        {
            var propertyName = default(string);

            var memberExpression = (expr.Body as MemberExpression);
            if (memberExpression == null)
            {
                return;
            }

            OnPropertyChanged(obj, propertyName);
        }

        public static void OnPropertyChanged<T>(T obj, string propertyName)
            where T : INotifyPropertyChanged
        {
            var type = obj.GetType();

            var eventInfo = type.GetEvent("PropertyChanged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (eventInfo == null)
            {
                return;
            }

            var eventDelegate = (MulticastDelegate)type.GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);

            if (eventDelegate == null)
            {
                return;
            }

            var delegates = eventDelegate.GetInvocationList();

            if (delegates == null)
            {
                return;
            }

            try
            {
                var args = new object[] { obj, new PropertyChangedEventArgs(propertyName) };
                var dispatcher = UIDispatcher;
                if (dispatcher != null)
                {
                    foreach (Delegate dlg in delegates)
                    {
                        dispatcher.BeginInvoke(dlg, DispatcherPriority.DataBind, args);
                    }
                }
                else
                {
                    foreach (Delegate dlg in delegates)
                    {
                        dlg.DynamicInvoke(args);
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO: ex.Log()
            }
        }
    }

    #endregion
}