# Portas entre salas com abertura por presença e lockdown de combate

**Data:** 2026-06-08
**Status:** Design aprovado — pronto para plano de implementação

## Problema

Hoje as salas da masmorra procedural não têm portas nos vãos que ligam uma sala à
outra. O `DungeonBuilder` só instancia o `socketCapPrefab` (tampão/parede) nos sockets
*abertos* (que não conectaram); os sockets que **conectam** duas salas não recebem nada,
deixando um buraco. Além disso, a `DoorController` atual é `IInteractable` (tecla E para
abrir/fechar manualmente), o que não é o comportamento desejado.

## Objetivo

1. **Porta não-interagível**: abre automaticamente por presença do player e fica aberta.
2. **Lockdown de combate**: ao entrar numa sala de combate e os inimigos serem ativados,
   todas as portas daquela sala fecham e trancam; ao limpar a sala, destrancam e reabrem.
3. **Colocar a porta entre salas** (vão de conexão), usando o prefab
   `Wood Doorframe(Abrivel) Variant` (tem `DoorController` + `Animator` + `BoxCollider`
   sólido).
4. **Manter o tampão/parede** (`socketCapPrefab`) nos sockets abertos, sem mudança.

## Decisões de comportamento (confirmadas)

- Abertura **por porta**: cada porta abre quando o player chega perto *dela* (gatilho de
  proximidade na própria porta) e fica aberta.
- Lockdown **prende o player**: trancam **todas** as portas da sala de combate (inclusive a
  de entrada) até a sala ser limpa.

## Arquitetura

### 1. Planner expõe as conexões (assembly Core)

Novo tipo em `Assets/Scenes/Scripts/Dungeon/Core/PlannedRoom.cs`:

```csharp
public class PlannedDoorway
{
    public Vector3 WorldPosition;
    public CardinalDirection WorldDirection;
    public PlannedRoom RoomA;   // sala existente (dona do socket aberto)
    public PlannedRoom RoomB;   // sala recém-colocada
}
```

Em `Assets/Scenes/Scripts/Dungeon/Core/DungeonLayout.cs`:

```csharp
public List<PlannedDoorway> Doorways = new List<PlannedDoorway>();
```

Em `DungeonPlanner.TryFit`, logo após marcar os sockets como `Used` (hoje nas linhas
136-137), registra o vão:

```csharp
layout.Doorways.Add(new PlannedDoorway
{
    WorldPosition = fe.Socket.WorldPosition,
    WorldDirection = fe.Socket.WorldDirection,
    RoomA = fe.Owner,
    RoomB = placed,
});
```

`TryFit` é o único ponto de conexão (usado tanto por salas normais quanto pela boss), então
isso cobre todas as conexões. Determinístico por seed.

### 2. `DungeonBuilder` instancia as portas

Em `Assets/Scenes/Scripts/Dungeon/DungeonBuilder.cs`:

- Novo campo: `public GameObject doorPrefab;`
- Novo tipo de retorno:

```csharp
public struct PlacedDoor
{
    public DoorController Door;
    public GameObject RoomA;
    public GameObject RoomB;
}
```

- Novo método:

```csharp
public List<PlacedDoor> BuildDoors(DungeonLayout layout, List<GameObject> roomInstances)
```

Comportamento:
- Constrói um `Dictionary<PlannedRoom, GameObject>` mapeando `layout.Rooms[i] → roomInstances[i]`.
- Para cada `PlannedDoorway`: se `doorPrefab == null`, pula; senão instancia `doorPrefab`
  em `WorldPosition` com rotação `Quaternion.LookRotation(CardinalDirections.ToVector(WorldDirection))`
  (mesma convenção do `socketCapPrefab`), parentado em `root`.
- Pega o `DoorController` do objeto instanciado e adiciona um `PlacedDoor` à lista de retorno,
  resolvendo `RoomA`/`RoomB` pelo dicionário.

O bloco do `socketCapPrefab` (tampão) em `Build()` continua **inalterado**.

### 3. `DoorController` reescrito

