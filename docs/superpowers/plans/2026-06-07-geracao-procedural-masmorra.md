# Geração Procedural de Masmorra — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gerar masmorras contínuas e conectadas a cada partida, montando salas hand-designed (combate, baú, corredor-armadilha, parkour, boss) por encaixe de sockets, com NavMesh bakeado em runtime e inimigos data-driven.

**Architecture:** Núcleo puro em C# (`Game.Dungeon.Core` asmdef) faz o **planejamento** do layout (algoritmo + overlap + dados), totalmente unit-testável em EditMode. Camada Unity (Assembly-CSharp) faz o **build** (instancia prefabs), bake de NavMesh e população de conteúdo. Inimigos e salas são ScriptableObjects/prefabs — adicionar conteúdo novo é zero código.

**Tech Stack:** Unity 6000.3.11f1, C#, `com.unity.ai.navigation` (NavMeshSurface), Unity Test Framework (NUnit/EditMode).

---

## Spec de referência

`docs/superpowers/specs/2026-06-07-geracao-procedural-masmorra-design.md`

## Pré-requisitos / notas de execução

- **Tarefas marcadas `[EDITOR]`** exigem ação manual no Unity Editor (criar prefabs, ScriptableObjects, montar cena, configurar `NavMeshSurface`). Um worker que só edita arquivos NÃO consegue completá-las — sinalize ao usuário para executar no Editor.
- **Tarefas de teste** rodam no Unity Test Runner: `Window > General > Test Runner > EditMode > Run All`. Não há runner via CLI configurado neste ambiente; a verificação de PASS/FAIL é feita no Editor.
- Os scripts MonoBehaviour/SO ficam em `Assets/Scenes/Scripts/Dungeon/` (Assembly-CSharp). O núcleo puro fica em `Assets/Scenes/Scripts/Dungeon/Core/` com asmdef próprio.

## Mapa de arquivos

**Núcleo puro (`Game.Dungeon.Core` asmdef) — `Assets/Scenes/Scripts/Dungeon/Core/`**
- `Game.Dungeon.Core.asmdef` — assembly do núcleo.
- `RoomType.cs` — enum de tipos de sala.
- `CardinalDirection.cs` — enum cardeal + helpers (`CardinalDirections`).
- `PlacementMath.cs` — rotação Y em passos de 90°, alinhamento de socket, overlap AABB.
- `RoomSocketData.cs` — dado de um socket (posição local + direção).
- `RoomDefinition.cs` — descrição de um candidato de sala (tipo, bounds local, sockets, peso, prefabRef).
- `PlannedRoom.cs` + `PlannedSocket.cs` — saída do planner (sala posicionada + sockets em mundo).
- `DungeonLayout.cs` — resultado completo do planner.
- `DungeonSettings.cs` — parâmetros puros da geração.
- `DungeonPlanner.cs` — o algoritmo.

**Testes (`Game.Dungeon.Core.Tests` asmdef) — `Assets/Tests/EditMode/`**
- `Game.Dungeon.Core.Tests.asmdef`
- `PlacementMathTests.cs`
- `DungeonPlannerTests.cs`

**Camada Unity — `Assets/Scenes/Scripts/Dungeon/`**
- `Room.cs` — componente da raiz do prefab de sala; constrói `RoomDefinition`.
- `DoorSocket.cs` — marker de porta.
- `EnemyMarker.cs`, `ChestMarker.cs`, `TrapMarker.cs` — markers de spawn.
- `RoomCatalog.cs` — SO: prefabs de sala ponderados por tipo.
- `DungeonProfile.cs` — SO: produz `DungeonSettings`.
- `DungeonBuilder.cs` — instancia layout + sela sockets.
- `NavMeshBaker.cs` — bake runtime.
- `EnemySO.cs` — SO: um tipo de inimigo.
- `EncounterTable.cs` — SO: tabela ponderada de inimigos + orçamento por profundidade.
- `RoomPopulator.cs` — popula markers de uma sala.
- `RoomController.cs` — runtime da sala: trigger de entrada, ativação de inimigos, trava de porta.
- `DungeonGenerator.cs` — orquestrador (plan → build → bake → populate → posiciona player).

**Fundação combate — `Assets/Scenes/Scripts/`**
- `IDamageable.cs` — interface comum de dano.
- Modificações: `EnemyScript.cs`, `BossEnemy.cs`, `Projectile.cs`, `CombatScript.cs`.
- Remoção: `SpawnerScript.cs` (legado).

---

## Fase 0 — Fundação de dano (`IDamageable`)

### Task 1: Interface `IDamageable` e adoção nos inimigos

**Files:**
- Create: `Assets/Scenes/Scripts/IDamageable.cs`
- Modify: `Assets/Scenes/Scripts/EnemyScript.cs:5` (assinatura da classe `EnemyDummy`)
- Modify: `Assets/Scenes/Scripts/BossEnemy.cs:5` (assinatura da classe `BossEnemy`)

- [ ] **Step 1: Criar a interface**

`Assets/Scenes/Scripts/IDamageable.cs`:
```csharp
/// <summary>
/// Qualquer coisa que pode receber dano (inimigos, boss, futuros tipos).
/// Flecha e melee batem nisto, sem checar tipo concreto.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int dano);
}
```

- [ ] **Step 2: `EnemyDummy` implementa a interface**

Em `Assets/Scenes/Scripts/EnemyScript.cs`, trocar a linha 5:
```csharp
public class EnemyDummy : MonoBehaviour
```
por:
```csharp
public class EnemyDummy : MonoBehaviour, IDamageable
```
(O método `public void TakeDamage(int damage)` já existente satisfaz a interface — nenhuma outra mudança.)

- [ ] **Step 3: `BossEnemy` implementa a interface**

Em `Assets/Scenes/Scripts/BossEnemy.cs`, trocar a linha 5:
```csharp
public class BossEnemy : MonoBehaviour
```
por:
```csharp
public class BossEnemy : MonoBehaviour, IDamageable
```

- [ ] **Step 4: Compilar no Editor**

No Unity, aguardar recompilar. Expected: Console sem erros de compilação.

- [ ] **Step 5: Commit**
```bash
git add Assets/Scenes/Scripts/IDamageable.cs Assets/Scenes/Scripts/EnemyScript.cs Assets/Scenes/Scripts/BossEnemy.cs
git commit -m "feat: IDamageable implementada por EnemyDummy e BossEnemy"
```

### Task 2: Flecha e melee usam `IDamageable`

**Files:**
- Modify: `Assets/Scenes/Scripts/Projectile.cs:22-41`
- Modify: `Assets/Scenes/Scripts/CombatScript.cs:271-302`

- [ ] **Step 1: Substituir a detecção por tipo no projétil**

Substituir o método `OnTriggerEnter` em `Projectile.cs` por:
```csharp
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[PROJECTILE] Colidiu com: " + other.name);
        if (other.CompareTag("Player")) return;

        IDamageable alvo = other.GetComponentInParent<IDamageable>();
        if (alvo != null)
        {
            Debug.Log($"[PROJECTILE] Acertou {((Component)alvo).name} - Dano: {damage}");
            alvo.TakeDamage(damage);
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
```

- [ ] **Step 2: Substituir a detecção por tipo no melee**

Em `CombatScript.cs`, no método `PerformMeleeAttack`, substituir o bloco `foreach (Collider enemy in hits) { ... }` por:
```csharp
        foreach (Collider enemy in hits)
        {
            IDamageable alvo = enemy.GetComponentInParent<IDamageable>();
            if (alvo == null) continue;

            Debug.Log($"[HIT] Inimigo atingido: {enemy.name} - Dano: {damage}");

            if (audioSource != null && currentWeapon?.hitSound != null)
                audioSource.PlayOneShot(currentWeapon.hitSound);

            if (hitSparks != null)
                Instantiate(hitSparks, enemy.ClosestPoint(transform.position), Quaternion.identity);

            alvo.TakeDamage(damage);
        }
```

