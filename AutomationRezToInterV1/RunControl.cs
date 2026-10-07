using System.Runtime.InteropServices;

namespace AutomationRezToInterV1
{
    public static class RunControl
    {
        private static readonly ManualResetEventSlim _gate = new(true);
        private static readonly CancellationTokenSource _cts = new();
        private static volatile bool _typing;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_SPACE = 0x20;
        private const int VK_F12 = 0x7B;

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
            _typing = true;
            try { FlaUI.Core.Input.Keyboard.Type(text); }
            finally
            {
                Thread.Sleep(60);
                _typing = false;
            }
        }

        private const int VK_PAUSE = 0x13;
        private const int VK_SCROLL = 0x91;
        private static volatile bool _cekaOdluku;

        public static bool WaitForDecision()   // Pause = nastavi; Scroll Lock = prekid (preko Checkpoint-a)
        {
            _cekaOdluku = true;
            try
            {
                while (IsDown(VK_PAUSE)) Thread.Sleep(30);   // ako je taster već držan, sačekaj
                while (true)
                {
                    Checkpoint();                            // Scroll Lock ovde prekida program
                    if (IsDown(VK_PAUSE))
                    {
                        while (IsDown(VK_PAUSE)) Thread.Sleep(30);   // sačekaj puštanje
                        return true;
                    }
                    Thread.Sleep(30);
                }
            }
            finally
            {
                Thread.Sleep(100);      // da watcher vidi da je taster pušten
                _cekaOdluku = false;    // tek onda Pause opet znači "pauza"
            }
        }
    }
}