Substitui `Assets/Scenes/Scripts/DoorController.cs`. Deixa de ser `IInteractable`.

```csharp
using UnityEngine;

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

Pontos:
- **Abre por presença e fica aberta** (`OnTriggerEnter` abre; `OnTriggerExit` não fecha).
- **`lockCount`** (contador, não bool) é robusto se duas salas vizinhas trancarem a mesma porta.
- Ao **destrancar** com o player ainda na área, reabre — cobre "combate acabou e o player está
  parado dentro da sala".
- Mantém o trigger `"change"` do `Animator` (alterna aberto/fechado), guardado por `isOpen`
  para nunca dessincronizar. **Sem alterar o Animator Controller** (assume que ele alterna
  nesse trigger; verificar no Editor).

### 4. `RoomController` — portas registradas externamente

Em `Assets/Scenes/Scripts/Dungeon/RoomController.cs`:

- Remover a coleta por filhos em `Configure`:
  `doors.AddRange(GetComponentsInChildren<DoorController>(true));`
- Em `Configure`, manter `doors.Clear();` (as portas são registradas depois pelo gerador).
- Adicionar:

```csharp
public void RegisterDoor(DoorController d)
{
    if (d != null && !doors.Contains(d)) doors.Add(d);
}
```

- `Activate()` e a limpeza (`Update`) já chamam `door.SetLocked(true/false)` — **sem mudança**.
  Só a origem das portas mudou (registradas em vez de filhas).

### 5. Ordem no `DungeonGenerator.Generate()`

Em `Assets/Scenes/Scripts/Dungeon/DungeonGenerator.cs`, apenas a ordem do `Generate()` muda
(nenhum campo novo no gerador — `doorPrefab` mora no `DungeonBuilder`, ao lado de
`socketCapPrefab`):

```
1. builder.Build(layout)          // salas + tampões (como hoje)
2. navMeshBaker.Bake()            // ANTES das portas → navmesh não é afetada
3. popular salas (loop atual)     // RoomControllers passam a existir
4. List<PlacedDoor> doors = builder.BuildDoors(layout, instances);
5. foreach PlacedDoor pd in doors:
       pd.RoomA.GetComponent<RoomController>()?.RegisterDoor(pd.Door);
       pd.RoomB.GetComponent<RoomController>()?.RegisterDoor(pd.Door);
6. PlacePlayerAtStart(...)        // como hoje
```

`doorPrefab` fica no **`DungeonBuilder`** (Inspector), coerente com `socketCapPrefab`, e o
gerador só chama `BuildDoors`.

## Passos de Editor (não-código)

- No prefab `Wood Doorframe(Abrivel) Variant`: adicionar um **segundo collider** `BoxCollider`
  marcado como **trigger**, maior que o sólido (~3 m de alcance), para detectar a presença do
  player. O `BoxCollider` sólido existente permanece (bloqueia quando fechada).
- Confirmar que o `Animator` da porta alterna aberta/fechada no trigger `"change"`.
- Atribuir `doorPrefab` = `Wood Doorframe(Abrivel) Variant` no `DungeonBuilder` da cena.
- Confirmar que o player tem a tag `Player`.

## Determinismo e NavMesh

- Os vãos (`Doorways`) são derivados das conexões do planner → reproduzíveis por seed.
- Portas são instanciadas **depois** do bake de NavMesh, então não interferem no pathfinding.
  Inimigos são locais à sala (não cruzam portas), então o lockdown não afeta navegação.

## Fora de escopo (YAGNI)

- Som/efeito de tranca.
- Animação de "porta sacudindo" quando trancada.
- Mini-mapa / indicação visual de sala trancada.
- Tratamento especial para duas salas de combate adjacentes além do que o `lockCount` já cobre.

## Como resolve o pedido

- Porta deixa de ser interagível; abre por presença e fica aberta (DoorController + trigger).
- Combate fecha/tranca todas as portas da sala (RoomController.Activate já chama SetLocked).
- Portas passam a existir nos vãos entre salas (planner expõe Doorways, builder instancia).
- O tampão/parede continua nos sockets abertos, sem mudança.
