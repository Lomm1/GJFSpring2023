using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GameController2D : MonoBehaviour
{
    public GameState gameState;
    public Camera mainCamera;
    public RectTransform rectTransformCanvas;

    public int mapSizeX;
    public int mapSizeY;
    public int mapSizeZ;
    public Tilemap tileMap;
    public TileType[,,] mapData;

    [SerializeField] private GameObject uiContentMenu;
    [SerializeField] private GameObject uiContentGame;
    [SerializeField] private Image imageCurrentTileType;
    [SerializeField] private TileType[] tileTypes;
    [SerializeField] private Tile[] tiles;
    [SerializeField] private Tile[] groundTiles;

    private int currentTileTypeIndex;
    private Vector2 mouseWorldPosition;

    private void Awake()
    {
        // Initilize map
        mapData = new TileType[mapSizeX, mapSizeY, mapSizeZ];

        SetAllMapData(TileType.Empty);
        DrawAllMap();

        SetGameState(gameState);

        SetCurrentTileIndex((int)TileType.Ground);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            if (CanBuildOnTop((int)mouseWorldPosition.x, (int)mouseWorldPosition.y, out var zIndex))
            {
                var tileVector = new Vector3Int((int)mouseWorldPosition.x, (int)mouseWorldPosition.y, zIndex);
                SetMapTile(tileVector.x, tileVector.y, tileVector.z, tileTypes[currentTileTypeIndex]);
                DrawAllMap();
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            if (CanEraseTile((int)mouseWorldPosition.x, (int)mouseWorldPosition.y, out var zIndex))
            {
                var tileVector = new Vector3Int((int)mouseWorldPosition.x, (int)mouseWorldPosition.y, zIndex);
                SetMapTile(tileVector.x, tileVector.y, tileVector.z, TileType.Empty);
                DrawAllMap();
            }
        }

        if (Input.mouseScrollDelta.y > 0)
        {
            SetCurrentTileIndex(++currentTileTypeIndex);
        }

        if (Input.mouseScrollDelta.y < 0)
        {
            SetCurrentTileIndex(--currentTileTypeIndex);
        }
    }

    private bool CanBuildOnTop(int x, int y, out int zIndex)
    {
        zIndex = 0;

        if (IsValidIndex(x, y, 0) == false)
            return false;

        var prevTileType = TileType.Empty;

        for (var z = 0; z < mapSizeZ; ++z)
        {
            switch(mapData[x,y,z])
            {
                default: continue;
                case TileType.Empty:
                    {
                        if (prevTileType == TileType.Ground || prevTileType == TileType.Empty)
                        {
                            zIndex = z;
                            return true;
                        }
                    } break;
                case TileType.Forest:
                case TileType.Stone: return false;
            }
            prevTileType = mapData[x, y, z];
        }

        return false;
    }

    private bool CanEraseTile(int x, int y, out int zIndex)
    {
        zIndex = 0;

        if (IsValidIndex(x, y, 0) == false)
            return false;

        for (var z = mapSizeZ - 1; z > 0; --z)
        {
            switch (mapData[x, y, z])
            {
                case TileType.Empty: continue;
                default: zIndex = z; return true;
            }
        }

        return true;
    }

    private bool IsValidIndex(int x, int y, int z)
    {
        if (x < 1 || x >= mapSizeX - 1) // edges are reserved for water
            return false;

        if (y < 1 || y >= mapSizeY - 1)
            return false;

        if (z < 0 || z >= mapSizeZ)
            return false;

        return true;
    }

    private void SetCurrentTileIndex(int index)
    {
        currentTileTypeIndex = index;

        if (currentTileTypeIndex >= tileTypes.Length)
            currentTileTypeIndex = 1; // 0 is empty, don't want to go there

        if (currentTileTypeIndex < 1)
            currentTileTypeIndex = tileTypes.Length - 1;

        imageCurrentTileType.sprite = tiles[currentTileTypeIndex].sprite;
    }

    public void OnClickPlayButton() => SetGameState(GameState.Active);

    private void SetGameState(GameState state)
    {
        uiContentMenu.SetActive(state == GameState.Menu);
        uiContentGame.SetActive(state == GameState.Active);
    }

    private void SetAllMapData(TileType tileType)
    {
        for (var x = 0; x < mapSizeX; ++x)
        {
            for (var y = 0; y < mapSizeY; ++y)
            {
                for (var z = 0; z < mapSizeZ; ++z)
                {
                    SetMapTile(x, y, z, tileType);
                }
            }
        }
    }

    private void SetMapTile(int x, int y, int z, TileType tileType)
    {
        if (IsValidIndex(x,y,z) == false)
            return;

        mapData[x,y,z] = tileType;
    }

    private void DrawAllMap()
    {
        for (var x = 0; x < mapSizeX; ++x)
        {
            for (var y = 0; y < mapSizeY; ++y)
            {
                for (var z = 0; z < mapSizeZ; ++z)
                {
                    DrawMapTile(x, y, z);
                }
            }
        }
    }

    private void DrawMapTile(int x, int y, int z)
    {
        var tileVector = new Vector3Int(x, y, z);
        if (mapData[x, y, z] == TileType.Empty)
        {
            tileMap.SetTile(tileVector, null);
        }
        else
        {
            switch (mapData[x, y, z])
            {
                case TileType.Ground:
                    {
                        if (mapData[x - 1, y, z] != TileType.Ground &&
                            mapData[x, y + 1, z] != TileType.Ground &&
                            mapData[x + 1, y, z] == TileType.Ground &&
                            mapData[x, y - 1, z] == TileType.Ground)
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.TopLeftCorner]);

                        else if (
                            mapData[x - 1, y, z] == TileType.Ground &&
                            mapData[x, y + 1, z] != TileType.Ground &&
                            mapData[x + 1, y, z] == TileType.Ground &&
                            mapData[x, y - 1, z] == TileType.Ground)
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.TopEdge]);

                        else if (
                            mapData[x - 1, y, z] == TileType.Ground &&
                            mapData[x, y + 1, z] != TileType.Ground &&
                            mapData[x + 1, y, z] != TileType.Ground &&
                            mapData[x, y - 1, z] == TileType.Ground)
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.TopRightCorner]);

                        else if (
                            mapData[x - 1, y, z] != TileType.Ground &&
                            mapData[x, y + 1, z] == TileType.Ground &&
                            mapData[x + 1, y, z] == TileType.Ground &&
                            mapData[x, y - 1, z] == TileType.Ground)
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.LeftEdge]);

                        else if (
                            mapData[x - 1, y, z] == TileType.Ground &&
                            mapData[x, y + 1, z] == TileType.Ground &&
                            mapData[x + 1, y, z] != TileType.Ground &&
                            mapData[x, y - 1, z] == TileType.Ground)
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.RightEdge]);

                        else if (
                            mapData[x - 1, y, z] != TileType.Ground &&
                            mapData[x, y + 1, z] == TileType.Ground &&
                            mapData[x + 1, y, z] == TileType.Ground &&
                            mapData[x, y - 1, z] != TileType.Ground)
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.BottomLeftCorner]);

                        else if (
                            mapData[x - 1, y, z] == TileType.Ground &&
                            mapData[x, y + 1, z] == TileType.Ground &&
                            mapData[x + 1, y, z] == TileType.Ground &&
                            mapData[x, y - 1, z] != TileType.Ground)
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.BottomEdge]);

                        else if (
                            mapData[x - 1, y, z] == TileType.Ground &&
                            mapData[x, y + 1, z] == TileType.Ground &&
                            mapData[x + 1, y, z] != TileType.Ground &&
                            mapData[x, y - 1, z] != TileType.Ground)
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.BottomRightCorner]);
                        else
                            tileMap.SetTile(tileVector, groundTiles[(int)GroundTileType.Middle]);
                    } break;
                case TileType.Forest:
                case TileType.Stone: tileMap.SetTile(tileVector, tiles[(int)mapData[x, y, z]]); break;
            }
        }
    }
}
