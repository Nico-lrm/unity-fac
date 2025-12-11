using UnityEngine;

public static class ChessVectors
{
    public static readonly Vector3Int[] Rook =
    {
        new(1,0,0), new(-1,0,0), new(0,1,0), new(0,-1,0)
    };

    public static readonly Vector3Int[] Bishop =
    {
        new(1,1,0), new(1,-1,0), new(-1,1,0), new(-1,-1,0)
    };

    public static readonly Vector3Int[] Queen =
    {
        new(1,0,0), new(-1,0,0), new(0,1,0), new(0,-1,0),
        new(1,1,0), new(1,-1,0), new(-1,1,0), new(-1,-1,0)
    };

    public static readonly Vector3Int[] King = Queen;

    public static readonly Vector3Int[] Knight =
    {
        new(1,2,0), new(2,1,0), new(-1,2,0), new(-2,1,0),
        new(1,-2,0), new(2,-1,0), new(-1,-2,0), new(-2,-1,0)
    };
}

