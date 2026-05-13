# Weapon Tier System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir `UpgradeArma.cs` por um sistema de Tier de arma (1–5) modular com multiplicador de dano progressivo e consumo de Fragmentos.

**Architecture:** `WeaponTierConfig` (ScriptableObject) centraliza os dados de tier. `WeaponTierManager` (MonoBehaviour no Player) gerencia estado e lógica. `WeaponTierUI` (MonoBehaviour no inventário) escuta o evento `OnTierChanged` e atualiza a UI. `CombatScript` recebe `tierDamageMultiplier` e aplica na fórmula de dano.

**Tech Stack:** Unity 6000.3.11f1, C#, TextMeshPro, Unity UI (Button), GerenciadorMoedas (Fragmentos como Shards)

---

## Mapa de Arquivos

| Ação | Arquivo | Responsabilidade |
|------|---------|-----------------|
| Criar | `Assets/Scenes/Scripts/WeaponTierConfig.cs` | ScriptableObject com dados dos tiers |
| Criar | `Assets/Scenes/Scripts/WeaponTierManager.cs` | Estado, lógica de upgrade, evento |
| Criar | `Assets/Scenes/UI/Scripts/WeaponTierUI.cs` | Atualiza TMP_Texts e Button |
| Modificar | `Assets/Scenes/Scripts/CombatScript.cs` | Adiciona `tierDamageMultiplier`, atualiza cálculo de dano |
| Deletar | `Assets/Scenes/Scripts/UpgradeArma.cs` | Substituído pelo novo sistema |

---

## Task 1: WeaponTierConfig (ScriptableObject)

**Files:**
- Create: `Assets/Scenes/Scripts/WeaponTierConfig.cs`

- [ ] **Step 1: Criar o arquivo WeaponTierConfig.cs**

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponTierConfig", menuName = "Game/Weapon Tier Config")]
public class WeaponTierConfig : ScriptableObject
{
    [System.Serializable]
    public struct TierData
    {
        [Min(1)] public int tier;
        [Min(0f)] public float damageMultiplier;
        [Min(0)] public int shardCost;
    }

    [SerializeField] private TierData[] tiers = new TierData[]
    {
        new TierData { tier = 1, damageMultiplier = 1.0f, shardCost = 0 },
        new TierData { tier = 2, damageMultiplier = 1.5f, shardCost = 1 },
        new TierData { tier = 3, damageMultiplier = 2.0f, shardCost = 2 },
        new TierData { tier = 4, damageMultiplier = 3.0f, shardCost = 4 },
        new TierData { tier = 5, damageMultiplier = 5.0f, shardCost = 8 },
    };

    public int TierCount => tiers.Length;

    public TierData GetTierData(int tier)
    {
        int index = Mathf.Clamp(tier - 1, 0, tiers.Length - 1);
        return tiers[index];
    }

    public float GetMultiplier(int tier) => GetTierData(tier).damageMultiplier;

    // Retorna o custo para subir DO tier atual para o próximo
    public int GetUpgradeCost(int currentTier)
    {
        if (currentTier >= TierCount) return 0;
        return GetTierData(currentTier + 1).shardCost;
    }
}
```

- [ ] **Step 2: Verificar compilação**

Salve o arquivo. No Console do Unity Editor, confirme que não há erros de compilação.

- [ ] **Step 3: Criar o asset no Editor**

No Project: clique com botão direito em `Assets/Scenes` → `Create > Game > Weapon Tier Config`. Nomeie `WeaponTierConfig`. Verifique no Inspector que os 5 tiers aparecem com os valores corretos.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/Scripts/WeaponTierConfig.cs
git commit -m "feat: adiciona WeaponTierConfig ScriptableObject com dados dos 5 tiers"
```

---

## Task 2: WeaponTierManager

**Files:**
- Create: `Assets/Scenes/Scripts/WeaponTierManager.cs`

- [ ] **Step 1: Criar o arquivo WeaponTierManager.cs**

