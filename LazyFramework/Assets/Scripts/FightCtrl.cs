using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 用于控制战斗相关内容
    引入Odin插件，自己面板创建战场信息
 */

public class FightCtrl : MonoBehaviour
{
    public FightData fightData = null;

    // Start is called before the first frame update
    void Start()
    {
        // 句柄相关的控制
    }



    // Update is called once per frame
    void Update()
    {
        fightData.Tick(Time.deltaTime);
    }

    public void BeginFight(List<UnitBase> myUnits,List<UnitBase> DiRenUnits)
    {
        // 根据 我放和敌方信息初始化战场

        fightData = new FightData(myUnits, DiRenUnits);
        fightData.Start();
    }
}


[Serializable]
public class UnitBase
{
    public string Name;
    public int ATK;
    public int DNF;
    public int MaxHP;
    public int CurHP;
    public int MaxMP;
    public int CurMp;
    public float AttackCD = 1f;

    private float lifeTime = 0f;
    private float lastAttackTime=0f;

    // 需要执行的内容， 按时间倒序排序
    private List<ActionData> ActionList = new List<ActionData>();

    /*
     * 这里搞错了一个概念
     * 战场的Tick用于生成事件
     * View的Tick是一个执行的过程，两个保持同步即可。
     * */

    internal void Tick(float deltaTime)
    {
        lifeTime += deltaTime;

        if(lifeTime - lastAttackTime >= AttackCD)
        {
            // Attack
        }

        for(int i = ActionList.Count-1; i <= 0; i--)
        {
            var action = ActionList[i];
            if (lifeTime >= action.beginTime)
            {
                action.action();
                ActionList.Remove(action);
            }
            else
            {
                break;
            }
        }

    }

    /// <summary>
    /// 行为数据
    /// </summary>
    public class ActionData
    {
        public float beginTime;
        public Action action;
    }
        
}

[Serializable]
public class FightData
{
    public List<UnitBase> AUnits;
    public List<UnitBase> BUnits;
    public FightState State = FightState.Ready;

    // 公用对象的保存

    public FightData(List<UnitBase> aUnits, List<UnitBase> bUnits)
    {
        AUnits = aUnits;
        BUnits = bUnits;
    }

    public void Start()
    {
        // 做一些 数据上的初始化 暂无

        State = FightState.Fighting;
    }

    public void Tick(float deltaTime)
    {
        if (State != FightState.Fighting)
        {
            return;
        }

        // 推动双方数据的Tick
        // 每个单位都是以时间驱动的，只是后续加入
        for(int i = 0; i < AUnits.Count; i++)
        {
            var unit = AUnits[i];
            unit.Tick(deltaTime);
        }
        for (int i = 0; i < BUnits.Count; i++)
        {

        }



        // Check End
        if (false)
        {
            return;
        }
        else
        {
            State = FightState.End;
        }
    }


    public enum FightState
    {
        Ready,
        Fighting,
        End,
    }
}