using UnityEngine;

[System.Serializable]
public enum GameState
{
    Menu,
    Active
}

[System.Serializable]
public enum TileType
{
    Empty = 0,
    Ground,
    Forest,
    House,
    Stone
}

[System.Serializable]
public enum GroundTileType
{
    TopLeftCorner = 0,
    TopEdge,
    TopRightCorner,
    LeftEdge,
    Middle,
    RightEdge,
    BottomLeftCorner,
    BottomEdge,
    BottomRightCorner
}