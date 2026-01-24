using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Menus")]
    public GameObject actionMenu; // Le panneau avec Attaquer, Skills, Attendre
    public GameObject skillMenu;  // Le panneau qui contient la liste des sorts

    [Header("Boutons Action Menu")]
    public Button attackBtn;
    public Button skillBtn;
    public Button endTurnBtn;
    
    [Header("Stat Panel")]
    public GameObject statPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI apText;

    [Header("Système de Skills")]
    public Transform skillListContainer; // L'endroit où on fait spawn les boutons (Content d'un ScrollView ou Panel)
    public GameObject skillButtonPrefab; // Le modèle de bouton à cloner

    [Header("Bannière Annonce")]
    public GameObject announcementPanel;
    public TextMeshProUGUI announcementText;
    
    // Unité actuellement sélectionnée pour l'UI
    private UnitController currentUIUnit;

    void Awake()
    {
        Instance = this;
        // On cache tout au début
        if(actionMenu != null) actionMenu.SetActive(false);
        if(skillMenu != null) skillMenu.SetActive(false);
        if(announcementPanel != null) announcementPanel.SetActive(false);
    }

    // Appelé quand on clique sur un perso qui peut jouer
    public void ShowActionMenu(bool show, System.Action onAttack, System.Action onWait)
    {
        // Si on demande de fermer
        if (!show) { CloseAllMenus(); return; }

        // Si on demande d'ouvrir
        if (GameManager.Instance.activeUnit != null)
        {
            currentUIUnit = GameManager.Instance.activeUnit;
            actionMenu.SetActive(true);
            
            // Configuration des boutons
            
            // 1. BOUTON ATTAQUE (Base)
            attackBtn.onClick.RemoveAllListeners();
            attackBtn.onClick.AddListener(() => {
                currentUIUnit.EnterCombatMode(null); // Null = Attaque de base
                // Pas de CloseAllMenus ici, c'est EnterCombatMode qui décidera
            });

            // 2. BOUTON SKILLS (Oouvre le sous-menu)
            skillBtn.onClick.RemoveAllListeners();
            // On vérifie s'il a des sorts
            if (currentUIUnit.data.skills.Count > 0)
            {
                skillBtn.interactable = true;
                skillBtn.onClick.AddListener(() => OpenSkillMenu());
            }
            else
            {
                skillBtn.interactable = false; // Grisé si pas de magie
            }

            // 3. BOUTON ATTENDRE
            endTurnBtn.onClick.RemoveAllListeners();
            endTurnBtn.onClick.AddListener(() => {
                GameManager.Instance.EndTurn();
                CloseAllMenus();
            });
        }
    }
    
    public void UpdateStatsPanel(UnitController unit)
    {
        if (unit == null)
        {
            statPanel.SetActive(false);
            return;
        }

        statPanel.SetActive(true);

        // 1. Nom
        nameText.text = unit.unitName;

        // 2. PV (Rouge si bas, Blanc sinon)
        hpText.text = $"HP: {unit.currentHP} / {unit.maxHP}";
        hpText.color = (unit.currentHP < unit.maxHP * 0.3f) ? Color.red : Color.white;

        // 3. PA (Seulement si c'est le joueur)
        if (unit.isPlayerTeam)
        {
            apText.gameObject.SetActive(true);
            apText.text = $"PA: {unit.currentAP} / {unit.maxAP}";
        }
        else
        {
            // Pour l'ennemi, on cache les PA (info inutile pour le joueur)
            apText.gameObject.SetActive(false);
        }
    }

    // --- LOGIQUE DU MENU DES SORTS ---
    
    public void OpenSkillMenu()
    {
        actionMenu.SetActive(false); // On cache le menu principal
        skillMenu.SetActive(true);   // On affiche le menu des sorts

        // 1. Nettoyage de l'ancienne liste
        foreach (Transform child in skillListContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Création des boutons
        foreach (SkillData skill in currentUIUnit.data.skills)
        {
            GameObject btnObj = Instantiate(skillButtonPrefab, skillListContainer);
            
            // On change le texte du bouton (si tu as TMP dessus)
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if(btnText != null) btnText.text = $"{skill.skillName} ({skill.apCost} PA)";

            // On ajoute le clic
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => 
            {
                // Quand on clique sur le sort :
                currentUIUnit.EnterCombatMode(skill);
                // Le combat mode s'occupe de fermer les menus via UIManager
            });
        }
        
        // Ajoute un bouton "Retour" optionnel si tu veux, 
        // ou gère le clic droit pour fermer.
    }

    public void CloseAllMenus()
    {
        if(actionMenu != null) actionMenu.SetActive(false);
        if(skillMenu != null) skillMenu.SetActive(false);
    }

    // --- ANNONCES ---
    public void ShowAnnouncement(string text, Color color, float duration)
    {
        StartCoroutine(AnimateAnnouncement(text, color, duration));
    }

    IEnumerator AnimateAnnouncement(string text, Color color, float duration)
    {
        if(announcementPanel == null) yield break;
        announcementText.text = text;
        announcementText.color = color;
        announcementPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        announcementPanel.SetActive(false);
    }
    
    // Met à jour l'interface (PA/PV) si tu as un panel de stats
    public void UpdateUI(UnitController unit)
    {
        // Ici tu mettras à jour tes barres de vie plus tard
    }
}