- [ ] **Step 3: Compilar no Editor**

Expected: Console sem erros.

- [ ] **Step 4: Verificação manual no Editor [EDITOR]**

Play como Arqueiro: atirar no `InimigoCuca` (leva dano) e no boss (leva dano). Atacar de perto: ambos levam dano. Expected: ambos recebem dano por flecha e melee.

- [ ] **Step 5: Commit**
```bash
git add Assets/Scenes/Scripts/Projectile.cs Assets/Scenes/Scripts/CombatScript.cs
git commit -m "refactor: flecha e melee aplicam dano via IDamageable"
```

---

## Fase 1 — Núcleo puro: tipos e matemática (testável)

### Task 3: Assembly do núcleo + enums

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/Core/Game.Dungeon.Core.asmdef`
- Create: `Assets/Scenes/Scripts/Dungeon/Core/RoomType.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/Core/CardinalDirection.cs`

- [ ] **Step 1: Criar o asmdef do núcleo**

`Assets/Scenes/Scripts/Dungeon/Core/Game.Dungeon.Core.asmdef`:
```json
{
    "name": "Game.Dungeon.Core",
    "rootNamespace": "Game.Dungeon",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "autoReferenced": true,
    "overrideReferences": false,
    "precompiledReferences": [],
    "defineConstraints": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Enum de tipos de sala**

`Assets/Scenes/Scripts/Dungeon/Core/RoomType.cs`:
```csharp
namespace Game.Dungeon
{
    public enum RoomType
    {
        Inicio,
        Combate,
        Bau,
        Armadilha,
        Parkour,
        Boss
    }
}
```

- [ ] **Step 3: Enum cardeal + helpers**

`Assets/Scenes/Scripts/Dungeon/Core/CardinalDirection.cs`:
```csharp
using UnityEngine;

namespace Game.Dungeon
{
    public enum CardinalDirection { North = 0, East = 1, South = 2, West = 3 }

    public static class CardinalDirections
    {
        public static CardinalDirection Opposite(CardinalDirection d)
            => (CardinalDirection)(((int)d + 2) % 4);

        /// <summary>Gira a direção por um yaw em graus (múltiplo de 90, sentido horário).</summary>
        public static CardinalDirection Rotate(CardinalDirection d, int yawDegrees)
        {
            int steps = (((yawDegrees / 90) % 4) + 4) % 4;
            return (CardinalDirection)(((int)d + steps) % 4);
        }

        /// <summary>Vetor mundo da direção (North=+Z, East=+X, South=-Z, West=-X).</summary>
        public static Vector3 ToVector(CardinalDirection d)
        {
            switch (d)
            {
                case CardinalDirection.North: return Vector3.forward;
                case CardinalDirection.East: return Vector3.right;
                case CardinalDirection.South: return Vector3.back;
                case CardinalDirection.West: return Vector3.left;
                default: return Vector3.zero;
            }
        }
    }
}
```

- [ ] **Step 4: Compilar no Editor**

Expected: novo assembly `Game.Dungeon.Core` compila sem erro.

- [ ] **Step 5: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/Core"
git commit -m "feat: assembly do nucleo de dungeon + enums RoomType/CardinalDirection"
```

### Task 4: `PlacementMath` + dados de sala

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/Core/RoomSocketData.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/Core/RoomDefinition.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/Core/PlacementMath.cs`

- [ ] **Step 1: Dado de socket**

`Assets/Scenes/Scripts/Dungeon/Core/RoomSocketData.cs`:
```csharp
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>Um vão de porta no espaço LOCAL da sala.</summary>
    public struct RoomSocketData
    {
        public Vector3 LocalPosition;
        public CardinalDirection Direction;

        public RoomSocketData(Vector3 localPosition, CardinalDirection direction)
        {
            LocalPosition = localPosition;
            Direction = direction;
        }
    }
}
```

- [ ] **Step 2: Definição de sala**

`Assets/Scenes/Scripts/Dungeon/Core/RoomDefinition.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Descrição pura de um candidato de sala (sem GameObject).
    /// O planner trabalha só com isto; PrefabRef é opaco (o builder converte de volta).
    /// </summary>
    public class RoomDefinition
    {
        public RoomType Type;
        public Bounds LocalBounds;
        public IReadOnlyList<RoomSocketData> Sockets;
        public float Weight = 1f;
        public object PrefabRef;
    }
}
```

- [ ] **Step 3: Matemática de posicionamento**

`Assets/Scenes/Scripts/Dungeon/Core/PlacementMath.cs`:
```csharp
using UnityEngine;

namespace Game.Dungeon
{
    public static class PlacementMath
    {
        /// <summary>Rotaciona um ponto em torno de Y por um yaw múltiplo de 90° (horário, igual ao Unity).</summary>
        public static Vector3 RotateY(Vector3 p, int yawDegrees)
        {
            int steps = (((yawDegrees / 90) % 4) + 4) % 4;
            switch (steps)
            {
                case 1: return new Vector3(p.z, p.y, -p.x);
                case 2: return new Vector3(-p.x, p.y, -p.z);
                case 3: return new Vector3(-p.z, p.y, p.x);
                default: return p;
            }
        }

        /// <summary>Rotaciona bounds AABB em torno de Y (90/270 troca extents X/Z).</summary>
        public static Bounds RotateBoundsY(Bounds b, int yawDegrees)
        {
            int steps = (((yawDegrees / 90) % 4) + 4) % 4;
            Vector3 center = RotateY(b.center, yawDegrees);
            Vector3 size = (steps % 2 == 0)
                ? b.size
                : new Vector3(b.size.z, b.size.y, b.size.x);
            return new Bounds(center, size);
        }

        /// <summary>Menor yaw (0/90/180/270) tal que entryDir girado vire targetDir.</summary>
        public static int SolveYaw(CardinalDirection entryDir, CardinalDirection targetDir)
        {
            for (int steps = 0; steps < 4; steps++)
                if (CardinalDirections.Rotate(entryDir, steps * 90) == targetDir)
                    return steps * 90;
            return 0;
        }

        /// <summary>
        /// Dado um socket aberto (posição+direção em mundo) e o socket de entrada de uma sala candidata,
        /// retorna o yaw e a translação que encostam os dois sockets (direções opostas).
        /// </summary>
        public static void PlaceRoom(
            Vector3 openWorldPos, CardinalDirection openWorldDir,
            RoomSocketData entrySocket,
            out int yaw, out Vector3 translation)
        {
            CardinalDirection target = CardinalDirections.Opposite(openWorldDir);
            yaw = SolveYaw(entrySocket.Direction, target);
            Vector3 rotatedEntry = RotateY(entrySocket.LocalPosition, yaw);
            translation = openWorldPos - rotatedEntry;
        }

        /// <summary>Overlap AABB com margem (encolhe XZ para permitir parede compartilhada). Y mantido.</summary>
        public static bool Overlaps(Bounds a, Bounds b, float margin)
        {
            Vector3 shrink = new Vector3(margin * 2f, 0f, margin * 2f);
            Bounds aa = new Bounds(a.center, a.size - shrink);
            Bounds bb = new Bounds(b.center, b.size - shrink);
            return aa.Intersects(bb);
        }
    }
}
```

- [ ] **Step 4: Compilar no Editor**

Expected: sem erros.

- [ ] **Step 5: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/Core"
git commit -m "feat: dados de sala (RoomSocketData/RoomDefinition) e PlacementMath"
```

### Task 5: Testes do `PlacementMath`

**Files:**
- Create: `Assets/Tests/EditMode/Game.Dungeon.Core.Tests.asmdef`
- Create: `Assets/Tests/EditMode/PlacementMathTests.cs`

- [ ] **Step 1: Criar o asmdef de teste**

`Assets/Tests/EditMode/Game.Dungeon.Core.Tests.asmdef`:
```json
{
    "name": "Game.Dungeon.Core.Tests",
    "rootNamespace": "",
    "references": [
        "Game.Dungeon.Core",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Escrever os testes (falhando até compilar o asmdef)**

`Assets/Tests/EditMode/PlacementMathTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;
using Game.Dungeon;

public class PlacementMathTests
{
    [Test]
    public void RotateY_90_ForwardViraRight()
    {
        Vector3 r = PlacementMath.RotateY(Vector3.forward, 90);
        Assert.AreEqual(Vector3.right, r);
    }

    [Test]
    public void RotateBoundsY_90_TrocaExtentsXZ()
    {
        Bounds b = new Bounds(Vector3.zero, new Vector3(4f, 2f, 10f));
        Bounds r = PlacementMath.RotateBoundsY(b, 90);
        Assert.AreEqual(new Vector3(10f, 2f, 4f), r.size);
    }

    [Test]
    public void SolveYaw_EncontraRotacaoCorreta()
    {
        // North precisa virar South -> 180
        Assert.AreEqual(180, PlacementMath.SolveYaw(CardinalDirection.North, CardinalDirection.South));
        // East precisa virar South -> 90
        Assert.AreEqual(90, PlacementMath.SolveYaw(CardinalDirection.East, CardinalDirection.South));
    }

    [Test]
    public void PlaceRoom_AlinhaSocketDeEntradaNoSocketAberto()
    {
        // Socket aberto em (0,0,5) apontando North (+z).
        // Sala candidata tem socket de entrada em local (0,0,-5) apontando South.
        var entry = new RoomSocketData(new Vector3(0f, 0f, -5f), CardinalDirection.South);
        PlacementMath.PlaceRoom(new Vector3(0f, 0f, 5f), CardinalDirection.North, entry,
            out int yaw, out Vector3 t);

        // South já é oposto de North -> yaw 0; translação leva o socket de entrada para (0,0,5).
        Assert.AreEqual(0, yaw);
        Vector3 entryWorld = PlacementMath.RotateY(entry.LocalPosition, yaw) + t;
        Assert.AreEqual(new Vector3(0f, 0f, 5f), entryWorld);
    }

    [Test]
    public void Overlaps_ParedeCompartilhadaNaoConta()
    {
        // Duas salas 10x10 lado a lado (encostadas em x=10), margem 0.1 -> não sobrepõem.
        Bounds a = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 4, 10));
        Bounds b = new Bounds(new Vector3(10, 0, 0), new Vector3(10, 4, 10));
        Assert.IsFalse(PlacementMath.Overlaps(a, b, 0.1f));
    }

    [Test]
    public void Overlaps_SobreposicaoRealConta()
    {
        Bounds a = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 4, 10));
        Bounds b = new Bounds(new Vector3(3, 0, 0), new Vector3(10, 4, 10));
        Assert.IsTrue(PlacementMath.Overlaps(a, b, 0.1f));
    }
}
```

- [ ] **Step 3: Rodar os testes [EDITOR]**

`Window > General > Test Runner > EditMode > Run All`.
Expected: 6 testes PASS em `PlacementMathTests`.

- [ ] **Step 4: Commit**
```bash
git add "Assets/Tests/EditMode"
git commit -m "test: PlacementMath (rotacao, alinhamento, overlap)"
```

---

## Fase 2 — Núcleo puro: o planner (testável)

### Task 6: Tipos de saída + settings

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/Core/PlannedRoom.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/Core/DungeonLayout.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/Core/DungeonSettings.cs`

- [ ] **Step 1: Sala planejada + socket em mundo**

`Assets/Scenes/Scripts/Dungeon/Core/PlannedRoom.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    public class PlannedSocket
    {
        public Vector3 WorldPosition;
        public CardinalDirection WorldDirection;
        public bool Used;
    }

    public class PlannedRoom
    {
        public RoomDefinition Definition;
        public int Yaw;                 // 0/90/180/270
        public Vector3 Position;        // translação em mundo
        public int Depth;               // distância no grafo a partir do início
        public Bounds WorldBounds;
        public List<PlannedSocket> Sockets = new List<PlannedSocket>();
    }
}
```

- [ ] **Step 2: Layout resultante**

`Assets/Scenes/Scripts/Dungeon/Core/DungeonLayout.cs`:
```csharp
using System.Collections.Generic;

namespace Game.Dungeon
{
    public class DungeonLayout
    {
        public bool Success;
        public int Seed;
        public List<PlannedRoom> Rooms = new List<PlannedRoom>();
        public List<PlannedSocket> OpenSockets = new List<PlannedSocket>(); // a selar com parede
    }
}
```

- [ ] **Step 3: Parâmetros da geração**

`Assets/Scenes/Scripts/Dungeon/Core/DungeonSettings.cs`:
```csharp
using System.Collections.Generic;

namespace Game.Dungeon
{
    /// <summary>Parâmetros puros (sem Unity SO) para o planner ser testável.</summary>
    public class DungeonSettings
    {
        public int MinRooms = 8;
        public int MaxRooms = 14;
        public float OverlapMargin = 0.1f;
        public int MinBossDepth = 3;

        /// <summary>Mínimo exigido por tipo (validação ao final). Inicio/Boss tratados à parte.</summary>
        public Dictionary<RoomType, int> MinQuota = new Dictionary<RoomType, int>
        {
            { RoomType.Combate, 3 },
            { RoomType.Bau, 1 },
            { RoomType.Armadilha, 1 },
            { RoomType.Parkour, 1 },
        };

        /// <summary>Máximo por tipo (corte durante a colocação).</summary>
        public Dictionary<RoomType, int> MaxQuota = new Dictionary<RoomType, int>
        {
            { RoomType.Combate, 5 },
            { RoomType.Bau, 2 },
            { RoomType.Armadilha, 2 },
            { RoomType.Parkour, 1 },
        };
    }
}
```

- [ ] **Step 4: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 5: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/Core"
git commit -m "feat: tipos de saida do planner (PlannedRoom/DungeonLayout) e DungeonSettings"
```

### Task 7: `DungeonPlanner`

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/Core/DungeonPlanner.cs`

- [ ] **Step 1: Implementar o planner**

`Assets/Scenes/Scripts/Dungeon/Core/DungeonPlanner.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Planeja o layout (C# puro). Estratégia: colocação gulosa por fronteira de sockets,
    /// selando sockets que não encaixam, com regeneração por seed quando as cotas não fecham
    /// (forma pragmática de backtracking, limitada por maxRegenerations).
    /// </summary>
    public static class DungeonPlanner
    {
        private class FrontierEntry
        {
            public PlannedRoom Owner;
            public PlannedSocket Socket;
        }

        public static DungeonLayout Plan(
            IReadOnlyList<RoomDefinition> catalog,
            DungeonSettings settings,
            int seed,
            int maxRegenerations = 25)
        {
            for (int attempt = 0; attempt < maxRegenerations; attempt++)
            {
                DungeonLayout layout = TryPlan(catalog, settings, seed + attempt);
                if (layout.Success)
                {
                    layout.Seed = seed + attempt;
                    return layout;
                }
            }
            return new DungeonLayout { Success = false, Seed = seed };
        }

        private static DungeonLayout TryPlan(
            IReadOnlyList<RoomDefinition> catalog, DungeonSettings settings, int seed)
        {
            var rng = new System.Random(seed);
            var layout = new DungeonLayout();
            var counts = new Dictionary<RoomType, int>();
            foreach (RoomType t in Enum.GetValues(typeof(RoomType))) counts[t] = 0;

            // 1. Sala de início na origem.
            RoomDefinition startDef = PickByType(catalog, RoomType.Inicio, rng);
            if (startDef == null) return layout; // sucesso=false
            PlannedRoom start = MakePlacedRoom(startDef, 0, Vector3.zero, 0);
            layout.Rooms.Add(start);
            counts[RoomType.Inicio]++;

            var frontier = new List<FrontierEntry>();
            AddOpenSockets(frontier, start);

            int target = rng.Next(settings.MinRooms, settings.MaxRooms + 1);

            // 2. Expansão gulosa.
            while (layout.Rooms.Count < target && frontier.Count > 0)
            {
                int fi = rng.Next(frontier.Count);
                FrontierEntry fe = frontier[fi];
                frontier.RemoveAt(fi);
                if (fe.Socket.Used) continue;

                PlannedRoom placed = TryPlaceAtSocket(catalog, settings, rng, layout, counts, fe);
                if (placed != null)
                {
                    counts[placed.Definition.Type]++;
                    AddOpenSockets(frontier, placed);
                }
                else
                {
                    layout.OpenSockets.Add(fe.Socket); // vira parede
                }
            }

            // 3. Boss na ponta mais profunda.
            if (!TryPlaceBoss(catalog, settings, rng, layout, counts, frontier))
                return layout; // sucesso=false

            // 4. Sela o que sobrou aberto.
            foreach (FrontierEntry fe in frontier)
                if (!fe.Socket.Used) layout.OpenSockets.Add(fe.Socket);

            // 5. Valida cotas mínimas.
            if (!QuotasMet(counts, settings)) return layout; // sucesso=false

            layout.Success = true;
            return layout;
        }

        private static PlannedRoom TryPlaceAtSocket(
            IReadOnlyList<RoomDefinition> catalog, DungeonSettings settings, System.Random rng,
            DungeonLayout layout, Dictionary<RoomType, int> counts, FrontierEntry fe)
        {
            // Tipos elegíveis (exclui Inicio/Boss; respeita MaxQuota), priorizando cotas não cumpridas.
            var types = new List<RoomType> { RoomType.Combate, RoomType.Bau, RoomType.Armadilha, RoomType.Parkour };
            types = types.Where(t => UnderMax(counts, settings, t)).OrderBy(_ => rng.Next()).ToList();
            types = types.OrderByDescending(t => NeedsMore(counts, settings, t)).ToList();

            foreach (RoomType type in types)
            {
                var candidates = catalog.Where(d => d.Type == type).OrderBy(_ => rng.Next()).ToList();
                foreach (RoomDefinition def in candidates)
                {
                    PlannedRoom placed = TryFit(settings, layout, def, fe);
                    if (placed != null)
                    {
                        layout.Rooms.Add(placed);
                        return placed;
                    }
                }
            }
            return null;
        }

        private static PlannedRoom TryFit(
            DungeonSettings settings, DungeonLayout layout, RoomDefinition def, FrontierEntry fe)
        {
            // Testa cada socket da candidata como entrada.
            for (int i = 0; i < def.Sockets.Count; i++)
            {
                RoomSocketData entry = def.Sockets[i];
                PlacementMath.PlaceRoom(
                    fe.Socket.WorldPosition, fe.Socket.WorldDirection, entry,
                    out int yaw, out Vector3 t);

                Bounds worldBounds = PlacementMath.RotateBoundsY(def.LocalBounds, yaw);
                worldBounds.center += t;

                bool collides = layout.Rooms.Any(r =>
                    PlacementMath.Overlaps(worldBounds, r.WorldBounds, settings.OverlapMargin));
                if (collides) continue;

                PlannedRoom placed = MakePlacedRoom(def, yaw, t, fe.Owner.Depth + 1);
                // marca o socket de entrada da nova sala e o socket aberto como usados
                placed.Sockets[i].Used = true;
                fe.Socket.Used = true;
                return placed;
            }
            return null;
        }

        private static bool TryPlaceBoss(
            IReadOnlyList<RoomDefinition> catalog, DungeonSettings settings, System.Random rng,
            DungeonLayout layout, Dictionary<RoomType, int> counts, List<FrontierEntry> frontier)
        {
            if (counts[RoomType.Boss] > 0) return true;

            // sockets abertos ordenados por profundidade do dono (mais fundo primeiro)
            var open = frontier.Where(f => !f.Socket.Used)
                               .OrderByDescending(f => f.Owner.Depth)
                               .ToList();
            var bossDefs = catalog.Where(d => d.Type == RoomType.Boss).OrderBy(_ => rng.Next()).ToList();

            foreach (FrontierEntry fe in open)
            {
                if (fe.Owner.Depth + 1 < settings.MinBossDepth) continue;
                foreach (RoomDefinition def in bossDefs)
                {
                    PlannedRoom placed = TryFit(settings, layout, def, fe);
                    if (placed != null)
                    {
                        counts[RoomType.Boss]++;
                        return true;
                    }
                }
            }
            return false;
        }

        // ---------- helpers ----------

        private static PlannedRoom MakePlacedRoom(RoomDefinition def, int yaw, Vector3 pos, int depth)
        {
            Bounds wb = PlacementMath.RotateBoundsY(def.LocalBounds, yaw);
            wb.center += pos;
            var room = new PlannedRoom
            {
                Definition = def,
                Yaw = yaw,
                Position = pos,
                Depth = depth,
                WorldBounds = wb
            };
            foreach (RoomSocketData s in def.Sockets)
            {
                room.Sockets.Add(new PlannedSocket
                {
                    WorldPosition = PlacementMath.RotateY(s.LocalPosition, yaw) + pos,
                    WorldDirection = CardinalDirections.Rotate(s.Direction, yaw),
                    Used = false
                });
            }
            return room;
        }

        private static void AddOpenSockets(List<FrontierEntry> frontier, PlannedRoom room)
        {
            foreach (PlannedSocket s in room.Sockets)
                if (!s.Used)
                    frontier.Add(new FrontierEntry { Owner = room, Socket = s });
        }

        private static RoomDefinition PickByType(
            IReadOnlyList<RoomDefinition> catalog, RoomType type, System.Random rng)
        {
            var defs = catalog.Where(d => d.Type == type).ToList();
            if (defs.Count == 0) return null;
            float total = defs.Sum(d => Mathf.Max(0.0001f, d.Weight));
            double r = rng.NextDouble() * total;
            foreach (RoomDefinition d in defs)
            {
                r -= Mathf.Max(0.0001f, d.Weight);
                if (r <= 0) return d;
            }
            return defs[defs.Count - 1];
        }

        private static bool UnderMax(Dictionary<RoomType, int> counts, DungeonSettings s, RoomType t)
            => !s.MaxQuota.ContainsKey(t) || counts[t] < s.MaxQuota[t];

        private static bool NeedsMore(Dictionary<RoomType, int> counts, DungeonSettings s, RoomType t)
            => s.MinQuota.ContainsKey(t) && counts[t] < s.MinQuota[t];

        private static bool QuotasMet(Dictionary<RoomType, int> counts, DungeonSettings s)
        {
            if (counts[RoomType.Inicio] != 1) return false;
            if (counts[RoomType.Boss] != 1) return false;
            foreach (var kv in s.MinQuota)
                if (counts[kv.Key] < kv.Value) return false;
            return true;
        }
    }
}
```

- [ ] **Step 2: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 3: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/Core/DungeonPlanner.cs"
git commit -m "feat: DungeonPlanner (frontier + selagem + regeneracao por seed)"
```

### Task 8: Testes do `DungeonPlanner`

**Files:**
- Create: `Assets/Tests/EditMode/DungeonPlannerTests.cs`

- [ ] **Step 1: Escrever os testes**

`Assets/Tests/EditMode/DungeonPlannerTests.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Game.Dungeon;

public class DungeonPlannerTests
{
    // Sala quadrada 10x10 com sockets nos 4 lados, no centro de cada parede.
    private static RoomDefinition Square(RoomType type, float weight = 1f)
    {
        var sockets = new List<RoomSocketData>
        {
            new RoomSocketData(new Vector3(0, 0, 5),  CardinalDirection.North),
            new RoomSocketData(new Vector3(5, 0, 0),  CardinalDirection.East),
            new RoomSocketData(new Vector3(0, 0, -5), CardinalDirection.South),
            new RoomSocketData(new Vector3(-5, 0, 0), CardinalDirection.West),
        };
        return new RoomDefinition
        {
            Type = type,
            LocalBounds = new Bounds(Vector3.zero, new Vector3(10, 4, 10)),
            Sockets = sockets,
            Weight = weight,
            PrefabRef = type // marcador qualquer
        };
    }

    private static List<RoomDefinition> Catalog()
    {
        return new List<RoomDefinition>
        {
            Square(RoomType.Inicio),
            Square(RoomType.Combate), Square(RoomType.Combate),
            Square(RoomType.Bau),
            Square(RoomType.Armadilha),
            Square(RoomType.Parkour),
            Square(RoomType.Boss),
        };
    }

    private static DungeonSettings Settings()
    {
        return new DungeonSettings
        {
            MinRooms = 8,
            MaxRooms = 12,
            OverlapMargin = 0.1f,
            MinBossDepth = 2,
            MinQuota = new Dictionary<RoomType, int>
            {
                { RoomType.Combate, 3 }, { RoomType.Bau, 1 },
                { RoomType.Armadilha, 1 }, { RoomType.Parkour, 1 },
            },
            MaxQuota = new Dictionary<RoomType, int>
            {
                { RoomType.Combate, 5 }, { RoomType.Bau, 2 },
                { RoomType.Armadilha, 2 }, { RoomType.Parkour, 1 },
            },
        };
    }

    [Test]
    public void Plan_GeraLayoutComSucesso()
    {
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), Settings(), seed: 1);
        Assert.IsTrue(layout.Success);
        Assert.GreaterOrEqual(layout.Rooms.Count, 2);
    }

    [Test]
    public void Plan_NuncaSobrepoeSalas()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            DungeonLayout layout = DungeonPlanner.Plan(Catalog(), Settings(), seed);
            if (!layout.Success) continue;
            var rooms = layout.Rooms;
            for (int i = 0; i < rooms.Count; i++)
                for (int j = i + 1; j < rooms.Count; j++)
                    Assert.IsFalse(
                        PlacementMath.Overlaps(rooms[i].WorldBounds, rooms[j].WorldBounds, 0.1f),
                        $"Salas {i} e {j} sobrepõem (seed {seed}).");
        }
    }

    [Test]
    public void Plan_TemExatamenteUmInicioEUmBoss()
    {
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), Settings(), seed: 7);
        Assert.IsTrue(layout.Success);
        Assert.AreEqual(1, layout.Rooms.Count(r => r.Definition.Type == RoomType.Inicio));
        Assert.AreEqual(1, layout.Rooms.Count(r => r.Definition.Type == RoomType.Boss));
    }

    [Test]
    public void Plan_BossRespeitaProfundidadeMinima()
    {
        var settings = Settings();
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), settings, seed: 3);
        Assert.IsTrue(layout.Success);
        PlannedRoom boss = layout.Rooms.First(r => r.Definition.Type == RoomType.Boss);
        Assert.GreaterOrEqual(boss.Depth, settings.MinBossDepth);
    }

    [Test]
    public void Plan_RespeitaCotasMinimas()
    {
        var settings = Settings();
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), settings, seed: 11);
        Assert.IsTrue(layout.Success);
        foreach (var kv in settings.MinQuota)
            Assert.GreaterOrEqual(
                layout.Rooms.Count(r => r.Definition.Type == kv.Key), kv.Value);
    }

    [Test]
    public void Plan_RespeitaCotasMaximas()
    {
        var settings = Settings();
        DungeonLayout layout = DungeonPlanner.Plan(Catalog(), settings, seed: 11);
        Assert.IsTrue(layout.Success);
        foreach (var kv in settings.MaxQuota)
            Assert.LessOrEqual(
                layout.Rooms.Count(r => r.Definition.Type == kv.Key), kv.Value);
    }

    [Test]
    public void Plan_MesmoSeedGeraLayoutIdentico()
    {
        DungeonLayout a = DungeonPlanner.Plan(Catalog(), Settings(), seed: 42);
        DungeonLayout b = DungeonPlanner.Plan(Catalog(), Settings(), seed: 42);
        Assert.AreEqual(a.Rooms.Count, b.Rooms.Count);
        Assert.AreEqual(a.Seed, b.Seed);
        for (int i = 0; i < a.Rooms.Count; i++)
        {
            Assert.AreEqual(a.Rooms[i].Definition.Type, b.Rooms[i].Definition.Type);
            Assert.AreEqual(a.Rooms[i].Position, b.Rooms[i].Position);
            Assert.AreEqual(a.Rooms[i].Yaw, b.Rooms[i].Yaw);
        }
    }
}
```

- [ ] **Step 2: Rodar os testes [EDITOR]**

`Window > General > Test Runner > EditMode > Run All`.
Expected: todos os testes de `DungeonPlannerTests` PASS.

- [ ] **Step 3: Commit**
```bash
git add "Assets/Tests/EditMode/DungeonPlannerTests.cs"
git commit -m "test: DungeonPlanner (sem overlap, cotas, boss, determinismo)"
```

---

## Fase 3 — Camada Unity: convenções de sala

### Task 9: Componentes de sala e markers

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/DoorSocket.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/Room.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/EnemyMarker.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/ChestMarker.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/TrapMarker.cs`

- [ ] **Step 1: `DoorSocket`**

`Assets/Scenes/Scripts/Dungeon/DoorSocket.cs`:
```csharp
using UnityEngine;
using Game.Dungeon;

/// <summary>Marker de porta no prefab de sala. O forward visual segue a direção cardeal local.</summary>
public class DoorSocket : MonoBehaviour
{
    public CardinalDirection direction = CardinalDirection.North;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 p = transform.position;
        Vector3 dir = transform.parent != null
            ? transform.parent.TransformDirection(CardinalDirections.ToVector(direction))
            : CardinalDirections.ToVector(direction);
        Gizmos.DrawSphere(p, 0.25f);
        Gizmos.DrawLine(p, p + dir.normalized * 1.5f);
    }
}
```

- [ ] **Step 2: `Room`**

`Assets/Scenes/Scripts/Dungeon/Room.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

/// <summary>
/// Componente da raiz do prefab de sala. Converte a autoria visual em RoomDefinition pura.
/// Assume o BoxCollider de bounds na própria raiz (escala 1).
/// </summary>
public class Room : MonoBehaviour
{
    public RoomType type;
    [Tooltip("BoxCollider (isTrigger) cobrindo a sala, na raiz. Usado como bounds e como gatilho de entrada.")]
    public BoxCollider boundsCollider;

    public RoomDefinition BuildDefinition(object prefabRef, float weight)
    {
        var sockets = new List<RoomSocketData>();
        foreach (DoorSocket s in GetComponentsInChildren<DoorSocket>(true))
        {
            sockets.Add(new RoomSocketData(
                transform.InverseTransformPoint(s.transform.position),
                s.direction));
        }

        Bounds local = boundsCollider != null
            ? new Bounds(boundsCollider.center, boundsCollider.size)
            : new Bounds(Vector3.zero, new Vector3(10, 4, 10));

        return new RoomDefinition
        {
            Type = type,
            LocalBounds = local,
            Sockets = sockets,
            Weight = weight,
            PrefabRef = prefabRef
        };
    }
}
```

- [ ] **Step 3: Markers**

`Assets/Scenes/Scripts/Dungeon/EnemyMarker.cs`:
```csharp
using UnityEngine;

/// <summary>Ponto de spawn de inimigo dentro do prefab da sala (não amarra prefab de inimigo).</summary>
public class EnemyMarker : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
```

`Assets/Scenes/Scripts/Dungeon/ChestMarker.cs`:
```csharp
using UnityEngine;

public class ChestMarker : MonoBehaviour
{
    [Tooltip("Opcional: prefab de baú específico. Vazio = usa o padrão do RoomPopulator.")]
    public GameObject chestPrefabOverride;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
}
```

`Assets/Scenes/Scripts/Dungeon/TrapMarker.cs`:
```csharp
using UnityEngine;

/// <summary>Ponto de armadilha. Geralmente a armadilha já vem montada no prefab; este marker é opcional.</summary>
public class TrapMarker : MonoBehaviour
{
    public GameObject trapPrefabOverride;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
}
```

- [ ] **Step 4: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 5: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/DoorSocket.cs" "Assets/Scenes/Scripts/Dungeon/Room.cs" "Assets/Scenes/Scripts/Dungeon/EnemyMarker.cs" "Assets/Scenes/Scripts/Dungeon/ChestMarker.cs" "Assets/Scenes/Scripts/Dungeon/TrapMarker.cs"
git commit -m "feat: componentes de sala (Room, DoorSocket) e markers de spawn"
```

### Task 10: Autorar os prefabs de sala [EDITOR]

**Files:** (prefabs criados no Editor; sem edição de texto)

- [ ] **Step 1: Montar um prefab por tipo**

No Editor, para cada `RoomType` (Inicio, Combate, Bau, Armadilha, Parkour, Boss), criar ao menos 1 prefab usando o RPG Dungeon Pack:
- Raiz com componente `Room` (setar `type`).
- `BoxCollider` na raiz, `isTrigger = true`, cobrindo a sala; arrastar para `Room.boundsCollider`.
- Filhos vazios com `DoorSocket` em cada vão de porta, com `direction` correta (forward do gizmo apontando pra fora) e posicionados no plano de encaixe (mesma convenção em todos: ex. socket no centro do vão, no chão).
- Conteúdo:
  - Combate: alguns `EnemyMarker`.
  - Bau: um `ChestMarker`.
  - Armadilha: corredor com armadilhas montadas + 2 `DoorSocket` (entrada/saída).
  - Parkour: geometria de parkour + 2 `DoorSocket`.
  - Boss: arena + `EnemyMarker`/spawn do boss.
- **Convenção crítica de alinhamento:** todos os sockets na mesma altura (y do chão) e o "plano da porta" consistente, senão o encaixe fica torto. Recomendado: trabalhar numa grade (ex. múltiplos de 1m) para vãos baterem.

- [ ] **Step 2: Salvar os prefabs** em `Assets/Scenes/MainScene/Prefabs/Rooms/`.

- [ ] **Step 3: Validar no Editor** que os gizmos de socket apontam pra fora e os bounds cobrem a sala.

- [ ] **Step 4: Commit**
```bash
git add "Assets/Scenes/MainScene/Prefabs/Rooms"
git commit -m "content: prefabs iniciais de sala (inicio/combate/bau/armadilha/parkour/boss)"
```

---

## Fase 4 — Catálogo, build e NavMesh

### Task 11: `RoomCatalog` e `DungeonProfile`

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/RoomCatalog.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/DungeonProfile.cs`

- [ ] **Step 1: `RoomCatalog`**

`Assets/Scenes/Scripts/Dungeon/RoomCatalog.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

[CreateAssetMenu(menuName = "Dungeon/Room Catalog", fileName = "RoomCatalog")]
public class RoomCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;
        public float weight = 1f;
    }

    public List<Entry> rooms = new List<Entry>();

    /// <summary>Lê os componentes Room dos prefabs e produz as definições puras para o planner.</summary>
    public List<RoomDefinition> BuildDefinitions()
    {
        var defs = new List<RoomDefinition>();
        foreach (Entry e in rooms)
        {
            if (e.prefab == null) continue;
            Room room = e.prefab.GetComponent<Room>();
            if (room == null)
            {
                Debug.LogWarning($"[RoomCatalog] Prefab {e.prefab.name} sem componente Room — ignorado.");
                continue;
            }
            defs.Add(room.BuildDefinition(e.prefab, e.weight));
        }
        return defs;
    }
}
```

- [ ] **Step 2: `DungeonProfile`**

`Assets/Scenes/Scripts/Dungeon/DungeonProfile.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

