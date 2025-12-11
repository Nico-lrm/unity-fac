using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RangeFinder
{
  public List<OverlayTile> GetTilesInRange(
    OverlayTile start,
    int maxRange,
    ChessPieceType piece)
  {
    var results = new HashSet<OverlayTile>();

    if (piece == ChessPieceType.Rook ||
        piece == ChessPieceType.Bishop ||
        piece == ChessPieceType.Queen)
    {
      var dirs = piece switch
      {
        ChessPieceType.Rook   => ChessVectors.Rook,
        ChessPieceType.Bishop => ChessVectors.Bishop,
        _                     => ChessVectors.Queen
      };

      foreach (var dir in dirs)
        Raycast(start, dir, maxRange, results);

      return results.ToList();
    }

    if (piece == ChessPieceType.Knight)
      return MapManager.Instance.GetKnightMoves(start, ChessVectors.Knight);

    if (piece == ChessPieceType.King)
      return MapManager.Instance.GetKingMoves(start, ChessVectors.King);

    return results.ToList();
  }

  private void Raycast(
    OverlayTile start,
    Vector3Int dir,
    int maxRange,
    HashSet<OverlayTile> result)
  {
    var pos = start.GridLocation;

    for (int i = 0; i < maxRange; i++)
    {
      pos += dir;

      if (!MapManager.Instance.map.TryGetValue(pos, out var tile))
        break;

      if (tile.isBlocked)
        break;
      
      result.Add(tile);


    }
  }
}