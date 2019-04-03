﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS0618, 0649
public abstract class BattleActorBase : NetworkBehaviour
{
    public bool IsAlive { get { return Health > 0; } }
    public int BlockAmount { get { return blockAmount; } }
    public int MaxHealth { get; private set; }
    public Transform UITransform { get { return uiTransform; } }

    public int AttackDamage
    {
        get
        {
            if (HasStatusEffect(StatusEffect.Weak))
                return basicDamage / 2;
            return basicDamage;
        }
    }

    public int Health
    {
        get { return health; }
        protected set
        {
            int hp = Mathf.Clamp(value, 0, MaxHealth);
            if (IsAlive && hp == 0)
            {
                Die();
            }
            health = hp;
            HealthBar.UpdateHealth();
        }
    }
    
    protected HealthBarUI HealthBar { get { return healthBarUI; } }

    [SerializeField] protected Animator animator;
    [SerializeField] protected DamagePopup damagePopup;
    [SerializeField] protected HealthBarUI healthBarUI;
    [SerializeField] protected StatusEffectOverlays overlays;
    [SerializeField] protected Transform uiTransform;
    [SerializeField] protected int attacksPerTurn;
    [SerializeField] protected int basicDamage;
    [SerializeField] protected int blockAmount;
    [SerializeField] protected int health;
    
    private Dictionary<StatusEffect, List<Stat>> statusEffects = new Dictionary<StatusEffect, List<Stat>>();

    //TODO: REMOVE AND REPLACE
    public GameObject tempAbilityTarget;
    
    public enum StatusEffect
    {
        None = -1,
        Bleed,      //Low damage over time. Stacks Damage.
        Blind,      //High chance to miss when attacking.
        Burn,       //High damage over time. Can be put out by teammates.
        Focus,      //Channeling a heal spell. Can be interrupted after x amount of damage taken.
        Freeze,     //Cannot attack enemies or use abilities. Attacks from enemies or allies helps break the ice.
        Invisible,  //Cannot be targeted by enemies.
        Poison,     //Low damage over time. Stacks duration.
        Protected,  //An ally will take all incomming damage for this actor.
        Reflect,    //All incoming damage will be deflected back at the attacker.
        Stun,       //Cannot attack enemies or use abilities.
        Weak,       //Lowers block and attack damage.
        Cure
    }
    
    private class Stat
    {
        //can be the person you are healing or the person protecting you
        public BattleActorBase LinkedActor;
        public StatusEffect Type;
        public int HealthOnRemove;
        public int RemainingDuration;
        public int DOT;
        public bool HasDOT;
        public Action OnRemove;

        public Stat(StatusEffect effect, BattleActorBase linkedActor, int duration, int healthGain = 0)
        {
            Type = effect;
            LinkedActor = linkedActor;
            HealthOnRemove = healthGain;
            RemainingDuration = duration;

            int dot = BattleController.Instance.GetDOT(effect);
            if (dot > -1)
            {
                DOT = dot;
                HasDOT = true;
            }
        }
    }

    #region Initialization

    public void Initialize()
    {
        MaxHealth = health;
        healthBarUI.Init(this);
        damagePopup.Init(this);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (healthBarUI != null)
            Destroy(healthBarUI.gameObject);
        if (damagePopup != null)
            Destroy(damagePopup.gameObject);
    }

    [Server]
    public virtual void OnPlayerPhaseStart() { }

    #endregion

    #region Attack
    
    [Server]
    protected bool TryAttack()
    {
        if (HasStatusEffect(StatusEffect.Blind))
        {
            return UnityEngine.Random.Range(0, 100) > 80;
        }
        return true;
    }

    #endregion

    #region Damage

    [Server]
    public abstract int TakeBlockedDamage(int damage);

    protected abstract void Die();

    [Server]
    public void DispatchBlockableDamage(BattleActorBase attacker, int damage = 0)
    {
        DispatchBlockableDamage(new List<BattleActorBase>() { attacker }, damage);
    }