[CreateAssetMenu(menuName = "Dungeon/Dungeon Profile", fileName = "DungeonProfile")]
public class DungeonProfile : ScriptableObject
{
    [Header("Quantidade de salas")]
    public int minRooms = 8;
    public int maxRooms = 14;

    [Header("Geometria")]
    public float overlapMargin = 0.1f;
    public int minBossDepth = 3;

    [System.Serializable]
    public class Quota { public RoomType type; public int min; public int max; }

    public List<Quota> quotas = new List<Quota>
    {
        new Quota { type = RoomType.Combate,   min = 3, max = 5 },
        new Quota { type = RoomType.Bau,       min = 1, max = 2 },
        new Quota { type = RoomType.Armadilha, min = 1, max = 2 },
        new Quota { type = RoomType.Parkour,   min = 1, max = 1 },
    };

    public DungeonSettings ToSettings()
    {
        var s = new DungeonSettings
        {
            MinRooms = minRooms,
            MaxRooms = maxRooms,
            OverlapMargin = overlapMargin,
            MinBossDepth = minBossDepth,
            MinQuota = new Dictionary<RoomType, int>(),
            MaxQuota = new Dictionary<RoomType, int>(),
        };
        foreach (Quota q in quotas)
        {
            s.MinQuota[q.type] = q.min;
            s.MaxQuota[q.type] = q.max;
        }
        return s;
    }
}
```

- [ ] **Step 3: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 4: Criar os assets [EDITOR]**

`Assets > Create > Dungeon > Room Catalog` e `Dungeon Profile`. Preencher o catálogo com os prefabs da Task 10 (com pesos). Salvar em `Assets/Scenes/MainScene/Data/`.

- [ ] **Step 5: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/RoomCatalog.cs" "Assets/Scenes/Scripts/Dungeon/DungeonProfile.cs" "Assets/Scenes/MainScene/Data"
git commit -m "feat: RoomCatalog e DungeonProfile (SOs) + assets iniciais"
```

