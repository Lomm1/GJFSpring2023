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
    public int houses;
    public int houseGoal;
    public int farms;
    public int farmGoal;

    [SerializeField] private SimpleCameraController cameraController;
    [SerializeField] private Transform transformMapObjectsParent;
    [SerializeField] private GameObject uiContentMenu;
    [SerializeField] private GameObject uiContentGameActive;
    [SerializeField] private GameObject uiContentGameOver;
    [SerializeField] private GameObject uiInfoPanel;
    [SerializeField] private MapObject objectIndicator;
    [SerializeField] private MapObject mapObjectPrefab;
    [SerializeField] private TextMeshProUGUI textTimer;
    [SerializeField] private TextMeshProUGUI textYear;
    [SerializeField] private TextMeshProUGUI textEnergy;
    [SerializeField] private TextMeshProUGUI textHouses;
    [SerializeField] private TextMeshProUGUI textFarms;
    [SerializeField] private GameObject water;
    [SerializeField] private Mesh meshCube;
    [SerializeField] private Material materialBuildModeAdd;
    [SerializeField] private Material materialBuildModeRemove;
    [SerializeField] private Image imageCurrentTileType;

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
        MapObjectSettings.Initialize();

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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
            return;
        }

        if (gameState != GameState.Active)
            return;
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
            SaveMap();

        if (Input.GetKeyDown(KeyCode.L))
            LoadMap();
