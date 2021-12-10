using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;

namespace Aimbot
{
    class Program
    {
        #region
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        private const int InputMouse = 0;
        private const int SleepTime = 4;
        private const int SleepTime2 = 1;
        private const int TriggerTime = 50;
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const uint KeyeventfKeyup = 0x0002;
        private const uint KeyeventfScancode = 0x0008;
        private const uint MouseeventfAbsolute = 0x8000;
        private const uint MouseeventfMove = 0x0001;
        private const ushort Key = (ushort)Keys.F8;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static bool _killautofire;
        private static bool _killautoaim;
        private static IntPtr _hookId = IntPtr.Zero;
        private static readonly LowLevelKeyboardProc Proc = HookCallback;
        private const int Lgoffset = 50; //44
        private const int Correction = 30;
        //private const int min = 1;
        //private const int max = 50;
        #endregion

        static void Main()
        {
            Console.WriteLine("Aimbot Loaded");
            Console.WriteLine(Lgoffset + "x" + SleepTime2);
            new Thread(Autofire).Start();
            new Thread(AutoAim).Start();
            _killautofire = true;
            _killautoaim = true;
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
                const int r = 0x52; // rail
                const int e = 0x45; // lg
                const int f = 0x46; // plasma
                const int q = 0x51; // rocket
                const int z = 0x5A; // gauntlet
                const int mg = 0x32; // mg = 2
                const int sg = 0x33; // sg = 3
                const int gl = 0x34; // gl = 4
                switch (vkCode)
                {
                    case r:
                    case sg:
                        _killautofire = false;
                        _killautoaim = true;
                        break;
                    case q:
                    case mg:
                    case z:
                    case gl:
                    case f:
                        _killautofire = true;
                        _killautoaim = true;
                        break;
                    case e:
                        _killautofire = true;
                        _killautoaim = false;
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
                if (_killautofire)
                {
                    Thread.Sleep(SleepTime);
                    continue;
                }

                SafeNativeMethods.GetCursorPos(ref cursor);

                var c = GetColorAt(cursor);
                //Console.WriteLine("R:{0} G:{1} B:{2}", c.R, c.G, c.B);
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
                if (_killautoaim)
                {
                    Thread.Sleep(10);
                    continue;
                }

                SafeNativeMethods.GetCursorPos(ref cursor);

                var c = GetColorAt(cursor);
                var cl = GetColorAtLeft(cursor);
                var cr = GetColorAtRight(cursor);

                //Console.WriteLine("{0}:{1}:{2}-{3}:{4}:{5}-{6}:{7}:{8}", cl.R, cl.G, cl.B, c.R, c.G, c.B, cr.R, cr.G, cr.B);

                //Random random = new Random();
                //int number = random.Next(min, max);

                if (c.R < 50 && c.G > 150 && c.B < 50)
                {
                    continue;
                } else if (cl.R < 50 && cl.G > 150 && cl.B < 50)
                {
                    SimMov(-1);
                    Thread.Sleep(SleepTime2);
                    continue;
                } else if (cr.R < 50 && cr.G > 150 && cr.B < 50)
                {
                    SimMov(1);
                    Thread.Sleep(SleepTime2);
                    continue;
                }
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
                            SafeNativeMethods.BitBlt(hDc, 0, 0, 1, 1, hSrcDc, location.X - Lgoffset, location.Y,
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
                            SafeNativeMethods.BitBlt(hDc, 0, 0, 1, 1, hSrcDc, location.X + Lgoffset, location.Y,
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

        public static double MovePixelsPerMillisecond { get; set; } = 1; //1

        public static double MovePixelsPerStep { get; set; } = 10; //3

        public static void Lerp(int endPoint, TimeSpan duration, TimeSpan interval)
        {
            //Console.WriteLine("ep: " + endPoint + "dur: " + duration + "interval: " + interval);
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                Thread.Sleep(interval);
                var factor = GetFactor(duration, stopwatch);

                var myDx = (int)(0 + (factor * (endPoint - 0)));

                var input = new Input { type = InputMouse, mi = new MouseInput { dx = myDx, dy = 0, mouseData = 0, time = 0, dwFlags = MouseeventfAbsolute | MouseeventfMove } };
                SafeNativeMethods.SendInput(1, ref input, Marshal.SizeOf(typeof(Input)));

                if (factor == 1)
                {
                    break;
                }
            }
        }

        private static double GetFactor(TimeSpan duration, Stopwatch stopwatch)
        {
            if (duration.TotalMilliseconds == 0)
            {
                return 1;
            }
            var factor = stopwatch.ElapsedMilliseconds / duration.TotalMilliseconds;
            if (factor > 1)
            {
                factor = 1;
            }
            return factor;
        }

        private static void SimMov(int x)
        {
            // Calculate some values for duration and interval
            var totalDistance = Lgoffset + Correction;
            var duration = TimeSpan.FromMilliseconds(Convert.ToInt32(totalDistance / MovePixelsPerMillisecond));
            var steps = Math.Max(Convert.ToInt32(totalDistance / MovePixelsPerStep), 1);
            var interval = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / steps);

            if (x > 0)
            {
                Lerp(totalDistance, duration, interval);
            }
            else if (x < 0)
            {
                Lerp(totalDistance * -1, duration, interval);
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
