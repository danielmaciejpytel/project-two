using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.InputSystem;

public readonly struct GridPosition : IEquatable<GridPosition>
{
    public static readonly GridPosition Invalid = new GridPosition(-1, -1);

    public readonly int x, y;

    public GridPosition(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool Equals(GridPosition other) => x == other.x && y == other.y;

    public override bool Equals(object obj) => obj is GridPosition other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(x, y);

    public override string ToString() => $"({x}, {y})";

    public static bool operator ==(GridPosition gp1, GridPosition gp2) => gp1.Equals(gp2);

    public static bool operator !=(GridPosition gp1, GridPosition gp2) => !gp1.Equals(gp2);
}

public enum HighlightType { MoveRange, Path, Hover, AttackRange, Deployment, Unit, Ability }

public class GameController : MonoBehaviour
{
    private const string GridFileName = "grid.csv";
    private const string MenuSceneName = "MenuScene";

    [Header("Technical:")]
    [SerializeField] private UIController _myUIController;
    [SerializeField] private SpriteRenderer _backgroundImage;
    [SerializeField] private SpriteRenderer _shadowImage;
    [SerializeField] private SpriteRenderer _lineImage;
    [Header("For designers:")]
    [Tooltip("Size of square board Tile, depends on tile sprite size.")]
    [SerializeField] private float _designerTileSize;
    [SerializeField] private float _tileWidth;
    [SerializeField] private float _tileHeight;
    [Tooltip("Every new tile should be added here.")]
    [SerializeField] private GameObject[] _tilePrefabs;
    [Tooltip("Starting player Id (used when the start is not random).")]
    [SerializeField] private int _startingPlayer;
    [Tooltip("Pick the starting player at random, so neither side always gets the first move.")]
    [SerializeField] private bool _randomStartingPlayer = true;
    [Tooltip("Turn limit in seconds")]
    [SerializeField] private int _timeLimit;

    private readonly List<GameObject> _unitPrefabsPlayer1 = new List<GameObject>();
    private readonly List<GameObject> _unitPrefabsPlayer2 = new List<GameObject>();
    private readonly List<UnitController> _units = new List<UnitController>();
    private readonly RaycastHit2D[] _clickHits = new RaycastHit2D[16];
    private IHoverable _hovered;
    private IGameState _myGameState;
    private int _activePlayer;
    private bool _bypassInputLock;
    private BoardGrid _myGrid;
    private Camera _myCamera;
    private bool _gameEnded;

    public static GameController Instance { get; private set; }

    public IGameState CurrentState => _myGameState;
    public int ActivePlayer => _activePlayer;
    public bool IsGameOver => _gameEnded;
    public IReadOnlyList<UnitController> Units => _units;

    // During the computer's turn the human's clicks, hovers and HUD buttons are ignored.
    private bool IsInputLocked => GameSession.IsAiPlayer(_activePlayer) && !_bypassInputLock;

    public static int GetOpponent(int playerId) => playerId == 1 ? 2 : 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void SetState(IGameState newState)
    {
        if (newState != null) _myGameState = newState;
    }

    private void OnUnitClicked(UnitController clickedUnit)
    {
        if (IsInputLocked) return;
        if (_myGameState != null) SetState(_myGameState.UnitClicked(this, clickedUnit));
    }

    private void OnUnitHovered(UnitController hoveredUnit)
    {
        if (IsInputLocked) return;
        if (_myGameState != null) SetState(_myGameState.UnitHovered(this, hoveredUnit));
    }

    private void OnUnitUnhovered(UnitController unhoveredUnit)
    {
        if (IsInputLocked) return;
        if (_myGameState != null) SetState(_myGameState.UnitUnhovered(this, unhoveredUnit));
    }

    private void OnTileClicked(TileController clickedTile)
    {
        if (IsInputLocked) return;
        if (_myGameState != null) SetState(_myGameState.TileClicked(this, clickedTile));
    }

    private void OnTileHovered(TileController hoveredTile)
    {
        if (IsInputLocked) return;
        if (_myGameState != null) SetState(_myGameState.TileHovered(this, hoveredTile));
    }

    private void OnUnitKilled(UnitController killedUnit)
    {
        _myUIController.KillUnit(killedUnit);
        if (killedUnit.IsKing() && !_gameEnded)
        {
            _gameEnded = true;
            _myGameState = new EndState(this, GetOpponent(killedUnit.GetPlayerId()));
        }
    }

    private void OnExecutionEnded(UnitController unit)
    {
        if (_gameEnded || _myGameState == null) return;
        SetState(_myGameState.ExecutionEnd(this));
    }

    private void Start()
    {
        _myCamera = Camera.main;
        _gameEnded = false;
        EventManager events = EventManager.Instance;
        events.OnUnitClicked += OnUnitClicked;
        events.OnUnitHovered += OnUnitHovered;
        events.OnUnitUnhovered += OnUnitUnhovered;
        events.OnTileClicked += OnTileClicked;
        events.OnTileHovered += OnTileHovered;
        events.OnExecutionEnd += OnExecutionEnded;
        events.OnUnitKilled += OnUnitKilled;
    }

    private void Update()
    {
        bool hasPointer = TryGetPointer(out Vector2 screenPosition, out bool pressed);
        // HUD buttons must not also hover or select tiles and units lying under them.
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        GameObject target = hasPointer && !overUI ? GetTopObjectAt(screenPosition) : null;

        UpdateHover(target);
        if (pressed && target != null && target.TryGetComponent(out IClickable clickedObject)) clickedObject.Click();
    }

    private GameObject GetTopObjectAt(Vector2 screenPosition)
    {
        Vector2 worldPosition = _myCamera.ScreenToWorldPoint(screenPosition);
        int hitCount = Physics2D.Raycast(worldPosition, Vector2.zero, ContactFilter2D.noFilter, _clickHits, 0.01f);
        SpriteRenderer topRenderer = null;
        for (int i = 0; i < hitCount; i++)
        {
            if (!_clickHits[i].collider.TryGetComponent(out SpriteRenderer currentRenderer)) continue;
            if (topRenderer == null || IsDrawnAbove(currentRenderer, topRenderer)) topRenderer = currentRenderer;
        }
        return topRenderer != null ? topRenderer.gameObject : null;
    }

    private void UpdateHover(GameObject target)
    {
        IHoverable newHovered = null;
        if (target != null) target.TryGetComponent(out newHovered);
        if (ReferenceEquals(newHovered, _hovered)) return;

        // A unit that died while hovered is already disabled and must not receive the exit.
        if (_hovered is Component oldComponent && oldComponent != null && oldComponent.gameObject.activeInHierarchy) _hovered.PointerExit();
        _hovered = newHovered;
        _hovered?.PointerEnter();
    }

    private static bool TryGetPointer(out Vector2 screenPosition, out bool pressed)
    {
        Touchscreen touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed)
        {
            screenPosition = touch.primaryTouch.position.ReadValue();
            pressed = touch.primaryTouch.press.wasPressedThisFrame;
            return true;
        }
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            screenPosition = mouse.position.ReadValue();
            pressed = mouse.leftButton.wasPressedThisFrame;
            return true;
        }
        screenPosition = default;
        pressed = false;
        return false;
    }

    private static bool IsDrawnAbove(SpriteRenderer candidate, SpriteRenderer current)
    {
        // Sorting layer IDs are arbitrary numbers, the draw order is given by the layer value.
        int candidateLayer = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
        int currentLayer = SortingLayer.GetLayerValueFromID(current.sortingLayerID);
        if (candidateLayer != currentLayer) return candidateLayer > currentLayer;
        return candidate.sortingOrder > current.sortingOrder;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        EventManager events = EventManager.Instance;
        if (events == null) return;
        events.OnUnitClicked -= OnUnitClicked;
        events.OnUnitHovered -= OnUnitHovered;
        events.OnUnitUnhovered -= OnUnitUnhovered;
        events.OnTileClicked -= OnTileClicked;
        events.OnTileHovered -= OnTileHovered;
        events.OnExecutionEnd -= OnExecutionEnded;
        events.OnUnitKilled -= OnUnitKilled;
    }

    public BoardGrid GetGrid() => _myGrid;

    public UIController GetUI() => _myUIController;

    public bool MovesDepleted(int playerId)
    {
        foreach (UnitController unit in _units)
        {
            if (unit.GetPlayerId() == playerId && unit.IsAvailable && !unit.IsKilled && unit.IsDeployed) return false;
        }
        return true;
    }

    public void EndTurnAction()
    {
        if (_myGameState == null || IsInputLocked) return;
        SoundController.Instance.PlayClick();
        SetState(_myGameState.EndTurnPressed(this));
    }

    public void DeployAction()
    {
        if (_myGameState == null || IsInputLocked) return;
        SoundController.Instance.PlayClick();
        SetState(_myGameState.DeploymentPressed(this));
    }

    public void AbilityAction()
    {
        if (_myGameState == null || IsInputLocked) return;
        SoundController.Instance.PlayClick();
        SetState(_myGameState.AbilityPressed(this));
    }

    /// <summary>
    /// Runs a game action on behalf of the computer player (or the turn timer) while human input is locked.
    /// </summary>
    public void RunWithoutInputLock(Action action)
    {
        _bypassInputLock = true;
        try
        {
            action();
        }
        finally
        {
            _bypassInputLock = false;
        }
    }

    // The turn timer ends the turn for whoever is playing, including the computer.
    public void TurnTimeExpired() => RunWithoutInputLock(EndTurnAction);

    public void EndPlayerTurn(int playerId)
    {
        foreach (UnitController unit in _units)
        {
            if (!unit.IsDeployed || unit.IsKilled) continue;
            foreach (IEndturnable endturnObject in unit.GetComponents<IEndturnable>())
            {
                endturnObject.EndTurnAction(playerId);
            }
        }
        _myGrid.MakeEndTurnActions(playerId);
        _activePlayer = GetOpponent(playerId);
        _myUIController.StartPlayerTurn(_activePlayer);
        EventManager.Instance.TurnStarted(_activePlayer);
    }

    public void AddUnitPrefab(GameObject unitPrefab, int playerId)
    {
        if (playerId == 1) _unitPrefabsPlayer1.Add(unitPrefab);
        else _unitPrefabsPlayer2.Add(unitPrefab);
    }

    public void StartGame()
    {
        string configFilePath = Path.Combine(Application.streamingAssetsPath, GridFileName);
        if (!File.Exists(configFilePath))
        {
            Debug.LogError($"Board layout not found: {configFilePath}");
            return;
        }
        _myGrid = new BoardGrid(File.ReadAllLines(configFilePath), _tilePrefabs, _designerTileSize, _tileWidth, _tileHeight);
        if (_randomStartingPlayer) _startingPlayer = UnityEngine.Random.Range(1, 3);
        _myGameState = new BeginTurnState(_startingPlayer);
        SpawnUnits(_unitPrefabsPlayer1, _myGrid.GetTile(0, _myGrid.GetBoardHeight() - 1));
        SpawnUnits(_unitPrefabsPlayer2, _myGrid.GetTile(_myGrid.GetBoardWidth() - 1, 0));
        // Run tile reactions again once every commander is on the board, so skills depending on neighbours see them.
        foreach (UnitController unit in _units)
        {
            if (!unit.IsDeployed) continue;
            foreach (IEnterTile reactor in unit.GetComponents<IEnterTile>())
            {
                reactor.EnterTileAction(unit.CurrentTile);
            }
        }
        _myUIController.InitializeUnitsPanel(_units, _startingPlayer, this, _timeLimit);
        _activePlayer = _startingPlayer;
        if (GameSession.AiPlayerId != GameSession.NoAi) gameObject.AddComponent<AIController>().Initialize(this, GameSession.AiPlayerId, GameSession.Difficulty);
        EventManager.Instance.TurnStarted(_startingPlayer);
    }

    private void SpawnUnits(List<GameObject> unitPrefabs, TileController commanderTile)
    {
        foreach (GameObject unitPrefab in unitPrefabs)
        {
            UnitController newUnit = Instantiate(unitPrefab, new Vector3(100.0f, 100.0f, 0.0f), Quaternion.identity).GetComponent<UnitController>();
            newUnit.InitializeUnit();
            if (newUnit.IsKing()) newUnit.DeployUnit(commanderTile);
            _units.Add(newUnit);
        }
    }

    public void ChangeMode()
    {
        SoundController.Instance.PlayClick();
        bool showArt = !_backgroundImage.enabled;
        _backgroundImage.enabled = showArt;
        _shadowImage.enabled = showArt;
        _lineImage.enabled = showArt;
        _myGameState?.ChangeMode(this);
    }

    public UnitController GetCommander(int playerId)
    {
        foreach (UnitController unit in _units)
        {
            if (unit.IsKing() && unit.GetPlayerId() == playerId) return unit;
        }
        return null;
    }

    public bool DeployedThisTurn() => _myUIController.DeployedThisTurn();

    public void QuitPressed()
    {
        SoundController.Instance.PlayClick();
        DOTween.KillAll(false);
        SceneManager.LoadScene(MenuSceneName);
    }

    public void HighlightUnits(int playerId, bool highlightKing)
    {
        foreach (UnitController unit in _units)
        {
            if (unit.GetPlayerId() != playerId || !unit.IsDeployed || unit.IsKilled) continue;
            if (!unit.IsKing() || highlightKing) unit.CurrentTile.Highlight(HighlightType.Ability, false);
        }
    }

    /// <summary>
    /// Finishes the unit's activation and, when the player has nothing left to do, passes the turn.
    /// Returns the state the game should continue in.
    /// </summary>
    public IGameState FinishUnitActivation(UnitController unit)
    {
        int playerId = unit.GetPlayerId();
        unit.SetReticle(false);
        unit.IsAvailable = false;
        _myUIController.MarkUnitUnavailable(unit);
        if (!MovesDepleted(playerId)) return new BeginTurnState(playerId);
        EndPlayerTurn(playerId);
        return new BeginTurnState(GetOpponent(playerId));
    }

    /// <summary>
    /// Ends the turn of <paramref name="playerId"/>, closing the activation of <paramref name="activeUnit"/> if there is one.
    /// </summary>
    public IGameState ForceEndTurn(int playerId, UnitController activeUnit)
    {
        _myGrid.HideHighlight();
        if (activeUnit != null)
        {
            activeUnit.SetReticle(false);
            activeUnit.IsAvailable = false;
            _myUIController.MarkUnitUnavailable(activeUnit);
        }
        _myUIController.EndDeployment();
        EndPlayerTurn(playerId);
        return new BeginTurnState(GetOpponent(playerId));
    }
}
