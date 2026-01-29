using UnityEngine;

public class WeaponIK : MonoBehaviour
{
    protected Animator animator;

    [Header("Réglages")]
    public bool ikActive = true;
    public Transform leftHandObj; // Le "LeftHandGrip" que tu as créé sur l'arme
    public Transform rightHandObj; // Optionnel : pour la rotation/visée précise
    
    [Range(0, 1)]
    public float leftHandPositionWeight = 1.0f;
    [Range(0, 1)]
    public float leftHandRotationWeight = 1.0f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Cette fonction spéciale est appelée par Unity juste après l'animation
    void OnAnimatorIK()
    {
        if (animator)
        {
            if (ikActive && leftHandObj != null)
            {
                // On dit à la main gauche d'aller vers la cible
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandPositionWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandRotationWeight);
                
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandObj.rotation);
            }
            else
            {
                // Si l'IK est désactivé, on relâche la contrainte (poids à 0)
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            }
        }
    }
}