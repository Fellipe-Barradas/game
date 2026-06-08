# Sistema de baús data-driven — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir os dois sistemas de baú (legado `ChestController` + `Bau` com probabilidades chumbadas) por um sistema data-driven que decide qual baú aparece (ponderado), se aparece, e o que dropa (moedas + itens) — tudo via ScriptableObjects.

**Architecture:** Espelha o padrão `EncounterTable`/`EnemySO` já usado para inimigos. A matemática de sorteio ponderado é extraída para `Game.Dungeon.WeightedPicker` (assembly Core, puro e testável). `ChestSO` carrega modelo + loot; `ChestTable` faz o `Pick` ponderado; `Bau` vira data-driven; `RoomPopulator` injeta o `ChestSO` no spawn.

**Tech Stack:** Unity 6000.3.11f1, C#, NUnit (Unity Test Framework, EditMode).

---

## Como rodar os testes (Unity não tem CLI de testes neste projeto)

Os testes EditMode rodam pelo Editor:
1. Unity aberto → menu **Window → General → Test Runner**.
2. Aba **EditMode** → **Run All** (ou clicar no teste específico → Run Selected).
3. Verde = passou, vermelho = falhou (detalhe no painel inferior).

Após criar/editar `.cs`, volte ao Editor e **espere recompilar** (canto inferior direito) antes de rodar.

---

## Estrutura de arquivos

**Criar:**
- `Assets/Scenes/Scripts/Dungeon/Core/WeightedPicker.cs` — sorteio ponderado puro (Core, testável).
- `Assets/Scenes/Scripts/Dungeon/ChestSO.cs` — dados de um tipo de baú (modelo + loot).
- `Assets/Scenes/Scripts/Dungeon/ChestTable.cs` — lista ponderada de `ChestSO` com `Pick`.
- `Assets/Tests/EditMode/WeightedPickerTests.cs` — testes do `WeightedPicker`.

**Modificar:**
- `Assets/MoedaSistema/Bau.cs` — vira data-driven (lê de `ChestSO`).
- `Assets/Scenes/Scripts/Dungeon/ChestMarker.cs` — `spawnChance` + `chestOverride`.
- `Assets/Scenes/Scripts/Dungeon/RoomPopulator.cs` — usa `ChestTable`, injeta `data`.
- `Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs` — troca `defaultChestPrefab` por `chestTable`.

**Apagar:**
- `Assets/Scenes/Scripts/ChestController.cs` (+ `.meta`) — legado, sem loot, fora do `IInteractable`. Verificado: nenhum prefab/cena o referencia.

---

## Task 1: WeightedPicker (Core, puro e testável)

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/Core/WeightedPicker.cs`
- Test: `Assets/Tests/EditMode/WeightedPickerTests.cs`

- [ ] **Step 1: Escrever o teste que falha**

Create `Assets/Tests/EditMode/WeightedPickerTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Game.Dungeon;

public class WeightedPickerTests
{
    [Test]
    public void Pesos_Iguais_RollDistribuiPorBucket()
    {
        var w = new List<float> { 1, 1, 1 };
        Assert.AreEqual(0, WeightedPicker.PickIndex(w, 0.0));
        Assert.AreEqual(1, WeightedPicker.PickIndex(w, 0.5));
        Assert.AreEqual(2, WeightedPicker.PickIndex(w, 0.9));
    }

    [Test]
    public void Peso_Zero_NuncaEscolhido()
    {
        var w = new List<float> { 0, 5, 0 };
        Assert.AreEqual(1, WeightedPicker.PickIndex(w, 0.0));
        Assert.AreEqual(1, WeightedPicker.PickIndex(w, 0.99));
    }

    [Test]
    public void Pesos_Desiguais_RespeitamProporcao()
    {
        var w = new List<float> { 3, 1 }; // fronteira em 0.75
        Assert.AreEqual(0, WeightedPicker.PickIndex(w, 0.0));
        Assert.AreEqual(0, WeightedPicker.PickIndex(w, 0.74));
        Assert.AreEqual(1, WeightedPicker.PickIndex(w, 0.8));
    }

