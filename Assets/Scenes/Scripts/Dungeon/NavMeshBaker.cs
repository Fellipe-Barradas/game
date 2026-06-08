using UnityEngine;
using Unity.AI.Navigation;

/// <summary>Bake do NavMesh em runtime após a masmorra estar montada.</summary>
[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshBaker : MonoBehaviour
{
    private NavMeshSurface surface;

    private void Awake()
    {
        surface = GetComponent<NavMeshSurface>();
    }

    public void Bake()
    {
        if (surface == null) surface = GetComponent<NavMeshSurface>();
        surface.BuildNavMesh();
    }
}
