# Project Two: Red vs Blue

[English](README.md) | **Polski**

Zaktualizowana i ulepszona wersja projektu stworzonego pierwotnie przez Team 13 podczas Game Dev School w 2021 roku.
Wszystkie zmiany od pierwszego wydania wprowadził Daniel Pytel.

Turowy pojedynek taktyczny na izometrycznej planszy. Drużyna Czerwona, prowadzona przez Superiora
Czerwonego Szefa, walczy z Drużyną Niebieską, prowadzoną przez Niebieskiego Szefa. Przed bitwą gracze wybierają
Doppelgangerów parami. W trakcie bitwy przyzywają je obok swojego Superiora, poruszają nimi
i atakują. Pola planszy leczą, ranią, dają osłonę, spowalniają albo zwiększają zasięg. Wygrywa
ten, kto pierwszy pokona Superiora przeciwnika, a każda zakończona partia jest punktowana
i zapisywana.

W grę mogą grać dwie osoby przy jednym ekranie albo jedna osoba przeciwko komputerowi na trzech
poziomach trudności.

## Zrzuty ekranu

| Menu | Twórcy |
| --- | --- |
| ![Menu główne](Docs/Screenshots/menu.png) | ![Twórcy](Docs/Screenshots/credits.png) |

![Wybór jednostek](Docs/Screenshots/unit-choice.png)

![Rozgrywka](Docs/Screenshots/gameplay.png)

![Panel zwycięzcy](Docs/Screenshots/winner.png)

## Co zmieniło się od pierwszego wydania

Pierwsze wydanie było prototypem z Game Dev School 2021 na Unity 2019.4, do gry we dwoje na jednym ekranie,
przygotowanym przez cały Team 13. Od tego czasu Daniel Pytel wprowadził:

- **Unity 6.** Projekt działa na Unity 6000.3.23f1. Sterowanie obsługuje wyłącznie pakiet Input
  System, więc działa zarówno mysz, jak i dotyk. Usunięto nieużywaną wtyczkę GitHub for Unity.
- **Poprawki błędów.**
  - Zakończenie tury podczas rozstawiania nie wywołuje już błędów.
  - Jednostki, które giną po utracie bonusowego zdrowia, naprawdę znikają z planszy.
  - Sprowokowane jednostki respektują jednostkę, która je sprowokowała.
  - Muzyka się nie dubluje, a zegar tury zatrzymuje się po końcu gry.
  - Kliknięcia w przyciski interfejsu nie zaznaczają już pól pod spodem.
  - Jednostka zabita w trakcie akcji Superiora nie blokuje już tury.
  - Jednostka zabita atakiem ginie dopiero po animacji ataku napastnika, a jej śmierć zaczyna się
    od razu, bez przeskoku z animacji bezczynności.
  - Jednostka, która już się ruszyła, nie pokazuje ponownie zasięgu ruchu, ale nadal pokazuje,
    gdzie może zaatakować.
- **Czystszy kod.** Plansza liczy zasięg ruchu i ścieżki jednym przeszukiwaniem wszerz. Skrypty
  trzymają się typowych konwencji C#, a animacje są bezpiecznie powiązane ze swoimi obiektami.
  Pliki i foldery w `Assets` mają nazwy w PascalCase, a nieużywane zasoby zostały usunięte.
- **Nowy interfejs.** Interfejs skaluje się z rozdzielczością ekranu i korzysta z TextMeshPro
  z ostrą pikselową czcionką o stałych rozmiarach. HUD ma napis tury w kolorze drużyny
  i zwijany dziennik bitwy po stronie aktywnego gracza, który przewija się kółkiem myszy
  i przeskakuje do nowych ruchów. Menu ma nową instrukcję i wolno unoszącą się grafikę,
  a panel informacji pokazuje maksymalne zdrowie razem z bonusami.
- **Pauza i ekran końca.** Esc otwiera menu pauzy nad przyciemnioną, zatrzymaną planszą z
  przyciskami Play again, Back to Menu, Settings i Quit. Play again, Back to Menu i Quit
  najpierw pytają „Na pewno?”. Po zakończeniu gry ten sam ekran pokazuje zwycięzcę i podsumowanie
  rozegranych tur, wezwanych i zabitych jednostek oraz wyniku obu drużyn. W grze z komputerem
  można zagrać rewanż ze zmianą stron.
- **Podpowiedzi i skróty.** Podpowiedzi o możliwych akcjach jednostki pojawiają się tylko dla
  drużyny, która ma turę, a w pierwszej turze dochodzą dodatkowe wskazówki. Spacja kończy turę,
  a C wzywa jednostkę. W ostatnich 10 sekundach tury zegar robi się czerwony i tyka.