### Task 12: `DungeonBuilder`

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/DungeonBuilder.cs`

- [ ] **Step 1: Implementar**

`Assets/Scenes/Scripts/Dungeon/DungeonBuilder.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

/// <summary>Instancia o layout planejado e sela sockets abertos com prefab de parede.</summary>
public class DungeonBuilder : MonoBehaviour
{
    [Tooltip("Pai de todas as salas instanciadas (também alvo do NavMeshSurface).")]
    public Transform root;
    [Tooltip("Prefab de tampa/parede para sockets que ficaram abertos.")]
    public GameObject socketCapPrefab;

    /// <summary>Instancia o layout. Retorna a sala raiz de cada PlannedRoom na mesma ordem de layout.Rooms.</summary>
    public List<GameObject> Build(DungeonLayout layout)
    {
        if (root == null) root = transform;
        var instances = new List<GameObject>(layout.Rooms.Count);

        foreach (PlannedRoom pr in layout.Rooms)
        {
            var prefab = (GameObject)pr.Definition.PrefabRef;
            GameObject go = Instantiate(prefab, pr.Position, Quaternion.Euler(0, pr.Yaw, 0), root);
            instances.Add(go);
        }

        if (socketCapPrefab != null)
        {
            foreach (PlannedSocket s in layout.OpenSockets)
            {
                Vector3 dir = CardinalDirections.ToVector(s.WorldDirection);
                Quaternion rot = dir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(dir)
                    : Quaternion.identity;
                Instantiate(socketCapPrefab, s.WorldPosition, rot, root);
            }
        }

        return instances;
    }

