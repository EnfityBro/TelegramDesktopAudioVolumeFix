using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Diagnostics;
using System.Threading;

namespace TelegramDesktopAudioVolumeFix
{
    class Program
    {
        private static float previousVolume;
        private static bool isTelegramPlaying;

        private static readonly string appName = "TelegramDesktopAudioVolumeFix";
        private static readonly string appVersion = "v2";
        private static readonly string telegramProcessName = "Telegram";
        private static readonly int checkInterval = 1000;
        private static readonly float defaultMaxVolume = 1.0f;

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
        }

        private static void RunApp()
        {
            Console.WriteLine($"{appName} {appVersion} has been started!\n");

            while (true)
            {
                DebugLog("Checking ...");

                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                bool isPlaying = IsTelegramPlaying(device);

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

        private static bool IsTelegramPlaying(MMDevice device)
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

        [Conditional("DEBUG")]
        private static void DebugLog(string message)
        {
            Console.WriteLine(message);
        }
    }
}