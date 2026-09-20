using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static AutomationRezToInterV1.UserInputConfig;
using static System.Net.Mime.MediaTypeNames;





namespace AutomationRezToInterV1
{
    public class AutomationHelpers
    {
        private const int UIRefreshDelay = 150;
        public static FlaUI.Core.Application StartAnApplication(string path)
        {
            FlaUI.Core.Application app = FlaUI.Core.Application.Launch(path);
            if (app != null)
            {
                Console.WriteLine("Cekamo da se prozor pojavi");
                Thread.Sleep(3000);
            }
            return app;
        }

        public static Window FindLogicWindow(FlaUI.Core.AutomationBase automation)
        {
            var desktop = automation.GetDesktop();

            var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
            foreach (var w in windows)
            {
                if (w.Name.Contains("Logik"))
                {
                    return w.AsWindow();
                }
            }

            return null;
        }

        public static bool PerformSafeClickToDocuments(Window window, int x, int y)
        {
            try
            {
                window.Focus();
                var rect = window.BoundingRectangle;
                var point = new System.Drawing.Point((int)rect.Left + x, (int)rect.Top + y);
                Mouse.MoveTo(point);
                Thread.Sleep(100);
                Mouse.Click();

                return true;

            }
            catch
            {

                return false;
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
                Thread.Sleep(200);
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


            Console.WriteLine($"[INFO] Unosim podatke za prijavu {config.Username} {config.Password} ");

            buttons[1].Click();
            Console.WriteLine("[INFO] kliknuto Uredu");

        }

        public static void ClickSpecificFieldInList(AutomationElement logikProzor, int blockIndex, int clicks)
        {
            var allPanels = logikProzor.FindAllChildren(cf => cf.ByControlType(ControlType.Pane));
            var targerParent = allPanels[3].FindAllChildren()[1];
            var allBlocks = targerParent.FindAllChildren(cf => cf.ByControlType(ControlType.Pane));

            if (blockIndex < allBlocks.Length)
            {
                var targetDoc = allBlocks[blockIndex].FindFirstDescendant(cf => cf.ByControlType(ControlType.Document));

                if (targetDoc != null)
                {
                    targetDoc.Focus();
                    PerformClicks(targetDoc, 4);
                }
            }
        }

        public static void PerformClicks(AutomationElement targerDoc, int numberOfDoubleClicks)
        {
            targerDoc.Focus();
            for (int i = 0; i < numberOfDoubleClicks; i++)
            {
                Mouse.DoubleClick(targerDoc.GetClickablePoint());
                Thread.Sleep(300);
            }

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

            Thread.Sleep(200);

            Keyboard.Press(VirtualKeyShort.DOWN);
            Thread.Sleep(200);


            var textBox = document.AsTextBox();
            textBox.Text = "";
            Thread.Sleep(100);

            Keyboard.Type(config.RezervationNumber);
            //document.AsTextBox().Text = config.RezervationNumber;
            Thread.Sleep(300);

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

                Thread.Sleep(800);

                Keyboard.Press(VirtualKeyShort.HOME);
                Thread.Sleep(300);


                var rect = grid.BoundingRectangle;
                var firstRowPoint = new System.Drawing.Point(rect.Left + 50, rect.Top + 35);

                Mouse.MoveTo(firstRowPoint);
                Thread.Sleep(150);
                Mouse.DoubleClick(firstRowPoint);

                Console.WriteLine("[INFO] Kliknuto na prvu rezervaciju");
                Console.WriteLine("[INFO] Poslat doubleClick na grid");
                return true;


            }
            Console.WriteLine("[ERROR] Grid nije pronadjen");
            return false;
        }

        private static Window GetDetailWindow(FlaUI.Core.AutomationBase automation)
        {
            var window = automation.GetDesktop().FindFirstDescendant(cf =>
        cf.ByClassName("TReservationDetailForm").And(cf.ByName("Rezervacija")))?.AsWindow();

            if (window == null)
            {
                Console.WriteLine("[DEBUG] Nisam našao prozor 'Rezervacija' (klasa TReservationDetailForm)");
            }
            return window;
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
                Thread.Sleep(100);
            }

            // nadji prozor reservacija 



            if (rezWindow == null)
            {
                Console.WriteLine("[ERROR] Prozor 'Rezervacija' se nije otvorio na vreme nakon dvoklika!");
                return false;
            }
            Thread.Sleep(300);

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

