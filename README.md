# AutomationRezToInterV1

Desktop alat u C# koji automatizuje svakodnevni proces u Logik ERP sistemu: maloprodajnu rezervaciju pretvara u interni prenos iz magacina u maloprodaju (MP), proknjižava ga i šalje rezervaciju na kasu radi izdavanja fiskalnog računa.

Alat se svakodnevno koristi u radu, na stvarnim porudžbinama.

<!-- Ovde ide GIF ili screenshot konzole iz test režima -->

## Problem

Ručno, ovaj proces ima 15 do 20 koraka po rezervaciji: otknjiži se rezervacija, kopira u interni prenos, biraju se ulazni i izlazni magacin, upisuje komentar, interni prenos se razdvaja od rezervacije, proknjižava, a rezervacija se šalje na kasu. Rutina se ponavlja desetine puta dnevno, i svaki propušteni klik znači grešku koju posle treba ručno ispravljati.

## Kako je rešenje evoluiralo

**Verzija 0, "mouse clicker".** Klikovi na fiksnim koordinatama ekrana. Radila je, ali sporo (oko 70 s) i lomila se na svaku promenu rezolucije ili položaja prozora.

**Verzija 1, FlaUI.** Elementi se traže po imenu i tipu (Windows UI Automation), umesto po koordinatama. Oko 40 s po transakciji i uspešnost 90 do 95%. Ostali su delovi rađeni "naslepo" (npr. 4 puta Tab do dugmeta), i baš su oni izazivali greške.

**Verzija 2 (trenutna).** Cilj više nije bio samo brzina, nego da program **nikad ne uradi pogrešnu stvar**. Svaki rizičan korak ima proveru pre i posle sebe, a sve što je ranije rađeno naslepo sada se radi po nazivu dugmeta ili polja.

## Šta radi, korak po korak

1. Pokreće Logik i radi isključivo sa prozorima **tog** procesa
2. Otvara modul Dokumenti (prečica Ctrl+F5) i prijavljuje se
3. Postavlja tip dokumenta na "Rezervacija", čita polje i klikće dok ne piše tačan tip
4. Unosi broj rezervacije i otvara je
5. **Provera:** čita broj, partnera i komentar otvorene rezervacije i prikazuje ih. Nastavlja tek kad korisnik potvrdi tasterom Pause
6. Otknjižava rezervaciju i kopira je u interni prenos
7. Potvrđuje ulazni magacin (1) i bira izlazni magacin (MP)
8. **Provera:** čita oba magacina iz internog prenosa (`/1` i `MP`)
9. Upisuje komentar (način plaćanja i broj rezervacije), pa ga **čita nazad** radi provere
10. Razdvaja interni prenos od rezervacije, uz **proveru** da je povezana baš ta rezervacija
11. Ponovo otvara interni prenos, **proverava komentar** i proknjižava ga
12. Ponovo otvara rezervaciju, šalje je u Logik Kasu i zatvara je

Ako bilo koja provera ne prođe, program staje sa porukom `[STOP]` koja kaže **u kom je stanju Logik i odakle se nastavlja ručno**, i ispisuje naslov i tekst svakog otvorenog prozora Logika.

## Tehnički izazovi i kako su rešeni

**FlaUI ne vidi sve kontrole u Delphi aplikaciji.** Logik je pisan u Delphiju, i mnoga polja i dugmad nisu vidljiva kroz UI Automation. Rešenje je čitanje direktno preko Win32 API-ja: `EnumChildWindows` prođe kroz sve kontrole prozora, `WM_GETTEXT` čita njihov tekst, a klik se šalje porukom `BM_CLICK`, nezavisno od pozicije miša. Za istraživanje nepoznatih prozora napravljeni su alati koji snime sve kontrole prozora (klasu, tekst i poziciju) u fajl.

**Skriveni moduli na istom mestu.** Logik drži svih 9 modula učitanih i naslaganih jedan preko drugog, pa npr. dugme "Pronađi" postoji 8 puta. Program zato traži kontrole samo unutar modula Dokumenti, a pre klika proverava (`WindowFromPoint`) da je na tom mestu ekrana zaista prava kontrola.

