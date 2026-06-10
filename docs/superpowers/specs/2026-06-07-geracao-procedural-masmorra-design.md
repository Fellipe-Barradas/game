# Geração Procedural de Masmorra — Design

- **Data:** 2026-06-07
- **Projeto:** 3D action RPG (Unity 6000.3.11f1)
- **Status:** Aprovado para implementação (pendente revisão do spec)

## Objetivo

Gerar masmorras proceduralmente a cada partida, montando salas hand-designed
(combate, baú, corredor com armadilha, parkour, boss) numa **masmorra contínua
conectada** — todas as salas no mesmo espaço 3D, exploradas andando, ligadas por
portas/corredores. Layout aleatório (por `seed`), **conteúdo das salas autorado**.

## Decisões de design (travadas)

1. **Topologia:** masmorra contínua conectada (não é sala-por-vez nem linear).
2. **Montagem:** encaixe por sockets (door-snap) — porta com porta, com checagem
   de overlap e backtracking. Formas/tamanhos de sala livres.
3. **NavMesh:** bake em runtime via `NavMeshSurface` (pacote `com.unity.ai.navigation`
   já instalado), uma vez, depois de montar tudo.
4. **Inimigos data-driven:** tipos de inimigo são ScriptableObjects; adicionar um
   novo inimigo (ex.: esqueleto) é zero código.
5. **Determinismo:** um único `System.Random(seed)` para toda aleatoriedade.

## Princípio arquitetural central: Planner ≠ Builder

Separar **planejar o layout** de **construir o layout**:

