using UnityEngine;
using System.Collections.Generic;

public class Pathfinding
{
    // Fonction qui renvoie la liste des cases atteignables et le chemin pour y aller
    public static Dictionary<TileData, TileData> GetReachableTiles(TileData startTile, int currentAP)
    {
        Dictionary<TileData, TileData> cameFrom = new Dictionary<TileData, TileData>();
        Dictionary<TileData, int> costSoFar = new Dictionary<TileData, int>();
        Queue<TileData> frontier = new Queue<TileData>();

        frontier.Enqueue(startTile);
        cameFrom[startTile] = null; // Le point de départ n'a pas de parent
        costSoFar[startTile] = 0;

        while (frontier.Count > 0)
        {
            TileData current = frontier.Dequeue();

            foreach (TileData neighbor in GetNeighbors(current))
            {
                // La hauteur (max 1 de différence)
                if (Mathf.Abs(neighbor.height - current.height) > 1) continue;

                // Est-ce marchable ?
                if (!neighbor.isWalkable) continue;

                // Est-ce occupé par une autre unité ?
                if (neighbor.currentUnit != null && neighbor != startTile) continue;

                // Calcul du coût (Coût actuel + coût de la case voisine)
                int newCost = costSoFar[current] + neighbor.movementCost;

                // Si on a assez de PA et que c'est le meilleur chemin trouvé
                if (newCost <= currentAP)
                {
                    if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                    {
                        costSoFar[neighbor] = newCost;
                        frontier.Enqueue(neighbor);
                        cameFrom[neighbor] = current; // On note qu'on vient de 'current' pour aller à 'neighbor'
                    }
                }
            }
        }

        return cameFrom;
    }

    // Fonction pour récupérer le chemin précis (Liste de cases à parcourir)
    public static List<TileData> GetPath(TileData endTile, Dictionary<TileData, TileData> cameFrom)
    {
        List<TileData> path = new List<TileData>();
        TileData current = endTile;

        while (current != null)
        {
            path.Add(current);
            cameFrom.TryGetValue(current, out current);
        }
        path.Reverse(); // On remet dans le bon ordre (Départ -> Arrivée)
        return path;
    }

    private static List<TileData> GetNeighbors(TileData tile)
    {
        List<TileData> neighbors = new List<TileData>();
        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (var dir in dirs)
        {
            Vector2 nextPos = tile.gridPosition + dir;
            if (MapGenerator.mapGrid.ContainsKey(nextPos))
            {
                neighbors.Add(MapGenerator.mapGrid[nextPos]);
            }
        }
        return neighbors;
    }
}