using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using DG.Tweening;

public struct GridPosition
{
    public int x, y;

    public GridPosition(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static bool operator ==(GridPosition gp1, GridPosition gp2)
    {
        if (gp1.x == gp2.x && gp1.y == gp2.y) return true;
        else return false;
    }

    public static bool operator !=(GridPosition gp1, GridPosition gp2)
    {
        if (gp1.x != gp2.x || gp1.y != gp2.y) return true;
        else return false;
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public enum HighlightType { MoveRange, Path, Hover, AttackRange, Deployment, Unit, Ability}

public class GameController : MonoBehaviour
{
    [Header("Technical:")]
    [SerializeField] private UIController _myUIController;
    [SerializeField] private SpriteRenderer _backgroundImage;
    [SerializeField] private SpriteRenderer _shadowImage;
    [SerializeField] private SpriteRenderer _lineImage;
    [Header("For designers:")]
    [Tooltip("Size of square board Tile, depends on tile sprote size.")]
    [SerializeField] private float _designerTileSize;
    [SerializeField] private float _tileWidth;
    [SerializeField] private float _tileHeight;
    [Tooltip("Every new tile should be added here.")]
    [SerializeField] private GameObject[] _tilePrefabs;
    [Tooltip("Starting player Id.")]
    [SerializeField] private int _startingPlayer;
    [Tooltip("Turn limit in seconds")]
    [SerializeField] private int _timeLimit;

    private static GameController _instance;
    private List<GameObject> _unitPrefabsPlayer1;
    private List<GameObject> _unitPrefabsPlayer2;
    private IGameState _myGameState;
    private BoardGrid _myGrid;
    private List<UnitController> _units;
    private Camera _myCamera;
    private bool _gameEnded;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
        {
            Destroy(this);
        }
        _unitPrefabsPlayer1 = new List<GameObject>();
        _unitPrefabsPlayer2 = new List<GameObject>();
    }

    private void OnUnitClicked(UnitController clickedUnit)
    {
        IGameState newState;
        newState = _myGameState.UnitClicked(this, clickedUnit);
        if(newState != null)
        {
            _myGameState = newState;
        }
    }

    private void OnUnitHovered(UnitController hoveredUnit)
    {
        IGameState newState;
        newState = _myGameState.UnitHovered(this, hoveredUnit);
        if (newState != null)
        {
            _myGameState = newState;
        }
    }

    private void OnUnitUnhovered(UnitController unhoveredUnit)
    {
        IGameState newState;
        newState = _myGameState.UnitUnhovered(this, unhoveredUnit);
        if (newState != null)
        {
            _myGameState = newState;
        }
    }

    private void OnTileClicked(TileController clickedTile)
    {
        IGameState newState;
        newState = _myGameState.TileClicked(this, clickedTile);
        if (newState != null)
        {
            _myGameState = newState;
        }
    }

    private void OnTileHovered(TileController hoveredTile)
    {
        IGameState newState;
        newState = _myGameState.TileHovered(this, hoveredTile);
        if (newState != null)
        {
            _myGameState = newState;
        }
    }

    private void OnUnitKilled(UnitController killedUnit)
    {
        int winner;
        _myUIController.KillUnit(killedUnit);
        if (killedUnit.IsKing())
        {
            _gameEnded = true;
            winner = (killedUnit.GetPlayerId() == 1 ? 2 : 1);
            _myGameState = new EndState(this, winner);
        }
    }

    private void OnExecutionEnded(UnitController unit)
    {
        IGameState newState;
        newState = _myGameState.ExecutionEnd(this);
        if (newState != null && !_gameEnded)
        {
            _myGameState = newState;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _myCamera = Camera.main;
        _units = new List<UnitController>();
        _gameEnded = false;
        EventManager._instance.OnUnitClicked += OnUnitClicked;
        EventManager._instance.OnUnitHovered += OnUnitHovered;
        EventManager._instance.OnUnitUnhovered += OnUnitUnhovered;
        EventManager._instance.OnTileClicked += OnTileClicked;
        EventManager._instance.OnTileHovered += OnTileHovered;
        EventManager._instance.OnExecutionEnd += OnExecutionEnded;
        EventManager._instance.OnUnitKilled += OnUnitKilled;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = _myCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, new Vector2(0, 0), 0.01f);
            SpriteRenderer topRenderer = null;
            foreach(RaycastHit2D hit in hits)
            {
                if(topRenderer == null)
                {
                    topRenderer = hit.collider.gameObject.GetComponent<SpriteRenderer>();
                    continue;
                }
                SpriteRenderer currentRenderer = hit.collider.gameObject.GetComponent<SpriteRenderer>();
                if (currentRenderer.sortingLayerID > topRenderer.sortingLayerID) topRenderer = currentRenderer;
                else if (currentRenderer.sortingLayerID == topRenderer.sortingLayerID)
                {
                    if(currentRenderer.sortingOrder > topRenderer.sortingOrder) topRenderer = currentRenderer;
                }
            }
            if(topRenderer != null)
            {
                IClickable clickedObject = null;
                clickedObject = topRenderer.gameObject.GetComponent<IClickable>();
                if(clickedObject != null) clickedObject.Click();
            }
        }
    }

    private void OnDestroy()
    {
        EventManager._instance.OnUnitClicked -= OnUnitClicked;
        EventManager._instance.OnTileClicked -= OnTileClicked;
        EventManager._instance.OnTileHovered -= OnTileHovered;
        EventManager._instance.OnUnitUnhovered -= OnUnitUnhovered;
        EventManager._instance.OnExecutionEnd -= OnExecutionEnded;
        EventManager._instance.OnUnitKilled -= OnUnitKilled;
        EventManager._instance.OnUnitHovered -= OnUnitHovered;
    }

    public BoardGrid GetGrid()
    {
        return _myGrid;
    }

    public UIController GetUI()
    {
        return _myUIController;
    }

    public bool MovesDepleted(int playerId)
    {
        bool allUnitsNotAvailable = true;
        foreach(UnitController unit in _units)
        {
            if (unit.GetPlayerId() == playerId && unit._isAvailable && !unit._isKilled && unit._isDeployed) allUnitsNotAvailable = false;
        }
        return allUnitsNotAvailable;
    }

    public void EndTurnAction()
    {
        IGameState newState;

        SoundController._instance.PlayClick();
        newState = _myGameState.EndTurnPressed(this);
        if (newState != null)
        {
            _myGameState = newState;
        }
    }

    public void DeployAction()
    {
        IGameState newState;

        SoundController._instance.PlayClick();
        newState = _myGameState.DeploymentPressed(this);
        if (newState != null)
        {
            _myGameState = newState;
        }
    }

    public void AbilityAction()
    {
        IGameState newState;

        SoundController._instance.PlayClick();
        newState = _myGameState.AbilityPressed(this);
        if (newState != null)
        {
            _myGameState = newState;
        }
    }

    public void EndPlayerTurn(int playerId)
    {
        IEndturnable[] endturnableList;

        foreach (UnitController unit in _units)
        {
            endturnableList = unit.gameObject.GetComponents<IEndturnable>();
            if (endturnableList.Length > 0 && unit._isDeployed && !unit._isKilled)
            {
                foreach (IEndturnable endturnObject  in endturnableList)
                {
                    endturnObject.EndTurnAction(playerId);
                }
            }
        }
        _myGrid.MakeEndTurnActions(playerId);
        _myUIController.StartPlayerTurn(playerId==1?2:1);
    }

    public void AddUnitPrefab(GameObject unitPrefab, int playerId)
    {
        if(playerId == 1) _unitPrefabsPlayer1.Add(unitPrefab);
        else _unitPrefabsPlayer2.Add(unitPrefab);
    }

    public void StartGame()
    {
        UnitController newUnit;

        string configFilePath = Application.streamingAssetsPath + "/grid.csv";
        string[] gridFile = File.ReadAllLines(configFilePath);
        _myGrid = new BoardGrid(gridFile, _tilePrefabs, _designerTileSize, _tileWidth, _tileHeight);
        _myGameState = new BeginTurnState(_startingPlayer);
        int i = 0;
        foreach (GameObject unitPrefab in _unitPrefabsPlayer1)
        {
            newUnit = Instantiate(unitPrefab, new Vector3(100.0f, 100.0f, 0.0f), Quaternion.identity).GetComponent<UnitController>();
            newUnit.InitializeUnit();
            if (newUnit.IsKing()) newUnit.DeployUnit(_myGrid.GetTile(0, _myGrid.GetBoardHeight() - 1));
            _units.Add(newUnit);
            i++;
        }
        i = 0;
        foreach (GameObject unitPrefab in _unitPrefabsPlayer2)
        {
            newUnit = Instantiate(unitPrefab, new Vector3(100.0f, 100.0f, 0.0f), Quaternion.identity).GetComponent<UnitController>();
            newUnit.InitializeUnit();
            if (newUnit.IsKing()) newUnit.DeployUnit(_myGrid.GetTile(_myGrid.GetBoardWidth() - 1, 0));
            _units.Add(newUnit);
            i++;
        }
        foreach (UnitController unit in _units)
        {
            IEnterTile[] unitEnterTileReactors;
            if (unit._isDeployed)
            {
                unitEnterTileReactors = unit.gameObject.GetComponents<IEnterTile>();
                foreach (IEnterTile reactor in unitEnterTileReactors)
                {
                    reactor.EnterTileAction(unit._myTile);
                }
            }
        }
        _myUIController.InitializeUnitsPanel(_units, _startingPlayer, this, _timeLimit);
    }

    public void ChangeMode()
    {
        SoundController._instance.PlayClick();
        if (_backgroundImage.enabled)
        {
            _backgroundImage.enabled = false;
            _shadowImage.enabled = false;
            _lineImage.enabled = false;
        }
        else
        {
            _backgroundImage.enabled = true;
            _shadowImage.enabled = true;
            _lineImage.enabled = true;
        }
        _myGameState.ChangeMode(this);
    }

    public UnitController GetCommander(int playerId)
    {
        foreach(UnitController unit in _units)
        {
            if (unit.IsKing() && unit.GetPlayerId() == playerId) return unit;
        }
        return null;
    }

    public bool DeployedThisTurn()
    {
        return _myUIController.DeployedThisTurn();
    }

    public void QuitPressed()
    {
        SoundController._instance.PlayClick();
        DOTween.KillAll(false);
        SceneManager.LoadScene("MenuScene");
    }

    public void HighlightUnits(int playerId, bool highlightKing)
    {
        foreach(UnitController unit in _units)
        {
            if(unit.GetPlayerId() == playerId && unit._isDeployed && !unit._isKilled)
            {
                if (!unit.IsKing() || (unit.IsKing() && highlightKing)) unit._myTile.Highlight(HighlightType.Ability, false);
            }
        }
    }
}
