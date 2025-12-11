using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class OverlayTile : MonoBehaviour
{
  private MeshRenderer _rend;
  public GameObject childObject;
  public GameObject blueDot;
  public Renderer Public_renderer => _rend;
  private MeshRenderer _blueDotRenderer;

  public int G;
  public int H;

  public int F
  {
    get
    {
      return G + H;
    }
  }

  public bool isBlocked;
  public OverlayTile previous;
  public Vector3Int GridLocation { get; set; }
  
  
  void Awake()
  {
    _rend = childObject.GetComponent<MeshRenderer>();
    _blueDotRenderer = blueDot.GetComponent<MeshRenderer>();
    HideTile();
    HideDot();
    _blueDotRenderer.sortingLayerID = _rend.sortingLayerID;
    _blueDotRenderer.sortingOrder = _rend.sortingOrder + 10;
  }

  public void ShowTile()
  {
    _rend.enabled = true;
  }

  public void HideTile()
  {
    _rend.enabled = false;
  }

  public void ShowDot()
  {
    _blueDotRenderer.enabled = true;
  }

  public void HideDot()
  {
    _blueDotRenderer.enabled = false;
  }
}