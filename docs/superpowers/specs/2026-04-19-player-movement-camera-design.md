# Design: Sistema de Movimentação e Câmera (Terceira Pessoa)

**Data:** 2026-04-19  
**Branch:** menu  
**Status:** Aprovado pelo usuário

---

## Visão Geral

Refatoração completa do sistema de movimentação do `FireKnightController` e da câmera `ThirdPersonCamera` para eliminar o conflito de yaw duplo e implementar um controle estilo shooter de terceira pessoa, onde o mouse controla câmera e o WASD move o personagem relativo à visão da câmera.

**Problema raiz identificado:** Tanto `PlayerController` quanto `ThirdPersonCamera` aplicavam `mouseX` independentemente ao yaw, gerando conflito de rotação.

---

## Hierarquia na Unity

```
Player                          ← Rigidbody, CapsuleCollider, Animator
│                                  FireKnightController, FireKnightCombat
├── GroundCheck                 ← Transform (já existe)
└── CameraPivot                 ← Transform vazio, posição configurável via Inspector
    └── Main Camera             ← filha do CameraPivot, sem scripts de movimento
```

O CameraPivot é filho do Player. Sua posição local (offset) é configurável via Inspector — não hardcoded.

---

## Scripts e Responsabilidades

### `ThirdPersonCamera.cs`

**Executa em:** `Update()`

**Responsabilidades:**
- Ler `mouse.delta.x` → acumular `currentYaw`
- Ler `mouse.delta.y` → acumular `currentPitch`
- Aplicar clamp no pitch: range configurável via Inspector (padrão: -80 a +80 graus)
- Aplicar pitch ao `cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0, 0)`
- Seguir posição do player: `transform.position = Vector3.Lerp(atual, target.position + offset, smoothSpeed * dt)`
- Colisão de câmera via `SphereCast` (comportamento existente mantido)
- Respeitar `GameStateManager.CanCameraLook` para bloquear input do mouse

**Expõe (read-only):**
```csharp
public Quaternion YawRotation => Quaternion.Euler(0f, currentYaw, 0f);
public float CurrentYaw => currentYaw; // para debug / lock-on futuro
```

**Referências via `[SerializeField]`:**
- `Transform target` — Player transform (apenas lê `position`, sem modificar)
- `Transform cameraPivot` — para aplicar pitch
- `float mouseSensitivity`
- `float minPitch`, `float maxPitch`
- `float smoothSpeed`
- `Vector3 offset` — distância/posição da câmera em relação ao pivot

**Não modifica:** `target.rotation` nem qualquer componente do Player.

---

### `FireKnightController.cs`

**Executa em:** `LateUpdate()` (rotação + input) e `FixedUpdate()` (física)

**`LateUpdate()` — rotação e input:**
- Checar `GameStateManager.CanPlayerMove`; se falso, zerar `moveDirection` e retornar
- Ler WASD via `Keyboard.current`
- Calcular direção de movimento:
  ```csharp
  Vector3 forward = cameraPivot.forward; forward.y = 0f; forward.Normalize();
  Vector3 right   = cameraPivot.right;   right.y   = 0f; right.Normalize();
  moveDirection = (forward * moveZ + right * moveX).normalized;
  ```
- Aplicar rotação do player (estilo shooter — sempre alinhado à câmera):
  ```csharp
  transform.rotation = Quaternion.Slerp(
      transform.rotation,
      cameraRig.YawRotation,
      rotationSpeed * Time.deltaTime
  );
  ```
- Processar input de pulo (Space) e dash (Shift/Alt)
- Atualizar booleans de animação (`isWalking`, `isRunning`)

**`FixedUpdate()` — física:**
- `MovePlayer()`: aplica `rb.linearVelocity = moveDirection * speed` (preserva Y)
- `HandleEvasion()`: lógica de dash (sem mudança estrutural)
- Gravidade gerida pelo Rigidbody (gravity habilitada, `freezeRotation = true`)