    public void Clear()
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
```

- [ ] **Step 2: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 3: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/DungeonBuilder.cs"
git commit -m "feat: DungeonBuilder (instancia salas + sela sockets)"
```

### Task 13: `NavMeshBaker`

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/NavMeshBaker.cs`

- [ ] **Step 1: Implementar**

`Assets/Scenes/Scripts/Dungeon/NavMeshBaker.cs`:
```csharp
using UnityEngine;
using Unity.AI.Navigation;

/// <summary>Bake do NavMesh em runtime após a masmorra estar montada.</summary>
[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshBaker : MonoBehaviour
{
    private NavMeshSurface surface;

    private void Awake()
    {
        surface = GetComponent<NavMeshSurface>();
    }

    public void Bake()
    {
        if (surface == null) surface = GetComponent<NavMeshSurface>();
        surface.BuildNavMesh();
    }
}
```

- [ ] **Step 2: Configurar a cena [EDITOR]**

Na cena de dungeon, num GameObject raiz da masmorra: adicionar `NavMeshSurface` (`Collect Objects = Children`, agentType padrão) + `NavMeshBaker`. Esse GameObject será o `root` do `DungeonBuilder`.

- [ ] **Step 3: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 4: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/NavMeshBaker.cs"
git commit -m "feat: NavMeshBaker (bake runtime via NavMeshSurface)"
```

---

## Fase 5 — Inimigos data-driven e população

### Task 14: `EnemySO` e `EncounterTable`

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/EnemySO.cs`
- Create: `Assets/Scenes/Scripts/Dungeon/EncounterTable.cs`

- [ ] **Step 1: `EnemySO`**

`Assets/Scenes/Scripts/Dungeon/EnemySO.cs`:
```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Enemy", fileName = "Enemy")]
public class EnemySO : ScriptableObject
{
    public string displayName;
    public GameObject prefab;
    [Tooltip("Peso relativo no sorteio.")]
    public float weight = 1f;
    [Tooltip("Custo no orçamento da sala (inimigos mais fortes custam mais).")]
    public int budgetCost = 1;
    [Tooltip("Profundidade mínima da sala para esse inimigo aparecer.")]
    public int minDepth = 0;
}
```

- [ ] **Step 2: `EncounterTable`**

`Assets/Scenes/Scripts/Dungeon/EncounterTable.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Encounter Table", fileName = "EncounterTable")]
public class EncounterTable : ScriptableObject
{
    public List<EnemySO> enemies = new List<EnemySO>();

    [Header("Orçamento por sala")]
    public int baseBudget = 2;
    [Tooltip("Orçamento adicional por unidade de profundidade.")]
    public int budgetPerDepth = 1;

    public int BudgetForDepth(int depth) => baseBudget + budgetPerDepth * depth;

    /// <summary>Sorteia um inimigo elegível (depth/budget) via rng; null se nenhum couber.</summary>
    public EnemySO Pick(int depth, int remainingBudget, System.Random rng)
    {
        var eligible = new List<EnemySO>();
        float total = 0f;
        foreach (EnemySO e in enemies)
        {
            if (e == null || e.prefab == null) continue;
            if (e.minDepth > depth) continue;
            if (e.budgetCost > remainingBudget) continue;
            eligible.Add(e);
            total += Mathf.Max(0.0001f, e.weight);
        }
        if (eligible.Count == 0) return null;

        double r = rng.NextDouble() * total;
        foreach (EnemySO e in eligible)
        {
            r -= Mathf.Max(0.0001f, e.weight);
            if (r <= 0) return e;
        }
        return eligible[eligible.Count - 1];
    }
}
```

- [ ] **Step 3: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 4: Criar assets [EDITOR]**

`Assets > Create > Dungeon > Enemy` para o `InimigoCuca` (apontar o prefab). Criar `Encounter Table` e adicionar o EnemySO. (Esqueleto no futuro: novo `Enemy` SO + arrastar aqui.)

- [ ] **Step 5: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/EnemySO.cs" "Assets/Scenes/Scripts/Dungeon/EncounterTable.cs"
git commit -m "feat: EnemySO e EncounterTable (spawn de inimigos data-driven)"
```

### Task 15: `RoomController` (runtime da sala)

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/RoomController.cs`

- [ ] **Step 1: Implementar**

`Assets/Scenes/Scripts/Dungeon/RoomController.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime de uma sala instanciada: ativa inimigos quando o player entra,
/// e (se for sala de combate) tranca as portas até a sala ser limpa.
/// </summary>
public class RoomController : MonoBehaviour
{
    public bool lockDoorsUntilCleared = false;

    private readonly List<GameObject> enemies = new List<GameObject>();
    private readonly List<DoorController> doors = new List<DoorController>();
    private bool activated;
    private bool cleared;

    public void Configure(IEnumerable<GameObject> spawnedEnemies, bool lockDoors)
    {
        lockDoorsUntilCleared = lockDoors;
        enemies.Clear();
        foreach (GameObject e in spawnedEnemies)
        {
            if (e == null) continue;
            e.SetActive(false);
            enemies.Add(e);
        }
        doors.Clear();
        doors.AddRange(GetComponentsInChildren<DoorController>(true));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated || cleared) return;
        if (!other.CompareTag("Player")) return;
        Activate();
    }

    private void Activate()
    {
        activated = true;
        foreach (GameObject e in enemies)
            if (e != null) e.SetActive(true);

        if (lockDoorsUntilCleared && enemies.Count > 0)
            foreach (DoorController d in doors) d.SetLocked(true);
    }

    private void Update()
    {
        if (!activated || cleared) return;
        enemies.RemoveAll(e => e == null);
        if (enemies.Count == 0)
        {
            cleared = true;
            foreach (DoorController d in doors) d.SetLocked(false);
        }
    }
}
```

- [ ] **Step 2: Adicionar `SetLocked` ao `DoorController`**

Em `Assets/Scenes/Scripts/DoorController.cs`, adicionar campo e método (e respeitar o lock em `Interact`):
```csharp
    private bool locked;

