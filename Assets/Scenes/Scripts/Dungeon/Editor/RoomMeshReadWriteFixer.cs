using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ferramenta de Editor: marca "Read/Write Enabled" em todas as meshes usadas
/// pelos prefabs de salas de um RoomCatalog. Necessário porque o NavMesh é
/// baked em runtime (NavMeshBaker), e isso exige acesso de leitura às meshes
/// — caso contrário o bake falha no player (build).
/// </summary>
public static class RoomMeshReadWriteFixer
{
    private const string MenuRoot = "Dungeon/Fix Mesh Read-Write/";

    [MenuItem(MenuRoot + "Catálogo selecionado")]
    private static void FixSelectedCatalog()
    {
        var catalog = Selection.activeObject as RoomCatalog;
        if (catalog == null)
        {
            EditorUtility.DisplayDialog(
                "Read/Write Fixer",
                "Selecione um RoomCatalog na janela Project antes de rodar.",
                "Ok");
            return;
        }
        Fix(new[] { catalog });
    }

    [MenuItem(MenuRoot + "Todos os catálogos do projeto")]
    private static void FixAllCatalogs()
    {
        var guids = AssetDatabase.FindAssets("t:RoomCatalog");
        var catalogs = new List<RoomCatalog>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var c = AssetDatabase.LoadAssetAtPath<RoomCatalog>(path);
            if (c != null) catalogs.Add(c);
        }

        if (catalogs.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Read/Write Fixer",
                "Nenhum RoomCatalog encontrado no projeto.",
                "Ok");
            return;
        }
        Fix(catalogs);
    }

    private static void Fix(IReadOnlyList<RoomCatalog> catalogs)
    {
        // Junta os caminhos de assets de modelo (FBX/OBJ) de todas as meshes.
        var modelPaths = new HashSet<string>();

        foreach (var catalog in catalogs)
        {
            if (catalog == null) continue;
            foreach (var entry in catalog.rooms)
            {
                if (entry == null || entry.prefab == null) continue;
                CollectModelPaths(entry.prefab, modelPaths);
            }
        }

        if (modelPaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Read/Write Fixer",
                "Nenhuma mesh importada de modelo (FBX/OBJ) encontrada nos prefabs.\n" +
                "Meshes geradas por código não precisam desse ajuste.",
                "Ok");
            return;
        }

        int changed = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var path in modelPaths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;     // mesh não veio de um modelo importável
                if (importer.isReadable) continue;   // já está ok

                importer.isReadable = true;
                importer.SaveAndReimport();
                changed++;
                Debug.Log($"[ReadWriteFixer] Read/Write habilitado: {path}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "Read/Write Fixer",
            $"Modelos analisados: {modelPaths.Count}\n" +
            $"Read/Write habilitado em: {changed}\n\n" +
            (changed == 0
                ? "Tudo já estava correto."
                : "Pronto — pode rodar o jogo / build sem o erro de NavMesh."),
            "Ok");
    }

    /// <summary>Coleta os caminhos de asset das meshes referenciadas por um prefab (incluindo filhos).</summary>
    private static void CollectModelPaths(GameObject prefab, HashSet<string> paths)
    {
        // MeshFilter (geometria estática) + SkinnedMeshRenderer (caso haja).
        foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
            AddMeshPath(mf.sharedMesh, paths);

        foreach (var smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            AddMeshPath(smr.sharedMesh, paths);
    }

    private static void AddMeshPath(Mesh mesh, HashSet<string> paths)
    {
        if (mesh == null) return;
        var path = AssetDatabase.GetAssetPath(mesh);
        if (!string.IsNullOrEmpty(path)) paths.Add(path);
    }
}
