using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;      // "VICTOIRE" ou "DÉFAITE"
    public TextMeshProUGUI subTitleText;   // Petit message d'ambiance
    public GameObject retryButton;         // On pourrait le cacher si on veut
    public GameObject menuButton;

    void Start()
    {
        // On récupère le résultat stocké dans GameData
        bool hasWon = false;
        
        if (GameData.Instance != null)
        {
            hasWon = GameData.Instance.lastMissionWon;
        }
        
        // On met à jour les textes
        if (hasWon)
        {
            titleText.text = "<color=green>VICTOIRE !</color>";
            subTitleText.text = "L'ennemi a été vaincu.";
        }
        else
        {
            titleText.text = "<color=red>DÉFAITE...</color>";
            subTitleText.text = "Votre Roi est tombé (ou l'armée est décimée).";
        }
    }

    // Lié au bouton "Recommencer"
    public void OnRetryClicked()
    {
        // On relance la scène qui était stockée dans sceneToLoad (Mission_1_1)
        if (GameData.Instance != null && !string.IsNullOrEmpty(GameData.Instance.sceneToLoad))
        {
            SceneManager.LoadScene(GameData.Instance.sceneToLoad);
        }
        else
        {
            // Sécurité si GameData est vide
            SceneManager.LoadScene("Mission_1_1");
        }
    }

    // Lié au bouton "Menu Principal"
    public void OnMenuClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }
}