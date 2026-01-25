using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnPortrait : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public GameObject activeFrame;
    public Button myButton; // Référence au bouton pour gérer l'interactivité

    private UnitController linkedUnit;

    public void Setup(UnitController unit, bool isActive, bool isDone)
    {
        linkedUnit = unit;
        
        // 1. IMAGE & NOM
        if (unit.data.icon != null) iconImage.sprite = unit.data.icon;
        
        // Gestion de la couleur (Blanc = Normal, Gris = A déjà joué)
        if (isDone)
        {
            iconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gris foncé
        }
        else
        {
            iconImage.color = Color.white;
        }

        if (nameText != null) nameText.text = unit.unitName;
        
        // 2. CADRE ACTIF (Seulement si c'est son tour actuel)
        if(activeFrame != null) activeFrame.SetActive(isActive);
    }

    public void OnClick()
    {
        if (linkedUnit == null) return;
        UnitController active = GameManager.Instance.activeUnit;

        // Si c'est mon tour et que je tape quelqu'un (même s'il a déjà joué !)
        if (active != null && active.isPlayerTeam && active.IsInTargetMode())
        {
            float dist = Vector3.Distance(active.transform.position, linkedUnit.transform.position);
            // Calcul distance Manhattan (Grille)
            int distX = Mathf.Abs((int)active.gridPosition.x - (int)linkedUnit.gridPosition.x);
            int distY = Mathf.Abs((int)active.gridPosition.y - (int)linkedUnit.gridPosition.y);
            int distInt = distX + distY;
            
            if (distInt >= active.GetMinRange() && distInt <= active.GetMaxRange())
            {
                active.PerformCombatAction(linkedUnit);
            }
            else
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowAnnouncement("Trop loin !", Color.yellow, 1f);
            }
        }
        // Sinon, je regarde juste la cible
        else
        {
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.isLockedOnUnit = false; 
                CameraFollow.Instance.transform.position = linkedUnit.transform.position + CameraFollow.Instance.offset;
            }
        }
    }
}