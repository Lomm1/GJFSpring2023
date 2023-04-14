using UnityEngine;

[CreateAssetMenu(fileName = "MapObjectSettings", menuName = "Settings/MapObjectSettings")]
public class MapObjectSettings : ScriptableObject
{
    public static MapObjectSettings Instance { get; private set; }
    public static void Initialize() => Instance = Resources.Load<MapObjectSettings>("MapObjectSettings");

    public MapObjectDefinition[] mapObjectDefinitions;
}

[System.Serializable]
public struct MapObjectDefinition
{
    public TileType tileType;
    public Color iconColor;
    public Mesh mesh;
    public Material material;
    public Vector3 offset;
    public Vector3 scale;
    public Vector3 rotation;
    public int gridSize;
    public int buildCost;
    public int removeCost;
    public int buildTime;
    public int removeTime;
}