    [Test]
    public void TodosZero_RetornaMenosUm()
    {
        Assert.AreEqual(-1, WeightedPicker.PickIndex(new List<float> { 0, 0 }, 0.5));
    }

    [Test]
    public void ListaVazia_RetornaMenosUm()
    {
        Assert.AreEqual(-1, WeightedPicker.PickIndex(new List<float>(), 0.5));
    }
}
```

- [ ] **Step 2: Rodar e ver falhar**

Test Runner → EditMode → Run All.
Esperado: FALHA de compilação ("WeightedPicker não existe").

- [ ] **Step 3: Implementar o mínimo**

Create `Assets/Scenes/Scripts/Dungeon/Core/WeightedPicker.cs`:

```csharp
using System.Collections.Generic;

namespace Game.Dungeon
{
    /// <summary>Seleção ponderada pura e determinística (sem dependência de Unity runtime).</summary>
    public static class WeightedPicker
    {
        /// <summary>
        /// Índice escolhido em 'weights' dado um roll em [0,1). Pesos &lt;= 0 são ignorados.
        /// Retorna -1 se não houver peso positivo (ou lista vazia).
        /// </summary>
        public static int PickIndex(IReadOnlyList<float> weights, double roll01)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
                if (weights[i] > 0f) total += weights[i];

            if (total <= 0f) return -1;

            double r = roll01 * total;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                r -= weights[i];
                if (r < 0) return i;
            }

            // Salvaguarda contra arredondamento: último com peso positivo.
            for (int i = weights.Count - 1; i >= 0; i--)
                if (weights[i] > 0f) return i;
            return -1;
        }
    }
}
```

- [ ] **Step 4: Rodar e ver passar**

Test Runner → EditMode → Run All.
Esperado: 5 testes de `WeightedPickerTests` verdes (e os de `DungeonPlannerTests`/`PlacementMathTests` continuam verdes).

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scenes/Scripts/Dungeon/Core/WeightedPicker.cs" "Assets/Tests/EditMode/WeightedPickerTests.cs"
git commit -m "feat: WeightedPicker puro em Core + testes EditMode"
```

---

## Task 2: ChestSO e ChestTable

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/ChestSO.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/ChestTable.cs`

- [ ] **Step 1: Criar `ChestSO` (modelo + loot)**

Create `Assets/Scenes/Scripts/Dungeon/ChestSO.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CoinDrop
{
    [Range(0, 100)] public float chance;   // chance de dropar
    public int amount;                      // quantidade se dropar
}

[System.Serializable]
public struct ItemDrop
{
    public ItemSO item;
    [Range(0, 100)] public float chance;
    public int amount;                      // quantidade (>= 1)
}

[CreateAssetMenu(menuName = "Dungeon/Chest", fileName = "Chest")]
public class ChestSO : ScriptableObject
{
    public string displayName;
    public GameObject prefab;               // modelo do baú (visual + Bau + Animator)
    [Tooltip("Peso relativo no sorteio de qual baú aparece.")]
    public float weight = 1f;
    [Tooltip("Profundidade mínima da sala para esse baú aparecer.")]
    public int minDepth = 0;

    [Header("Loot — moedas")]
    public CoinDrop prata;
    public CoinDrop ouro;
    public CoinDrop fragmentos;

    [Header("Loot — itens")]
    public List<ItemDrop> itens = new List<ItemDrop>();
}
```

- [ ] **Step 2: Criar `ChestTable` (sorteio ponderado)**

Create `Assets/Scenes/Scripts/Dungeon/ChestTable.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

[CreateAssetMenu(menuName = "Dungeon/Chest Table", fileName = "ChestTable")]
public class ChestTable : ScriptableObject
{
    public List<ChestSO> chests = new List<ChestSO>();

