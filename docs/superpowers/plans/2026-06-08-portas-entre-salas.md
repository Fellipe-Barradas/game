# Portas entre salas — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Colocar portas nos vãos entre salas da masmorra procedural, que abrem por presença do player (ficam abertas) e trancam quando a sala de combate é ativada, destrancando ao ser limpa.

**Architecture:** O planner passa a expor as conexões (`Doorways`); o `DungeonBuilder` instancia o `doorPrefab` em cada vão depois do bake de NavMesh e o gerador registra cada porta nos dois `RoomController` vizinhos. `DoorController` deixa de ser `IInteractable` e vira proximidade-por-trigger com lockdown via `SetLocked` (contador).

**Tech Stack:** Unity 6000.3.11f1, C#, NUnit (Unity Test Framework, EditMode).

---

## Como rodar os testes (sem CLI neste projeto)

Os testes EditMode rodam pelo Editor: **Window → General → Test Runner → aba EditMode → Run All**. Após editar `.cs`, volte ao Editor e espere recompilar antes de rodar. Verde = passou.

---

## Estrutura de arquivos

**Modificar:**
- `Assets/Scenes/Scripts/Dungeon/Core/PlannedRoom.cs` — novo tipo `PlannedDoorway`.
- `Assets/Scenes/Scripts/Dungeon/Core/DungeonLayout.cs` — lista `Doorways`.
- `Assets/Scenes/Scripts/Dungeon/Core/DungeonPlanner.cs` — registrar vão em `TryFit`.
- `Assets/Scenes/Scripts/DoorController.cs` — reescrito (proximidade + lock).
- `Assets/Scenes/Scripts/Dungeon/RoomController.cs` — `RegisterDoor`, remover coleta por filhos.
- `Assets/Scenes/Scripts/Dungeon/DungeonBuilder.cs` — `doorPrefab`, `PlacedDoor`, `BuildDoors`.
- `Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs` — chamar `BuildDoors` + registrar.
- `Assets/Tests/EditMode/DungeonPlannerTests.cs` — teste do invariante de árvore.

**Editor (não-código):** adicionar `BoxCollider` trigger no prefab da porta, atribuir `doorPrefab`, conferir Animator, Play-test.

---

## Task 1: Planner expõe as conexões (Doorways) + teste

**Files:**
- Modify: `Assets/Scenes/Scripts/Dungeon/Core/PlannedRoom.cs`
- Modify: `Assets/Scenes/Scripts/Dungeon/Core/DungeonLayout.cs`
- Modify: `Assets/Scenes/Scripts/Dungeon/Core/DungeonPlanner.cs` (dentro de `TryFit`)
- Test: `Assets/Tests/EditMode/DungeonPlannerTests.cs`

- [ ] **Step 1: Escrever o teste que falha**

Add this test method inside the existing `DungeonPlannerTests` class in `Assets/Tests/EditMode/DungeonPlannerTests.cs` (before the closing `}` of the class):

```csharp
    [Test]
    public void Plan_DoorwaysFormamArvore()
    {
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), Settings(), seed: 5);
        Assert.IsTrue(layout.Success);
        // Masmorra é uma árvore: cada sala (exceto a inicial) entra por exatamente 1 conexão.
        Assert.AreEqual(layout.Rooms.Count - 1, layout.Doorways.Count);
        foreach (PlannedDoorway d in layout.Doorways)
        {
            Assert.Contains(d.RoomA, layout.Rooms);
            Assert.Contains(d.RoomB, layout.Rooms);
        }
    }
```

- [ ] **Step 2: Rodar e ver falhar**

Test Runner → EditMode → Run All.
Esperado: FALHA de compilação (`PlannedDoorway` e `layout.Doorways` não existem).

- [ ] **Step 3: Criar o tipo `PlannedDoorway`**

In `Assets/Scenes/Scripts/Dungeon/Core/PlannedRoom.cs`, add this class inside the `namespace Game.Dungeon { ... }` block (after the `PlannedRoom` class):

```csharp
    public class PlannedDoorway
    {
        public Vector3 WorldPosition;
        public CardinalDirection WorldDirection;
        public PlannedRoom RoomA;   // sala existente (dona do socket aberto)
        public PlannedRoom RoomB;   // sala recém-colocada
    }
```

- [ ] **Step 4: Adicionar a lista no layout**

In `Assets/Scenes/Scripts/Dungeon/Core/DungeonLayout.cs`, add this field to the `DungeonLayout` class (after the `OpenSockets` line):

