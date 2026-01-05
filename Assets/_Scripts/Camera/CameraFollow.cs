using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Réglages")]
    public float smoothSpeed = 5f;
    public Vector3 offset; 
    public float rotationSpeed = 5f; // Vitesse de la rotation

    void Update()
    {
        // Gestion de la rotation avec les touches A et E
        if (Input.GetKeyDown(KeyCode.A))
        {
            RotateCamera(90);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            RotateCamera(-90);
        }
    }

    void RotateCamera(float angle)
    {
        // On fait tourner le vecteur offset autour de l'axe Y (Haut)
        offset = Quaternion.Euler(0, angle, 0) * offset;
    }

    void LateUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.activeUnit == null) 
            return;

        Transform target = GameManager.Instance.activeUnit.transform;

        // Position cible
        Vector3 desiredPosition = target.position + offset;
        
        // Mouvement fluide
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // La caméra regarde toujours la cible
        // Le Lerp permet aussi une rotation fluide du regard
        var targetRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}