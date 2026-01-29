using UnityEngine;
using UnityEngine.EventSystems; 

public class MouseController : MonoBehaviour
{
    public static MouseController Instance;

    [Header("R�glages")]
    public LayerMask unitLayer;  
    public LayerMask groundLayer; 

    [Header("Visuel")]
    public GameObject cursorPrefab; // Un petit cadre qui montre la case survol�e

    private GameObject cursorInstance;
    private Camera mainCam;

    void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
    }

    void Start()
    {
        // On fait appara�tre le curseur 3D si on en a un
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
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // 2. LANCER LE RAYON
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        LayerMask combinedMask = unitLayer | groundLayer;

        if (Physics.Raycast(ray, out hit, 1000f, combinedMask))
        {
            // --- GESTION DU CURSEUR 3D ---
            int x = Mathf.RoundToInt(hit.point.x);
            int z = Mathf.RoundToInt(hit.point.z);

            if (cursorInstance != null)
            {
                cursorInstance.transform.position = new Vector3(x, 0.1f, z); 
            }

            // --- GESTION DU SURVOL (HOVER) POUR L'UI ---
            UnitController hoveredUnit = hit.collider.GetComponent<UnitController>();

            // Si le collider est sur un enfant, on cherche dans les parents
            if (hoveredUnit == null) hoveredUnit = hit.collider.GetComponentInParent<UnitController>();

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
        if (clickedUnit != null)
        {
            Debug.Log("Unit� cliqu�e : " + clickedUnit.unitName);
        }
        else
        {
            Debug.Log("Sol cliqu� en : " + hit.point);
        }
    }
}