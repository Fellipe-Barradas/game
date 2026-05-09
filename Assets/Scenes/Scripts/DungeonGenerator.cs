using UnityEngine;

/// <summary>
/// DungeonGenerator — História 4.3: Cenário
/// 
/// Como usar:
/// 1. Coloque este script em um GameObject vazio chamado "DungeonGenerator"
/// 2. Leia as instruções abaixo para configurar os prefabs no Inspector
/// 3. Clique em Play — o dungeon é gerado automaticamente!
/// 
/// IMPORTANTE: Antes de usar, você precisa criar Prefabs dos modelos FBX.
/// Veja as instruções no final deste arquivo.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("=== PEÇAS DO CENÁRIO ===")]
    [Tooltip("Arraste o prefab do CHÃO aqui (floor ou floor-flat do Kenney)")]
    public GameObject prefabChao;

    [Tooltip("Arraste o prefab da PAREDE aqui (bricks do Kenney)")]
    public GameObject prefabParede;

    [Tooltip("Arraste o prefab do TETO aqui (floor-flat serve como teto também)")]
    public GameObject prefabTeto;

    [Tooltip("Arraste o prefab da COLUNA aqui (column do Kenney)")]
    public GameObject prefabColuna;

    [Header("=== PROPS / DECORAÇÃO ===")]
    [Tooltip("Arraste o prefab do BARRIL aqui (barrels do Kenney)")]
    public GameObject prefabBarril;

    [Tooltip("Arraste o prefab da CERCA/GRADE aqui (fence do Kenney)")]
    public GameObject prefabCerca;

    [Header("=== TAMANHO DO DUNGEON ===")]
    [Tooltip("Largura do dungeon em tiles (cada tile = 1 unidade)")]
    public int largura = 10;

    [Tooltip("Comprimento do dungeon em tiles")]
    public int comprimento = 10;

    [Tooltip("Altura das paredes")]
    public float alturaParede = 3f;

    [Tooltip("Tamanho de cada tile do chão")]
    public float tamanhoTile = 1f;

    [Header("=== DECORAÇÃO ===")]
    [Tooltip("Quantos barris espalhar pelo dungeon")]
    public int quantidadeBarris = 5;

    [Tooltip("Seed para aleatoriedade (mude para gerar layouts diferentes)")]
    public int seed = 42;

    // Pai organizador na Hierarchy
    private GameObject pastaAmbiente;
    private GameObject pastaChao;
    private GameObject pastaParedes;
    private GameObject pastaTeto;
    private GameObject pastaProps;

    void Start()
    {
        Random.InitState(seed);
        CriarEstrutura();
        GerarChao();
        GerarParedes();
        GerarTeto();
        GerarColunas();
        GerarProps();

        Debug.Log("[DungeonGenerator] Dungeon gerado com sucesso!");
    }

    void CriarEstrutura()
    {
        // Cria grupos organizadores na Hierarchy
        pastaAmbiente = new GameObject("--- AMBIENTE ---");

        pastaChao = new GameObject("Chao");
        pastaChao.transform.SetParent(pastaAmbiente.transform);

        pastaParedes = new GameObject("Paredes e Muros");
        pastaParedes.transform.SetParent(pastaAmbiente.transform);

        pastaTeto = new GameObject("Teto");
        pastaTeto.transform.SetParent(pastaAmbiente.transform);

        pastaProps = new GameObject("Props Medievais");
        pastaProps.transform.SetParent(pastaAmbiente.transform);
    }

    void GerarChao()
    {
        if (prefabChao == null) { Debug.LogWarning("[Dungeon] Prefab de Chão não definido!"); return; }

        for (int x = 0; x < largura; x++)
        {
            for (int z = 0; z < comprimento; z++)
            {
                Vector3 pos = new Vector3(x * tamanhoTile, 0f, z * tamanhoTile);
                GameObject tile = Instantiate(prefabChao, pos, Quaternion.identity, pastaChao.transform);
                tile.name = $"Chao_{x}_{z}";
            }
        }
    }

    void GerarParedes()
    {
        if (prefabParede == null) { Debug.LogWarning("[Dungeon] Prefab de Parede não definido!"); return; }

        // Parede Sul (Z = 0)
        for (int x = 0; x < largura; x++)
        {
            CriarParede(new Vector3(x * tamanhoTile, 0f, -tamanhoTile), Quaternion.identity, $"Parede_Sul_{x}");
        }

        // Parede Norte (Z = comprimento)
        for (int x = 0; x < largura; x++)
        {
            CriarParede(new Vector3(x * tamanhoTile, 0f, comprimento * tamanhoTile), Quaternion.Euler(0, 180, 0), $"Parede_Norte_{x}");
        }

        // Parede Oeste (X = 0)
        for (int z = 0; z < comprimento; z++)
        {
            CriarParede(new Vector3(-tamanhoTile, 0f, z * tamanhoTile), Quaternion.Euler(0, 90, 0), $"Parede_Oeste_{z}");
        }

        // Parede Leste (X = largura)
        for (int z = 0; z < comprimento; z++)
        {
            CriarParede(new Vector3(largura * tamanhoTile, 0f, z * tamanhoTile), Quaternion.Euler(0, -90, 0), $"Parede_Leste_{z}");
        }
    }

    void CriarParede(Vector3 posBase, Quaternion rotacao, string nome)
    {
        // Empilha paredes até a altura definida
        int niveis = Mathf.RoundToInt(alturaParede);
        for (int y = 0; y < niveis; y++)
        {
            Vector3 pos = posBase + Vector3.up * (y * tamanhoTile);
            GameObject parede = Instantiate(prefabParede, pos, rotacao, pastaParedes.transform);
            parede.name = $"{nome}_Y{y}";
        }
    }

    void GerarTeto()
    {
        if (prefabTeto == null) { Debug.LogWarning("[Dungeon] Prefab de Teto não definido! Pulando..."); return; }

        float alturaTeto = alturaParede * tamanhoTile;

        for (int x = 0; x < largura; x++)
        {
            for (int z = 0; z < comprimento; z++)
            {
                Vector3 pos = new Vector3(x * tamanhoTile, alturaTeto, z * tamanhoTile);
                GameObject tile = Instantiate(prefabTeto, pos, Quaternion.Euler(180, 0, 0), pastaTeto.transform);
                tile.name = $"Teto_{x}_{z}";
            }
        }
    }

    void GerarColunas()
    {
        if (prefabColuna == null) { Debug.LogWarning("[Dungeon] Prefab de Coluna não definido! Pulando..."); return; }

        // Coloca colunas nos 4 cantos internos do dungeon
        Vector3[] cantos = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3((largura - 1) * tamanhoTile, 0, 0),
            new Vector3(0, 0, (comprimento - 1) * tamanhoTile),
            new Vector3((largura - 1) * tamanhoTile, 0, (comprimento - 1) * tamanhoTile)
        };

        for (int i = 0; i < cantos.Length; i++)
        {
            GameObject coluna = Instantiate(prefabColuna, cantos[i], Quaternion.identity, pastaProps.transform);
            coluna.name = $"Coluna_{i}";
        }
    }

    void GerarProps()
    {
        if (prefabBarril == null) { Debug.LogWarning("[Dungeon] Prefab de Barril não definido! Pulando..."); return; }

        // Espalha barris em posições aleatórias dentro do dungeon
        for (int i = 0; i < quantidadeBarris; i++)
        {
            // Evita colocar barris nas bordas (margem de 1 tile)
            int x = Random.Range(1, largura - 1);
            int z = Random.Range(1, comprimento - 1);
            Vector3 pos = new Vector3(x * tamanhoTile, 0f, z * tamanhoTile);

            float rotacaoAleatoria = Random.Range(0f, 360f);
            GameObject barril = Instantiate(prefabBarril, pos, Quaternion.Euler(0, rotacaoAleatoria, 0), pastaProps.transform);
            barril.name = $"Barril_{i}";
        }
    }

    // Desenha o outline do dungeon no Editor para visualização
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Vector3 centro = new Vector3((largura / 2f) * tamanhoTile, alturaParede / 2f, (comprimento / 2f) * tamanhoTile);
        Vector3 tamanho = new Vector3(largura * tamanhoTile, alturaParede, comprimento * tamanhoTile);
        Gizmos.DrawCube(centro, tamanho);

        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireCube(centro, tamanho);
    }
}

/*
=============================================================
  INSTRUÇÕES: COMO CRIAR PREFABS DOS MODELOS FBX
=============================================================

Os modelos FBX que você importou precisam virar "Prefabs" antes
de serem usados no script. Faça assim para cada modelo:

1. Na aba PROJECT, navegue até a pasta MedievalKit > kenney_retr... > Models > FBX format
2. Clique no modelo (ex: "floor") para selecioná-lo
3. Arraste ele da aba PROJECT para dentro da aba SCENE
4. O objeto vai aparecer na cena — agora arraste ele de volta
   da HIERARCHY para a pasta "Assets/Scenes/Prefabs" na aba PROJECT
   (crie a pasta Prefabs se não existir)
5. Um ícone azul vai aparecer — isso é o Prefab pronto!
6. Delete o objeto da cena (ele já está salvo como Prefab)
7. Repita para: floor, bricks, column, barrels, fence

Depois:
- Selecione o GameObject "DungeonGenerator" na Hierarchy
- No Inspector, arraste cada Prefab para o campo correspondente
- Clique em Play e o dungeon é gerado!

=============================================================
*/
