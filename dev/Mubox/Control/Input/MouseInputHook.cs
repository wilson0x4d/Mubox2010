using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Mubox.Model.Input;

namespace Mubox.Control.Input
{
    public static class MouseInputHook
    {
        static MouseInputHook()
        {
            hookProc = new Win32.WindowHook.HookProc(MouseHook);
            hookProcPtr = Marshal.GetFunctionPointerForDelegate(hookProc);
            MouseInputPerformance = Performance.CreatePerformance("_MouseInput");
        }

        private static Performance MouseInputPerformance = null;
        private static Win32.WindowHook.HookProc hookProc = null;
        private static IntPtr hookProcPtr = IntPtr.Zero;

        public static int MouseHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode == 0)
                {
                    Mubox.Win32.WindowHook.MSLLHOOKSTRUCT mouseHookStruct = (Mubox.Win32.WindowHook.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Mubox.Win32.WindowHook.MSLLHOOKSTRUCT));
                    if (OnMouseInputReceived((Win32.WM)wParam, mouseHookStruct))
                    {
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            try
            {
                return Mubox.Win32.WindowHook.CallNextHookEx(hHook, nCode, wParam, lParam);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            return 0;
        }

        private static bool isStarted;
        private static IntPtr hHook = IntPtr.Zero;
        private static System.Windows.Threading.Dispatcher dispatcher;

        public static void Start()
        {
            if (isStarted)
                return;
            isStarted = true;

            if (dispatcher == null)
            {
                dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            }

            //            IntPtr nextHook = IntPtr.Zero // COMMENTED BY CODEIT.RIGHT;
            //IntPtr dwThreadId = Win32.Threads.GetCurrentThreadId();
            IntPtr hModule = Marshal.GetHINSTANCE(System.Reflection.Assembly.GetEntryAssembly().GetModules()[0]);
            hHook = Win32.WindowHook.SetWindowsHookEx(Win32.WindowHook.HookType.WH_MOUSE_LL, hookProcPtr, hModule, IntPtr.Zero);
            if (hHook == IntPtr.Zero)
            {
                // failed
                isStarted = false;
                Debug.WriteLine("MSHOOK: Hook Failed winerr=0x" + Marshal.GetLastWin32Error().ToString("X"));
            }
        }

        public static void Stop()
        {
            if (!isStarted)
                return;
            isStarted = false;

            if (hHook != IntPtr.Zero)
            {
                Mubox.Win32.WindowHook.UnhookWindowsHookEx(hHook);
                Debug.WriteLine("MSHOOK: Unhook Success.");
                hHook = IntPtr.Zero;
            }
        }

        public static event Action<MouseInput> MouseInputReceived;

        private static bool OnMouseInputReceived(Win32.WM wParam, Win32.WindowHook.MSLLHOOKSTRUCT hookStruct)
        {
            if (Win32.WindowHook.LLMHF.INJECTED == (hookStruct.flags & Win32.WindowHook.LLMHF.INJECTED))
            {
                return false;
            }

            MouseInput mouseInputEventArgs = MouseInput.CreateFrom((Win32.WM)wParam, hookStruct);
            mouseInputEventArgs.WM = wParam;
            if (Performance.IsPerformanceEnabled)
            {
                MouseInputPerformance.Count(Convert.ToInt64(mouseInputEventArgs.Time));
            }
            try
            {
                if (MouseInputReceived != null)
                {
                    MouseInputReceived(mouseInputEventArgs);
                    return mouseInputEventArgs.Handled;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            return false;
        }
    }
}