```csharp
using System;
using UnityEngine;

public class WeaponTierManager : MonoBehaviour
{
    public static WeaponTierManager Instance { get; private set; }

    [Header("Configuração")]
    [SerializeField] private WeaponTierConfig config;

    [Header("Feedback (opcional)")]
    [SerializeField] private GameObject feedbackInsuficiente;

    // Chave para persistência futura via PlayerPrefs
    private const string SaveKey = "WeaponTier";

    public int CurrentTier { get; private set; } = 1;
    public int MaxTier => config.TierCount;
    public bool IsMaxTier => CurrentTier >= MaxTier;
    public int UpgradeCost => config.GetUpgradeCost(CurrentTier);
    public float CurrentMultiplier => config.GetMultiplier(CurrentTier);

    // Disparado sempre que o tier muda (UI e outros sistemas escutam)
    public event Action<int> OnTierChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Para ativar persistência: CurrentTier = PlayerPrefs.GetInt(SaveKey, 1);
        ApplyTierToCombat();
        OnTierChanged?.Invoke(CurrentTier);
    }

    public bool TryUpgrade()
    {
        if (IsMaxTier) return false;

        var gm = GerenciadorMoedas.Instancia;
        if (gm == null) return false;

        int cost = UpgradeCost;
        if (!gm.GastarFragmentos(cost))
        {
            Debug.Log($"[WeaponTier] Fragmentos insuficientes. Necessário: {cost}, disponível: {gm.Fragmentos}");
            if (feedbackInsuficiente != null) feedbackInsuficiente.SetActive(true);
            return false;
        }

        CurrentTier++;
        // Para ativar persistência: PlayerPrefs.SetInt(SaveKey, CurrentTier);
        ApplyTierToCombat();
        OnTierChanged?.Invoke(CurrentTier);
        Debug.Log($"[WeaponTier] Upgrade! Tier {CurrentTier}, multiplicador: {CurrentMultiplier}x");
        return true;
    }

    private void ApplyTierToCombat()
    {
        if (TryGetComponent(out CombatScript combat))
            combat.tierDamageMultiplier = CurrentMultiplier;
    }
}
```

- [ ] **Step 2: Verificar compilação**

Salve. No Console do Unity, confirme sem erros. Haverá warning de `tierDamageMultiplier` não existir em `CombatScript` — isso é esperado, será resolvido na Task 4.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scenes/Scripts/WeaponTierManager.cs
git commit -m "feat: adiciona WeaponTierManager com logica de upgrade e evento OnTierChanged"
```

---

## Task 3: WeaponTierUI

**Files:**
- Create: `Assets/Scenes/UI/Scripts/WeaponTierUI.cs`

- [ ] **Step 1: Criar o arquivo WeaponTierUI.cs**

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponTierUI : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button upgradeButton;

    [Header("Textos Configuráveis")]
    [SerializeField] private string tierPrefix = "Arma Tier ";
    [SerializeField] private string costPrefix = "Custo: ";
    [SerializeField] private string costSuffix = " Shards";
    [SerializeField] private string maxLabel = "MAX";

    private WeaponTierManager tierManager;

    private void Start()
    {
        tierManager = WeaponTierManager.Instance;
        if (tierManager == null)
        {
            Debug.LogError("[WeaponTierUI] WeaponTierManager.Instance não encontrado. Adicione WeaponTierManager ao Player.");
            return;
        }

        tierManager.OnTierChanged += Refresh;
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        Refresh(tierManager.CurrentTier);
    }

    private void OnDestroy()
    {
        if (tierManager != null)
            tierManager.OnTierChanged -= Refresh;
    }

    private void OnUpgradeClicked() => tierManager.TryUpgrade();

    private void Refresh(int tier)
    {
        tierText.text = tierPrefix + tier;

        if (tierManager.IsMaxTier)
        {
            costText.text = maxLabel;
            upgradeButton.interactable = false;
        }
        else
        {
            costText.text = costPrefix + tierManager.UpgradeCost + costSuffix;
            upgradeButton.interactable = true;
        }
    }
}
```

- [ ] **Step 2: Verificar compilação**