- **Punkty i wyniki.** Każda zakończona partia jest punktowana: punkty za wygraną, zabójstwa,
  szybkie zwycięstwo i zdrowie, które zostało Superiorowi zwycięzcy, minus utracone jednostki,
  a całość zależy od poziomu trudności AI. Ekran końca pokazuje wynik oraz nowy rekord albo
  miejsce w rankingu. Nowy ekran **RECORDS** (po polsku WYNIKI) w menu ma ranking (10 najlepszych,
  z filtrem poziomu trudności), historię ostatnich 10 partii ze szczegółami i dziennikiem bitwy
  oraz statystyki. Ostatnie 200 partii zapisuje się w pliku JSON.
- **Nowy ekran wyboru jednostek.** Draft ma nowy układ z osobnym panelem dla każdej drużyny,
  statystykami jednostek, ikonami cech, siatką zasięgu ruchu i ataku oraz pulsującymi strzałkami.
  Działa też w grze z komputerem. Menu, draft i bitwa mają to samo animowane tło, które można
  przyciemnić opcjonalnymi warstwami dim i shade.
- **Opcje.** Suwaki głośności efektów i muzyki, poziom trudności AI, rozdzielczość ekranu
  (domyślnie natywna) i język (angielski lub polski, z pełnym polskim tłumaczeniem). Wszystko
  zapisuje się automatycznie, a przycisk w menu przywraca ustawienia domyślne. Głośność efektów
  i muzyki, poziom trudności i rozdzielczość można też zmienić w trakcie gry z menu pauzy.
- **Szybszy start.** Animacje jednostek wczytują się tylko dla postaci wybranych w drafcie.
- **Testy automatyczne.** Folder `Tests` zawiera testy EditMode i PlayMode dla zasad gry, AI,
  punktacji i wyników, menu oraz ekranu końca, więc zmianę można sprawdzić bez ręcznego grania
  każdej partii. W [Tests/TestsUpdate.md](Tests/TestsUpdate.md) jest opisane, jak je uruchomić
  i jak je zmieniać, gdy zmienia się gra.
- **Przeciwnik komputerowy.** Nowy tryb PLAY VS AI (w menu PLAY) pozwala poprowadzić Czerwonego Szefa przeciwko
  Niebieskiemu Szefowi sterowanemu przez komputer. Komputer wybiera jednostki, przyzywa posiłki, porusza się,
  atakuje i używa Confuse oraz Teleport. W opcjach można wybrać trzy poziomy trudności:
  - **Easy** gra niezdarnie i częściowo losowo.
  - **Normal** gra solidnie.
  - **Hard** unika zagrożenia, dobija ranne jednostki i chroni swojego Superiora.

  Komputer rozpoznaje też pat i wtedy przechodzi do natarcia.
- **Balans.**
  - Gracz rozpoczynający jest losowany.
  - Confuse działa co drugą turę, a Teleport przenosi sojusznika o maksymalnie 2 pola.
  - Repeater i Tormentor mają 7 punktów zdrowia, a Operative zadaje 2 obrażenia.
  - Superiorzy są bez zmian, więc nadal są najsilniejszymi jednostkami na planszy.

## Cele

- **Krótkie, czytelne pojedynki.** Mecz ma trwać kilka minut, a każda zasada ma być widoczna na
  planszy albo w panelu informacji.
- **Znaczące wybory.** Wybór jednostek, ich cechy, efekty pól i umiejętności mają dawać w każdym
  meczu inny plan zamiast jednej dominującej strategii.
- **Równe strony.** Drużyna Czerwona i Niebieska mają wygrywać równie często. Plansza jest symetryczna,
  pierwszy ruch jest losowy, a jednostki w parach mają podobną siłę.
- **Gra jednoosobowa warta uwagi.** Każdy poziom AI ma pasować do innego gracza, od pierwszej
  partii po prawdziwe wyzwanie.
- **Projekt, nad którym łatwo dalej pracować.** Gra korzysta z aktualnego silnika i czytelnego
  kodu, bez pozostałości po nieużywanych wtyczkach.

## Uruchomienie

1. Otwórz projekt w Unity 6000.3.23f1.
2. Otwórz `Assets/Scenes/MenuScene.unity` i naciśnij Play.
3. Wybierz **PLAY**, a potem **PLAYER VS PLAYER**, aby zagrać we dwoje na jednym ekranie, albo
   **PLAY VS AI**, aby zagrać z komputerem. Poziom trudności AI zmienisz w **Options**,
   a zakończone partie znajdziesz w **Records**.

## Twórcy

Pierwsze wydanie (Team 13, Game Dev School 2021):

- Programowanie: Karol Ławicki
- Grafika: Daniel Pytel
- Projekt gry: Matt Matuszewski, Mateusz Niziołek

Wersja zaktualizowana: Daniel Pytel
