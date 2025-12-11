using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder
{
    public List<OverlayTile> FindPath(OverlayTile startTile, OverlayTile endTile, List<OverlayTile> searchableTiles, ChessPieceType pieceType)
    {
        foreach (var tile in MapManager.Instance.map.Values)
        {
            tile.G = 0;
            tile.H = 0;
            tile.previous = null;
        }

        List<OverlayTile> openList = new List<OverlayTile>();
        List<OverlayTile> closedList = new List<OverlayTile>();

        openList.Add(startTile);

        while (openList.Count > 0)
        {
            OverlayTile currentTile = openList.OrderBy(x => x.F).First();

            openList.Remove(currentTile);
            closedList.Add(currentTile);

            if (currentTile == endTile)
            {
                return BuildPath(startTile, endTile);
            }

            List<OverlayTile> neighbours;

            if (pieceType == ChessPieceType.Knight)
            {
                neighbours = MapManager.Instance.GetKnightMoves(currentTile, ChessVectors.Knight);
            }
            else
            {
                neighbours = MapManager.Instance.GetNeighbourTiles(currentTile, searchableTiles);
            }

            foreach (var neighbour in neighbours)
            {
                if (neighbour.isBlocked || closedList.Contains(neighbour))
                    continue;

                if (Mathf.Abs(neighbour.GridLocation.z - currentTile.GridLocation.z) > 1)
                    continue;

                int tentativeG = currentTile.G + 1;

                if (tentativeG < neighbour.G || !openList.Contains(neighbour))
                {
                    neighbour.G = tentativeG;
                    neighbour.H = GetManhattenDistance(neighbour, endTile);
                    neighbour.previous = currentTile;

                    if (!openList.Contains(neighbour))
                        openList.Add(neighbour);
                }
            }
        }

        return new List<OverlayTile>(); 
    }


    private List<OverlayTile> BuildPath(OverlayTile startTile, OverlayTile endTile)
    {
        List<OverlayTile> path = new List<OverlayTile>();
        OverlayTile current = endTile;

        while (current != startTile)
        {
            path.Add(current);
            current = current.previous;
        }

        path.Reverse();
        return path;
    }


    private int GetManhattenDistance(OverlayTile a, OverlayTile b)
    {
        return Mathf.Abs(a.GridLocation.x - b.GridLocation.x) +
               Mathf.Abs(a.GridLocation.y - b.GridLocation.y);
    }



}
