using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [Header("Réglages")]
    public float rotationSpeed = 5f; // Vitesse de rotation
    public Vector3 rotationAxis = Vector3.up; // Axe (Tourne sur elle-même)

    void Update()
    {
        // On fait tourner l'objet un petit peu à chaque frame
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}