using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ferramenta de Editor: garante um MeshCollider sólido em cada peça de mesh
/// dos prefabs de sala de um RoomCatalog. Necessário porque salas como
/// SalaInicio/SalaBaus só tinham o BoxCollider de bounds (isTrigger), que não
/// barra o player — logo o jogador atravessava chão e paredes.
/// Espelha o padrão já usado em SalaBoss (MeshCollider por peça de geometria).
/// </summary>
public static class RoomColliderFixer
{
    private const string MenuRoot = "Dungeon/Add Room Colliders/";

    [MenuItem(MenuRoot + "Catálogo selecionado")]
    private static void FixSelectedCatalog()
    {
        var catalog = Selection.activeObject as RoomCatalog;
        if (catalog == null)
        {
            EditorUtility.DisplayDialog(
                "Room Collider Fixer",
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
                "Room Collider Fixer",
                "Nenhum RoomCatalog encontrado no projeto.",
                "Ok");
            return;
        }
        Fix(catalogs);
    }

    private static void Fix(IReadOnlyList<RoomCatalog> catalogs)
    {
        // Junta os caminhos únicos dos prefabs de sala de todos os catálogos.
        var prefabPaths = new HashSet<string>();
        foreach (var catalog in catalogs)
        {
            if (catalog == null) continue;
            foreach (var entry in catalog.rooms)
            {
                if (entry == null || entry.prefab == null) continue;
                var path = AssetDatabase.GetAssetPath(entry.prefab);
                if (!string.IsNullOrEmpty(path)) prefabPaths.Add(path);
            }
        }

        if (prefabPaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Room Collider Fixer",
                "Nenhum prefab de sala encontrado nos catálogos.",
                "Ok");
            return;
        }

        int totalAdded = 0;
        int prefabsChanged = 0;

        foreach (var path in prefabPaths)
        {
            // Edita o asset do prefab diretamente (não uma instância em cena).
            GameObject rootContents = PrefabUtility.LoadPrefabContents(path);
            if (rootContents == null) continue;

            int addedHere = 0;
            try
            {
                foreach (var mf in rootContents.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    var go = mf.gameObject;

                    // Já tem MeshCollider sólido aqui? Não duplica.
                    if (go.TryGetComponent<MeshCollider>(out _)) continue;

                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;     // geometria estática de cenário
                    addedHere++;
                }

                if (addedHere > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(rootContents, path);
                    prefabsChanged++;
                    totalAdded += addedHere;
                    Debug.Log($"[RoomColliderFixer] {addedHere} MeshCollider(s) adicionados em {path}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rootContents);
            }
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Room Collider Fixer",
            $"Prefabs analisados: {prefabPaths.Count}\n" +
            $"Prefabs alterados: {prefabsChanged}\n" +
            $"MeshColliders adicionados: {totalAdded}\n\n" +
            (totalAdded == 0
                ? "Todas as salas já tinham colliders."
                : "Pronto — o player não deve mais atravessar o cenário."),
            "Ok");
    }
}
