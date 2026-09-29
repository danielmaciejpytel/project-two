# Project Two: Super Hot vs Super Cold

[English](README.md) | **Polski**

Zaktualizowana i ulepszona wersja projektu stworzonego pierwotnie przez Team 13 podczas Game Dev School w 2021 roku.
Wszystkie zmiany od pierwszego wydania wprowadził Daniel Pytel.

Turowy pojedynek taktyczny na izometrycznej planszy. Dwaj Superiorzy, Super Hot i Super Cold,
dowodzą drużynami Doppelgangerów. Przed bitwą gracze wybierają jednostki parami. W trakcie bitwy
przyzywają je obok swojego Superiora, poruszają nimi i atakują. Pola planszy leczą, ranią, dają
osłonę, spowalniają albo zwiększają zasięg. Wygrywa ten, kto pierwszy pokona Superiora
przeciwnika.

W grę mogą grać dwie osoby przy jednym ekranie albo jedna osoba przeciwko komputerowi.

## Zrzuty ekranu

| Menu | Twórcy |
| --- | --- |
| ![Menu główne](Docs/Screenshots/menu.png) | ![Twórcy](Docs/Screenshots/credits.png) |

![Rozgrywka](Docs/Screenshots/gameplay.png)

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
- **Czystszy kod.** Plansza liczy zasięg ruchu i ścieżki jednym przeszukiwaniem wszerz. Skrypty
  trzymają się typowych konwencji C#, a animacje są bezpiecznie powiązane ze swoimi obiektami.
- **Nowy interfejs.** Interfejs skaluje się z rozdzielczością ekranu i korzysta z TextMeshPro
  z ostrą pikselową czcionką o stałych rozmiarach. HUD ma napis tury w kolorze drużyny
  i zwijany dziennik bitwy po stronie aktywnego gracza. Menu ma nową instrukcję, a panel
  informacji pokazuje maksymalne zdrowie razem z bonusami.
- **Przeciwnik komputerowy.** Nowy tryb PLAY VS AI (w menu PLAY) pozwala poprowadzić Super Hot przeciwko Super
  Cold sterowanemu przez komputer. Komputer wybiera jednostki, przyzywa posiłki, porusza się,
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
- **Równe strony.** Super Hot i Super Cold mają wygrywać równie często. Plansza jest symetryczna,
  pierwszy ruch jest losowy, a jednostki w parach mają podobną siłę.
- **Gra jednoosobowa warta uwagi.** Każdy poziom AI ma pasować do innego gracza, od pierwszej
  partii po prawdziwe wyzwanie.
- **Projekt, nad którym łatwo dalej pracować.** Gra korzysta z aktualnego silnika i czytelnego
  kodu, bez pozostałości po nieużywanych wtyczkach.

## Uruchomienie

1. Otwórz projekt w Unity 6000.3.23f1.
2. Otwórz `Assets/Scenes/MenuScene.unity` i naciśnij Play.
3. Wybierz **PLAY**, a potem **PLAYER VS PLAYER**, aby zagrać we dwoje na jednym ekranie, albo
   **PLAY VS AI**, aby zagrać z komputerem. Poziom trudności AI zmienisz w **Options**.

## Twórcy

Pierwsze wydanie (Team 13, Game Dev School 2021):

- Programowanie: Karol Ławicki
- Grafika: Daniel Pytel
- Projekt gry: Matt Matuszewski, Mateusz Niziołek

Wersja zaktualizowana: Daniel Pytel
