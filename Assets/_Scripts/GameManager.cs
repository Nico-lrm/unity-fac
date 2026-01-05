using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Listes d'Unités")]
    public List<UnitController> allUnits = new List<UnitController>();
    public List<UnitController> playerTeam = new List<UnitController>();
    public List<UnitController> enemyTeam = new List<UnitController>();

    [Header("Tour par Tour")]
    private Queue<UnitController> turnQueue = new Queue<UnitController>();
    public UnitController activeUnit;

    void Awake()
    {
        // Singleton : S'assure qu'il n'y a qu'un seul GameManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Fonction appelée par le MapGenerator une fois que tout le monde est sur la carte
    public void StartCombat(List<UnitController> unitsToInit)
    {
        // On nettoie les listes pour repartir de zéro
        allUnits.Clear();
        playerTeam.Clear();
        enemyTeam.Clear();
        turnQueue.Clear();

        // On remplit les listes et on trie les équipes
        foreach (var unit in unitsToInit)
        {
            if (unit == null) continue;

            allUnits.Add(unit);

            if (unit.isPlayerTeam)
            {
                playerTeam.Add(unit);
            }
            else
            {
                enemyTeam.Add(unit);
            }
        }

        Debug.Log($"Combat lancé ! Joueurs: {playerTeam.Count} | Ennemis: {enemyTeam.Count}");

        // On trie tout le monde par Vitesse (Speed) pour définir l'ordre du tour
        var sortedUnits = allUnits.OrderByDescending(u => u.speed).ToList();

        //  On remplit la file d'attente
        foreach (var unit in sortedUnits)
        {
            turnQueue.Enqueue(unit);
        }

        StartNextTurn();
    }

    public void StartNextTurn()
    {
        if (turnQueue.Count == 0)
        {
            // Si la file est vide (tout le monde a joué), on recommence un round
            RebuildTurnQueue();
        }

        // Si la file est encore vide après rebuild, on arrête
        if (turnQueue.Count == 0) return;

        // On prend le prochain
        activeUnit = turnQueue.Dequeue();

        // Sécurité : Si l'unité est morte entre temps, on passe au suivant
        if (activeUnit == null || activeUnit.currentHP <= 0)
        {
            StartNextTurn();
            return;
        }

        Debug.Log($"➤ C'est le tour de : {activeUnit.unitName} ({(activeUnit.isPlayerTeam ? "Joueur" : "Ennemi")})");
        activeUnit.BeginTurn();
    }

    public void EndTurn()
    {
        // Nettoyage visuel de l'unité qui vient de finir
        if (activeUnit != null)
        {
            activeUnit.EndTurnLogic();
        }

        StartNextTurn();
    }

    // Appelé quand on doit refaire la file d'attente (nouveau round)
    void RebuildTurnQueue()
    {
        var livingUnits = allUnits.Where(u => u != null && u.currentHP > 0).OrderByDescending(u => u.speed).ToList();
        foreach (var unit in livingUnits)
        {
            turnQueue.Enqueue(unit);
        }
    }


    public void OnUnitDied(UnitController unit)
    {
        Debug.Log($"☠️ {unit.unitName} est mort.");

        // On vérifie immédiatement si la partie est finie
        CheckWinCondition();
    }

    public void CheckWinCondition()
    {
        // Compter les vivants dans chaque équipe
        int playersAlive = playerTeam.Count(u => u.currentHP > 0);
        int enemiesAlive = enemyTeam.Count(u => u.currentHP > 0);
        
        // On cherche s'il y a un roi mort
        bool kingIsDead = playerTeam.Any(u => u.data.pieceType == ChessType.King && u.currentHP <= 0);

        if (kingIsDead)
        {
            GameOver(false); // Défaite immédiate si le roi meurt
        }
        else if (playersAlive == 0)
        {
            GameOver(false); // Défaite si toute l'équipe est morte
        }
        else if (enemiesAlive == 0)
        {
            GameOver(true); // Victoire si tous les ennemis sont morts
        }
    }

    public void GameOver(bool win)
    {
        Debug.Log("--- FIN DE PARTIE : " + (win ? "VICTOIRE" : "DÉFAITE") + " ---");

        if (GameData.Instance != null)
        {
            GameData.Instance.lastMissionWon = win;
            // On sauvegarde la scène actuelle pour que le bouton "Recommencer" sache où revenir
            GameData.Instance.sceneToLoad = SceneManager.GetActiveScene().name;
        }

        SceneManager.LoadScene("GameOverScreen");
    }
}