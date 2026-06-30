using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace TelegramDesktopAudioVolumeFix
{
    class Program
    {
        #region Fields

        private static MMDevice device;
        private static ConsoleCtrlDelegate consoleHandler;
        private static float previousVolume;
        private static bool isTelegramPlaying;

        private static readonly string appName = "TelegramDesktopAudioVolumeFix";
        private static readonly string appVersion = "v2";
        private static readonly string telegramProcessName = "Telegram";
        private static readonly int checkInterval = 1000;
        private static readonly float defaultMaxVolume = 1.0f;

        #endregion

        #region Main Life Cycle

        private static void Main()
        {
            Console.WriteLine($"Starting {appName} {appVersion} ...\n");

            Mutex mutex = new Mutex(true, appName, out bool createdNew);
            if (!createdNew)
                return;

            InitializeApp();
            RunApp();
        }

        private static void InitializeApp()
        {
            Console.Title = appName;

            consoleHandler = new ConsoleCtrlDelegate(ConsoleCtrlHandler);
            SetConsoleCtrlHandler(consoleHandler, true);
        }

        private static void RunApp()
        {
            Console.WriteLine($"{appName} {appVersion} has been started!\n");

            while (true)
            {
                DebugLog("Checking ...");

                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                bool isPlaying = IsTelegramPlaying();

                if (isPlaying && (!isTelegramPlaying))
                {
                    DebugLog($"Telegram started playing");

                    previousVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                    if (previousVolume > 0)
                    {
                        device.AudioEndpointVolume.MasterVolumeLevelScalar = defaultMaxVolume;
                        DebugLog($"Volume set to {defaultMaxVolume:P0} (was {previousVolume:P0})");
                    }

                    isTelegramPlaying = true;
                }
                else if (!isPlaying && isTelegramPlaying)
                {
                    DebugLog($"Telegram stopped playing");

                    if (previousVolume > 0)
                    {
                        device.AudioEndpointVolume.MasterVolumeLevelScalar = previousVolume;
                        DebugLog($"Volume restored to {previousVolume:P0}");
                    }

                    isTelegramPlaying = false;
                }

                Thread.Sleep(checkInterval);
            }
        }

        private static bool IsTelegramPlaying()
        {
            AudioSessionManager sessionManager = device.AudioSessionManager;
            SessionCollection sessions = sessionManager.Sessions;

            for (int i = 0; i < sessions.Count; i++)
            {
                AudioSessionControl session = sessions[i];

                try
                {
                    uint pid = session.GetProcessID;
                    DebugLog($"Current pid name: {Process.GetProcessById((int)pid)}");

                    if (pid == 0)
                        continue;

                    using (Process process = Process.GetProcessById((int)pid))
                    {
                        if (process.ProcessName == telegramProcessName)
                        {
                            if (session.State == AudioSessionState.AudioSessionStateActive)
                                return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLog(ex.Message);
                }
            }

            return false;
        }

        #endregion

        #region Exit Handle

        private delegate bool ConsoleCtrlDelegate(CtrlTypes ctrlType);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);

        private static bool ConsoleCtrlHandler(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    return true;
                case CtrlTypes.CTRL_BREAK_EVENT:
                    return true;
                case CtrlTypes.CTRL_CLOSE_EVENT:
                    ExitHandler();
                    return false;
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                    ExitHandler();
                    return false;
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    ExitHandler();
                    return false;
            }

            return false;
        }

        private static void ExitHandler()
        {
            if (previousVolume > 0)
                device.AudioEndpointVolume.MasterVolumeLevelScalar = previousVolume;
        }

        private enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        #endregion

        #region Secondary

        [Conditional("DEBUG")]
        private static void DebugLog(string message)
        {
            Console.WriteLine(message);
        }

        #endregion
    }
}