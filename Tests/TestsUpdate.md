# TestsUpdate

Wytyczne, jak zmieniać testy automatyczne, gdy zmienia się projekt. Części oznaczone `AUTO` **aktualizują się same przy każdym commicie** (hook `Tests/hooks/pre-commit` uruchamia `Tests/tools/update-tests-doc.py` i dodaje zmieniony plik do tego samego commita). Reszta jest napisana ręcznie i można ją edytować.

Wszystko, co dotyczy testów, leży w folderze `Tests/` (osobno od `Assets`, zwykle śledzony przez git jak reszta projektu):

| Folder | Zawartość |
|---|---|
| `Tests/unity/` | testy dla Unity jako lokalny pakiet: `EditMode/`, `PlayMode/`, `package.json` |
| `Tests/tools/` | `update-tests-doc.py` (odświeża ten plik) i `tests-map.json` (mapa plików na testy) |
| `Tests/hooks/` | hook `pre-commit` |
| `Tests/TestsUpdate.md` | ten plik |

Poza `Tests/` muszą leżeć tylko dwa pliki `.asmdef`, bo dotyczą folderu, w którym są (a nie testów): `Assets/Scripts/Game.asmdef` (zestaw z kodem gry, do którego odwołują się testy) i `Assets/Plugins/Demigiant/DOTween/Modules/DOTween.Modules.asmdef` (moduły DOTween). Nie przenoś ich.

Jednorazowo po sklonowaniu repozytorium:

1. Włącz hook:

```bash
git config core.hooksPath Tests/hooks
```

2. Dodaj pakiet testów do `Packages/manifest.json` (ten plik jest w tym repozytorium ukryty przed gitem przez `skip-worktree`, więc nie da się tego zacommitować i każdy robi to u siebie). W `dependencies` dopisz `"com.team13.project-two-tests": "file:../Tests/unity"`, a obok `dependencies` dodaj:

```json
"testables": [ "com.team13.project-two-tests" ]
```

Unity zna testy tylko z `Assets` albo z pakietu, dlatego folder `Tests/unity` jest pakietem. Bez tego wpisu Test Runner nie pokaże testów, ale gra działa normalnie.

## Jak uruchomić testy

- **W edytorze:** Window → General → Test Runner, zakładki EditMode i PlayMode.
- **Z linii poleceń (edytor otwarty):** `unity command run_tests --mode EditMode`; PlayMode musi być asynchroniczny: `unity command run_tests --mode PlayMode --async_tests true`, a wynik odczytasz komendą `unity command test_status`. Filtr po nazwie: `--filter GameplayTests --filter_type testName`.
- **Przed PlayMode (ważne):**
  1. *Play Mode Start Scene* (Edit → Project Settings → Editor → Enter Play Mode Settings, albo wybór sceny startowej przy przycisku Play) musi być **pusty**. Gdy jest ustawiony (np. na MenuScene), Test Runner wchodzi w Play w tej scenie i uruchomienie testów wisi bez końca. Po testach możesz ustawić go z powrotem.
  2. Otwarta scena nie może być zmodyfikowana. Inaczej edytor pokaże okno „Scene(s) Have Been Modified” i czeka na kliknięcie. Zapisz scenę albo otwórz zapisaną (np. MenuScene).
- **Czas:** EditMode ok. 1 s, PlayMode ok. 1–2 min (partie z AI przyspieszone do 20x).

## Co testujemy