    [Server]
    public void DispatchBlockableDamage(List<BattleActorBase> attackers, int damage = 0)
    {
        if (HasStatusEffect(StatusEffect.Freeze))
        {
            RpcDecrementDuration(StatusEffect.Freeze, attackers.Count);
            RpcDisplayStat(StatusEffect.Freeze, 0);
        }

        if (HasStatusEffect(StatusEffect.Reflect))
        {
            foreach (BattleActorBase attacker in attackers)
            {
                int damageDealt = (damage > 0) ? damage : attacker.AttackDamage;
                attacker.TakeBlockedDamage(damageDealt);
            }
        }
        else
        {
            int sumDamage = 0;
            if (damage > 0)
            {
                sumDamage = damage * attackers.Count;
            }
            else
            {
                foreach (BattleActorBase attacker in attackers)
                {
                    sumDamage += attacker.AttackDamage;
                }
            }

            if (HasStatusEffect(StatusEffect.Protected))
            {
                BattleActorBase protector = GetOtherActor(StatusEffect.Protected);
                protector.TakeBlockedDamage(sumDamage);
            }
            else
            {
                TakeBlockedDamage(sumDamage);
            }
        }
    }
    
    [ClientRpc]
    protected void RpcTakeDamage(int damageTaken, int blocked)
    {
        if (animator != null && damageTaken > 0)
        {
            animator.SetTrigger("Hurt");
        }
        
        Health -= damageTaken;
        damagePopup.DisplayDamage(damageTaken, blocked);
    }

    [ClientRpc]
    public void RpcMiss()
    {
        damagePopup.DisplayMiss();
    }
    
    public void Heal(int amount)
    {
        Health += amount;
        //TODO: hp popup like damage popup
    }

    #endregion

    #region StatusEffects
    
    protected virtual void OnAddBleed() {}
    protected virtual void OnAddBlind() {}
    protected virtual void OnAddBurn() {}
    protected virtual void OnAddFocus() {}
    protected virtual void OnAddFreeze() {}
    protected virtual void OnAddInvisible() {}
    protected virtual void OnAddPoison() {}
    protected virtual void OnAddProtected() {}
    protected virtual void OnAddReflect() {}
    protected virtual void OnAddStun() {}
    protected virtual void OnAddWeak() {}
    protected virtual void OnAddCure() {}

    public bool HasStatusEffect(StatusEffect type)
    {
        return statusEffects.ContainsKey(type);
    }

    public BattleActorBase GetOtherActor(StatusEffect type)
    {
        if (HasStatusEffect(type))
            return statusEffects[type][0].LinkedActor;
        return null;
    }

    [Server] //stacks the duration if it is already in the list
    public void AddStatusEffect(StatusEffect effect, BattleActorBase otherActor, int duration, int healthOnRemove = 0)
    {
        RpcAddStatusEffect(effect, otherActor.gameObject, duration, healthOnRemove);
    }

    [ClientRpc]
    private void RpcAddStatusEffect(StatusEffect type, GameObject otherActor, int duration, int healthOnRemove)
    {
        BattleController.Instance.PlaySoundEffect(type);

        Stat s = new Stat(type, otherActor.GetComponent<BattleActorBase>(), duration, healthOnRemove);

        RemoveOnAdd(type);
        if (type == StatusEffect.Cure)
        {
            Heal(healthOnRemove);
            return;
        }

        damagePopup.DisplayStat(GetStatColor(type), duration, "+");
        overlays.ToggleOverlay(type, true);

        if (HasStatusEffect(type))
            AddStack(s);
        else
            statusEffects.Add(type, new List<Stat>() { s });

        switch (s.Type)
        {
            case StatusEffect.Bleed:
                OnAddBleed();
                break;
            case StatusEffect.Blind:
                OnAddBlind();
                break;
            case StatusEffect.Burn:
                OnAddBurn();
                break;
            case StatusEffect.Focus:
                OnAddFocus();
                break;
            case StatusEffect.Freeze:
                OnAddFreeze();
                break;
            case StatusEffect.Invisible:
                OnAddInvisible();
                break;
            case StatusEffect.Poison:
                OnAddPoison();
                break;
            case StatusEffect.Protected:
                OnAddProtected();
                break;
            case StatusEffect.Reflect:
                OnAddReflect();
                break;
            case StatusEffect.Stun:
                OnAddStun();
                break;
            case StatusEffect.Weak:
                OnAddWeak();
                break;
            case StatusEffect.Cure:
                OnAddCure();
                break;
            default:
                break;
        }
    }

    private void AddStack(Stat s)
    {
        int maxDuration = 10;
        switch (s.Type)
        {
            case StatusEffect.Bleed:
                //stack damage
                statusEffects[s.Type].Add(s);
                break;
            case StatusEffect.Blind:
            case StatusEffect.Burn:
            case StatusEffect.Freeze:
            case StatusEffect.Poison:
            case StatusEffect.Stun:
            case StatusEffect.Weak:
                //stack duration
                int duration = statusEffects[s.Type][0].RemainingDuration + s.RemainingDuration;
                statusEffects[s.Type][0].RemainingDuration = Mathf.Min(duration, maxDuration);
                break;
            default:
                //dont stack
                break;
        }
    }

