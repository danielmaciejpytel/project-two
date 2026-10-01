using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class UnitController : MonoBehaviour, IClickable, IHoverable, IEndturnable
{
    [SerializeField] private Sprite unitSprite;
    [SerializeField] private Sprite _unitPortrait;
    [SerializeField] private Sprite _unitCard;
    [SerializeField] private Sprite _unitDesignerSprite;
    [SerializeField] private ScriptableUnit _unit;
    [SerializeField] private HealthController _myHealth;
    [SerializeField] private SpriteRenderer _myReticle;
    [SerializeField] private int _myPlayerId;
    [SerializeField] private Vector3 _spriteShift;
    [SerializeField] private AudioClip _myAttackClip;
    [SerializeField] private AudioClip _myDamageClip;
    [SerializeField] private AudioClip _myDeathClip;
    public TileController CurrentTile { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsKilled { get; set; }
    public bool IsDeployed { get; set; }
    public bool HasMoved { get; set; }
    public int FreeAttacksCount { get; set; }
    private SpriteRenderer _mySpriteRenderer;
    private bool _showingPotentialDamage;
    private bool _isDesignerMode;
    private CapsuleCollider2D _myCollider;
    private BoxCollider2D _myDesignerCollider;
    private Animator _myAnimator;
    private UnitController _myTarget;
    private AudioSource _myAudioSource;

    [Tooltip("Name of the Animator controller in Resources/UnitAnimators, loaded when the unit is created.")]
    [SerializeField] private string _animatorName;

    public string AnimatorName => _animatorName;

    private void Awake()
    {
        _mySpriteRenderer = GetComponent<SpriteRenderer>();
        _myCollider = GetComponent<CapsuleCollider2D>();
        _myDesignerCollider = GetComponent<BoxCollider2D>();
        _myAnimator = GetComponent<Animator>();
        // The prefab holds only the controller's name, see UnitAnimators.
        if (_myAnimator.runtimeAnimatorController == null) _myAnimator.runtimeAnimatorController = UnitAnimators.Get(_animatorName);
        _myAudioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if(!_isDesignerMode) _mySpriteRenderer.sprite = unitSprite;
    }

    public void PointerEnter()
    {
        if(IsDeployed) EventManager.Instance.UnitHovered(this);
    }

    public void PointerExit()
    {
        if (IsDeployed) EventManager.Instance.UnitUnhovered(this);
    }

    private IEnumerator MakeMove(List<TileController> movePath)
    {
        ITileBehaviour myTileBehaviour;
        IEnterTile[] enterTileReactors;
        TileController currentNode;
        float step;
        Vector3 shift;

        CurrentTile.Unit = null;
        CurrentTile.IsOccupied = false;
        if (!_isDesignerMode) shift = SpriteShift;
        else shift = Vector3.zero;
        while (movePath.Count > 0)
        {
            currentNode = movePath[0];

            while (Vector3.Distance(currentNode.transform.position + shift, transform.position) > 0.001f)
            {
                // The speed is in board units, so a smaller or larger board is crossed in the same time.
                step = _unit.moveSpeed * GridPlacement.Scale * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, currentNode.transform.position + shift, step);
                yield return 0;
            }
            CurrentTile = currentNode;
            movePath.Remove(currentNode);
        }
        CurrentTile.Unit = this;
        CurrentTile.IsOccupied = true;
        EventManager.Instance.UnitMoved(this, CurrentTile);
        myTileBehaviour = CurrentTile.gameObject.GetComponent<ITileBehaviour>();
        if(myTileBehaviour != null) myTileBehaviour.EnterTileAction(this);
        enterTileReactors = GetComponents<IEnterTile>();
        foreach(IEnterTile reactor in enterTileReactors)
        {
            reactor.EnterTileAction(CurrentTile);
        }
        // Losing a bonus on the new tile (e.g. leaving the commander) can kill the unit in designer mode.
        if (CurrentTile != null) _mySpriteRenderer.sortingOrder = CurrentTile.GetGridPosition().y;
        EventManager.Instance.ExecutionEnded(this);
    }

    private int GetBonusArmor()
    {
        int result = 0;
        IArmorModifier[] myArmorModifiers;
        myArmorModifiers = GetComponents<IArmorModifier>();
        foreach(IArmorModifier modifier in myArmorModifiers)
        {
            result += modifier.GetArmorModifier();
        }
        return result;
    }

    public int CalculateDamage(int damage)
    {
        int damageTaken;
        int damageModifier = 0;
        IDamageModifier[] myDamageModifiers;
        myDamageModifiers = GetComponents<IDamageModifier>();
        foreach (IDamageModifier modifier in myDamageModifiers)
        {
            damageModifier += modifier.GetDamageModifier();
        }
        damageTaken = damage - _unit.armor - GetBonusArmor() + damageModifier;
        return Mathf.Max(0, damageTaken);
    }

    private int CalculateAttack(UnitController target)
    {
        IAttackModifier[] myAttackModifiers;
        int modifiersDamageBonus = 0;
        myAttackModifiers = GetComponents<IAttackModifier>();
        foreach (IAttackModifier modifier in myAttackModifiers)
        {
            modifiersDamageBonus += modifier.GetAttackModifier(target);
        }
        return _unit.attackDamage + modifiersDamageBonus;
    }

    public void EndTurnAction(int playerId)
    {
        if (_myPlayerId != playerId)
        {
            IsAvailable = true;
            HasMoved = false;
        }
    }

    public void DeployUnit(TileController initialTile)
    {
        IEnterTile[] enterTileReactors;

        CurrentTile = initialTile;
        CurrentTile.Unit = this;
        CurrentTile.IsOccupied = true;
        transform.position = initialTile.transform.position;
        ITileBehaviour myTileBehaviour = initialTile.gameObject.GetComponent<ITileBehaviour>();
        if (myTileBehaviour != null) myTileBehaviour.EnterTileAction(this);
        enterTileReactors = GetComponents<IEnterTile>();
        foreach (IEnterTile reactor in enterTileReactors)
        {
            reactor.EnterTileAction(CurrentTile);
        }
        if(CurrentTile.IsDesignerMode()) ChangeMode("designer");
        else ChangeMode("player");
        _mySpriteRenderer.sortingOrder = CurrentTile.GetGridPosition().y;
    }

    public void InitializeUnit()
    {
        _isDesignerMode = false;
        _myHealth.InitializeHealth(_unit.unitHealth, _myPlayerId, _unit.isKing);
        _myHealth.SetMode("player");
        _myReticle.enabled = false;
        IsAvailable = true;
        HasMoved = false;
        FreeAttacksCount = _unit.attacksCount;
        IsKilled = false;
        if (_unit.isKing) IsDeployed = true;
        else IsDeployed = false;
        _showingPotentialDamage = false;
        _myDesignerCollider.enabled = false;

    }

    public void Click()
    {
        Debug.Log("Kliknięta jednostka: " + _unit.name);
        EventManager.Instance.UnitClicked(this);
    }

    // Where the unit stands in relation to the middle of its tile, in world units (follows the size of the board).
    private Vector3 SpriteShift => _spriteShift * GridPlacement.Scale;

    public GridPosition GetGridPosition()
    {
        return CurrentTile.GetGridPosition();
    }

    public int GetMoveRange()
    {
        int moveRangeModifier = 0;
        IMoveRangeModifier[] myMoveRangeModifiers;
        myMoveRangeModifiers = GetComponents<IMoveRangeModifier>();
        foreach(IMoveRangeModifier modifier in myMoveRangeModifiers)
        {
            moveRangeModifier += modifier.GetMoveRangeModifier();
        }
        if (_unit.moveRange + moveRangeModifier < 1) return 0;
        else return _unit.moveRange + moveRangeModifier;
    }

    public float GetMoveSpeed()
    {
        return _unit.moveSpeed;
    }

    public void MoveUnit(List<TileController> movePath)
    {
        HasMoved = true;
        StartCoroutine(MakeMove(movePath));
    }


    public void AttackUnit(UnitController target)
    {

        FreeAttacksCount--;
        _myTarget = target;
        if (!_isDesignerMode) _myAnimator.SetTrigger("Attack");
        else AttackEnded();
    }

    public void PlayAttackSound()
    {
        PlayUnitSound(_myAttackClip);
    }

    public void StartAnimation(string animationName)
    {
        if (!_isDesignerMode) _myAnimator.SetTrigger(animationName);
    }

    public void AttackEnded()
    {
        IAddEffect[] myEffectGivers;
        int attackPower = CalculateAttack(_myTarget);
        EventManager.Instance.UnitAttacked(this, _myTarget, attackPower, Mathf.Min(_myTarget.CalculateDamage(attackPower), _myTarget.GetHP()));
        _myTarget.DamageUnit(attackPower, null, this);
        myEffectGivers = GetComponents<IAddEffect>();
        foreach (IAddEffect giver in myEffectGivers)
        {
            giver.AddEffect(_myTarget);
        }
        if (FreeAttacksCount < 1) FreeAttacksCount = _unit.attacksCount;
        EventManager.Instance.ExecutionEnded(this);
    }

    public int GetPlayerId()
    {
        return _myPlayerId;
    }

    public int GetAttackRange()
    {
        int rangeModifier = 0;
        IAttackRangeModifier[] myAttackRangeModifiers;
        myAttackRangeModifiers = GetComponents<IAttackRangeModifier>();
        foreach(IAttackRangeModifier modifier in myAttackRangeModifiers)
        {
            rangeModifier += modifier.GetAttackRangeModifier();
        }
        return _unit.attackRange + rangeModifier;
    }

    /// <param name="source">Name shown in the battle log for damage that doesn't come from an attack (tiles, burning).</param>
    /// <param name="attacker">The unit that attacks: a unit it kills dies only when its attack animation has ended.</param>
    public void DamageUnit(int damage, string source = null, UnitController attacker = null)
    {
        int damageTaken;

        damageTaken = CalculateDamage(damage);
        if (source != null) EventManager.Instance.UnitDamaged(this, Mathf.Min(damageTaken, GetHP()), source);
        if (_myHealth.ChangeHealth(-damageTaken))
        {
            Kill(attacker);
        }
        else
        {
            PlayUnitSound(_myDamageClip);
            if (!_isDesignerMode) _myAnimator.SetTrigger("TakeDamage");
        }
    }

    // The unit is dead for the game at once; its death animation waits until the attacker has finished the attack animation.
    private void Kill(UnitController attacker = null)
    {
        if (IsKilled) return;
        IsKilled = true;
        if (attacker != null && attacker.IsPlayingAttack) StartCoroutine(DieAfterAttack(attacker));
        else PlayDeath();
    }

    private IEnumerator DieAfterAttack(UnitController attacker)
    {
        // The attack animation goes on after the moment of the hit; a limit keeps a stuck animator from holding the death back.
        float end = Time.time + 10.0f;
        while (attacker != null && attacker.IsPlayingAttack && Time.time < end) yield return null;
        PlayDeath();
    }

    private void PlayDeath()
    {
        PlayUnitSound(_myDeathClip);
        if (!_isDesignerMode) _myAnimator.SetTrigger("Die");
        else DeathEnded();
    }

    // True while the animator plays the attack animation. The states of the controllers have different names, so the clip is
    // recognised by its name (like "RedMeleeAttack").
    public bool IsPlayingAttack => !_isDesignerMode && _myAnimator != null && _myAnimator.isActiveAndEnabled && (_myAnimator.GetBool("Attack") || PlaysClip("attack"));

    // True while the animator plays the death animation.
    public bool IsPlayingDeath => !_isDesignerMode && _myAnimator != null && _myAnimator.isActiveAndEnabled && PlaysClip("death");

    // The clip that is playing or, while the animator blends into another state, the one it is blending into: a fast attack hits
    // while its animation is only just starting.
    private bool PlaysClip(string nameEnd)
    {
        foreach (AnimatorClipInfo info in _myAnimator.GetCurrentAnimatorClipInfo(0))
        {
            if (info.weight > 0.5f && NameEndsWith(info.clip, nameEnd)) return true;
        }
        if (_myAnimator.IsInTransition(0))
        {
            foreach (AnimatorClipInfo info in _myAnimator.GetNextAnimatorClipInfo(0))
            {
                if (NameEndsWith(info.clip, nameEnd)) return true;
            }
        }
        return false;
    }

    private static bool NameEndsWith(AnimationClip clip, string nameEnd) => clip != null && clip.name.EndsWith(nameEnd, System.StringComparison.OrdinalIgnoreCase);

    private void PlayUnitSound(AudioClip clip)
    {
        if (clip != null) SoundController.Instance?.PlayClip(clip);
    }

    // Called by the death animation event (or directly in designer mode).
    public void DeathEnded()
    {
        if (CurrentTile == null) return;
        CurrentTile.IsOccupied = false;
        CurrentTile.Unit = null;
        CurrentTile = null;
        gameObject.SetActive(false);
        EventManager.Instance.UnitKilled(this);
    }

    public void HealUnit(int healPoints, string source = null)
    {
        int before = GetHP();
        _myHealth.ChangeHealth(healPoints);
        int healed = GetHP() - before;
        if (healed > 0 && source != null) EventManager.Instance.UnitHealed(this, healed, source);
    }

    public void SetReticle(bool visible)
    {
        if (_isDesignerMode) _myReticle.enabled = visible;
        else
        {
            if (visible) CurrentTile.Highlight(HighlightType.Unit, false);
            else CurrentTile.ClearTile();
        }
    }

    public bool IsKing()
    {
        return _unit.isKing;
    }

    public int GetArmor()
    {
        return _unit.armor + GetBonusArmor();
    }

    public void ChangeHP(int change)
    {
        // Previously this only raised the "killed" event, leaving a dead unit standing on the board.
        if (_myHealth.ChangeHPNumber(change)) Kill();
    }

    public bool IsTargetValid(UnitController attackTarget)
    {
        IValidateTarget[] myValidateTargetModifiers;
        myValidateTargetModifiers = GetComponents<IValidateTarget>();
        foreach(IValidateTarget modifier in myValidateTargetModifiers)
        {
            if (!modifier.IsTargetValid(attackTarget)) return false;
        }
        if (attackTarget.GetPlayerId() != _myPlayerId) return true;
        else return false;
    }

    public void ShowPotentialDamage(int damage)
    {
        int damageTaken;

        if (!_showingPotentialDamage)
        {
            CurrentTile.AnimateHighlight();
            _showingPotentialDamage = true;
            damageTaken = CalculateDamage(damage);
            StartCoroutine(_myHealth.ShowPotentialDamage(damageTaken));
        }
    }

    public void StopShowingPotentialDamage()
    {
        CurrentTile.StopAnimatingHighlight();
        _myHealth.StopShowingPotentialDamage();
        _showingPotentialDamage = false;
    }

    public string GetUnitName()
    {
        return Loc.T(_unit.unitName);
    }

    public int GetHP()
    {
        return _myHealth.GetCurrentHealth();
    }

    public int GetMaxHP()
    {
        // Units on the board can have bonus health (e.g. "11/11" instead of "11/9"); prefabs in the draft use the base value.
        return IsDeployed ? _myHealth.GetMaxHealth() : _unit.unitHealth;
    }

    public int GetBaseMoveRange()
    {
        return _unit.moveRange;
    }

    public int GetAttackStrength()
    {
        return _unit.attackDamage;
    }

    public int GetCalculatedAttack(UnitController target)
    {
        return CalculateAttack(target);
    }

    public int GetBaseAttackRange()
    {
        return _unit.attackRange;
    }

    public int GetBaseArmor()
    {
        return _unit.armor;
    }

    public int GetBaseAttacksCount()
    {
        return _unit.attacksCount;
    }

    public Sprite GetUnitImage()
    {
        if (_isDesignerMode) return _unitDesignerSprite;
        else return _unitPortrait;
    }

    public Sprite GetUnitPortrait()
    {
        return _unitPortrait;
    }

    public Sprite GetUnitCard()
    {
         return _unitCard;
    }

    public int GetUnitType()
    {
        return _unit.unitTypeId;
    }

    public void PlaySound(AudioClip soundToPlay)
    {
        if (soundToPlay != null) SoundController.Instance?.PlayClip(soundToPlay);
    }

    public void ChangePosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    public bool SummoningSickness()
    {
        return _unit.summoningSickness;
    }

    public void HighlighUnitTile(HighlightType hType)
    {
        CurrentTile.Highlight(hType, false);
    }

    public void ChangeMode(string newMode)
    {
        if (newMode == "designer")
        {
            _isDesignerMode = true;
            _mySpriteRenderer.sprite = _unitDesignerSprite;
            transform.position = CurrentTile.transform.position;
            _myDesignerCollider.enabled = true;
            _myCollider.enabled = false;
            _myHealth.SetMode("designer");
        }
        else
        {
            _isDesignerMode = false;
            _mySpriteRenderer.sprite = unitSprite;
            transform.position = CurrentTile.transform.position + SpriteShift;
            _myDesignerCollider.enabled = false;
            _myCollider.enabled = true;
            _myHealth.SetMode("player");
        }
    }
}