    /// <summary>Sorteia um ChestSO elegível (minDepth &lt;= depth) ponderado por weight. null se nenhum.</summary>
    public ChestSO Pick(int depth, System.Random rng)
    {
        var eligible = new List<ChestSO>();
        var weights = new List<float>();
        foreach (ChestSO c in chests)
        {
            if (c == null || c.prefab == null) continue;
            if (c.minDepth > depth) continue;
            eligible.Add(c);
            weights.Add(c.weight);
        }
        if (eligible.Count == 0) return null;

        int idx = WeightedPicker.PickIndex(weights, rng.NextDouble());
        return idx >= 0 ? eligible[idx] : null;
    }
}
```

- [ ] **Step 3: Verificar compilação**

Voltar ao Unity, esperar recompilar. Console sem erros.
Confirmar menus: `Assets → Create → Dungeon → Chest` e `→ Chest Table` aparecem.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Scenes/Scripts/Dungeon/ChestSO.cs" "Assets/Scenes/Scripts/Dungeon/ChestTable.cs"
git commit -m "feat: ChestSO (modelo+loot) e ChestTable (sorteio ponderado)"
```

---

## Task 3: Refatorar `Bau` para data-driven

**Files:**
- Modify: `Assets/MoedaSistema/Bau.cs` (substitui o conteúdo inteiro)

- [ ] **Step 1: Substituir `Bau.cs`**

Replace the entire contents of `Assets/MoedaSistema/Bau.cs` with:

```csharp
using UnityEngine;

public class Bau : MonoBehaviour, IInteractable
{
    [Tooltip("Dados de loot. Injetado pelo RoomPopulator no spawn; pode servir de padrão no prefab.")]
    public ChestSO data;

    private bool jaFoiAberto = false;

    public string ActionLabel => "open";
    public bool CanInteract => !jaFoiAberto;

    public void Interact()
    {
        if (jaFoiAberto) return;
        Abrir();
    }

    private void Abrir()
    {
        jaFoiAberto = true;

        if (data == null)
        {
            Debug.LogWarning("[Bau] sem ChestSO 'data' — baú vazio.", this);
            gameObject.SetActive(false);
            return;
        }

        int dropPrata = RollCoin(data.prata);
        int dropOuro = RollCoin(data.ouro);
        int dropFragmentos = RollCoin(data.fragmentos);

        if (dropPrata > 0 || dropOuro > 0 || dropFragmentos > 0)
        {
            Debug.Log($"Baú aberto! Drops: {dropPrata} Prata | {dropOuro} Ouro | {dropFragmentos} Fragmentos");
            GerenciadorMoedas.Instancia?.AdicionarDrops(dropPrata, dropOuro, dropFragmentos);
        }

        if (data.itens != null && data.itens.Count > 0)
        {
            Inventory inv = FindPlayerInventory();
            foreach (ItemDrop d in data.itens)
            {
                if (d.item == null || d.amount <= 0) continue;
                if (Random.Range(0f, 100f) <= d.chance)
                {
                    if (inv != null) inv.AddItem(d.item, d.amount);
                    else Debug.LogWarning("[Bau] item dropado mas player sem Inventory.", this);
                }
            }
        }

        gameObject.SetActive(false);
    }

    private static int RollCoin(CoinDrop c)
    {
        if (c.amount <= 0) return 0;
        return Random.Range(0f, 100f) <= c.chance ? c.amount : 0;
    }

    private static Inventory FindPlayerInventory()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.GetComponent<Inventory>() : null;
    }
}
```

- [ ] **Step 2: Verificar compilação**

Unity recompila sem erros. O prefab `Assets/Scenes/MainScene/Prefabs/Chest Variant.prefab` (que usa `Bau`) agora mostra o campo `Data (ChestSO)` no Inspector no lugar de `Prata No Bau`/`Ouro No Bau` (os valores antigos viram lixo serializado e somem — esperado).

- [ ] **Step 3: Commit**

```bash
git add "Assets/MoedaSistema/Bau.cs"
git commit -m "refactor: Bau data-driven (loot vem do ChestSO, moedas + itens)"
```

---

## Task 4: `ChestMarker` data-driven

**Files:**
- Modify: `Assets/Scenes/Scripts/Dungeon/ChestMarker.cs` (substitui o conteúdo inteiro)

- [ ] **Step 1: Substituir `ChestMarker.cs`**

Replace the entire contents of `Assets/Scenes/Scripts/Dungeon/ChestMarker.cs` with:

```csharp
using UnityEngine;

public class ChestMarker : MonoBehaviour
{
    [Range(0f, 100f)]
    [Tooltip("Chance desta posição virar um baú no spawn.")]
    public float spawnChance = 100f;

    [Tooltip("Opcional: força um baú específico aqui (ignora a ChestTable).")]
    public ChestSO chestOverride;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
}
```

- [ ] **Step 2: Verificar compilação**

Unity recompila sem erros. (Nenhum prefab usa `ChestMarker` hoje, então não há referência quebrada ao campo antigo `chestPrefabOverride`.)

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scenes/Scripts/Dungeon/ChestMarker.cs"
git commit -m "feat: ChestMarker com spawnChance e chestOverride (ChestSO)"
```

---

## Task 5: Wiring no `RoomPopulator` e `DungeonGenerator`

**Files:**
- Modify: `Assets/Scenes/Scripts/Dungeon/RoomPopulator.cs:9` e bloco de baús (linhas 36-41)
- Modify: `Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs:21` e `:66`

- [ ] **Step 1: `RoomPopulator` — trocar o campo**

In `Assets/Scenes/Scripts/Dungeon/RoomPopulator.cs`, replace:

```csharp
    public GameObject defaultChestPrefab;
```

with:

```csharp
    public ChestTable chestTable;
```

- [ ] **Step 2: `RoomPopulator` — novo bloco de baús**

In the same file, replace the entire `// Baús.` block:

```csharp
        // Baús.
        foreach (ChestMarker c in roomInstance.GetComponentsInChildren<ChestMarker>(true))
        {
            GameObject prefab = c.chestPrefabOverride != null ? c.chestPrefabOverride : defaultChestPrefab;
            if (prefab != null)
                Instantiate(prefab, c.transform.position, c.transform.rotation, roomInstance.transform);
        }
```

with:

```csharp
        // Baús.
        foreach (ChestMarker c in roomInstance.GetComponentsInChildren<ChestMarker>(true))
        {
            // PROB #1 — esta posição vira baú?
            if (rng.NextDouble() * 100.0 >= c.spawnChance) continue;

            // PROB #2 — qual baú? (override do marker tem prioridade sobre a tabela)
            ChestSO pick = c.chestOverride != null
                ? c.chestOverride
                : (chestTable != null ? chestTable.Pick(depth, rng) : null);
            if (pick == null || pick.prefab == null) continue;

            GameObject go = Instantiate(pick.prefab, c.transform.position, c.transform.rotation, roomInstance.transform);
            if (go.TryGetComponent<Bau>(out var bau)) bau.data = pick;
        }
```

- [ ] **Step 3: `DungeonGenerator` — trocar o campo**

In `Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs`, replace:

```csharp
    public GameObject defaultChestPrefab;
```

with:

```csharp
    public ChestTable chestTable;
```

- [ ] **Step 4: `DungeonGenerator` — repassar ao populator**

In the same file, in `Generate()`, replace:

```csharp
            populator.defaultChestPrefab = defaultChestPrefab;
```

with:

```csharp
            populator.chestTable = chestTable;
```

- [ ] **Step 5: Verificar compilação**

Unity recompila sem erros. No `DungeonGenerator` da cena, o campo antigo `Default Chest Prefab` some e aparece `Chest Table` (vazio por enquanto — preenchido na Task 7).

- [ ] **Step 6: Commit**

```bash
git add "Assets/Scenes/Scripts/Dungeon/RoomPopulator.cs" "Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs"
git commit -m "feat: RoomPopulator/DungeonGenerator usam ChestTable e injetam ChestSO"
```

---

## Task 6: Apagar `ChestController` (legado)

**Files:**
- Delete: `Assets/Scenes/Scripts/ChestController.cs`
- Delete: `Assets/Scenes/Scripts/ChestController.cs.meta`

- [ ] **Step 1: Apagar os arquivos**

```bash
git rm "Assets/Scenes/Scripts/ChestController.cs" "Assets/Scenes/Scripts/ChestController.cs.meta"
```

- [ ] **Step 2: Verificar compilação**

