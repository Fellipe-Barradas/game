# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

3D action RPG desenvolvido em **Unity 6000.3.11f1**. Jogador escolhe entre 3 classes (Espadachim, Lanceiro, Arqueiro) e enfrenta inimigos em dungeons geradas. Código-fonte em C#, comentários e nomes em português.

## Build & Desenvolvimento

Este é um projeto Unity — não há CLI de build separado. Toda compilação, execução e testes ocorrem pelo **Unity Editor**:

- Abrir: `File > Open Project` apontando para esta pasta
- Play: botão ▶ no Editor (roda a cena ativa)
- Build: `File > Build Settings > Build`
- Logs de erro: Console do Editor (não há test runner configurado)

Para editar scripts, abrir `.cs` diretamente ou via `Assets/Open C# Project` (abre no IDE configurado).

## Arquitetura

### Sistemas centrais

**`GameStateManager`** — Singleton que persiste entre cenas (`DontDestroyOnLoad`). É o hub de estado global:
- Estados: `InitialScreen`, `Playing`, `Pause`, `GameOver`, `InventoryCrafting`
- Propriedades consultadas por todos os outros sistemas: `CanPlayerMove`, `CanPlayerAct`, `CanCameraLook`
- Controla `Time.timeScale` e lock do cursor
- Hotkeys globais tratadas aqui (Esc, I/Tab, C)

**`UIManager`** — Singleton de UI. Escuta mudanças de estado do `GameStateManager` e mostra/esconde os canvases (HUD, inventário, pause).

**`GerenciadorMoedas`** — Singleton do sistema de moedas (ouro/prata). `MoedasHUD` escuta o evento `OnMoedasMudaram`.

**`MusicManager`** — Singleton de áudio. Gerencia transições de música por cena/área.

### Fluxo de combate

1. **Melee**: Input → `FireKnightCombat.Attack()` → animação toca → evento de animação dispara `ExecuteAttackEvent()` → BoxCast detecta inimigos → `TakeDamage()`
2. **Ranged (Arqueiro)**: Segurar clique → `isChargingShot=true` → rig de mira blendado em `FireKnightController.UpdateRigWeight()` → soltar → evento de animação spawna `Projectile`
3. **Bloqueio**: Botão direito → `isBlocking=true` → dano ignorado em `TakeDamage()`

### Inventário

`Inventory.cs` (no player) ↔ `InventoryUI.cs` (UI) comunicam via eventos:
- `OnInventoryChanged` → InventoryUI redesenha slots
- `OnEquippedChanged` → hotbar muda opacidade
- `Slot.cs` (execution order -200) inicializa antes de `InventoryUI.cs` (order -100)
- Pickup: raycast do centro da tela (tecla E, alcance 3m)
- Item no mundo: componente `Item.cs` com referência a `ItemSO` (ScriptableObject)

### ScriptableObjects

- **`WeaponData`**: dano, cadência, alcance, sons — cria via menu `Fire Knight/Weapon Data`
- **`ItemSO`**: nome, ícone, max stack, prefab do item no mundo

### IA de Inimigos

`EnemyScript.cs` usa `NavMeshAgent` + máquina de estados por enum:
`Chasing → Attacking → HitStun → Dead`

Requer **NavMesh baked** na cena para pathfinding funcionar.

### Layers importantes

| Layer | Número | Uso |
|-------|--------|-----|
| ground | 3 | Detecção de chão |
| enemy | 6 | Raycast de ataque |
| player | 7 | Detecção pelo inimigo |

### Classes do jogador

Enum `PlayerClass`: `Espadachim`, `Lanceiro`, `Arqueiro`. Selecionada no menu e armazenada em `GameStateManager.SelectedClass`. Controla qual layer do `NossoAnimatorController` é ativado e quais ataques estão disponíveis em `FireKnightCombat`.
