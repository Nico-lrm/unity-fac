using UnityEngine;
using UnityEngine.AI;

public class CharacterInfos : MonoBehaviour
{
    public OverlayTile activeTile;
    public ChessPieceType _pieceType = ChessPieceType.Queen;

    private const string Idle = "Idle";
    private const string Walk = "Walk";
    private string _currentAnim = "Rook";

    private Animator _animator;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public ChessPieceType GetPieceType()
    {
        return _pieceType;
    }

    public void SetActiveTile(OverlayTile tile)
    {
        if (activeTile)
            activeTile.isBlocked = false;
        activeTile = tile;
        activeTile.isBlocked = true;
    }

    public OverlayTile GetActiveTile()
    {
        return activeTile;
    }

    public void SetIdle()
    {
        PlayAnim(Idle);
    }

    public void SetWalking()
    {
        PlayAnim(Walk);
    }

    void PlayAnim(string anim)
    {
        if (_currentAnim != anim)
        {
            _animator.Play(anim);
            _currentAnim = anim;
        }
    }
}