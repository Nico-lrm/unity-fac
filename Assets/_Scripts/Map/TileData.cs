using UnityEngine;

public class TileData : MonoBehaviour
{
    public Vector2 gridPosition; // X, Z
    public int height;           // Y
    public bool isWalkable = true;
    public int movementCost = 1; // 1 par défaut, 2 pour la montagne
    public UnitController currentUnit = null;
}