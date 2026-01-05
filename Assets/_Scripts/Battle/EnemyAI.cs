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
        StartCoroutine(ThinkAndAct());
    }

    IEnumerator ThinkAndAct()
    {
        yield return new WaitForSeconds(1.0f); // Temps de réflexion

        // Calcul des mouvements (Respecte le type : Boss Reine bouge comme Reine, Sbire Pion comme Pion)
        myUnit.CalculateChessMoves();
        List<TileData> possibleMoves = myUnit.validMoveTiles.Keys.ToList();
        
        TileData bestMove = null;
        UnitController targetToAttack = null;

        // CIBLAGE : Y a-t-il quelqu'un à tuer à portée ? (Rayon 1.5 case)
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach(var hit in hits)
        {
            UnitController u = hit.GetComponent<UnitController>();
            // On cherche un joueur vivant
            if (u != null && u.isPlayerTeam && u.currentHP > 0)
            {
                targetToAttack = u;
                break; 
            }
        }

        // MOUVEMENT : Si on ne peut pas attaquer, on se rapproche
        if (targetToAttack == null && possibleMoves.Count > 0)
        {
            UnitController closestPlayer = FindClosestPlayer();
            if (closestPlayer != null)
            {
                // On trie les cases : celle qui est physiquement la plus proche du joueur gagne
                possibleMoves.Sort((a, b) => 
                {
                    float distA = Vector3.Distance(a.transform.position, closestPlayer.transform.position);
                    float distB = Vector3.Distance(b.transform.position, closestPlayer.transform.position);
                    return distA.CompareTo(distB);
                });
                
                // On prend la meilleure, MAIS on vérifie le hasard pour varier un peu (IA moins robotique)
                // 80% chance de prendre le meilleur coup, 20% coup aléatoire
                if(Random.value > 0.2f) bestMove = possibleMoves[0];
                else bestMove = possibleMoves[Random.Range(0, possibleMoves.Count)];
            }
        }

        // --- EXECUTION ---

        if (targetToAttack != null)
        {
            myUnit.PerformAttack(targetToAttack);
            yield return new WaitForSeconds(1f);
        }
        else if (bestMove != null)
        {
            yield return StartCoroutine(myUnit.MoveChessPiece(bestMove));
            // Après mouvement, l'IA pourrait attaquer si elle a encore des PA, mais restons simple pour l'instant
        }
        else
        {
            // Bloqué ou rien à faire
        }

        yield return new WaitForSeconds(0.5f);
        GameManager.Instance.EndTurn();
    }

    UnitController FindClosestPlayer()
    {
        // Trouve le joueur vivant le plus proche
        var players = GameManager.Instance.allUnits.Where(u => u.isPlayerTeam && u.currentHP > 0).ToArray();
        if (players.Length == 0) return null;
        return players.OrderBy(p => Vector3.Distance(transform.position, p.transform.position)).First();
    }
}