using System.Runtime.Serialization;

namespace Mubox.Model.Input
{
    [DataContract]
    public class MouseInput
        : StationInput
    {
        public static MouseInput CreateFrom(Win32.WM wm, Mubox.Win32.WindowHook.MSLLHOOKSTRUCT hookStruct)
        {
            MouseInput e = new MouseInput();
            e.WM = wm;
            e.Point = new System.Windows.Point(hookStruct.pt.X, hookStruct.pt.Y);
            e.MouseData = hookStruct.mouseData;
            e.Time = hookStruct.time;
            return e;
        }

        [DataMember]
        public bool IsClickEvent { get; set; }

        [DataMember]
        public Win32.WM WM { get; set; }

        [DataMember]
        public System.Windows.Point Point { get; set; }

        [DataMember]
        public uint MouseData { get; set; }

        public Win32.SendInputApi.MouseEventFlags Flags
        {
            get
            {
                Win32.SendInputApi.MouseEventFlags flags = IsAbsolute
                    ? Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_ABSOLUTE
                    : Win32.SendInputApi.MouseEventFlags.NotSet;

                if (IsClickEvent)
                {
                    // TODO i don't see why this has to be done client side, why cant server send the correct mouse input object to represent a 'click'?
                    switch (WM)
                    {
                        case Win32.WM.LBUTTONUP:
                            flags ^= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTUP;
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
                            break;
                        case Win32.WM.RBUTTONUP:
                            flags ^= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTUP;
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
                            break;
                        case Win32.WM.MBUTTONUP:
                            flags ^= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEUP;
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN;
                            break;
                        case Win32.WM.XBUTTONUP:
                            flags ^= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_XUP;
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_XDOWN;
                            break;
                    }
                }
                else
                {
                    switch (WM)
                    {
                        case Win32.WM.MOUSEMOVE:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_MOVE;
                            break;
                        case Win32.WM.LBUTTONDOWN:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
                            break;
                        case Win32.WM.LBUTTONUP:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_LEFTUP;
                            break;
                        case Win32.WM.RBUTTONDOWN:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
                            break;
                        case Win32.WM.RBUTTONUP:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_RIGHTUP;
                            break;
                        case Win32.WM.MBUTTONDOWN:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN;
                            break;
                        case Win32.WM.MBUTTONUP:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_MIDDLEUP;
                            break;
                        case Win32.WM.XBUTTONDOWN:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_XDOWN;
                            break;
                        case Win32.WM.XBUTTONUP:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_XUP;
                            break;
                        case Win32.WM.MOUSEWHEEL:
                        case Win32.WM.MOUSEHWHEEL:
                            flags |= Win32.SendInputApi.MouseEventFlags.MOUSEEVENTF_WHEEL;
                            break;
                        default:
                            // flags |= Win32.SendInputAPI.MouseEventFlags.MOUSEEVENTF_MOVE;
                            break;
                    }
                }

                return flags;
            }
            set
            {
                Flags = value;
            }
        }

        [DataMember]
        public bool IsAbsolute { get; set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}/{3}/{4}/{5}/{6}",
                            IsClickEvent ? "CLK" : "MS",
                            (uint)Flags,
                            (uint)MouseData,
                            (int)Point.X,
                            (int)Point.Y,
                            (uint)Time,
                            base.ToString());
        }
    }
}