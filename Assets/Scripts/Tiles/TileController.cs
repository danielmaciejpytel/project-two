using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class TileController : MonoBehaviour, IClickable
{
    [SerializeField] private SpriteRenderer _overlayColorSpriteRenderer;
    [SerializeField] private SpriteRenderer _overlayMarkerSpriteRenderer;
    [SerializeField] private SpriteRenderer _crosshairSpriteRenderer;
    [SerializeField] private ScriptableTile _tile;
    [SerializeField] private Sprite _designerPlainTileSprite;
    [SerializeField] private Sprite _plainTileSprite;
    [SerializeField] private Sprite _designerCrosshairSprite;
    [SerializeField] private Sprite _crosshairSprite;
    [SerializeField] private Sprite _attackSprite;
    [SerializeField] private Sprite _moveRangeSprite;
    [SerializeField] private Sprite _deploySprite;
    [SerializeField] private Sprite _currentUnitSprite;
    [SerializeField] private Color _inMoveRangeColor;
    [SerializeField] private Color _pathColor;
    [SerializeField] private Color _hoverColor;
    [SerializeField] private Color _inAttackRangeColor;
    [SerializeField] private Color _deploymentZoneColor;
    [SerializeField] private Color _player1Color;
    [SerializeField] private Color _player2Color;
    [SerializeField] private Color _abilityColor;
    public UnitController _myUnit { get; set; }
    public bool _isOccupied { get; set; }
    public int _gCost { get; set; }
    public int _hCost { get; set; }
    public int _fCost { get; set; }
    public TileController _cameFromNode { get; set; }
    private SpriteRenderer _mySpriteRenderer;
    private GridPosition _gridPosition;
    private Color _previousColor;
    private ITileBehaviour _myBehaviour;
    private BoardGrid _myBoard;
    private Sprite _previousMarker;
    private bool _isDesignerMode;
    private PolygonCollider2D _myCollider;
    private BoxCollider2D _myDesignerCollider;

    private void OnMouseEnter()
    {
        if(_isOccupied) EventManager._instance.UnitHovered(_myUnit);
        else if (_tile.isWalkable && !_isOccupied) EventManager._instance.TileHovered(this);
    }

    private void OnMouseExit()
    {
        if (_isOccupied) EventManager._instance.UnitUnhovered(_myUnit);
        else
        {
            _overlayMarkerSpriteRenderer.transform.DOKill(false);
            _overlayMarkerSpriteRenderer.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        }
        if (_tile.isWalkable && !_isOccupied)
        {
            _overlayColorSpriteRenderer.color = _previousColor;
            _overlayMarkerSpriteRenderer.sprite = _previousMarker;
        }
    }

    public void InitializeTile(GridPosition position, BoardGrid myBoardGrid)
    {
        _mySpriteRenderer = GetComponent<SpriteRenderer>();
        _myCollider = GetComponent<PolygonCollider2D>();
        _myDesignerCollider = GetComponent<BoxCollider2D>();
        _mySpriteRenderer.sprite = _tile.tileSprite;
        _crosshairSpriteRenderer.enabled = false;
        _crosshairSpriteRenderer.sprite = _crosshairSprite;
        _overlayColorSpriteRenderer.sprite = _plainTileSprite;
        _gridPosition = position;
        _previousColor = _overlayColorSpriteRenderer.color;
        _previousMarker = _overlayMarkerSpriteRenderer.sprite;
        _myUnit = null;
        _isOccupied = false;
        _gCost = 0;
        _hCost = 0;
        _fCost = 0;
        _myBehaviour = GetComponent<ITileBehaviour>();
        _myBoard = myBoardGrid;
        _isDesignerMode = false;
        _myDesignerCollider.enabled = false;
    }

    public void CalculateFCost()
    {
        _fCost = _gCost + _hCost;
    }

    public int GetGridDistance(GridPosition startingPosition)
    {
        return Mathf.Abs(startingPosition.x - _gridPosition.x) + Mathf.Abs(startingPosition.y - _gridPosition.y);
    }

    public void Click()
    {
        Debug.Log("Kliknięto na tile");
        if (!_isOccupied) EventManager._instance.TileClicked(this);
    }

    public GridPosition GetGridPosition()
    {
        return _gridPosition;
    }

    public bool isWalkable()
    {
        return _tile.isWalkable;
    }

    public void ClearTile()
    {
        _overlayColorSpriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        _overlayMarkerSpriteRenderer.sprite = null;
        _previousColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        _previousMarker = null;
        _crosshairSpriteRenderer.enabled = false;
        _overlayMarkerSpriteRenderer.transform.DOKill(false);
        _overlayMarkerSpriteRenderer.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    public void Highlight(HighlightType hType, bool showAttackRange, int playerId = 0)
    {
        if (!_isOccupied && showAttackRange && _tile.isWalkable) _crosshairSpriteRenderer.enabled = true;
        else
        {
            if (!_isOccupied && _tile.isWalkable || _isOccupied && playerId != _myUnit.GetPlayerId())
            {
                _previousColor = _overlayColorSpriteRenderer.color;
                _previousMarker = _overlayMarkerSpriteRenderer.sprite;
                switch (hType)
                {
                    case HighlightType.MoveRange:
                        _overlayColorSpriteRenderer.color = _inMoveRangeColor;
                        if (!_isDesignerMode) _overlayMarkerSpriteRenderer.sprite = _moveRangeSprite;
                        break;
                    case HighlightType.Path:
                        _overlayColorSpriteRenderer.color = _pathColor;
                        break;
                    case HighlightType.Hover:
                        _overlayColorSpriteRenderer.color = _hoverColor;
                        AnimateHighlight();
                        break;
                    case HighlightType.AttackRange:
                        _overlayColorSpriteRenderer.color = _inAttackRangeColor;
                        if (!_isDesignerMode) _overlayMarkerSpriteRenderer.sprite = _attackSprite;
                        break;
                    case HighlightType.Deployment:
                        if (!_isOccupied)
                        {
                            _overlayColorSpriteRenderer.color = _deploymentZoneColor;
                            if (!_isDesignerMode)
                            {
                                _overlayMarkerSpriteRenderer.sprite = _deploySprite;
                            }
                        }
                        break;
                    case HighlightType.Unit:
                        if (!_isDesignerMode) _overlayMarkerSpriteRenderer.sprite = _currentUnitSprite;
                        if(_myUnit.GetPlayerId() == 1) _overlayColorSpriteRenderer.color = _player1Color;
                        else _overlayColorSpriteRenderer.color = _player2Color;
                        AnimateHighlight();
                        break;
                    case HighlightType.Ability:
                        if (!_isDesignerMode) _overlayMarkerSpriteRenderer.sprite = _attackSprite;
                        _overlayColorSpriteRenderer.color = _abilityColor;
                        break;
                }
            }
        }
    }

    public void StopAnimatingHighlight()
    {
        _overlayMarkerSpriteRenderer.transform.DOKill(false);
        _overlayMarkerSpriteRenderer.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    public void AnimateHighlight()
    {
        if (!_isDesignerMode && _overlayMarkerSpriteRenderer.sprite != null)
        {
            StopAnimatingHighlight();
            _overlayMarkerSpriteRenderer.transform.DOScale(0.8f, 0.5f).SetEase(Ease.OutQuart).SetLoops(-1, LoopType.Yoyo);
        }
    }

    public string GetLetter()
    {
        return _tile.letter;
    }

    public TileController GetAnotherTile(GridPosition tilePosition)
    {
        return _myBoard.GetTile(tilePosition);
    }

    public TileController GetAnotherTile(int x, int y)
    {
        return _myBoard.GetTile(x,y);
    }

    public int GetPlayerZone()
    {
        if (_gridPosition.x < _myBoard.GetBoardWidth() / 2) return 1;
        else return 2;
    }

    public string GetTileName()
    {
        return _tile.tileName;
    }

    public string GetDescription()
    {
        return _tile.tileDescription;
    }

    public void ChangeMode(string newMode, Vector3 newPosition)
    {
        if (newMode == "designer")
        {
            _mySpriteRenderer.sprite = _tile.tileDesignerSprite;
            _overlayColorSpriteRenderer.sprite = _designerPlainTileSprite;
            _crosshairSpriteRenderer.sprite = _designerCrosshairSprite;
            _overlayMarkerSpriteRenderer.color = new Color(_overlayMarkerSpriteRenderer.color.r, _overlayMarkerSpriteRenderer.color.g, _overlayMarkerSpriteRenderer.color.b, 0.0f);
            _isDesignerMode = true;
            _myDesignerCollider.enabled = true;
            _myCollider.enabled = false;
        }
        else
        {
            _mySpriteRenderer.sprite = _tile.tileSprite;
            _overlayColorSpriteRenderer.sprite = _plainTileSprite;
            _crosshairSpriteRenderer.sprite = _crosshairSprite;
            _overlayMarkerSpriteRenderer.color = new Color(_overlayMarkerSpriteRenderer.color.r, _overlayMarkerSpriteRenderer.color.g, _overlayMarkerSpriteRenderer.color.b, 1.0f);
            _isDesignerMode = false;
            _myDesignerCollider.enabled = false;
            _myCollider.enabled = true;
        }
        transform.position = newPosition;
        if (_isOccupied)
        {
            _myUnit.ChangePosition(newPosition);
            _myUnit.ChangeMode(newMode);
        }
    }

    public bool IsDesignerMode()
    {
        return _isDesignerMode;
    }
}