| Zestaw | Co pilnuje |
|---|---|
| `LocalizationTests` (EditMode) | Polish.txt bez duplikatów i pustych wpisów, zgodne `{0}`, każdy tekst ze scen i z kodu ma tłumaczenie |
| `BoardGridTests` (EditMode) | plansza z `Grid.csv`: wymiary, kafelki, rogi dowódców, ścieżki omijające przeszkody |
| `GameplayTests` (PlayMode) | przebieg tury: start, Call (użycie, anulowanie, przełączanie), koniec tury i strona HUD, timer, panel info, podpowiedzi zasięgu tylko dla drużyny z turą, karty zabitych (wyszarzone, przekreślone, podpis MARTWY), panel postaci bez efektów jej pola |
| `EndScreenTests` (PlayMode) | koniec gry: ekran z przyciskami, szerokości banera i przycisków, Settings, Play again, Back to Menu, pauza ESC i `Time.timeScale` |
| `PauseAndSummaryTests` (PlayMode) | pytanie „Na pewno?” w menu pauzy (Tak, Nie, ESC), brak pytania po końcu gry, podsumowanie partii (tury, wezwane, zabite), rewanż ze zmianą stron tylko w grze z komputerem |
| `HintsAndWarningTests` (PlayMode) | wskazówki w pierwszej turze każdego gracza (i ich brak później oraz w turze komputera), kolor ostrzeżenia w ostatnich 10 s tury |
| `GameStatsTests` (EditMode) | liczniki podsumowania i `GameSession.SwapSides` |
| `ScoreCalculatorTests`, `GameResultTests` (EditMode) | wzór punktacji (wygrana, zabici, straty, bonusy za szybkość i zdrowie, mnożnik trudności) i budowa rekordu partii |
| `RecordStoreTests` (EditMode) | plik wyników: zapis i odczyt, limit 200 partii, uszkodzony plik, ranking (top 10, filtr trudności), statystyki |
| `SoundAssetTests` (EditMode) | dźwięk tik istnieje i jest przypisany w SoundController sceny menu |
| `ResultsTests` (PlayMode) | koniec gry zapisuje wynik (wygrana, przegrana, dwóch graczy), miejsce w rankingu na ekranie końca, skład i Battle log, porzucona partia nie jest zapisywana |
| `ShortcutAndSettingsTests` (PlayMode) | spacja i C (i kiedy nie działają), poziom trudności w ustawieniach pauzy i ekranu końca |
| `AttackAnimationTests` (PlayMode) | jednostka zabita atakiem dostaje polecenie śmierci (i gra się kończy) dopiero po zakończeniu animacji ataku, a obrażenia niebędące atakiem (kafelek, podpalenie) zabijają od razu; test czyta stan animatora (`attack`, `death`, trigger `Die`), więc zmiana nazw stanów w kontrolerach animacji wymaga poprawki |
| `OptionsPanelTests` (PlayMode) | panel Options w menu ma układ jak Wyniki: rozmiar tekstu wierszy = zakładki, wspólna lewa krawędź tytułu, wierszy i BACK, BACK na wysokości jak w Wynikach, każdy wiersz (Effects, Music, Difficulty, Resolution, Language) robi się różowy i wysuwa pasek, przycisk „Reset settings” stoi jak „Clear results” i przywraca połowę głośności, Normal i natywną rozdzielczość bez ruszania języka |
| `OptionsPanelTests` (PlayMode) | panel Options w menu ma układ jak Wyniki: rozmiar tekstu wierszy = zakładki, wspólna lewa krawędź tytułu, wierszy i BACK, BACK na wysokości jak w Wynikach, każdy wiersz (Effects, Music, Difficulty, Resolution, Language) robi się różowy i wysuwa pasek, przycisk „Reset settings” stoi jak „Clear results” i przywraca połowę głośności, Normal i natywną rozdzielczość bez ruszania języka |
| `RecordsMenuTests` (PlayMode) | panel Wyniki w menu: ranking i filtr, historia (10 ostatnich) ze szczegółami, statystyki, czyszczenie w dwóch kliknięciach |
| `BattleLogTests` (PlayMode) | historia, przewijanie, powrót na dół przy nowym wpisie, szerokości HUD-u względem timera |
| `AiSoakTests` (PlayMode) | komputer kończy partię przeciw biernemu graczowi na Normal i Hard, bez błędów w konsoli |

## Co robić po zmianie w projekcie

Najpierw uruchom testy z tabeli „Które testy sprawdzić” poniżej. Czerwony test znaczy jedno z dwóch: znalazł błąd (napraw kod) albo jego założenie jest nieaktualne (zmień test). Nigdy nie luzuj testu, żeby przeszedł, bez zrozumienia, dlaczego się przewrócił.

