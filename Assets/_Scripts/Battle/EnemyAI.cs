using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private UnitController myUnit;

    void Awake()
    {
        myUnit = GetComponent<UnitController>();
    }

    public void DoTurn()
    {
        if (myUnit == null) myUnit = GetComponent<UnitController>();
        StartCoroutine(ThinkAndActTactical());
    }

    IEnumerator ThinkAndActTactical()
    {
        CancelInvoke("ForceEndTurn");
        Invoke("ForceEndTurn", 8.0f); 

        yield return new WaitForSeconds(0.8f); 

        // 1. MÉCANIQUE DE MISSION
        if (GameManager.Instance != null && GameManager.Instance.currentMission != null)
        {
            var mechanic = GameManager.Instance.currentMission.activeMechanic;
            UnitController potentialTarget = FindClosestPlayerInGrid(); 
            
            if (mechanic != null && potentialTarget != null && mechanic.OverrideEnemyAI(myUnit, potentialTarget))
            {
                yield return new WaitForSeconds(1.0f);
                EndAITurn();
                yield break; 
            }
        }

        SkillData healSkill = GetBestHealSkill();
        UnitController woundedAlly = FindMostWoundedAlly();

        // Si j'ai un sort de soin, assez d'AP, et quelqu'un à soigner
        if (healSkill != null && woundedAlly != null && myUnit.currentAP >= healSkill.apCost)
        {
            int distToAlly = GetGridDistance(myUnit.gridPosition, woundedAlly.gridPosition);

            // Cas A : Je peux soigner sans bouger
            if (distToAlly <= healSkill.range)
            {
                myUnit.EnterCombatMode(healSkill);
                myUnit.PerformCombatAction(woundedAlly);
                
                yield return new WaitForSeconds(1.0f);
                EndAITurn();
                yield break; // On arrête le tour ici
            }
            // Cas B : Je dois bouger pour soigner
            else
            {
                myUnit.CalculateChessMoves();
                TileData healPos = FindBestTileToHealFrom(woundedAlly, healSkill.range);

                if (healPos != null)
                {
                    yield return StartCoroutine(myUnit.MoveChessPiece(healPos));
                    yield return new WaitForSeconds(0.5f);

                    // Vérification finale après mouvement
                    if (GetGridDistance(myUnit.gridPosition, woundedAlly.gridPosition) <= healSkill.range)
                    {
                        myUnit.EnterCombatMode(healSkill);
                        myUnit.PerformCombatAction(woundedAlly);
                    }
                    
                    yield return new WaitForSeconds(1.0f);
                    EndAITurn();
                    yield break; // On arrête le tour ici
                }
            }
        }


        // 3. LOGIQUE D'ATTAQUE (Classique)
        UnitController target = FindClosestPlayerInGrid();

        if (target != null)
        {
            bool hasAttacked = TryBestAttackAction(target);

            if (!hasAttacked)
            {
                myUnit.CalculateChessMoves();

                TileData bestTile = FindBestTileToAttackFrom(target);

                if (bestTile != null)
                {
                    yield return StartCoroutine(myUnit.MoveChessPiece(bestTile));
                    
                    yield return new WaitForSeconds(0.5f);
                    TryBestAttackAction(target);
                }
                else
                {
                    TileData closestTile = FindClosestTileToTarget();
                    if (closestTile != null)
                    {
                        yield return StartCoroutine(myUnit.MoveChessPiece(closestTile));
                    }
                }
            }
        }

        yield return new WaitForSeconds(1.0f);
        EndAITurn();
    }

    SkillData GetBestHealSkill()
    {
        // On cherche le skill de soin le plus puissant (Power) que l'unité possède
        return myUnit.data.skills
            .Where(s => s.isHeal && myUnit.currentAP >= s.apCost)
            .OrderByDescending(s => s.power)
            .FirstOrDefault();
    }

    UnitController FindMostWoundedAlly()
    {
        // On cherche toutes les unités vivantes de MA propre équipe (pas les joueurs)
        var allies = GameManager.Instance.allUnits
            .Where(u => !u.isPlayerTeam && u.currentHP > 0 && u.currentHP < u.data.maxHP)
            .ToList();

        if (allies.Count == 0) return null;

        // On trie par pourcentage de vie (on soigne le plus critique d'abord)
        return allies.OrderBy(a => (float)a.currentHP / (float)a.data.maxHP).First();
    }

    TileData FindBestTileToHealFrom(UnitController allyToHeal, int skillRange)
    {
        // On cherche une case accessible d'où on peut toucher l'allié
        foreach (var kvp in myUnit.validMoveTiles) 
        {
            TileData tile = kvp.Key;
            int moveCost = kvp.Value;
            int remainingAP = myUnit.currentAP - moveCost;

            int distToAlly = GetGridDistance(tile.gridPosition, allyToHeal.gridPosition);

            // Il faut qu'il reste assez d'AP pour lancer le sort
            if (remainingAP >= 3 && distToAlly <= skillRange)
            {
                return tile; 
            }
        }
        return null;
    }



    bool TryBestAttackAction(UnitController target)
    {
        int dist = GetGridDistance(myUnit.gridPosition, target.gridPosition);

        SkillData bestSkill = null;
        bool canUseBasicAttack = false;
        int maxDamage = -1;

        foreach (var skill in myUnit.data.skills)
        {
            if (skill.isHeal) continue; // On ignore les soins ici car gérés avant

            if (dist <= skill.range && myUnit.currentAP >= skill.apCost)
            {
                if (skill.power > maxDamage)
                {
                    maxDamage = skill.power;
                    bestSkill = skill;
                }
            }
        }

        if (dist >= myUnit.data.minRange && dist <= myUnit.data.maxRange)
        {
            if (myUnit.currentAP >= 3) 
            {
                if (bestSkill == null || myUnit.attackDamage >= maxDamage)
                {
                    canUseBasicAttack = true;
                    if (myUnit.attackDamage > maxDamage) bestSkill = null; 
                }
            }
        }

        if (bestSkill != null)
        {
            myUnit.EnterCombatMode(bestSkill);
            myUnit.PerformCombatAction(target);
            return true;
        }
        else if (canUseBasicAttack)
        {
            myUnit.EnterCombatMode(null);
            myUnit.PerformCombatAction(target);
            return true;
        }

        return false;
    }

    TileData FindBestTileToAttackFrom(UnitController target)
    {
        foreach (var kvp in myUnit.validMoveTiles) 
        {
            TileData tile = kvp.Key;
            int moveCost = kvp.Value;
            int remainingAP = myUnit.currentAP - moveCost;

            int distToTarget = GetGridDistance(tile.gridPosition, target.gridPosition);

            if (remainingAP >= 3 && 
                distToTarget >= myUnit.data.minRange && 
                distToTarget <= myUnit.data.maxRange)
            {
                return tile; 
            }
        }
        return null;
    }

    TileData FindClosestTileToTarget()
    {
        UnitController target = FindClosestPlayerInGrid();
        if (target == null || myUnit.validMoveTiles.Count == 0) return null;

        return myUnit.validMoveTiles.Keys.OrderBy(t => GetGridDistance(t.gridPosition, target.gridPosition)).FirstOrDefault();
    }


    UnitController FindClosestPlayerInGrid()
    {
        var players = GameManager.Instance.allUnits.Where(u => u.isPlayerTeam && u.currentHP > 0).ToArray();
        if (players.Length == 0) return null;
        
        return players.OrderBy(p => GetGridDistance(myUnit.gridPosition, p.gridPosition)).First();
    }

    int GetGridDistance(Vector2 posA, Vector2 posB)
    {
        return Mathf.Abs((int)posA.x - (int)posB.x) + Mathf.Abs((int)posA.y - (int)posB.y);
    }

    void EndAITurn()
    {
        CancelInvoke("ForceEndTurn");
        GameManager.Instance.EndTurn();
    }

    void ForceEndTurn()
    {
        GameManager.Instance.EndTurn();
    }
}