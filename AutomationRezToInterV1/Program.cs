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
            Console.Title = "Automatizacija MP";  // novi nazi foldera 

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

        static void Zaustavi(string poruka, FlaUI.Core.AutomationBase automation, int processId)
        {
            Console.WriteLine($"[STOP] {poruka}");
            AutomationHelpers.IspisiProzoreProcesa(automation, processId);
            Console.ReadKey();
        }

        static void Run(string[] args)
        {
           

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

            AutomationHelpers.LogikProcessId = app.ProcessId; // samo logic procese

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
                                    Zaustavi("Otknjižavanje nije uspelo. Proveri rezervaciju u Logiku ručno.", automation, app.ProcessId);
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

                                if (!noPressed)
                                {
                                    Zaustavi("Nije se pojavio prozor Potvrda za cene. Rezervacija JE otknjižena, nastavi ručno od Kopiraj.", automation, app.ProcessId);
                                    return;
                                }
                                Console.WriteLine("[INFO] Odabir cena");

                                // ===== ULAZNI MAGACIN (ostaje 1) =====

                                bool warningHandled = false;
                                AutomationHelpers.StopWatchSteps("Upozorenje: ulazni magacin", () =>
                                {
                                    warningHandled = AutomationHelpers.TryClickDialog(automation, "Upozorenje", "U redu");
                                });
                                Console.WriteLine(warningHandled
                                    ? "[SUCCESS] Upozorenje za ULAZNI magacin zatvoreno."
                                    : "[INFO] Upozorenje za ULAZNI magacin se nije pojavilo.");


                                // automatizaij za magacine 
                                bool ulazniOk = false;
                                AutomationHelpers.StopWatchSteps("Ulazni magacin (1): U redu", () =>
                                {
                                    ulazniOk = AutomationHelpers.ClickButtonOnDialog(automation, "Magacini", "U redu", 15000);
                                });
                                if (!ulazniOk)
                                {
                                    Zaustavi("Prozor Magacini za ULAZNI magacin nije obrađen. Rezervacija JE otknjižena. U prozoru Magacini samo klikni 'U redu', pa nastavi ručno.", automation, app.ProcessId);
                                    return;
                                }
                                Console.WriteLine("[SUCCESS] ULAZNI magacin potvrđen (ostaje 1).");

                                // ===== IZLAZNI MAGACIN (MP) =====
                                bool warningIzlazni = false;
                                AutomationHelpers.StopWatchSteps("Upozorenje: izlazni magacin", () =>
                                {
                                    warningIzlazni = AutomationHelpers.TryClickDialog(automation, "Upozorenje", "U redu");
                                });
                                Console.WriteLine(warningIzlazni
                                    ? "[SUCCESS] Upozorenje za IZLAZNI magacin zatvoreno."
                                    : "[INFO] Upozorenje za IZLAZNI magacin se nije pojavilo.");

                                bool izlazniOk = false;
                                AutomationHelpers.StopWatchSteps("Izlazni magacin (MP)", () =>
                                {
                                    izlazniOk = AutomationHelpers.IzaberiMagacinWin32(automation, "MP");
                                });
                                if (!izlazniOk)
                                {
                                    Zaustavi("IZLAZNI magacin MP nije izabran. Rezervacija JE otknjižena. U prozoru Magacini upiši MP, Pretraga, pa 'U redu', i nastavi ručno.", automation, app.ProcessId);
                                    return;
                                }
                                Console.WriteLine("[SUCCESS] IZLAZNI magacin MP izabran.");

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
                                bool slanjeNaKasu = !config.TestRezim;  
                                if (!slanjeNaKasu)
                                    Console.WriteLine("[TEST] Test režim: slanje na kasu preskočeno.");   

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
            if (zavrseno && !config.TestRezim)
            {
                string izabraniNacin = (config.Payment == PaymentOption.Cek) ? "Cek" : "Gotovina";
                AplikacijaStatistika.SacuvajStatistiku(izabraniNacin);
                var (noviCek, novaGotovina) = AplikacijaStatistika.UcitajStatistiku();
                Console.WriteLine("==========================================");
                Console.WriteLine($" UKUPAN SKOR: Ček: {noviCek} | Gotovina: {novaGotovina}");
                Console.WriteLine("==========================================");
            }
            else if (zavrseno)
            {
                Console.WriteLine("[TEST] Test prolaz uspešan, statistika nije menjana.");
            }
            else
            {
                Console.WriteLine("[INFO] Proces nije završen do kraja, statistika nije menjana.");
            }
            Console.ReadKey();
        }
    }
}