#endif
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
                    if (CanBuildOnTop(hitMapObject.xPos, hitMapObject.zPos, (TileType)currentTileTypeIndex, out var y))
                    {
                        objectIndicator.SetMaterial(materialBuildModeAdd);
                    }
                    else
                    {
                        objectIndicator.SetMaterial(materialBuildModeRemove);
                    }
                    objectIndicator.transform.position = new Vector3(hitMapObject.xPos, y, hitMapObject.zPos);
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
                        TryDemolish(hitMapObject.xPos, hitMapObject.zPos);
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
            textTimer.color = remainingSeconds <= 5 ? Color.red : Color.white;
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

        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[currentTileTypeIndex];

        switch (buildMode)
        {
            case BuildMode.Add:
                {
                    objectIndicator.SetVisuals(objectDef.mesh, materialBuildModeAdd, objectDef.offset, objectDef.scale, Quaternion.Euler(objectDef.rotation));
                }
                break;
            case BuildMode.Remove:
                {
                    objectIndicator.SetVisuals(meshCube, materialBuildModeRemove, Vector3.zero, Vector3.one, Quaternion.identity);
                }
                break;
        }
    }

    private bool CanBuildOnTop(int x, int z, TileType selectedTile, out int yIndex)
    {
        yIndex = 0;

        if (IsValidMapIndex(x, yIndex, z) == false)
            return false;

        var prevTileType = TileType.Empty;

        for (var y = 0; y < mapSizeY; ++y)
        {
            switch(mapData[x,y,z])
            {
                default: prevTileType = mapData[x, y, z]; continue;
                case TileType.Empty:
                    {
                        if (selectedTile != TileType.Ground && waterLevel >= y || y < waterLevel)
                        {
                            yIndex = y;
                            return false;
                        }

                        if ((prevTileType == TileType.Ground || prevTileType == TileType.Empty) ||
                            (prevTileType == TileType.Invisible && selectedTile == TileType.Ground))
                        {
                            if (y > 0 && mapObjects[x, y - 1, z].State != MapObjectState.Complete)
                            {
                                yIndex = y - 1;
                                return false;
                            }

                            yIndex = y;
                            return true;
                        }
                    } break;
                case TileType.Forest:
                case TileType.House:
                case TileType.Farm:
                case TileType.Stone:
                    {
                        yIndex = y;
                        return false;
                    }
            }
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
                default:
                    {
                        if (mapData[x, y, z] != TileType.Ground && waterLevel >= y || y < waterLevel)
                        {
                            yIndex = y;
                            return false;
                        }

                        if (y > 0 && mapObjects[x, y, z].State != MapObjectState.Complete)
                        {
                            yIndex = y - 1;
                            return false;
                        }

                        yIndex = y;
                        return true;
                    }
            }
        }

        return true;
    }

    private bool TryBuild(int x, int z)
    {
        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[currentTileTypeIndex];
        if (CanBuildOnTop(x, z, objectDef.tileType, out var y) == false)
            return false;

        if (energy < objectDef.buildCost)
        {
            return false;
        }

        if (objectDef.buildTime == 0)
        {
            SetMapTile(x, y, z, objectDef.tileType, MapObjectState.Complete, 0);
        }
        else
        {
            SetMapTile(x, y, z, objectDef.tileType, MapObjectState.Building, objectDef.buildTime);
        }

        DrawMapTile(x, y, z);
        SetEnergy(energy - objectDef.buildCost);

        return true;
    }

    private bool TryDemolish(int x, int z)
    {
        if (CanEraseTile(x, z, out var y) == false)
            return false;

        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[currentTileTypeIndex];

        if (energy < objectDef.removeCost)
        {
            return false;
        }

        if (objectDef.removeTime == 0)
        {
            SetMapTile(x, y, z, TileType.Empty, MapObjectState.Complete, 0);
        }
        else
        {
            SetMapTile(x, y, z, mapData[x, y, z], MapObjectState.Demolishing, objectDef.removeTime);
        }

        DrawMapTile(x, y, z);
        SetEnergy(energy - objectDef.removeCost);

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
        var definitionsLength = MapObjectSettings.Instance.mapObjectDefinitions.Length;

        currentTileTypeIndex = index;

        if (currentTileTypeIndex >= definitionsLength)
            currentTileTypeIndex = 1; // 0 is empty, don't want to go there

        if (currentTileTypeIndex < 1)
            currentTileTypeIndex = definitionsLength - 1;

        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[currentTileTypeIndex];

      //  imageCurrentTileType.sprite = tiles[currentTileTypeIndex].sprite;
        imageCurrentTileType.color = MapObjectSettings.Instance.mapObjectDefinitions[currentTileTypeIndex].iconColor;

        if (buildMode == BuildMode.Add)
        {
            objectIndicator.SetVisuals(objectDef.mesh, objectIndicator.meshRenderer.material, objectDef.offset, objectDef.scale, Quaternion.Euler(objectDef.rotation));
        }
    }

    public void OnClickPlayButton() => SetGameState(GameState.Active);

    public void OnClickMenuButton() => SetGameState(GameState.Menu);

    public void OnClickInfoButton() => uiInfoPanel.SetActive(!uiInfoPanel.activeSelf);

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

        cameraController.minY = waterLevel + 1f;

        CountScores();

        if (value >= maxYears)
        {
            SetGameState(GameState.GameOver);
            return;
        }

        for (var x = 0; x < mapSizeX; ++x)
        {
            for (var y = 0; y < mapSizeY; ++y)
            {
                for (var z = 0; z < mapSizeZ; ++z)
                {
                    switch (mapData[x, y, z])
                    {
                        case TileType.Empty: continue;
                        default:
                            {
                                if (mapObjects[x, y, z].State == MapObjectState.Complete)
                                {
                                    continue;
                                }

                                mapObjects[x, y, z].yearsRemaining--;

                                if (mapObjects[x, y, z].yearsRemaining <= 0)
                                {
                                    if (mapObjects[x, y, z].State == MapObjectState.Building)
                                    {
                                        mapObjects[x, y, z].SetState(MapObjectState.Complete, 0);
                                    }
                                    else if (mapObjects[x, y, z].State == MapObjectState.Demolishing)
                                    {
                                        SetMapTile(x, y, z, TileType.Empty, MapObjectState.Complete, 0);
                                    }
                                    DrawMapTile(x, y, z);
                                }
                            } break;
                    }
                }
            }
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
            case GameState.Active:
                {
                    uiInfoPanel.SetActive(false);
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
        CountScores();
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
                    SetMapTile(x, y, z, (TileType)newData[index], MapObjectState.Complete, 0);
                    ++index;
                }
            }
        }
    }

    private void SetMapTile(int x, int y, int z, TileType tileType, MapObjectState state, int yearDelay)
    {
        if (IsValidMapIndex(x,y,z) == false)
            return;

        mapData[x,y,z] = tileType;
        mapObjects[x, y, z].SetState(state, yearDelay);
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
            mapObjects[x, y, z].SetVisualsActive(false, false);
        }
        else
        {
            int rand = Random.Range(0, 4);
            var objectType = (int)mapData[x, y, z];
            var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[objectType];

            if (mapData[x, y, z] == TileType.Invisible)
            {
                mapObjects[x, y, z].SetVisualsActive(false, true);
            }
            else
            {
                mapObjects[x, y, z].SetVisualsActive(true);
            }

            switch (mapObjects[x, y, z].State)
            {
                case MapObjectState.Complete:
                    {
                        mapObjects[x, y, z].SetVisuals(objectDef.mesh, objectDef.material, objectDef.offset, objectDef.scale, Quaternion.Euler(objectDef.rotation));
                    }
                    break;
                case MapObjectState.Building:
                    {
                        mapObjects[x, y, z].SetVisuals(objectDef.mesh, materialBuildModeAdd, objectDef.offset, objectDef.scale, Quaternion.Euler(objectDef.rotation));
                    }
                    break;
                case MapObjectState.Demolishing:
                    {
                        mapObjects[x, y, z].SetVisuals(objectDef.mesh, materialBuildModeRemove, objectDef.offset, objectDef.scale, Quaternion.Euler(objectDef.rotation));
                    }
                    break;
            }
        }
    }

    private void CountScores()
    {
        houses = 0;
        farms = 0;

        for (var x = 0; x < mapSizeX; ++x)
        {
            for (var y = 0; y < mapSizeY; ++y)
            {
                for (var z = 0; z < mapSizeZ; ++z)
                {
                    if (y > waterLevel)
                    {
                        switch (mapData[x, y, z])
                        {
                            case TileType.House: ++houses; break;
                            case TileType.Farm: ++farms; break;
                        }
                    }
                }
            }
        }

        textHouses.text = $"{houses}/{houseGoal}";
        textFarms.text = $"{farms}/{farmGoal}";
    }
}
