using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panel Stats")]
    public GameObject statsPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI statsText;

    [Header("Menu Actions")]
    public GameObject actionMenuPanel;
    public Button attackButton;
    public Button skillButton;
    public Button endTurnButton;

    [Header("Menu Skills")]
    public GameObject skillMenuPanel;
    public GameObject skillButtonPrefab; // Glisse le prefab ici
    public Transform skillListContainer; // C'est le panel "SkillMenu" lui-même

    [Header("Info Cible")]
    public GameObject targetInfoPanel;
    public TextMeshProUGUI targetInfoText;

    void Awake() { Instance = this; }

    void Start()
    {
        endTurnButton.onClick.AddListener(() => GameManager.Instance.EndTurn());
        CloseAllMenus();
    }

    public void UpdateUI(UnitController activeUnit)
    {
        statsPanel.SetActive(true);
        if (activeUnit.isPlayerTeam)
        {
            nameText.text = activeUnit.unitName;
            statsText.text = $"HP: <color=green>{activeUnit.currentHP}/{activeUnit.maxHP}</color>\nAP: <color=#00FFFF>{activeUnit.currentAP}/{activeUnit.maxAP}</color>";
        }
        else
        {
            nameText.text = $"{activeUnit.unitName}";
            statsText.text = $"HP: <color=red>{activeUnit.currentHP}/{activeUnit.maxHP}</color>";
            CloseAllMenus();
        }
    }

    public void ShowActionMenu(bool show, System.Action onAttack, System.Action onSkill)
    {
        skillMenuPanel.SetActive(false); // On ferme le sous-menu skill si on revient au principal
        actionMenuPanel.SetActive(show);
        
        if (show)
        {
            attackButton.onClick.RemoveAllListeners();
            skillButton.onClick.RemoveAllListeners();

            if(onAttack != null) attackButton.onClick.AddListener(() => onAttack());
            if(onSkill != null) skillButton.onClick.AddListener(() => onSkill());
        }
    }

    public void ShowSkillMenu(List<SkillData> skills, System.Action<SkillData> onSkillChosen)
    {
        actionMenuPanel.SetActive(false); // On cache le menu principal
        skillMenuPanel.SetActive(true);

        // Nettoyer les vieux boutons
        foreach (Transform child in skillListContainer) Destroy(child.gameObject);

        // Créer les nouveaux boutons
        foreach (var skill in skills)
        {
            GameObject btnObj = Instantiate(skillButtonPrefab, skillListContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = $"{skill.skillName} ({skill.apCost} AP)";
            
            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                skillMenuPanel.SetActive(false); // On ferme le menu après choix
                onSkillChosen(skill);
            });
        }
        
        // Bouton retour (Optionnel mais conseillé)
        GameObject backBtn = Instantiate(skillButtonPrefab, skillListContainer);
        backBtn.GetComponentInChildren<TextMeshProUGUI>().text = "RETOUR";
        backBtn.GetComponent<Button>().onClick.AddListener(() => {
            skillMenuPanel.SetActive(false);
            actionMenuPanel.SetActive(true);
        });
    }

    public void ShowTargetInfo(UnitController unit)
    {
        if (unit == null) { targetInfoPanel.SetActive(false); return; }
        targetInfoPanel.SetActive(true);
        string team = unit.isPlayerTeam ? "Allié" : "Ennemi";
        string color = unit.isPlayerTeam ? "green" : "red";
        targetInfoText.text = $"<b>{unit.unitName}</b> ({team})\nHP: <color={color}>{unit.currentHP}/{unit.maxHP}</color>\nDégâts: {unit.attackDamage}";
    }

    public void CloseAllMenus()
    {
        actionMenuPanel.SetActive(false);
        skillMenuPanel.SetActive(false);
        targetInfoPanel.SetActive(false);
    }
}