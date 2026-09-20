using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA2;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using static AutomationRezToInterV1.AutomationHelpers;
using static AutomationRezToInterV1.UserInputConfig;
using static System.Net.Mime.MediaTypeNames;


namespace AutomationRezToInterV1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // prioritet 
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;

            Stopwatch ukupnaStoperica = Stopwatch.StartNew();
            Console.WriteLine("[START] proces automacije zapocet");

            var (ukupnoCek, ukupnoGotovina) = AplikacijaStatistika.UcitajStatistiku();
            Console.WriteLine($"[STATISTIKA] Do sada urađeno -> Ček: {ukupnoCek} | Gotovina: {ukupnoGotovina}");

            // 1. Učitavamo konfiguraciju iz fajla
            AppConfig osnovnaPodesavanja = AutomationHelpers.UcitajKonfiguracijuPrograma();



            // uictavanje konfiguracije 

            var config = UserInputConfig.LoadConfiguration();
            config.Username = osnovnaPodesavanja.KorisnickoIme;
            config.Password = osnovnaPodesavanja.Lozinka;


            // 2. Umesto stare hardkodirane putanje, koristimo onu iz fajla
            string pathToExeFile = osnovnaPodesavanja.LogikPutanja;

            // 3. Startujemo aplikaciju


            // string pathToExeFile = @"C:\Program Files (x86)\Logik\FirmA1\firma.exe";

            FlaUI.Core.Application app = AutomationHelpers.StartAnApplication(pathToExeFile);

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
                    targetWindow = AutomationHelpers.FindLogicWindow(automation);
                });

                if (targetWindow != null)
                {
                    Console.WriteLine("[SUCCESS] Pronasao sam Logik prozor");

                    // klik na dokument dugme sa leve strane preko mouse clika 
                    FlaUI.Core.AutomationElements.Window loginWindow = null;
                    bool kliknuto = false;


                    AutomationHelpers.StopWatchSteps("Safe Click to Documents", () =>
                    {
                        kliknuto = AutomationHelpers.PerformSafeClickToDocuments(targetWindow, 78, 828);
                    });

                    if (kliknuto)
                    {
                        // trazenje prozora prijava 
                        AutomationHelpers.StopWatchSteps("Cekanje prozora Prijava", () =>
                        {
                            loginWindow = AutomationHelpers.WaitForWindow(automation, "Prijava");
                        });



                        if (loginWindow != null)
                        {
                            // loogovanje naa prozor prijava 
                            AutomationHelpers.StopWatchSteps("Prijava", () => {
                                AutomationHelpers.PerformLogicLogin(loginWindow, config);
                            });
                            //AutomationHelpers.PerformLogicLogin(loginWindow, config);
                            Thread.Sleep(500);

                            // trazimo prozor logik firma posto je nakon prijave promenjen prozor
                            var logikProzor = AutomationHelpers.FindWindow(automation, "Logik Firma");

                            if (logikProzor != null)
                            {
                                Console.WriteLine("[INFO] Pocinjem proces pretrage i unosa rezervacije");

                                // pretraga rezervacije 
                                AutomationHelpers.StopWatchSteps("Pretraga Rezervacije", () => {
                                    AutomationHelpers.ClickSpecificFieldInList(logikProzor, 0, 4);
                                });
                                //AutomationHelpers.ClickSpecificFieldInList(logikProzor, 0, 4);
                                Thread.Sleep(200);

                                // pronalazenje specificnok prozora 
                                FlaUI.Core.AutomationElements.AutomationElement myDocument = null;

                                AutomationHelpers.StopWatchSteps("Get specific dokument", () =>
                                {
                                    myDocument = AutomationHelpers.GetSpecificDocument(logikProzor, 0);
                                });

                                if (myDocument != null)
                                {
                                    // unos broja rezervacije
                                    AutomationHelpers.StopWatchSteps("Enter reservation number", () => {
                                        AutomationHelpers.EnterReservationNumber(logikProzor, myDocument, config);
                                    });


                                    Thread.Sleep(300);

                                    // dvoklik na prvu rezervaciju 
                                    AutomationHelpers.StopWatchSteps("Select first in grid", () => {
                                        AutomationHelpers.SelectFirstInGridWithoutSearch(logikProzor);
                                    });

                                    Thread.Sleep(800);

                                    // otknizavanje 
                                    bool success = false;
                                    AutomationHelpers.StopWatchSteps("Otknjizvanje rezervacije ", () => {
                                        success = AutomationHelpers.UnbookReservation(automation);
                                    });



                                    Console.WriteLine("[INFO] Otknjizvanje uspesno potvrdjujem enterom");


                                    Console.WriteLine("[INFO] Počinjem sekvencu kopiranja...");
                                    var rezWindow = automation.GetDesktop().FindFirstDescendant(cf => cf.ByName("Rezervacija"))?.AsWindow();

                                    if (rezWindow != null)
                                    {
                                        // Ovde sad svaki korak meri vreme i ispisuje ga u konzolu
                                        AutomationHelpers.StopWatchSteps("Klikni i premeri", () => {
                                            AutomationHelpers.ClickAndMeasure(rezWindow, "Kopiraj");
                                        });



                                        // Koreografija nakon klika
                                        AutomationHelpers.StopWatchSteps("Kopiraj u interni prenos", () => {
                                            AutomationHelpers.ExecutePresses((VirtualKeyShort.UP, 8, 150), (VirtualKeyShort.ENTER, 1, 150));
                                        });


                                        Console.WriteLine("[INFO] Sekvenca kopiranja završena.");
                                    }
                                    bool noPressed = false;
                                    AutomationHelpers.StopWatchSteps("Ne uzimaj cene iz magacina", () => {
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
                                    AutomationHelpers.StopWatchSteps("Upozorenje za ulazni magacin", () => {


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
                                    AutomationHelpers.StopWatchSteps("Prvi magacin", () => {
                                        warehouseHandled = AutomationHelpers.ClickButtonOnDialog(automation, "Magacini", "U redu");
                                    });



                                    if (warehouseHandled)
                                    {
                                        Console.WriteLine("[SUCCESS] Prozor 'Magacini' zatvoren klikom 'U redu'");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[ERROR] Nije pronadjen prozor 'Magacini' ili dugme 'U redu'");
                                    }
                                    // upozeorenje za izlazni magacin

                                    bool warningHandledExitWarehouse = false;
                                    AutomationHelpers.StopWatchSteps("Izlazni magacin ", () => {
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
                                    AutomationHelpers.StopWatchSteps("mp magacin", () => {
                                        warehouseExitSelected = AutomationHelpers.SelectAndConfirmExitWarehouse(automation, "MP");
                                    });



                                    if (warehouseExitSelected)
                                    {
                                        Console.WriteLine($"[SUCCESS] Magacin MP je selektovan");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[ERROR] Neuspeh pri selekcije MP magacina");
                                    }

                                    bool warning = false;
                                    AutomationHelpers.StopWatchSteps("Upozorenje o kolicini ", () => {
                                        warning = AutomationHelpers.TryClickDialog(automation, "Upozorenje", "U redu");
                                    });




                                    if (warning)
                                    {
                                        Console.WriteLine("[SUCCESS] Prozor upozorenje");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[ERROR] pzrozo upozorenje");
                                    }
                                    bool infoAboutCreatingInternal = false;
                                    AutomationHelpers.StopWatchSteps("Kreiranje internog naloga", () => {
                                        infoAboutCreatingInternal = AutomationHelpers.TryClickDialog(automation, "Obaveštenje", "U redu");
                                    });



                                    if (infoAboutCreatingInternal)
                                    {
                                        Console.WriteLine("[SUCCESS] Prozor 'Obavestenje' o kreiranju insternog prenosa");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[ERROR] Prozor Obavestenje' o kreiranju insternog prenosa se nije pojavilo'");
                                    }


                                    //AutomationHelpers.EnterReservationComment(automation, config);
                                    AutomationHelpers.StopWatchSteps("Cekam na interni prenos ", () => {
                                        AutomationHelpers.WaitForAndProcessInterniPrenos(automation);
                                    });

                                    //AutomationHelpers.EnterReservationComment(automation, config);
                                    AutomationHelpers.StopWatchSteps("Tab klikovi do unosa vrednosti", () => {
                                        AutomationHelpers.PerformTabSequenceAndInput(automation, config);
                                    });

                                    AutomationHelpers.StopWatchSteps("4 taba do dugmeta razdvoj rezervaciju ", () => {
                                        AutomationHelpers.MoveAndConfirm(4);
                                    });


                                    //AutomationHelpers.FastSpaceDownEnter();
                                    AutomationHelpers.StopWatchSteps("Klik na fokusirano dugme razvoj rezervaciju ", () => {
                                        AutomationHelpers.PametniKlikNaFokusiranoDugme(automation);
                                    });

                                    AutomationHelpers.StopWatchSteps("Razdvoj rezervaciju", () => {
                                        AutomationHelpers.RazdvojiRezervacijuBrzo();
                                    });

                                    Console.WriteLine("[INFO] Čekam 3 sekunde da se baza osveži...");
                                    for (int i = 0; i < 20; i++)
                                    {
                                        Thread.Sleep(50);
                                    }
                                    Console.WriteLine("[INFO] Baza osvezena, otvaram interni prenos");

                                    Console.WriteLine("[INFO] Šaljem ENTER da otvorim Interni prenos...");

                                    AutomationHelpers.StopWatchSteps("Dupli klik na fokusirani red", () => {
                                        AutomationHelpers.DupliKlikNaFokusiraniRed(automation);
                                    });

                                    FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);

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
                                            Thread.Sleep(50);

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



                                    // 5. Sada konačno tražimo dugme i klikćemo Proknjiži
                                    AutomationHelpers.StopWatchSteps("Proknizi interni prenos", () => {
                                        AutomationHelpers.ProknjiziInterniPrenos(automation);
                                    });

                                    bool yesPressed = false;
                                    AutomationHelpers.StopWatchSteps("Clik na potvrdu da ", () => {
                                        yesPressed = AutomationHelpers.TryClickDialog(automation, "Potvrda", "Da", 10);
                                    });

                                    if (yesPressed)
                                    {
                                        Console.WriteLine("[INFO] Potvrda knjizenje internog prenosa ");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[ERROR] nije se pojavio prozor potvrda knjizienja internog prenosa  ");
                                    }
                                    // 8. Čekamo sekundu-dve da Logik to sažvaće i da popup nestane
                                    for (int i = 0; i < 20; i++)
                                    {
                                        // proveravamo da li je prozor porvrda tu jos uvek 
                                        var potvProzor = AutomationHelpers.FindWindow(automation, "Potvrda");
                                        if (potvProzor == null)
                                        {
                                            break;
                                        }
                                        Thread.Sleep(50);
                                    }

                                    // 9. ZAVRŠNI KLIK - Zatvaramo Interni prenos!
                                    AutomationHelpers.StopWatchSteps("Zatvori interni prenos ", () => {
                                        AutomationHelpers.ZatvoriInterniPrenosUredu(automation);
                                    });


                                    Console.WriteLine("[KRAJ] Proces automatizacije je uspešno završen od početka do kraja!");


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
            }
            ukupnaStoperica.Stop();
            Console.WriteLine($"[INFO] KOMPLETAN PROCES ZAVRŠEN ZA: {ukupnaStoperica.Elapsed.TotalSeconds:F2} sekundi.");
            Console.WriteLine("[KRAJ] Proces automatizacije je zavrsen");
            // Na kraju uspešnog procesa:
            string izabraniNacin = (config.Payment == PaymentOption.Cek) ? "Cek" : "Gotovina";

            AplikacijaStatistika.SacuvajStatistiku(izabraniNacin); // Proslediš string "Cek" ili "Gotovina"

            var (noviCek, novaGotovina) = AplikacijaStatistika.UcitajStatistiku();
            Console.WriteLine($"==========================================");
            Console.WriteLine($" UKUPAN SKOR: Ček: {noviCek} | Gotovina: {novaGotovina}");
            Console.WriteLine($"==========================================");
            Console.ReadKey();

        }
    }
}
