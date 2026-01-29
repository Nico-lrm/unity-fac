using UnityEngine;

[CreateAssetMenu(fileName = "Mission_X_X", menuName = "Tactical/Mission Data")]
public class MissionData : ScriptableObject
{
    public string missionID;
    public string missionName; // Ex: "1-1 : L'Attaque"
    public string sceneName;   // Ex: "Mission_1_1"

    public MapDefinition mapConfig;
    
    [Header("Audio d'Ambiance")]
    public AudioClip backgroundMusic;
    public AudioClip ambienceSFX;
    
    [Header("Règle Spéciale")]
    public MissionMechanic activeMechanic;

    [TextArea] public string description; // Ex: "Tuez le Roi ennemi."
}