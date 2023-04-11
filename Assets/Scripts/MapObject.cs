using UnityEngine;

public class MapObject : MonoBehaviour
{
    public int xPos;
    public int yPos;
    public int zPos;

    [SerializeField] private GameObject objectVisual;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private BoxCollider boxCollider;

    public void Initialize(int x, int y, int z)
    {
        xPos = x;
        yPos = y;
        zPos = z;
        gameObject.transform.position = new Vector3(x, y, z);
    }

    public void SetVisualsActive(bool value)
    {
        boxCollider.enabled = value;
        objectVisual.SetActive(value);
    }

    public void SetVisuals(Mesh mesh, Material material, Vector3 visualOffset, Vector3 scale, Quaternion rotation)
    {
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
        objectVisual.transform.localPosition = visualOffset;
        objectVisual.transform.localScale = scale;
        objectVisual.transform.rotation = rotation;
    }
}
