using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

/// <summary>
/// Orquestra a geração: planeja -> constrói -> bakeia NavMesh -> popula -> posiciona o player.
/// Ordem é crítica: NavMesh precisa estar pronto antes dos NavMeshAgent dos inimigos.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("Dados")]
    public RoomCatalog catalog;
    public DungeonProfile profile;
    public EncounterTable encounterTable;

    [Header("Cena")]
    public DungeonBuilder builder;
    public NavMeshBaker navMeshBaker;
    public RoomPopulator populator;
    public Transform player;
    public ChestTable chestTable;

    [Header("Seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    [Header("Execução")]
    [Tooltip("Gera automaticamente no Start (útil em cena dedicada de dungeon).")]
    public bool generateOnStart = false;

    public DungeonLayout CurrentLayout { get; private set; }

    private void Start()
    {
        if (generateOnStart) Generate();
    }

    public void Generate()
    {
        int usedSeed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : seed;

        List<RoomDefinition> defs = catalog.BuildDefinitions();
        DungeonSettings settings = profile.ToSettings();

        DungeonLayout layout = DungeonPlanner.Plan(defs, settings, usedSeed);
        if (!layout.Success)
        {
            Debug.LogError("[DungeonGenerator] Falha ao planejar a masmorra (seed " + usedSeed + ").");
            return;
        }
        CurrentLayout = layout;
        seed = layout.Seed;

        // 1. Construir.
        builder.Clear();
        List<GameObject> instances = builder.Build(layout);

        // 2. Bakear NavMesh (depois de tudo instanciado).
        navMeshBaker.Bake();

        // 3. Popular (inimigos desativados; ativam por trigger).
        var rng = new System.Random(layout.Seed);
        if (populator != null)
        {
            populator.encounterTable = encounterTable;
            populator.chestTable = chestTable;
        }
        for (int i = 0; i < layout.Rooms.Count; i++)
        {
            PlannedRoom pr = layout.Rooms[i];
            populator.Populate(instances[i], pr.Definition.Type, pr.Depth, rng);
        }

        // 4. Portas entre salas (depois do bake e dos RoomControllers existirem).
        List<PlacedDoor> doors = builder.BuildDoors(layout, instances);
        foreach (PlacedDoor pd in doors)
        {
            if (pd.RoomA != null) pd.RoomA.GetComponent<RoomController>()?.RegisterDoor(pd.Door);
            if (pd.RoomB != null) pd.RoomB.GetComponent<RoomController>()?.RegisterDoor(pd.Door);
        }

        // 5. Posicionar o player na sala de início.
        PlacePlayerAtStart(layout, instances);
    }

    private void PlacePlayerAtStart(DungeonLayout layout, List<GameObject> instances)
    {
        if (player == null) return;
        for (int i = 0; i < layout.Rooms.Count; i++)
        {
            if (layout.Rooms[i].Definition.Type == RoomType.Inicio)
            {
                player.position = instances[i].transform.position + Vector3.up * 1f;
                return;
            }
        }
    }
}
