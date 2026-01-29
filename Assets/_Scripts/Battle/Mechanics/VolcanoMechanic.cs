using UnityEngine;

[CreateAssetMenu(menuName = "Tactical/Mechanics/Volcano Logic")]
public class VolcanoMechanic : MissionMechanic
{
    [Header("Réglages")]
    public int lavaDamage = 1;
    public int teleportRange = 9;
    public int triggerDistance = 5; 

    public override void OnTurnStart(UnitController unit)
    {
        unit.TakeDamage(lavaDamage);
        
        if (UIManager.Instance != null)
            UIManager.Instance.AddToLog($"<color=orange>{unit.unitName}</color> brûle ({lavaDamage} Dégâts).");
    }

    public override bool OverrideEnemyAI(UnitController unit, UnitController target)
    {
        if (target == null) return false;

        int dist = Mathf.Abs((int)unit.gridPosition.x - (int)target.gridPosition.x) + 
                   Mathf.Abs((int)unit.gridPosition.y - (int)target.gridPosition.y);

        if (dist <= triggerDistance) return false;

        Debug.Log($"{unit.unitName} se déplace furtivement !");
        
        TileData bestTile = FindTeleportTile(unit, target);

        if (bestTile != null)
        {
            
            unit.ForceMoveTo(bestTile);
            
            if (UIManager.Instance != null)
                UIManager.Instance.AddToLog($"<color=red>{unit.unitName}</color> se téléporte !");

            return true; 
        }

        return false; 
    }

    TileData FindTeleportTile(UnitController me, UnitController target)
    {
        Vector2 dirToTarget = target.gridPosition - me.gridPosition;
        Vector2 dirSign = new Vector2(System.Math.Sign(dirToTarget.x), System.Math.Sign(dirToTarget.y));
        
        Vector2 backPos = target.gridPosition + dirSign;

        if (IsValidTile(backPos)) return MapGenerator.mapGrid[backPos];

        Vector2[] offsets = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 off in offsets)
        {
            Vector2 adjPos = target.gridPosition + off;
            if (IsValidTile(adjPos)) return MapGenerator.mapGrid[adjPos];
        }

        return null;
    }

    bool IsValidTile(Vector2 pos)
    {
        if (!MapGenerator.mapGrid.ContainsKey(pos)) return false;
        TileData t = MapGenerator.mapGrid[pos];
        return t.isWalkable && t.currentUnit == null;
    }
}