# Sistema de baús data-driven com probabilidades e loot

**Data:** 2026-06-08
**Status:** Design aprovado — pronto para plano de implementação

## Problema

Hoje o spawn de baú é dirigido por `ChestMarker` + um único `defaultChestPrefab` no
`DungeonGenerator`. Existem **dois** sistemas de baú concorrentes e desconectados:

- `ChestController.cs` — interação por polling da tecla R, fora do `IInteractable`, **sem loot**.
- `Bau.cs` — `IInteractable` (tecla E via `PlayerInteraction`), com probabilidades de loot
  **chumbadas no código** (prata 80%, ouro 50%, fragmentos 10/30%).

Dores:
1. Não há controle de **qual** baú aparece (raridade/tier) — só um prefab default.
2. Probabilidades não são editáveis sem mexer no script.
3. Trocar o modelo do baú exige novo prefab + religar o script certo manualmente.
4. Loot só dá moedas; não integra com itens de inventário (`ItemSO`/`Inventory`).

## Objetivo

Unificar tudo num sistema **data-driven** que espelha o padrão já existente para inimigos
(`EncounterTable` + `EnemySO` + `Pick(...)`), com três camadas de probabilidade editáveis
em assets, e que integra moedas **e** itens.

## Arquitetura

### `ChestSO` (espelha `EnemySO`) — menu `Dungeon/Chest`

Um asset por tipo de baú. Carrega o modelo **e** os dados de loot.

```csharp
[CreateAssetMenu(menuName = "Dungeon/Chest", fileName = "Chest")]
public class ChestSO : ScriptableObject
{
    public string displayName;
    public GameObject prefab;          // modelo do baú (visual + Bau + Animator)
    public float weight = 1f;          // PROB #2 — peso no sorteio "qual baú"
    public int minDepth = 0;           // profundidade mínima para aparecer

    // PROB #3 — conteúdo (cada drop tem chance% + quantidade)
    public CoinDrop prata;
    public CoinDrop ouro;
    public CoinDrop fragmentos;
    public List<ItemDrop> itens = new List<ItemDrop>();
}

[System.Serializable]
public struct CoinDrop
{
    [Range(0, 100)] public float chance;   // chance de dropar
    public int amount;                     // quantidade se dropar
}

[System.Serializable]
public struct ItemDrop
{
    public ItemSO item;
    [Range(0, 100)] public float chance;
    public int amount;                     // quantidade (>= 1)
}
```

### `ChestTable` (espelha `EncounterTable`) — menu `Dungeon/Chest Table`

```csharp
[CreateAssetMenu(menuName = "Dungeon/Chest Table", fileName = "ChestTable")]
public class ChestTable : ScriptableObject
{
    public List<ChestSO> chests = new List<ChestSO>();

    /// Sorteio ponderado entre os ChestSO elegíveis (minDepth <= depth). null se nenhum.
    public ChestSO Pick(int depth, System.Random rng);
}
```

Lógica de `Pick` idêntica à de `EncounterTable.Pick` (acumula `weight`, gate por `minDepth`),
porém **sem** conceito de budget — é 1 baú por marker.

### `Bau` (refatorado, continua `IInteractable`)

```csharp
public class Bau : MonoBehaviour, IInteractable
{
    public ChestSO data;     // injetado no spawn pelo RoomPopulator

    private bool jaFoiAberto;
    public string ActionLabel => "open";
    public bool CanInteract => !jaFoiAberto;

    public void Interact();  // sorteia o loot de 'data', aplica, desativa o baú
}
```

`Interact()`:
- Para cada `CoinDrop`, rola `UnityEngine.Random.Range(0,100)`; se passar, soma a quantidade.
- Chama `GerenciadorMoedas.Instancia?.AdicionarDrops(prata, ouro, fragmentos)`.
- Para cada `ItemDrop`, rola a chance; se passar, acha o `Inventory` do player (tag `Player`)
  e chama `inventory.AddItem(item, amount)`.
- `gameObject.SetActive(false)` ao final (como hoje).
- As probabilidades chumbadas saem de `Bau` e passam a vir de `data`.

### `ChestMarker` (vira data-driven)

```csharp
public class ChestMarker : MonoBehaviour
{
    [Range(0, 100)] public float spawnChance = 100f;  // PROB #1 — aparece nesta posição?
    public ChestSO chestOverride;                     // força um baú específico (ignora a tabela)
    // gizmo igual ao atual
}
```

Substitui o campo antigo `chestPrefabOverride` (GameObject) por `chestOverride` (ChestSO).

## Fluxo de spawn (`RoomPopulator`)

Substitui o bloco atual de baús:

```csharp
foreach (ChestMarker c in roomInstance.GetComponentsInChildren<ChestMarker>(true))
{
    // PROB #1 — aparece?
    if (rng.NextDouble() * 100.0 >= c.spawnChance) continue;

    // PROB #2 — qual baú? (override do marker tem prioridade sobre a tabela)
    ChestSO pick = c.chestOverride != null ? c.chestOverride : chestTable?.Pick(depth, rng);
    if (pick == null || pick.prefab == null) continue;

    GameObject go = Instantiate(pick.prefab, c.transform.position, c.transform.rotation, roomInstance.transform);
    if (go.TryGetComponent<Bau>(out var bau)) bau.data = pick;
}
```

`RoomPopulator`: remove `defaultChestPrefab`, adiciona `public ChestTable chestTable;`.

## `DungeonGenerator`

- Remove `public GameObject defaultChestPrefab;`.
- Adiciona `public ChestTable chestTable;`.
- No `Generate()`, repassa ao populator igual já faz com `encounterTable`:
  `populator.chestTable = chestTable;`.

## Limpeza

- **Apagar `ChestController.cs`** (legado, sem loot, fora do `IInteractable`).
- Remover `defaultChestPrefab` de `RoomPopulator` e `DungeonGenerator`.
- Remover probabilidades chumbadas de `Bau`.
- Verificar baús existentes na cena/prefabs: se algum usa `ChestController`, migrar para
  `Bau` + `ChestSO`. Garantir que o prefab de baú tenha o componente `Bau`.

## As três probabilidades — resumo

| Probabilidade | Onde mora | Quem aplica |
|---|---|---|
| #1 Aparece ou não | `ChestMarker.spawnChance` (0-100, por posição) | `RoomPopulator` (no spawn, `rng`) |
| #2 Qual baú/tier | `weight` em cada `ChestSO` | `ChestTable.Pick` (no spawn, `rng`) |
| #3 O que tem dentro | `chance` de cada `CoinDrop`/`ItemDrop` no `ChestSO` | `Bau.Interact` (na abertura) |

## Determinismo

- **Posição e qual baú**: sorteados na geração com o `rng` por seed → reproduzíveis.
- **Conteúdo**: sorteado quando o player abre (`UnityEngine.Random`), igual ao comportamento
  atual. Não faz parte do determinismo de layout. Decisão: manter assim (mais simples).

## Fora de escopo (YAGNI)

- Pré-sorteio determinístico do conteúdo por seed.
- Quantidades min/max por drop (usa quantidade fixa por enquanto).
- Faixas escalonadas de fragmentos (o designer modela via `chance` + `amount`).
- UI de "loot recebido" (mantém o `Debug.Log`/fluxo atual de moedas).

## Como resolve as dores

- Trocar modelo = prefab variant com a malha nova + o mesmo script `Bau` (ou só apontar
  `ChestSO.prefab`). Sem religar lógica.
- Mudar loot/probabilidade = editar o asset `ChestSO`.
- Mudar raridade = ajustar `weight` na `ChestTable`.
- Loot integra moedas **e** itens de inventário.
