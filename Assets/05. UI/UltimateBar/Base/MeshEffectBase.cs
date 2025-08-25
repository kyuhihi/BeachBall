using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public abstract class MeshEffectBase : MonoBehaviour
{
    protected MeshFilter meshFilter;
    protected Mesh originalMesh;
    protected Mesh modifiedMesh;

    protected virtual void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh != null)
        {
            originalMesh = meshFilter.sharedMesh;
            modifiedMesh = Instantiate(originalMesh);
            meshFilter.sharedMesh = modifiedMesh;
        }
    }

    protected virtual void OnEnable()
    {
        ApplyEffect();
    }

    protected virtual void OnValidate()
    {
        if (isActiveAndEnabled)
            ApplyEffect();
    }

    public abstract void ApplyEffect();

    protected Vector3[] GetVertices()
    {
        return originalMesh != null ? (Vector3[])originalMesh.vertices.Clone() : null;
    }

    protected void SetVertices(Vector3[] vertices)
    {
        if (modifiedMesh == null) return;
        modifiedMesh.vertices = vertices;
        modifiedMesh.RecalculateNormals();
        modifiedMesh.RecalculateBounds();
    }
}
