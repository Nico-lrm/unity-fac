using UnityEngine;

public abstract class MissionMechanic : ScriptableObject
{
    public string mechanicName;

    public virtual void OnAttackHit(UnitController attacker, UnitController defender) { }

    public virtual void OnTurnStart(UnitController unit) { }

    public virtual bool OverrideEnemyAI(UnitController unit, UnitController target) 
    { 
        return false; // Par défaut, on laisse l'IA normale faire
    }
}