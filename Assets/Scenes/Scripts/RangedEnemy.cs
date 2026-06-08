using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
public class RangedEnemy : MonoBehaviour, IDamageable
{
    public enum Estado { Idle, Reposicionando, Atirando, Fugindo, HitStun, Morto }

    [Header("Vida")]
    public int vidaMaxima = 60;
    private int vidaAtual;

    [Header("Drops")]
    public int dropOuro       = 5;
    public int dropPrata      = 10;
    public int dropFragmentos = 1;

    [Header("Detecção")]
    public float rangeDeteccao = 15f;
    public float rangeDesistir = 22f;

    [Header("Combate")]
    public int   danoProjétil       = 15;
    public float cooldownTiro       = 2.5f;
    public float velocidadeProjétil = 14f;

    [Header("Posicionamento")]
    public float rangeIdealMin = 7f;
    public float rangeIdealMax = 13f;
    public float rangeFuga     = 5f;

    [Header("Movimento")]
    public float velocidadeFuga          = 5.5f;
    public float velocidadeDeslocamento  = 3f;

    // ── Estado ────────────────────────────────────────────────────────────────
    public Estado estadoAtual { get; private set; }
    private bool  estaEmAggro;
    private float timerTiro;

    // ── Referências ───────────────────────────────────────────────────────────
    private NavMeshAgent agente;
    private Transform    player;

    // ── Visuais (construídos em código) ───────────────────────────────────────
    private Transform  visualRaiz;
    private GameObject corpo, cabeca, bastaoObj, orbObj;
    private readonly List<Renderer> todosRenderers = new();
    private Material matCorpo, matCabeca, matOlhoE, matOlhoD, matBastao, matOrb;

    private static readonly Color CorCorpo   = new(0.15f, 0.05f, 0.25f);
    private static readonly Color CorCabeca  = new(0.85f, 0.78f, 0.72f);
    private static readonly Color CorOlho    = new(1f, 0.1f, 0.05f);
    private static readonly Color CorBastao  = new(0.28f, 0.18f, 0.08f);
    private static readonly Color CorOrbBase = new(0.1f, 0.8f, 1f);

    // ── Animação por código ────────────────────────────────────────────────────
    private float bobTimer;
    private bool  executandoAnimacao;
    private float carregamentoOrb;
    private const float EscalaOrbBase = 0.22f;

