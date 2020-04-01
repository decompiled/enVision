using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;

namespace enVision
{
    class Program
    {
        #region
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        private const int InputMouse = 0;
        private const int SleepTime = 4;
        private const int SleepTime2 = 1;
        private const int TriggerTime = 200;
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const uint KeyeventfKeyup = 0x0002;
        private const uint KeyeventfScancode = 0x0008;
        private const uint MouseeventfAbsolute = 0x8000;
        private const uint MouseeventfMove = 0x0001;
        private const ushort Key = (ushort)Keys.F8;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static bool _killAutoClick;
        private static bool _killAutoMove;
        private static IntPtr _hookId = IntPtr.Zero;
        private static readonly LowLevelKeyboardProc Proc = HookCallback;
        private const int pointoffset = 50; //32
        private const int Correction = 46;
        #endregion

        static void Main()
        {
            Console.WriteLine("enVision Loaded");
            Console.WriteLine(pointoffset + "x" + SleepTime2);
            new Thread(Autofire).Start();
            new Thread(AutoAim).Start();
            _killAutoClick = true;
            _killAutoMove = true;
            _hookId = SetHook(Proc);
            Application.Run();
            SafeNativeMethods.UnhookWindowsHookEx(_hookId);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WhKeyboardLl, proc, SafeNativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WmKeydown)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                const int r = 0x52; 
                const int e = 0x45; 
                const int f = 0x46; 
                const int q = 0x51; 
                const int z = 0x5A; 
                const int x2 = 0x32; // 2
                const int x3 = 0x33; // 3
                const int x4 = 0x34; // 4
                switch (vkCode)
                {
                    case r:
                    case x3:
                        _killAutoClick = false;
                        _killAutoMove = true;
                        break;
                    case q:
                    case x2:
                    case z:
                    case x4:
                    case f:
                        _killAutoClick = true;
                        _killAutoMove = true;
                        break;
                    case e:
                        _killAutoClick = true;
                        _killAutoMove = false;
                        break;
                    case 44:
                        Environment.Exit(0);
                        break;
                }
            }
            return SafeNativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static void Autofire()
        {
            var cursor = new System.Drawing.Point();

            while (true)
            {
                if (_killAutoClick)
                {
                    Thread.Sleep(SleepTime);
                    continue;
                }

                SafeNativeMethods.GetCursorPos(ref cursor);

                var c = GetColorAt(cursor);

                if (c.R < 50 && c.G > 150 && c.B < 50)
                {
                    var scanCode = (ushort)SafeNativeMethods.MapVirtualKey(Key, 0);
                    var input = new Input { type = 1, ki = new KeyboardInput { wScan = scanCode, dwFlags = KeyeventfScancode } };
                    SafeNativeMethods.SendInput(1, ref input, Marshal.SizeOf(typeof(Input)));
                    input.ki.dwFlags = KeyeventfScancode | KeyeventfKeyup;
                    SafeNativeMethods.SendInput(1, ref input, Marshal.SizeOf(typeof(Input)));
                    Thread.Sleep(TriggerTime);

                }

                Thread.Sleep(SleepTime);
            }
// ReSharper disable once FunctionNeverReturns
        }

        private static void AutoAim()
        {
            var cursor = new System.Drawing.Point();

            while (true)
            {
                if (_killAutoMove)
                {
                    Thread.Sleep(500);
                    continue;
                }

                SafeNativeMethods.GetCursorPos(ref cursor);

                var c = GetColorAt(cursor);
                var cl = GetColorAtLeft(cursor);
                var cr = GetColorAtRight(cursor);

                //Console.WriteLine("{0}:{1}:{2}-{3}:{4}:{5}-{6}:{7}:{8}", cl.R, cl.G, cl.B, c.R, c.G, c.B, cr.R, cr.G, cr.B);

                if (c.R < 50 && c.G > 150 && c.B < 50)
                {
                    Thread.Sleep(SleepTime2);
                    continue;
                }

                if (cl.R < 50 && cl.G > 150 && cl.B < 50)
                {
                    SimMov(-1);
                    Thread.Sleep(SleepTime2);
                    continue;
                }

                if (cr.R < 50 && cr.G > 150 && cr.B < 50)
                {
                    SimMov(1);
                    Thread.Sleep(SleepTime2);
                    continue;
                }

                Thread.Sleep(SleepTime2);

            }
// ReSharper disable once FunctionNeverReturns
        }

