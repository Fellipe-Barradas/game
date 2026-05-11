using UnityEngine;
using UnityEngine.InputSystem;

public class CombatScript : MonoBehaviour
{
    [Header("Arma Equipada")]
    public WeaponData currentWeapon;

    [Header("Configuração de Acerto — Corpo a Corpo")]
    public Transform meleeAttackPoint;
    public LayerMask enemyLayers;
    public float swordAttackRange = 2f;
    public float swordAttackWidth = 3f;
    public float lanceAttackRange = 4f;
    public float lanceAttackWidth = 1f;

    [Header("Configuração de Acerto — Distância")]
    public Transform rangedFirePoint;
    public GameObject projectilePrefab;

    [Header("Feedbacks Visuais")]
    public ParticleSystem hitSparks;

    [Header("Sons")]
    public AudioSource audioSource;
    public AudioClip blockSound;
    public AudioClip hurtSound;

    public bool isBlocking { get; private set; }

    private float nextAttackTime;
    private bool isChargingShot = false;
    private FireKnightController controller;

    private void Start()
    {
        // Removida a inicialização de vida daqui, pois agora está no PlayerHealth
        controller = GetComponent<FireKnightController>();
    }

    private void Update()
    {
        GameStateManager stateManager = GameStateManager.Instance;
        if (stateManager != null && !stateManager.CanPlayerAct)
        {
            isBlocking = false;
            CancelAim();
            return;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (controller != null && controller.currentClass == PlayerClass.Arqueiro)
        {
            HandleArcherInput(mouse);
            return;
        }

        isBlocking = mouse.rightButton.isPressed;

        if (!isBlocking && mouse.leftButton.wasPressedThisFrame)
        {
            if (Time.time >= nextAttackTime)
            {
                float rate     = currentWeapon != null ? currentWeapon.attackRate : 1f;
                nextAttackTime = Time.time + 1f / rate;
                Attack();
            }
        }
    }

    private void HandleArcherInput(Mouse mouse)
    {
        if (mouse.leftButton.wasPressedThisFrame && !isChargingShot)
        {
            isChargingShot = true;
            controller.SetAiming(true);

            if (audioSource != null && currentWeapon?.swingSound != null)
                audioSource.PlayOneShot(currentWeapon.swingSound);
        }

        if (mouse.leftButton.wasReleasedThisFrame && isChargingShot)
        {
            isChargingShot = false;
            controller.SetAiming(false);
            
            float rate = currentWeapon != null ? currentWeapon.attackRate : 1f;
            float animDuration = 1f / rate;
            
            controller.TriggerAttackAnimation();
            nextAttackTime = Time.time + animDuration;
        }

        if (mouse.rightButton.wasPressedThisFrame && isChargingShot)
            CancelAim();
    }

    private void CancelAim()
    {
        if (!isChargingShot) return;
        isChargingShot = false;
        controller.SetAiming(false);
    }

    private void Attack()
    {
        if (controller != null)
            controller.TriggerAttackAnimation();

        if (audioSource != null && currentWeapon?.swingSound != null)
            audioSource.PlayOneShot(currentWeapon.swingSound);
    }

    public void ExecuteAttackEvent()
    {
         Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        if (controller == null) return;

        if (controller.currentClass == PlayerClass.Arqueiro){
            Debug.Log("ccccccccccccccccccccccccccccccc");
            ShootProjectile();
        }
        else
            Debug.Log("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            PerformMeleeAttack();
    }

    private void PerformMeleeAttack()
    {
       
        if (meleeAttackPoint == null) return;

        Vector3 hitboxSize = controller.currentClass == PlayerClass.Espadachim
            ? new Vector3(swordAttackWidth, 2f, swordAttackRange)
            : new Vector3(lanceAttackWidth, 2f, lanceAttackRange);

        Vector3 boxCenter = meleeAttackPoint.position + meleeAttackPoint.forward * (hitboxSize.z / 2f);

        Collider[] hits = Physics.OverlapBox(boxCenter, hitboxSize / 2f, meleeAttackPoint.rotation, enemyLayers);
        
        Debug.Log($"[MELEE ATTACK] Acertou {hits.Length} inimigo(s)!");

        int damage = currentWeapon != null ? currentWeapon.attackDamage : 10;

        foreach (Collider enemy in hits)
        {
            Debug.Log($"[HIT] Inimigo atingido: {enemy.name} - Dano: {damage}");
            
            if (audioSource != null && currentWeapon?.hitSound != null)
                audioSource.PlayOneShot(currentWeapon.hitSound);

            if (hitSparks != null)
                Instantiate(hitSparks, enemy.ClosestPoint(transform.position), Quaternion.identity);

            if (enemy.TryGetComponent<EnemyDummy>(out EnemyDummy e))
                e.TakeDamage(damage);
        }
    }

    private void ShootProjectile()
    {
        if (rangedFirePoint == null || projectilePrefab == null) return;
        
        // Instancia a flecha e guarda a referência dela na variável 'projectile'
        GameObject projectile = Instantiate(projectilePrefab, rangedFirePoint.position, rangedFirePoint.rotation);
        
        // Pega o script da flecha recém-criada e passa o valor do dano
        ProjectileScript projScript = projectile.GetComponent<ProjectileScript>();
        if (projScript != null)
        {
            projScript.damage = currentWeapon != null ? currentWeapon.attackDamage : 10;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isBlocking)
        {
            if (audioSource != null && blockSound != null)
                audioSource.PlayOneShot(blockSound);
            Debug.Log("Dano bloqueado!");
            return;
        }

        if (controller != null && controller.isInvincible) return;

        if (audioSource != null && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);

        // AQUI ESTÁ A LIGAÇÃO: Avisa o script de saúde que o dano passou pelo escudo
        PlayerHealth scriptDeVida = GetComponent<PlayerHealth>();
        if (scriptDeVida != null)
        {
            scriptDeVida.ReceberDano(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (meleeAttackPoint == null || controller == null) return;

        Gizmos.color = controller.currentClass switch
        {
            PlayerClass.Espadachim => Color.red,
            PlayerClass.Lanceiro   => Color.blue,
            _                      => Color.green
        };

        if (controller.currentClass == PlayerClass.Arqueiro) return;

        Vector3 hitboxSize = controller.currentClass == PlayerClass.Espadachim
            ? new Vector3(swordAttackWidth, 2f, swordAttackRange)
            : new Vector3(lanceAttackWidth, 2f, lanceAttackRange);

        Vector3 boxCenter = meleeAttackPoint.position + meleeAttackPoint.forward * (hitboxSize.z / 2f);

        Gizmos.matrix = Matrix4x4.TRS(boxCenter, meleeAttackPoint.rotation, hitboxSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}