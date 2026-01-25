using UnityEngine;
using System.Collections.Generic;

// Définition des types de pièces
public enum ChessType { King, Queen, Rook, Bishop, Knight, Pawn }

[CreateAssetMenu(fileName = "NewUnit", menuName = "Tactical/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Identité")]
    public string unitName;
    public ChessType pieceType;
    public GameObject unitPrefab; // Le modèle 3D du personnage
    public Sprite icon;           // Pour l'UI

    [Header("Coûts de Déploiement")]
    public int deploymentCost = 1; // 1 par défaut

    [Header("Stats de Base")]
    public int maxHP = 20;
    public int attackDamage = 4;
    public int speed = 5;
    
    [Header("Combat & Portée")]
    public int minRange = 1;
    public int maxRange = 1;
    
    [Header("Compétences")]
    public List<SkillData> skills = new List<SkillData>();

	[Header("FX Attaque Base")]
    public AudioClip attackSound; // Bruit coup d'épée / Tir
    public GameObject hitVFX;     // Sang / Étincelles

	[Header("Timing Attaque")]
    public float attackAnimDelay = 0.3f;
}