    // Ponto de origem do tiro: posição do orbe + um pouco à frente
    private Vector3 OrigemDisparo =>
        orbObj != null
            ? orbObj.transform.position + transform.forward * 0.2f
            : transform.position + Vector3.up * 1.6f;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        ConstruirVisuais();
        // Layer e collider apenas — NavMeshAgent não é tocado aqui
        gameObject.layer = 6;
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.4f;
        col.center = new Vector3(0, 0.9f, 0);
    }

    void Start()
    {
        vidaAtual = vidaMaxima;
        timerTiro = cooldownTiro * 0.5f;
        player    = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Igual ao EnemyDummy: obtém e configura o agente em Start, nunca em Awake
        agente                  = GetComponent<NavMeshAgent>();
        agente.speed            = velocidadeDeslocamento;
        agente.angularSpeed     = 300f;
        agente.acceleration     = 10f;
        agente.stoppingDistance = 0.1f;

        if (player == null)
            Debug.LogError("[RangedEnemy] Player não encontrado! Verifique se o player tem a tag 'Player'.");

        // Aguarda um frame para o NavMeshAgent terminar de inicializar antes de verificar
        StartCoroutine(VerificarNavMesh());
    }

    IEnumerator VerificarNavMesh()
    {
        yield return null; // 1 frame: NavMeshAgent termina de inicializar

        if (!agente.isOnNavMesh)
            Debug.LogError("[RangedEnemy] Inimigo fora da NavMesh! " +
                "Use o Move tool (W) para arrastar 'Inimigo Arcano' para dentro da dungeon (área azul na Scene view).");
    }

    void Update()
    {
        if (estadoAtual == Estado.Morto || player == null) return;

        AtualizarAnimacaoPassiva();

        if (estadoAtual == Estado.HitStun) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (!estaEmAggro && dist <= rangeDeteccao)
            estaEmAggro = true;

        if (estaEmAggro && dist > rangeDesistir)
        {
            estaEmAggro = false;
            MudarEstado(Estado.Idle);
            agente.isStopped = true;
            return;
        }

        if (!estaEmAggro) return;
        if (!agente.isOnNavMesh) return;

        MaquinaDeEstados(dist);
    }

    // ── Máquina de estados ────────────────────────────────────────────────────

    void MaquinaDeEstados(float dist)
    {
        if      (dist < rangeFuga)     Fugir();
        else if (dist < rangeIdealMin) AfastarDevagar();
        else if (dist <= rangeIdealMax) AtirarNoPlayer();
        else                            AproximarParaRange();
    }

    void Fugir()
    {
        MudarEstado(Estado.Fugindo);
        agente.isStopped = false;
        agente.speed     = velocidadeFuga;

        // Fuga em curva para dificultar o rastreamento
        Vector3 dirFuga  = (transform.position - player.position).normalized;
        float   lateral  = Mathf.Sin(Time.time * 0.9f) * 0.75f;
        Vector3 curva    = Vector3.Cross(dirFuga, Vector3.up) * lateral;
        Vector3 alvo     = transform.position + (dirFuga + curva).normalized * 7f;

        if (NavMesh.SamplePosition(alvo, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            agente.SetDestination(hit.position);

        RotacionarParaOlharPlayer();
    }

    void AfastarDevagar()
    {
        MudarEstado(Estado.Reposicionando);
        agente.isStopped = false;
        agente.speed     = velocidadeDeslocamento;

        Vector3 dirFuga = (transform.position - player.position).normalized;
        Vector3 alvo    = transform.position + dirFuga * 4f;

        if (NavMesh.SamplePosition(alvo, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            agente.SetDestination(hit.position);
        else
            agente.SetDestination(transform.position);

        RotacionarParaOlharPlayer();
    }

    void AtirarNoPlayer()
    {
        MudarEstado(Estado.Atirando);
        agente.isStopped = false;
        agente.speed     = 1.5f; // strafe lento enquanto mira

        // Circle-strafe suave: circula o player enquanto atira
        Vector3 dirParaPlayer = (player.position - transform.position).normalized;
        Vector3 strafeDir     = Vector3.Cross(dirParaPlayer, Vector3.up);
        float   fatorStrafe   = Mathf.Sin(Time.time * 0.5f);
        Vector3 alvoStrafe    = transform.position + strafeDir * fatorStrafe * 2f;

        if (NavMesh.SamplePosition(alvoStrafe, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
            agente.SetDestination(hit.position);
        else
            agente.SetDestination(transform.position);

        RotacionarParaOlharPlayer();

        timerTiro -= Time.deltaTime;
        if (timerTiro <= 0f && !executandoAnimacao)
        {
            timerTiro = cooldownTiro;
            StartCoroutine(AnimacaoDisparo());
        }

        AtualizarOrbCarregamento(1f - Mathf.Clamp01(timerTiro / cooldownTiro));
    }

    void AproximarParaRange()
    {
        MudarEstado(Estado.Reposicionando);
        agente.isStopped = false;
        agente.speed     = velocidadeDeslocamento;
        agente.SetDestination(player.position);
        RotacionarParaOlharPlayer();
    }

    void RotacionarParaOlharPlayer()
    {
        Vector3 dir = new(
            player.position.x - transform.position.x, 0,
            player.position.z - transform.position.z);

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 8f);
    }

    // ── Disparo ───────────────────────────────────────────────────────────────

    IEnumerator AnimacaoDisparo()
    {
        executandoAnimacao = true;

        // Pulso do orbe antes de atirar
        float t = 0f;
        while (t < 0.35f)
        {
            if (orbObj != null)
            {
                float s = EscalaOrbBase * (1f + Mathf.Sin(t / 0.35f * Mathf.PI) * 0.55f);
                orbObj.transform.localScale = Vector3.one * s;
            }
            t += Time.deltaTime;
            yield return null;
        }

        Disparar();

        // Recuo do bastão
        if (bastaoObj != null)
        {
            Vector3 posOriginal = bastaoObj.transform.localPosition;
            bastaoObj.transform.localPosition += bastaoObj.transform.up * -0.12f;
            yield return new WaitForSeconds(0.08f);
            bastaoObj.transform.localPosition = posOriginal;
        }

        AtualizarOrbCarregamento(0f);
        executandoAnimacao = false;
    }

    void Disparar()
    {
        if (player == null || estadoAtual == Estado.Morto) return;

        Vector3 origem   = OrigemDisparo;
        Vector3 direcao  = (player.position + Vector3.up * 0.9f - origem).normalized;

        EnemyBolt bolt = EnemyBolt.Criar(danoProjétil, velocidadeProjétil);
        bolt.transform.position = origem;
        bolt.transform.forward  = direcao;
        bolt.DefinirDirecao(direcao);

        // Garante que o bolt não colide imediatamente com o próprio collider
        Physics.IgnoreCollision(GetComponent<CapsuleCollider>(),
                                bolt.GetComponent<SphereCollider>());
    }

    // ── Dano e morte ──────────────────────────────────────────────────────────

    public void TakeDamage(int dano)
    {
        if (estadoAtual == Estado.Morto) return;

        vidaAtual    -= dano;
        estaEmAggro   = true;

        if (vidaAtual <= 0)
            Morrer();
        else
            StartCoroutine(FlashDano());
    }

    IEnumerator FlashDano()
    {
        Estado estadoAnterior = estadoAtual;
        MudarEstado(Estado.HitStun);
        agente.isStopped = true;

        foreach (Renderer r in todosRenderers)
            if (r != null) r.material.color = Color.white;

        yield return new WaitForSeconds(0.15f);

        if (estadoAtual != Estado.Morto)
        {
            RestaurarCores();
            MudarEstado(estadoAnterior);
            agente.isStopped = false;
        }
    }

    void Morrer()
    {
        MudarEstado(Estado.Morto);
        agente.isStopped = true;
        StopAllCoroutines();

        GerenciadorMoedas.Instancia?.AdicionarDrops(dropPrata, dropOuro, dropFragmentos);

        StartCoroutine(AnimacaoMorte());
    }

    IEnumerator AnimacaoMorte()
    {
        Quaternion rotInicial = visualRaiz != null
            ? visualRaiz.localRotation
            : Quaternion.identity;

        float t        = 0f;
        float duracao  = 1.4f;

        while (t < duracao)
        {
            float prog = t / duracao;

            if (visualRaiz != null)
                visualRaiz.localRotation = Quaternion.Lerp(
                    rotInicial, Quaternion.Euler(85f, 0, 20f), prog);

            Color c = Color.Lerp(Color.grey, new Color(0.08f, 0f, 0.08f, 0f), prog);
            foreach (Renderer r in todosRenderers)
                if (r != null) r.material.color = c;

            t += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // ── Animação passiva (código) ─────────────────────────────────────────────

    void AtualizarAnimacaoPassiva()
    {
        bobTimer += Time.deltaTime;
        if (visualRaiz == null) return;

        // Levitação suave
        float bob = Mathf.Sin(bobTimer * 1.8f) * 0.04f;
        visualRaiz.localPosition = new Vector3(0, bob, 0);

        // Inclinação ao fugir
        Quaternion rotAlvo = estadoAtual == Estado.Fugindo
            ? Quaternion.Euler(22f, 0, 0)
            : Quaternion.identity;

        visualRaiz.localRotation = Quaternion.Slerp(
            visualRaiz.localRotation, rotAlvo, Time.deltaTime * 5f);
    }

    void AtualizarOrbCarregamento(float frac)
    {
        if (orbObj == null || matOrb == null) return;

        carregamentoOrb = frac;
        orbObj.transform.localScale = Vector3.one * Mathf.Lerp(EscalaOrbBase, EscalaOrbBase * 1.6f, frac);

        Color cor = Color.Lerp(CorOrbBase, new Color(1f, 0.3f, 0f), frac);
        matOrb.color = cor;
        if (matOrb.HasProperty("_EmissionColor"))
            matOrb.SetColor("_EmissionColor", cor * Mathf.Lerp(2f, 5f, frac));
    }

    void MudarEstado(Estado novo)
    {
        if (estadoAtual == novo) return;
        Estado anterior = estadoAtual;
        estadoAtual = novo;

        if (anterior == Estado.Atirando && novo != Estado.Atirando)
            AtualizarOrbCarregamento(0f);
    }

    // ── Construção visual (tudo por código) ──────────────────────────────────

    void ConstruirVisuais()
    {
        visualRaiz = new GameObject("VisualRaiz").transform;
        visualRaiz.SetParent(transform, false);

        CriarCorpo();
        CriarCabeca();
        CriarBastaoEOrbe();
    }

    void CriarCorpo()
    {
        corpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        corpo.name = "Corpo";
        corpo.transform.SetParent(visualRaiz, false);
        corpo.transform.localPosition = new Vector3(0, 0.82f, 0);
        corpo.transform.localScale    = new Vector3(0.52f, 0.68f, 0.52f);
        Destroy(corpo.GetComponent<CapsuleCollider>());

        matCorpo = CriarMaterial(CorCorpo, glossiness: 0.2f);
        corpo.GetComponent<Renderer>().material = matCorpo;
        todosRenderers.Add(corpo.GetComponent<Renderer>());
    }

    void CriarCabeca()
    {
        cabeca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cabeca.name = "Cabeca";
        cabeca.transform.SetParent(visualRaiz, false);
        cabeca.transform.localPosition = new Vector3(0, 1.72f, 0);
        cabeca.transform.localScale    = Vector3.one * 0.42f;
        Destroy(cabeca.GetComponent<SphereCollider>());

        matCabeca = CriarMaterial(CorCabeca);
        cabeca.GetComponent<Renderer>().material = matCabeca;
        todosRenderers.Add(cabeca.GetComponent<Renderer>());

        // Olhos: posições locais relativas à escala da cabeça (0.42)
        // Z = 0.5 coloca o olho na superfície frontal da esfera
        CriarOlho("OlhoE", new Vector3(-0.3f, 0.05f, 0.48f), ref matOlhoE);
        CriarOlho("OlhoD", new Vector3( 0.3f, 0.05f, 0.48f), ref matOlhoD);
    }

    void CriarOlho(string nome, Vector3 localPos, ref Material mat)
    {
        GameObject olho = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        olho.name = nome;
        olho.transform.SetParent(cabeca.transform, false);
        olho.transform.localPosition = localPos;
        olho.transform.localScale    = Vector3.one * 0.3f;
        Destroy(olho.GetComponent<SphereCollider>());

        mat = CriarMaterial(CorOlho, emissao: CorOlho * 2.5f);
        olho.GetComponent<Renderer>().material = mat;
        todosRenderers.Add(olho.GetComponent<Renderer>());
    }

    void CriarBastaoEOrbe()
    {
        // Cabo do bastão — posicionado como mão direita do personagem
        // Cilindro Unity: altura local 2 (±1), com scale.y = 0.72 → ±0.72 no espaço pai
        // Centro em y=0.93 → topo em y = 0.93 + 0.72 = 1.65 (espaço visualRaiz)
        bastaoObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bastaoObj.name = "Bastao";
        bastaoObj.transform.SetParent(visualRaiz, false);
        bastaoObj.transform.localPosition = new Vector3(0.38f, 0.93f, 0.05f);
        bastaoObj.transform.localScale    = new Vector3(0.07f, 0.72f, 0.07f);
        Destroy(bastaoObj.GetComponent<CapsuleCollider>());

        matBastao = CriarMaterial(CorBastao, metallic: 0.1f, glossiness: 0.55f);
        bastaoObj.GetComponent<Renderer>().material = matBastao;
        todosRenderers.Add(bastaoObj.GetComponent<Renderer>());

        // Orbe mágico no topo do bastão
        // Parente de visualRaiz (não do bastão) para evitar distorção de escala não-uniforme
        // Posicionado no topo do bastão: y = 1.65 + metade do orbe (0.11) = 1.76
        orbObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orbObj.name = "Orbe";
        orbObj.transform.SetParent(visualRaiz, false);
        orbObj.transform.localPosition = new Vector3(0.38f, 1.76f, 0.05f);
        orbObj.transform.localScale    = Vector3.one * EscalaOrbBase;
        Destroy(orbObj.GetComponent<SphereCollider>());

        matOrb = CriarMaterial(CorOrbBase, emissao: CorOrbBase * 2f);
        orbObj.GetComponent<Renderer>().material = matOrb;
        todosRenderers.Add(orbObj.GetComponent<Renderer>());
    }

    // ── Helpers de material ───────────────────────────────────────────────────

    static Material CriarMaterial(
        Color cor,
        Color?  emissao    = null,
        float   metallic   = 0f,
        float   glossiness = 0.3f)
    {
        Shader sh  = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var    mat = new Material(sh) { color = cor };

        if (mat.HasProperty("_Metallic"))  mat.SetFloat("_Metallic",  metallic);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", glossiness);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", glossiness);

        if (emissao.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emissao.Value);
        }

        return mat;
    }

    void RestaurarCores()
    {
        if (matCorpo  != null) matCorpo.color  = CorCorpo;
        if (matCabeca != null) matCabeca.color = CorCabeca;
        if (matOlhoE  != null) matOlhoE.color  = CorOlho;
        if (matOlhoD  != null) matOlhoD.color  = CorOlho;
        if (matBastao != null) matBastao.color = CorBastao;
        AtualizarOrbCarregamento(carregamentoOrb);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeFuga);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, rangeIdealMin);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, rangeIdealMax);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangeDeteccao);
    }
}