        private static Color GetColorAt(System.Drawing.Point location)
        {
            using (var screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            {
                using (var gdest = Graphics.FromImage(screenPixel))
                {
                    using (var gsrc = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        try
                        {
                            var hSrcDc = gsrc.GetHdc();
                            var hDc = gdest.GetHdc();
                            SafeNativeMethods.BitBlt(hDc, 0, 0, 1, 1, hSrcDc, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);

                        }
                        finally
                        {
                            gdest.ReleaseHdc();
                            gsrc.ReleaseHdc();
                        }
                    }
                }
                return screenPixel.GetPixel(0, 0);
            }



        }

        private static Color GetColorAtLeft(System.Drawing.Point location)
        {
            using (var screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            {
                using (var gdest = Graphics.FromImage(screenPixel))
                {
                    using (var gsrc = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        try
                        {
                            var hSrcDc = gsrc.GetHdc();
                            var hDc = gdest.GetHdc();
                            SafeNativeMethods.BitBlt(hDc, 0, 0, 1, 1, hSrcDc, location.X - pointoffset, location.Y,
                                (int)CopyPixelOperation.SourceCopy);

                        }
                        finally
                        {
                            gdest.ReleaseHdc();
                            gsrc.ReleaseHdc();
                        }
                    }
                }
                return screenPixel.GetPixel(0, 0);
            }
        }

        private static Color GetColorAtRight(System.Drawing.Point location)
        {
            using (var screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            {
                using (var gdest = Graphics.FromImage(screenPixel))
                {
                    using (var gsrc = Graphics.FromHwnd(IntPtr.Zero))
                    {
                        try
                        {
                            var hSrcDc = gsrc.GetHdc();
                            var hDc = gdest.GetHdc();
                            SafeNativeMethods.BitBlt(hDc, 0, 0, 1, 1, hSrcDc, location.X + pointoffset, location.Y,
                                (int)CopyPixelOperation.SourceCopy);
                        }
                        finally
                        {
                            gdest.ReleaseHdc();
                            gsrc.ReleaseHdc();
                        }
                    }
                }
                return screenPixel.GetPixel(0, 0);
            }
        }

        private static void SimMov(int x)
        {
            if (x > 0)
            {
                var input = new Input { type = InputMouse, mi = new MouseInput { dx = pointoffset + Correction, dy = 0, mouseData = 0, time = 0, dwFlags = MouseeventfAbsolute | MouseeventfMove } };
                SafeNativeMethods.SendInput(1, ref input, Marshal.SizeOf(typeof(Input)));

            }
            else if (x < 0)
            {
                var input = new Input { type = InputMouse, mi = new MouseInput { dx = (pointoffset + Correction) * -1, dy = 0, mouseData = 0, time = 0, dwFlags = MouseeventfAbsolute | MouseeventfMove } };
                SafeNativeMethods.SendInput(1, ref input, Marshal.SizeOf(typeof(Input)));
            }

        }
    }

    #region
    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseInput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardInput
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HardwareInput
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Input
    {
        [FieldOffset(0)]
        public int type;
        [FieldOffset(4)]
        public MouseInput mi;
        [FieldOffset(4)]
        public KeyboardInput ki;
        [FieldOffset(4)]
        public HardwareInput hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static implicit operator System.Drawing.Point(Point p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator Point(System.Drawing.Point p)
        {
            return new Point(p.X, p.Y);
        }
    }
    #endregion
    
    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        //Kernel32
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        //GDI32
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        //USER32
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, ref Input pInputs, int cbSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
