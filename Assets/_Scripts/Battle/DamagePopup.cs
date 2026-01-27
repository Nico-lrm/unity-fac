using UnityEngine;
using TMPro; // Nécessaire pour TextMeshPro

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh; // On utilise TextMeshPro 3D (pas UI)
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;

    // Réglages
    private const float DISAPPEAR_TIMER_MAX = 1f;
    private float scaleSize = 1f; // Taille de base

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        // Si tu utilises le composant UI (TextMeshProUGUI), change le type ci-dessus
        if (textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();
    }

    public void Setup(int damageAmount, bool isHeal)
    {
        if (textMesh == null) return;

        textMesh.text = damageAmount.ToString();

        if (isHeal)
        {
            // Vert pour le soin
            textMesh.fontSize = 6;
            textColor = Color.green;
        }
        else
        {
            // Rouge / Jaune pour les dégâts
            textMesh.fontSize = 8;
            textColor = new Color(1f, 0.2f, 0.2f); // Rouge vif
        }

        textMesh.color = textColor;
        disappearTimer = DISAPPEAR_TIMER_MAX;

        // Mouvement vers le haut (et un peu aléatoire sur les côtés pour le style)
        moveVector = new Vector3(Random.Range(-0.5f, 0.5f), 2f, 0) * 2f;
    }

    void Update()
    {
        // 1. MOUVEMENT (Monter)
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 2f * Time.deltaTime; // Ralentir doucement (friction)

        if (disappearTimer > DISAPPEAR_TIMER_MAX * 0.5f)
        {
            // Effet de "Pop" (Grossit au début)
            float increaseScaleAmount = 1f;
            transform.localScale += Vector3.one * increaseScaleAmount * Time.deltaTime;
        }
        else
        {
            // Effet de "Shrink" (Rétrécit à la fin)
            float decreaseScaleAmount = 1f;
            transform.localScale -= Vector3.one * decreaseScaleAmount * Time.deltaTime;
        }

        // 2. DISPARITION (Fade Out)
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // On commence à devenir transparent
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0) Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        // 3. BILLBOARDING (Regarder la caméra)
        if (Camera.main != null)
        {
            // Le texte se tourne pour avoir la même rotation que la caméra
            transform.rotation = Camera.main.transform.rotation;

            // OPTIONNEL : Si tu veux que le texte garde la même taille à l'écran même de loin
            // Décommente les deux lignes ci-dessous :
            // float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            // transform.localScale = Vector3.one * (distance * 0.1f); // Ajuste 0.1f selon tes goûts
        }
    }
}