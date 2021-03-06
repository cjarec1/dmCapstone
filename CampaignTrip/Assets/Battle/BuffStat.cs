﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static StatusEffect;
using Random = UnityEngine.Random;

/*
    The only thing worse than using UNET
    is using UNET with structs

    it is 3 am

    fuck this
 */

#pragma warning disable 0649
[Serializable]
public class BattleStats
{
    public BuffStatNum AttacksPerTurn;
    public BuffStatNum BasicDamage;
    public BuffStatNum BlockAmount;
    public BuffStatNum MaxHealth;
    public BuffStatEffects Immunities;
    public BuffStatEffects AppliedEffects;
    public BuffStatNum ChanceToApply;
    public BuffStatNum ApplyDuration;

    public static BattleStats Copy(BattleStats template)
    {
        return new BattleStats()
        {
            AttacksPerTurn = BuffStatNum.Copy(template.AttacksPerTurn),
            BasicDamage = BuffStatNum.Copy(template.BasicDamage),
            BlockAmount = BuffStatNum.Copy(template.BlockAmount),
            MaxHealth = BuffStatNum.Copy(template.MaxHealth),
            Immunities = BuffStatEffects.Copy(template.Immunities),
            AppliedEffects = BuffStatEffects.Copy(template.AppliedEffects),
            ChanceToApply = BuffStatNum.Copy(template.ChanceToApply),
            ApplyDuration = BuffStatNum.Copy(template.ApplyDuration),
        };
    }

    public enum BuffType
    {
        AttacksPerTurn, BasicDamage, BlockAmount, MaxHealth,
        Immunities, AppliedEffects, ChanceToApply, ApplyDuration
    }

    public IBuffStat GetStat(BuffType type)
    {
        switch (type)
        {
            case BuffType.AttacksPerTurn:
                return AttacksPerTurn;
            case BuffType.BasicDamage:
                return BasicDamage;
            case BuffType.BlockAmount:
                return BlockAmount;
            case BuffType.MaxHealth:
                return MaxHealth;
            case BuffType.Immunities:
                return Immunities;
            case BuffType.AppliedEffects:
                return AppliedEffects;
            case BuffType.ChanceToApply:
                return ChanceToApply;
            case BuffType.ApplyDuration:
                return ApplyDuration;
            default:
                return null;
        }
    }

    //public void SetStat(BuffType type, IBuffStat stat)
    //{
    //    if (stat is BuffStatNum)
    //    {
    //        BuffStatNum b = (BuffStatNum)stat;
    //        switch (type)
    //        {
    //            case BuffType.AttacksPerTurn:
    //                AttacksPerTurn = b;
    //                break;
    //            case BuffType.BasicDamage:
    //                BasicDamage = b;
    //                break;
    //            case BuffType.BlockAmount:
    //                BlockAmount = b;
    //                break;
    //            case BuffType.MaxHealth:
    //                MaxHealth = b;
    //                break;
    //            case BuffType.ChanceToApply:
    //                ChanceToApply = b;
    //                break;
    //            case BuffType.ApplyDuration:
    //                ApplyDuration = b;
    //                break;
    //        }
    //    }
    //    else
    //    {
    //        BuffStatEffects b = (BuffStatEffects)stat;
    //        switch (type)
    //        {
    //            case BuffType.Immunities:
    //                Immunities = b;
    //                break;
    //            case BuffType.AppliedEffects:
    //                AppliedEffects = b;
    //                break;
    //        }
    //    }
    //}

    public /*BattleStats*/void ApplyRandomBuff()
    {
        List<BuffType> stats = new List<BuffType>()
        {
            BuffType.AttacksPerTurn, BuffType.BasicDamage, BuffType.BlockAmount,
            BuffType.MaxHealth, BuffType.Immunities, BuffType.AppliedEffects
        };

        if (AppliedEffects.Stats.Length > 0)
        {
            stats.Add(BuffType.ChanceToApply);
            stats.Add(BuffType.ApplyDuration);
        }

        List<BuffType> buffable = new List<BuffType>();
        for (int i = 0; i < stats.Count; i++)
        {
            if (GetStat(stats[i]).IsBuffable)
                buffable.Add(stats[i]);
        }

        if (stats.Count > 0)
        {
            BuffType type = stats.Random();
            /*IBuffStat buffedValue = */GetStat(type).Buff();
            //SetStat(type, buffedValue);
        }
        //return this;
    }

    public /*BattleStats*/void ApplyBuff(BuffType type)
    {
        /*IBuffStat buffedValue = */GetStat(type).Buff();
        //SetStat(type, buffedValue);
        //return this;
    }
}

public interface IBuffStat
{
    bool IsBuffable { get; }
    IBuffStat Buff();
}

[Serializable]
public class BuffStatNum : IBuffStat
{
    public bool IsBuffable { get { return value < max; } }
    public int value;
    public int max;
    public int buffAmmount;
    
    public static BuffStatNum Copy(BuffStatNum template)
    {
        return new BuffStatNum()
        {
            value = template.value,
            max = template.max,
            buffAmmount = template.buffAmmount
        };
    }

    public IBuffStat Buff()
    {
        value = Mathf.Min(value + buffAmmount, max);
        return this;
    }

    public static implicit operator int(BuffStatNum b)
    {
        return b.value;
    }
}

[Serializable]
public class BuffStatEffects : IBuffStat
{
    public bool IsBuffable { get { return BuffPool.Length > 0; } }
    public Stat[] Stats;
    //[HideInInspector]
    public Stat[] BuffPool;

    public static BuffStatEffects Copy(BuffStatEffects template)
    {
        return new BuffStatEffects()
        {
            Stats = template.Stats,
            BuffPool = template.BuffPool
        };
    }

    public IBuffStat Buff()
    {
        List<Stat> tempStats = new List<Stat>(Stats);
        List<Stat> tempPool = new List<Stat>(BuffPool);

        int i = Random.Range(0, BuffPool.Length);
        tempStats.Add(BuffPool[i]);
        tempPool.RemoveAt(i);

        Stats = tempStats.ToArray();
        BuffPool = tempPool.ToArray();
        return this;
    }
}