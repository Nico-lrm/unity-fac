using UnityEngine;
using System.Collections;

public class LaserEffect : MonoBehaviour
{
    public LineRenderer line;
    public float duration = 0.2f;

    public void Setup(Vector3 start, Vector3 end)
    {
        if (line == null) line = GetComponent<LineRenderer>();
        
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Color startColor = line.startColor;
        Color endColor = line.endColor;

        while (elapsed < duration)
        {
            // On réduit l'alpha (transparence) progressivement
            float alpha = 1f - (elapsed / duration);
            
            line.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            line.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject); // On détruit le laser une fois fini
    }
}