| Zmiana | Co zrobić w testach |
|---|---|
| **Nowy tekst w scenie lub w kodzie** (`LocalizedText`, `Loc.T`, `Loc.F`) | Dodaj wpis do `Assets/Resources/Localization/Polish.txt` (angielski tekst, tabulator, tłumaczenie). `LocalizationTests` i tak wskażą brak. Tekst budowany przez `+` nie jest kluczem i nie jest sprawdzany. |
| **Zmiana układu planszy** (`Grid.csv`, nowe kafelki) | W `BoardGridTests` popraw oczekiwania: szerokość 10, przeszkody w rzędzie 0 na x = 4 i 5, długość ścieżki `9 + 2 + 1`, niedostępny kafelek (4, 0). Nowa litera kafelka wymaga prefabu w `Assets/Prefabs` z danymi kafelka. |
| **Nowy stan gry albo zmiana przejść między stanami** (`*State.cs`) | Dodaj test w `GameplayTests` w stylu: doprowadź do stanu przez `Game.DeployAction()`, `EventManager.Instance.UnitClicked/TileClicked`, sprawdź `Game.CurrentState`. Popraw testy, których stan początkowy się zmienił. |
| **Zmiana zasad Call, timera, tury** | `GameplayTests`: `CallPutsAUnit…`, `CancellingCall…`, `TheTurnEndsWhenTheTimerRunsOut`. Limit czasu ustawia `_timeLimit` w `GameController`; test zakłada, że po 60 wywołaniach tura się kończy. |
| **Zmiana nazwy lub przeniesienie obiektu HUD** | Testy szukają obiektów po nazwie (lista niżej w „Nazwy”). Zmień nazwę w testach (`Tests/unity/PlayMode`) razem ze sceną. |
| **Zmiana układu HUD-u** (szerokości, pozycje) | `BattleLogTests.CallAndEndTurn…`, `TheLogSitsUnderTheButtons…` i `EndScreenTests.TheBannerIsAsWide…` sprawdzają konkretne relacje szerokości (log = kolumna przycisków = Call + End Turn + odstęp; baner = panel podsumowania; przyciski = baner). Wymiary i pozycje elementów HUD-u są w `HudLayout` i w grafikach `Assets/Sprites/GameplayHud` (źródła SVG w `Source~`); scenę HUD-u odtwarza `Assets/Editor/GameplayHudBuilder.cs` (menu Tools → Gameplay HUD → Build). Zmień relację w teście, jeśli zmiana jest zamierzona. |
| **Zmiana wskazówek lub ostrzeżenia timera** (`UIController`) | `HintsAndWarningTests`: teksty wskazówek są sprawdzane przez fragmenty (`Click one of your units`, `Pick a card`, `Click a highlighted tile`), liczba tur ze wskazówkami (2) i pole `_warningColor`. Nowy tekst wskazówki dodaj do `Polish.txt`; `LocalizationTests` sprawdzają też stałe `const string Hint...`. |
| **Zmiana podsumowania, pytania „Na pewno?” albo rewanżu** (`EndGameController`, `GameStats`) | `PauseAndSummaryTests` i `GameStatsTests`; pytanie dotyczy tylko menu pauzy, więc test `TheEndScreenDoesNotAsk` musi dalej przechodzić. Nowe pytanie przekazywane do `Ask("…", …)` też musi mieć tłumaczenie. |
| **Zmiana wzoru punktacji** (`ScoreCalculator`) | Popraw liczby w `ScoreCalculatorTests` (są policzone ze stałych: 1000, 100, 50, 30 tur, 20, 30 za punkt zdrowia) oraz w `ResultsTests`, które sprawdzają zgodność zapisanego wyniku ze wzorem. Wyniki zapisane wcześniej zostają takie, jakie były. |
| **Zmiana zapisywanych danych** (`GameRecord`, `RecordStore`) | Dodawaj pola tak, aby stare pliki dalej się wczytywały (`JsonUtility` ustawia nowe pola na wartości domyślne). Test `ADamagedFileIsSetAside…` i `ASavedGameIsReadBack…` pilnują formatu. Nowy rodzaj statystyki: dopisz do `RecordStats` i do `RecordStoreTests.TheStatistics…`. |
| **Zmiana układu Options albo Wyników** (menu) | `OptionsPanelTests` pilnują spójności obu paneli: ten sam rozmiar tekstu, lewa krawędź, wysokość tytułu i BACK. Zmieniasz jeden panel: zmień też drugi albo test. Nowy wiersz w Options: dodaj `HoverTint` (różowy kolor i pasek) i dopisz go do listy `Rows` w teście. |
| **Zmiana układu Options albo Wyników** (menu) | `OptionsPanelTests` pilnują spójności obu paneli: ten sam rozmiar tekstu, lewa krawędź, wysokość tytułu i BACK. Zmieniasz jeden panel: zmień też drugi albo test. Nowy wiersz w Options: dodaj `HoverTint` (różowy kolor i pasek) i dopisz go do listy `Rows` w teście. |
| **Zmiana panelu Wyniki** (`RecordsController`, scena menu) | `RecordsMenuTests` szukają obiektów po nazwie (`recordsButton`, `RecordsPanel`, `RecordsBackButton`, `FilterButton`, `ClearButton`, `Row1`, `DetailBackButton`). Zmiana nazwy w scenie wymaga zmiany w teście. |
| **Nowy test PlayMode, który kończy grę** | Na początku `[SetUp]` wywołaj `Reset()` (ustawia osobny plik wyników). Bez tego test zapisałby partie w prawdziwych wynikach gracza. |
| **Nowy przycisk HUD-u** | Dopisz go do listy w `GameplayTests.TheHudButtonsAreWired`, żeby test pilnował, że ma podpięte akcje. |
| **Zmiana ekranu końca / menu pauzy** | `EndScreenTests`: nazwy przycisków (`Buttons/PlayAgainButton` itd.), oczekiwany `Time.timeScale`, sceny docelowe (`MainScene`, `MenuScene`). |
| **Zmiana Battle logu** | `BattleLogTests`; metoda `ScrollBy` i pole `_entriesRect` są czytane refleksją, więc ich zmiana nazwy wymaga poprawki w teście. |
| **Nowa jednostka, zdolność, efekt** | Sprawdź `AiSoakTests` (partie muszą się kończyć). Dla nowych reguł dodaj test w `GameplayTests` z prawdziwą jednostką (`unit.DamageUnit(999, "test")` zabija jednostkę). |
| **Zmiana AI** | Uruchom `AiSoakTests` na Normal i Hard; jeśli partia nie kończy się w limicie, popraw AI, nie limit. |
| **Nowy plik `.cs` w `Assets/Scripts`** | Jeśli powstaje nowy podkatalog albo asmdef, sprawdź `Game.asmdef`. Jeśli plik nie pasuje do żadnej reguły w `Tests/tools/tests-map.json`, hook doda w logu adnotację „bez testów”, wtedy dodaj regułę albo test. |
| **Nowy pakiet, zmiana `.asmdef`** | Upewnij się, że oba zestawy testów się kompilują (Test Runner nie pokazuje testów, jeśli asmdef się nie kompiluje). |
| **Nowa scena** | Testy ładują sceny po ścieżce `Assets/Scenes/<Nazwa>.unity` (`GameTestUtil.LoadScene`). |

