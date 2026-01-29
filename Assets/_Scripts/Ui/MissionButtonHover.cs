using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MissionButtonHover : MonoBehaviour, IPointerEnterHandler
{
    private MapDefinition myMapData;
    private string myDescription;
    
    // Référence vers le texte de l'UI principale (qu'on va remplir via le controller)
    private TextMeshProUGUI descriptionLabelRef; 

    // fonction Setup
    public void Setup(MapDefinition mapData, string desc, TextMeshProUGUI descLabel)
    {
        myMapData = mapData;
        myDescription = desc;
        descriptionLabelRef = descLabel;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. Lancer l'Hologramme
        if (MapPreviewManager.Instance != null && myMapData != null)
        {
            MapPreviewManager.Instance.ShowMapPreview(myMapData);
        }

        // 2. Afficher la Description
        if (descriptionLabelRef != null)
        {
            descriptionLabelRef.text = myDescription;
        }
    }
}