    private void RemoveOnAdd(StatusEffect type)
    {
        switch (type)
        {
            case StatusEffect.Burn:
                RemoveStatusEffect(StatusEffect.Freeze);
                break;
            case StatusEffect.Blind:
                RemoveStatusEffect(StatusEffect.Focus);
                break;
            case StatusEffect.Freeze:
                RemoveStatusEffect(StatusEffect.Burn);
                RemoveStatusEffect(StatusEffect.Focus);
                break;
            case StatusEffect.Stun:
                RemoveStatusEffect(StatusEffect.Focus);
                break;
            case StatusEffect.Protected:
                RemoveStatusEffect(StatusEffect.Protected);
                break;
            case StatusEffect.Cure:
                RemoveStatusEffect(StatusEffect.Bleed);
                RemoveStatusEffect(StatusEffect.Blind);
                RemoveStatusEffect(StatusEffect.Burn);
                RemoveStatusEffect(StatusEffect.Freeze);
                RemoveStatusEffect(StatusEffect.Poison);
                RemoveStatusEffect(StatusEffect.Stun);
                RemoveStatusEffect(StatusEffect.Weak);
                break;
            default:
                break;
        }
    }

    [Server]
    public void RemoveStatusEffect(StatusEffect type)
    {
        if (!HasStatusEffect(type))
            return;
        RpcRemoveStatusEffect(type);
    }

    [ClientRpc]
    private void RpcRemoveStatusEffect(StatusEffect type)
    {
        switch (type)
        {
            case StatusEffect.Focus:
                OnRemoveFocus();
                break;
            default:
                break;
        }
        statusEffects.Remove(type);
        overlays.ToggleOverlay(type, false);
    }

    private void OnRemoveFocus()
    {
        Stat s = statusEffects[StatusEffect.Focus][0];
        if (s.RemainingDuration > 0)
        {
            damagePopup.DisplayInterrupt();
        }
        else
        {
            s.LinkedActor.Heal(s.HealthOnRemove);
        }
    }
    
    [Server]
    public bool ApplyDOT(StatusEffect type)
    {
        if (!HasStatusEffect(type))
            return false;

        int dot;
        if (type == StatusEffect.Burn)
        {
            dot = statusEffects[type][0].RemainingDuration;
        }
        else
        {
            dot = 0;
            for (int i = 0; i < statusEffects[type].Count; i++)
            {
                dot += statusEffects[type][i].DOT;
            }
        }

        RpcDisplayStat(type, -1);
        RpcTakeDamage(dot, 0);
        BattleController.Instance.RpcPlaySoundEffect(type);
        return true;
    }

    [ClientRpc]
    public void RpcDisplayStat(StatusEffect type, int durationOffset)
    {
        List<Stat> stats = statusEffects[type];
        damagePopup.DisplayStat(GetStatColor(type), stats[stats.Count - 1].RemainingDuration + durationOffset);
    }

    private Color GetStatColor(StatusEffect type)
    {
        switch (type)
        {
            case StatusEffect.Bleed:
                return Color.red;
            case StatusEffect.Burn:
                return new Color(1, 0.5f, 0);
            case StatusEffect.Poison:
                return Color.green;
            default:
                return Color.black;
        }
    }

    [ClientRpc]
    public void RpcDecrementDurations()
    {
        List<StatusEffect> types = new List<StatusEffect>(statusEffects.Keys);
        for (int i = 0; i < types.Count; i++)
        {
            DecrementDuration(types[i], 1);
        }
    }

    [ClientRpc]
    public void RpcDecrementDuration(StatusEffect type, int decBy)
    {
        if (!HasStatusEffect(type))
            return;

        DecrementDuration(type, decBy);
    }

    private void DecrementDuration(StatusEffect type, int decBy)
    {
        List<Stat> s = statusEffects[type];
        for (int i = 0; i < s.Count; i++)
        {
            s[i].RemainingDuration -= decBy;
            if (s[i].RemainingDuration <= 0)
            {
                if (s.Count > 1)
                {
                    s.RemoveAt(i);
                    i--;
                }
                else
                {
                    RemoveStatusEffect(type);
                }
            }
        }
    }

#endregion
}