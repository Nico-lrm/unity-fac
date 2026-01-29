using UnityEngine;
using System.Collections.Generic;

public class TurnBarUI : MonoBehaviour
{
    public static TurnBarUI Instance;
    
    public Transform barContainer; 
    public GameObject portraitPrefab; 

    void Awake() { Instance = this; }

    public void UpdateTurnBar()
    {
        // 1. Nettoyer l'affichage précédent
        foreach (Transform child in barContainer) Destroy(child.gameObject);

        // 2. Récupérer TOUS les vivants (pas que la queue)
        List<UnitController> allLiving = GameManager.Instance.GetAllLivingUnitsSorted();

        // 3. Créer les portraits avec le bon état
        foreach (UnitController unit in allLiving)
        {
            GameObject obj = Instantiate(portraitPrefab, barContainer);
            
            bool isActive = (unit == GameManager.Instance.activeUnit);

            bool isWaiting = GameManager.Instance.IsUnitInQueue(unit);
            bool isDone = !isActive && !isWaiting;

            obj.GetComponent<TurnPortrait>().Setup(unit, isActive, isDone);
        }
    }
}