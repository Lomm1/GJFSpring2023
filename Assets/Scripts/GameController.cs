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
    public int maxYears;
    public int waterRiseYears;
    public int energyPerLevel;
    public int houseGoal;
    public int farmGoal;

    [SerializeField] private SimpleCameraController cameraController;
    [SerializeField] private Transform cameraStartTransform;
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
    [SerializeField] private TextMeshProUGUI textEnergyDelta;
    [SerializeField] private TextMeshProUGUI textWood;
    [SerializeField] private TextMeshProUGUI textWoodDelta;
    [SerializeField] private TextMeshProUGUI textHouses;
    [SerializeField] private TextMeshProUGUI textFarms;
    [SerializeField] private TextMeshProUGUI textHousesLost;
    [SerializeField] private TextMeshProUGUI textFarmsLost;
    [SerializeField] private TextMeshProUGUI textGameplayInfo;
    [SerializeField] private TextMeshProUGUI textWaterRises;
    [SerializeField] private Slider sliderTimeInYear;
    [SerializeField] private Slider sliderMaxYears;
    [SerializeField] private GameObject water;
    [SerializeField] private Mesh meshCube;
    [SerializeField] private Material materialBuildModeAdd;
    [SerializeField] private Material materialBuildModeRemove;
    [SerializeField] private ParticleSystem particleSystemBuildingComplete;
    [SerializeField] private GameObject hotkeyButtonsPanel;
    [SerializeField] private GameObject[] hotkeyButtons;

    private int currentYear;
    private int waterLevel;
    private int farms;
    private int energy;
    private int wood;
    private int houses;
    private bool infiniteTime;
    private float timer;
    private int previousSecond;
    private int currentTileTypeIndex;
    private int hotkeyIndex;
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
        SetBuildMode(BuildMode.Add);
        OnMaxYearSliderChanged();
        OnTimeSliderChanged();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
            return;
        }

        if (textGameplayInfo.alpha > 0)
            textGameplayInfo.alpha -= Time.deltaTime;

        if (textEnergyDelta.alpha > 0)
            textEnergyDelta.alpha -= Time.deltaTime;

        if (textWoodDelta.alpha > 0)
            textWoodDelta.alpha -= Time.deltaTime;

        if (textWaterRises.alpha > 0)
            textWaterRises.alpha -= Time.deltaTime;

        if (gameState != GameState.Active)
            return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.P))
            SaveMapToClipboard();

        if (Input.GetKeyDown(KeyCode.L))
            LoadMap();
