using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro; // Pour le texte

public enum GameState { Cutscene, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameState state;

    [Header("Références")]
    public SpaceshipController spaceship;
    public TextMeshProUGUI announcementText; // "Mission Start", "Victory"...
    public GameObject announcementPanel; // Le fond noir du texte

    [Header("Listes")]
    public List<UnitController> allUnits = new List<UnitController>();
    public List<UnitController> playerTeam = new List<UnitController>();
    public List<UnitController> enemyTeam = new List<UnitController>();

    private Queue<UnitController> turnQueue = new Queue<UnitController>();
    public UnitController activeUnit;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Appelé par MapGenerator quand la map est prête
    public void StartCombat(List<UnitController> unitsToInit)
    {
        // 1. Initialisation des listes
        allUnits.Clear(); playerTeam.Clear(); enemyTeam.Clear(); turnQueue.Clear();
        foreach (var unit in unitsToInit)
        {
            if (unit == null) continue;
            allUnits.Add(unit);
            if (unit.isPlayerTeam) playerTeam.Add(unit); else enemyTeam.Add(unit);
        }

        // 2. LANCEMENT DE LA SÉQUENCE D'INTRO
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        state = GameState.Cutscene; // BLOQUE LES CLICS
        
        // Cacher l'UI de combat si besoin
        if (announcementPanel != null) announcementPanel.SetActive(false);

        // A. Animation Vaisseau
        if (spaceship != null)
        {
            // On peut cacher les unités joueurs ici et les faire apparaître après si tu veux pousser le réalisme
            yield return spaceship.PlayDropSequence(null);
        }

        // B. Afficher "MISSION START"
        if (announcementText != null)
        {
            announcementPanel.SetActive(true);
            announcementText.text = "MISSION START";
            announcementText.color = Color.white;
            yield return new WaitForSeconds(2f);
            announcementPanel.SetActive(false);
        }

        // C. Début du Jeu
        state = GameState.Playing; // DÉBLOQUE LES CLICS
        RebuildTurnQueue(); // On trie les unités par vitesse
        StartNextTurn();
    }

    public void StartNextTurn()
    {
        if (state == GameState.GameOver) return;

        if (turnQueue.Count == 0) RebuildTurnQueue();
        if (turnQueue.Count == 0) return;

        activeUnit = turnQueue.Dequeue();

        if (activeUnit == null || activeUnit.currentHP <= 0)
        {
            StartNextTurn();
            return;
        }
        
        if (CameraFollow.Instance != null) CameraFollow.Instance.ResetCameraOnActiveUnit();

        activeUnit.BeginTurn();
    }

    public void EndTurn() { StartNextTurn(); }

    void RebuildTurnQueue()
    {
        var livingUnits = allUnits.Where(u => u != null && u.currentHP > 0).OrderByDescending(u => u.speed).ToList();
        foreach (var unit in livingUnits) turnQueue.Enqueue(unit);
    }

    public void OnUnitDied(UnitController unit)
    {
        CheckWinCondition();
    }

    public void CheckWinCondition()
    {
        if (state == GameState.GameOver) return; // Déjà fini

        int playersAlive = playerTeam.Count(u => u.currentHP > 0);
        int enemiesAlive = enemyTeam.Count(u => u.currentHP > 0);
        bool kingIsDead = playerTeam.Any(u => u.data.pieceType == ChessType.King && u.currentHP <= 0);

        if (kingIsDead || playersAlive == 0)
        {
            StartCoroutine(EndGameSequence(false));
        }
        else if (enemiesAlive == 0)
        {
            StartCoroutine(EndGameSequence(true));
        }
    }

    IEnumerator EndGameSequence(bool win)
    {
        state = GameState.GameOver; // BLOQUE TOUT

        // 1. Afficher VICTOIRE / DÉFAITE
        if (announcementPanel != null)
        {
            announcementPanel.SetActive(true);
            announcementText.text = win ? "<color=green>VICTORY</color>" : "<color=red>DEFEAT</color>";
            yield return new WaitForSeconds(2f);
            announcementPanel.SetActive(false);
        }

        // 2. Si Gagné, le vaisseau vient chercher les survivants
        if (win && spaceship != null)
        {
            yield return spaceship.PlayPickupSequence(null);
        }

        // 3. Sauvegarde et Changement de scène
        if (GameData.Instance != null)
        {
            GameData.Instance.lastMissionWon = win;
            GameData.Instance.sceneToLoad = SceneManager.GetActiveScene().name;
        }
        SceneManager.LoadScene("GameOverScreen");
    }
}