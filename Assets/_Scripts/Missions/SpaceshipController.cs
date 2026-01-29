using UnityEngine;
using System.Collections;
using System;

public class SpaceshipController : MonoBehaviour
{
    public float speed = 10f;
    public Transform dropPoint; 
    
    private Vector3 startSkyPosition; 
    private Vector3 landPosition;

    void Start()
    {

        landPosition = transform.position; 
        startSkyPosition = landPosition + Vector3.up * 20f; 
        
        transform.position = startSkyPosition;
    }

    public IEnumerator PlayDropSequence(Action onComplete)
    {
        yield return MoveTo(landPosition);
        
        yield return new WaitForSeconds(1f);
        
        yield return MoveTo(startSkyPosition);

        onComplete?.Invoke();
    }

    public IEnumerator PlayPickupSequence(Action onComplete)
    {
        yield return MoveTo(landPosition);
        
        yield return new WaitForSeconds(1.0f);
        
        yield return MoveTo(startSkyPosition);

        onComplete?.Invoke();
    }

    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }
}