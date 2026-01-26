using UnityEngine;

[CreateAssetMenu(fileName = "Mission_X_X", menuName = "Tactical/Mission Data")]
public class MissionData : ScriptableObject
{
    public string missionName; // Ex: "1-1 : L'Attaque"
    public string sceneName;   // Ex: "Mission_1_1"

    public MapDefinition mapConfig;

    [TextArea] public string description; // Ex: "Tuez le Roi ennemi."
}