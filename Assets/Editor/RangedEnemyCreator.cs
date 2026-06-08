#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class RangedEnemyCreator
{
    [MenuItem("GameObject/Inimigo/Inimigo Arcano", false, 10)]
    static void CriarInimigoArcano(MenuCommand cmd)
    {
        GameObject go = new("Inimigo Arcano");
        go.AddComponent<NavMeshAgent>();
        go.AddComponent<CapsuleCollider>();
        go.AddComponent<RangedEnemy>();

        // Tenta posicionar no chão sob o cursor da Scene view
        Vector3 posicao = ObterPosicaoNoCenario();
        go.transform.position = posicao;

        GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Criar Inimigo Arcano");
        Selection.activeObject = go;
    }

    static Vector3 ObterPosicaoNoCenario()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return Vector3.zero;

        // Raycast a partir do centro da Scene view para encontrar o chão
        Ray ray = sv.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1f));
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            return hit.point;

        // Fallback: projetar a 10 unidades na direção da câmera
        return sv.camera.transform.position + sv.camera.transform.forward * 10f;
    }
}
#endif
