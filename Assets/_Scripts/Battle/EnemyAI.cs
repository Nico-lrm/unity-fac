using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private UnitController myUnit;

    // Chance d'utiliser un skill si disponible (30% ici)
    [Range(0, 100)] public int skillUseChance = 30; 

    void Awake()
    {
        myUnit = GetComponent<UnitController>();
    }

    public void DoTurn()
    {
        if (myUnit == null) myUnit = GetComponent<UnitController>();
        StartCoroutine(ThinkAndActSafe());
    }

    IEnumerator ThinkAndActSafe()
    {
        // Sécurité anti-blocage
        CancelInvoke("ForceEndTurn");
        Invoke("ForceEndTurn", 5.0f);

        yield return new WaitForSeconds(1.0f); // Temps de réflexion simulé

        UnitController target = FindClosestPlayer();
        
        if (target != null)
        {
            // --- ÉTAPE 1 : Essayer d'attaquer ou lancer un sort ---
            bool actionDone = TryAttackOrSkill(target);

            // --- ÉTAPE 2 : Si on n'a rien pu faire (trop loin), on bouge ---
            if (!actionDone)
            {
                // On calcule le chemin
                myUnit.CalculateChessMoves();
                
                // On cherche la case la plus proche du joueur
                var possibleMoves = myUnit.validMoveTiles.Keys.ToList();
                if (possibleMoves.Count > 0)
                {
                    // Tri des cases par distance vers le joueur
                    possibleMoves.Sort((a, b) => 
                    {
                        float distA = Vector3.Distance(a.transform.position, target.transform.position);
                        float distB = Vector3.Distance(b.transform.position, target.transform.position);
                        return distA.CompareTo(distB);
                    });

                    // On bouge vers la meilleure case
                    yield return StartCoroutine(myUnit.MoveChessPiece(possibleMoves[0]));
                    
                    // Après avoir bougé, on réessaie d'attaquer (si on a encore des PA)
                    yield return new WaitForSeconds(0.5f);
                    TryAttackOrSkill(target);
                }
            }
        }

        // Fin du tour
        yield return new WaitForSeconds(0.5f);
        EndAITurn();
    }

    bool TryAttackOrSkill(UnitController target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position);
        int distInt = Mathf.CeilToInt(dist - 0.1f);

        // A. Tenter un SKILL (Aléatoire)
        if (myUnit.data.skills.Count > 0 && Random.Range(0, 100) < skillUseChance)
        {
            // On prend un skill au hasard
            SkillData chosenSkill = myUnit.data.skills[Random.Range(0, myUnit.data.skills.Count)];
            
            // On vérifie Portée et Coût
            if (distInt <= chosenSkill.range && myUnit.currentAP >= chosenSkill.apCost && !chosenSkill.isHeal)
            {
                myUnit.EnterCombatMode(chosenSkill); // Prépare le skill
                myUnit.PerformCombatAction(target);  // Lance le skill
                return true;
            }
        }

        // B. Sinon, ATTAQUE DE BASE
        // On vérifie la portée de base de l'unité
        if (distInt >= myUnit.data.minRange && distInt <= myUnit.data.maxRange)
        {
            if (myUnit.currentAP >= 3) // Coût attaque base
            {
                myUnit.EnterCombatMode(null); // Null = Attaque de base
                myUnit.PerformCombatAction(target);
                return true;
            }
        }

        return false;
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

    UnitController FindClosestPlayer()
    {
        var players = GameManager.Instance.allUnits.Where(u => u.isPlayerTeam && u.currentHP > 0).ToArray();
        if (players.Length == 0) return null;
        return players.OrderBy(p => Vector3.Distance(transform.position, p.transform.position)).First();
    }
}