using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static AutomationRezToInterV1.UserInputConfig;





namespace AutomationRezToInterV1
{
    public class AutomationHelpers
    {
      
        public static Application StartAnApplication(string path)
        {
            var app = Application.Launch(path);
            Console.WriteLine("[INFO] Logik pokrenut, čekam glavni prozor...");
            return app;

        }

        public static Window CekajLogikProzor(FlaUI.Core.AutomationBase automation, int processId, int timeoutMs = 15000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                foreach (var el in automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window)))
                {
                    try
                    {
                        if (el.Properties.ProcessId.ValueOrDefault != processId) continue;
                        if (!(el.Name ?? "").Contains("Logik")) continue;

                        var w = el.AsWindow();
                        if (NadjiDonjiLeviPanel(w.Properties.NativeWindowHandle.Value) != null)
                        {
                            RunControl.Sleep(300);
                            return w;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) { }
                }
                RunControl.Sleep(200);
            }
            Console.WriteLine($"[ERROR] Prozor pokrenutog Logika (proces {processId}) se nije pojavio za {timeoutMs / 1000}s.");
            return null;
        }

        public static bool DupliKlikNaGridDokumenata(Window logikProzor)
        {
            IntPtr h = logikProzor.Properties.NativeWindowHandle.Value;
            IntPtr forma = NadjiFormu(h, "TInvoiceViewerForm");
            if (forma == IntPtr.Zero)
            {
                Console.WriteLine("[ERROR] Ne nalazim modul Dokumenti (TInvoiceViewerForm).");
                return false;
            }
            var grid = GetChildFields(forma).FirstOrDefault(p => p.Klasa == "TDBGrid");
            if (grid == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim grid u modulu Dokumenti.");
                return false;
            }

            var tacka = new System.Drawing.Point(grid.R.Left + 50, (grid.R.Top + grid.R.Bottom) / 2);
            SetForegroundWindow(h);
            RunControl.Sleep(150);

            // ako je na vrhu drugi modul (npr. SEF), vraćamo se na Dokumenti jednom
            string modul = ModulNaTacki(tacka);
            if (modul != "TInvoiceViewerForm")
            {
                Console.WriteLine($"[WARNING] Na vrhu je '{modul}' umesto Dokumenata. Vraćam se na Dokumenti...");
                KlikniDokumenti(logikProzor);
                RunControl.Sleep(1000);
                SetForegroundWindow(h);
                RunControl.Sleep(150);
                modul = ModulNaTacki(tacka);
            }

            IntPtr pogodak = WindowFromPoint(tacka);
            if (modul != "TInvoiceViewerForm" || (pogodak != grid.H && !IsChild(grid.H, pogodak)))
            {
                Console.WriteLine($"[ERROR] Na mestu klika nije grid Dokumenata (na vrhu: '{modul}').");
                return false;
            }

            Mouse.MoveTo(tacka);
            RunControl.Sleep(50);
            Mouse.DoubleClick(tacka);
            Console.WriteLine($"[SUCCESS] Dvoklik na grid Dokumenata ({tacka.X}, {tacka.Y})");
            return true;
        }




        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MAXIMIZE = 3;

        public static void MaximizeWindow(Window window)
        {
            try
            {
                // pokusaj preko UI Automation
                var pattern = window.Patterns.Window.PatternOrDefault;
                if (pattern != null)
                {
                    if (pattern.WindowVisualState.Value == WindowVisualState.Maximized)
                    {
                        Console.WriteLine("[INFO] Prozor je vec maksimizovan");
                        return;
                    }

                    if (pattern.CanMaximize.Value)
                    {
                        pattern.SetWindowVisualState(WindowVisualState.Maximized);
                        RunControl.Sleep(500);

                        if (pattern.WindowVisualState.Value == WindowVisualState.Maximized)
                        {
                            Console.WriteLine("[SUCCESS] Prozor maksimizovan UIA");
                            return;
                        }
                    }
                }

                // pokusaj 2 direktno preko windows (delphi prozori cfesto ne podrzvaju UIA)
                var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
                if (handle != IntPtr.Zero)
                {
                    ShowWindow(handle, SW_MAXIMIZE);
                    RunControl.Sleep(500);
                    Console.WriteLine("[SUCCESS] Prozor maximizovan (ShowWindow)");
                    return;
                }

                Console.WriteLine("[WARNING] Nisam uspeo da maximizuje prozor.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.WriteLine($"[WARNING] Greska pri maksimizovanju : {ex.Message}");
            }
        }



        public static Window WaitForWindow(FlaUI.Core.AutomationBase automation, string windowName)
        {
            Console.WriteLine($"[INFO] Cekam da se {windowName} pojavi ");
            for (int i = 0; i < 10; i++)
            {
                var w = FindWindow(automation, windowName);
                if (w != null)
                {
                    return w;

                }
                RunControl.Sleep(200);
            }
            return null;
        }

        public static Window FindWindow(FlaUI.Core.AutomationBase automation, string namePart)
        {
            var widows = automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window));

            foreach (var w in widows)
            {
                if (w.Name.Contains(namePart))
                {
                    return w.AsWindow();
                }
            }
            return null;
        }

        public static void PerformLogicLogin(Window loginWidow, UserInputConfig config)
        {
            var editFields = loginWidow.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
            var buttons = loginWidow.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));

            editFields[0].AsTextBox().Text = config.Password;
            editFields[1].AsTextBox().Text = config.Username;


            Console.WriteLine($"[INFO] Unosim podatke za prijavu: {config.Username}");

            buttons[1].Click();
            Console.WriteLine("[INFO] kliknuto Uredu");

        }
               

        public static AutomationElement GetSpecificDocument(AutomationElement logikProzor, int blockIndex)
        {
            var allPane = logikProzor.FindAllChildren(cf => cf.ByControlType(ControlType.Pane));
            if (allPane.Length <= 3)
                return null;

            var targetParent = allPane[3].FindAllChildren()[1];
            var allBlocks = targetParent.FindAllChildren(cf => cf.ByControlType(ControlType.Pane));

            if (blockIndex < allBlocks.Length)
            {
                return allBlocks[blockIndex].FindFirstDescendant(cf => cf.ByControlType(ControlType.Document));
            }
            return null;

        }

        public static void EnterReservationNumber(Window logikProzor, AutomationElement document, UserInputConfig config)
        {
            if (document == null)
            {
                Console.WriteLine("[ERROR] ne nmogu da unesem rezerviacuju, dokument je null");
                return;
            }
            logikProzor.Focus();

            RunControl.Sleep(200);

            Keyboard.Press(VirtualKeyShort.DOWN);
            RunControl.Sleep(200);


            var textBox = document.AsTextBox();
            textBox.Text = "";
            RunControl.Sleep(100);

            RunControl.TypeText(config.RezervationNumber);
            //document.AsTextBox().Text = config.RezervationNumber;
            RunControl.Sleep(300);

            Keyboard.Press(VirtualKeyShort.ENTER);

            Console.WriteLine($"[SUCCESS] Uneta reservacija {config.RezervationNumber}");


        }

        public static bool SelectFirstInGridWithoutSearch(AutomationElement window)
        {
            var allPanes = window.FindAllChildren(cf => cf.ByControlType(ControlType.Pane));

            if (allPanes.Length <= 2)
            {
                Console.WriteLine("[ERROR] Panel sa indexom ne postoji");
                return false;
            }

            var grid = allPanes[2].FindFirstDescendant(cf => cf.ByClassName("TDBGrid"));

            if (grid != null)
            {
                grid.Focus();

                RunControl.Sleep(400);

                Keyboard.Press(VirtualKeyShort.HOME);
                RunControl.Sleep(150);


                var rect = grid.BoundingRectangle;
                var firstRowPoint = new System.Drawing.Point(rect.Left + 50, rect.Top + 35);

                Mouse.MoveTo(firstRowPoint);
                RunControl.Sleep(50);
                Mouse.DoubleClick(firstRowPoint);

                Console.WriteLine("[INFO] Kliknuto na prvu rezervaciju");
                Console.WriteLine("[INFO] Poslat doubleClick na grid");
                return true;


            }
            Console.WriteLine("[ERROR] Grid nije pronadjen");
            return false;
        }



        public static bool UnbookReservation(FlaUI.Core.AutomationBase automation)
        {
            Console.WriteLine("[DEBUG] Tražim prozor za otknjižavanje...");

            var desktop = automation.GetDesktop();
            Window rezWindow = null;

            for (int i = 0; i < 20; i++)
            {
                rezWindow = desktop.FindFirstDescendant(cf => cf.ByName("Rezervacija"))?.AsWindow();
                if (rezWindow != null) break;
                RunControl.Sleep(100);
            }

            // nadji prozor reservacija 



            if (rezWindow == null)
            {
                Console.WriteLine("[ERROR] Prozor 'Rezervacija' se nije otvorio na vreme nakon dvoklika!");
                return false;
            }
            RunControl.Sleep(300);

            // nadji dugme otnjizi
            var unbookButton = rezWindow.FindFirstDescendant(cf => cf.ByName("Otknjiži"))?.AsButton();
            if (unbookButton == null)
            {
                Console.WriteLine("[ERROR] Prozor se otvorio, ali nema dugmeta 'Otknjiži'!");
                return false;
            }



            unbookButton.Click();
            Console.WriteLine("[INFO] Kliknuto na otknjizi");

            // return ClickButtonOnDialog(automation, "Potvrda", "Da");
            return TryClickDialog(automation, "Potvrda", "Da", 50);


        }
             



        private delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumProc proc, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder sb, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, StringBuilder lParam);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT r);

        [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int max);

        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(System.Drawing.Point p);
        [DllImport("user32.dll")] private static extern bool IsChild(IntPtr parent, IntPtr child);

        [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr hWnd);

        private static string KlasaProzora(IntPtr h)
        {
            var sb = new StringBuilder(256);
            GetClassName(h, sb, 256);
            return sb.ToString();
        }

        // koji modul (…ViewerForm) je na vrhu na datoj tački ekrana
        private static string ModulNaTacki(System.Drawing.Point tacka)
        {
            IntPtr h = WindowFromPoint(tacka);
            while (h != IntPtr.Zero)
            {
                string k = KlasaProzora(h);
                if (k.EndsWith("ViewerForm")) return k;
                h = GetParent(h);
            }
            return "(nepoznato)";
        }

        private static IntPtr NadjiFormu(IntPtr root, string klasa) =>
            GetChildFields(root).FirstOrDefault(p => p.Klasa == klasa)?.H ?? IntPtr.Zero;
        private const uint BM_CLICK = 0x00F5;
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }

        private static Polje NadjiDonjiLeviPanel(IntPtr logikHwnd)
        {
            GetWindowRect(logikHwnd, out var wr);
            return GetChildFields(logikHwnd)
                .Where(p => p.Klasa == "TPanel" && IsWindowVisible(p.H) && Math.Abs(p.R.Left - wr.Left) < 15)
                .OrderByDescending(p => p.R.Top)
                .FirstOrDefault();
        }



        public static bool KlikniDokumenti(Window logik, int dx = 70, int dy = 140)
        {
            IntPtr h = logik.Properties.NativeWindowHandle.Value;
            var panel = NadjiDonjiLeviPanel(h);
            if (panel == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim donji levi panel sa dugmetom Dokumenti.");
                return false;
            }

            int sirina = panel.R.Right - panel.R.Left;
            int visina = panel.R.Bottom - panel.R.Top;
            if (dx >= sirina || dy >= visina)
            {
                Console.WriteLine($"[ERROR] Panel je {sirina}x{visina}, klik ({dx},{dy}) bi pao van njega.");
                return false;
            }

            logik.Focus();
            RunControl.Sleep(150);
            Mouse.Click(new System.Drawing.Point(panel.R.Left + dx, panel.R.Top + dy));
            Console.WriteLine($"[SUCCESS] Klik na Dokumenti (panel + {dx},{dy})");
            return true;
        }
        private const uint WM_GETTEXT = 0x000D;

        public class RezInfo { public string Broj = ""; public string Partner = ""; public string Komentar = ""; }


        private class Polje { public IntPtr H; public string Klasa = ""; public string Tekst = ""; public RECT R; }

        private static List<Polje> GetChildFields(IntPtr root)
        {
            var lista = new List<Polje>();
            EnumChildWindows(root, (h, _) =>
            {
                var cls = new StringBuilder(256); GetClassName(h, cls, 256);
                var txt = new StringBuilder(1024); SendMessage(h, WM_GETTEXT, (IntPtr)1024, txt);
                GetWindowRect(h, out var r);
                lista.Add(new Polje { H = h, Klasa = cls.ToString(), Tekst = txt.ToString().Trim(), R = r });
                return true;
            }, IntPtr.Zero);
            return lista;
        }

        public static RezInfo ProcitajRezervaciju(IntPtr hwnd)
        {
            var edits = GetChildFields(hwnd).Where(p => p.Klasa == "TDBEdit").ToList();

            var broj = edits.FirstOrDefault(p => Regex.IsMatch(p.Tekst, @"^\d+/\d{2}/\d+$"));
            if (broj == null) return null;

            var partner = edits.Where(p => Math.Abs(p.R.Top - broj.R.Top) < 5 && p.R.Left > broj.R.Left)
                               .OrderBy(p => p.R.Left).FirstOrDefault();

            var drugiRed = edits.Where(p => p.R.Top > broj.R.Top + 10)
                                .GroupBy(p => p.R.Top).OrderBy(g => g.Key).FirstOrDefault();
            var komentar = drugiRed?.OrderBy(p => p.R.Left).Skip(1).FirstOrDefault();

            return new RezInfo { Broj = broj.Tekst, Partner = partner?.Tekst ?? "", Komentar = komentar?.Tekst ?? "" };
        }

        public static bool UnesiKomentarInterniPrenos(FlaUI.Core.AutomationBase automation, string komentar)
        {
            var prozor = FindWindow(automation, "Interni prenos");
            if (prozor == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim prozor Interni prenos.");
                return false;
            }
            IntPtr hwnd = prozor.Properties.NativeWindowHandle.Value;

            // Komentar = drugo polje sleva u drugom redu TDBEdit polja
            var edits = GetChildFields(hwnd).Where(p => p.Klasa == "TDBEdit").ToList();
            var redovi = edits.GroupBy(p => p.R.Top).OrderBy(g => g.Key).ToList();
            if (redovi.Count < 2)
            {
                Console.WriteLine("[ERROR] Ne prepoznajem raspored polja u Internom prenosu.");
                return false;
            }
            var polje = redovi[1].OrderBy(p => p.R.Left).Skip(1).FirstOrDefault();
            if (polje == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim polje za komentar.");
                return false;
            }

            // Klik u polje, selektuj postojeći tekst, kucaj preko njega
            prozor.Focus();
            RunControl.Sleep(200);
            var centar = new System.Drawing.Point((polje.R.Left + polje.R.Right) / 2, (polje.R.Top + polje.R.Bottom) / 2);
            Mouse.Click(centar);
            RunControl.Sleep(150);
            Keyboard.Press(VirtualKeyShort.HOME);
            Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.END);
            RunControl.Sleep(100);
            RunControl.TypeText(komentar);
            RunControl.Sleep(200);

            // Provera: pročitaj polje nazad
            var sb = new StringBuilder(1024);
            SendMessage(polje.H, WM_GETTEXT, (IntPtr)1024, sb);
            string upisano = sb.ToString().Trim();
            if (!upisano.Equals(komentar.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ERROR] Komentar nije upisan kako treba. U polju piše: '{upisano}'");
                return false;
            }

            Keyboard.Press(VirtualKeyShort.ENTER);   // kao i ranije, potvrda unosa
            RunControl.Sleep(300);
            Console.WriteLine($"[SUCCESS] Komentar upisan i proveren: '{upisano}'");
            return true;
        }

        public static bool ProveriRezervacijuPreOtknjizavanja(FlaUI.Core.AutomationBase automation, UserInputConfig config,
         string naslov = "PROVERI PRE OTKNJIŽAVANJA", bool traziPotvrdu = true)

        {
            Window rezProzor = null;
            for (int i = 0; i < 30 && rezProzor == null; i++)
            {
                rezProzor = FindWindow(automation, "Rezervacija");
                if (rezProzor == null) RunControl.Sleep(100);
            }
            if (rezProzor == null)
            {
                Console.WriteLine("[STOP] Prozor Rezervacija se nije otvorio. Ništa nije otknjiženo.");
                return false;
            }

            var info = ProcitajRezervaciju(rezProzor.Properties.NativeWindowHandle.Value);
            string ocekivano = $"{config.RezervationNumber}/{DateTime.Now:yy}/";
            bool brojOk = info != null && info.Broj.StartsWith(ocekivano);

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine($"  {naslov}");
            Console.WriteLine($"  Rezervacija: {info?.Broj} {(brojOk ? "" : "  <<< NE POKLAPA SE!")}");
            Console.WriteLine($"  Partner:     {info?.Partner}");
            Console.WriteLine($"  Komentar:    {info?.Komentar}");
            Console.WriteLine($"  Plaćanje:    {config.Payment}");
            Console.WriteLine("==========================================");

            if (!brojOk)
            {
                Console.Beep(300, 800);
                Console.WriteLine($"[STOP] Očekivao sam {ocekivano}..., otvorena je pogrešna rezervacija. Ništa nije otknjiženo.");
                return false;
            }
            if (!traziPotvrdu) return true;

            Console.Beep(1000, 300);
            Console.WriteLine("  PAUSE = nastavi (otknjiži)   |   SCROLL LOCK = pogrešna, prekini");
            if (!RunControl.WaitForDecision())
            {
                Console.WriteLine("[STOP] Prekinuto na tvoj zahtev. Ništa nije otknjiženo.");
                return false;
            }
            return true;
        }

        public static bool IzaberiStavkuMenija(FlaUI.Core.AutomationBase automation, string nazivStavke, int timeoutMs = 3000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                RunControl.Checkpoint();
                try
                {
                    var menus = automation.GetDesktop().FindAllChildren(cf =>
                        cf.ByControlType(ControlType.Menu).Or(cf.ByClassName("#32768")));

                    foreach (var m in menus)
                    {
                        var stavka = m.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                            .FirstOrDefault(s => string.Equals(s.Name?.Trim(), nazivStavke, StringComparison.OrdinalIgnoreCase));

                        if (stavka != null)
                        {
                            if (!stavka.IsEnabled)
                            {
                                Console.WriteLine($"[ERROR] Stavka '{nazivStavke}' postoji, ali je neaktivna.");
                                return false;
                            }
                            try { stavka.AsMenuItem().Invoke(); }
                            catch { stavka.Click(); }
                            Console.WriteLine($"[SUCCESS] Izabrano iz menija: '{nazivStavke}'");
                            return true;
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { }
                RunControl.Sleep(100);
            }
            Console.WriteLine($"[ERROR] Meni sa stavkom '{nazivStavke}' se nije pojavio.");
            return false;
        }

        public static string ProcitajTekstPolja(AutomationElement el)
        {
            if (el == null) return "";
            try { if (el.Patterns.Value.IsSupported) { var v = el.Patterns.Value.Pattern.Value.Value; if (!string.IsNullOrWhiteSpace(v)) return v.Trim(); } } catch { }
            try { if (el.Patterns.Text.IsSupported) { var t = el.Patterns.Text.Pattern.DocumentRange.GetText(-1); if (!string.IsNullOrWhiteSpace(t)) return t.Trim(); } } catch { }
            try
            {
                var h = el.Properties.NativeWindowHandle.ValueOrDefault;
                if (h != IntPtr.Zero)
                {
                    var sb = new StringBuilder(256);
                    SendMessage(h, WM_GETTEXT, (IntPtr)256, sb);
                    return sb.ToString().Trim();
                }
            }
            catch { }
            return "";
        }

        public static bool PosaljiNaKasu(FlaUI.Core.AutomationBase automation)
        {
            var rezZaKasu = FindWindow(automation, "Rezervacija");
            if (rezZaKasu == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim prozor Rezervacija za slanje na kasu.");
                return false;
            }

            ClickAndMeasure(rezZaKasu, "Kopiraj");
            if (!IzaberiStavkuMenija(automation, "Pošalji u Logik Kasu"))
                return false;

            // PRIVREMENO: snimamo šta iskoči posle slanja
           
            Console.WriteLine("[SUCCESS] Poslato na kasu.");
            return true;
        }



        public static AutomationElement PostaviTipDokumenta(AutomationElement logikProzor, string zeljeniTip, int maxKlikova = 12)
        {
            var polje = GetSpecificDocument(logikProzor, 0);
            if (polje == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim polje Tip.");
                return null;
            }

            string trenutno = ProcitajTekstPolja(polje);
            for (int i = 0; i <= maxKlikova; i++)
            {
                Console.WriteLine($"[DEBUG] Tip posle {i} klikova: '{trenutno}'");
                if (trenutno.Equals(zeljeniTip, StringComparison.OrdinalIgnoreCase))
                    return polje;
                if (i == maxKlikova) break;

                string pre = trenutno;
                polje.Focus();
                Mouse.DoubleClick(polje.GetClickablePoint());

                // čekamo da se vrednost promeni, najviše 800 ms
                var sw = Stopwatch.StartNew();
                do
                {
                    RunControl.Sleep(50);
                    trenutno = ProcitajTekstPolja(polje);
                }
                while (trenutno == pre && sw.ElapsedMilliseconds < 800);
            }

            Console.WriteLine($"[ERROR] Ni posle {maxKlikova} klikova Tip nije '{zeljeniTip}'.");
            return null;
        }

        public static string CekajPrviOdProzora(FlaUI.Core.AutomationBase automation, string[] imena, int timeoutMs = 5000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                foreach (var w in automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window)))
                {
                    string ime = "";
                    try { ime = w.Name ?? ""; } catch { }
                    foreach (var trazeno in imena)
                        if (ime.Contains(trazeno)) return trazeno;
                }
                RunControl.Sleep(100);
            }
            return null;
        }
        

        public static void IspisiProzoreProcesa(FlaUI.Core.AutomationBase automation, int processId)
        {
            Console.WriteLine("[DEBUG] Otvoreni prozori Logika u ovom trenutku:");
            foreach (var el in automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window)))
            {
                try
                {
                    if (el.Properties.ProcessId.ValueOrDefault != processId) continue;
                    string ime = el.Name ?? "";
                    Console.WriteLine($"   '{ime}' | klasa: {el.ClassName}");
                    if (ime.StartsWith("Logik")) continue;   // glavni prozor preskačemo, ima previše polja

                    IntPtr h = el.Properties.NativeWindowHandle.ValueOrDefault;
                    if (h != IntPtr.Zero)
                        foreach (var p in GetChildFields(h).Where(p => p.Tekst != ""))
                            Console.WriteLine($"      - {p.Klasa}: {p.Tekst}");
                    foreach (var t in el.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)))
                        Console.WriteLine($"      - tekst: {t.Name}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException) { }
            }
        }
        public static bool KlikniDugmeWin32(Window prozor, string tekstDugmeta, string unutarKlase = null)


        {
            IntPtr hwnd = prozor.Properties.NativeWindowHandle.Value;
            IntPtr koren = hwnd;
            if (unutarKlase != null)
            {
                koren = NadjiFormu(hwnd, unutarKlase);
                if (koren == IntPtr.Zero)
                {
                    Console.WriteLine($"[ERROR] Ne nalazim '{unutarKlase}'.");
                    return false;
                }
            }
            var kandidati = GetChildFields(koren).Where(p =>
                (p.Klasa == "TButton" || p.Klasa == "TBitBtn") &&
                p.Tekst.Replace("&", "").Equals(tekstDugmeta, StringComparison.OrdinalIgnoreCase)).ToList();

            var dugme = kandidati.FirstOrDefault(p => IsWindowVisible(p.H) && IsWindowEnabled(p.H));
            if (dugme == null)
            {
                Console.WriteLine($"[ERROR] Dugme '{tekstDugmeta}': nađeno {kandidati.Count}, ali nijedno nije vidljivo i aktivno.");
                return false;
            }
            if (kandidati.Count > 1)
                Console.WriteLine($"[DEBUG] '{tekstDugmeta}' postoji {kandidati.Count} puta, uzimam vidljivo.");

            SetForegroundWindow(hwnd);
            RunControl.Sleep(150);

            // miš parkiramo na samo dugme (bez klika), da ne ostane iznad nečeg drugog
            Mouse.MoveTo(new System.Drawing.Point((dugme.R.Left + dugme.R.Right) / 2, (dugme.R.Top + dugme.R.Bottom) / 2));
            RunControl.Sleep(50);

            PostMessage(dugme.H, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            Console.WriteLine($"[SUCCESS] Poslat klik na dugme '{tekstDugmeta}'");
            return true;
        }

        public static bool ProveriMagacinInternogPrenosa(FlaUI.Core.AutomationBase automation, string ocekivaniMagacin)
        {
            var prozor = FindWindow(automation, "Interni prenos");
            if (prozor == null)
            {
                Console.WriteLine("[ERROR] Interni prenos nije otvoren za proveru magacina.");
                return false;
            }

            var polja = GetChildFields(prozor.Properties.NativeWindowHandle.Value);
            var brojDok = polja.FirstOrDefault(p => p.Klasa == "TDBEdit" && Regex.IsMatch(p.Tekst, @"^\d+/\d{2}"));
            if (brojDok == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim broj dokumenta u internom prenosu.");
                return false;
            }

            // magacini su TEdit polja u istom redu kao broj dokumenta, sleva nadesno
            var magacini = polja.Where(p => p.Klasa == "TEdit" && Math.Abs(p.R.Top - brojDok.R.Top) < 5)
                                .OrderBy(p => p.R.Left).ToList();
            Console.WriteLine($"[DEBUG] Magacini u internom prenosu: {string.Join(" | ", magacini.Select(m => $"'{m.Tekst}'"))}");

            if (magacini.Count < 2)
            {
                Console.WriteLine("[ERROR] Ne prepoznajem polja magacina.");
                return false;
            }

            string ulazni = magacini[0].Tekst.Trim().TrimStart('/');
            if (ulazni != "1")
            {
                Console.WriteLine($"[ERROR] Ulazni magacin je '{ulazni}', očekivao sam '1'.");
                return false;
            }

            string izlazni = magacini[1].Tekst.Trim();   // desno polje
            if (!izlazni.Equals(ocekivaniMagacin, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ERROR] Magacin je '{izlazni}', očekivao sam '{ocekivaniMagacin}'.");
                return false;
            }

            Console.WriteLine($"[SUCCESS] Magacin proveren: '{izlazni}'");
            return true;
        }

        public static bool RazdvojiRezervaciju(FlaUI.Core.AutomationBase automation, UserInputConfig config)
        {
            // Nađi prozor "Rezervacija" koji ima dugme "Razdvoji rezervaciju"
            Window mali = null;
            for (int i = 0; i < 30 && mali == null; i++)
            {
                foreach (var w in automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window)))
                {
                    try
                    {
                        if (!(w.Name ?? "").Contains("Rezervacija")) continue;
                        var h = w.Properties.NativeWindowHandle.ValueOrDefault;
                        if (h != IntPtr.Zero && GetChildFields(h).Any(p => p.Tekst.Replace("&", "") == "Razdvoji rezervaciju"))
                        {
                            mali = w.AsWindow();
                            break;
                        }
                    }
                    catch { }
                }
                if (mali == null) RunControl.Sleep(100);
            }
            if (mali == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim prozor sa dugmetom 'Razdvoji rezervaciju'.");
                return false;
            }

            // Provera da je povezana rezervacija ona koju obrađujemo
            string ocekivano = $" {config.RezervationNumber}/{DateTime.Now:yy}/";
            var polja = GetChildFields(mali.Properties.NativeWindowHandle.Value);
            var oznaka = polja.FirstOrDefault(p => p.Klasa == "TEdit" && p.Tekst.StartsWith("Rezervacija"));
            if (oznaka == null || !oznaka.Tekst.Contains(ocekivano))
            {
                Console.WriteLine($"[ERROR] Povezana rezervacija je '{oznaka?.Tekst}', a očekivao sam{ocekivano}...");
                return false;
            }

            return KlikniDugmeWin32(mali, "Razdvoji rezervaciju");
        }

       

        public static bool ProveriKomentarInternogPrenosa(FlaUI.Core.AutomationBase automation, string ocekivaniKomentar)
        {
            Window prozor = null;
            for (int i = 0; i < 30 && prozor == null; i++)
            {
                prozor = FindWindow(automation, "Interni prenos");
                if (prozor == null) RunControl.Sleep(100);
            }
            if (prozor == null)
            {
                Console.WriteLine("[ERROR] Interni prenos nije otvoren.");
                return false;
            }

            IntPtr hwnd = prozor.Properties.NativeWindowHandle.Value;
            string komentar = "";
            for (int i = 0; i < 15; i++)   // do 3 s da se podaci učitaju
            {
                var redovi = GetChildFields(hwnd).Where(p => p.Klasa == "TDBEdit")
                             .GroupBy(p => p.R.Top).OrderBy(g => g.Key).ToList();
                komentar = redovi.Count >= 2
                    ? redovi[1].OrderBy(p => p.R.Left).Skip(1).FirstOrDefault()?.Tekst ?? ""
                    : "";

                if (komentar.Equals(ocekivaniKomentar.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[SUCCESS] Otvoren je pravi interni prenos (komentar '{komentar}').");
                    return true;
                }
                RunControl.Sleep(200);
            }
            Console.WriteLine($"[ERROR] Otvoreni interni prenos ima komentar '{komentar}', očekivao sam '{ocekivaniKomentar}'.");
            return false;
        }

       



        public static void ClickAndMeasure(AutomationElement element, string buttonName)
        {
            Stopwatch sw = Stopwatch.StartNew();

            // trzimo dugme
            var btn = element.FindFirstDescendant(cf => cf.ByName(buttonName))?.AsButton();

            if (btn != null)
            {
                btn.Click();

                sw.Stop();
                Console.WriteLine($"[PERF] Kliknuto {buttonName} za {sw.ElapsedMilliseconds}");
            }
            else
            {
                Console.WriteLine($"[ERROR] dugme {buttonName} nije nadjeno");
            }
        }

        public static void StopWatchSteps(string imeKoraka, Action akcija)
        {
            RunControl.Checkpoint();
            Stopwatch sw = Stopwatch.StartNew();
            akcija(); // Izvršava se kod koji proslediš
            sw.Stop();
            Console.WriteLine($"[PERF] Korak '{imeKoraka}' završen za {sw.ElapsedMilliseconds}ms.");
        }

        public static bool ClickButtonOnDialog(FlaUI.Core.AutomationBase automation, string dialogName, string buttonName, int timeoutMs = 8000)
        {
            Console.WriteLine($"[INFO] Trazim dijalog {dialogName}.");
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var dialog = FindWindow(automation, dialogName);
                if (dialog != null && !dialog.IsOffscreen)
                {
                    try
                    {
                        var button = dialog.FindFirstDescendant(cf => cf.ByName(buttonName))?.AsButton();
                        if (button != null)
                        {
                            button.Click();
                            Console.WriteLine($"[SUCCESS] Kliknuto '{buttonName}' na dijalogu '{dialogName}'");
                            return true;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) { }
                }
                RunControl.Sleep(150);
            }
            Console.WriteLine($"[ERROR] Dijalog '{dialogName}' se nije pojavio za {timeoutMs / 1000}s.");
            return false;
        }


        public static bool IzaberiMagacinWin32(FlaUI.Core.AutomationBase automation, string sifra)
        {
            Window prozor = null;
            var sw = Stopwatch.StartNew();
            while (prozor == null && sw.ElapsedMilliseconds < 5000)
            {
                prozor = FindWindow(automation, "Magacini");
                if (prozor == null) RunControl.Sleep(100);
            }
            if (prozor == null)
            {
                Console.WriteLine("[ERROR] Prozor 'Magacini' se nije pojavio.");
                return false;
            }
            IntPtr h = prozor.Properties.NativeWindowHandle.Value;

            var pretraga = GetChildFields(h).FirstOrDefault(p => p.Klasa == "TEdit" && IsWindowVisible(p.H));
            if (pretraga == null)
            {
                Console.WriteLine("[ERROR] Ne nalazim polje za pretragu magacina.");
                return false;
            }

            // upiši šifru u polje za pretragu
            SetForegroundWindow(h);
            RunControl.Sleep(150);
            Mouse.Click(new System.Drawing.Point((pretraga.R.Left + pretraga.R.Right) / 2, (pretraga.R.Top + pretraga.R.Bottom) / 2));
            RunControl.Sleep(100);
            Keyboard.Press(VirtualKeyShort.HOME);
            Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.END);
            RunControl.TypeText(sifra);
            RunControl.Sleep(150);

            // provera: da li u polju stvarno piše šifra
            var sb = new StringBuilder(64);
            SendMessage(pretraga.H, WM_GETTEXT, (IntPtr)64, sb);
            if (!sb.ToString().Trim().Equals(sifra, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[ERROR] U pretrazi piše '{sb}', očekivao sam '{sifra}'.");
                return false;
            }

            if (!KlikniDugmeWin32(prozor, "Pretraga")) return false;
            RunControl.Sleep(500);   // filtriranje liste; pogrešan izbor hvata ProveriMagacinInternogPrenosa
            if (!KlikniDugmeWin32(prozor, "U redu")) return false;

            // čekamo da se Magacini zatvori
            sw.Restart();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (FindWindow(automation, "Magacini") == null)
                {
                    Console.WriteLine($"[SUCCESS] Magacin '{sifra}' izabran.");
                    return true;
                }
                RunControl.Sleep(100);
            }
            Console.WriteLine("[ERROR] Prozor 'Magacini' se nije zatvorio posle 'U redu'.");
            return false;
        }



        public static bool TryClickDialog(FlaUI.Core.AutomationBase automation, string dialogName, string buttonName, int maxRetries = 10)
        {
            Console.WriteLine($"[INFO] Pokušavam da nađem dijalog: {dialogName}...");

          
            
            for (int i = 0; i < maxRetries; i++)
            {
                
                var dialog = FindWindow(automation, dialogName);

                if (dialog != null && !dialog.IsOffscreen)
                {
                    var btn = dialog.FindFirstDescendant(cf => cf.ByName(buttonName))?.AsButton();
                    if (btn != null)
                    {
                        btn.Click();
                        Console.WriteLine($"[SUCCESS] Kliknuto '{buttonName}' na dijalogu '{dialogName}'");
                        return true;
                    }
                }
                RunControl.Sleep(100); 
            }

            Console.WriteLine($"[INFO] Dijalog '{dialogName}' se nije pojavio, idem dalje.");
            return false; // Vraća false, ali program NE PADA
        }

        public static bool WaitForAndProcessInterniPrenos(FlaUI.Core.AutomationBase automation, int timeoutMs = 8000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (FindWindow(automation, "Interni prenos") != null)
                {
                    Console.WriteLine("[SUCCESS] Prozor 'Interni prenos' pronađen");
                    return true;
                }
                RunControl.Sleep(100);
            }
            Console.WriteLine($"[ERROR] Prozor 'Interni prenos' se nije pojavio za {timeoutMs / 1000}s.");
            return false;
        }



        public static bool OtvoriDokumentiPrecicom(Window logik)
        {
            IntPtr h = logik.Properties.NativeWindowHandle.Value;
            SetForegroundWindow(h);
            RunControl.Sleep(150);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.F5);
            Console.WriteLine("[INFO] Poslat Ctrl+F5 (Dokumenti)");
            return true;
        }


        public static bool ProknjiziInterniPrenos(FlaUI.Core.AutomationBase automation)
        {
            var prozor = FindWindow(automation, "Interni prenos");
            if (prozor == null)
            {
                Console.WriteLine("[ERROR] Prozor 'Interni prenos' nije otvoren.");
                return false;
            }

            var dugme = prozor.FindFirstDescendant(cf =>
                cf.ByName("Proknjiži").And(cf.ByControlType(ControlType.Button)))?.AsButton();
            if (dugme == null)
            {
                Console.WriteLine("[ERROR] Ne vidim dugme 'Proknjiži'.");
                return false;
            }

            Mouse.Click(dugme.GetClickablePoint());
            Console.WriteLine("[SUCCESS] Kliknuto 'Proknjiži'");
            return true;
        }


        public static void ZatvoriInterniPrenosUredu(FlaUI.Core.AutomationBase automation)
        {
            Console.WriteLine("[INFO] Tražim prozor 'Interni prenos' za završni klik...");

            var interniPrenosProzor = FindWindow(automation, "Interni prenos");

            if (interniPrenosProzor != null)
            {
                // Tražimo dugme 'U redu'
                var uReduDugme = interniPrenosProzor.FindFirstDescendant(cf =>
                    cf.ByName("U redu").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))?.AsButton();

                if (uReduDugme != null)
                {
                    uReduDugme.Invoke();
                    Console.WriteLine("[SUCCESS] Kliknuto na dugme 'U redu'! PROCES JE KONAČNO ZAVRŠEN!");
                }
                else
                {
                    Console.WriteLine("[GRESKA] Prozor je tu, ali ne vidim dugme 'U redu'.");
                }
            }
            else
            {
                Console.WriteLine("[GRESKA] Prozor 'Interni prenos' nije pronađen za zatvaranje.");
            }
        }

        public static class AplikacijaStatistika
        {
            private static readonly string statFile = "statistika.txt";

            public static (int cek, int gotovina) UcitajStatistiku()
            {
                int cek = 0;
                int gotovina = 0;

                if (File.Exists(statFile))
                {
                    try
                    {
                        string[] linije = File.ReadAllLines(statFile);
                        foreach (var linija in linije)
                        {
                            if (linija.StartsWith("Cek:"))
                                int.TryParse(linija.Split(':')[1], out cek);
                            else if (linija.StartsWith("Gotovina:"))
                                int.TryParse(linija.Split(':')[1], out gotovina);
                        }
                    }
                    catch { /* Ako pukne čitanje, ignorišemo da ne ruši program */ }
                }

                return (cek, gotovina);
            }

            public static void SacuvajStatistiku(string nacinPlacanja)
            {
                var (cek, gotovina) = UcitajStatistiku();

                if (nacinPlacanja.Contains("Cek"))
                    cek++;
                else if (nacinPlacanja.Contains("Gotovina"))
                    gotovina++;

                try
                {
                    File.WriteAllLines(statFile, new[] {
                $"Cek:{cek}",
                $"Gotovina:{gotovina}"
            });
                }
                catch { /* Ignoriši grešku pri upisu */ }
            }
        }

        public static AppConfig UcitajKonfiguracijuPrograma()
        {
            string putanjaFajla = "appsettings.json";

            // Ako fajl ne postoji, mi ga kreiramo sa probnim podacima
            if (!File.Exists(putanjaFajla))
            {
                Console.WriteLine("[INFO] Konfiguracioni fajl ne postoji. Kreiram default 'appsettings.json'...");

                var defaultKonfig = new AppConfig
                {
                    LogikPutanja = @"C:\Program Files (x86)\Logik.exe",
                    KorisnickoIme = "username",
                    Lozinka = "pasword"
                };

                // Pretvaramo C# objekat u lep tekstualni JSON format
                string jsonZaUpis = JsonSerializer.Serialize(defaultKonfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(putanjaFajla, jsonZaUpis);

                return defaultKonfig;
            }

            // Ako fajl postoji, čitamo ga i pretvaramo iz JSON-a u C# objekat
            string procitanJson = File.ReadAllText(putanjaFajla);
            return JsonSerializer.Deserialize<AppConfig>(procitanJson);
        }

        public static bool ZatvoriRezervaciju(FlaUI.Core.AutomationBase automation, string dugme = "U redu")
        {
            var rez = FindWindow(automation, "Rezervacija");
            if (rez == null)
            {
                Console.WriteLine("[INFO] Rezervacija je već zatvorena.");
                return true;
            }

            if (!KlikniDugmeWin32(rez, dugme))
                return false;

            // čekamo da se prozor stvarno zatvori (max 3 s)
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (FindWindow(automation, "Rezervacija") == null)
                {
                    Console.WriteLine("[SUCCESS] Rezervacija zatvorena.");
                    return true;
                }
                RunControl.Sleep(100);
            }
            Console.WriteLine("[ERROR] Rezervacija se nije zatvorila (možda je iskočio neki dijalog).");
            return false;
        }

    

    #region Debug alati (dump prozora i menija)

      public static void DumpWindowToFile(AutomationElement root, string fileName)
        {
            var sb = new StringBuilder();
            int i = 0;
            foreach (var el in root.FindAllDescendants())
            {
                string tip = "", ime = "", klasa = "", vrednost = "";
                try { tip = el.ControlType.ToString(); } catch { }
                try { ime = el.Name; } catch { }
                try { klasa = el.ClassName; } catch { }
                try { if (el.Patterns.Value.IsSupported) vrednost = el.Patterns.Value.Pattern.Value.Value; } catch { }
                System.Drawing.Rectangle r = default;
                try { r = el.BoundingRectangle; } catch { }
                sb.AppendLine($"[{i++}] {tip} | Klasa: {klasa} | Ime: {ime} | Vrednost: | {vrednost} | Poz: {r.X},{r.Y}");
            }
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, fileName), sb.ToString());

        }

        public static void DumpOpenMenus(FlaUI.Core.AutomationBase automation, string fileName)
        {
            var sb = new StringBuilder();
            var menus = automation.GetDesktop().FindAllChildren(cf =>
                cf.ByControlType(ControlType.Menu).Or(cf.ByClassName("#32768")));
            sb.AppendLine($"Otvorenih menija: {menus.Length}");

            foreach (var m in menus)
            {
                sb.AppendLine("---- meni ----");
                foreach (var item in m.FindAllDescendants())
                {
                    string ime = "", tip = ""; bool aktivno = false;
                    try { ime = item.Name; } catch { }
                    try { tip = item.ControlType.ToString(); } catch { }
                    try { aktivno = item.IsEnabled; } catch { }
                    sb.AppendLine($"{tip} | {ime} | aktivno: {aktivno}");
                }
            }

            string putanja = Path.Combine(AppContext.BaseDirectory, fileName);
            File.WriteAllText(putanja, sb.ToString());
            System.Diagnostics.Process.Start("notepad.exe", putanja);
        }

        public static void DumpWin32ToFile(IntPtr root, string fileName, bool otvoriNotepad = true)
        {
            var sb = new StringBuilder();
            int i = 0;
            EnumChildWindows(root, (h, _) =>
            {
                var cls = new StringBuilder(256);
                GetClassName(h, cls, 256);
                var txt = new StringBuilder(1024);
                SendMessage(h, WM_GETTEXT, (IntPtr)1024, txt);
                GetWindowRect(h, out var r);
                sb.AppendLine($"[{i++}] {cls} | Tekst: {txt} | Poz: {r.Left},{r.Top}");
                return true;
            }, IntPtr.Zero);

            string putanja = Path.Combine(AppContext.BaseDirectory, fileName);
            File.WriteAllText(putanja, sb.ToString());
            if (otvoriNotepad) System.Diagnostics.Process.Start("notepad.exe", putanja);

        }


        public static void DumpForegroundWindow(string fileName)
        {
            IntPtr h = GetForegroundWindow();
            var naslov = new StringBuilder(256);
            GetWindowText(h, naslov, 256);
            Console.WriteLine($"[DEBUG] Aktivni prozor: '{naslov}'");
            DumpWin32ToFile(h, fileName, otvoriNotepad: false);
        }

        public static void IzmeriPomerajDokumenti(Window logik, int staroX, int staroY)
        {
            IntPtr h = logik.Properties.NativeWindowHandle.Value;
            GetWindowRect(h, out var wr);
            var panel = NadjiDonjiLeviPanel(h);
            if (panel == null) { Console.WriteLine("[TEST] Panel nije nađen."); return; }

            int dx = (wr.Left + staroX) - panel.R.Left;
            int dy = (wr.Top + staroY) - panel.R.Top;
            Console.WriteLine($"[TEST] Prozor: {wr.Left},{wr.Top}  Panel: {panel.R.Left},{panel.R.Top} - {panel.R.Right},{panel.R.Bottom}");
            Console.WriteLine($"[TEST] Pomeraj u odnosu na panel: X={dx}, Y={dy}");
        }

        private static string GetSafeInfo(AutomationElement el)
        {
            string tip = "Nepoznat";
            try { tip = el.ControlType.ToString(); } catch { tip = "Grip/Poseban"; }

            string name = "N/A";
            try { name = el.Name; } catch { }

            return $"Tip: {tip} | Ime {name}";
        }

        public static void DeepSearch(AutomationElement element, int level)
        {
            AutomationElement[] children;

            try
            {
                children = element.FindAllChildren();
            }
            catch
            {
                return;
            }

            foreach (var child in children)
            {
                string distance = new string(' ', level * 2);
                string info = GetSafeInfo(child);
                Console.WriteLine($"{distance}{info}");

                try
                {
                    child.DrawHighlight(System.Drawing.Color.Yellow);
                }
                catch { }

                DeepSearch(child, level + 1);
            }
        }
        #endregion
    }


}