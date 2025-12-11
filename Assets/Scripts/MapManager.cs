using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
  private static MapManager _instance;

  public static MapManager Instance
  {
    get { return _instance; }
  }

  public OverlayTile overlayTilePrefab;
  public GameObject overlayContainer;

  public Dictionary<Vector3Int, OverlayTile> map;

  public void Awake()
  {
    if (_instance && _instance != this)
    {
      Destroy(this.gameObject);
    }
    else
    {
      _instance = this;
    }
  }

  private void Start()
  {
    var tileMap = GetComponentInChildren<Tilemap>();
    map = new Dictionary<Vector3Int, OverlayTile>();

    int count = 0;
    foreach (Transform child in tileMap.transform)
    {
      count++;

      var overlay = Instantiate(overlayTilePrefab, overlayContainer.transform);

      overlay.transform.position = child.position + Vector3.up * 0.50f;

      var sr = overlay.GetComponent<SpriteRenderer>();
      if (sr)
      {
        sr.sortingOrder = 10;
      }
      Vector3Int cellPos = tileMap.WorldToCell(child.position);
      overlay.GridLocation = cellPos;
      map.Add(cellPos,overlay);
    }

    Debug.Log($"Overlay tiles created for {count} cubes.");
  }
  
  public List<OverlayTile> GetNeighbourTiles(OverlayTile currentTile, List<OverlayTile> searchableTiles)
  {
    Dictionary<Vector3Int, OverlayTile> tileToSearch = new Dictionary<Vector3Int, OverlayTile>();
    if (searchableTiles.Count > 0)
    {
      foreach (var tile in searchableTiles)
      {
        tileToSearch.Add(tile.GridLocation, tile);
      }
    }
    else
    {
      tileToSearch = map;
    }
    List<OverlayTile> neighbours = new List<OverlayTile>();

    // 8-Directional movement
    Vector3Int[] dirs =
    {
      new Vector3Int( 1,  0, 0),
      new Vector3Int(-1,  0, 0),
      new Vector3Int( 0,  1, 0),
      new Vector3Int( 0, -1, 0),

      new Vector3Int( 1,  1, 0),
      new Vector3Int( 1, -1, 0),
      new Vector3Int(-1,  1, 0),
      new Vector3Int(-1, -1, 0),
    };

    foreach (var d in dirs)
    {
      Vector3Int pos = new Vector3Int(
        currentTile.GridLocation.x + d.x,
        currentTile.GridLocation.y + d.y,
        currentTile.GridLocation.z
      );

      if (tileToSearch.ContainsKey(pos))
        neighbours.Add(tileToSearch[pos]);
    }

    return neighbours;
  }

  public List<OverlayTile> GetKnightMoves(
    OverlayTile currentTile,
    Vector3Int[] offsets)
  {
    List<OverlayTile> results = new();

    foreach (var d in offsets)
    {
      Vector3Int pos = currentTile.GridLocation + d;

      if (map.TryGetValue(pos, out var tile))
        results.Add(tile);
    }

    return results;
  }
  public List<OverlayTile> GetKingMoves(
    OverlayTile currentTile,
    Vector3Int[] offsets)
  {
    List<OverlayTile> results = new();

    foreach (var d in offsets)
    {
      Vector3Int pos = currentTile.GridLocation + d;

      if (map.TryGetValue(pos, out var tile))
        results.Add(tile);
    }

    return results;
  }

}