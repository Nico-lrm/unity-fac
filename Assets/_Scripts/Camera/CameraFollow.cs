using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance; // Singleton pour être appelé facilement

    [Header("Cible & Suivi")]
    public bool isLockedOnUnit = true; // Est-ce qu'on suit le perso ?
    public float smoothSpeed = 5f;

    [Header("Positionnement")]
    public Vector3 offset; // Ton offset actuel
    private Vector3 focusPosition; // Le point invisible que la caméra regarde (soit le perso, soit le sol)

    [Header("Contrôles Manuel")]
    public float moveSpeed = 15f;
    public float rotationSpeed = 90f; // Pour le Lerp de rotation
    
    [Header("Zoom")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 20f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Initialisation : on regarde le centre ou le premier perso
        if (GameManager.Instance != null && GameManager.Instance.activeUnit != null)
        {
            focusPosition = GameManager.Instance.activeUnit.transform.position;
        }
        else
        {
            focusPosition = transform.position - offset;
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // --- 1. ROTATION ---
        if (Input.GetKeyDown(KeyCode.Q)) RotateCamera(90);
        if (Input.GetKeyDown(KeyCode.E)) RotateCamera(-90);

        // --- 2. DÉPLACEMENT (CORRIGÉ) ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // On ajoute une "Deadzone" pour éviter que la manette drift si tu en as une
        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
        {
            isLockedOnUnit = false; 

            // --- CORRECTION MATHÉMATIQUE ---
            // Au lieu de prendre le forward de la caméra (qui peut bugger si on regarde en bas),
            // On prend le Forward du MONDE, et on le tourne selon l'angle Y de la caméra.
            // C'est infaillible.
            Vector3 camEuler = transform.rotation.eulerAngles;
            Quaternion flatRotation = Quaternion.Euler(0, camEuler.y, 0);

            Vector3 forward = flatRotation * Vector3.forward;
            Vector3 right = flatRotation * Vector3.right;

            Vector3 moveDir = (forward * v + right * h).normalized;
            focusPosition += moveDir * moveSpeed * Time.deltaTime;
        }

        // --- 3. ZOOM ---
        if (Input.mouseScrollDelta.y != 0)
        {
            isLockedOnUnit = false;
            float currentDist = offset.magnitude;
            float targetDist = Mathf.Clamp(currentDist - Input.mouseScrollDelta.y * zoomSpeed, minZoom, maxZoom);
            offset = offset.normalized * targetDist;
        }
    }

    // Ta fonction de rotation (gardée telle quelle, très bien)
    void RotateCamera(float angle)
    {
        offset = Quaternion.Euler(0, angle, 0) * offset;
    }

    // Fonction appelée par le GameManager pour forcer le retour sur le héros
    public void ResetCameraOnActiveUnit()
    {
        if (GameManager.Instance != null && GameManager.Instance.activeUnit != null)
        {
            isLockedOnUnit = true;
        }
    }

    void LateUpdate()
    {
        // 1. Déterminer le point à regarder
        // Si on est verrouillé, le point focus devient la position du héros
        if (isLockedOnUnit && GameManager.Instance != null && GameManager.Instance.activeUnit != null)
        {
            focusPosition = GameManager.Instance.activeUnit.transform.position;
        }

        // 2. Calcul de la position désirée de la CAMÉRA (Focus + Offset)
        Vector3 desiredPosition = focusPosition + offset;

        // 3. Application fluide
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 4. Rotation fluide vers le point focus
        Quaternion targetRotation = Quaternion.LookRotation(focusPosition - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}