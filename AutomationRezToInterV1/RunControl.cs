using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutomationRezToInterV1
{
    // Kontrola rada preko tastera koje Logik ne koristi:
    // PAUSE = pauza/nastavak (i "nastavi" na proveri), SCROLL LOCK = prekid
    public static class RunControl
    {
        private static readonly ManualResetEventSlim _gate = new(true);
        private static readonly CancellationTokenSource _cts = new();
        private static volatile bool _cekaOdluku;

        private const int VK_PAUSE = 0x13;
        private const int VK_SCROLL = 0x91;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool IsDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

        public static void StartHotkeyWatcher()
        {
            var thread = new Thread(() =>
            {
                bool pausePrev = false, stopPrev = false;

                while (!_cts.IsCancellationRequested)
                {
                    bool pauseNow = IsDown(VK_PAUSE);
                    bool stopNow = IsDown(VK_SCROLL);

                    if (pauseNow && !pausePrev && !_cekaOdluku) TogglePause();
                    if (stopNow && !stopPrev) Stop();

                    pausePrev = pauseNow;
                    stopPrev = stopNow;
                    Thread.Sleep(30);
                }
            })
            { IsBackground = true };

            thread.Start();
        }

        public static void TogglePause()
        {
            if (_gate.IsSet) { _gate.Reset(); Console.Beep(500, 200); Console.WriteLine("[PAUZA]"); }
            else { _gate.Set(); Console.Beep(900, 200); Console.WriteLine("[NASTAVAK]"); }
        }

        public static void Stop()
        {
            _cts.Cancel();
            _gate.Set();
        }

        public static void Checkpoint()
        {
            _cts.Token.ThrowIfCancellationRequested();
            if (!_gate.IsSet)
            {
                _gate.Wait(_cts.Token);
                _cts.Token.ThrowIfCancellationRequested();
            }
        }

        public static void Sleep(int ms)
        {
            while (ms > 0)
            {
                Checkpoint();
                int step = Math.Min(50, ms);
                Thread.Sleep(step);
                ms -= step;
            }
        }

        public static void TypeText(string text)
        {
            FlaUI.Core.Input.Keyboard.Type(text);
            Thread.Sleep(60);
        }

        // Čeka odluku na proveri: PAUSE = nastavi, SCROLL LOCK = prekid
        public static bool WaitForDecision()
        {
            _cekaOdluku = true;
            try
            {
                // ako je Pause pritisnut pre pitanja, program je pauziran, pa ga odpauziramo
                if (!_gate.IsSet)
                {
                    _gate.Set();
                    Console.WriteLine("[NASTAVAK] (Pause je pritisnut pre pitanja, pritisni ga ponovo za potvrdu)");
                }

                while (IsDown(VK_PAUSE)) Thread.Sleep(30);   // sačekaj da se taster pusti
                while (true)
                {
                    _cts.Token.ThrowIfCancellationRequested();   // Scroll Lock prekida
                    if (IsDown(VK_PAUSE))
                    {
                        while (IsDown(VK_PAUSE)) Thread.Sleep(30);
                        return true;
                    }
                    Thread.Sleep(30);
                }
            }
            finally
            {
                Thread.Sleep(100);
                _cekaOdluku = false;
            }
        }
    }
}