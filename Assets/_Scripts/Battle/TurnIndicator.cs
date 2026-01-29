using UnityEngine;

public class TurnIndicator : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Material mat;

    // Pour faire tourner le cercle doucement (effet sympa)
    public float rotateSpeed = 30f;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) mat = meshRenderer.material;
    }

    public void Setup(bool isPlayerTeam)
    {
        if (mat == null) return;

        // Bleu pour joueur, Rouge pour ennemi
        Color color = isPlayerTeam ? new Color(0, 0.5f, 1f, 1f) : new Color(1f, 0, 0, 1f);
        mat.SetColor("_BaseColor", color);
    }

    void Update()
    {
        // Rotation lente sur l'axe Z (car le Quad est couché)
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
}