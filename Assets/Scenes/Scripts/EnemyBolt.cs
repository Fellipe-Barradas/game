using UnityEngine;

public class EnemyBolt : MonoBehaviour
{
    public int dano = 15;
    public float velocidade = 14f;
    public float tempoVida = 4f;

    private Vector3 direcao;

    void Start()
    {
        Destroy(gameObject, tempoVida);
    }

    public void DefinirDirecao(Vector3 dir)
    {
        direcao = dir.normalized;
    }

    void Update()
    {
        transform.position += direcao * velocidade * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (other.gameObject.layer == 6) return; // ignora outros inimigos

        if (other.CompareTag("Player"))
        {
            CombatScript cs = other.GetComponent<CombatScript>()
                           ?? other.GetComponentInParent<CombatScript>()
                           ?? other.GetComponentInChildren<CombatScript>();
            cs?.TakeDamage(dano);
        }

        Destroy(gameObject);
    }

    // Cria o bolt completo sem necessidade de prefab
    public static EnemyBolt Criar(int dano, float velocidade)
    {
        GameObject go = new GameObject("EnemyBolt");
        go.layer = 6; // layer enemy para ignorar outros inimigos

        // Visual: esfera com emissão
        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "BoltVisual";
        esfera.transform.SetParent(go.transform, false);
        esfera.transform.localScale = Vector3.one * 0.22f;
        Destroy(esfera.GetComponent<SphereCollider>());

        Material mat = new Material(BuscarShader());
        mat.color = new Color(1f, 0.35f, 0.05f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", new Color(2.5f, 0.6f, 0f));
        esfera.GetComponent<Renderer>().material = mat;

        // Trail para efeito de rastro
        TrailRenderer trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.22f;
        trail.startWidth = 0.16f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.5f, 0.05f, 1f);
        trail.endColor = new Color(1f, 0.15f, 0f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Collider trigger
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.radius = 0.18f;
        col.isTrigger = true;

        // Rigidbody cinemático — necessário para trigger disparar com colisores estáticos
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        EnemyBolt bolt = go.AddComponent<EnemyBolt>();
        bolt.dano = dano;
        bolt.velocidade = velocidade;

        return bolt;
    }

    static Shader BuscarShader() =>
        Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
}
