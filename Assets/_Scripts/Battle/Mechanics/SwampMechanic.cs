using UnityEngine;

[CreateAssetMenu(menuName = "Tactical/Mechanics/Swamp Push")]
public class SwampMechanic : MissionMechanic
{
    public override void OnAttackHit(UnitController attacker, UnitController defender)
    {
        // 1. Seulement si c'est un monstre qui tape
        if (attacker.isPlayerTeam) return; 

        Debug.Log("ACTIVATION MÉCANIQUE MARÉCAGE : POUSSÉE !");

        // 2. Calculer la direction de la poussée
        Vector2 dir = (defender.gridPosition - attacker.gridPosition);
        
        // On normalise pour avoir juste (1,0), (-1,0), (0,1) ou (0,-1)
        int pushX = 0;
        int pushY = 0;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) pushX = (int)Mathf.Sign(dir.x);
        else pushY = (int)Mathf.Sign(dir.y);

        // 3. Calculer la case d'arrivée
        Vector2 targetPos = defender.gridPosition + new Vector2(pushX, pushY);

        // 4. Vérifier si la case est valide (Pas de mur, pas d'autre unité)
        if (MapGenerator.mapGrid.ContainsKey(targetPos))
        {
            TileData tile = MapGenerator.mapGrid[targetPos];
            
            if (tile.isWalkable && tile.currentUnit == null)
            {
                // On applique la poussée !
                defender.ForceMoveTo(tile);
            }
        }
    }
}