Dobre praktyki: testy PlayMode kończą się przywróceniem `Time.timeScale = 1` i `GameSession` (`GameTestUtil.Reset`), nowe testy robią tak samo. Test nie powinien zależeć od losowania (kto zaczyna: `Game.ActivePlayer`). Kolejny test dopisz w pliku o tej samej tematyce; nazwa mówi, co jest oczekiwane.

## Nazwy i składowe prywatne, od których zależą testy

<!-- AUTO:NAMES:START -->
Sprawdzane automatycznie przy każdym commicie: nazwa, w których testach jest użyta, i czy wciąż istnieje w projekcie.

**Składowe prywatne i metody (odczytywane przez refleksję):**

- `ConfirmPick` (GameTestUtil.cs)
- `ScrollBy` (BattleLogTests.cs)
- `Step` (GameTestUtil.cs)
- `_barImage` (OptionsPanelTests.cs)
- `_body` (BattleLogTests.cs)
- `_cardImage` (GameplayTests.cs)
- `_difficultyLabel` (ShortcutAndSettingsTests.cs)
- `_entriesRect` (BattleLogTests.cs)
- `_entriesText` (BattleLogTests.cs)
- `_hintPanel` (HintsAndWarningTests.cs)
- `_hintText` (HintsAndWarningTests.cs)
- `_killedImage` (GameplayTests.cs)
- `_myAnimator` (AttackAnimationTests.cs)
- `_myInfoPanel` (GameplayTests.cs)
- `_myTimer` (EndScreenTests.cs, GameplayTests.cs, HintsAndWarningTests.cs)
- `_myUnit` (GameplayTests.cs)
- `_name` (GameplayTests.cs)
- `_overlayColorSpriteRenderer` (GameTestUtil.cs)
- `_recordText` (ResultsTests.cs)
- `_summaryText` (PauseAndSummaryTests.cs, ResultsTests.cs)
- `_tile` (BoardGridTests.cs)
- `_timerText` (HintsAndWarningTests.cs)
- `_unitText` (GameplayTests.cs)
- `_warningColor` (HintsAndWarningTests.cs)

**Obiekty sceny (szukane po nazwie):**

- `AbilityButton` (GameplayTests.cs)
- `BackButton` (EndScreenTests.cs)
- `BackToMenuButton` (EndScreenTests.cs, ResultsTests.cs)
- `BattleLog` (BattleLogTests.cs)
- `BattleLogCanvas` (ScreenFitPlayTests.cs)
- `Buttons` (AttackAnimationTests.cs, EndScreenTests.cs, ResultsTests.cs, ShortcutAndSettingsTests.cs)
- `ClearButton` (OptionsPanelTests.cs, RecordsMenuTests.cs)
- `ConfirmPanel` (ResultsTests.cs)
- `DeployMinionButton` (BattleLogTests.cs, EndScreenTests.cs, GameplayTests.cs, ScreenFitPlayTests.cs, ShortcutAndSettingsTests.cs)
- `DetailBackButton` (RecordsMenuTests.cs)
- `DifficultyButton` (OptionsPanelTests.cs, ShortcutAndSettingsTests.cs)
- `EffectsLabel` (OptionsPanelTests.cs)
- `EffectsSlider` (OptionsPanelTests.cs)
- `EndGameOverlay` (GameTestUtil.cs)
- `EndTurnButton` (BattleLogTests.cs, EndScreenTests.cs, GameplayTests.cs, ScreenFitPlayTests.cs)
- `FilterButton` (RecordsMenuTests.cs)
- `LanguageButton` (OptionsPanelTests.cs)
- `MainMenuPanel` (OptionsPanelTests.cs)
- `MapShadow` (GridPlacementTests.cs, ScreenFitPlayTests.cs)
- `MusicSlider` (OptionsPanelTests.cs)
- `Name` (GameplayTests.cs)
- `OptionsPanel` (OptionsPanelTests.cs)
- `PlayAgainButton` (EndScreenTests.cs)
- `QuitGameButton` (EndScreenTests.cs)
- `RecordsBackButton` (OptionsPanelTests.cs, RecordsMenuTests.cs)
- `RecordsPanel` (OptionsPanelTests.cs, RecordsMenuTests.cs)
- `ResetSettingsButton` (OptionsPanelTests.cs)
- `Row1` (RecordsMenuTests.cs)
- `SettingsButton` (EndScreenTests.cs, ShortcutAndSettingsTests.cs)
- `SettingsPanel` (EndScreenTests.cs, ShortcutAndSettingsTests.cs)
- `SummaryPanel` (AttackAnimationTests.cs, EndScreenTests.cs, ResultsTests.cs)
- `TabLEADERBOARD` (OptionsPanelTests.cs)
- `Text` (OptionsPanelTests.cs)
- `Title` (OptionsPanelTests.cs)
- `UnitButton1` (GameplayTests.cs)
- `UnitButton5` (GameplayTests.cs)
- `WinnerBackgroundImage` (AttackAnimationTests.cs, EndScreenTests.cs)
- `YesButton` (ResultsTests.cs)
- `optionsButton` (OptionsPanelTests.cs)
- `playButton` (RecordsMenuTests.cs)
- `recordsButton` (RecordsMenuTests.cs)

**Obiekty, których testy oczekują, że NIE istnieją:**