**Taster koji je otvarao SEF.** Za potvrdu je prvo korišćen F9, a pokazalo se da je F9 u Logiku prečica za modul SEF, pa je pritisak "procurio" u aplikaciju. Kontrola je prebačena na tastere koje Logik ne koristi: **Pause** (nastavi, pauza) i **Scroll Lock** (prekid).

**Tip dokumenta.** Polje Tip se menja dvoklikom kroz niz Račun, Kalkulacija, Predračun, Rezervacija, a početno stanje nije uvek isto. Stari kod je radio fiksna 4 dvoklika i ponekad stao na Predračunu. Novi čita polje posle svakog klika.

**Pogrešan prozor.** Program je u jednom trenutku "našao" prozor Rezervacija u naslovu sopstvene konzole (zbog imena foldera), a ranije i stari, već otvoren Logik. Oba slučaja je rešilo traženje prozora isključivo unutar procesa koji je program sam pokrenuo.

**Brzina miša.** FlaUI podrazumevano animira pomeranje miša, što je dodavalo i preko sekunde po koraku. Animacija je ubrzana, a gde je moguće, klik ide porukom, bez miša.

## Bezbednost u radu

- **Provera pre otknjižavanja:** korisnik vidi broj, partnera i komentar, i potvrđuje tasterom Pause (Scroll Lock prekida)
- **Test režim (T):** ceo proces se izvršava, ali bez slanja na kasu i bez upisa u statistiku
- **Pauza i prekid u svakom trenutku:** Pause / Scroll Lock
- **Log svakog pokretanja:** fajl u folderu `logovi`, sa vremenom za svaku liniju, uključujući i neočekivane greške
- **Statistika:** broj obrađenih rezervacija po načinu plaćanja (ček/gotovina), samo za uspešno završene

## Rezultati

U prva tri dana rada nove verzije, na stvarnim porudžbinama:

- **35 rezervacija obrađeno od početka do kraja**, bez ručne intervencije
- **3 bezbedna zaustavljanja**: jedna veleprodajna rezervacija koju Logik ne dozvoljava da se otknjiži, i dva slučaja koja su ispravljena istog dana
- **0 pogrešno proknjiženih dokumenata**

Automatski deo procesa, uključujući slanje na kasu, traje oko 40 do 45 sekundi. Ukupno vreme sa unosom broja i potvrdom je oko 50 sekundi.

## Tehnologije

- C# / .NET Framework 4.8
- FlaUI.Core + FlaUI.UIA2 (Windows UI Automation)
- Win32 API preko P/Invoke (`EnumChildWindows`, `SendMessage`/`WM_GETTEXT`, `PostMessage`/`BM_CLICK`, `WindowFromPoint`, `GetAsyncKeyState`)
- System.Text.Json (konfiguracija)

## Struktura projekta

| Fajl | Uloga |
|---|---|
| `Program.cs` | Tok procesa, korak po korak, sa proverama i `[STOP]` porukama |
| `AutomationHelpers.cs` | Koraci u Logiku, Win32 čitanje polja i klikovi, debug alati za snimanje prozora |
| `RunControl.cs` | Pauza, nastavak i prekid preko tastera Pause i Scroll Lock |
| `UserInputConfig.cs` | Unos broja rezervacije, načina plaćanja i izbor test režima |
| `DvostrukiIzlaz.cs` | Ispis istovremeno u konzolu i u log fajl |

## Pokretanje

Potrebni su Windows, .NET Framework 4.8 i instaliran Logik.

1. Build u **Release** režimu, pa ceo folder `bin\Release\net48\` prebaciti gde se program koristi.
2. Pri prvom pokretanju program pravi `appsettings.json` sa primer vrednostima. Upisati putanju do Logik `.exe` fajla, korisničko ime i lozinku.
3. Pokrenuti, uneti broj rezervacije i način plaćanja, pa izabrati **D** (pravi rad) ili **T** (test, bez kase).

`appsettings.json`, logovi i statistika nisu deo repozitorijuma.

## Napomena

Alat je pisan za internu upotrebu i zavisi od izgleda konkretne Logik instalacije. Na drugom sistemu bi trebalo prilagoditi nazive prozora, dugmadi i polja.
