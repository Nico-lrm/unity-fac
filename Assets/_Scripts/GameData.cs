using UnityEngine;
using System.Collections.Generic;

public class GameData : MonoBehaviour
{
    public static GameData Instance;
    
    // l'armée
    public List<UnitData> selectedRoster = new List<UnitData>();
    
    // Résultat
    public bool lastMissionWon = false;

	//La scène à loader
    public string sceneToLoad = "Mission_1_1";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}