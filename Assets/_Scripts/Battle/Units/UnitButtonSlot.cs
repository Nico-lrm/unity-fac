using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitButtonSlot : MonoBehaviour
{
    public Image iconImage; // L'image du perso 
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Button myButton;
    public GameObject selectedOverlay;

    private UnitData myData;

    public void Setup(UnitData data, System.Action<UnitData> onClickCallback)
    {
        myData = data;
        nameText.text = data.unitName;
        // costText.text = data.deploymentCost.ToString(); afficher le coût

        if (data.icon != null && iconImage != null)
            iconImage.sprite = data.icon;

        if (selectedOverlay != null)
            selectedOverlay.SetActive(false);
        
        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(() => onClickCallback(myData));
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedOverlay != null) selectedOverlay.SetActive(isSelected);
    }
}