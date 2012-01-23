using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Mubox.Model.Input;

namespace Mubox.Control.Input
{
    public static class KeyboardInputHook
    {
        static KeyboardInputHook()
        {
            hookProc = new Win32.WindowHook.HookProc(KeyboardHook);
            hookProcPtr = Marshal.GetFunctionPointerForDelegate(hookProc);
        }

        private static Win32.WindowHook.HookProc hookProc = null;
        private static IntPtr hookProcPtr = IntPtr.Zero;

        public static int KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode == 0)
                {
                    Mubox.Win32.WindowHook.KBDLLHOOKSTRUCT keyboardHookStruct = (Mubox.Win32.WindowHook.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Mubox.Win32.WindowHook.KBDLLHOOKSTRUCT));
                    if (OnKeyboardInputReceived((Win32.WM)wParam, keyboardHookStruct))
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
            return 1;
        }

        private static bool isStarted;
        private static IntPtr hHook = IntPtr.Zero;

        private static System.Threading.Thread keyboardHookCheckThread = null;

        private static object StartLock = new object();
        public static void Start()
        {
            lock (StartLock)
            {
                if (isStarted)
                    return;
                isStarted = true;

                if (keyboardHookCheckThread == null)
                {
                    keyboardHookCheckThread = new System.Threading.Thread((System.Threading.ThreadStart)KeyboardHookCheckProc);
                    keyboardHookCheckThread.IsBackground = true;
                    keyboardHookCheckThread.Priority = System.Threading.ThreadPriority.Lowest;
                    keyboardHookCheckThread.Start();
                }

                //                IntPtr nextHook = IntPtr.Zero // COMMENTED BY CODEIT.RIGHT;
                //IntPtr dwThreadId = Win32.Threads.GetCurrentThreadId();
                var modules = System.Reflection.Assembly.GetEntryAssembly().GetModules();
                IntPtr hModule = Marshal.GetHINSTANCE(modules[0]);
                System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate()
                {
                    hHook = Win32.WindowHook.SetWindowsHookEx(Win32.WindowHook.HookType.WH_KEYBOARD_LL, hookProcPtr, hModule, IntPtr.Zero);
                    if (hHook == IntPtr.Zero)
                    {
                        // failed
                        isStarted = false;
                        Debug.WriteLine("KBHOOK: Hook Failed 0x" + Marshal.GetLastWin32Error().ToString("X"));
                    }
                });
            }
        }

        private static void KeyboardHookCheckProc()
        {
            while (keyboardHookCheckThread != null)
            {
                try
                {
                    if (isStarted)
                    {
                        if (Stop())
                        {
                            Start();
                        }
                        else
                        {
                            Debug.WriteLine("Detected Stop");
                            keyboardHookCheckThread = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
                finally
                {
                    System.Threading.Thread.Sleep(3000);
                }
            }
        }

        public static bool Stop()
        {
            lock (StartLock)
            {
                if (!isStarted)
                {
                    return false;
                }
                isStarted = false;

                if (hHook != IntPtr.Zero)
                {
                    Mubox.Win32.WindowHook.UnhookWindowsHookEx(hHook);
                    hHook = IntPtr.Zero;
                }

                return true;
            }
        }

        public static event Action<KeyboardInput> KeyboardInputReceived;

        private static byte[] pressedKeys = new byte[256];

        private static Performance KeyboardInputPerformance = Performance.CreatePerformance("_KeyboardInput");
        private static Performance KeyboardHandlerPerformance = Performance.CreatePerformance("_KeyboardHandler");

        private static bool OnKeyboardInputReceived(Win32.WM wParam, Win32.WindowHook.KBDLLHOOKSTRUCT hookStruct)
        {
            // fix for 'key repeat' windows feature
            if (pressedKeys.Contains((byte)(hookStruct.vkCode & 0xFF)))
            {
                return false;
            }

            // and ignore "global desktop keys"
            Mubox.Configuration.KeySetting globalKeySetting = null;
            if (Mubox.Configuration.MuboxConfigSection.Default.Keys.TryGetKeySetting((Win32.VK)hookStruct.vkCode, out globalKeySetting) && (globalKeySetting.SendToDesktop))
            {
                return false;
            }

            // filter repeated keys, we don't rebroadcast these
            if (IsRepeatKey(hookStruct) && Mubox.Configuration.MuboxConfigSection.Default.IsCaptureEnabled && !Mubox.Configuration.MuboxConfigSection.Default.DisableRepeatKeyFiltering)
            {
                return true;
            }

            // count
            if (Performance.IsPerformanceEnabled)
            {
                KeyboardInputPerformance.Count(Convert.ToInt64(hookStruct.time));
            }

            // handle high-level
            if (KeyboardInputReceived != null)
            {
                KeyboardInput keyboardInputEventArgs = KeyboardInput.CreateFrom(wParam, hookStruct);
                {
                    Mubox.Configuration.KeySetting keySetting = globalKeySetting;
                            if (Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam != null)
                    {
                        Mubox.Configuration.ClientSettings activeClient = Mubox.Configuration.MuboxConfigSection.Default.Teams.ActiveTeam.ActiveClient;
                        if (activeClient != null)
                        {
                            activeClient.Keys.TryGetKeySetting((Win32.VK)keyboardInputEventArgs.VK, out keySetting);
                        }
                        if (keySetting != null)
                        {
                            keyboardInputEventArgs.VK = (uint)keySetting.OutputKey;
                            keyboardInputEventArgs.CAS = keySetting.OutputModifiers;
                        }
                    }
                }
                OnKeyboardInputReceivedInternal(keyboardInputEventArgs);
                return keyboardInputEventArgs.Handled;
            }

            return false;
        }

        private static bool IsRepeatKey(Win32.WindowHook.KBDLLHOOKSTRUCT hookStruct)
        {
            int vk = (int)(hookStruct.vkCode & 0xFF);
            lock (pressedKeys)
            {
                bool keyIsPressed = pressedKeys[vk] == 0x80;
                if (Win32.WindowHook.LLKHF.UP != (hookStruct.flags & Win32.WindowHook.LLKHF.UP))
                {
                    if (keyIsPressed)
                    {
                        return true;
                    }
                    else
                    {
                        pressedKeys[vk] = 0x80;
                    }
                }
                else
                {
                    if (!keyIsPressed)
                    {
                        return true;
                    }
                    else
                    {
                        pressedKeys[vk] = (byte)(Win32.IsToggled((Win32.VK)vk) ? 1 : 0);
                    }
                }
            }
            return false;
        }

        private static void OnKeyboardInputReceivedInternal(KeyboardInput e)
        {
            try
            {
                if (e != null)
                {
                    KeyboardInputReceived(e);
                    if (Performance.IsPerformanceEnabled)
                    {
                        KeyboardHandlerPerformance.Count((long)(e.CreatedTime - DateTime.Now).TotalMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}