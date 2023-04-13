using UnityEngine;

public class MapObject : MonoBehaviour
{
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

    public void SetVisualsActive(bool value)
    {
        if (meshCollider != null)
            meshCollider.enabled = value;

        objectVisual.SetActive(value);
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