Unity recompila sem erros. Console sem "missing script" (verificado: nenhum prefab/cena referencia o GUID `8b2665d019779e44383d3107b4e6ea5c`).

- [ ] **Step 3: Commit**

```bash
git commit -m "chore: remove ChestController legado (substituido por Bau data-driven)"
```

---

## Task 7: Autoria de assets + verificação ponta a ponta (manual, no Editor)

Esta task não tem código — é configuração no Editor e teste de jogo. É o que valida o sistema inteiro.

- [ ] **Step 1: Garantir o prefab de baú com `data`**

Selecione `Assets/Scenes/MainScene/Prefabs/Chest Variant.prefab`. Confirme que tem o componente `Bau` com o campo `Data`. (Pode deixar `Data` vazio — será injetado no spawn.)

- [ ] **Step 2: Criar um `ChestSO`**

`Assets → Create → Dungeon → Chest`. Nomeie `ChestComum`. Configure:
- `prefab` = `Chest Variant.prefab`
- `weight` = 1
- `prata`: chance 80, amount 15
- `ouro`: chance 50, amount 1
- `fragmentos`: chance 30, amount 10
- `itens`: (opcional) adicione um `ItemDrop` com um `ItemSO` existente, chance 25, amount 1.

- [ ] **Step 3: Criar a `ChestTable`**

`Assets → Create → Dungeon → Chest Table`. Nomeie `ChestTable`. Em `chests`, adicione `ChestComum`.
(Para testar raridade depois, crie um `ChestRaro` com `weight` menor e adicione à lista.)

- [ ] **Step 4: Ligar a tabela ao gerador**

Na cena `MainScene`, selecione o objeto com `DungeonGenerator`. Arraste `ChestTable` para o campo `Chest Table`.

- [ ] **Step 5: Colocar um `ChestMarker` na sala de baús**

Abra `Assets/Scenes/MainScene/Prefabs/Rooms/SalaBaus Variant.prefab` (modo prefab). Crie um GameObject vazio filho da raiz, posicione no chão onde quer o baú, e adicione o componente `ChestMarker` (deixe `spawnChance` = 100 e `chestOverride` vazio). Salve o prefab.

- [ ] **Step 6: Testar no Play**

Entre em Play, gere a masmorra, vá até a sala de baús.
Esperado:
- Um baú aparece na posição do marker.
- Aproximando, o prompt "open" (tecla E) aparece.
- Apertando E: baú abre, Console loga os drops, moedas sobem no HUD; se um item dropou, ele entra no inventário.

- [ ] **Step 7: Testar determinismo e raridade (opcional)**

No `DungeonGenerator`, desmarque `useRandomSeed` e fixe um `seed`. Gere duas vezes: a posição e o tipo de baú devem ser iguais (conteúdo pode variar, pois é sorteado na abertura).

- [ ] **Step 8: Commit dos assets**

```bash
git add Assets/Scenes/MainScene Assets/Scenes/Scripts/Dungeon/Editor 2>/dev/null; git add -A
git commit -m "feat: assets de baú (ChestComum, ChestTable) e ChestMarker na SalaBaus"
```

---

## Self-review (cobertura do spec)

- PROB #1 (aparece ou não) → `ChestMarker.spawnChance` + checagem no `RoomPopulator` (Task 4, Task 5). ✓
- PROB #2 (qual baú) → `ChestSO.weight` + `ChestTable.Pick` via `WeightedPicker` (Task 1, 2, 5). ✓
- PROB #3 (conteúdo) → `CoinDrop`/`ItemDrop.chance` em `ChestSO`, rolado em `Bau.Interact` (Task 2, 3). ✓
- Loot moedas + itens → `Bau.Abrir` chama `AdicionarDrops` e `Inventory.AddItem` (Task 3). ✓
- Determinismo de posição/tipo → `rng` compartilhado no `RoomPopulator`/`ChestTable.Pick` (Task 5). ✓
- Desacoplar modelo de lógica → todo prefab usa o mesmo `Bau`; modelo vem de `ChestSO.prefab` (Task 2, 3, 7). ✓
- Limpeza: apagar `ChestController`, remover `defaultChestPrefab` (Task 5, 6). ✓
