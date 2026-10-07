using FlaUI.Core;
using FlaUI.UIA2;
using System.Diagnostics;
using static AutomationRezToInterV1.AutomationHelpers;
using static AutomationRezToInterV1.UserInputConfig;


namespace AutomationRezToInterV1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "logovi");
            Directory.CreateDirectory(folder);
            string logPutanja = Path.Combine(folder, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            var izlaz = new DvostrukiIzlaz(Console.Out, logPutanja);
            Console.SetOut(izlaz);
            Console.WriteLine($"[INFO] Log: {logPutanja}");

            try
            {
                Run(args);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[STOP] Rucno zaustavljam");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                // neočekivana greška: upiši je u log, da znamo gde je puklo
                Console.WriteLine($"[CRASH] {ex}");
                Console.ReadKey();
            }
            finally
            {
                izlaz.Dispose();
            }
        }

        static void Run(string[] args)
        {
            /*
                        // ===== TEST INTERNI PRENOS - obrisati posle =====
            Console.WriteLine("[TEST] Imaš 8 sekundi da otvoriš meni dugmeta za razdvajanje (ako ga ima)...");
            Thread.Sleep(8000);
            using (var testAuto = new UIA2Automation())
            {
                var ip = AutomationHelpers.FindWindow(testAuto, "Interni prenos");
                if (ip != null)
                    AutomationHelpers.DumpWin32ToFile(ip.Properties.NativeWindowHandle.Value, "interni_win32.txt");
                else
                    Console.WriteLine("[TEST] Prozor Interni prenos nije nađen.");

                AutomationHelpers.DumpOpenMenus(testAuto, "interni_meni.txt");
            }
            Console.ReadKey();
            return;
            // ===== KRAJ TESTA =====
            */
            /*
            // ===== TEST DOKUMENTI - obrisati posle =====
            var podesavanja = AutomationHelpers.UcitajKonfiguracijuPrograma();
            AutomationHelpers.StartAnApplication(podesavanja.LogikPutanja);
            using (var testAuto = new UIA2Automation())
            {
                Window w = null;
                for (int i = 0; i < 75 && w == null; i++)   // do 15 s
                {
                    w = AutomationHelpers.FindLogicWindow(testAuto);
                    if (w == null) Thread.Sleep(200);
                }

                if (w == null)
                {
                    Console.WriteLine("[TEST] Logik prozor nije nađen ni posle 15 s.");
                    Console.WriteLine("[TEST] Otvoreni prozori:");
                    foreach (var p in testAuto.GetDesktop().FindAllChildren())
                        Console.WriteLine($"   '{p.Name}'");
                }
                else
                {
                    Console.WriteLine($"[TEST] Nađen prozor: '{w.Name}'");
                    AutomationHelpers.MaximizeWindow(w);
                    Thread.Sleep(1000);
                    AutomationHelpers.IzmeriPomerajDokumenti(w, 78, 828);
                    AutomationHelpers.DumpWin32ToFile(w.Properties.NativeWindowHandle.Value, "main_win32.txt");
                    AutomationHelpers.DumpWindowToFile(w, "main_uia.txt");
                    Console.WriteLine("[TEST] Dumpovi snimljeni.");
                }
            }
            Console.ReadKey();
            return;
            // ===== KRAJ TESTA =====

            */

            FlaUI.Core.Input.Mouse.MovePixelsPerMillisecond = 20;   // brzo pomeranje miša umesto animacije
            Stopwatch ukupnaStoperica = Stopwatch.StartNew();
            bool zavrseno = false;
            Console.WriteLine("[START] proces automacije zapocet");

            var (ukupnoCek, ukupnoGotovina) = AplikacijaStatistika.UcitajStatistiku();
            Console.WriteLine($"[STATISTIKA] Do sada urađeno -> Ček: {ukupnoCek} | Gotovina: {ukupnoGotovina}");

            // 1. Učitavamo konfiguraciju iz fajla
            AppConfig osnovnaPodesavanja = AutomationHelpers.UcitajKonfiguracijuPrograma();



            // uictavanje konfiguracije 

            var config = UserInputConfig.LoadConfiguration();
            RunControl.StartHotkeyWatcher();

            Console.WriteLine("[INFO] Tokom rada: PAUSE = pauza/nastavak | SCROLL LOCK = prekid");

            if (string.IsNullOrEmpty(config.Username))   // F1 -> nalog iz appsettings.json
            {
                config.Username = osnovnaPodesavanja.KorisnickoIme;
                config.Password = osnovnaPodesavanja.Lozinka;

            }


            // 2. Umesto stare hardkodirane putanje, koristimo onu iz fajla
            string pathToExeFile = osnovnaPodesavanja.LogikPutanja;

            // 3. Startujemo aplikaciju




            Application app = AutomationHelpers.StartAnApplication(pathToExeFile);

            if (app == null)
            {
                Console.WriteLine("[ERROR] Aplikacija nije mogla da se startuje");
                Console.ReadKey();
                return;
            }

            using (UIA2Automation automation = new UIA2Automation())
            {
                // traznjenje glavnog prozora 
                FlaUI.Core.AutomationElements.Window targetWindow = null;

                AutomationHelpers.StopWatchSteps("Find logic window", () =>
                {
                    targetWindow = AutomationHelpers.CekajLogikProzor(automation, app.ProcessId);
                });

                if (targetWindow != null)
                {
                    Console.WriteLine("[SUCCESS] Pronasao sam Logik prozor");

                    // klik na dokument dugme sa leve strane preko mouse clika 
                    FlaUI.Core.AutomationElements.Window loginWindow = null;
                    bool kliknuto = false;

                    AutomationHelpers.StopWatchSteps("Maximuzuj Logik", () =>
                    {
                        AutomationHelpers.MaximizeWindow(targetWindow);
                    });

                    AutomationHelpers.StopWatchSteps("Dokumenti (Ctrl+F5)", () =>
                    {
                        AutomationHelpers.OtvoriDokumentiPrecicom(targetWindow);
                    });
                    AutomationHelpers.StopWatchSteps("Cekanje prozora Prijava", () =>
                    {
                        loginWindow = AutomationHelpers.WaitForWindow(automation, "Prijava");
                    });

                    // rezerva: ako prečica nije otvorila Prijavu, probaj klikom
                    if (loginWindow == null)
                    {
                        Console.WriteLine("[WARNING] Ctrl+F5 nije otvorio Prijavu, probam klikom na Dokumenti...");
                        if (AutomationHelpers.KlikniDokumenti(targetWindow, 70, 140))
                            loginWindow = AutomationHelpers.WaitForWindow(automation, "Prijava");
                    }
                    kliknuto = loginWindow != null;


                    if (loginWindow != null)
                    {
                        AutomationHelpers.StopWatchSteps("Maksimuzuj Logik Firma", () =>
                        {
                            AutomationHelpers.MaximizeWindow(loginWindow);
                        });

                        // loogovanje naa prozor prijava 
                        AutomationHelpers.StopWatchSteps("Prijava", () =>
                        {
                            AutomationHelpers.PerformLogicLogin(loginWindow, config);
                        });
                        //AutomationHelpers.PerformLogicLogin(loginWindow, config);
                        // čekamo da prozor Prijava nestane (max 5 s)
                        for (int i = 0; i < 50 && AutomationHelpers.FindWindow(automation, "Prijava") != null; i++)
                            RunControl.Sleep(100);
                        RunControl.Sleep(200);

                        var logikProzor = targetWindow;
                        if (logikProzor != null)
                        {
                            Console.WriteLine("[INFO] Pocinjem proces pretrage i unosa rezervacije");

                            /*
                            // pretraga rezervacije 
                            AutomationHelpers.StopWatchSteps("Pretraga Rezervacije", () => {
                                AutomationHelpers.ClickSpecificFieldInList(logikProzor, 0, 4);

                            });
                            */

                            FlaUI.Core.AutomationElements.AutomationElement myDocument = null;
                            AutomationHelpers.StopWatchSteps("Postavi tip Rezervacija", () =>
                            {
                                myDocument = AutomationHelpers.PostaviTipDokumenta(logikProzor, "Rezervacija");
                            });
                            if (myDocument == null)
                            {
                                Console.WriteLine("[STOP] Tip dokumenta nije Rezervacija. Ništa nije otknjiženo.");
                                Console.ReadKey();
                                return;
                            }



                            if (myDocument != null)
                            {
                                // unos broja rezervacije
                                AutomationHelpers.StopWatchSteps("Enter reservation number", () =>
                                {
                                    AutomationHelpers.EnterReservationNumber(logikProzor, myDocument, config);
                                });


                                RunControl.Sleep(300);

                                // dvoklik na prvu rezervaciju 
                                AutomationHelpers.StopWatchSteps("Select first in grid", () =>
                                {
                                    AutomationHelpers.SelectFirstInGridWithoutSearch(logikProzor);
                                });

                                if (!AutomationHelpers.ProveriRezervacijuPreOtknjizavanja(automation, config))
                                {
                                    Console.ReadKey();
                                    return;
                                }


                                // otknizavanje 
                                bool success = false;
                                AutomationHelpers.StopWatchSteps("Otknjizvanje rezervacije ", () =>
                                {
                                    success = AutomationHelpers.UnbookReservation(automation);
                                });



                                if (!success)
                                {
                                    Console.WriteLine("[STOP] Otknjižavanje nije uspelo. Proveri rezervaciju u Logiku ručno.");
                                    Console.ReadKey();
                                    return;
                                }
                                Console.WriteLine("[INFO] Otknjiženo uspešno.");

                                Console.WriteLine("[INFO] Počinjem sekvencu kopiranja...");
                                var rezWindow = AutomationHelpers.FindWindow(automation, "Rezervacija");
                                if (rezWindow == null)
                                {
                                    Console.WriteLine("[STOP] Ne nalazim prozor Rezervacija. Rezervacija JE otknjižena, nastavi ručno od Kopiraj.");
                                    Console.ReadKey();
                                    return;
                                }

                                AutomationHelpers.StopWatchSteps("Klik na Kopiraj", () =>
                                {
                                    AutomationHelpers.ClickAndMeasure(rezWindow, "Kopiraj");
                                });

                                bool kopiranoOk = false;
                                AutomationHelpers.StopWatchSteps("Izaberi 'U interni prenos'", () =>
                                {
                                    kopiranoOk = AutomationHelpers.IzaberiStavkuMenija(automation, "U interni prenos");
                                });
                                if (!kopiranoOk)
                                {
                                    Console.WriteLine("[STOP] Nisam izabrao 'U interni prenos'. Rezervacija JE otknjižena, nastavi ručno od Kopiraj.");
                                    Console.ReadKey();
                                    return;
                                }

                                bool noPressed = false;
                                AutomationHelpers.StopWatchSteps("Ne uzimaj cene iz magacina", () =>
                                {
                                    noPressed = AutomationHelpers.ClickButtonOnDialog(automation, "Potvrda", "Ne");
                                });

                                if (noPressed)
                                {
                                    Console.WriteLine("[INFO] Odabir cena ");
                                }
                                else
                                {
                                    Console.WriteLine("[ERROR] nije se pojavio prozor potvrda cene ");
                                }

                                bool warningHandled = false;
                                AutomationHelpers.StopWatchSteps("Upozorenje za ulazni magacin", () =>
                                {


                                    warningHandled = AutomationHelpers.TryClickDialog(automation, "Upozorenje", "U redu");
                                });

                                if (warningHandled)
                                {
                                    Console.WriteLine("[SUCCESS] Prozor 'Upozorenje' zatvoren klikon na 'U redu'");
                                }
                                else
                                {
                                    Console.WriteLine("[ERROR] Prozor 'Upozorenje se nije pojavilo'");
                                }

                                // automatizaij za magacine 
                                bool warehouseHandled = false;
                                AutomationHelpers.StopWatchSteps("Prvi magacin", () =>
                                {
                                    warehouseHandled = AutomationHelpers.ClickButtonOnDialog(automation, "Magacini", "U redu");
                                });



                                if (!warehouseHandled)
                                {
                                    Console.WriteLine("[STOP] Prozor 'Magacini' (ulazni) nije obrađen. Interni prenos NIJE proknjižen. Proveri u Logiku ručno.");
                                    Console.ReadKey();
                                    return;
                                }
                                Console.WriteLine("[SUCCESS] Prozor 'Magacini' zatvoren klikom 'U redu'");
                                // upozeorenje za izlazni magacin

                                bool warningHandledExitWarehouse = false;
                                AutomationHelpers.StopWatchSteps("Izlazni magacin ", () =>
                                {
                                    warningHandledExitWarehouse = AutomationHelpers.TryClickDialog(automation, "Upozorenje", "U redu");
                                });


                                if (warningHandledExitWarehouse)
                                {
                                    Console.WriteLine("[SUCCESS] Prozor 'Upozorenje' zatvoren klikon na 'U redu'");
                                }
                                else
                                {
                                    Console.WriteLine("[ERROR] Prozor 'Upozorenje se nije pojavilo'");
                                }

                                // AutomationHelpers.DeepSearch(automation.GetDesktop(), 0);
                                bool warehouseExitSelected = false;
                                AutomationHelpers.StopWatchSteps("mp magacin", () =>
                                {
                                    warehouseExitSelected = AutomationHelpers.IzaberiMagacinWin32(automation, "MP");
                                });



                                if (!warehouseExitSelected)
                                {
                                    Console.WriteLine("[STOP] MP magacin nije izabran. Interni prenos NIJE proknjižen. Proveri magacine ručno.");
                                    Console.ReadKey();
                                    return;
                                }
                                Console.WriteLine("[SUCCESS] Magacin MP je selektovan");

                                string prviProzor = null;
                                AutomationHelpers.StopWatchSteps("Upozorenje o količini / Obaveštenje", () =>
                                {
                                    prviProzor = AutomationHelpers.CekajPrviOdProzora(automation, new[] { "Upozorenje", "Obaveštenje" });
                                });

                                if (prviProzor == "Upozorenje")
                                {
                                    AutomationHelpers.TryClickDialog(automation, "Upozorenje", "U redu");
                                    Console.WriteLine("[INFO] Upozorenje o količini zatvoreno.");
                                }

                                bool infoAboutCreatingInternal = false;
                                AutomationHelpers.StopWatchSteps("Kreiranje internog naloga", () =>
                                {
                                    infoAboutCreatingInternal = AutomationHelpers.TryClickDialog(automation, "Obaveštenje", "U redu", 30);
                                });
                                if (!infoAboutCreatingInternal)
                                {
                                    Console.WriteLine("[STOP] Nije se pojavilo obaveštenje o kreiranju internog prenosa. Proveri u Logiku ručno.");
                                    Console.ReadKey();
                                    return;
                                }
                                Console.WriteLine("[SUCCESS] Interni prenos kreiran.");


                                //AutomationHelpers.EnterReservationComment(automation, config);
                                bool ipOtvoren = false;
                                AutomationHelpers.StopWatchSteps("Cekam na interni prenos", () =>
                                {
                                    ipOtvoren = AutomationHelpers.WaitForAndProcessInterniPrenos(automation);
                                });
                                if (!ipOtvoren)
                                {
                                    Console.WriteLine("[STOP] Interni prenos se nije otvorio. Proveri da li je ostao otvoren neki dijalog (npr. Obaveštenje).");
                                    AutomationHelpers.IspisiProzoreProcesa(automation, app.ProcessId);
                                    Console.ReadKey();
                                    return;
                                }

                                bool magacinOk = false;
                                AutomationHelpers.StopWatchSteps("Provera magacina", () =>
                                {
                                    magacinOk = AutomationHelpers.ProveriMagacinInternogPrenosa(automation, "MP");
                                });
                                if (!magacinOk)
                                {
                                    Console.WriteLine("[STOP] Interni prenos nema MP magacin. Kreiran je, ali NIJE proknjižen. Ispravi magacin ručno.");
                                    AutomationHelpers.IspisiProzoreProcesa(automation, app.ProcessId);
                                    Console.ReadKey();
                                    return;
                                }

                                bool komentarOk = false;
                                AutomationHelpers.StopWatchSteps("Upis komentara", () =>
                                {
                                    komentarOk = AutomationHelpers.UnesiKomentarInterniPrenos(automation, $"{config.Payment} {config.RezervationNumber}");
                                });
                                if (!komentarOk)
                                {
                                    Console.WriteLine("[STOP] Komentar nije upisan. Interni prenos je kreiran, ali NIJE proknjižen. Nastavi ručno od komentara.");
                                    Console.ReadKey();
                                    return;
                                }

                                var ipProzor = AutomationHelpers.FindWindow(automation, "Interni prenos");
                                bool povezaniOk = ipProzor != null;

                                if (povezaniOk)
                                    AutomationHelpers.StopWatchSteps("Klik na Povezani dok.", () =>
                                    {
                                        povezaniOk = AutomationHelpers.KlikniDugmeWin32(ipProzor, "Povezani dok.");
                                    });

                                if (povezaniOk)
                                    AutomationHelpers.StopWatchSteps("Izaberi 'Rezervacija'", () =>
                                    {
                                        povezaniOk = AutomationHelpers.IzaberiStavkuMenija(automation, "Rezervacija");
                                    });

                                if (!povezaniOk)
                                {
                                    Console.WriteLine("[STOP] Povezani dok. nije uspelo. Interni prenos je kreiran sa komentarom, ali NIJE proknjižen. Nastavi ručno od Povezani dok.");
                                    Console.ReadKey();
                                    return;
                                }
                                /*
                                // PRIVREMENO: snimamo mali prozor koji iskoči, da vidimo njegova dugmad
                                RunControl.Sleep(800);
                                AutomationHelpers.DumpForegroundWindow("mali_prozor.txt");
                                */

                                bool razdvojenoOk = false;
                                AutomationHelpers.StopWatchSteps("Razdvoji rezervaciju", () =>
                                {
                                    razdvojenoOk = AutomationHelpers.RazdvojiRezervaciju(automation, config);
                                });
                                if (!razdvojenoOk)
                                {
                                    Console.WriteLine("[STOP] Razdvajanje nije urađeno. Interni prenos je kreiran sa komentarom, ali NIJE proknjižen. Nastavi ručno od Razdvoji rezervaciju.");
                                    Console.ReadKey();
                                    return;
                                }


                                Console.WriteLine("[INFO] Čekam 3 sekunde da se baza osveži...");
                                for (int i = 0; i < 20; i++)
                                {
                                    RunControl.Sleep(50);
                                }
                                Console.WriteLine("[INFO] Baza osvezena, otvaram interni prenos");

                                Console.WriteLine("[INFO] Šaljem ENTER da otvorim Interni prenos...");

                                bool gridOk = false;
                                AutomationHelpers.StopWatchSteps("Dvoklik na grid dokumenata", () =>
                                {
                                    gridOk = AutomationHelpers.DupliKlikNaGridDokumenata(logikProzor);
                                });
                                if (!gridOk)
                                {
                                    Console.WriteLine("[STOP] Ne mogu da otvorim interni prenos iz liste. Razdvojeno je, ali NIJE proknjiženo. Nastavi ručno.");
                                    AutomationHelpers.IspisiProzoreProcesa(automation, app.ProcessId);
                                    Console.ReadKey();
                                    return;
                                }



                                // 4. Čekamo još 2 sekunde da se prozor fizički pojavi na ekranu
                                Console.WriteLine("[INFO] Čekam 2 sekunde da se prozor otvori...");


                                AutomationHelpers.StopWatchSteps("Cekanje da se otvori interni prenos", () =>
                                {
                                    FlaUI.Core.AutomationElements.AutomationElement prozor = null;

                                    for (int i = 0; i < 100; i++)
                                    {
                                        prozor = AutomationHelpers.FindWindow(automation, "Interni prenos");

                                        if (prozor != null)
                                        {

                                            break;
                                        }
                                        RunControl.Sleep(50);

                                    }
                                    if (prozor != null)
                                    {
                                        Console.WriteLine("[INFO] Prozor se otvorio, nastavljam odmah");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[ERROR] Prozor se nije otvorio na vreme!");
                                    }
                                });

                                if (!AutomationHelpers.ProveriKomentarInternogPrenosa(automation, $"{config.Payment} {config.RezervationNumber}"))
                                {
                                    Console.WriteLine("[STOP] Nije otvoren pravi interni prenos. Ništa nije proknjiženo. Nastavi ručno od otvaranja internog prenosa.");
                                    Console.ReadKey();
                                    return;
                                }

                                // 5. Sada konačno tražimo dugme i klikćemo Proknjiži
                                bool proknjiziKlik = false;
                                AutomationHelpers.StopWatchSteps("Proknjiži interni prenos", () =>
                                {
                                    proknjiziKlik = AutomationHelpers.ProknjiziInterniPrenos(automation);
                                });
                                if (!proknjiziKlik)
                                {
                                    Console.WriteLine("[STOP] Nisam kliknuo Proknjiži. Interni prenos je razdvojen, ali NIJE proknjižen. Proknjiži ručno.");
                                    Console.ReadKey();
                                    return;
                                }

                                bool yesPressed = false;
                                AutomationHelpers.StopWatchSteps("Potvrda knjiženja", () =>
                                {
                                    yesPressed = AutomationHelpers.TryClickDialog(automation, "Potvrda", "Da", 10);
                                });
                                if (!yesPressed)
                                {
                                    Console.WriteLine("[STOP] Nije potvrđeno knjiženje. Proveri u Logiku da li je interni prenos proknjižen.");
                                    Console.ReadKey();
                                    return;
                                }
                                Console.WriteLine("[INFO] Knjiženje internog prenosa potvrđeno.");
                                // 8. Čekamo sekundu-dve da Logik to sažvaće i da popup nestane
                                for (int i = 0; i < 20; i++)
                                {
                                    // proveravamo da li je prozor porvrda tu jos uvek 
                                    var potvProzor = AutomationHelpers.FindWindow(automation, "Potvrda");
                                    if (potvProzor == null)
                                    {
                                        break;
                                    }
                                    RunControl.Sleep(50);
                                }

                                // 9. ZAVRŠNI KLIK - Zatvaramo Interni prenos!
                                AutomationHelpers.StopWatchSteps("Zatvori interni prenos ", () =>
                                {
                                    AutomationHelpers.ZatvoriInterniPrenosUredu(automation);
                                });

                                // ===== DEO 2: ponovo otvori rezervaciju (priprema za kasu) =====
                                // čekamo da se Interni prenos zatvori (max 3 s)
                                for (int i = 0; i < 30 && AutomationHelpers.FindWindow(automation, "Interni prenos") != null; i++)
                                    RunControl.Sleep(100);
                                RunControl.Sleep(200);

                                bool pronadjiOk = false;
                                AutomationHelpers.StopWatchSteps("Klik na Pronađi", () =>
                                {
                                    pronadjiOk = AutomationHelpers.KlikniDugmeWin32(logikProzor, "Pronađi", "TInvoiceNavigatorForm");
                                });
                                if (!pronadjiOk)
                                {
                                    Console.WriteLine("[STOP] Nisam kliknuo Pronađi. Interni prenos je ZAVRŠEN, slanje na kasu uradi ručno.");
                                    Console.ReadKey();
                                    return;
                                }

                                RunControl.Sleep(700);   // da se lista osveži
                                AutomationHelpers.StopWatchSteps("Ponovo otvori rezervaciju", () =>
                                {
                                    AutomationHelpers.SelectFirstInGridWithoutSearch(logikProzor);
                                });

                                if (!AutomationHelpers.ProveriRezervacijuPreOtknjizavanja(automation, config, "REZERVACIJA SPREMNA ZA KASU", traziPotvrdu: false))
                                {
                                    Console.WriteLine("[STOP] Nije otvorena prava rezervacija. Interni prenos je ZAVRŠEN, slanje na kasu uradi ručno.");
                                    Console.ReadKey();
                                    return;
                                }
                                // Ovde će ići slanje u Logik Kasu, kad bude smelo da se testira.

                                // ===== SLANJE NA KASU =====
                                bool slanjeNaKasu = true;   // prebaci na true kad kolega odobri

                                if (slanjeNaKasu && !AutomationHelpers.PosaljiNaKasu(automation))
                                {
                                    Console.WriteLine("[STOP] Slanje na kasu nije uspelo. Interni prenos je ZAVRŠEN, pošalji ručno.");
                                    Console.ReadKey();
                                    return;
                                }

                                bool zatvorenoOk = false;
                                AutomationHelpers.StopWatchSteps("Zatvori rezervaciju", () =>
                                {
                                    zatvorenoOk = AutomationHelpers.ZatvoriRezervaciju(automation, "U redu");
                                });
                                if (!zatvorenoOk)
                                {
                                    Console.WriteLine("[STOP] Sve je urađeno, samo Rezervacija nije zatvorena. Zatvori je ručno.");
                                    AutomationHelpers.IspisiProzoreProcesa(automation, app.ProcessId);
                                    Console.ReadKey();
                                    return;
                                }

                                Console.WriteLine("[KRAJ] Proces automatizacije je uspešno završen od početka do kraja!");
                                zavrseno = true;


                                // AutomationHelpers.DeepSearch(automation.GetDesktop(), 0);



                            }
                            else
                            {
                                Console.WriteLine("[ERROR] Nije pronadjen ciljani dokument elemtn za  unos ");
                            }


                        }
                        else
                        {
                            Console.WriteLine("[ERROR] Glavni prozor logi firma se nije pojavio");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] Prozor prijava se nije pojavio na vreme");
                    }



                }
            }
            ukupnaStoperica.Stop();
            Console.WriteLine($"[INFO] KOMPLETAN PROCES ZAVRŠEN ZA: {ukupnaStoperica.Elapsed.TotalSeconds:F2} sekundi.");
            Console.WriteLine("[KRAJ] Proces automatizacije je zavrsen");

            // Na kraju uspešnog procesa:
            if (zavrseno)
            {
                string izabraniNacin = (config.Payment == PaymentOption.Cek) ? "Cek" : "Gotovina";
                AplikacijaStatistika.SacuvajStatistiku(izabraniNacin);
                var (noviCek, novaGotovina) = AplikacijaStatistika.UcitajStatistiku();
                Console.WriteLine("==========================================");
                Console.WriteLine($" UKUPAN SKOR: Ček: {noviCek} | Gotovina: {novaGotovina}");
                Console.WriteLine("==========================================");
            }
            else
            {
                Console.WriteLine("[INFO] Proces nije završen do kraja, statistika nije menjana.");
            }
            Console.ReadKey();
        }
    }
}
