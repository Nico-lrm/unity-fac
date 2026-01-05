using UnityEngine;

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
}