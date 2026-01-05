using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class DeploymentManager : MonoBehaviour
{
    [Header("Données")]
    public UnitDatabase unitDB; 

    [Header("UI Références")]
    public Transform rosterContainer; 
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

    // Liste pour garder une trace des boutons et mettre à jour leur visuel "Sélectionné"
    private List<UnitButtonSlot> allSlots = new List<UnitButtonSlot>();

    void Start()
    {
        GenerateRoster();
        UpdateUI();
    }

    void GenerateRoster()
    {
        // Nettoyage
        foreach (Transform child in rosterContainer) Destroy(child.gameObject);
        allSlots.Clear();

        // Création des boutons depuis la DB
        foreach (var unit in unitDB.allUnits) 
        {
            
            // On utilise directement la variable de la boucle
            UnitData data = unit; 
            
            if (data == null) continue;

            GameObject btnObj = Instantiate(unitButtonPrefab, rosterContainer);
            UnitButtonSlot slot = btnObj.GetComponent<UnitButtonSlot>();
            
            slot.Setup(data, OnUnitClicked);
        }
    }

    public void OnUnitClicked(UnitData data)
    {
        // Si déjà dans l'équipe -> On retire
        if (myTeam.Contains(data))
        {
            RemoveUnit(data);
        }
        // Sinon -> On essaie d'ajouter
        else
        {
            TryAddUnit(data);
        }
        
        UpdateUI();
        RefreshVisuals();
    }

    void TryAddUnit(UnitData data)
    {
        // Vérif Roi Unique
        if (data.pieceType == ChessType.King && hasKing)
        {
            Debug.Log("Vous avez déjà un Roi !");
            return;
        }

        // Vérif Slots (Max 4 bonhommes sur le terrain)
        if (myTeam.Count >= MAX_SLOTS)
        {
            Debug.Log("Armée complète (Max 4 unités) !");
            return;
        }

        // Vérif Points
        if (currentPoints + data.deploymentCost > MAX_POINTS)
        {
            Debug.Log("Pas assez de points de déploiement !");
            return;
        }
        
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

    void RefreshVisuals()
    {
        if (teamContainer != null)
        {
            foreach (Transform child in teamContainer) Destroy(child.gameObject);
            foreach (var unit in myTeam)
            {
                GameObject icon = Instantiate(unitButtonPrefab, teamContainer);
                icon.GetComponent<UnitButtonSlot>().Setup(unit, OnUnitClicked); 
                icon.GetComponent<UnitButtonSlot>().SetSelected(true);
            }
        }
    }

    void UpdateUI()
    {
        infoText.text = $"Unités : {myTeam.Count}/{MAX_SLOTS}\nPoints : {currentPoints}/{MAX_POINTS}\nRoi : {(hasKing ? "<color=green>OUI</color>" : "<color=red>NON</color>")}";
        
        // Le bouton Lancer n'est actif que si on a un Roi et au moins 1 autre unité
        launchButton.interactable = hasKing && myTeam.Count > 1;
    }

    public void LaunchMission()
    {
        // Sauvegarde l'équipe
        if (GameData.Instance != null)
        {
            GameData.Instance.selectedRoster = new List<UnitData>(myTeam);
            
            // Charge la mission (le nom est stocké dans GameData depuis le Menu Principal)
            string sceneToLoad = GameData.Instance.sceneToLoad;
            
            // Sécurité si vide
            if (string.IsNullOrEmpty(sceneToLoad)) sceneToLoad = "Mission_1_1";
            
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}