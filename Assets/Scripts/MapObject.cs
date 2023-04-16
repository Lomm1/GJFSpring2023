using UnityEngine;

public class MapObject : MonoBehaviour
{
    public TileType TileType { get; private set; }
    public MapObjectState State { get; private set; }

    public int yearsRemaining;

    public int xPos;
    public int yPos;
    public int zPos;

    public GameObject objectVisual;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public void Initialize(int x, int y, int z)
    {
        xPos = x;
        yPos = y;
        zPos = z;
        gameObject.transform.position = new Vector3(x, y, z);
    }

    public void SetTile(TileType tileType, MapObjectState state, int buildTime)
    {
        TileType = tileType;
        State = state;
        yearsRemaining = buildTime;
    }

    public void SetVisualsActive(bool value, bool meshActive = true)
    {
        if (meshCollider != null)
            meshCollider.enabled = meshActive;

        meshRenderer.enabled = value;
    }

    public void SetVisuals(Mesh mesh, Material material, Vector3 visualOffset, Vector3 scale, Quaternion rotation)
    {
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
        objectVisual.transform.localPosition = visualOffset;
        objectVisual.transform.localScale = scale;
        objectVisual.transform.rotation = rotation;

        if (meshCollider != null)
            meshCollider.sharedMesh = mesh;
    }

    public void SetMaterial(Material material)
    {
        meshRenderer.material = material;
    }
}
