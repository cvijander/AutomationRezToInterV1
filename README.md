# AutomationRezToInterV1

Desktop alat u C# / .NET za automatizaciju svakodnevnog poslovnog procesa u Logik ERP sistemu — otknjižavanje rezervacije, kreiranje i knjiženje internog prenosa i slanje rezervacije na kasu radi fiskalnog računa.

## Problem

Ručno izvršavanje ovog procesa ima 15–20 koraka i traje 40–50 sekundi po transakciji: otknjiži se rezervacija, bira se ulazni i izlazni magacin, unosi se komentar, interni prenos se razdvaja od rezervacije, proknjižava se, a rezervacija se šalje na kasu za fiskalni račun. Rutina se ponavlja desetine puta dnevno, pa svaka sekunda i svaki propušteni klik direktno utiču na tempo rada.

## Rešenje

Prethodna verzija ovog alata je bila klasičan "mouse clicker" — simulacija pokreta i klikova miša na fiksnim koordinatama ekrana. Radila je, ali sporo (~70s po transakciji) i lomila se na svaku promenu rezolucije ili pozicije prozora.

`AutomationRezToInterV1` koristi **FlaUI** (Windows UI Automation) da pronalazi i upravlja UI elementima Logik aplikacije po imenu i tipu elementa, a ne po koordinatama na ekranu. Rezultat: ~40 sekundi po transakciji, oko 40% brže, i znatno stabilnije na promene ekrana.

## Šta radi, korak po korak

1. Pokreće Logik aplikaciju i prijavljuje se
2. Pronalazi rezervaciju po unetom broju
3. Otknjižava rezervaciju
4. Kreira interni prenos iz rezervacije
5. Bira ulazni i izlazni magacin
6. Unosi komentar o rezervaciji
7. Razdvaja interni prenos od rezervacije
8. Proknjižava interni prenos
9. Šalje originalnu rezervaciju na kasu radi izdavanja fiskalnog računa
10. Beleži statistiku obrađenih transakcija (ček / gotovina)

Svaki korak se posebno meri (stopwatch) i ispisuje u konzolu — korisno za debagovanje i praćenje gde tačno vreme odlazi.

## Tehnologije

- C# / .NET 10
- FlaUI.Core + FlaUI.UIA2 — UI Automation, ne simulacija miša
- System.Text.Json — čuvanje konfiguracije i statistike

## Tehnički detalji i ograničenja

Cilj je bio potpuno izbeći mouse-clicker pristup, ali Logik je pisan u Delphiju, a FlaUI ne vidi sva polja i dugmad u toj aplikaciji. Zbog toga je na par mesta u procesu (npr. prvi klik za otvaranje dokumenta) i dalje zadržan klik po koordinatama kao fallback, dok se za sve gde FlaUI može da pronađe element po imenu/tipu koristi to — na nekim mestima navigacija tastaturom (npr. 4x Tab do dugmeta), na drugima direktno pronalaženje ciljanog UI elementa.

Druga bitna razlika u odnosu na stari mouse-clicker: umesto fiksnih pauza, alat aktivno čeka da se prozor pojavi na desktopu (pretragom aktivnih prozora), pa nastavlja čim ga pronađe — brže kad je sistem responzivan, a otpornije kad Logik zakasni zbog svojih internih procesa.

**Uspešnost:** oko 90–95%. Poznato mesto greške je sekvenca od 4 pritiska na Tab do dugmeta "razdvoj rezervaciju" — u oko 5% slučajeva fokus stigne posle 3 Taba umesto 4, pa dugme ne bude fokusirano kad se očekuje klik.

## Pokretanje

Zahteva .NET 10 i Windows (aplikacija komunicira sa desktop UI-jem, pa ne radi na drugim OS-ovima).

Pri prvom pokretanju program sam kreira `appsettings.json` sa podrazumevanim vrednostima (putanja do Logik `.exe` fajla, korisničko ime i lozinka) — te vrednosti treba izmeniti da odgovaraju konkretnoj instalaciji. Nakon toga, program traži prijavu (brzi F1 nalog ili ručni unos), pa broj rezervacije i način plaćanja, i sam izvršava ostatak procesa.

## Rezultat

- Vreme izvršavanja: ~70s (stari mouse-clicker pristup) → ~40s
- Svakodnevno u upotrebi za obradu maloprodajnih rezervacija

## Napomena

Alat je pisan za internu upotrebu i zavisi od izgleda konkretne Logik aplikacije koja se koristi u firmi, tako da nije direktno prenosiv na druge sisteme bez prilagođavanja selektora UI elemenata.
