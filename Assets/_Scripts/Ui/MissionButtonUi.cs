using UnityEngine;
using TMPro;

public class MissionButtonUI : MonoBehaviour
{
    public TextMeshProUGUI numberText; // Pour "X-X"
    public TextMeshProUGUI titleText;  // Pour "Le Marécage"

    public void SetInfo(string id, string title)
    {
        if (numberText != null) numberText.text = id;
        if (titleText != null) titleText.text = title;
    }
}