#endif

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetCurrentTileTypeIndex(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetCurrentTileTypeIndex(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetCurrentTileTypeIndex(2);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            SetCurrentTileTypeIndex(3);

        inputRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        hitMapObject = null;

        if (Input.mousePosition.y > 128 && Input.mousePosition.y < Screen.height - 96 && Physics.Raycast(inputRay, out var hit, Mathf.Infinity))
        {
            hitMapObject = hit.transform.GetComponentInParent<MapObject>();
            objectIndicator.gameObject.SetActive(hitMapObject != null);

            if (hitMapObject != null)
            {
                var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[(int)hitMapObject.TileType];
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
                    objectIndicator.transform.localScale = Vector3.one;
                }
                else if (buildMode == BuildMode.Remove)
                {
                    objectIndicator.transform.localScale = Vector3.one * objectDef.gridSize;
                    objectIndicator.transform.position = new Vector3(hitMapObject.xPos + objectDef.offset.x, hitMapObject.yPos, hitMapObject.zPos + objectDef.offset.z);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (buildMode == BuildMode.Add)
                    {
                        TryBuild(hitMapObject.xPos, hitMapObject.zPos);
                    }
                    else if (buildMode == BuildMode.Remove)
                    {
                        TryDemolish(hitMapObject.xPos, hitMapObject.zPos, objectDef.tileType);
                    }
                }
            }
        }

        if (Input.mouseScrollDelta.y > 0)
        {
            SetCurrentTileTypeIndex(++hotkeyIndex);
        }

        if (Input.mouseScrollDelta.y < 0)
        {
            SetCurrentTileTypeIndex(--hotkeyIndex);
        }

        if (infiniteTime == false)
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

    public void OnClickPlayButton() => SetGameState(GameState.Active);

    public void OnClickMenuButton() => SetGameState(GameState.Menu);

    public void OnClickInfoButton() => uiInfoPanel.SetActive(!uiInfoPanel.activeSelf);

    public void OnClickSkipYearButton() => SetYear(currentYear + 1);

    public void SetCurrentTileTypeIndex(int index)
    {
        hotkeyIndex = index;

        if (hotkeyIndex > 3)
            hotkeyIndex = 0;
        
        if (hotkeyIndex < 0)
            hotkeyIndex = 3;

        switch (hotkeyIndex)
        {
            case 0: currentTileTypeIndex = 1; break;
            case 1: currentTileTypeIndex = 3; break;
            case 2: currentTileTypeIndex = 5; break;
        }

        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[currentTileTypeIndex];

        if (hotkeyIndex == 3)
        {
            SetBuildMode(BuildMode.Remove);
        }
        else
        {
            SetBuildMode(BuildMode.Add);
        }

        for (var i = 0; i < hotkeyButtons.Length; ++i)
            hotkeyButtons[i].transform.localPosition = new Vector3(hotkeyButtons[i].transform.localPosition.x, 0, hotkeyButtons[i].transform.localPosition.z);

        hotkeyButtons[hotkeyIndex].transform.localPosition = new Vector3(hotkeyButtons[hotkeyIndex].transform.localPosition.x, 24, hotkeyButtons[hotkeyIndex].transform.localPosition.z);

        if (buildMode == BuildMode.Add)
        {
            objectIndicator.SetVisuals(objectDef.mesh, objectIndicator.meshRenderer.material, objectDef.offset, objectDef.scale, Quaternion.Euler(objectDef.rotation));
        }
    }

    public void OnTimeSliderChanged()
    {
        secondsInYear = Mathf.CeilToInt(30 * sliderTimeInYear.value);
        textTimer.gameObject.SetActive(secondsInYear > 0);
        textTimer.text = $"{secondsInYear / 60}:{secondsInYear % 60:00}";
    }

    public void OnMaxYearSliderChanged()
    {
        maxYears = Mathf.CeilToInt(sliderMaxYears.value);
        textYear.text = $"YEAR {currentYear}/{maxYears}";
    }

    private void SaveMapToClipboard()
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

        if (buildMode == BuildMode.Add)
            objectIndicator.SetVisuals(objectDef.mesh, materialBuildModeAdd, objectDef.offset, objectDef.scale, Quaternion.Euler(objectDef.rotation));
        else
            objectIndicator.SetVisuals(meshCube, materialBuildModeRemove, Vector3.zero, Vector3.one, Quaternion.identity);
    }

    private bool CanBuildOnTop(int x, int z, TileType selectedTile, out int yIndex)
    {
        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[currentTileTypeIndex];

        yIndex = 0;
        var canBuild = false;

        for (var y = 0; y < mapSizeY; ++y)
        {
            canBuild = false;
            for (var xExtent = 0; xExtent < objectDef.gridSize; ++xExtent)
            {
                for (var zExtent = 0; zExtent < objectDef.gridSize; ++zExtent)
                {
                    if (IsValidMapIndex(x + xExtent, yIndex, z + zExtent) == false)
                    {
                        yIndex = y;
                        return false;
                    }

                    switch (mapData[x + xExtent, y, z + zExtent])
                    {
                        default:
                            {
                                if (canBuild && (xExtent > 0 || zExtent > 0))
                                {
                                    yIndex = y;
                                    return false;
                                }
                            } break;
                        case TileType.Empty:
                            {
                                if (selectedTile != TileType.Ground && waterLevel >= y || y < waterLevel)
                                {
                                    yIndex = y;
                                    return false;
                                }

                                var prevTileType = mapData[x, y - 1, z];
                                if ((prevTileType == TileType.Ground || prevTileType == TileType.Empty) ||
                                    (prevTileType == TileType.Seafloor && selectedTile == TileType.Ground))
                                {
                                    if (y > 0 && mapObjects[x, y - 1, z].State != MapObjectState.Complete)
                                    {
                                        yIndex = y - 1;
                                        return false;
                                    }
                                }

                                yIndex = y;
                                canBuild = true;
                            }
                            break;
                        case TileType.Forest:
                        case TileType.House:
                        case TileType.Farm:
                        case TileType.Stone:
                        case TileType.Invisible:
                            {
                                yIndex = y;
                                return false;
                            }
                    }
                }
            }

            if (canBuild)
                return true;
        }

        return canBuild;
    }

    private bool CanEraseTile(int x, int z, TileType tileType, out int yIndex)
    {
        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[(int)tileType];

        yIndex = 1;

        if (IsValidMapIndex(x, yIndex, z) == false)
            return false;

        for (var y = mapSizeY - 1; y > 0; --y)
        {
            switch (mapData[x, y, z])
            {
                case TileType.Empty:
                case TileType.Invisible: continue;
                default:
                    {
                        if (IsValidMapIndex(x, y + objectDef.gridSize + 1, z) && mapData[x, y + objectDef.gridSize + 1, z] != TileType.Empty)
                        {
                            yIndex = y;
                            return false;
                        }
                        if (mapData[x, y, z] != TileType.Ground && waterLevel >= y || y < waterLevel)
                        {
                            yIndex = y;
                            return false;
                        }

                        if (y > 0 && mapObjects[x, y, z].State != MapObjectState.Complete)
                        {
                            if (buildMode == BuildMode.Add)
                            {
                                yIndex = y - 1;
                                return false;
                            }
                            else
                            {
                                yIndex = y;
                                return true;
                            }
                        }

                        yIndex = y;
                        return true;
                    }
            }
        }

        return true;
    }

    private void ShowGameplayInfoText(string text)
    {
        textGameplayInfo.text = text;
        textGameplayInfo.alpha = 2;
    }

    private bool TryBuild(int x, int z)
    {
        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[currentTileTypeIndex];
        if (CanBuildOnTop(x, z, objectDef.tileType, out var y) == false)
        {
            ShowGameplayInfoText("CAN'T BUILD THERE");
            return false;
        }

        if (energy < objectDef.buildCostEnergy)
        {
            ShowGameplayInfoText("NOT ENOUGH ENERGY");
            return false;
        }

        if (wood < objectDef.buildCostWood)
        {
            ShowGameplayInfoText("NOT ENOUGH WOOD");
            return false;
        }

        if (objectDef.buildTime == 0)
        {
            SetMapTile(x, y, z, objectDef.tileType, MapObjectState.Complete, 0);
            particleSystemBuildingComplete.transform.position = new Vector3(x, y, z);
            particleSystemBuildingComplete.Play();
        }
        else
        {
            SetMapTile(x, y, z, objectDef.tileType, MapObjectState.Building, objectDef.buildTime);
        }

        DrawMapTile(x, y, z);
        SetEnergy(energy - objectDef.buildCostEnergy, true);
        SetWood(wood - objectDef.buildCostWood, true);

        return true;
    }

    private bool TryDemolish(int x, int z, TileType tileType)
    {
        if (CanEraseTile(x, z, tileType, out var y) == false)
        {
            ShowGameplayInfoText("CAN'T DEMOLISH THERE");
            return false;
        }

        var existingMapObject = mapObjects[x, y, z];
        var existingMapObjectDef = MapObjectSettings.Instance.mapObjectDefinitions[(int)existingMapObject.TileType];

        if (existingMapObject.State == MapObjectState.Building)
        {
            SetEnergy(energy + existingMapObjectDef.buildCostEnergy, true);
            SetWood(wood + existingMapObjectDef.buildCostWood, true);
            SetMapTile(x, y, z, TileType.Empty, MapObjectState.Complete, 0);
        }
        else if (existingMapObject.State == MapObjectState.Demolishing)
        {
            SetEnergy(energy + existingMapObjectDef.removeCost, true);
            SetMapTile(x, y, z, existingMapObjectDef.tileType, MapObjectState.Complete, 0);
        }
        else
        {
            var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[(int)tileType];
            if (energy < objectDef.removeCost)
            {
                ShowGameplayInfoText("NOT ENOUGH ENERGY");
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

            SetEnergy(energy - objectDef.removeCost, true);
        }

        DrawMapTile(x, y, z);

        return true;
    }

    private void SetEnergy(int value, bool showDeltaText)
    {
        var delta = value - energy;
        energy = value;
        textEnergy.text = energy.ToString();

        if (showDeltaText && delta != 0)
        {
            if (delta >= 0)
                textEnergyDelta.text = $"<color=#00FF00>+{delta}</color>";
            else
                textEnergyDelta.text = $"<color=#FF0000>{delta}</color>";

            textEnergyDelta.alpha = 1.5f;
        }
    }

    private void SetWood(int value, bool showDeltaText)
    {
        var delta = value - wood;
        wood = value;
        textWood.text = $"{wood}";

        if (showDeltaText && delta != 0)
        {
            if (delta >= 0)
                textWoodDelta.text = $"<color=#00FF00>+{delta}</color>";
            else
                textWoodDelta.text = $"<color=#FF0000>{delta}</color>";

            textWoodDelta.alpha = 1.5f;
        }
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

    private void SetYear(int value)
    {
        if (gameState != GameState.Active)
            return;

        currentYear = value;
        textYear.text = $"YEAR {currentYear}/{maxYears}";
        timer = secondsInYear;
        infiniteTime = secondsInYear == 0;

        SetEnergy(energyPerLevel, true);

        if (currentYear % waterRiseYears == 0)
        {
            waterLevel += 1;
            textWaterRises.alpha = 2;
        }

        water.transform.position = new Vector3(water.transform.position.x, waterLevel, water.transform.position.z);

        cameraController.minY = waterLevel + 1f;

        var woodGain = 0;

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
                                        woodGain += MapObjectSettings.Instance.mapObjectDefinitions[(int)mapData[x, y, z]].woodFromBuilding;
                                        mapObjects[x, y, z].SetTile(mapData[x, y, z], MapObjectState.Complete, 0);
                                        particleSystemBuildingComplete.transform.position = new Vector3(x, y, z);
                                        particleSystemBuildingComplete.Emit(10);
                                    }
                                    else if (mapObjects[x, y, z].State == MapObjectState.Demolishing)
                                    {
                                        woodGain += MapObjectSettings.Instance.mapObjectDefinitions[(int)mapData[x, y, z]].woodFromDestruction;
                                        SetMapTile(x, y, z, TileType.Empty, MapObjectState.Complete, 0);
                                    }
                                    DrawMapTile(x, y, z);
                                }
                            } break;
                    }
                }
            }
        }

        SetWood(wood + woodGain, true);

        CountScores();

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
                    currentYear = 1;
                    OnMaxYearSliderChanged();
                    OnTimeSliderChanged();
                } break;
            case GameState.Active:
                {
                    cameraController.SetTargetRotation(cameraStartTransform);
                    SetCurrentTileTypeIndex(0);
                    ResetGame();
                    uiInfoPanel.SetActive(false);
                } break;
            case GameState.GameOver:
                {
                    var housesLost = 0;
                    var farmsLost = 0;

                    for (var x = 0; x < mapSizeX; ++x)
                    {
                        for (var y = 0; y < mapSizeY; ++y)
                        {
                            for (var z = 0; z < mapSizeZ; ++z)
                            {
                                if (mapData[x, y, z] == TileType.House && waterLevel >= y)
                                    housesLost++;
                                if (mapData[x, y, z] == TileType.Farm && waterLevel >= y)
                                    farmsLost++;
                            }
                        }
                    }

                    textHousesLost.text = $"HOUSES FLOODED: {housesLost}";
                    textFarmsLost.text = $"FARMS FLOODED: {farmsLost}";
                } break;
        }

        objectIndicator.gameObject.SetActive(gameState == GameState.Active);
        hotkeyButtonsPanel.SetActive(gameState == GameState.Active);

        uiContentMenu.SetActive(gameState == GameState.Menu);
        uiContentGameActive.SetActive(gameState == GameState.Active || gameState == GameState.GameOver);
        uiContentGameOver.SetActive(gameState == GameState.GameOver);
    }

    private void ResetGame()
    {
        LoadMap();
        waterLevel = 1;
        SetWood(0, false);
        SetYear(1);
        CountScores();
    }

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
        var objectDef = MapObjectSettings.Instance.mapObjectDefinitions[(int)tileType];
        for (var xExtent = 0; xExtent < objectDef.gridSize; ++xExtent)
        {
            for (var yExtent = 0; yExtent < objectDef.gridSize; ++yExtent)
            {
                for (var zExtent = 0; zExtent < objectDef.gridSize; ++zExtent)
                {
                    var newX = x + xExtent;
                    var newY = y + yExtent;
                    var newZ = z + zExtent;

                    if (IsValidMapIndex(newX, newY, newZ) == false)
                        return;

                    if (xExtent == 0 && yExtent == 0 && zExtent == 0)
                    {
                        mapData[newX, newY, newZ] = tileType;
                    }
                    else
                    {
                        mapData[newX, newY, newZ] = TileType.Invisible;
                    }

                    mapObjects[newX, newY, newZ].SetTile(tileType, state, yearDelay);
                }
            }
        }
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

            if (mapData[x, y, z] == TileType.Seafloor)
            {
                mapObjects[x, y, z].SetVisualsActive(false, true);
            }
            if (mapData[x, y, z] == TileType.Invisible)
            {
                mapObjects[x, y, z].SetVisualsActive(false, false);
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

        if (houses < houseGoal)
            textHouses.text = $"<color=#FF0000>{houses}</color>/{houseGoal}";
        else
            textHouses.text = $"<color=#00FF00>{houses}</color>/{houseGoal}";

        if (farms < farmGoal)
            textFarms.text = $"<color=#FF0000>{farms}</color>/{farmGoal}";
        else
            textFarms.text = $"<color=#00FF00>{farms}</color>/{farmGoal}";
    }
}
