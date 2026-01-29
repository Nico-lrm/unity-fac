using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource ambienceSource;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // --- 1. FONCTIONS GÉNÉRIQUES (Utilisables par le Menu Principal) ---

    public void PlayMusic(AudioClip clip)
    {
        if (clip != null)
        {
            // Si c'est déjà la même musique, on ne fait rien
            if (musicSource.clip == clip) return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = 0.2f; 
            musicSource.Play();
        }
        else
        {
            musicSource.Stop(); // Si on envoie null, on coupe la musique
        }
    }

    public void PlayAmbience(AudioClip clip)
    {
        if (ambienceSource == null) return;

        if (clip != null)
        {
            if (ambienceSource.clip == clip) return;

            ambienceSource.clip = clip;
            ambienceSource.loop = true;
            ambienceSource.volume = 0.5f;
            ambienceSource.Play();
        }
        else
        {
            ambienceSource.Stop();
        }
    }

    // --- 2. FONCTION SPÉCIALE MISSION (Utilise les fonctions ci-dessus) ---

    public void PlayMissionAudio(MissionData mission)
    {
        PlayMusic(mission.backgroundMusic);
        PlayAmbience(mission.ambienceSFX);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }
}