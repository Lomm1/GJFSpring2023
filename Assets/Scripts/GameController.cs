using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    public GameState gameState;
    public Camera mainCamera;
    public RectTransform rectTransformCanvas;

    public int mapSizeX;
    public int mapSizeY;
    public int mapSizeZ;
    public int secondsInYear;
    public int currentYear;
    public int maxYears;
    public int waterRiseYears;
    public int waterLevel;
    public int energy;
    public int energyPerLevel;

    [SerializeField] private Transform transformMapObjectsParent;
    [SerializeField] private GameObject uiContentMenu;
    [SerializeField] private GameObject uiContentGameActive;
    [SerializeField] private GameObject uiContentGameOver;
    [SerializeField] private MapObject mapObjectPrefab;
    [SerializeField] private TextMeshProUGUI textTimer;
    [SerializeField] private TextMeshProUGUI textYear;
    [SerializeField] private TextMeshProUGUI textEnergy;
    [SerializeField] private GameObject water;
    [SerializeField] private Image imageCurrentTileType;
    [SerializeField] private Color[] colorCurrentTileType;
    [SerializeField] private TileType[] tileTypes;
    [SerializeField] private Mesh[] objectMeshes;
    [SerializeField] private Material[] objectMaterials;
    [SerializeField] private Vector3[] objectOffsets;
    [SerializeField] private Vector3[] objectScales;
    [SerializeField] private Vector3[] objectRotations;

    private float timer;
    private int previousSecond;
    private int currentTileTypeIndex;
    private Vector2 mouseWorldPosition;

    private TileType[,,] mapData;
    private MapObject[,,] mapObjects;

    private void Awake()
    {
        // Initilize map
        mapData = new TileType[mapSizeX, mapSizeY, mapSizeZ];
        mapObjects = new MapObject[mapSizeX, mapSizeY, mapSizeZ];

        for (var x = 0; x < mapSizeX; ++x)
        {
            for (var y = 0; y < mapSizeY; ++y)
            {
                for (var z = 0; z < mapSizeZ; ++z)
                {
                    mapObjects[x, y, z] = Instantiate(mapObjectPrefab, transformMapObjectsParent);
                    mapObjects[x, y, z].Initialize(x, y, z);
                }
            }
        }

        ResetGame();

        SetGameState(gameState);
        SetCurrentTileIndex((int)TileType.Ground);
    }

    private void Update()
    {
        if (gameState != GameState.Active)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            var inputRay = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(inputRay, out var hit, Mathf.Infinity))
            {
                var mapObject = hit.transform.GetComponent<MapObject>();

                if (mapObject != null)
                {
                    if (CanBuildOnTop(mapObject.xPos, mapObject.zPos, out var yPos))
                    {
                        SetMapTile(mapObject.xPos, yPos, mapObject.zPos, tileTypes[currentTileTypeIndex]);
                        DrawMapTile(mapObject.xPos, yPos, mapObject.zPos);
                    }
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            var inputRay = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(inputRay, out var hit, Mathf.Infinity))
            {
                var mapObject = hit.transform.GetComponent<MapObject>();

                if (mapObject != null)
                {
                    if (CanEraseTile(mapObject.xPos, mapObject.zPos, out var yPos))
                    {
                        SetMapTile(mapObject.xPos, yPos, mapObject.zPos, TileType.Empty);
                        DrawMapTile(mapObject.xPos, yPos, mapObject.zPos);
                    }
                }
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

        timer -= Time.deltaTime;

        if (timer < 0)
        {
            SetYear(currentYear + 1);
        }

        int remainingSeconds = Mathf.CeilToInt(timer);

        if (remainingSeconds != previousSecond)
        {
            textTimer.text = $"{remainingSeconds / 60}:{remainingSeconds % 60:00}";
        }

        previousSecond = remainingSeconds;
    }

    private bool CanBuildOnTop(int x, int z, out int yIndex)
    {
        yIndex = 1;

        if (IsValidIndex(x, yIndex, z) == false)
            return false;

        var prevTileType = TileType.Empty;

        for (var y = 0; y < mapSizeY; ++y)
        {
            switch(mapData[x,y,z])
            {
                default: continue;
                case TileType.Empty:
                    {
                        if (prevTileType == TileType.Ground || prevTileType == TileType.Empty)
                        {
                            yIndex = y;
                            return true;
                        }
                    } break;
                case TileType.Forest:
                case TileType.House:
                case TileType.Field:
                case TileType.Stone: return false;
            }
            prevTileType = mapData[x, y, z];
        }

        return false;
    }

    private bool CanEraseTile(int x, int z, out int yIndex)
    {
        yIndex = 1;

        if (IsValidIndex(x, yIndex, z) == false)
            return false;

        for (var y = mapSizeY - 1; y > 0; --y)
        {
            switch (mapData[x, y, z])
            {
                case TileType.Empty: continue;
                default: yIndex = y; return true;
            }
        }

        return true;
    }

    private bool IsValidIndex(int x, int y, int z)
    {
        if (x < 0 || x >= mapSizeX)
            return false;

        if (y < 0 || y >= mapSizeY)
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

      //  imageCurrentTileType.sprite = tiles[currentTileTypeIndex].sprite;
        imageCurrentTileType.color = colorCurrentTileType[currentTileTypeIndex];
    }

    public void OnClickPlayButton() => SetGameState(GameState.Active);

    public void OnClickMenuButton() => SetGameState(GameState.Menu);

    public void OnClickSkipYearButton() => SetYear(currentYear + 1);

    private void SetYear(int value)
    {
        currentYear = value;
        textYear.text = $"YEAR {currentYear}/{maxYears}";
        timer = secondsInYear;
        energy = energyPerLevel;
        textEnergy.text = energy.ToString();

        if (currentYear % waterRiseYears == 0)
        {
            waterLevel += 1;
        }

        water.transform.position = new Vector3(water.transform.position.x, waterLevel, water.transform.position.z);

        if (value >= maxYears)
        {
            SetGameState(GameState.GameOver);
        }
    }

    private void SetGameState(GameState state)
    {
        gameState = state;

        switch (gameState)
        {
            case GameState.Menu:
                {
                    ResetGame();
                } break;
        }

        uiContentMenu.SetActive(gameState == GameState.Menu);
        uiContentGameActive.SetActive(gameState == GameState.Active);
        uiContentGameOver.SetActive(gameState == GameState.GameOver);
    }

    private void ResetGame()
    {
        SetAllMapData(TileType.Empty);
        SetLayerMapData(TileType.Sand, 0);
        DrawAllMap();
        waterLevel = 0;
        SetYear(1);
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


    private void SetLayerMapData(TileType tileType, int y)
    {
        for (var x = 0; x < mapSizeX; ++x)
        {
            for (var z = 0; z < mapSizeZ; ++z)
            {
                SetMapTile(x, y, z, tileType);
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
            mapObjects[x, y, z].SetVisualsActive(false);
        }
        else
        {
            int rand = Random.Range(0, 4);
            var objectType = (int)mapData[x, y, z];
            mapObjects[x, y, z].SetVisualsActive(true);
            mapObjects[x, y, z].SetVisuals(objectMeshes[objectType], objectMaterials[objectType], objectOffsets[objectType], objectScales[objectType], Quaternion.Euler(objectRotations[objectType]));
        }
    }
}
