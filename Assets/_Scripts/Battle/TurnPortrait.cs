using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnPortrait : MonoBehaviour
{
    [Header("UI Structure")]
    public Image portraitImage;   // Le perso
    public Image backgroundImage; // Le fond (Indique l'ÉTAT)
    public Image borderImage;     // La bordure (Indique l'ÉQUIPE) - NOUVEAU
    public TextMeshProUGUI nameText;

    [Header("Couleurs d'État (Fond)")]
    public Color readyColor = Color.white;             // Blanc (Prêt)
    public Color activeColor = new Color(1f, 0.8f, 0f); // Or/Jaune (En train de jouer)
    public Color doneColor = new Color(0.2f, 0.2f, 0.2f); // Noir/Gris (A fini)

    [Header("Couleurs d'Équipe (Bordure)")]
    public Color playerTeamColor = new Color(0f, 0.5f, 1f); // Bleu (Joueur)
    public Color enemyTeamColor = new Color(1f, 0.2f, 0.2f); // Rouge (Ennemi)

    private UnitController linkedUnit;

    public void Setup(UnitController unit, bool isActive, bool isDone)
    {
        linkedUnit = unit;

        // 1. Image & Nom
        if (unit.data.icon != null)
        {
            portraitImage.sprite = unit.data.icon;
            portraitImage.gameObject.SetActive(true);
        }
        else portraitImage.gameObject.SetActive(false);

        if (nameText != null) nameText.text = unit.unitName;

        // 2. Mise à jour des couleurs
        UpdateVisuals(unit, isActive, isDone);
    }

    void UpdateVisuals(UnitController unit, bool isActive, bool isDone)
    {
        // --- GESTION DE LA BORDURE (L'Équipe) ---
        if (borderImage != null)
        {
            if (unit.isPlayerTeam)
                borderImage.color = playerTeamColor; // Bleu
            else
                borderImage.color = enemyTeamColor;  // Rouge
        }

        // --- GESTION DU FOND (L'État) ---
        if (backgroundImage != null)
        {
            if (isActive)
            {
                backgroundImage.color = activeColor; // Or (C'est son tour)
            }
            else if (isDone)
            {
                backgroundImage.color = doneColor;   // Noir (A déjà joué)
            }
            else
            {
                backgroundImage.color = readyColor;  // Blanc (En attente)
            }
        }

        // Optionnel : Griser un peu le perso s'il a fini pour renforcer l'effet
        if (portraitImage != null)
        {
            portraitImage.color = isDone ? Color.gray : Color.white;
        }
    }

    // --- CLIC (Inchangé) ---
    public void OnClick()
    {
        if (linkedUnit == null) return;
        UnitController active = GameManager.Instance.activeUnit;

        if (active != null && active.isPlayerTeam && active.IsInTargetMode())
        {
            float dist = Vector3.Distance(active.transform.position, linkedUnit.transform.position);
            int distX = Mathf.Abs((int)active.gridPosition.x - (int)linkedUnit.gridPosition.x);
            int distY = Mathf.Abs((int)active.gridPosition.y - (int)linkedUnit.gridPosition.y);

            if ((distX + distY) >= active.GetMinRange() && (distX + distY) <= active.GetMaxRange())
            {
                active.PerformCombatAction(linkedUnit);
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowAnnouncement("Trop loin !", Color.yellow, 1f);
            }
        }
        else if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.isLockedOnUnit = false;
            CameraFollow.Instance.transform.position = linkedUnit.transform.position + CameraFollow.Instance.offset;
        }
    }
}