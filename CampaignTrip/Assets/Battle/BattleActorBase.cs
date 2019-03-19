﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static StatusEffect;

#pragma warning disable CS0618, 0649
public abstract class BattleActorBase : NetworkBehaviour
{
    public bool IsAlive { get { return Health > 0; } }
    public int BasicDamage { get { return basicDamage; } }
    public int MaxHealth { get { return maxHealth; } }
    public Transform UITransform { get { return uiTransform; } }

    public int Health
    {
        get { return health; }
        protected set
        {
            int hp = Mathf.Clamp(value, 0, maxHealth);
            if (IsAlive && hp == 0)
            {
                Die();
            }
            health = hp;
        }
    }
    
    protected HealthBarUI HealthBar { get { return healthBarUI; } }

    [SerializeField] protected Animator animator;
    [SerializeField] protected DamagePopup damagePopup;
    [SerializeField] protected HealthBarUI healthBarUI;
    [SerializeField] protected Transform uiTransform;
    [SerializeField] protected int attacksPerTurn;
    [SerializeField] protected int basicDamage;
    [SerializeField] protected int maxHealth;
    
    protected int blockAmount;

    private int health;
    private Dictionary<StatusEffectType, List<Stat>> statusEffects = new Dictionary<StatusEffectType, List<Stat>>();

    //TODO: REMOVE AND REPLACE
    public GameObject tempAbilityTarget;

    private class Stat
    {
        public int RemainingDuration;

        public BattleActorBase GivenBy;
        public StatusEffect Effect;

        public Stat(StatusEffect s, BattleActorBase givenBy, int duration)
        {
            Effect = s;
            GivenBy = givenBy;
            RemainingDuration = duration;
        }
    }

    public void Initialize()
    {
        Health = maxHealth;
        healthBarUI.Init(this);
        damagePopup.Init(this);
    }

    private void OnDestroy()
    {
        if (healthBarUI != null)
            Destroy(healthBarUI.gameObject);
        if (damagePopup != null)
            Destroy(damagePopup.gameObject);
    }

    [Server]
    public abstract void DispatchDamage(int damage, bool canBlock);

    [ClientRpc]
    protected void RpcTakeDamage(int damageTaken, int blocked)
    {
        //TODO: play damage animation
        Health -= damageTaken;
        HealthBar.SetHealth(Health);
        damagePopup.Display(damageTaken, blocked);
    }

    protected abstract void Die();

    public bool HasStatusEffect(StatusEffectType type)
    {
        return statusEffects.ContainsKey(type);
    }

    public BattleActorBase GetGivenBy(StatusEffectType type)
    {
        if (statusEffects.ContainsKey(type))
            return statusEffects[type][0].GivenBy;
        return null;
    }

    [Server] //stacks the duration if it is already in the list
    public void AddStatusEffect(StatusEffect add, BattleActorBase givenBy, int duration)
    {
        RpcAddStatusEffect(add.Type, givenBy.gameObject, duration);
    }

    [ClientRpc]
    private void RpcAddStatusEffect(StatusEffectType type, GameObject givenBy, int duration)
    {
        StatusEffect add = BattleController.Instance.GetStatusEffect(type);
        Stat s = new Stat(add, givenBy.GetComponent<BattleActorBase>(), duration);

        RemoveOnAdd(s.Effect.Type);
        
        if (HasStatusEffect(s.Effect.Type))
        {
            AddStack(s);
        }
        else
        {
            statusEffects.Add(type, new List<Stat>() { s });
        }

        switch (s.Effect.Type)
        {
            case StatusEffectType.Bleed:
                OnAddBleed(s);
                break;
            case StatusEffectType.Blind:
                OnAddBlind(s);
                break;
            case StatusEffectType.Burn:
                OnAddBurn(s);
                break;
            case StatusEffectType.Focus:
                OnAddFocus(s);
                break;
            case StatusEffectType.Freeze:
                OnAddFreeze(s);
                break;
            case StatusEffectType.Invisible:
                OnAddInvisible(s);
                break;
            case StatusEffectType.Poison:
                OnAddPoison(s);
                break;
            case StatusEffectType.Protected:
                OnAddProtected(s);
                break;
            case StatusEffectType.Reflect:
                OnAddReflect(s);
                break;
            case StatusEffectType.Stun:
                OnAddStun(s);
                break;
            case StatusEffectType.Weak:
                OnAddWeak(s);
                break;
            default:
                break;
        }
    }

