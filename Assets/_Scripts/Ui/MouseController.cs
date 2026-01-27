using UnityEngine;
using UnityEngine.EventSystems; // Important pour ne pas cliquer à travers les boutons UI

public class MouseController : MonoBehaviour
{
    public static MouseController Instance;

    [Header("Réglages")]
    public LayerMask unitLayer;   // Le Layer de tes Unités (ex: "Unit")
    public LayerMask groundLayer; // Le Layer de ton Sol (ex: "Ground")

    [Header("Visuel")]
    public GameObject cursorPrefab; // Un petit cadre qui montre la case survolée

    private GameObject cursorInstance;
    private Camera mainCam;

    void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
    }

    void Start()
    {
        // On fait apparaître le curseur 3D si on en a un
        if (cursorPrefab != null)
        {
            cursorInstance = Instantiate(cursorPrefab);
        }
    }

    void Update()
    {
        HandleMouseRaycast();
    }

    void HandleMouseRaycast()
    {
        // 1. BLOQUER SI ON EST SUR L'UI
        // Si la souris est sur un bouton (Attaquer, Fin de tour...), on ne fait rien dans le monde 3D
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 2. LANCER LE RAYON
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // On combine les layers (Unités + Sol) pour que le rayon touche tout
        LayerMask combinedMask = unitLayer | groundLayer;

        if (Physics.Raycast(ray, out hit, 1000f, combinedMask))
        {
            // --- GESTION DU CURSEUR 3D ---
            // On arrondit la position pour que le curseur "snap" sur la grille (1x1)
            int x = Mathf.RoundToInt(hit.point.x);
            int z = Mathf.RoundToInt(hit.point.z);

            if (cursorInstance != null)
            {
                cursorInstance.transform.position = new Vector3(x, 0.1f, z); // 0.1f pour être un peu au dessus du sol
            }

            // --- GESTION DU SURVOL (HOVER) POUR L'UI ---
            // Est-ce qu'on touche une unité ?
            UnitController hoveredUnit = hit.collider.GetComponent<UnitController>();

            // Si le collider est sur un enfant, on cherche dans les parents
            if (hoveredUnit == null) hoveredUnit = hit.collider.GetComponentInParent<UnitController>();

            // On envoie l'info à l'UIManager (C'est ça qui va faire marcher ton panneau cible !)
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTargetPanel(hoveredUnit);
            }

            // --- GESTION DU CLIC (SELECTION / ACTION) ---
            if (Input.GetMouseButtonDown(0)) // Clic Gauche
            {
                HandleLeftClick(hit, hoveredUnit);
            }
        }
        else
        {
            // Si on vise le ciel (rien du tout), on vide le panneau cible
            if (UIManager.Instance != null) UIManager.Instance.UpdateTargetPanel(null);
        }
    }

    void HandleLeftClick(RaycastHit hit, UnitController clickedUnit)
    {
        // Cas 1 : On a cliqué sur une UNITÉ
        if (clickedUnit != null)
        {
            // C'est à toi de voir comment tu gères ça, souvent on passe par le GameManager
            // Exemple : GameManager.Instance.OnUnitClicked(clickedUnit);
            Debug.Log("Unité cliquée : " + clickedUnit.unitName);
        }
        // Cas 2 : On a cliqué sur le SOL (pour se déplacer)
        else
        {
            Debug.Log("Sol cliqué en : " + hit.point);
            // Exemple : GameManager.Instance.OnGroundClicked(hit.point);
        }
    }
}