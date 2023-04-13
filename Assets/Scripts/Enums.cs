[System.Serializable]
public enum GameState
{
    Menu,
    Active,
    GameOver
}

[System.Serializable]
public enum TileType
{
    Empty = 0,
    Ground,
    Forest,
    House,
    Stone,
    Farm,
    Invisible
}

[System.Serializable]
public enum BuildMode
{
    Add = 0,
    Remove = 1
}