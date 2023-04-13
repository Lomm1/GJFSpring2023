using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

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
    [SerializeField] private MapObject objectIndicator;
    [SerializeField] private MapObject mapObjectPrefab;
    [SerializeField] private TextMeshProUGUI textTimer;
    [SerializeField] private TextMeshProUGUI textYear;
    [SerializeField] private TextMeshProUGUI textEnergy;
    [SerializeField] private GameObject water;
    [SerializeField] private Mesh meshCube;
    [SerializeField] private Material materialBuildModeAdd;
    [SerializeField] private Material materialBuildModeRemove;
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
    private Ray inputRay;
    private MapObject hitMapObject;
    private BuildMode buildMode;

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
        SetCurrentTileTypeIndex((int)TileType.Ground);
        SetBuildMode(BuildMode.Add);
    }

    private void Update()
    {
        if (gameState != GameState.Active)
            return;

        if (Input.GetKeyDown(KeyCode.P))
            SaveMap();

        if (Input.GetKeyDown(KeyCode.L))
            LoadMap();

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetBuildMode(BuildMode.Add);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetBuildMode(BuildMode.Remove);

        inputRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        hitMapObject = null;

        if (Physics.Raycast(inputRay, out var hit, Mathf.Infinity))
        {
            hitMapObject = hit.transform.GetComponentInParent<MapObject>();
            objectIndicator.gameObject.SetActive(hitMapObject != null);

            if (hitMapObject != null)
            {
                if (buildMode == BuildMode.Add)
                {
                    if (CanBuildOnTop(hitMapObject.xPos, hitMapObject.zPos, out var y))
                    {
                        objectIndicator.SetMaterial(materialBuildModeAdd);
                        objectIndicator.transform.position = new Vector3(hitMapObject.xPos, y, hitMapObject.zPos);
                    }
                    else
                    {
                        objectIndicator.SetMaterial(materialBuildModeRemove);
                        objectIndicator.transform.position = new Vector3(hitMapObject.xPos, hitMapObject.yPos, hitMapObject.zPos);
                    }
                }
                else if (buildMode == BuildMode.Remove)
                {
                    objectIndicator.transform.position = new Vector3(hitMapObject.xPos, hitMapObject.yPos, hitMapObject.zPos);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (buildMode == BuildMode.Add)
                    {
                        TryBuild(hitMapObject.xPos, hitMapObject.zPos);
                    }
                    else if (buildMode == BuildMode.Remove)
                    {
                        TryErase(hitMapObject.xPos, hitMapObject.zPos);
                    }
                }
            }
        }

        if (Input.mouseScrollDelta.y > 0)
        {
            SetCurrentTileTypeIndex(++currentTileTypeIndex);
        }

        if (Input.mouseScrollDelta.y < 0)
        {
            SetCurrentTileTypeIndex(--currentTileTypeIndex);
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

    private void SaveMap()
    {
        var stringBuilder = new StringBuilder();
        for (var x = 0; x < mapSizeX; ++x)
        {
            for (var y = 0; y < mapSizeY; ++y)
            {
                for (var z = 0; z < mapSizeZ; ++z)
                {
                    stringBuilder.Append((int)mapData[x, y, z]);
                }
            }
        }
        var textEditor = new TextEditor()
        {
            text = stringBuilder.ToString()
        };

        textEditor.SelectAll();
        textEditor.Copy();

        Debug.Log("MAP COPIED TO CLIPBOARD");
    }

    private void LoadMap()
    {
        var defaultMapData = new int[mapSizeX * mapSizeY * mapSizeZ];
        for (var i = 0; i < Constants.DEFAULT_MAP_DATA.Length; ++i)
        {
            var mapChar = Constants.DEFAULT_MAP_DATA[i];
            defaultMapData[i] = mapChar - '0';
        }

        SetAllMapData(defaultMapData);
        DrawAllMap();
    }

    private void SetBuildMode(BuildMode mode)
    {
        buildMode = mode;

        switch (buildMode)
        {
            case BuildMode.Add:
                {
                    objectIndicator.SetVisuals(objectMeshes[currentTileTypeIndex],
                        materialBuildModeAdd, objectOffsets[currentTileTypeIndex],
                        objectScales[currentTileTypeIndex], Quaternion.Euler(objectRotations[currentTileTypeIndex]));
                }
                break;
            case BuildMode.Remove:
                {
                    objectIndicator.SetVisuals(meshCube, materialBuildModeRemove, Vector3.zero, Vector3.one, Quaternion.identity);
                }
                break;
        }
    }

    private bool CanBuildOnTop(int x, int z, out int yIndex)
    {
        yIndex = 0;

        if (IsValidMapIndex(x, yIndex, z) == false)
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

        if (IsValidMapIndex(x, yIndex, z) == false)
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

    private bool TryBuild(int x, int z)
    {
        if (CanBuildOnTop(x, z, out var y) == false)
            return false;

        var buildCost = 0;
        switch (tileTypes[currentTileTypeIndex])
        {
            default: buildCost = 0; break;
            case TileType.Field: buildCost = 2; break;
            case TileType.House: buildCost = 3; break;
            case TileType.Forest:
            case TileType.Ground: buildCost = 1; break;
        }

        if (energy < buildCost)
        {
            return false;
        }

        SetMapTile(x, y, z, tileTypes[currentTileTypeIndex]);
        DrawMapTile(x, y, z);
        SetEnergy(energy - buildCost);

        return true;
    }

    private bool TryErase(int x, int z)
    {
        if (CanEraseTile(x, z, out var y) == false)
            return false;

        var eraseCost = 0;
        switch (mapData[x, y, z])
        {
            default: eraseCost = 0; break;
            case TileType.Field:
            case TileType.Forest: eraseCost = 1; break;
            case TileType.House:
            case TileType.Ground: eraseCost = 2; break;
        }

        if (energy < eraseCost)
        {
            return false;
        }

        SetMapTile(x, y, z, TileType.Empty);
        DrawMapTile(x, y, z);
        SetEnergy(energy - eraseCost);

        return true;
    }

    private void SetEnergy(int value)
    {
        energy = value;
        textEnergy.text = energy.ToString();
    }

    private bool IsValidMapIndex(int x, int y, int z)
    {
        if (x < 0 || x >= mapSizeX)
            return false;

        if (y < 0 || y >= mapSizeY)
            return false;

        if (z < 0 || z >= mapSizeZ)
            return false;

        return true;
    }

    private void SetCurrentTileTypeIndex(int index)
    {
        currentTileTypeIndex = index;

        if (currentTileTypeIndex >= tileTypes.Length)
            currentTileTypeIndex = 1; // 0 is empty, don't want to go there

        if (currentTileTypeIndex < 1)
            currentTileTypeIndex = tileTypes.Length - 1;

      //  imageCurrentTileType.sprite = tiles[currentTileTypeIndex].sprite;
        imageCurrentTileType.color = colorCurrentTileType[currentTileTypeIndex];

        if (buildMode == BuildMode.Add)
        {
            objectIndicator.SetVisuals(objectMeshes[currentTileTypeIndex],
                objectIndicator.meshRenderer.material, objectOffsets[currentTileTypeIndex],
                objectScales[currentTileTypeIndex], Quaternion.Euler(objectRotations[currentTileTypeIndex]));
        }
    }

    public void OnClickPlayButton() => SetGameState(GameState.Active);

    public void OnClickMenuButton() => SetGameState(GameState.Menu);

    public void OnClickSkipYearButton() => SetYear(currentYear + 1);

    private void SetYear(int value)
    {
        currentYear = value;
        textYear.text = $"YEAR {currentYear}/{maxYears}";
        timer = secondsInYear;
        SetEnergy(energyPerLevel);

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
        //SetAllMapData(TileType.Empty);
        //SetLayerMapData(TileType.Invisible, 0);
        //SetLayerMapData(TileType.Ground, 1);
        //DrawAllMap();
        LoadMap();
        waterLevel = 1;
        SetYear(1);
    }

    //private void SetAllMapData(TileType tileType)
    //{
    //    for (var x = 0; x < mapSizeX; ++x)
    //    {
    //        for (var y = 0; y < mapSizeY; ++y)
    //        {
    //            for (var z = 0; z < mapSizeZ; ++z)
    //            {
    //                SetMapTile(x, y, z, tileType);
    //            }
    //        }
    //    }
    //}

    //private void SetLayerMapData(TileType tileType, int y)
    //{
    //    for (var x = 0; x < mapSizeX; ++x)
    //    {
    //        for (var z = 0; z < mapSizeZ; ++z)
    //        {
    //            SetMapTile(x, y, z, tileType);
    //        }
    //    }
    //}

    private void SetAllMapData(int[] newData)
    {
        int index = 0;
        for (var x = 0; x < mapSizeX; ++x)
        {
            for (var y = 0; y < mapSizeY; ++y)
            {
                for (var z = 0; z < mapSizeZ; ++z)
                {
                    SetMapTile(x, y, z, (TileType)newData[index]);
                    ++index;
                }
            }
        }
    }

    private void SetMapTile(int x, int y, int z, TileType tileType)
    {
        if (IsValidMapIndex(x,y,z) == false)
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
