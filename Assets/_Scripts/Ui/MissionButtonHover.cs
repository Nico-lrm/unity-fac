using UnityEngine;
using UnityEngine.EventSystems;
using TMPro; // Si tu veux gérer le texte ici aussi

public class MissionButtonHover : MonoBehaviour, IPointerEnterHandler
{
    // Variable privée, remplie par le code
    private MapDefinition myMapData;

    // Fonction d'initialisation (appelée par MainMenuController)
    public void Setup(MapDefinition data)
    {
        myMapData = data;
    }

    // Quand la souris passe dessus
    public void OnPointerEnter(PointerEventData eventData)
    {
        // On vérifie qu'on a bien les données et que le PreviewManager existe
        if (MapPreviewManager.Instance != null && myMapData != null)
        {
            MapPreviewManager.Instance.ShowMapPreview(myMapData);
        }
    }
}