- `ChangeModeButton` (GameplayTests.cs)
- `QuitButton` (GameplayTests.cs)
<!-- AUTO:NAMES:END -->

## Które testy sprawdzić dla których plików

Reguły są w `Tests/tools/tests-map.json` (dodaj tam wpis dla nowego obszaru).

<!-- AUTO:MAP:START -->
| Zmieniasz | Sprawdź testy | Dlaczego |
|---|---|---|
| `Assets/Resources/Localization/*`<br>`Assets/Scripts/Localization/*`<br>`Assets/Scenes/*.unity` | LocalizationTests | Nowe lub zmienione teksty muszą mieć tłumaczenie w Polish.txt; zmiana formatu pliku wymaga poprawienia parsera w LocalizationTests. |
| `Assets/StreamingAssets/Grid.csv`<br>`Assets/Prefabs/TilePrefab*`<br>`Assets/Scripts/BoardGrid.cs`<br>`Assets/Scripts/Tiles/*`<br>`Assets/Scriptables/*` | BoardGridTests, GameplayTests | Plansza: wymiary, przeszkody, rogi dowódców i długość ścieżki w BoardGridTests zależą od układu w Grid.csv. |
| `Assets/Scripts/*State.cs`<br>`Assets/Scripts/GameController.cs`<br>`Assets/Scripts/EventManager.cs`<br>`Assets/Scripts/UI/UIController.cs`<br>`Assets/Scripts/UI/PlayerUnitsController.cs`<br>`Assets/Scripts/UI/ButtonUnitController.cs`<br>`Assets/Scripts/UI/UnitTilePanelController.cs`<br>`Assets/Scripts/UI/CommanderBarController.cs` | GameplayTests, EndScreenTests, HintsAndWarningTests, ShortcutAndSettingsTests | Przebieg tury, Call, timer i jego ostrzeżenie (ostatnie 10 s), skróty klawiaturowe (spacja, C), wskazówki w pierwszych turach, podpowiedzi zasięgu, panel info i karty jednostek. |
| `Assets/Scripts/UI/EndGameController.cs`<br>`Assets/Scripts/GameStats.cs`<br>`Assets/Sprites/UI/*_Solid*` | EndScreenTests, PauseAndSummaryTests, GameStatsTests, ShortcutAndSettingsTests, ResultsTests | Ekran końca i menu pauzy (ESC): przyciski, pytanie „Na pewno?”, podsumowanie i wynik, rewanż ze zmianą stron, trudność w ustawieniach, Time.timeScale, przeładowanie scen. |
| `Assets/Scripts/UI/BattleLogController.cs` | BattleLogTests | Historia, przewijanie kółkiem i powrót na dół przy nowym wpisie. |
| `Assets/Scripts/AIController.cs`<br>`Assets/Scripts/GameSession.cs`<br>`Assets/Scripts/Units/*` | AiSoakTests, GameplayTests, PauseAndSummaryTests, AttackAnimationTests | Komputer musi dokończyć partię po obu stronach i na każdym poziomie trudności bez błędów w konsoli; zmiana zasad jednostek zmienia przebieg partii; animacja śmierci zabitej jednostki zaczyna się dopiero po animacji ataku przeciwnika. |
| `Assets/Scenes/MainScene.unity` | GameplayTests, BattleLogTests, EndScreenTests, PauseAndSummaryTests, HintsAndWarningTests, ShortcutAndSettingsTests, ResultsTests | Układ HUD-u (nazwy obiektów, szerokości, pozycje) jest sprawdzany w testach przez nazwy i liczby. |
| `*.asmdef`<br>`Packages/manifest.json` | EditModeTests, PlayModeTests | Zmiana zależności skryptów lub pakietów może zepsuć kompilację testów. |
| `Assets/Scripts/Results/*`<br>`Assets/Scripts/GameStats.cs` | ScoreCalculatorTests, RecordStoreTests, GameResultTests, GameStatsTests, ResultsTests | Punktacja (wzór), zapis partii w games.json (limit 200, uszkodzony plik), ranking, statystyki i wynik po końcu gry. |
| `Assets/Scripts/UI/RecordsController.cs`<br>`Assets/Scripts/UI/MainMenuController.cs`<br>`Assets/Scripts/UI/HoverTint.cs`<br>`Assets/Scenes/MenuScene.unity` | RecordsMenuTests, OptionsPanelTests, SoundAssetTests | Panele menu głównego: Wyniki (ranking, historia, statystyki, czyszczenie) i Options (ten sam układ i rozmiar tekstu co Wyniki, różowy kolor i pasek pod wierszem po najechaniu), oraz dźwięk tik przypisany w scenie menu. |
| `Assets/Scripts/SoundController.cs`<br>`Assets/Sounds/Tick.wav` | SoundAssetTests | Dźwięk tik w ostatnich 10 s tury musi istnieć i być przypisany w SoundController sceny menu. |
| `Assets/Scripts/UI/ScreenFit.cs`<br>`Assets/Scripts/UI/ScreenCover.cs`<br>`Assets/Scripts/UI/WorldBackdrop.cs`<br>`Assets/Scripts/UI/CameraAspectFit.cs` | ScreenFitTests, ScreenFitPlayTests | Układ 16:9 zostaje bez zmian na każdym ekranie: kamera pokazuje całą ramkę 16:9, a tła i przyciemnienie rosną i wypełniają ekran; dekoracje planszy nie zależą od skali kanwy. |
| `Assets/Scripts/GridPlacement.cs`<br>`Assets/Scripts/BoardGrid.cs`<br>`Assets/Scripts/Tiles/TileController.cs`<br>`Assets/Scripts/Units/HealthController.cs` | GridPlacementTests | Obiekt GridPosition (skala i przesunięcie planszy): plansza, cień, jednostki i ich punkty życia idą razem, klikanie trafia w pola tam, gdzie są narysowane, a jednostka chodzi po pomniejszonej planszy. |
<!-- AUTO:MAP:END -->

