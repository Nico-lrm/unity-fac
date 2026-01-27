using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitButtonSlot : MonoBehaviour
{
    [Header("Text Data")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;

    [Header("Portrait Structure")]
    public Image portraitImage;   // L'image du perso (Enfant du Mask)
    public Image backgroundImage; // Le fond coloré (Indique la SÉLECTION)
    public Image borderImage;     // La bordure (Indique l'ÉQUIPE) - NOUVEAU

    [Header("Selection Logic")]
    public Button myButton;
    public GameObject selectedOverlay; // Cadre supplémentaire (Optionnel)

    [Header("Colors - State (Background)")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);   // Gris sombre (Pas sélectionné)
    public Color selectedColor = new Color(1f, 0.8f, 0f, 1f);     // Or/Jaune (Sélectionné)

    [Header("Colors - Team (Border)")]
    public Color playerTeamColor = new Color(0f, 0.5f, 1f, 1f);   // Bleu (Couleur du joueur)

    private UnitData myData;

    public void Setup(UnitData data, System.Action<UnitData> onClickCallback)
    {
        myData = data;

        // 1. Remplissage des Textes
        if (nameText != null) nameText.text = data.unitName;
        // if (costText != null) costText.text = data.deploymentCost.ToString(); 

        // 2. Remplissage de l'Image
        if (portraitImage != null && data.icon != null)
        {
            portraitImage.sprite = data.icon;
            portraitImage.gameObject.SetActive(true);
        }

        // 3. Couleur de la Bordure (Toujours bleu ici, car c'est NOTRE déploiement)
        if (borderImage != null)
        {
            borderImage.color = playerTeamColor;
        }

        // 4. Reset de l'état (Non sélectionné par défaut)
        SetSelected(false);

        // 5. Configuration du Bouton
        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(() => onClickCallback(myData));
    }

    public void SetSelected(bool isSelected)
    {
        // A. Changer la couleur du fond (Gris vs Or)
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }

        // B. Afficher/Cacher l'overlay supplémentaire (si tu en utilises un)
        if (selectedOverlay != null)
        {
            selectedOverlay.SetActive(isSelected);
        }
    }
}