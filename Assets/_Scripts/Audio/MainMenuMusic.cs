using UnityEngine;

public class MainMenuMusic : MonoBehaviour
{
    [Header("Musique du Menu")]
    public AudioClip menuTheme;

    void Start()
    {
        // On demande à l'AudioManager de jouer ce son spécifique
        if (AudioManager.Instance != null)
        {
            // On lance la musique
            AudioManager.Instance.PlayMusic(menuTheme);
            
            // On coupe l'ambiance (pas de bruit de vent/lave dans le menu)
            AudioManager.Instance.PlayAmbience(null); 
        }
    }
}