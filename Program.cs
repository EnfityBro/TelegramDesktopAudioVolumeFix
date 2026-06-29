using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Diagnostics;
using System.Threading;

namespace TelegramDesktopAudioVolumeFix
{
    class Program
    {
        private static float previousVolume = 50f;
        private static bool isTelegramPlaying = false;

        private static readonly string TelegramProcessName = "Telegram";
        private static readonly int CheckInterval = 1000;

        private static void Main()
        {
            Console.WriteLine("Starting TelegramDesktopAudioVolumeFix ...\n");

            while (true)
            {
                Console.WriteLine("Checking ...");

                MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
                MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                bool isPlaying = IsTelegramPlaying(device);

                if (isPlaying && (!isTelegramPlaying))
                {
                    // Starting playback
                    previousVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = 1.0f;
                    isTelegramPlaying = true;

                    Console.WriteLine($"Telegram started playing. Volume set to 100% (was {previousVolume:P0})");
                }
                else if (!isPlaying && isTelegramPlaying)
                {
                    // End of playback
                    if (previousVolume >= 0)
                    {
                        device.AudioEndpointVolume.MasterVolumeLevelScalar = previousVolume;
                        Console.WriteLine($"Telegram stopped. Volume restored to {previousVolume:P0}");
                    }

                    isTelegramPlaying = false;
                    previousVolume = -1f;
                }

                Thread.Sleep(CheckInterval);
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

                    Console.WriteLine($"Current pid name: {Process.GetProcessById((int)pid)}");

                    if (pid == 0)
                        continue;

                    using (Process process = Process.GetProcessById((int)pid))
                    {
                        if (process.ProcessName == TelegramProcessName)
                        {
                            if (session.State == AudioSessionState.AudioSessionStateActive)
                                return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IsTelegramPlaying Exception: {ex.Message}");
                }
            }

            return false;
        }
    }
}