using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public TextMeshPro textMesh;
    public float disappearSpeed = 1f;
    public float moveYSpeed = 2f;
    private Color textColor;

    // Ajout du paramètre isHeal
    public void Setup(int amount, bool isHeal)
    {
        if (isHeal)
        {
            textMesh.text = "+" + amount.ToString();
            textMesh.fontSize = 6; // Un peu plus gros pour le feedback positif
            textColor = Color.green;
        }
        else
        {
            textMesh.text = "-" + amount.ToString();
            textColor = Color.red;
            
            // Bonus : Couleur critique si gros dégâts (> 10)
            if (amount > 10) 
            {
                textMesh.fontSize = 7;
                textColor = new Color(1f, 0.5f, 0f); // Orange critique
            }
        }
        
        textMesh.color = textColor;
    }

	void Update()
    {
        if (textMesh == null || gameObject == null) return;

        transform.position += Vector3.up * moveYSpeed * Time.deltaTime;
        
        textColor.a -= disappearSpeed * Time.deltaTime;
        textMesh.color = textColor;

        if (textColor.a < 0) 
        {
            Destroy(gameObject);
        }
    }
}