## Lista testów

<!-- AUTO:TESTS:START -->
Razem: **129** testów w 21 plikach.

- `Tests/unity/EditMode/BoardGridTests.cs` (5): EveryCellOfTheLayoutBecomesATile, TilesOutsideTheBoardDoNotExist, CommandersStartOnWalkableCornerTiles, PathGoesAroundObstaclesInTheShortestWay, PathToAnObstacleDoesNotExist
- `Tests/unity/EditMode/GameResultTests.cs` (4): AGameAgainstTheComputerIsScoredWithTheDifficulty, ThePlayersTeamCanBeTheSecondOne, AGameOfTwoPlayersHasNoDifficultyAndNoHumanTeam, TheHighScoreIsThePlaceOne
- `Tests/unity/EditMode/GameStatsTests.cs` (3): ANewGameHasNothingCounted, TurnsCallsAndKillsAreCountedPerTeam, SwappingSidesOnlyChangesAGameAgainstTheComputer
- `Tests/unity/EditMode/LocalizationTests.cs` (5): EveryPolishLineHasAKeyAndATranslation, PolishFileHasNoDuplicateKeys, TranslationsKeepTheFormatPlaceholders, EveryTextOfTheScenesHasATranslation, EveryTextInTheCodeHasATranslation
- `Tests/unity/EditMode/RecordStoreTests.cs` (11): AMissingFileIsAnEmptyList, ASavedGameIsReadBackWithAllItsData, OnlyTheLatest200GamesAreKept, ADamagedFileIsSetAsideAndDoesNotStopTheGame, ClearingRemovesEverything, TheLeaderboardHasOnlyWonGamesAgainstTheComputerBestFirst, TheLeaderboardHasTheTopTenAndCanBeFilteredByDifficulty, AGameKnowsItsPlaceOnTheLeaderboard, TheLatestGamesComeNewestFirst, TheStatisticsCountWinsStreaksAndTheFavoriteUnit, NoGamesGiveEmptyStatistics
- `Tests/unity/EditMode/ScoreCalculatorTests.cs` (6): ALostGameScoresOnlyKillsMinusLosses, AScoreNeverGoesBelowZero, AWinAddsThePointsForTheWinTheSpeedAndTheCommandersHealth, ASlowWinGetsNoSpeedBonus, ALossGetsNoBonusesEvenWithAHealthyCommander, TheDifficultyMultipliesTheScore
- `Tests/unity/EditMode/ScreenFitTests.cs` (5): SixteenByNineNeedsNoChange, AWiderScreenEnlargesTheBackgroundByTheWidthAndKeepsTheCameraHeight, ANarrowerScreenEnlargesTheBackgroundByTheHeightAndWidensTheCamera, TheBackgroundAlwaysCoversTheScreenAndNeverShrinks, TheSpaceBeyondTheFrameIsOnTheSidesOfAWideScreenAndAboveAndBelowATallOne
- `Tests/unity/EditMode/SoundAssetTests.cs` (2): TheTickSoundExists, TheMenuSceneGivesTheTickToTheSoundController
- `Tests/unity/PlayMode/AiSoakTests.cs` (3): TheComputerWinsAgainstAPassingPlayerOnNormal, TheComputerWinsAgainstAPassingPlayerOnHard, TheComputerCanPlayTheOtherSideToo
- `Tests/unity/PlayMode/AttackAnimationTests.cs` (4): TheDeathAnimationStartsRightAfterTheAttackAnimationHasEnded, TheSameHoldsForAUnitThatWasCalled, ADamageThatIsNotAnAttackKillsAtOnce, TheVictimGoesFromTheIdleFramesStraightIntoTheDeathFramesWithoutAJumpBack
- `Tests/unity/PlayMode/BattleLogTests.cs` (6): TheLogListsTheLatestActionsUnderTheTurnHeader, TheLogRemembersMoreThanItShows, ScrollingStaysInsideTheContent, ANewEntryScrollsBackToTheLatest, TheLogSitsUnderTheButtonsWithTheSameWidth, CallAndEndTurnTogetherAreAsWideAsTheLog
- `Tests/unity/PlayMode/EndScreenTests.cs` (8): KillingACommanderEndsTheGameAndShowsTheEndScreen, TheBannerIsAsWideAsTheSummaryAndTheButtonsAsTheBanner, SettingsReplacesTheButtonsAndBackBringsThemBack, PlayAgainStartsANewDraftInTheSameMode, BackToMenuLoadsTheMenu, EscapeOpensThePauseMenuAndStopsTheGame, EscapeInTheSettingsGoesBackToTheButtonsFirst, PausingStopsTheTurnTimer
- `Tests/unity/PlayMode/GameplayTests.cs` (13): TheGameStartsInTheFirstTurnWithBothCommandersOnTheBoard, TheHudButtonsAreWired, CallPutsAUnitNextToTheCommanderAndUsesUpTheCall, CancellingCallKeepsItAvailable, PickingACardToCallDoesNotOfferAnAbility, EndingTheTurnPassesItAndMovesTheControlsToTheOtherSide, TheTurnEndsWhenTheTimerRunsOut, TheInfoPanelIsClearedWhenTheTurnChanges, TheDetailsOfAUnitDoNotListWhatItsTileDoes, RangeHintsAreShownOnlyForTheTeamThatIsPlaying, KilledUnitsKeepTheirGreyedOutStruckCardsAndSayDeadAfterTheTurnChanges, AUnitThatHasMovedShowsNoMoveRangeFromItsNewPlace, AUnitThatHasPlayedShowsNoRanges
- `Tests/unity/PlayMode/GridPlacementTests.cs` (6): TheBoardIsWhereItWasMadeAtScaleOneAndNoOffset, TheScaleChangesTheSizeOfTheWholeBoardAroundItsMiddle, TheOffsetMovesTheWholeBoardAndItsShadowTogether, UnitsStandOnTheirTilesAtAnyScale, ClicksFindTheTilesAndUnitsWhereTheyAreDrawn, AUnitCanMoveOnASmallBoard
- `Tests/unity/PlayMode/HintsAndWarningTests.cs` (4): TheFirstTurnShowsAHintThatFollowsTheStepsOfTheTurn, EachPlayerGetsHintsInTheFirstTurnAndNotLater, TheComputersTurnHasNoHints, TheLastSecondsAreShownInTheWarningColor
- `Tests/unity/PlayMode/OptionsPanelTests.cs` (7): TheRowsHaveTheTextSizeOfTheRecordsTabs, TheTitleAndTheBackButtonShareTheLeftEdgeLikeInRecords, EveryRowTurnsPinkUnderTheMouseAndBringsTheBarToItsHeight, TheSlidersAreAPartOfTheirRow, ClosingThePanelWithTheMouseOverARowDoesNotLeaveItPink, ResetSettingsIsWhereClearResultsIsInRecords, ResetSettingsHalvesTheVolumesAndRestoresNormalAndTheNativeResolutionButKeepsTheLanguage
- `Tests/unity/PlayMode/PauseAndSummaryTests.cs` (9): PlayAgainInThePauseMenuAsksFirst, SayingNoGoesBackToThePauseMenu, EscapeInTheQuestionMeansNo, SayingYesLeavesTheGame, TheEndScreenDoesNotAsk, TheSummaryCountsTurnsCallsAndKills, TheSummaryIsNotInThePauseMenu, RematchWithSwappedSidesIsOnlyAgainstTheComputer, RematchSwapsTheSidesAgainstTheComputer
- `Tests/unity/PlayMode/RecordsMenuTests.cs` (7): TheRecordsButtonOpensThePanelAndBackClosesIt, WithoutGamesThePanelSaysSo, TheLeaderboardListsTheBestWinsFirstAndCanBeFilteredByDifficulty, TheHistoryShowsTheLatestTenAndTheDetailsOfAGame, TheStatisticsShowTheWinRateAndTheBestScore, ClearingTheResultsTakesTwoClicks, SwitchingTabsForgetsAClearThatWasStarted
- `Tests/unity/PlayMode/ResultsTests.cs` (8): AWonGameAgainstTheComputerIsSavedWithItsScoreAndPlace, TheSavedScoreFollowsTheFormula, ALostGameIsSavedButIsNotOnTheLeaderboard, AGameOfTwoPlayersIsOnlyInTheHistory, AGoodScoreTakesAPlaceOnTheLeaderboard, TheSavedGameHasTheUnitsOfBothTeamsAndTheBattleLog, AGameThatIsLeftIsNotSaved, TheSummaryHasTheTeamNumbersInTwoColumnsInsideItsPanel
- `Tests/unity/PlayMode/ScreenFitPlayTests.cs` (5): TheCameraKeepsTheWholeSixteenByNineFrameInView, TheBackgroundsFillTheScreenOfAnyShape, TheShadowOfTheBoardStaysWhereItIsWhateverTheScreen, TheHudSticksToTheCornersAndEdgesOfTheScreen, TheControlsOfThePlayerWithTheTurnStickToHisCornerAndFollowTheTurn
- `Tests/unity/PlayMode/ShortcutAndSettingsTests.cs` (8): SpaceEndsTheTurn, CStartsCallAndPressingItAgainCancelsIt, CDoesNothingOnceTheCallIsUsedUp, TheShortcutsAreOffInThePauseMenu, TheShortcutsAreOffAfterTheGame, TheShortcutsAreOffInTheComputersTurn, TheSettingsOfThePauseMenuHaveTheDifficultyThatCyclesAndIsKept, TheSettingsAfterTheGameHaveTheDifficultyToo
<!-- AUTO:TESTS:END -->