```csharp
        public List<PlannedDoorway> Doorways = new List<PlannedDoorway>();
```

- [ ] **Step 5: Registrar o vão em `TryFit`**

In `Assets/Scenes/Scripts/Dungeon/Core/DungeonPlanner.cs`, in the method `TryFit`, find these lines:

```csharp
                placed.Sockets[i].Used = true;
                fe.Socket.Used = true;
                layout.Rooms.Add(placed);
                return placed;
```

Replace them with:

```csharp
                placed.Sockets[i].Used = true;
                fe.Socket.Used = true;
                layout.Rooms.Add(placed);
                layout.Doorways.Add(new PlannedDoorway
                {
                    WorldPosition = fe.Socket.WorldPosition,
                    WorldDirection = fe.Socket.WorldDirection,
                    RoomA = fe.Owner,
                    RoomB = placed,
                });
                return placed;
```

- [ ] **Step 6: Rodar e ver passar**

Test Runner → EditMode → Run All.
Esperado: `Plan_DoorwaysFormamArvore` verde, e todos os testes antigos (`DungeonPlannerTests`, `PlacementMathTests`, `WeightedPickerTests`) continuam verdes.

- [ ] **Step 7: Commit**

```bash
git add "Assets/Scenes/Scripts/Dungeon/Core/PlannedRoom.cs" "Assets/Scenes/Scripts/Dungeon/Core/DungeonLayout.cs" "Assets/Scenes/Scripts/Dungeon/Core/DungeonPlanner.cs" "Assets/Tests/EditMode/DungeonPlannerTests.cs"
git commit -m "feat: planner expoe Doorways (conexoes entre salas) + teste de arvore"
```

---

## Task 2: Reescrever `DoorController`

**Files:**
- Modify: `Assets/Scenes/Scripts/DoorController.cs` (substitui o arquivo inteiro)

- [ ] **Step 1: Substituir `DoorController.cs`**

Replace the entire contents of `Assets/Scenes/Scripts/DoorController.cs` with:

```csharp
using UnityEngine;

/// <summary>
/// Porta entre salas: abre por presença do player (gatilho de proximidade) e fica aberta.
/// Tranca/fecha sob lockdown de combate (RoomController.SetLocked via contador).
/// </summary>
public class DoorController : MonoBehaviour
{
    private Animator anim;
    private bool isOpen;
    private int lockCount;        // trancada enquanto > 0
    private bool playerInRange;

    private void Awake() => anim = GetComponent<Animator>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (lockCount == 0) Open();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;   // não fecha — fica aberta
    }

    /// <summary>Trancar fecha a porta; destrancar reabre se o player ainda estiver perto.</summary>
    public void SetLocked(bool value)
    {
        if (value)
        {
            lockCount++;
            if (lockCount == 1) Close();
        }
        else
        {
            lockCount = Mathf.Max(0, lockCount - 1);
            if (lockCount == 0 && playerInRange) Open();
        }
    }

    private void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (anim != null) anim.SetTrigger("change");
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (anim != null) anim.SetTrigger("change");
    }
}
```

- [ ] **Step 2: Verificar (inspeção)**

- First Read the current `Assets/Scenes/Scripts/DoorController.cs` to see what you're replacing (it currently implements `IInteractable` with `ActionLabel`/`CanInteract`/`Interact`).
- Confirm no other `.cs` file calls the removed members `Interact()`/`ActionLabel`/`CanInteract` **on a DoorController**. Grep for `DoorController` usage. `PlayerInteraction` uses the `IInteractable` interface generically (not DoorController by name), so removing the interface from DoorController is safe — doors simply stop being interactable. `RoomController.SetLocked` is still called and still exists. If you find a direct DoorController interaction call, STOP and report.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scenes/Scripts/DoorController.cs"
git commit -m "refactor: DoorController abre por presenca e tranca em combate (nao mais IInteractable)"
```

---

## Task 3: `RoomController` — registrar portas externamente

**Files:**
- Modify: `Assets/Scenes/Scripts/Dungeon/RoomController.cs`

- [ ] **Step 1: Remover a coleta de portas por filhos**

In `Assets/Scenes/Scripts/Dungeon/RoomController.cs`, in `Configure`, find:

```csharp
        doors.Clear();
        doors.AddRange(GetComponentsInChildren<DoorController>(true));
```

Replace with (portas agora são registradas externamente pelo gerador):

```csharp
        doors.Clear();