- **`DungeonPlanner` (C# puro, sem GameObject):** roda o algoritmo trabalhando só
  com dados (AABBs + sockets). Rápido, determinístico e **testável em EditMode**.
- **`DungeonBuilder` (Unity):** recebe o plano pronto e instancia prefabs, sela
  sockets, dispara bake e população.

Benefício: o algoritmo (a parte com mais risco de bug) é unit-testável sem abrir cena.

## Componentes

| Componente | Tipo | Responsabilidade |
|---|---|---|
| `Room` | MonoBehaviour (raiz do prefab) | Expõe sockets, AABB local e `RoomType`. |
| `DoorSocket` | MonoBehaviour/marker | Vão de porta: posição local + direção cardeal "pra fora"; estado usado/aberto. |
| `RoomType` | enum | `Inicio, Combate, Bau, Armadilha, Parkour, Boss`. |
| `RoomCatalog` | ScriptableObject | Listas ponderadas de prefabs de sala por `RoomType`. |
| `DungeonProfile` | ScriptableObject | Parâmetros da run: orçamento de salas, cotas por tipo, `maxAttempts`, prefab de tampa de socket. |
| `DungeonPlanner` | C# puro | O algoritmo: gera o grafo de salas posicionadas a partir do `seed`. |
| `PlannedRoom` / `DungeonLayout` | classes de dados | Resultado do planner: salas escolhidas (prefab ref, posição, rotação Y, profundidade) + conexões. |
| `DungeonBuilder` | MonoBehaviour | Instancia o layout sob um objeto-raiz; sela sockets abertos. |
| `NavMeshBaker` | MonoBehaviour | `NavMeshSurface.BuildNavMesh()` após construir. |
| `RoomPopulator` | MonoBehaviour | Por sala, instancia conteúdo a partir de markers; ativa por trigger. |
| `EnemyMarker` / `ChestMarker` / `TrapMarker` | markers | Pontos de spawn dentro do prefab da sala (não amarram prefab de conteúdo). |
| `EnemySO` | ScriptableObject | Um por tipo de inimigo: prefab, peso, custo de orçamento, profundidade mínima. |
| `EncounterTable` | ScriptableObject | Lista ponderada de `EnemySO` + orçamento por sala (escala com profundidade). |
| `IDamageable` | interface | `void TakeDamage(int dano);` — implementada por `EnemyDummy` e `BossEnemy`. |

## Convenção de autoria dos prefabs de sala

Você desenha as salas à mão; o gerador só as embaralha.

- Raiz: componente `Room` + `BoxCollider` (isTrigger) cobrindo a sala. O collider
  serve para (a) bounds e (b) detectar entrada do jogador.
- Filhos vazios em cada porta: `DoorSocket`, com forward apontando pra fora.
- Filhos vazios de spawn: `EnemyMarker`, `ChestMarker`, `TrapMarker`.
- **Corredor-com-armadilha e sala de parkour são apenas salas com 2 sockets**
  (entrada/saída) e geometria/armadilhas/parkour já montadas no prefab. Não há
  código especial — entram no mesmo pipeline.

## Algoritmo do `DungeonPlanner` (frontier + backtracking)

1. Coloca a sala de **início** na origem. Registra seu AABB-mundo e enfileira seus
   sockets numa **fronteira** (sockets abertos).
2. Loop até atingir o orçamento de salas (ex.: 8–14):
   - Tira um socket aberto da fronteira (escolha via `seed`).
   - Sorteia um `RoomType` respeitando as **cotas** do `DungeonProfile` e pega um
     prefab do `RoomCatalog`.
   - Escolhe um socket de entrada da sala nova e calcula a transformação
     (posição + yaw 0/90/180/270) que encosta o socket novo no socket aberto,
     direções opostas.
   - **Teste de overlap:** AABB transformado da sala nova vs. todos os AABBs já
     colocados, com margem levemente negativa (permite parede compartilhada).
     Teste matemático puro, sem `Physics`.
   - Colidiu → tenta outro prefab/rotação; esgotou candidatos no socket → fecha o
     socket (vira parede); travou geral → **backtrack** (desfaz última sala),
     limitado por `maxAttempts`.
   - Encaixou → comita: registra AABB, marca os dois sockets como usados, adiciona
     os sockets restantes à fronteira; grava `profundidade` = distância no grafo.
3. **Obrigatórias:** início é fixo; **boss** é colocado por último num socket de
   maior profundidade. Se o orçamento acabar sem boss, força a última sala válida
   como boss.
4. **Sela** todos os sockets abertos restantes com o prefab de tampa.

### Cotas / gramática leve (no `DungeonProfile`)

Ex.: `{ Combate: 3–5, Bau: 1–2, Armadilha: 1–2, Parkour: 1, Boss: 1 }`. Mantém
variedade sem IA de geração complexa.

### Tratamento de falha

Se o planner não fechar um layout válido em `maxAttempts`, **regenera com o próximo
seed** e loga (não trava). O builder nunca recebe layout inválido.

## NavMesh

- `NavMeshSurface` no objeto-raiz da masmorra, `Collect Objects = Children`.
- Após o builder instanciar tudo: `surface.BuildNavMesh()` **uma vez**.
- **Ordem obrigatória:** gerar → construir → **bakear** → spawnar/ativar inimigos.
  `NavMeshAgent` (usado por `EnemyDummy` e `BossEnemy`) exige o mesh pronto.

## População e ativação (ritmo)

- `RoomPopulator` roda por sala após o bake: instancia conteúdo a partir dos
  markers, com inimigos **desativados**.
- O `BoxCollider` trigger da sala detecta a entrada do jogador → ativa os inimigos
  daquela sala.
- Sala de **combate**: ao entrar, tranca portas (`DoorController`) até zerar os
  inimigos; ao limpar, destranca (encounter room).
- Evita a masmorra inteira ativa de uma vez (performance) e dá ritmo de jogo.

## Controle de inimigos (extensível)

### Data-driven
- `EnemySO` por tipo (InimigoCuca agora; Esqueleto depois): prefab, peso, custo,
  profundidade mínima.
- `EncounterTable`: lista ponderada de `EnemySO` + orçamento por sala que escala
  com a profundidade.
- `RoomPopulator` preenche os `EnemyMarker` sorteando da tabela pelo `seed`,
  gastando o orçamento.
- **Adicionar o esqueleto = criar prefab + `EnemySO` + arrastar na `EncounterTable`.
  Zero código.**

### `IDamageable` (remove acoplamento por tipo)
- Hoje flecha (`ProjectileScript`) e melee (`CombatScript.PerformMeleeAttack`) fazem
  `GetComponent<EnemyDummy>` **e** `GetComponent<BossEnemy>` separados — origem do
  bug do boss não levar dano de flecha.
- Criar `interface IDamageable { void TakeDamage(int dano); }`.
- `EnemyDummy` e `BossEnemy` passam a implementá-la (já têm `TakeDamage(int)`).
- Flecha e melee passam a chamar `GetComponentInParent<IDamageable>()?.TakeDamage(dano)`.
- Esqueleto novo só implementa `IDamageable` e já leva dano de tudo.

## Determinismo / seed

- Um único `System.Random(seed)` para todas as escolhas (socket, tipo, prefab,
  inimigos). Seed guardado no `GameStateManager` → masmorra reprodutível
  (debug e "compartilhar seed").

## Integração com o existente

- `GameStateManager` dispara a geração ao entrar em `Playing` (ou numa cena de
  dungeon dedicada). Player posicionado na sala de início **após** o bake.
- Reaproveita: `DoorController` (trancar/destrancar), `ChestController`,
  `BossEnemy`, `EnemyDummy`, prefab `InimigoCuca`.
- **Remover** o `EnemySpawner` (código legado, fora do pipeline novo).

## Testes

- **EditMode (`DungeonPlanner`, C# puro):**
  - nunca gera salas sobrepostas;
  - sempre há exatamente 1 início e 1 boss;
  - boss fica a ≥ N de profundidade do início;
  - mesmo seed → layout idêntico;
  - respeita as cotas do `DungeonProfile`;
  - falha em `maxAttempts` → regenera, nunca retorna layout inválido.
- **PlayMode (smoke):** gerar + construir + bakear → existe NavMesh válido na sala
  de início; player consegue spawnar lá.

## Fora de escopo (YAGNI por enquanto)

- Múltiplos andares / transição entre níveis.
- Minimapa (o `DungeonLayout` já guarda dados suficientes para um futuro minimapa).
- Salas com múltiplos footprints irregulares além do AABB (AABB é suficiente).
- Loot/dificuldade dinâmica além da escala por profundidade.

## Ordem de implementação (resumo; detalhada no plano)

1. Fundação: `IDamageable` + adotar em `EnemyDummy`/`BossEnemy`, flecha e melee.
2. Convenções: `Room`, `DoorSocket`, `RoomType`, markers; montar 1 prefab por tipo.
3. `DungeonPlanner` + dados (`PlannedRoom`, `DungeonLayout`) + **EditMode tests**.
4. `DungeonBuilder` + selagem de sockets.
5. `NavMeshBaker` + ordem de bake.
6. Inimigos: `EnemySO`, `EncounterTable`, `RoomPopulator` + ativação por trigger.
7. Integração com `GameStateManager` + remover `EnemySpawner`.
