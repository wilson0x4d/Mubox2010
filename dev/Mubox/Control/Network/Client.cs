using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Mubox.Control.Network
{
    public class Client
    {
        public TcpClient EndPoint { get; set; }

        public string DisplayName { get; set; }

        public IntPtr WindowStationHandle { get; set; }

        public IntPtr WindowDesktopHandle { get; set; }

        public IntPtr WindowHandle
        {
            get
            {
                return windowHandle;
            }
            set
            {
                if (windowHandle != value)
                {
                    lock (inputQueueLock)
                    {
                        windowHandle = value;
                        if (windowHandle == IntPtr.Zero)
                        {
                            WindowInputQueue = IntPtr.Zero;
                            return;
                        }
                        IntPtr windowInputQueue = WindowInputQueue;
                        if (WindowInputQueue == IntPtr.Zero)
                        {
                            if (windowHandle != IntPtr.Zero)
                            {
                                if (IntPtr.Zero == Win32.Windows.GetWindowThreadProcessId(windowHandle, out windowInputQueue))
                                {
                                    Debug.WriteLine("GWTPID Failed for set_WindowHandle(" + windowHandle + ") ");
                                }
                                WindowInputQueue = windowInputQueue;
                            }
                        }
                    }
                }
            }
        }

        private IntPtr windowHandle;

        private static object inputQueueLock = new object();
        public IntPtr WindowInputQueue { get; set; }

        public static IntPtr MyInputQueue { get; set; }

        private System.Runtime.Serialization.DataContractSerializer Serializer = new System.Runtime.Serialization.DataContractSerializer(typeof(Model.Input.InputBase),
            new Type[]
            {
                typeof(Model.Input.CommandInput),
                typeof(Model.Input.KeyboardInput),
                typeof(Model.Input.MouseInput),
            });

        public Client()
        {
        }

        public void Connect(string host, int port)
        {
            if (EndPoint == null)
            {
                EndPoint = new TcpClient();
                EndPoint.NoDelay = true;
                EndPoint.Connect(host, port);
                EndPoint.LingerState.Enabled = false;
                OnConnected();
                StartReceiving();
            }
        }

        public void Disconnect()
        {
            if (EndPoint != null)
            {
                try
                {
                    EndPoint.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
                finally
                {
                    EndPoint = null;
                }
            }
            OnDisconnected();
        }

        private void StartReceiving()
        {
            try
            {
                SocketError socketError;
                byte[] receiveBuffer = new byte[4096];
                if (EndPoint != null && EndPoint.Client != null)
                {
                    EndPoint.Client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, out socketError, BeginReceiveCallback, receiveBuffer);
                }
                else
                {
                    socketError = SocketError.NetworkDown;
                }
                if (socketError != SocketError.Success)
                {
                    throw new SocketException((int)socketError);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private byte[] fragmentBuffer = new byte[0];

        // TODO: re-add encryption

        private void BeginReceiveCallback(IAsyncResult ar)
        {
            Queue<Action> actionQueue = new Queue<Action>();
            try
            {
                TcpClient lEndPoint = EndPoint;
                Thread.MemoryBarrier();
                if (lEndPoint == null)
                {
                    Debug.WriteLine("NoEndPoint for " + this.DisplayName);
                    fragmentBuffer = new byte[0];
                    return;
                }
                int cb = lEndPoint.Client.EndReceive(ar);

                if (cb == 0)
                {
                    //Disconnect();
                    return;
                }

                byte[] receiveBuffer = ar.AsyncState as byte[];

                if (fragmentBuffer.Length > 0)
                {
                    var temp = new byte[cb + fragmentBuffer.Length];
                    fragmentBuffer.CopyTo(temp, 0);
                    Array.ConstrainedCopy(receiveBuffer, 0, temp, fragmentBuffer.Length, cb);
                    fragmentBuffer = new byte[0];
                    receiveBuffer = temp;
                }
                else
                {
                    var temp = new byte[cb];
                    Array.ConstrainedCopy(receiveBuffer, 0, temp, 0, cb);
                    receiveBuffer = temp;
                }

                Debug.WriteLine("MCNC: receiveBuffer=" + System.Text.Encoding.ASCII.GetString(receiveBuffer));

                using (var stream = new System.IO.MemoryStream(receiveBuffer))
                {
                    using (var reader = new System.IO.BinaryReader(stream))
                    {
                        while (stream.Position < stream.Length && stream.ReadByte() == 0x1b)
                        {
                            var lastReadPosition = stream.Position - 1;
                            object o = null;
                            try
                            {
                                var len = reader.ReadUInt16();
                                byte[] payload = new byte[len];
                                stream.Read(payload, 0, payload.Length);
                                using (var payloadStream = new System.IO.MemoryStream(payload))
                                {
                                    o = Serializer.ReadObject(payloadStream);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("MCNC: lastReadPosition=" + lastReadPosition + " currentPosition=" + stream.Position);

                                Debug.WriteLine(ex.Message);
                                Debug.WriteLine(ex.StackTrace);
                                // TODO: log/debug exception types thrown here, not documented on MSDN and it's not clear how to handle a buffer underrun in ReadObject
                                // TODO: if the exception is due to an unknown type, the fragmentBuffer logic will result in a deadlock on the network (always retrying a bad object)
                                o = null;
                            }

                            if (o == null)
                            {
                                stream.Seek(lastReadPosition, System.IO.SeekOrigin.Begin);
                                fragmentBuffer = new byte[stream.Length - lastReadPosition];
                                stream.Read(fragmentBuffer, 0, fragmentBuffer.Length);
                                break;
                            }
                            else
                            {
                                EnqueueAction(actionQueue, o);
                            }
                        }
                    }
                }

                if (actionQueue.Count == 0)
                {
                    Debug.WriteLine("NoActions for " + this.DisplayName);
                    return;
                }

                #region process action queue

                while (actionQueue.Count > 0)
                {
                    Action action = actionQueue.Dequeue();
                    if (action != null)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                }

                #endregion
            }
            catch (SocketException ex)
            {
                Disconnect();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            finally
            {
                StartReceiving();
            }
        }

        private void OnUnknownInputReceived(object o)
        {
            // TODO: log
        }

        private void OnMouseInputReceived(Model.Input.MouseInput mouseInput)
        {
            bool useVIQ = (Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_ABSOLUTE == (mouseInput.Flags & Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_ABSOLUTE));

            // translate message and track MK changes
            Win32.WM wm = Win32.WM.USER;
            bool isButtonUpEvent = false;
            ushort wheelDelta = 0;
            Win32.SendInputApi.MouseEventFlags lFlags = useVIQ
                ? mouseInput.Flags ^ Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_ABSOLUTE
                : mouseInput.Flags;
            switch (lFlags)
            {
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_MOVE:
                    wm = Win32.WM.MOUSEMOVE;
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTDOWN:
                    wm = Win32.WM.LBUTTONDOWN;
                    CurrentMK |= Win32.Windows.MK.MK_LBUTTON;
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTUP:
                    wm = Win32.WM.LBUTTONUP;
                    isButtonUpEvent = true;
                    CurrentMK = (CurrentMK | Win32.Windows.MK.MK_LBUTTON) ^ Win32.Windows.MK.MK_LBUTTON;
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTDOWN:
                    wm = Win32.WM.RBUTTONDOWN;
                    CurrentMK |= Win32.Windows.MK.MK_RBUTTON;
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTUP:
                    wm = Win32.WM.RBUTTONUP;
                    CurrentMK = (CurrentMK | Win32.Windows.MK.MK_RBUTTON) ^ Win32.Windows.MK.MK_RBUTTON;
                    isButtonUpEvent = true;
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN:
                    wm = Win32.WM.MBUTTONDOWN;
                    CurrentMK |= Win32.Windows.MK.MK_MBUTTON;
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEUP:
                    wm = Win32.WM.MBUTTONUP;
                    CurrentMK = (CurrentMK | Win32.Windows.MK.MK_MBUTTON) ^ Win32.Windows.MK.MK_MBUTTON;
                    isButtonUpEvent = true;
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_XDOWN:
                    wm = Win32.WM.XBUTTONDOWN;
                    {
                        var xbutton = Win32.MACROS.GET_XBUTTON_WPARAM(mouseInput.MouseData);
                        wheelDelta = (ushort)xbutton;
                        switch (xbutton)
                        {
                            case Win32.MACROS.XBUTTONS.XBUTTON1:
                                CurrentMK |= Win32.Windows.MK.MK_XBUTTON1;
                                break;
                            case Win32.MACROS.XBUTTONS.XBUTTON2:
                                CurrentMK |= Win32.Windows.MK.MK_XBUTTON2;
                                break;
                            default:
                                Debug.WriteLine("UnsupportedButtonDown in MouseData(" + xbutton + ") for " + this.DisplayName);
                                break;
                        }
                    }
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_XUP:
                    wm = Win32.WM.XBUTTONUP;
                    isButtonUpEvent = true;
                    {
                        var xbutton = Win32.MACROS.GET_XBUTTON_WPARAM(mouseInput.MouseData);
                        wheelDelta = (ushort)xbutton;
                        switch (xbutton)
                        {
                            case Win32.MACROS.XBUTTONS.XBUTTON1:
                                CurrentMK = (CurrentMK | Win32.Windows.MK.MK_XBUTTON1) ^ Win32.Windows.MK.MK_XBUTTON1;
                                break;
                            case Win32.MACROS.XBUTTONS.XBUTTON2:
                                CurrentMK = (CurrentMK | Win32.Windows.MK.MK_XBUTTON2) ^ Win32.Windows.MK.MK_XBUTTON2;
                                break;
                            default:
                                Debug.WriteLine("UnsupportedButtonUp in MouseData(" + xbutton + ") for " + this.DisplayName);
                                break;
                        }
                    }
                    break;
                case Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_WHEEL:
                    wheelDelta = Win32.MACROS.HIWORD(mouseInput.MouseData);
                    wm = Win32.WM.MOUSEWHEEL;
                    break;
                default:
                    return;
            }

            mouseInput.MouseData = Win32.MACROS.MAKEWPARAM((ushort)CurrentMK, wheelDelta);
            // only use VIQ for "mouse clone" events
            if (!useVIQ)
            {
                Win32.SendInputApi.SendInputViaMSParams(mouseInput.Flags, mouseInput.Time, (int)mouseInput.Point.X, (int)mouseInput.Point.Y, mouseInput.MouseData);
                return;
            }

            // no target window? can't use
            if (WindowHandle == IntPtr.Zero)
            {
                Debug.WriteLine("NoWindowHandle Failed OnMouseInputReceived, Input Loss for " + this.DisplayName);
                return;
            }

            // can't resolve VIQ? can't use
            IntPtr windowInputQueue = WindowInputQueue;
            if (windowInputQueue == IntPtr.Zero)
            {
                Debug.WriteLine("NoWindowInputQueue Failed OnMouseInputReceived, Input Loss for " + this.DisplayName);
                return;
            }

            // denormalize coordinates
            Win32.Windows.RECT clientRect;
            Win32.Windows.GetClientRect(WindowHandle, out clientRect);
            int lPointX = (int)(((double)clientRect.Width / (double)65536) * mouseInput.Point.X);
            int lPointY = (int)(((double)clientRect.Height / (double)65536) * mouseInput.Point.Y);

            // prep action
            Action action = () =>
            {
                Debug.WriteLine("MouseVIQAction for " + this.DisplayName);
                OnMouseEvent_Action((int)mouseInput.Point.X, (int)mouseInput.Point.Y, lPointX, lPointY, wm, mouseInput.MouseData, isButtonUpEvent);
            };

            // resolve VIQ
            IntPtr foregroundWindowHandle;
            IntPtr foregroundInputQueue;
            if (!TryResolveViq(out foregroundInputQueue, out foregroundWindowHandle, DateTime.Now.AddMilliseconds(1000).Ticks))
            {
                Debug.WriteLine("TryResolveVIQ Failed OnMouseInputReceived, Input Loss for " + this.DisplayName);
                return;
            }

            ActionViaViq(action, foregroundInputQueue, "OnMouseInputReceived");
        }

        private void OnKeyboardInputReceived(Model.Input.KeyboardInput keyboardInput)
        {
            // coerce specialized left/right shift-state to generalized shift-state
            switch ((Win32.VK)keyboardInput.VK)
            {
                case Win32.VK.LeftShift:
                case Win32.VK.RightShift:
                    keyboardInput.VK = (uint)Win32.VK.Shift;
                    break;
                case Win32.VK.LeftMenu:
                case Win32.VK.RightMenu:
                    keyboardInput.VK = (uint)Win32.VK.Menu;
                    break;
                case Win32.VK.LeftControl:
                case Win32.VK.RightControl:
                    keyboardInput.VK = (uint)Win32.VK.Control;
                    break;
            }

            // prevent windows key-repeat
            if (IsRepeatKey(keyboardInput.VK, keyboardInput.Scan, keyboardInput.Flags, keyboardInput.Time))
            {
                return;
            }

            // maintain MK state
            switch ((Win32.VK)keyboardInput.VK)
            {
                case Win32.VK.Control:
                case Win32.VK.LeftControl:
                case Win32.VK.RightControl:
                    if ((keyboardInput.Flags & Win32.WindowHook.LLKHF.UP) == Win32.WindowHook.LLKHF.UP)
                    {
                        CurrentMK = (CurrentMK | Win32.Windows.MK.MK_CONTROL) ^ Win32.Windows.MK.MK_CONTROL;
                    }
                    else
                    {
                        CurrentMK |= Win32.Windows.MK.MK_CONTROL;
                    }
                    break;
                case Win32.VK.Shift:
                case Win32.VK.LeftShift:
                case Win32.VK.RightShift:
                    if ((keyboardInput.Flags & Win32.WindowHook.LLKHF.UP) == Win32.WindowHook.LLKHF.UP)
                    {
                        CurrentMK = (CurrentMK | Win32.Windows.MK.MK_SHIFT) ^ Win32.Windows.MK.MK_SHIFT;
                    }
                    else
                    {
                        CurrentMK |= Win32.Windows.MK.MK_CONTROL;
                    }
                    break;
            }

            IntPtr windowHandle = WindowHandle;

            // no target window
            if (windowHandle == IntPtr.Zero)
            {
                Debug.WriteLine("NoWindowHandle Failed OnKeyboardInputReceived, using SendInput for " + this.DisplayName);
                Win32.SendInputApi.SendInputViaKBParams(keyboardInput.Flags, keyboardInput.Time, keyboardInput.Scan, keyboardInput.VK, keyboardInput.CAS);
                return;
            }

            // no VIQ available
            IntPtr windowInputQueue = WindowInputQueue;
            if (windowInputQueue == IntPtr.Zero)
            {
                Debug.WriteLine("NoWindowInputQueue Failed OnKeyboardInputReceived, using SendInput for " + this.DisplayName);
                Win32.SendInputApi.SendInputViaKBParams(keyboardInput.Flags, keyboardInput.Time, keyboardInput.Scan, keyboardInput.VK, keyboardInput.CAS);
                return;
            }

            // resolve VIQ
            IntPtr foregroundWindowHandle;
            IntPtr foregroundInputQueue;
            if (!TryResolveViq(out foregroundInputQueue, out foregroundWindowHandle, DateTime.Now.AddMilliseconds(1000).Ticks))
            {
                Debug.WriteLine("TryResolveVIQ Failed OnKeyboardInputReceived, using SendInput for " + this.DisplayName);
                Win32.SendInputApi.SendInputViaKBParams(keyboardInput.Flags, keyboardInput.Time, keyboardInput.Scan, keyboardInput.VK, keyboardInput.CAS);
                return;
            }

            // use VIQ
            Action action = () => OnKeyboardEventViaViq(keyboardInput.VK, keyboardInput.Flags, keyboardInput.Scan, keyboardInput.Time, keyboardInput.CAS);
            ActionViaViq(action, foregroundInputQueue, "OnKeyboardInputReceived");
        }

        private void OnCommandInputReceived(Model.Input.CommandInput commandInput)
        {
            switch (commandInput.Text.ToUpper())
            {
                case "AC":
                    OnActivateClient();
                    break;
                case "DA":
                    OnDeactivateClient();
                    break;
                case "PING":
                    try
                    {
                        OnPing();
                    }
                    catch (SocketException ex)
                    {
                        Disconnect();
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                    break;
                case "DC":
                    try
                    {
                        Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                    break;
                default:
                    Debug.WriteLine("UnknownCommand '" + (commandInput.Text ?? "") + "' for " + this.DisplayName);
                    break;
            }
        }

        private void EnqueueAction(Queue<Action> queue, object input)
        {
            var action = (Action)(() =>
            {
                if (input is Model.Input.CommandInput)
                {
                    OnCommandInputReceived(input as Model.Input.CommandInput);
                }
                else if (input is Model.Input.KeyboardInput)
                {
                    OnKeyboardInputReceived(input as Model.Input.KeyboardInput);
                }
                else if (input is Model.Input.MouseInput)
                {
                    OnMouseInputReceived(input as Model.Input.MouseInput);
                }
                else
                {
                    OnUnknownInputReceived(input);
                }
            });
            queue.Enqueue(action);
        }

        public Win32.Windows.MK CurrentMK { get; private set; }

        public static bool TryResolveViq(out IntPtr foregroundInputQueue, out IntPtr foregroundWindowHandle, long activationExpiryTime)
        {
            foregroundWindowHandle = IntPtr.Zero;
            foregroundInputQueue = IntPtr.Zero;
            while ((foregroundInputQueue == IntPtr.Zero) && (DateTime.Now.Ticks <= activationExpiryTime))
            {
                foregroundWindowHandle = Win32.Windows.GetForegroundWindow();
                if (foregroundWindowHandle != IntPtr.Zero)
                {
                    Win32.Windows.GetWindowThreadProcessId(foregroundWindowHandle, out foregroundInputQueue);
                }
                System.Threading.Thread.Sleep(1);
            }
            return foregroundInputQueue != IntPtr.Zero;
        }

        private void ActionViaViq(Action action, IntPtr foregroundInputQueue, string callingComponent)
        {
            bool detachWIQ = false;
            bool detachMIQ = false;
            IntPtr oldFocusWindowHandle = IntPtr.Zero;
            try
            {
                lock (inputQueueLock) // TODO: profile this lock
                {
                    if (MyInputQueue != foregroundInputQueue)
                    {
                        detachMIQ = true;
                        if (Win32.Windows.AttachThreadInput(MyInputQueue, foregroundInputQueue, true))
                        {
                            Debug.WriteLine("ATI MIQ Failed " + callingComponent + " for " + this.DisplayName);
                        }
                    }

                    if (WindowInputQueue != foregroundInputQueue)
                    {
                        detachWIQ = true;
                        if (Win32.Windows.AttachThreadInput(WindowInputQueue, foregroundInputQueue, true))
                        {
                            Debug.WriteLine("ATI WIQ Failed " + callingComponent + " for " + this.DisplayName);
                        }
                    }

                    Win32.Windows.SetActiveWindow(WindowHandle);
                    oldFocusWindowHandle = Win32.Windows.SetFocus(WindowHandle);
                }

                // send message
                action();
            }
            finally
            {
                // clean-up
                if (oldFocusWindowHandle != IntPtr.Zero)
                {
                    Win32.Windows.SetActiveWindow(oldFocusWindowHandle);
                    Win32.Windows.SetFocus(oldFocusWindowHandle);
                }
                if (detachWIQ)
                {
                    Win32.Windows.AttachThreadInput(WindowInputQueue, foregroundInputQueue, false);
                }
                if (detachMIQ)
                {
                    Win32.Windows.AttachThreadInput(MyInputQueue, foregroundInputQueue, false);
                }
            }
        }

        #region client-side 'IsRepeatKey' behavior

        private byte[] pressedKeys = new byte[256];

        private bool IsRepeatKey(uint vk, uint scan, Win32.WindowHook.LLKHF flags, uint time)
        {
            bool keyIsPressed = pressedKeys[vk] == 0x80;
            if (Win32.WindowHook.LLKHF.UP != (flags & Win32.WindowHook.LLKHF.UP))
            {
                if (keyIsPressed)
                {
                    return true;
                }
                else
                {
                    this.pressedKeys[vk] = 0x80;
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
                    this.pressedKeys[vk] = (byte)(Win32.IsToggled((Win32.VK)vk) ? 1 : 0);
                }
            }
            return false;
        }

        #endregion

        private void OnKeyboardEventViaViq(uint vk, Win32.WindowHook.LLKHF flags, uint scan, uint time, Win32.CAS cas)
        {
            int wParam = (int)vk;

            Win32.WM wm = (((flags & Win32.WindowHook.LLKHF.UP) == Win32.WindowHook.LLKHF.UP) ? Win32.WM.KEYUP : Win32.WM.KEYDOWN); // TODO SYSKEYDOWN via Win32.WindowHook.LLKHF.AltKey ?
            uint lParam = 0x01;

            if (wm == Win32.WM.KEYUP)
            {
                lParam |= 0xC0000000;
            }

            uint scanCode = scan;
            if (scanCode > 0)
            {
                lParam |= ((scanCode & 0xFF) << 16);
            }

            if ((flags & Win32.WindowHook.LLKHF.UP) != Win32.WindowHook.LLKHF.UP)
            {
                if ((cas & Win32.CAS.CONTROL) != 0)
                {
                    OnKeyboardEventViaViq((uint)Win32.VK.Control, (Win32.WindowHook.LLKHF)0, (uint)0, time, (Win32.CAS)0);
                }
                if ((cas & Win32.CAS.ALT) != 0)
                {
                    OnKeyboardEventViaViq((uint)Win32.VK.Menu, (Win32.WindowHook.LLKHF)0, (uint)0, time, (Win32.CAS)0);
                    flags |= Win32.WindowHook.LLKHF.ALTDOWN;
                }
                if ((cas & Win32.CAS.SHIFT) != 0)
                {
                    OnKeyboardEventViaViq((uint)Win32.VK.Shift, (Win32.WindowHook.LLKHF)0, (uint)0, time, (Win32.CAS)0);
                }
            }

            Win32.SetKeyboardState(this.pressedKeys);
            Win32.Windows.SendMessage(WindowHandle, wm, wParam, lParam);

            // if keydown, translate message
            if (wm == Win32.WM.KEYDOWN)
            {
                Mubox.Win32.Windows.MSG msg = new Win32.Windows.MSG();
                msg.hwnd = WindowHandle;
                msg.lParam = lParam;
                msg.message = wm;
                msg.pt = new Win32.Windows.POINT();
                msg.time = Win32.SendInputApi.GetTickCount();
                msg.wParam = (int)vk;
                Win32.Windows.TranslateMessage(ref msg);
            }

            if ((flags & Win32.WindowHook.LLKHF.UP) != Win32.WindowHook.LLKHF.UP)
            {
                if ((cas & Win32.CAS.CONTROL) != 0)
                {
                    OnKeyboardEventViaViq((uint)Win32.VK.Control, Win32.WindowHook.LLKHF.UP, (uint)0, Win32.SendInputApi.GetTickCount(), (Win32.CAS)0);
                }
                if ((cas & Win32.CAS.ALT) != 0)
                {
                    OnKeyboardEventViaViq((uint)Win32.VK.Menu, Win32.WindowHook.LLKHF.UP, (uint)0, Win32.SendInputApi.GetTickCount(), (Win32.CAS)0);
                }
                if ((cas & Win32.CAS.SHIFT) != 0)
                {
                    OnKeyboardEventViaViq((uint)Win32.VK.Shift, Win32.WindowHook.LLKHF.UP, (uint)0, Win32.SendInputApi.GetTickCount(), (Win32.CAS)0);
                }
            }
        }

        public static volatile IntPtr LastActivatedClientWindowHandle = IntPtr.Zero; // HACK can't mouse-click between game windows without mouse buttons getting stuck

        private static object OnMouseEventLock = new object();

        private void OnMouseEvent_Action(int pointX, int pointY, int lPointX, int lPointY, Win32.WM wm, uint mouseData, bool isButtonUpEvent)
        {
            uint clientRelativeCoordinates = Win32.MACROS.MAKELPARAM(
                (ushort)lPointX,
                (ushort)lPointY);

            //            IntPtr previousWindowCapture = Win32.Cursor.GetCapture() // COMMENTED BY CODEIT.RIGHT;
            int hwnd = windowHandle.ToInt32();
            lock (OnMouseEventLock)
            {
                //Win32.Cursor.SetCapture(windowHandle);
                Win32.Windows.PostMessage(windowHandle, Win32.WM.MOUSEMOVE, (int)CurrentMK, clientRelativeCoordinates);
                Win32.Windows.PostMessage(windowHandle, Win32.WM.SETCURSOR, hwnd, Win32.MACROS.MAKELPARAM((ushort)Win32.WM.MOUSEMOVE, (ushort)Win32.HitTestValues.HTCLIENT));
                Win32.Windows.PostMessage(windowHandle, Win32.WM.MOUSEACTIVATE, hwnd, Win32.MACROS.MAKELPARAM((ushort)wm, (ushort)Win32.HitTestValues.HTCLIENT));
                Win32.Windows.PostMessage(windowHandle, wm, mouseData, clientRelativeCoordinates);
                Debug.WriteLine("OnMouseEvent SendMessage(" + windowHandle.ToString() + ", " + wm + ", " + mouseData + ", " + clientRelativeCoordinates + ", " + pointX + ", " + pointY + ", " + lPointX + ", " + lPointY + ", (" + CurrentMK + "), " + isButtonUpEvent);
                //Win32.Cursor.ReleaseCapture();
            }
        }

        private void OnActivateClient()
        {
            Debug.WriteLine("ReceivedActivateRequest for " + this.DisplayName);
            DateTime onActivateClientReceivedTimestamp = DateTime.Now;
            lock (inputQueueLock)
            {
                Debug.WriteLine("ActivateClientLock took " + onActivateClientReceivedTimestamp.Subtract(DateTime.Now) + " for " + this.DisplayName);
                onActivateClientReceivedTimestamp = DateTime.Now;
                LastActivatedClientWindowHandle = windowHandle;
                long activationExpiryTime = DateTime.Now.AddMilliseconds(1000).Ticks;
                do
                {
                    Debug.WriteLine("ActivateClientAttempt@" + WindowHandle + " for " + this.DisplayName);
                    if (WindowHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine("NoWindowHandle Failed OnActivateClient for " + this.DisplayName);
                        return;
                    }

                    IntPtr windowInputQueue = WindowInputQueue;
                    if (windowInputQueue == IntPtr.Zero)
                    {
                        Debug.WriteLine("NoWindowInputQueue Failed OnActivateClient for " + this.DisplayName);
                        return;
                    }

                    // resolve VIQ
                    IntPtr foregroundWindowHandle;
                    IntPtr foregroundInputQueue;
                    if (!TryResolveViq(out foregroundInputQueue, out foregroundWindowHandle, activationExpiryTime))
                    {
                        Debug.WriteLine("TryResolveVIQ Failed OnActivateClient for " + this.DisplayName);
                        return;
                    }

                    // use VIQ
                    Action action = () =>
                    {
                        try
                        {
                            Win32.Windows.SetForegroundWindow(windowHandle);
                            Win32.Windows.SetWindowPos(windowHandle, Win32.Windows.Position.HWND_TOP, -1, -1, -1, -1, Win32.Windows.Options.SWP_NOSIZE | Win32.Windows.Options.SWP_NOMOVE | Win32.Windows.Options.SWP_SHOWWINDOW);
                            System.Threading.Thread.Sleep(1);
                            Win32.Windows.SetForegroundWindow(windowHandle);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    };
                    ActionViaViq(action, foregroundInputQueue, "OnActivateClient");
                } while ((DateTime.Now.Ticks < activationExpiryTime) && (windowHandle != Win32.Windows.GetForegroundWindow()));
                Debug.WriteLine("ActivateClientAction took " + onActivateClientReceivedTimestamp.Subtract(DateTime.Now) + " for " + this.DisplayName);
            }
            NotifyClientActivated();
        }

        private void OnDeactivateClient()
        {
            Debug.WriteLine("ReceivedDeactivateRequest for " + this.DisplayName);
            DateTime onDeactivateClientReceivedTimestamp = DateTime.Now;
            lock (inputQueueLock)
            {
                Debug.WriteLine("DeactivateClientLock took " + onDeactivateClientReceivedTimestamp.Subtract(DateTime.Now) + " for " + this.DisplayName);
                onDeactivateClientReceivedTimestamp = DateTime.Now;
                LastActivatedClientWindowHandle = windowHandle;
                long activationExpiryTime = DateTime.Now.AddMilliseconds(1000).Ticks;

                Debug.WriteLine("DeactivateClientAttempt@" + WindowHandle + " for " + this.DisplayName);
                if (WindowHandle == IntPtr.Zero)
                {
                    Debug.WriteLine("NoWindowHandle Failed OnDeactivateClient for " + this.DisplayName);
                    return;
                }

                IntPtr windowInputQueue = WindowInputQueue;
                if (windowInputQueue == IntPtr.Zero)
                {
                    Debug.WriteLine("NoWindowInputQueue Failed OnDeactivateClient for " + this.DisplayName);
                    return;
                }

                // resolve VIQ
                IntPtr foregroundWindowHandle;
                IntPtr foregroundInputQueue;
                if (!TryResolveViq(out foregroundInputQueue, out foregroundWindowHandle, activationExpiryTime))
                {
                    Debug.WriteLine("TryResolveVIQ Failed OnDeactivateClient for " + this.DisplayName);
                    return;
                }

                // use VIQ
                Action action = () =>
                {
                    try
                    {
                        Win32.Windows.SetWindowPos(windowHandle, Win32.Windows.Position.HWND_TOP, -1, -1, -1, -1, Win32.Windows.Options.SWP_NOSIZE | Win32.Windows.Options.SWP_NOMOVE | Win32.Windows.Options.SWP_NOACTIVATE);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                };
                ActionViaViq(action, foregroundInputQueue, "OnDeactivateClient");
            }
            Debug.WriteLine("DeactivateClientAction took " + onDeactivateClientReceivedTimestamp.Subtract(DateTime.Now) + " for " + this.DisplayName);
        }

        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> Disconnected;

        private void OnConnected()
        {
            SendClientConfig();
            if (this.Connected != null)
            {
                Connected(this, new EventArgs());
            }
        }

        public void SendClientConfig()
        {
            byte[] machineNameCommand = ASCIIEncoding.ASCII.GetBytes(string.Format("|NAME/{0}/{1}/{2}/{3}/{4}",
                this.DisplayName,
                this.WindowStationHandle.ToString(),
                this.WindowDesktopHandle.ToString(),
                this.WindowHandle.ToString(),
                "?"));
            if (this.EndPoint != null)
            {
                this.EndPoint.Client.Send(machineNameCommand, SocketFlags.None);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122")]
        public void SendPerformanceInfo(System.Diagnostics.Process process)
        {
            string[] stats = null;
            try
            {
                if (process == null)
                {
                    stats = new string[]
                            {
                                "Untitled",
                                "0",
                                "Untitled",
                                "False",
                                "0",
                                "0",
                                "0",
                                "0",
                                "0"
                                // TODO Disk Time, CPU Time
                            };
                }
                else
                {
                    if (!process.HasExited)
                    {
                        try
                        {
                            stats = new string[]
                            {
                                process.MainWindowTitle,
                                process.Id.ToString(),
                                System.IO.Path.GetFileName(process.ProcessName),
                                process.Responding.ToString(),
                                process.WorkingSet64.ToString(),
                                process.PeakWorkingSet64.ToString(),
                                process.PagedMemorySize64.ToString(),
                                process.PeakPagedMemorySize64.ToString(),
                                sendCommandTimeSpent.Ticks.ToString()
                                // TODO Disk Time, CPU Time
                            };
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            if (stats != null)
            {
                SendCommand("STAT",
                (sender, e) =>
                {
                    // NOP
                },
                stats);
            }
        }

        private void SendCommand(string commandName)
        {
            SendCommand(commandName, null, (string[])null);
        }

        private void SendCommand(string commandName, EventHandler<SocketAsyncEventArgs> callback)
        {
            SendCommand(commandName, callback, null);
        }

        private void SendCommand(string commandName, params string[] args)
        {
            SendCommand(commandName, null, args);
        }

        private Dictionary<string, Performance> ClientTxPerformance = new Dictionary<string, Performance>();
        private Dictionary<string, Performance> ClientRxPerformance = new Dictionary<string, Performance>();

        public void ClientTxPerformanceIncrement(string command)
        {
            if (!Performance.IsPerformanceEnabled)
            {
                return;
            }

            Performance performance = null;
            if (!ClientTxPerformance.TryGetValue(command, out performance))
            {
                try
                {
                    performance = Performance.CreatePerformance("_Ctx" + command.ToUpper());
                    ClientTxPerformance[command] = performance;
                }
                catch { }
            }
            if (performance != null)
            {
                performance.Count(DateTime.Now.Ticks / 10000 / 1000);
            }
        }

        public void ClientRxPerformanceIncrement(string command)
        {
            if (!Performance.IsPerformanceEnabled)
            {
                return;
            }
            Performance performance = null;
            if (!ClientRxPerformance.TryGetValue(command, out performance))
            {
                try
                {
                    performance = Performance.CreatePerformance("_Crx" + command.ToUpper());
                    ClientRxPerformance[command] = performance;
                }
                catch { }
            }
            if (performance != null)
            {
                performance.Count(DateTime.Now.Ticks / 10000 / 1000);
            }
        }

        private void SendCommand(string commandName, EventHandler<SocketAsyncEventArgs> callback, params string[] args)
        {
            if (commandName == null)
            {
                return;
            }
            DateTime sendCommandStartTime = DateTime.Now;
            StringBuilder format = new StringBuilder("|" + Encode(commandName));

            if ((args != null) && (args.Length > 0))
            {
                for (int i = 0; i < args.Length; i++)
                {
                    format.AppendFormat("/{0}", Encode(args[i]));
                }
            }
            format.Append("/?");

            byte[] message = ASCIIEncoding.ASCII.GetBytes(format.ToString());

            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.SetBuffer(message, 0, message.Length);
            socketAsyncEventArgs.Completed += (sender, e) =>
                {
                    try
                    {
                        sendCommandTimeSpent = (DateTime.Now - sendCommandStartTime);
                        if (callback != null)
                        {
                            callback(sender, e);
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: log
                        if (socketAsyncEventArgs != null)
                        {
                            socketAsyncEventArgs.Dispose();
                        }
                    }
                };
            if ((this.EndPoint == null) || (this.EndPoint.Client == null))
            {
                return;
            }
            if (this.EndPoint.Client.SendAsync(socketAsyncEventArgs))
            {
                sendCommandTimeSpent = (DateTime.Now - sendCommandStartTime);
                if (callback != null)
                {
                    callback(this, socketAsyncEventArgs);
                }
            }
            ClientTxPerformanceIncrement(commandName);
        }

        public static string Encode(string text)
        {
            return text.Replace(',', '_').Replace('|', '!');
        }

        public static string Decode(string text)
        {
            return text; // TODO one-way encoder
        }

        private TimeSpan sendCommandTimeSpent = TimeSpan.Zero;

        private void OnPing()
        {
            byte[] pingResponse = ASCIIEncoding.ASCII.GetBytes("|PONG/?");
            this.EndPoint.Client.Send(pingResponse, SocketFlags.None);
        }

        private void OnDisconnected()
        {
            if (this.Disconnected != null)
            {
                Disconnected(this, new EventArgs());
            }
        }

        public void CoerceActivation()
        {
            SendCommand("CACT",
                (sender, e) =>
                {
                    // NOP
                });
        }

        public event EventHandler<EventArgs> ClientActivated;

        public void NotifyClientActivated()
        {
            try
            {
                if (ClientActivated != null)
                {
                    ClientActivated(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            SendCommand("ACTV",
                (sender, e) =>
                {
                    // NOP
                });
        }
    }
}