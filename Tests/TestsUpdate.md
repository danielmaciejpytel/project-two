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
| `GameplayTests` (PlayMode) | przebieg tury: start, Call (użycie, anulowanie, przełączanie), koniec tury i strona HUD, timer, panel info, podpowiedzi zasięgu tylko dla drużyny z turą, małe karty zabitych |
| `EndScreenTests` (PlayMode) | koniec gry: ekran z 4 przyciskami, szerokości banera i przycisków, Settings, Play again, Back to Menu, pauza ESC i `Time.timeScale` |
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
| **Zmiana układu HUD-u** (szerokości, pozycje) | `BattleLogTests.CallAndEndTurn…`, `TheLogSitsUnderTheTimer…` i `EndScreenTests.TheBannerIsAsWide…` sprawdzają konkretne relacje szerokości (timer = Call + End Turn + odstęp = log; baner = panel Settings; przyciski = baner). Zmień relację w teście, jeśli zmiana jest zamierzona. |
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
- `_body` (BattleLogTests.cs)
- `_entriesRect` (BattleLogTests.cs)
- `_entriesText` (BattleLogTests.cs)
- `_myInfoPanel` (GameplayTests.cs)
- `_myTimer` (EndScreenTests.cs, GameplayTests.cs)
- `_myUnit` (GameplayTests.cs)
- `_name` (GameplayTests.cs)
- `_overlayColorSpriteRenderer` (GameTestUtil.cs)
- `_tile` (BoardGridTests.cs)

**Obiekty sceny (szukane po nazwie):**

- `AbilityButton` (GameplayTests.cs)
- `BackButton` (EndScreenTests.cs)
- `BackToMenuButton` (EndScreenTests.cs)
- `BattleLog` (BattleLogTests.cs)
- `Buttons` (EndScreenTests.cs)
- `DeployMinionButton` (BattleLogTests.cs, EndScreenTests.cs, GameplayTests.cs)
- `EndGameOverlay` (GameTestUtil.cs)
- `EndTurnButton` (BattleLogTests.cs, EndScreenTests.cs, GameplayTests.cs)
- `PlayAgainButton` (EndScreenTests.cs)
- `QuitGameButton` (EndScreenTests.cs)
- `SettingsButton` (EndScreenTests.cs)
- `SettingsPanel` (EndScreenTests.cs)
- `TimerBackgroundImage` (BattleLogTests.cs)
- `UnitButton1` (GameplayTests.cs)
- `UnitButton5` (GameplayTests.cs)
- `WinnerBackgroundImage` (EndScreenTests.cs)

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
| `Assets/Scripts/*State.cs`<br>`Assets/Scripts/GameController.cs`<br>`Assets/Scripts/EventManager.cs`<br>`Assets/Scripts/UI/UIController.cs`<br>`Assets/Scripts/UI/PlayerUnitsController.cs`<br>`Assets/Scripts/UI/ButtonUnitController.cs`<br>`Assets/Scripts/UI/UnitTilePanelController.cs`<br>`Assets/Scripts/UI/CommanderBarController.cs` | GameplayTests, EndScreenTests | Przebieg tury, Call, timer, podpowiedzi zasięgu, panel info i karty jednostek. |
| `Assets/Scripts/UI/EndGameController.cs`<br>`Assets/Sprites/UI/*_Solid*` | EndScreenTests | Ekran końca gry i menu pauzy (ESC): przyciski, szerokości, Time.timeScale, przeładowanie scen. |
| `Assets/Scripts/UI/BattleLogController.cs` | BattleLogTests | Historia, przewijanie kółkiem i powrót na dół przy nowym wpisie. |
| `Assets/Scripts/AIController.cs`<br>`Assets/Scripts/GameSession.cs`<br>`Assets/Scripts/Units/*` | AiSoakTests, GameplayTests | Komputer musi dokończyć partię na każdym poziomie trudności bez błędów w konsoli; zmiana zasad jednostek zmienia przebieg partii. |
| `Assets/Scenes/MainScene.unity` | GameplayTests, BattleLogTests, EndScreenTests | Układ HUD-u (nazwy obiektów, szerokości, pozycje) jest sprawdzany w testach przez nazwy i liczby. |
| `*.asmdef`<br>`Packages/manifest.json` | EditModeTests, PlayModeTests | Zmiana zależności skryptów lub pakietów może zepsuć kompilację testów. |
<!-- AUTO:MAP:END -->

## Lista testów

<!-- AUTO:TESTS:START -->
Razem: **36** testów w 6 plikach.

- `Tests/unity/EditMode/BoardGridTests.cs` (5): EveryCellOfTheLayoutBecomesATile, TilesOutsideTheBoardDoNotExist, CommandersStartOnWalkableCornerTiles, PathGoesAroundObstaclesInTheShortestWay, PathToAnObstacleDoesNotExist
- `Tests/unity/EditMode/LocalizationTests.cs` (5): EveryPolishLineHasAKeyAndATranslation, PolishFileHasNoDuplicateKeys, TranslationsKeepTheFormatPlaceholders, EveryTextOfTheScenesHasATranslation, EveryTextInTheCodeHasATranslation
- `Tests/unity/PlayMode/AiSoakTests.cs` (2): TheComputerWinsAgainstAPassingPlayerOnNormal, TheComputerWinsAgainstAPassingPlayerOnHard
- `Tests/unity/PlayMode/BattleLogTests.cs` (6): TheLogListsTheLatestActionsUnderTheTurnHeader, TheLogRemembersMoreThanItShows, ScrollingStaysInsideTheContent, ANewEntryScrollsBackToTheLatest, TheLogSitsUnderTheTimerWithTheSameWidth, CallAndEndTurnTogetherAreAsWideAsTheTimer
- `Tests/unity/PlayMode/EndScreenTests.cs` (8): KillingACommanderEndsTheGameAndShowsTheEndScreen, TheBannerIsAsWideAsTheSettingsPanelAndTheButtonsAsTheBanner, SettingsReplacesTheButtonsAndBackBringsThemBack, PlayAgainStartsANewDraftInTheSameMode, BackToMenuLoadsTheMenu, EscapeOpensThePauseMenuAndStopsTheGame, EscapeInTheSettingsGoesBackToTheButtonsFirst, PausingStopsTheTurnTimer
- `Tests/unity/PlayMode/GameplayTests.cs` (10): TheGameStartsInTheFirstTurnWithBothCommandersOnTheBoard, TheHudButtonsAreWired, CallPutsAUnitNextToTheCommanderAndUsesUpTheCall, CancellingCallKeepsItAvailable, PickingACardToCallDoesNotOfferAnAbility, EndingTheTurnPassesItAndMovesTheControlsToTheOtherSide, TheTurnEndsWhenTheTimerRunsOut, TheInfoPanelIsClearedWhenTheTurnChanges, RangeHintsAreShownOnlyForTheTeamThatIsPlaying, KilledUnitsKeepTheirSmallCardsAfterTheTurnChanges
<!-- AUTO:TESTS:END -->

## Ostatnie zmiany wymagające przeglądu testów

Wpis dodaje się przy każdym commicie, który zmienia pliki z tabeli powyżej (najnowsze na górze, ostatnie 15).

<!-- AUTO:LOG:START -->
- 2026-09-30: `Assets/Plugins/Demigiant/DOTween/Modules/DOTween.Modules.asmdef`, `Assets/Scripts/Game.asmdef` → sprawdź: EditModeTests, PlayModeTests
<!-- AUTO:LOG:END -->
