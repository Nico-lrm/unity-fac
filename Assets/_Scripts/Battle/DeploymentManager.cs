using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class DeploymentManager : MonoBehaviour
{
    [Header("Données")]
    public UnitDatabase unitDB; 

    [System.Serializable]
    public class DeploymentCategory
    {
        public string name;          // Nom pour aider dans l'inspecteur (ex: "Rois")
        public ChessType type;       // Le type d'unité lié (King, Queen...)
        public Transform container;  // La ligne (Horizontal Layout) où mettre les boutons
    }

    [Header("UI Références")]
    // On remplace le simple rosterContainer par une liste de catégories
    public List<DeploymentCategory> categories; 

    public Transform teamContainer;  
    public GameObject unitButtonPrefab; 
    
    [Header("UI Infos")]
    public TextMeshProUGUI infoText; 
    public Button launchButton;

    // --- LOGIQUE ---
    private List<UnitData> myTeam = new List<UnitData>();
    
    private const int MAX_SLOTS = 4; 
    private int currentPoints = 0;   
    private const int MAX_POINTS = 5; 
    private bool hasKing = false;

    void Start()
    {
        GenerateRoster();
        UpdateUI();
    }

    void GenerateRoster()
    {
        // 1. Nettoyage de TOUTES les catégories
        foreach (var cat in categories)
        {
            if (cat.container != null)
            {
                foreach (Transform child in cat.container) Destroy(child.gameObject);
            }
        }

        // 2. Création des boutons depuis la DB
        foreach (var unit in unitDB.allUnits) 
        {
            UnitData data = unit; 
            if (data == null) continue;

            // On cherche dans notre liste la catégorie qui correspond au type de l'unité
            DeploymentCategory targetCat = categories.Find(c => c.type == data.pieceType);

            if (targetCat != null && targetCat.container != null)
            {
                // On instancie DANS le conteneur spécifique
                GameObject btnObj = Instantiate(unitButtonPrefab, targetCat.container);
                UnitButtonSlot slot = btnObj.GetComponent<UnitButtonSlot>();
                slot.Setup(data, OnUnitClicked);
                
                // Si l'unité est déjà dans l'équipe (au reload), on la marque
                if (myTeam.Contains(data)) slot.SetSelected(true);
            }
            else
            {
                Debug.LogWarning($"Pas de catégorie configurée pour le type : {data.pieceType}");
            }
        }
    }

    public void OnUnitClicked(UnitData data)
    {
        // Si déjà dans l'équipe On retire
        if (myTeam.Contains(data))
        {
            RemoveUnit(data);
        }
        // Sinon On essaie d'ajouter
        else
        {
            TryAddUnit(data);
        }
        
        UpdateUI();
        RefreshTeamVisuals(); // On ne refresh que la barre du bas
    }

    void TryAddUnit(UnitData data)
    {
        if (data.pieceType == ChessType.King && hasKing) { Debug.Log("Déjà un Roi !"); return; }
        if (myTeam.Count >= MAX_SLOTS) { Debug.Log("Armée complète !"); return; }
        if (currentPoints + data.deploymentCost > MAX_POINTS) { Debug.Log("Pas assez de points !"); return; }
        
        myTeam.Add(data);
        currentPoints += data.deploymentCost;
        if (data.pieceType == ChessType.King) hasKing = true;
    }

    void RemoveUnit(UnitData data)
    {
        myTeam.Remove(data);
        currentPoints -= data.deploymentCost;
        if (data.pieceType == ChessType.King) hasKing = false;
    }

    // Met à jour la barre "Mon Équipe" en bas
    void RefreshTeamVisuals()
    {
        if (teamContainer != null)
        {
            foreach (Transform child in teamContainer) Destroy(child.gameObject);
            foreach (var unit in myTeam)
            {
                GameObject icon = Instantiate(unitButtonPrefab, teamContainer);
                // Dans la barre d'équipe, cliquer retire l'unité
                icon.GetComponent<UnitButtonSlot>().Setup(unit, (d) => { 
                    RemoveUnit(d); 
                    UpdateUI(); 
                    RefreshTeamVisuals(); 
                }); 
                icon.GetComponent<UnitButtonSlot>().SetSelected(true);
            }
        }
    }

    void UpdateUI()
    {
        infoText.text = $"Unités : {myTeam.Count}/{MAX_SLOTS}\nPoints : {currentPoints}/{MAX_POINTS}\nRoi : {(hasKing ? "<color=green>OUI</color>" : "<color=red>NON</color>")}";
        launchButton.interactable = hasKing && myTeam.Count > 1;
    }

    public void LaunchMission()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.selectedRoster = new List<UnitData>(myTeam);
            string sceneToLoad = GameData.Instance.sceneToLoad;
            if (string.IsNullOrEmpty(sceneToLoad)) sceneToLoad = "Mission_1_1";
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}