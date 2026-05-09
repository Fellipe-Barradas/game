using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// SceneOrganizer — História 4.3: Cenário
/// 
/// Como usar:
/// 1. Crie um GameObject vazio na cena e adicione este script.
/// 2. No Inspector, clique em "Organizar Cena" para criar a hierarquia.
/// 3. Mova seus objetos de cena para dentro dos grupos gerados.
/// 4. Remova este script quando terminar (ele é só uma ferramenta de setup).
/// </summary>
public class SceneOrganizer : MonoBehaviour
{
    [Header("Grupos a Criar")]
    public bool criarAmbiente = true;
    public bool criarJogador = true;
    public bool criarInimigos = true;
    public bool criarUI = true;
    public bool criarAudio = true;
    public bool criarLuz = true;

#if UNITY_EDITOR
    [ContextMenu("Organizar Cena")]
    public void OrganizarCena()
    {
        if (criarAmbiente) CriarGrupo("--- AMBIENTE ---", new string[]
        {
            "Terreno",
            "Chao",
            "Paredes e Muros",
            "Obstaculos",
            "Vegetacao",
            "Props Medievais",
            "Agua",
            "Skybox e Fog"
        });

        if (criarJogador) CriarGrupo("--- JOGADOR ---", new string[]
        {
            "Player",
            "Camera Rig",
            "Armas"
        });

        if (criarInimigos) CriarGrupo("--- INIMIGOS ---", new string[]
        {
            "Spawn Points",
            "Inimigos Ativos",
            "Patrulhas"
        });

        if (criarUI) CriarGrupo("--- UI ---", new string[]
        {
            "HUD",
            "Menu Pause",
            "Canvas"
        });

        if (criarAudio) CriarGrupo("--- AUDIO ---", new string[]
        {
            "Musica de Fundo",
            "Sons de Ambiente",
            "Audio Manager"
        });

        if (criarLuz) CriarGrupo("--- ILUMINACAO ---", new string[]
        {
            "Luz Direcional (Sol)",
            "Tochas e Pontos de Luz",
            "Reflection Probes"
        });

        Debug.Log("[SceneOrganizer] Hierarquia criada com sucesso! Mova seus objetos para dentro dos grupos.");
    }

    private void CriarGrupo(string nomeGrupo, string[] subGrupos)
    {
        // Verifica se o grupo já existe para não duplicar
        GameObject grupo = GameObject.Find(nomeGrupo);
        if (grupo == null)
        {
            grupo = new GameObject(nomeGrupo);
            Undo.RegisterCreatedObjectUndo(grupo, "Criar " + nomeGrupo);
        }

        foreach (string nomeSubGrupo in subGrupos)
        {
            // Procura se já existe antes de criar
            Transform filho = grupo.transform.Find(nomeSubGrupo);
            if (filho == null)
            {
                GameObject sub = new GameObject(nomeSubGrupo);
                Undo.RegisterCreatedObjectUndo(sub, "Criar " + nomeSubGrupo);
                sub.transform.SetParent(grupo.transform);
                sub.transform.localPosition = Vector3.zero;
            }
        }
    }
#endif
}

#if UNITY_EDITOR
/// <summary>
/// Botão customizado no Inspector para facilitar o uso.
/// </summary>
[CustomEditor(typeof(SceneOrganizer))]
public class SceneOrganizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("▶  Organizar Cena Agora", GUILayout.Height(40)))
        {
            SceneOrganizer organizer = (SceneOrganizer)target;
            organizer.OrganizarCena();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Clique no botão acima para gerar a hierarquia de grupos na cena.\n" +
            "Depois mova seus objetos para os grupos e remova este script.",
            MessageType.Info
        );
    }
}
#endif