    private void AddStack(Stat s)
    {
        int maxDuration = 10;
        switch (s.Effect.Type)
        {
            case StatusEffectType.Bleed:
                //stack damage
                statusEffects[s.Effect.Type].Add(s);
                break;
            case StatusEffectType.Blind:
            case StatusEffectType.Burn:
            case StatusEffectType.Freeze:
            case StatusEffectType.Poison:
            case StatusEffectType.Stun:
            case StatusEffectType.Weak:
                //stack duration
                int duration = statusEffects[s.Effect.Type][0].RemainingDuration + s.RemainingDuration;
                statusEffects[s.Effect.Type][0].RemainingDuration += Mathf.Min(duration, maxDuration);
                break;
            default:
                //dont stack
                break;
        }
    }

    private void RemoveOnAdd(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Burn:
                statusEffects.Remove(StatusEffectType.Freeze);
                break;
            case StatusEffectType.Freeze:
                statusEffects.Remove(StatusEffectType.Burn);
                break;
            case StatusEffectType.Protected:
                statusEffects.Remove(StatusEffectType.Protected);
                break;
            default:
                break;
        }
    }
    
    private bool OnAddBleed(Stat s)
    {
        return true;
    }
            
    private bool OnAddBlind(Stat s)
    {
        throw new NotImplementedException();
    }
            
    private bool OnAddBurn(Stat s)
    {
        if (HasStatusEffect(StatusEffectType.Freeze))
        {
            statusEffects.Remove(StatusEffectType.Freeze);
        }
        throw new NotImplementedException();
    }
            
    private bool OnAddFocus(Stat s)
    {
        throw new NotImplementedException();
    }
            
    private bool OnAddFreeze(Stat s)
    {
        if (HasStatusEffect(StatusEffectType.Burn))
        {
            statusEffects.Remove(StatusEffectType.Burn);
        }
        throw new NotImplementedException();
    }
            
    private bool OnAddInvisible(Stat s)
    {
        throw new NotImplementedException();
    }
            
    private bool OnAddPoison(Stat s)
    {
        throw new NotImplementedException();
    }
            
    private bool OnAddProtected(Stat s)
    {
        throw new NotImplementedException();
    }
            
    private bool OnAddReflect(Stat s)
    {
        throw new NotImplementedException();
    }
            
    private bool OnAddStun(Stat s)
    {
        OnStun();
        return true;
    }
            
    private bool OnAddWeak(Stat s)
    {
        throw new NotImplementedException();
    }

    protected virtual void OnStun()
    {
        
    }
    
    public IEnumerator ApplySatusEffects()
    {
        List<StatusEffectType> types = new List<StatusEffectType>(statusEffects.Keys);
        for (int i = 0; i < types.Count; i++)
        {
            List<Stat> s = statusEffects[types[i]];
            yield return DamageOverTime(s);

            DecrementDuration(s[i].Effect.Type);
        }
    }
    
    private void DecrementDuration(StatusEffectType type)
    {
        List<Stat> s = statusEffects[type];
        for (int i = 0; i < s.Count; i++)
        {
            s[i].RemainingDuration--;
            if (s[i].RemainingDuration <= 0)
            {
                s.RemoveAt(i);
                i--;
            }
        }

        if (s.Count == 0)
        {
            statusEffects.Remove(type);
        }
    }

    [Server]
    private IEnumerator DamageOverTime(List<Stat> s)
    {
        switch (s[0].Effect.Type)
        {
            case StatusEffectType.Bleed:
            case StatusEffectType.Burn:
            case StatusEffectType.Freeze:
            case StatusEffectType.Poison:
                int damage = 0;
                for (int i = 0; i < s.Count; i++)
                {
                    damage += s[i].Effect.EffectAmount;
                }
                DispatchDamage(damage, false);
                yield return new WaitForSeconds(0.5f);
                break;
            case StatusEffectType.Blind:
            case StatusEffectType.Focus:
            case StatusEffectType.Invisible:
            case StatusEffectType.Protected:
            case StatusEffectType.Reflect:
            case StatusEffectType.Weak:
            default:
                break;
        }
    }
}
#pragma warning restore CS0618, 0649