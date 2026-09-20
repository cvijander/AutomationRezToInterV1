using System;
using System.Collections.Generic;
using System.Text;


namespace AutomationRezToInterV1
{
    public enum PaymentOption
    {
        Cek = 1,
        Gotovina = 2
    }

    public class UserInputConfig
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public PaymentOption Payment { get; set; }

        public string RezervationNumber { get; set; }


        public static UserInputConfig Initialize()
        {
            var config = new UserInputConfig();
            LoginSetup(config);

            CollectReservationData(config);

            return config;
        }

        public static void InitialDataDisplay(UserInputConfig config)
        {
            Console.WriteLine("----------------------------");
            Console.WriteLine($"Korisnik {config.Username}");
            Console.WriteLine($"Password {config.Password}");
            Console.WriteLine("----------------------------");
            Console.WriteLine();
            Console.WriteLine("Pritisni ENTER za dalje ...");
            Console.ReadLine();
        }

        public static void LoginSetup(UserInputConfig config)
        {
            bool isValid = false;

            Console.WriteLine("=== BRZI START ===");
            Console.WriteLine();
            Console.WriteLine("Pritisni [F1] za nalog Maloprodaje (0202)");
            Console.WriteLine("Pritisni [F2] za drugi nalog (rucni unos podataka)");
            Console.WriteLine();
            Console.WriteLine("Pritisni [ESC] za izlaz iz programa");
            Console.WriteLine();
            Console.WriteLine("==================");

            while (!isValid)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.F1)
                {
                    config.Username = "maloprodaja";
                    config.Password = "0202";
                    isValid = true;
                }
                else if (key == ConsoleKey.F2)
                {
                    string userNameByUser = "";
                    while (string.IsNullOrWhiteSpace(userNameByUser))
                    {
                        Console.Write("Unesi username: ");
                        userNameByUser = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(userNameByUser))
                        {
                            Console.WriteLine("Username ne sme biti prazan");
                        }
                        config.Username = userNameByUser;
                    }

                    string passwordByUser = "";
                    while (string.IsNullOrWhiteSpace(passwordByUser))
                    {
                        Console.Write("Unesi password: ");
                        passwordByUser = Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(passwordByUser))
                        {
                            Console.WriteLine("Password ne sme biti prazan");
                        }
                        config.Password = passwordByUser;
                    }

                    isValid = true;
                }
                else if (key == ConsoleKey.Escape)
                {
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Pogresan taster pokusaj ponovo");
                }
            }
        }

        public static void CollectReservationData(UserInputConfig config)
        {
            bool confirmed = false;

            while (!confirmed)
            {
                string inputNumber = "";

                while (string.IsNullOrWhiteSpace(inputNumber))
                {
                    Console.WriteLine();

                    Console.Write("Unesi broj rezervacije (npr 28993) / (29573) : ");
                    inputNumber = Console.ReadLine().Trim();

                    if (string.IsNullOrWhiteSpace(inputNumber))
                    {

                        Console.WriteLine("[ERROR] -> Broj ne sme biti prazan");
                        Console.WriteLine("Unesite ponovo broj rezervacije ");
                    }

                    config.RezervationNumber = inputNumber;
                }

                bool validPayment = false;

                while (!validPayment)
                {
                    Console.WriteLine();
                    Console.WriteLine("-----------------");
                    Console.WriteLine("Unesite nacin placanja");
                    Console.WriteLine("1 - Cek ");
                    Console.WriteLine("2 - Gotovina");
                    Console.WriteLine("--------------------");
                    Console.WriteLine();
                    Console.Write("Unesi: ");
                    string input = Console.ReadLine().Trim();

                    if (input == "1")
                    {
                        config.Payment = PaymentOption.Cek;
                        validPayment = true;
                    }
                    else if (input == "2")
                    {
                        config.Payment = PaymentOption.Gotovina;
                        validPayment = true;
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] Pogresan unos samo 1 ili 2");
                        continue;
                    }

                }

                Console.WriteLine("-----------------");
                Console.WriteLine($"Zabelezeno Broj rezervacije: {config.RezervationNumber} i kucamo nacin placanja : {config.Payment}");
                Console.WriteLine();
                Console.WriteLine($"Da li su podaci tacni (Da) - nastavi  |  (Ne) -  ponovi  |  (ESC) - prekid programa ");
                Console.Write("Odgovor : ");
                Console.WriteLine();
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.D)
                {
                    confirmed = true;
                }
                else if (key == ConsoleKey.Escape)
                {
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("=============================");
                    Console.WriteLine();
                    Console.WriteLine("Vracam se na ponovni unos");
                    Console.WriteLine();
                }

            }
        }

        public static UserInputConfig LoadConfiguration()
        {
            var config = UserInputConfig.Initialize();
            UserInputConfig.InitialDataDisplay(config);
            return config;
        }

        public class AppConfig
        {
            public string LogikPutanja { get; set; }
            public string KorisnickoIme { get; set; }
            public string Lozinka { get; set; }
        }
    }
}
