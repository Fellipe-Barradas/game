# Spec: Sistema de Tier de Arma

**Data:** 2026-05-13  
**Status:** Aprovado

---

## Visão Geral

Substituição completa do `UpgradeArma.cs` por um sistema de Tier modular (1–5) com multiplicador de dano progressivo, consumo de Shards (Fragmentos) e UI dedicada no painel de inventário.

---

## Arquitetura

Três scripts com responsabilidades isoladas + modificação mínima no `CombatScript`.

```
WeaponTierConfig (ScriptableObject)
│  Dados puros: array de TierData { tier, multiplier, shardCost }
│  Criado via: Assets > Create > Game/Weapon Tier Config
│
WeaponTierManager (MonoBehaviour — GameObject do Player)
│  Estado: currentTier (1–5)
│  Lógica: TryUpgrade() → valida Fragmentos → gasta → sobe tier → notifica
│  Evento: OnTierChanged(int tier)
│  Salvar/carregar: PlayerPrefs preparado (não ativo por padrão)
│
WeaponTierUI (MonoBehaviour — painel de inventário)
│  Referências: TMP_Text tierText, TMP_Text costText, Button upgradeButton
│  Escuta OnTierChanged → atualiza textos e estado do botão
│
CombatScript (modificação mínima)
   Novo campo: float tierDamageMultiplier = 1f
   Cálculo novo: damage = (int)(baseDamage * tierDamageMultiplier) + bonusDanoUpgrade
```

**Fluxo do upgrade:**  
`Botão clicado → WeaponTierUI.OnUpgradeClicked() → WeaponTierManager.TryUpgrade() → GerenciadorMoedas.GastarFragmentos() → OnTierChanged disparado → WeaponTierUI atualiza → CombatScript.tierDamageMultiplier atualizado`

---

## Dados dos Tiers

Centralizados no `WeaponTierConfig` (ScriptableObject), editáveis no Inspector:

| Tier | Multiplicador | Custo (Fragmentos) |
|------|--------------|-------------------|
| 1    | 1.0×         | — (estado inicial) |
| 2    | 1.5×         | 1                 |
| 3    | 2.0×         | 2                 |
| 4    | 3.0×         | 4                 |
| 5    | 5.0×         | 8 → MAX           |

---

## UI

**Hierarquia no Canvas do Inventário:**
```
InventoryPanel
└── WeaponTierPanel
    ├── TierText          (TMP_Text)  → "Arma Tier 1"
    ├── CostText          (TMP_Text)  → "Custo: 1 Shard" / "MAX"
    └── UpgradeButton     (Button)
        └── ButtonLabel   (TMP_Text)  → "Upgrade"
```

**Estados da UI:**

| Estado | TierText | CostText | Botão |
|--------|----------|----------|-------|
| Tier 1–4, shards suficientes | "Arma Tier X" | "Custo: X Shards" | Interativo |
| Tier 1–4, shards insuficientes | "Arma Tier X" | "Custo: X Shards" | Interativo (feedback no clique) |
| Tier 5 (MAX) | "Arma Tier 5" | "MAX" | Desabilitado |

**Feedback de shards insuficientes:** `Debug.Log` na primeira versão. Campo `[SerializeField]` preparado para painel de feedback visual futuro.

---

## Arquivos

| Ação | Arquivo |
|------|---------|
| Criar | `Assets/Scenes/Scripts/WeaponTierConfig.cs` |
| Criar | `Assets/Scenes/Scripts/WeaponTierManager.cs` |
| Criar | `Assets/Scenes/UI/Scripts/WeaponTierUI.cs` |
| Modificar | `Assets/Scenes/Scripts/CombatScript.cs` |
| Deletar | `Assets/Scenes/Scripts/UpgradeArma.cs` |

---

## Fora de Escopo

- Salvar/carregar tier (PlayerPrefs preparado mas não implementado)
- Feedback visual de "shards insuficientes" além de Debug.Log
- Tier acima de 5

---

## Melhorias Futuras

- `PlayerPrefs.SetInt("WeaponTier", currentTier)` no `WeaponTierManager` para persistência
- Efeito de partícula/som no upgrade
- Painel "Shards insuficientes" com animação
- Tier por tipo de arma (cada `WeaponData` com seu próprio `WeaponTierConfig`)
