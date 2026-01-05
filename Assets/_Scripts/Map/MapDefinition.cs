using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NouvelleCarte", menuName = "Tactical/Map Definition")]
public class MapDefinition : ScriptableObject
{
    [Header("Dessin de la Carte")]
    [Tooltip("Dessine ta map ici. 0=Eau, 1=Sable, 2=Herbe, 3=Roche, 4=Neige")]
    // L'utilisateur pourra remplir ça comme un tableau de texte dans l'inspecteur
    public string[] mapRows; 

    [Header("Zones de Spawn (Lignes Z)")]
    [Tooltip("Le joueur apparaît entre la ligne Z Min et Z Max")]
    public Vector2Int playerSpawnZoneY = new Vector2Int(0, 2); // Par défaut lignes du bas
    
    [Tooltip("Les ennemis (si spawn dynamique) apparaissent entre ces lignes")]
    public Vector2Int enemySpawnZoneY = new Vector2Int(7, 9); // Par défaut lignes du haut
}