## Ostatnie zmiany wymagające przeglądu testów

Wpis dodaje się przy każdym commicie, który zmienia pliki z tabeli powyżej (najnowsze na górze, ostatnie 15).

<!-- AUTO:LOG:START -->
- 2026-10-02: `Assets/Scripts/UI/RecordsController.cs`, `Assets/Scripts/UI/UIController.cs` → sprawdź: RecordsMenuTests, OptionsPanelTests, SoundAssetTests, GameplayTests, EndScreenTests, HintsAndWarningTests, ShortcutAndSettingsTests
- 2026-10-01: `Assets/Prefabs/TilePrefab.prefab`, `Assets/Resources/Localization/Polish.txt`, `Assets/Scenes/MainScene.unity`, `Assets/Scenes/MenuScene.unity`, `Assets/Scripts/AttackSelectedState.cs`, `+19 więcej` → sprawdź: BoardGridTests, GameplayTests, LocalizationTests, BattleLogTests, EndScreenTests, PauseAndSummaryTests, HintsAndWarningTests, ShortcutAndSettingsTests, ResultsTests, RecordsMenuTests, OptionsPanelTests, SoundAssetTests, GridPlacementTests, GameStatsTests, AiSoakTests, AttackAnimationTests · **bez testów:** `Assets/Scripts/IEffect.cs`, `Assets/Scripts/UI/DamageCalloutController.cs`, `Assets/Scripts/UI/FitTextSize.cs`, `Assets/Scripts/UI/HudLayout.cs`
- 2026-10-01: `Assets/Scripts/UI/WorldBackdrop.cs` → sprawdź: ScreenFitTests, ScreenFitPlayTests · **bez testów:** `Assets/Scripts/UI/BackgroundMotion.cs`, `Assets/Scripts/UI/HideWhileActive.cs`
- 2026-09-30: `Assets/Scenes/MainScene.unity`, `Assets/Scripts/UI/BattleLogController.cs`, `Assets/Scripts/UI/ScreenFit.cs`, `Assets/Scripts/UI/UIController.cs` → sprawdź: LocalizationTests, GameplayTests, BattleLogTests, EndScreenTests, PauseAndSummaryTests, HintsAndWarningTests, ShortcutAndSettingsTests, ResultsTests, ScreenFitTests, ScreenFitPlayTests · **bez testów:** `Assets/Scripts/UI/ScreenCorner.cs`, `Assets/Scripts/UI/ScreenCorners.cs`
- 2026-09-30: `Assets/Scenes/MainScene.unity`, `Assets/Scenes/MenuScene.unity`, `Assets/Scripts/UI/CameraAspectFit.cs`, `Assets/Scripts/UI/MainMenuController.cs`, `Assets/Scripts/UI/ScreenCover.cs`, `+2 więcej` → sprawdź: LocalizationTests, GameplayTests, BattleLogTests, EndScreenTests, PauseAndSummaryTests, HintsAndWarningTests, ShortcutAndSettingsTests, ResultsTests, RecordsMenuTests, OptionsPanelTests, SoundAssetTests, ScreenFitTests, ScreenFitPlayTests
- 2026-09-30: `Assets/Resources/Localization/Polish.txt`, `Assets/Scenes/MainScene.unity`, `Assets/Scenes/MenuScene.unity`, `Assets/Scripts/BeginTurnState.cs`, `Assets/Scripts/GameController.cs`, `+20 więcej` → sprawdź: LocalizationTests, GameplayTests, BattleLogTests, EndScreenTests, PauseAndSummaryTests, HintsAndWarningTests, ShortcutAndSettingsTests, ResultsTests, RecordsMenuTests, OptionsPanelTests, SoundAssetTests, GameStatsTests, ScoreCalculatorTests, RecordStoreTests, GameResultTests, AiSoakTests, AttackAnimationTests
- 2026-09-30: `Assets/Resources/Localization/Polish.txt`, `Assets/Scenes/MainScene.unity`, `Assets/Scripts/GameController.cs`, `Assets/Scripts/GameSession.cs`, `Assets/Scripts/GameStats.cs`, `+2 więcej` → sprawdź: LocalizationTests, GameplayTests, BattleLogTests, EndScreenTests, PauseAndSummaryTests, HintsAndWarningTests, AiSoakTests, GameStatsTests
- 2026-09-30: `Assets/Resources/Localization/Polish.txt`, `Assets/Scenes/MainScene.unity`, `Assets/Scripts/GameController.cs`, `Assets/Scripts/UI/EndGameController.cs` → sprawdź: LocalizationTests, GameplayTests, BattleLogTests, EndScreenTests · **bez testów:** `Assets/Scripts/SoundController.cs`, `Assets/Scripts/UI/UnitChoiceController.cs`, `Assets/Scripts/UI/UnitChoiceControllerOld.cs`, `Assets/Scripts/UI/UnitPanelController.cs`
- 2026-09-30: `Assets/Plugins/Demigiant/DOTween/Modules/DOTween.Modules.asmdef`, `Assets/Scripts/Game.asmdef` → sprawdź: EditModeTests, PlayModeTests
<!-- AUTO:LOG:END -->
