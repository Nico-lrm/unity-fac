using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseController : MonoBehaviour
{
  public float speed;
  public GameObject characterPrefab;
  private List<CharacterInfos> _allCharacters = new List<CharacterInfos>();
  private CharacterInfos _selectedCharacter;
  private bool _isInMotion = false;
  public bool spawnMode = false;

  private PathFinder _pathFinder;
  private RangeFinder _rangeFinder;
  private List<OverlayTile> _path = new List<OverlayTile>();
  private List<OverlayTile> inRangeTiles = new List<OverlayTile>();

  private void Start()
  {
    _pathFinder = new PathFinder();
    _rangeFinder = new RangeFinder();
  }

  void LateUpdate()
  {
    HandleCharacterSelection();

    if (!_selectedCharacter &&  !spawnMode)
      return;

    var focusedTileHit = GetFocusedOnTile();
    if (!focusedTileHit.HasValue)
      return;

    GameObject overlayTile = focusedTileHit.Value.collider.gameObject;
    transform.position = overlayTile.transform.position;

    OverlayTile tile = overlayTile.GetComponent<OverlayTile>();
    if (tile && tile.Public_renderer )
    {
      TileMovement(tile);
    }
  }


  private void HandleCharacterSelection()
  {
    if (!Mouse.current.leftButton.wasPressedThisFrame || spawnMode)
      return;

    var focusedTileHit = GetFocusedOnTile();
    if (!focusedTileHit.HasValue)
      return;

    var tile = focusedTileHit.Value.collider.GetComponent<OverlayTile>();
    if (!tile)
      return;

    CharacterInfos clickedCharacter = GetCharacterOnTile(tile);

    if (clickedCharacter)
    {
      _selectedCharacter = clickedCharacter;
      GetInRangeTiles();
      _path.Clear();
    }
  }

  
  private CharacterInfos GetCharacterOnTile(OverlayTile tile)
  {
    foreach (var character in _allCharacters)
    {
      if (character.GetActiveTile() == tile)
        return character;
    }

    return null;
  }


  void Update()
  {
    if (Keyboard.current.sKey.wasPressedThisFrame)
    {
      spawnMode = !spawnMode;
    }
  }


  private void GetInRangeTiles()
  {
    foreach (var item in inRangeTiles)
    {
      item.HideTile();
    }

    inRangeTiles = _rangeFinder.GetTilesInRange(_selectedCharacter.GetActiveTile(), 4, _selectedCharacter.GetPieceType());

    foreach (var item in inRangeTiles)
    {
      item.ShowTile();
    }
  }

  private void MoveAlongPath()
  {
    var step = speed * Time.deltaTime;

    _selectedCharacter.SetWalking();

    Vector3 targetPos = _path[0].transform.position;

    Vector3 direction = (targetPos - _selectedCharacter.transform.position).normalized;
    if (direction != Vector3.zero)
    {
      Quaternion lookRotation = Quaternion.LookRotation(direction);
      _selectedCharacter.transform.rotation = Quaternion.Slerp(
        _selectedCharacter.transform.rotation,
        lookRotation,
        Time.deltaTime * 10f
      );
    }

    _selectedCharacter.transform.position = Vector3.MoveTowards(
      _selectedCharacter.transform.position,
      targetPos,
      step
    );

    if (Vector3.Distance(_selectedCharacter.transform.position, targetPos) < 0.1f)
    {
      PositionCharacterOnTile(_path[0]);
      _path[0].HideDot();
      _path.RemoveAt(0);
    }

    if (_path.Count == 0)
    {
      _selectedCharacter.SetIdle();
      GetInRangeTiles();
      _isInMotion = false;
    }
  }

  public void TileMovement(OverlayTile tile)
  {
    gameObject.GetComponent<Renderer>().sortingOrder = tile.Public_renderer.sortingOrder;

    if (inRangeTiles.Contains(tile) && !_isInMotion)
    {
      _path = _pathFinder.FindPath(_selectedCharacter.GetActiveTile(), tile, inRangeTiles,
        _selectedCharacter.GetPieceType());

      foreach (var t in inRangeTiles)
        t.HideDot();

      foreach (var p in _path)
      {
        p.ShowDot();
      }
    }
    else if (!_isInMotion)
    {
      foreach (var t in inRangeTiles)
        t.HideDot();
    }

    if (Mouse.current.leftButton.wasPressedThisFrame && spawnMode)
    {
      var character = Instantiate(characterPrefab).GetComponent<CharacterInfos>();

      _allCharacters.Add(character);

      _selectedCharacter = character;

      PositionCharacterOnTile(tile);
      GetInRangeTiles();

      spawnMode = false;
      return;
    }

    if (Mouse.current.leftButton.wasPressedThisFrame && (!_selectedCharacter || inRangeTiles.Contains(tile)))
    {
      _isInMotion = true;
    }

    if (_path.Count > 0 && _isInMotion)
    {
      MoveAlongPath();
    }
    else
    {
      _isInMotion = false;
    }
  }

  public RaycastHit? GetFocusedOnTile()
  {
    Vector3 screenPosition = Mouse.current.position.ReadValue();

    Ray ray = Camera.main.ScreenPointToRay(screenPosition);

    if (Physics.Raycast(ray, out RaycastHit hitData))
    {
      return hitData;
    }

    return null;
  }
  
  private void PositionCharacterOnTile(OverlayTile tile)
  {
    Collider col = tile.GetComponent<Collider>();
    Vector3 tileCenter = col.bounds.center;

    _selectedCharacter.transform.position = tileCenter;

    _selectedCharacter.GetComponent<Renderer>().sortingOrder = tile.Public_renderer.sortingOrder;
    _selectedCharacter.SetActiveTile(tile);
  }
}