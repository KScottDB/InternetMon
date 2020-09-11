using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace InternetMon
{
    class Program
    {
        #region Win32 console hide/show
        static bool hidden = false;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static void showConsole()
        {
            hidden = false;
            ShowWindow(GetConsoleWindow(), SW_SHOW);
        }
        static void hideConsole()
        {
            hidden = true;
            ShowWindow(GetConsoleWindow(), SW_HIDE);
        }
        #endregion

        #region Constants (language, ping settings, colors)
        const string pingcmd = "ping";
        const string pingarg = "-t {0}";

        const string routerip = "192.168.1.1";
        const string internetip = "1.1.1.1";
        
        const string warningsign = @"/!\";
        const string checkedsign = " :)";
        const string warningmsg = "You may have been disconnected from the Internet. Current status:";
        const string connectedmsg = "You are now connected to the internet.";
        const string routerconnection = "[{0}] Main router connection";
        const string internetconnection = "[{0}] Internet connection";

        static readonly string[] failiurestrings =
        {
            "Request timed out.",
            "Destination host unreachable."
        };
        static readonly string[] successstrings =
        {
            "bytes="
        };

        static readonly ConsoleColor[] defaultColors = {
            ConsoleColor.Black,
            ConsoleColor.Gray
        };
        static readonly ConsoleColor[] warningSignColors = {
            ConsoleColor.Black,
            ConsoleColor.Yellow
        };
        static readonly ConsoleColor[] checkColors =
        {
            ConsoleColor.Green,
            ConsoleColor.Gray
        };
        #endregion

        #region Console Abstraction
        private static void setConsoleSize(int width, int height)
        {
            Console.SetWindowSize(width, height);
            Console.SetBufferSize(width, height);
        }

        private static void setConsoleColor(ConsoleColor fg, ConsoleColor bg)
        {
            setConsoleColor(new ConsoleColor[] { fg, bg });
        }

        private static void setConsoleColor(ConsoleColor[] fgBgCombo) {
            Console.ForegroundColor = fgBgCombo[0];
            Console.BackgroundColor = fgBgCombo[1];
        }
        #endregion

        private static void drawWarningScreen()
        {
            drawWarningScreen(connectedRouter, connectedInternet);
        }

        private static void drawWarningScreen(bool router, bool internet)
        {
            Console.Clear();

            setConsoleColor(defaultColors);

            setConsoleSize(
                1 + warningsign.Length + 1 + Math.Max( warningmsg.Length, connectedmsg.Length ) + 3,
                5
            );

            Console.SetCursorPosition(1, 1);

            string sign = internet ? checkedsign : warningsign;
            string msg = internet ? connectedmsg : warningmsg;
            ConsoleColor[] signcolor = internet ? checkColors : warningSignColors;

            setConsoleColor(signcolor);
            Console.Write(sign);
            setConsoleColor(defaultColors);
            Console.Write(" "); Console.Write(msg);

            Console.SetCursorPosition(1, 3); Console.Write(string.Format(routerconnection, router ? "X" : " "));
            Console.SetCursorPosition(1, 4); Console.Write(string.Format(internetconnection, internet ? "X" : " "));
        }

        static bool connectedRouter = true;
        static bool connectedInternet = true;

        static void Main(string[] args)
        {
            Console.WriteLine("== INTERNETMON - KSCOTT 2020 ==\nInternetMon is being initialized. Please wait.");

            Process ping_router;
            Process ping_internet;

            Debug.WriteLine("starting router pinger");
            ping_router = Process.Start(new ProcessStartInfo
            {
                FileName = pingcmd,
                Arguments = string.Format(pingarg, routerip),
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });

            Debug.WriteLine("starting internet pinger");
            ping_internet = Process.Start(new ProcessStartInfo
            {
                FileName = pingcmd,
                Arguments = string.Format(pingarg, internetip),
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            });

            Debug.WriteLine("binding events");
            ping_router.OutputDataReceived += Ping_router_OutputDataReceived;
            ping_internet.OutputDataReceived += Ping_internet_OutputDataReceived;

            hideConsole();

            ping_router.BeginOutputReadLine();
            ping_internet.BeginOutputReadLine();
            ping_router.WaitForExit();
            ping_internet.WaitForExit();
        }

        private static void Ping_internet_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Process p = (Process)sender;
            string line = e.Data;

            foreach (string fs in failiurestrings)
            {
                if (line.Contains(fs))
                {
                    connectedInternet = false;
                    showConsole();
                    drawWarningScreen();
                }
            }
            foreach (string fs in successstrings)
            {
                if (line.Contains(fs))
                {
                    connectedInternet = true;
                    if (!hidden)
                    {
                        drawWarningScreen();
                        Thread.Sleep(1500);
                        hideConsole();
                    }
                }
            }

            Debug.WriteLine($"connected to internet: {connectedInternet}\ninternet ping line: {line}");
        }

        private static void Ping_router_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Process p = (Process)sender;
            string line = e.Data;

            foreach (string fs in failiurestrings)
            {
                if (line.Contains(fs)) connectedRouter = false;
            }
            foreach (string fs in successstrings)
            {
                if (line.Contains(fs)) connectedRouter = true;
            }

            Debug.WriteLine($"connected to router: {connectedInternet}\nrouter ping line: {line}");
        }
    }
}