    public void SetLocked(bool value) => locked = value;
```
E na primeira linha de `Interact()`:
```csharp
        if (locked) return;
```
(Adicionar logo após a linha `if (!CanInteract) return;`.)

- [ ] **Step 3: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 4: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/RoomController.cs" "Assets/Scenes/Scripts/DoorController.cs"
git commit -m "feat: RoomController (ativacao por trigger + trava de porta) e DoorController.SetLocked"
```

### Task 16: `RoomPopulator`

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/RoomPopulator.cs`

- [ ] **Step 1: Implementar**

`Assets/Scenes/Scripts/Dungeon/RoomPopulator.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

/// <summary>Popula uma sala instanciada a partir dos markers, usando a EncounterTable.</summary>
public class RoomPopulator : MonoBehaviour
{
    public EncounterTable encounterTable;
    public GameObject defaultChestPrefab;

    /// <summary>
    /// Popula a sala. roomInstance = GameObject instanciado; type/depth vêm do PlannedRoom;
    /// rng compartilhado garante determinismo por seed.
    /// </summary>
    public void Populate(GameObject roomInstance, RoomType type, int depth, System.Random rng)
    {
        var spawnedEnemies = new List<GameObject>();

        // Inimigos (qualquer sala com EnemyMarker; orçamento escala com profundidade).
        var enemyMarkers = roomInstance.GetComponentsInChildren<EnemyMarker>(true);
        if (encounterTable != null && enemyMarkers.Length > 0)
        {
            int budget = encounterTable.BudgetForDepth(depth);
            foreach (EnemyMarker m in enemyMarkers)
            {
                if (budget <= 0) break;
                EnemySO pick = encounterTable.Pick(depth, budget, rng);
                if (pick == null) break;
                budget -= pick.budgetCost;
                GameObject e = Instantiate(pick.prefab, m.transform.position, m.transform.rotation, roomInstance.transform);
                spawnedEnemies.Add(e);
            }
        }

        // Baús.
        foreach (ChestMarker c in roomInstance.GetComponentsInChildren<ChestMarker>(true))
        {
            GameObject prefab = c.chestPrefabOverride != null ? c.chestPrefabOverride : defaultChestPrefab;
            if (prefab != null)
                Instantiate(prefab, c.transform.position, c.transform.rotation, roomInstance.transform);
        }

        // Armadilhas com override (as fixas já vêm no prefab).
        foreach (TrapMarker t in roomInstance.GetComponentsInChildren<TrapMarker>(true))
        {
            if (t.trapPrefabOverride != null)
                Instantiate(t.trapPrefabOverride, t.transform.position, t.transform.rotation, roomInstance.transform);
        }

        // Liga o controlador de sala.
        RoomController rc = roomInstance.GetComponent<RoomController>();
        if (rc == null) rc = roomInstance.AddComponent<RoomController>();
        rc.Configure(spawnedEnemies, lockDoors: type == RoomType.Combate || type == RoomType.Boss);
    }
}
```

- [ ] **Step 2: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 3: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/RoomPopulator.cs"
git commit -m "feat: RoomPopulator (inimigos/baus/armadilhas + liga RoomController)"
```

---

## Fase 6 — Orquestração e integração

### Task 17: `DungeonGenerator` (orquestrador)

**Files:**
- Create: `Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs`

- [ ] **Step 1: Implementar**

`Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon;

/// <summary>
/// Orquestra a geração: planeja -> constrói -> bakeia NavMesh -> popula -> posiciona o player.
/// Ordem é crítica: NavMesh precisa estar pronto antes dos NavMeshAgent dos inimigos.
/// </summary>
public class DungeonGenerator : MonoBehaviour
{
    [Header("Dados")]
    public RoomCatalog catalog;
    public DungeonProfile profile;
    public EncounterTable encounterTable;

    [Header("Cena")]
    public DungeonBuilder builder;
    public NavMeshBaker navMeshBaker;
    public RoomPopulator populator;
    public Transform player;
    public GameObject defaultChestPrefab;

    [Header("Seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    public DungeonLayout CurrentLayout { get; private set; }

    public void Generate()
    {
        int usedSeed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : seed;

        List<RoomDefinition> defs = catalog.BuildDefinitions();
        DungeonSettings settings = profile.ToSettings();

        DungeonLayout layout = DungeonPlanner.Plan(defs, settings, usedSeed);
        if (!layout.Success)
        {
            Debug.LogError("[DungeonGenerator] Falha ao planejar a masmorra (seed " + usedSeed + ").");
            return;
        }
        CurrentLayout = layout;
        seed = layout.Seed;

        // 1. Construir.
        builder.Clear();
        List<GameObject> instances = builder.Build(layout);

        // 2. Bakear NavMesh (depois de tudo instanciado).
        navMeshBaker.Bake();

        // 3. Popular (inimigos desativados; ativam por trigger).
        var rng = new System.Random(layout.Seed);
        if (populator != null) populator.defaultChestPrefab = defaultChestPrefab;
        for (int i = 0; i < layout.Rooms.Count; i++)
        {
            PlannedRoom pr = layout.Rooms[i];
            populator.Populate(instances[i], pr.Definition.Type, pr.Depth, rng);
        }

        // 4. Posicionar o player na sala de início.
        PlacePlayerAtStart(layout, instances);
    }

    private void PlacePlayerAtStart(DungeonLayout layout, List<GameObject> instances)
    {
        if (player == null) return;
        for (int i = 0; i < layout.Rooms.Count; i++)
        {
            if (layout.Rooms[i].Definition.Type == RoomType.Inicio)
            {
                player.position = instances[i].transform.position + Vector3.up * 1f;
                return;
            }
        }
    }
}
```

- [ ] **Step 2: Compilar no Editor.** Expected: sem erros.

- [ ] **Step 3: Commit**
```bash
git add "Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs"
git commit -m "feat: DungeonGenerator (plan -> build -> bake -> populate -> player)"
```

### Task 18: Integração com `GameStateManager` e remoção do legado

**Files:**
- Modify: `Assets/Scenes/Scripts/GameStateManager.cs` (chamar `Generate()` ao iniciar o jogo)
- Delete: `Assets/Scenes/Scripts/SpawnerScript.cs`

- [ ] **Step 1: Disparar a geração**

Decidir o gancho conforme o fluxo do projeto. Abordagem recomendada (sem acoplar o GameStateManager ao gerador): num GameObject da cena de dungeon, chamar `DungeonGenerator.Generate()` no `Start()` via um pequeno bootstrap, OU adicionar no `GameStateManager` (quando entra em `Playing`) uma referência opcional:
```csharp
    [SerializeField] private DungeonGenerator dungeonGenerator;
```
e, no ponto em que o estado vira `Playing` pela primeira vez na cena de jogo:
```csharp
        if (dungeonGenerator != null) dungeonGenerator.Generate();
```
(Inserir guardando um flag para não regenerar a cada `Playing`/`Pause`.)

- [ ] **Step 2: Remover o spawner legado**
```bash
git rm "Assets/Scenes/Scripts/SpawnerScript.cs" "Assets/Scenes/Scripts/SpawnerScript.cs.meta"
```
Conferir que nenhum GameObject na cena referencia `EnemySpawner` (remover o componente se houver).

- [ ] **Step 3: Compilar no Editor.** Expected: sem erros, sem referências quebradas.

- [ ] **Step 4: Commit**
```bash
git add -A
git commit -m "feat: dispara geracao de masmorra no inicio do jogo; remove EnemySpawner legado"
```

### Task 19: Smoke test integrado [EDITOR]

**Files:** (cena/teste manual)

- [ ] **Step 1: Montar a cena de dungeon**

Numa cena (pode ser cópia da `MainScene`): GameObject raiz com `NavMeshSurface` + `NavMeshBaker`; objeto com `DungeonGenerator` (preencher catalog, profile, encounterTable, builder, navMeshBaker, populator, player, defaultChestPrefab, socketCapPrefab no builder).

- [ ] **Step 2: Play e verificar**

Expected:
- masmorra montada sem salas sobrepostas;
- sockets abertos selados com parede;
- player aparece na sala de início;
- ao entrar numa sala de combate, inimigos ativam e andam (NavMesh OK) e as portas trancam até limpar;
- flecha e melee matam inimigos e o boss.

- [ ] **Step 3: Testar reprodutibilidade**

Setar `useRandomSeed = false` e um `seed` fixo → duas Plays geram a mesma masmorra.

- [ ] **Step 4: Commit (ajustes de cena/prefab)**
```bash
git add -A
git commit -m "chore: cena de dungeon configurada para geracao procedural"
```

---

## Self-review (cobertura do spec)

- Topologia contínua conectada → Builder instancia tudo num espaço só (Task 12). ✓
- Encaixe por sockets + overlap + "backtracking" → PlacementMath + DungeonPlanner (regeneração por seed como forma pragmática de backtracking) (Tasks 4,7). ✓
- NavMesh runtime, ordem correta → NavMeshBaker + DungeonGenerator (Tasks 13,17). ✓
- Inimigos data-driven (esqueleto = zero código) → EnemySO/EncounterTable/RoomPopulator (Tasks 14,16). ✓
- IDamageable → Tasks 1,2. ✓
- Corredor-armadilha e parkour como salas comuns → convenção de prefab (Task 10). ✓
- Encounter rooms (trancar portas) → RoomController + DoorController.SetLocked (Task 15). ✓
- Seed/determinismo → System.Random único; testes de determinismo (Tasks 7,8,17). ✓
- Remover EnemySpawner → Task 18. ✓
- Testes EditMode (planner/math) → Tasks 5,8. ✓

**Desvio consciente do spec:** o spec menciona "backtracking (desfaz última sala)". O plano implementa **colocação gulosa com selagem + regeneração por seed** (limitada por `maxRegenerations`), que atinge o mesmo objetivo (layout válido garantido) com muito menos risco de bug que o undo recursivo. Documentado aqui e na docstring do `DungeonPlanner`.