**Referências via `[SerializeField]`:**
- `ThirdPersonCamera cameraRig` — lê `YawRotation` (read-only)
- `Transform cameraPivot` — lê `forward` e `right` para direção de movimento
- `float rotationSpeed = 15f` — velocidade do Slerp

**Não modifica:** nenhuma propriedade da câmera.

---

### `FireKnightCombat.cs`

Sem mudança estrutural. Referencia `FireKnightController` via `GetComponent` para ler `isInvincible`. Pequeno cleanup: remover dependências desnecessárias se encontradas.

---

## Fluxo de Dados por Frame

```
FixedUpdate:
  FireKnightController → aplica velocity no Rigidbody (physics step)

Update:
  ThirdPersonCamera → lê mouse → atualiza currentYaw / currentPitch
                    → aplica pitch ao CameraPivot.localRotation
                    → move câmera para seguir player

LateUpdate:
  FireKnightController → lê YawRotation da câmera → Slerp player.rotation
                       → lê cameraPivot.forward/right → calcula moveDirection
                       → processa input de pulo / dash / animações
```

---

## Fluxo de Dependências

```
ThirdPersonCamera ──lê position──▶ Player.transform   (Transform puro, sem script)
FireKnightController ──lê──▶ ThirdPersonCamera.YawRotation  (propriedade read-only)
FireKnightController ──lê──▶ cameraPivot.forward / right    (SerializeField direto)
FireKnightCombat ──lê──▶ FireKnightController.isInvincible  (sem mudança)
```

Não há ciclo: a câmera não lê nenhum dado de script do player.

---

## Garantias de Qualidade

| Requisito | Solução |
|---|---|
| Sem jitter de câmera | `Vector3.Lerp` com `smoothSpeed * Time.deltaTime` |
| Sem jitter de rotação | `Quaternion.Slerp` com `rotationSpeed * Time.deltaTime` |
| FPS-independente | Todo `Lerp`/`Slerp` usa `Time.deltaTime` |
| Sem conflito de yaw | Apenas `ThirdPersonCamera` acumula yaw; player o lê passivamente |
| Movimento sem inclinação | `forward.y = 0` e `right.y = 0` antes de normalizar |
| Velocidade consistente | `moveDirection` normalizado antes de multiplicar por `speed` |
| Cursor travado | `GameStateManager.ApplyState` já gerencia (sem mudança) |

---

## Parâmetros Configuráveis via Inspector

### ThirdPersonCamera
- `mouseSensitivity` (float)
- `minPitch`, `maxPitch` (float, padrão: -80, +80)
- `smoothSpeed` (float, padrão: 10)
- `offset` (Vector3, padrão: (0, 0, -5))
- `collisionLayers` (LayerMask)
- `cameraRadius` (float, padrão: 0.2)

### FireKnightController
- `walkSpeed`, `runSpeed` (float)
- `jumpForce` (float)
- `dashForce`, `dashDuration`, `dashCooldown` (float)
- `rotationSpeed` (float, padrão: 15)
- `groundDistance` (float)
- `groundMask` (LayerMask)

---

## Setup na Unity (passo a passo)

1. Criar GameObject vazio `CameraPivot` como filho do Player
2. Definir posição local do `CameraPivot` a ~(0, 1.6, 0) via Inspector
3. Mover `Main Camera` para ser filha do `CameraPivot`
4. Definir posição local da Camera a (0, 0, -5) via Inspector
5. No componente `ThirdPersonCamera`: arrastar Player em `target`, arrastar `CameraPivot` em `cameraPivot`
6. No componente `FireKnightController`: arrastar `ThirdPersonCamera` em `cameraRig`, arrastar `CameraPivot` em `cameraPivot`
7. Remover quaisquer scripts de câmera antigos que possam conflitar

---

## O que NÃO muda

- `GameStateManager`: nenhuma modificação
- `FireKnightCombat`: apenas cleanup menor, sem mudança de interface
- Lógica de dash/evasion: mantida
- Lógica de animações: mantida (`isWalking`, `isRunning`, `jumpTrigger`)
- Colisão de câmera via `SphereCast`: mantida