```

- [ ] **Step 2: Adicionar `RegisterDoor`**

In the same file, add this public method to the `RoomController` class (e.g. right after `Configure`):

```csharp
    /// <summary>Registra uma porta desta sala (chamado pelo gerador após instanciar as portas).</summary>
    public void RegisterDoor(DoorController d)
    {
        if (d != null && !doors.Contains(d)) doors.Add(d);
    }
```

- [ ] **Step 3: Verificar (inspeção)**

Confirm `Activate()` still iterates `doors` calling `d.SetLocked(true)` and the cleared path calls `d.SetLocked(false)` — these are unchanged. Confirm `doors` is the existing `List<DoorController>` field.

- [ ] **Step 4: Commit**

```bash
git add "Assets/Scenes/Scripts/Dungeon/RoomController.cs"
git commit -m "feat: RoomController.RegisterDoor (portas registradas pelo gerador)"
```

---

## Task 4: `DungeonBuilder` — instanciar as portas

**Files:**
- Modify: `Assets/Scenes/Scripts/Dungeon/DungeonBuilder.cs`

- [ ] **Step 1: Adicionar o campo `doorPrefab`**

In `Assets/Scenes/Scripts/Dungeon/DungeonBuilder.cs`, after the `socketCapPrefab` field declaration, add:

```csharp
    [Tooltip("Prefab da porta colocada nos vãos que conectam duas salas.")]
    public GameObject doorPrefab;
```

- [ ] **Step 2: Adicionar o struct `PlacedDoor`**

In the same file, add this struct at the top level (outside the `DungeonBuilder` class, after the `using` lines, before `public class DungeonBuilder`):

```csharp
public struct PlacedDoor
{
    public DoorController Door;
    public GameObject RoomA;
    public GameObject RoomB;
}
```

- [ ] **Step 3: Adicionar o método `BuildDoors`**

In the same file, add this method to the `DungeonBuilder` class (e.g. after `Build`):

```csharp
    /// <summary>
    /// Instancia uma porta em cada vão de conexão. Deve ser chamado DEPOIS do bake de NavMesh
    /// e de os RoomControllers existirem. Retorna as portas com as duas salas que cada uma liga.
    /// </summary>
    public List<PlacedDoor> BuildDoors(DungeonLayout layout, List<GameObject> roomInstances)
    {
        var result = new List<PlacedDoor>();
        if (doorPrefab == null) return result;
        if (root == null) root = transform;

        // Mapa PlannedRoom -> instância (mesma ordem de layout.Rooms).
        var map = new Dictionary<PlannedRoom, GameObject>(layout.Rooms.Count);
        for (int i = 0; i < layout.Rooms.Count && i < roomInstances.Count; i++)
            map[layout.Rooms[i]] = roomInstances[i];

        foreach (PlannedDoorway dw in layout.Doorways)
        {
            Vector3 dir = CardinalDirections.ToVector(dw.WorldDirection);
            Quaternion rot = dir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(dir)
                : Quaternion.identity;

            GameObject go = Instantiate(doorPrefab, dw.WorldPosition, rot, root);
            DoorController door = go.GetComponent<DoorController>();

            map.TryGetValue(dw.RoomA, out GameObject ra);
            map.TryGetValue(dw.RoomB, out GameObject rb);
            result.Add(new PlacedDoor { Door = door, RoomA = ra, RoomB = rb });
        }
        return result;
    }
```

- [ ] **Step 4: Verificar (inspeção)**

- Confirm `DungeonBuilder.cs` already has `using System.Collections.Generic;` and `using Game.Dungeon;` (it does) — needed for `Dictionary`/`List`/`PlannedDoorway`/`CardinalDirections`.
- Confirm `root` is a field already set in `Build()`; `BuildDoors` also guards `if (root == null) root = transform;`.
- Confirm the `socketCapPrefab` block in `Build()` is untouched.

- [ ] **Step 5: Commit**

```bash
git add "Assets/Scenes/Scripts/Dungeon/DungeonBuilder.cs"
git commit -m "feat: DungeonBuilder.BuildDoors instancia portas nos vaos de conexao"
```

---

## Task 5: `DungeonGenerator` — colocar e registrar as portas

**Files:**
- Modify: `Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs`

- [ ] **Step 1: Inserir a etapa de portas antes de posicionar o player**

In `Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs`, in `Generate()`, find the populate loop followed by the player placement:

```csharp
        for (int i = 0; i < layout.Rooms.Count; i++)
        {
            PlannedRoom pr = layout.Rooms[i];
            populator.Populate(instances[i], pr.Definition.Type, pr.Depth, rng);
        }

        // 4. Posicionar o player na sala de início.
        PlacePlayerAtStart(layout, instances);
