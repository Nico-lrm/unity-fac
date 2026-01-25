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
            
            // ÉTAT 1 : C'est le personnage qui joue MAINTENANT
            bool isActive = (unit == GameManager.Instance.activeUnit);

            // ÉTAT 2 : A-t-il fini son tour ?
            // Il a fini SI : Il n'est pas actif ET Il n'est plus dans la file d'attente
            bool isWaiting = GameManager.Instance.IsUnitInQueue(unit);
            bool isDone = !isActive && !isWaiting;

            // On configure le portrait
            obj.GetComponent<TurnPortrait>().Setup(unit, isActive, isDone);
        }
    }
}