using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(int damageAmount, bool isHeal = false)
    {
        if (isHeal)
        {
            textMesh.text = "+" + damageAmount;
            textColor = Color.green;
        }
        else
        {
            textMesh.text = "-" + damageAmount;
            textColor = Color.red;
        }
        
        textMesh.color = textColor;
        disappearTimer = 1f; 
    }

    void Update()
    {
        transform.position += new Vector3(0, 2f, 0) * Time.deltaTime;

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            float fadeSpeed = 3f;
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}