Salve. No Console do Unity, confirme sem erros de compilação.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scenes/UI/Scripts/WeaponTierUI.cs
git commit -m "feat: adiciona WeaponTierUI com TMP e Button, escuta OnTierChanged"
```

---

## Task 4: Modificar CombatScript

**Files:**
- Modify: `Assets/Scenes/Scripts/CombatScript.cs`

- [ ] **Step 1: Adicionar campo tierDamageMultiplier no header "Status de Upgrade"**

Localize o bloco:
```csharp
[Header("Status de Upgrade")]
public int bonusDanoUpgrade = 0;
```

Substitua por:
```csharp
[Header("Status de Upgrade")]
public int bonusDanoUpgrade = 0;
public float tierDamageMultiplier = 1f;
```

- [ ] **Step 2: Atualizar cálculo de dano em PerformMeleeAttack**

Localize:
```csharp
int damage = (currentWeapon != null ? currentWeapon.attackDamage : 10) + bonusDanoUpgrade;
```

Substitua por:
```csharp
int damage = (int)((currentWeapon != null ? currentWeapon.attackDamage : 10) * tierDamageMultiplier) + bonusDanoUpgrade;
```

- [ ] **Step 3: Atualizar cálculo de dano em ShootProjectile**

Localize:
```csharp
projScript.damage = (currentWeapon != null ? currentWeapon.attackDamage : 10) + bonusDanoUpgrade;
```

Substitua por:
```csharp
projScript.damage = (int)((currentWeapon != null ? currentWeapon.attackDamage : 10) * tierDamageMultiplier) + bonusDanoUpgrade;
```

- [ ] **Step 4: Verificar compilação**

Salve. No Console do Unity, confirme zero erros. O warning de `tierDamageMultiplier` na Task 2 deve desaparecer.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scenes/Scripts/CombatScript.cs
git commit -m "feat: adiciona tierDamageMultiplier ao CombatScript com formula multiplicativa"
```

---

## Task 5: Remover UpgradeArma.cs

**Files:**
- Delete: `Assets/Scenes/Scripts/UpgradeArma.cs`
- Delete: `Assets/Scenes/Scripts/UpgradeArma.cs.meta`

- [ ] **Step 1: Deletar o arquivo pelo Unity Editor**

No Project do Unity, clique com botão direito em `Assets/Scenes/Scripts/UpgradeArma.cs` → **Delete**. Deixe o Unity deletar o `.meta` junto.

> Não delete pelo Windows Explorer — o Unity não sincronizaria o `.meta` corretamente.

- [ ] **Step 2: Verificar se algum GameObject referenciava UpgradeArma**

No Console do Unity, procure por `MissingMonoBehaviour` ou `MissingComponent`. Se aparecer, selecione o GameObject indicado na Hierarquia e remova o componente quebrado no Inspector.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "refactor: remove UpgradeArma substituido pelo sistema de tier"
```

---

## Task 6: Configuração no Unity Editor

> Esta task é manual no Editor — sem código.

- [ ] **Step 1: Adicionar WeaponTierManager ao Player**

Na Hierarquia, selecione o GameObject **Player**. No Inspector, clique em `Add Component` → procure `WeaponTierManager`. No campo **Config**, arraste o asset `WeaponTierConfig` criado na Task 1.

- [ ] **Step 2: Criar a hierarquia de UI no Canvas do Inventário**

No Canvas do Inventário, crie a seguinte hierarquia:

```
WeaponTierPanel          (GameObject vazio com RectTransform)
├── TierText             (UI > Text - TextMeshPro)
├── CostText             (UI > Text - TextMeshPro)
└── UpgradeButton        (UI > Button - TextMeshPro)
    └── Text (TMP)       → "Upgrade"
```

- [ ] **Step 3: Adicionar e configurar WeaponTierUI**

Selecione `WeaponTierPanel`. Clique em `Add Component` → `WeaponTierUI`. Arraste nos campos:
- **Tier Text** → `TierText`
- **Cost Text** → `CostText`
- **Upgrade Button** → `UpgradeButton`

Deixe os campos de texto com os valores padrão (`"Arma Tier "`, `"Custo: "`, `" Shards"`, `"MAX"`).

- [ ] **Step 4: Verificar em Play Mode**

Entre em Play Mode. Abra o inventário. Verifique:
- `TierText` exibe **"Arma Tier 1"**
- `CostText` exibe **"Custo: 1 Shards"**
- Botão está **interativo**

Com Fragmentos suficientes (`GerenciadorMoedas.Instancia` → adicione via Inspector ou pelo sistema de drops), clique em Upgrade. Verifique:
- Tier sobe para 2
- `CostText` muda para **"Custo: 2 Shards"**
- No Console aparece `[WeaponTier] Upgrade! Tier 2, multiplicador: 1,5x`

Suba até Tier 5. Verifique:
- `CostText` exibe **"MAX"**
- Botão fica **desabilitado**

- [ ] **Step 5: Verificar dano multiplicado**

No Inspector em Play Mode, com Tier 2 ativo, verifique que `CombatScript.tierDamageMultiplier` exibe `1.5`. Ataque um inimigo e confirme no Console que o dano é `(attackDamage * 1.5) + bonusDanoUpgrade`.

- [ ] **Step 6: Commit final**

```bash
git add -A
git commit -m "feat: sistema de tier de arma completo e configurado na cena"
```