```

Replace with:

```csharp
        for (int i = 0; i < layout.Rooms.Count; i++)
        {
            PlannedRoom pr = layout.Rooms[i];
            populator.Populate(instances[i], pr.Definition.Type, pr.Depth, rng);
        }

        // 4. Portas entre salas (depois do bake e dos RoomControllers existirem).
        List<PlacedDoor> doors = builder.BuildDoors(layout, instances);
        foreach (PlacedDoor pd in doors)
        {
            if (pd.RoomA != null) pd.RoomA.GetComponent<RoomController>()?.RegisterDoor(pd.Door);
            if (pd.RoomB != null) pd.RoomB.GetComponent<RoomController>()?.RegisterDoor(pd.Door);
        }

        // 5. Posicionar o player na sala de início.
        PlacePlayerAtStart(layout, instances);
```

- [ ] **Step 2: Verificar (inspeção)**

- Confirm `DungeonGenerator.cs` has `using System.Collections.Generic;` (it does — used by `List<GameObject>`).
- Confirm `RoomController` is reachable (same default assembly).
- Confirm `instances` is the `List<GameObject>` returned by `builder.Build(layout)` earlier in `Generate()`.

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs"
git commit -m "feat: DungeonGenerator coloca portas e registra nos RoomControllers"
```

---

## Task 6: Configuração no Editor + verificação ponta a ponta (manual)

Esta task não tem código — é configuração no Editor e teste de jogo.

- [ ] **Step 1: Adicionar o trigger de proximidade no prefab da porta**

Abra `Assets/Scenes/MainScene/Prefabs/Props/Wood Doorframe(Abrivel) Variant.prefab` (modo prefab). Na raiz (onde está o `DoorController`), **adicione um segundo `BoxCollider`** e marque **Is Trigger**. Aumente o `Size` para cobrir ~3 m ao redor da porta (maior que o collider sólido existente, que deve permanecer não-trigger). Salve o prefab.

- [ ] **Step 2: Conferir o Animator**

No mesmo prefab, confirme que o `Animator` alterna entre estados aberto/fechado ao receber o trigger `"change"`. Se o parâmetro tiver outro nome, ajuste o nome em `DoorController.Open()/Close()` (ou renomeie o parâmetro no Animator Controller para `change`).

- [ ] **Step 3: Atribuir o `doorPrefab` no builder**

Na cena `MainScene`, selecione o objeto com `DungeonBuilder`. Arraste `Wood Doorframe(Abrivel) Variant` para o campo `Door Prefab`.

- [ ] **Step 4: Conferir tag do player**

Confirme que o GameObject do player tem a tag `Player`.

- [ ] **Step 5: Play-test — abertura por presença**

Entre em Play, gere a masmorra. Aproxime-se de uma porta entre salas: ela deve **abrir e ficar aberta** ao você chegar perto. Ao se afastar, continua aberta.

- [ ] **Step 6: Play-test — lockdown de combate**

Entre numa sala de **combate**. Ao os inimigos ativarem, as portas da sala devem **fechar/trancar** (você fica preso). Mate todos os inimigos: as portas devem **destrancar e reabrir** (com você ainda dentro).

- [ ] **Step 7: Commit da configuração**

```bash
git add -A
git commit -m "feat: trigger de proximidade no prefab da porta e doorPrefab no builder"
```

---

## Self-review (cobertura do spec)

- Planner expõe `Doorways` → Task 1. ✓
- Porta não-interagível, abre por presença e fica aberta → Task 2 (`DoorController`) + Task 6 Step 1 (trigger). ✓
- Lockdown de combate (fecha ao ativar, reabre ao limpar) → Task 2 (`SetLocked`/`lockCount`) + Task 3 (`RoomController` já chama `SetLocked`). ✓
- Porta instanciada nos vãos entre salas → Task 4 (`BuildDoors`) + Task 5 (chamada + registro). ✓
- Tampão/parede inalterado → nenhuma task toca o bloco `socketCapPrefab`. ✓
- NavMesh não afetada → Task 5 coloca portas depois do bake (ordem preservada). ✓
- `doorPrefab` mora no `DungeonBuilder` → Task 4 (campo) + Task 6 Step 3 (atribuição). ✓