        public static void PressEnterToConfirm(FlaUI.Core.AutomationBase automation)
        {
            Console.WriteLine("[DEBUG] Čekam da se pojavi prozor za potvrdu...");

            bool prozorPojavljen = false;
            // Pokušaj da nađeš prozor 20 puta, sa pauzom od samo 50ms (ukupno 1 sekunda max čekanja)
            for (int i = 0; i < 20; i++)
            {
                var prozori = automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window)); // #32770 je klasa za "standardni Windows dijalog"

                foreach (var p in prozori)
                {
                    // Ako prozor postoji i nije glavni (Logik), to je naš popup
                    if (p != null && !p.Name.Contains("Logik"))
                    {
                        Console.WriteLine($"[DEBUG] Pronađen popup: {p.Name}. Lupam Enter.");
                        Keyboard.Press(VirtualKeyShort.ENTER);
                        prozorPojavljen = true;
                        break;
                    }
                }
                if (prozorPojavljen) break;
                Thread.Sleep(100);


                if (!prozorPojavljen)
                {
                    Console.WriteLine("[WARNING] Nisam detektovao prozor preko petlje, šaljem Enter naslepo.");
                    Keyboard.Press(VirtualKeyShort.ENTER);
                }

            }

            if (!prozorPojavljen)
            {
                Console.WriteLine("[WARNING] Nisam dočekao prozor za potvrdu, nastavljam dalje.");
            }
        }

        public static bool CopyToInternalTransfer(FlaUI.Core.AutomationBase automation)
        {
            var desktop = automation.GetDesktop();

            // uhvati prozor rezervacija 
            var rezWidow = desktop.FindFirstDescendant(cf => cf.ByName("Rezervacija"))?.AsWindow();

            if (rezWidow == null)
            {
                Console.WriteLine("[ERROR] Ne mogu da nadjem prozor 'Rezervacija' za kopianje");
                return false;
            }

            // nadji dugme kopiraj 
            var copyButton = rezWidow.FindFirstDescendant(cf => cf.ByName("Kopiraj"))?.AsButton();

            if (copyButton != null)
            {
                copyButton.Invoke();
                Console.WriteLine("[INFO] Kliknuto na dugme 'Kopiraj'.");
                return true;
            }

            Console.WriteLine("[ERROR] Dugme 'Kopiraj' nije nadjeno.");
            return false;
        }

        public static void ExecutePresses(params (VirtualKeyShort key, int count, int delay)[] steps)
        {
            foreach (var step in steps)
            {
                for (int i = 0; i < step.count; i++)
                {
                    Keyboard.Press(step.key);
                    Thread.Sleep(step.delay);

                }
            }
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

        private static string GetSafeInfo(AutomationElement el)
        {
            string tip = "Nepoznat";
            try { tip = el.ControlType.ToString(); } catch { tip = "Grip/Poseban"; }

            string name = "N/A";
            try { name = el.Name; } catch { }

            return $"Tip: {tip} | Ime {name}";
        }

        public static bool WaitForMenu(FlaUI.Core.AutomationBase automation, string menuName)
        {
            Console.WriteLine($"[INFO] Čekam da se meni '{menuName}' pojavi...");
            var desktop = automation.GetDesktop();

            // Čekamo do 2 sekunde, ali proveravamo svakih 50ms
            for (int i = 0; i < 40; i++)
            {
                // Tražimo bilo koji element koji liči na meni ili novi prozor
                var menu = desktop.FindFirstDescendant(cf => cf.ByName(menuName));
                if (menu != null)
                {
                    Console.WriteLine($"[INFO] Meni '{menuName}' detektovan!");
                    return true;
                }
                Thread.Sleep(50);
            }
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
            Stopwatch sw = Stopwatch.StartNew();
            akcija(); // Izvršava se kod koji proslediš
            sw.Stop();
            Console.WriteLine($"[PERF] Korak '{imeKoraka}' završen za {sw.ElapsedMilliseconds}ms.");
        }

        public static bool ClickButtonOnDialog(FlaUI.Core.AutomationBase automation, string dialogName, string buttonName)
        {
            Console.WriteLine($"[INFO] Trazim dijalog {dialogName}.");



            for (int i = 0; i < 200; i++)
            {
                var dialog = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName(dialogName))?.AsWindow();

                if (dialog != null && dialog.IsOffscreen == false)
                {
                    try
                    {
                        var button = dialog.FindFirstDescendant(cf => cf.ByName(buttonName))?.AsButton();
                        if (button != null)
                        {
                            button.Click();
                            Console.WriteLine($"[SUCCESS] Click  {buttonName} na dijalig {dialogName}");
                            return true;
                        }
                    }
                    catch
                    {


                    }


                }
                Thread.Sleep(200);
            }
            Console.WriteLine($"[ERROR] Dijalog {dialogName} nije nađen ili dugme nije dostupno.");
            return false;

        }

        public static bool SelectAndConfirmExitWarehouse(FlaUI.Core.AutomationBase automation, string warehouseCode)
        {
            // uhvati prozor 
            var desktop = automation.GetDesktop();
            Window window = null;

            for (int i = 0; i < 20; i++)
            {
                var sviProzori = automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window));
                foreach (var p in sviProzori)
                {
                    if (p.Name.Contains("Magacini"))
                    {
                        Console.WriteLine($"[DEBUG] nasao sam prozor {p.Name}");
                        window = p.AsWindow();
                        break;
                    }
                }


                // window = desktop.FindFirstDescendant(cf => cf.ByName("Magacini"))?.AsWindow();
                if (window != null) break;
                Thread.Sleep(100);
            }


            if (window == null)
            {
                Console.WriteLine("[ERROR] nije nasao izlazni prozor Magacini");
                return false;
            }

            Thread.Sleep(300);

            // pronadji edit polje 
            var editFiled = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
            if (editFiled.Length > 0)
            {
                var searchField = editFiled[0].AsTextBox();
                searchField.Focus();
                Thread.Sleep(100);


                // kucamo tekst
                searchField.Text = warehouseCode;
                Thread.Sleep(200);





                // pronadji dugme ptrraga
                var searchButton = window.FindFirstDescendant(cf => cf.ByName("Pretraga"))?.AsButton();
                if (searchButton != null)
                {
                    searchButton.Click();
                    Thread.Sleep(800);
                }


            }
            FlaUI.Core.AutomationElements.Button confirmationButton = null;

            for (int i = 0; i < 20; i++)
            {
                // klikni u redu 
                confirmationButton = window.FindFirstDescendant(cf => cf.ByName("U redu"))?.AsButton();

                if (confirmationButton != null)
                {
                    confirmationButton.Click();
                    Console.WriteLine($"[SUCCESS] Magacin '{warehouseCode} izabran i potvrdjen'");
                    return true;
                }

                Thread.Sleep(200);
            }



            Console.WriteLine("[ERROR] Našao je prozor, ukucao tekst, ali nije našao dugme 'U redu'");
            return false;
        }





        public static void ClearAllWarnings(FlaUI.Core.AutomationBase automation)
        {
            // pokusavamo dandjemo upozorenja sve dok ih ima
            var desktop = automation.GetDesktop();
            var allWindows = desktop.FindAllChildren(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window));

            foreach (var window in allWindows)
            {
                // Tražimo prozor čije ime sadrži "Upozorenje"
                if (window.Name != null && window.Name.Contains("Upozorenje"))
                {
                    Console.WriteLine($"[DEBUG] Pronađen prozor preko UIA2: {window.Name}");

                    // 3. Traži dugme unutar tog prozora
                    var okButton = window.FindFirstDescendant(cf => cf.ByName("U redu"))?.AsButton();
                    if (okButton != null)
                    {
                        okButton.Click();
                        Console.WriteLine("[INFO] Kliknuto 'U redu'");
                        return; // Zatvorili smo ga, izlazimo
                    }
                }
            }
        }

        public static void EnterReservationComment(FlaUI.Core.AutomationBase automation, UserInputConfig config)
        {
            var window = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("Interni prenos"))?.AsWindow();
            if (window == null)
            {
                Console.WriteLine("[ERROR] Ne mogu da nadjem interni prenos");
                return;
            }
            else
            {
                Console.WriteLine("[SUCCESS] Fokusiran prozor interni prenos ");
            }
        }


        public static void SelectTab(FlaUI.Core.AutomationBase automation, string tabName)
        {
            var tabItem = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName(tabName))?.AsTabItem();
            if (tabItem != null)
            {
                tabItem.Click();
                Thread.Sleep(200); // Kratka pauza za renderovanje
                Console.WriteLine($"[INFO] Tab '{tabName}' je selektovan.");
            }
        }

        public static void FocusCommentWithTabs(FlaUI.Core.AutomationBase automation, int tabCount, UserInputConfig config)
        {
            var window = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("Interni prenos"))?.AsWindow();
            if (window == null) return;

            window.Focus(); // Osiguraj da je prozor u fokusu
            Thread.Sleep(300);

            // 1. Pritisni TAB onoliko puta koliko je potrebno
            for (int i = 0; i < tabCount; i++)
            {
                Keyboard.Press(VirtualKeyShort.TAB);
                Thread.Sleep(100); // Kratka pauza između tabova
            }

            // 2. Sada je fokus verovatno na polju za komentar
            // Pošto je fokus tu, možemo koristiti Keyboard.Type()
            var commentBox = "";
            // formiramo string
            string fullComment = $"{config.Payment} {config.RezervationNumber}";

            commentBox = fullComment;
            Console.WriteLine($"[INFO] Unet komentar {fullComment}");

            // 3. Pritisni ENTER da potvrdiš unos
            Keyboard.Press(VirtualKeyShort.ENTER);
            Console.WriteLine("[SUCCESS] Komentar unet preko TAB navigacije.");
        }

        public static void EnterReservationComment1(FlaUI.Core.AutomationBase automation, UserInputConfig config)
        {
            var window = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("Interni prenos"))?.AsWindow();
            if (window == null) return;

            var allEdits = window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
            if (allEdits.Length > 0)
            {
                // Uzimamo prvo polje (ako je pogrešno, samo promeni indeks u [1])
                var commentBox = allEdits[0].AsTextBox();
                commentBox.Focus();
                Thread.Sleep(300);

                // Formiramo string direktno iz config-a
                string fullComment = $"{config.Payment} {config.RezervationNumber}";

                // Čistimo i kucamo
                commentBox.Text = string.Empty;
                Keyboard.Type(fullComment);

                // Enter da "zalepi" tekst u Logik
                Keyboard.Press(VirtualKeyShort.ENTER);
                Thread.Sleep(500);

                Console.WriteLine($"[SUCCESS] Upisan komentar: {fullComment}");
            }
        }

        public static bool TryClickDialog(FlaUI.Core.AutomationBase automation, string dialogName, string buttonName, int maxRetries = 10)
        {
            Console.WriteLine($"[INFO] Pokušavam da nađem dijalog: {dialogName}...");

            // Vrtimo petlju 20 puta, ali sa pauzom od samo 50 milisekundi
            // 20 puta po 50ms = tačno 1 sekunda maksimalnog čekanja ako dijalog kasni
            for (int i = 0; i < maxRetries; i++)
            {
                // Ako dijalog ne postoji, želimo da proveri brzo i da odmah ide dalje ako ga nema
                // var dialog = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName(dialogName))?.AsWindow();
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
                Thread.Sleep(100); // Proveravamo na svakih 50ms (ultra brzo, a efikasno)
            }

            Console.WriteLine($"[INFO] Dijalog '{dialogName}' se nije pojavio, idem dalje.");
            return false; // Vraća false, ali program NE PADA
        }

        public static void WaitForAndProcessInterniPrenos(FlaUI.Core.AutomationBase automation)
        {
            Console.WriteLine("[INFO] Čekam da se otvori prozor 'Interni prenos'...");

            // Čekamo do 5 sekundi (10 pokušaja po 500ms)
            FlaUI.Core.AutomationElements.Window interniWindow = null;

            for (int i = 0; i < 300; i++)
            {
                // interniWindow = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("Interni prenos"))?.AsWindow();
                interniWindow = FindWindow(automation, "Interni prenos");

                if (interniWindow != null) break;
                Thread.Sleep(50);
            }

            if (interniWindow == null)
            {
                Console.WriteLine("[ERROR] Prozor 'Interni prenos' se nije pojavio ni posle 5 sekundi!");
                return;
            }

            Console.WriteLine("[SUCCESS] Prozor 'Interni prenos' pronađen");

            // Sada imaš prozor i možeš da radiš sa tabovima i poljima
        }

        public static void PerformTabSequenceAndInput(FlaUI.Core.AutomationBase automation, UserInputConfig config)
        {
            // 1. Fokusiraj prozor "Interni prenos"
            var window = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("Interni prenos"))?.AsWindow();

            if (window != null)
            {
                window.Focus();
                Thread.Sleep(500); // Sačekaj da prozor stvarno dobije fokus

                // 2. Simuliraj 5 pritisaka tastera TAB
                for (int i = 0; i < 4; i++)
                {
                    FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.TAB);
                    Thread.Sleep(200); // Mala pauza između tabova da aplikacija stigne da odreaguje
                    Console.WriteLine($"[INFO] Pritisnut TAB {i + 1}");
                }
                string input = $"{config.Payment} {config.RezervationNumber}";

                // 3. Unesi vrednost
                FlaUI.Core.Input.Keyboard.Type(input);
                Thread.Sleep(200);
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                window.Focus();

                // Da potvrdiš unos

                Console.WriteLine($"[SUCCESS] Unesena vrednost: {input}");
            }
        }
        public static void MoveAndConfirm(int tabCount)
        {
            Console.WriteLine($"[INFO] Radim sekvencu: {tabCount} TAB-ova i ENTER.");

            // 1. Pomeranje tabovima
            for (int i = 0; i < tabCount; i++)
            {
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.TAB);
                Thread.Sleep(150); // Možemo malo smanjiti pauzu ako je mašina brza
            }

            // 2. Potvrda
            // FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);

            Console.WriteLine("[SUCCESS] Sekvenca završena.");
        }

        public static void MouseClickAndSelect(AutomationElement element)
        {
            // 1. Dobijamo tačne koordinate tog elementa na ekranu
            var point = element.GetClickablePoint();

            // 2. Pomeramo miša na te koordinate (ovo ga dovodi do polja)
            Mouse.MoveTo(point);
            Thread.Sleep(50); // Kratka pauza da miš stigne

            // 3. Klikćemo
            Mouse.Click(point);
            Thread.Sleep(50);

            // 4. Sada šaljemo tastere (pošto je miš sad sigurno tamo gde treba)
            Keyboard.Press(VirtualKeyShort.DOWN);
            Thread.Sleep(50);
            Keyboard.Press(VirtualKeyShort.ENTER);
        }

        public static void FastClickFocusedElement(AutomationElement element)
        {
            // 1. Dobijamo tačnu koordinatu elementa koji je u fokusu
            var point = element.GetClickablePoint();

            // 2. Klikćemo direktno tu (nema pomeranja miša, nema pretrage)
            // Smanjili smo Sleep na apsolutni minimum od 20ms
            Mouse.Click(point);
            Thread.Sleep(20);

            Console.WriteLine("[INFO] Brzi klik na element obavljen.");
        }
        public static void FastSpaceDownEnter()
        {
            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);

            // OBAVEZNO čekamo pola sekunde da UI stigne da iscrta taj novi mali meni na ekranu
            Thread.Sleep(500);

            // 2. Strelica dole (da pređemo na prvu opciju u tom podmeniju)
            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DOWN);
            Thread.Sleep(500);

            // 3. Enter (da potvrdimo izbor te opcije i zatvorimo podmeni)
            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
            Thread.Sleep(500);

            Console.WriteLine("[INFO] Podmeni uspesno otvoren, prva opcija izabrana.");
        }

        public static void PametniKlikNaFokusiranoDugme(FlaUI.Core.AutomationBase automation)
        {
            // 1. Pitamo Windows: "Šta je trenutno markirano (fokusirano) na ekranu?"
            var trenutnoFokusirano = automation.FocusedElement();

            if (trenutnoFokusirano != null)
            {
                // 2. Uzimamo tačne x, y koordinate tog dugmeta
                var tackaZaKlik = trenutnoFokusirano.GetClickablePoint();

                // 3. Dajemo komandu mišu da klikne tačno tu
                FlaUI.Core.Input.Mouse.Click(tackaZaKlik);
                Console.WriteLine("[INFO] Miš je uspešno kliknuo na fokusirano dugme.");

                // Čekamo malo da se taj podmeni pojavi na ekranu
                Thread.Sleep(500);

                // 4. Kad se meni otvorio od klika, strelicom dole biramo prvu opciju
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DOWN);
                Thread.Sleep(500);

                // 5. Potvrđujemo enterom
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                Console.WriteLine("[INFO] Opcija iz podmenija je izabrana.");
            }
            else
            {
                Console.WriteLine("[GRESKA] Windows ne vidi šta je fokusirano!");
            }
        }

        public static void RazdvojiRezervacijuBrzo()
        {
            Console.WriteLine("[INFO] Čekam da iskoči mali prozor...");
            Thread.Sleep(500); // Dajemo mu pola sekunde da se pojavi na ekranu

            // Šaljemo TAB koji si otkrio da radi posao
            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.TAB);
            Thread.Sleep(200);

            // Šaljemo ENTER da potvrdimo akciju na tom dugmetu
            FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);

            Console.WriteLine("[SUCCESS] Akcija 'Razdvoji rezervaciju' uspešno izvršena tastaturom!");
        }

        public static void ProknjiziInterniPrenos(FlaUI.Core.AutomationBase automation)
        {
            Console.WriteLine("[INFO] Čekam da se otvori glavni prozor 'Interni prenos'...");

            // Koristimo tvoju postojeću metodu za traženje prozora
            var interniPrenosProzor = FindWindow(automation, "Interni prenos");

            if (interniPrenosProzor != null)
            {
                Console.WriteLine("[SUCCESS] Prozor 'Interni prenos' je pronađen!");

                // Tražimo dugme 'Proknjiži' (FlaUI će ga lako naći jer ima tačno ime)
                var proknjiziDugme = interniPrenosProzor.FindFirstDescendant(cf =>
                    cf.ByName("Proknjiži").And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button)))?.AsButton();

                if (proknjiziDugme != null)
                {
                    // Uzimamo x,y koordinate dugmeta na ekranu
                    var point = proknjiziDugme.GetClickablePoint();

                    // Naređujemo pravom mišu da klikne tamo (brzina svetlosti)
                    FlaUI.Core.Input.Mouse.Click(point);
                }
                else
                {
                    Console.WriteLine("[GRESKA] Prozor je otvoren, ali ne vidim dugme 'Proknjiži'.");
                }
            }
            else
            {
                Console.WriteLine("[GRESKA] Prozor 'Interni prenos' se nije otvorio nakon duplog klika.");
            }
        }

        public static void DupliKlikNaFokusiraniRed(FlaUI.Core.AutomationBase automation)
        {
            Console.WriteLine("[INFO] Pripremam se za dupli klik na grid...");

            // Tražimo gde se trenutno nalazi fokus (plavi red u Logiku)
            var trenutniRed = automation.FocusedElement();

            if (trenutniRed != null)
            {
                try
                {
                    if (trenutniRed.Patterns.ScrollItem.IsSupported)
                    {
                        trenutniRed.Patterns.ScrollItem.Pattern.ScrollIntoView();
                        Thread.Sleep(50); // Kratka pauza da se UI osveži nakon skrola
                    }

                    // UZIMAMO PRAVOUGAONIK FOKUSIRANOG REDA
                    var rect = trenutniRed.BoundingRectangle;

                    // Računamo sigurnu tačku unutar tog reda. 
                    // rect.Left + 50 pomera miša 50 piksela udesno (izbegavamo margine)
                    // rect.Top + (rect.Height / 2) gađa tačno vertikalnu sredinu reda!
                    int x = (int)rect.Left + 50;
                    int y = (int)rect.Top + ((int)rect.Height / 2);

                    var tackaZaKlik = new System.Drawing.Point(x, y);

                    // Pomeramo miša fizički na tu tačku i radimo dvoklik
                    FlaUI.Core.Input.Mouse.Position = tackaZaKlik;
                    Thread.Sleep(100); // Kratka pauza da se miš "smiri" na novoj lokaciji

                    FlaUI.Core.Input.Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Left);
                    Console.WriteLine($"[SUCCESS] Poslat doubleClick tačno na koordinate ({x}, {y}) reda!");
                }
                catch (Exception ex) // Hvatamo opšti Exception u slučaju da element nestane
                {
                    // BEKAP PLAN: Ako i pored skrola prijavi grešku, šaljemo ENTER!
                    Console.WriteLine($"[UPOZORENJE] Greška pri kliku na red: {ex.Message}. Šaljem ENTER kao zamenu...");
                    FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                }
            }
            else
            {
                Console.WriteLine("[UPOZORENJE] Windows ne vidi fokusirani red. Pokušavam alternativu sa ENTER tasterom...");
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
            }
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
                    LogikPutanja = @"C:\Program Files (x86)\Logik\FirmA1\firma.exe",
                    KorisnickoIme = "maloprodaja",
                    Lozinka = "0202"
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




    }
}
