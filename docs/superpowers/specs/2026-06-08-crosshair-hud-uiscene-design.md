# Crosshair na UIScene (HUD_Canvas) + ponto para todas as classes

Data: 2026-06-08
Branch: feat/geracao-procedural-impl

## Problema

Hoje o crosshair do arqueiro é filho do Player e é controlado direto pelo
`CombatScript` via referências de Inspector (`crosshairUI`, `crosshairCanvasGroup`,
`chargeBarFill`). A barra de carga é criada em runtime (`CriarChargeBarSeNecessario`)
como filha do `crosshairUI`.

Queremos:

1. Tirar o crosshair de filho do Player e colocá-lo sob `HUD_Canvas` na `UIScene`.
2. Mostrar **um pontinho** central como mira para **todas as classes** durante o jogo.
3. No **Arqueiro**, ao segurar o tiro, mostrar o crosshair completo + barra de carga
   por cima do ponto; ao soltar/cancelar, voltar só o ponto.

Restrição central: `CombatScript` está na cena de gameplay e o crosshair vai para a
`UIScene` — **referência por Inspector não cruza cenas**. A ligação tem que ser em
runtime, seguindo o padrão de singleton já usado no projeto (`UIManager.Instance`,
`GameStateManager.Instance`).

## Comportamento desejado

- **Ponto** visível quando o estado é `Playing` e o jogador **não** está mirando.
  Some em `Pause`/`GameOver`/`Victory`/`InventoryCrafting`.
- **Arqueiro mirando**: esconde o ponto, mostra a arte de mira + barra de carga.
  A barra enche conforme `currentChargeTime / bowChargeDuration`.
- Ao atirar, cancelar (botão direito), soltar antes de carregar, ou sair de `Playing`
  (pause/inventário): esconde a mira e (se voltar a `Playing`) restaura o ponto.

Regra de visibilidade (estado interno do `CrosshairHUD`):

| isPlaying | isAiming | ponto | mira+barra |
|-----------|----------|-------|------------|
| false     | —        | off   | off        |
| true      | false    | on    | off        |
| true      | true     | off   | on         |

## Arquitetura

### `CrosshairHUD.cs` (novo)

Vive num objeto sob `HUD_Canvas` na `UIScene`. É dono de toda a UI de mira.
Singleton `CrosshairHUD.Instance` (set no `Awake`, limpo no `OnDestroy`).

Campos serializados (refs na MESMA cena — Inspector funciona):
- `GameObject dot` — o pontinho central.
- `GameObject aimGroup` — a arte de crosshair do arqueiro (inclui a barra como filha).
- `Image chargeBarFill` — o preenchimento da barra (largura por `anchorMax.x`).

Estado interno: `bool isPlaying`, `bool isAiming`.

Método privado `Refresh()` aplica a tabela acima:
- `dot.SetActive(isPlaying && !isAiming)`
- `aimGroup.SetActive(isPlaying && isAiming)`

API pública:
- `MostrarMira()` — `isAiming = true`; zera a barra (`chargeBarFill.rectTransform.anchorMax = (0,1)`); `Refresh()`.
- `SetCarga(float t01)` — `chargeBarFill.rectTransform.anchorMax = (Clamp01(t01), 1)`.
- `EsconderMira()` — `isAiming = false`; `Refresh()`.
- `AplicarEstado(GameState state)` — `isPlaying = (state == GameState.Playing)`;
  se `!isPlaying` então `isAiming = false`; `Refresh()`.

Todas as chamadas externas usam `CrosshairHUD.Instance?.X()` (null-safe), porque o
`CombatScript` pode existir antes da UIScene carregar.

### `CombatScript.cs` (modificado)

- Remover os campos `crosshairUI`, `crosshairCanvasGroup`, `chargeBarFill`.
- Remover o método `CriarChargeBarSeNecessario()` e sua chamada no `Start`
  (a barra passa a ser autorada na UIScene, não criada em runtime).
- Remover, no `Start`, as linhas que escondiam a UI (`crosshairUI.SetActive(false)` etc).
- Manter `camScript` (a câmera é objeto de gameplay; ref de Inspector continua válida).
- Substituir as chamadas de UI:
  - início da mira (`HandleArcherInput`, ao pressionar): `CrosshairHUD.Instance?.MostrarMira();`
  - carregando: `CrosshairHUD.Instance?.SetCarga(Mathf.Clamp01(currentChargeTime / bowChargeDuration));`
  - `ExecuteShot()` e `CancelAim()`: `CrosshairHUD.Instance?.EsconderMira();`
- A lógica de carga/tiro (`currentChargeTime`, `bowChargeDuration`, decisão de atirar)
  permanece no `CombatScript`; só o desenho da barra sai para o `CrosshairHUD`.

### `UIManager.cs` (modificado)

Em `ApplyGameState(GameState state)`, adicionar uma linha no fim:

```csharp
CrosshairHUD.Instance?.AplicarEstado(state);
```

`UIManager.ApplyGameState` já é o ponto único de fan-out de UI por estado (chamado por
`GameStateManager.ApplyState`), então o ponto liga/desliga junto com o resto do HUD.

## Fluxo de dados

```
Input (mouse) → CombatScript.HandleArcherInput
   ├─ pressiona  → controller.SetAiming(true), camScript.SetAimingCamera(true),
   │               CrosshairHUD.Instance.MostrarMira()
   ├─ segurando  → CrosshairHUD.Instance.SetCarga(t)
   └─ solta/cancela/atira → controller.SetAiming(false),
                            camScript.SetAimingCamera(false),
                            CrosshairHUD.Instance.EsconderMira()

GameStateManager.ApplyState → UIManager.ApplyGameState → CrosshairHUD.AplicarEstado(state)
```

## Trabalho no Editor (manual, fora do código)

Na `UIScene`, sob `HUD_Canvas`, criar:

1. Objeto `CrosshairHUD` (RectTransform centralizado) com o componente `CrosshairHUD`.
2. Filho **Ponto** (Image pequena, âncora central) → atribuir a `dot`.
3. Filho **MiraArqueiro** (reaproveitar a arte de crosshair atual do player) →
   atribuir a `aimGroup`. Dentro dele, **ChargeBar** (BG + Fill); atribuir o Fill a
   `chargeBarFill` (âncora `min=(0,0)`, `max` controlado em runtime).
4. No Player: remover o crosshair antigo. Os campos removidos somem do Inspector do
   `CombatScript` sozinhos.

A barra de carga reproduz o que `CriarChargeBarSeNecessario` fazia: BG escuro
(`~120x12`, offset y `-50`), Fill amarelo (`1, 0.85, 0`) com largura por `anchorMax.x`.

## Divisão de trabalho

- **Código (eu):** criar `CrosshairHUD.cs`; editar `CombatScript.cs` e `UIManager.cs`.
- **Editor (você):** montar a hierarquia na `UIScene` e remover o crosshair do Player,
  seguindo o passo a passo acima.

## Fora de escopo (YAGNI)

- Mira diferente por classe além do ponto (espadachim/lanceiro usam o mesmo ponto).
- Animação/tween de fade do crosshair (hoje é alpha 0/1; mantemos liga/desliga simples).
- Crosshair que segue a posição do mouse (continua centralizado na tela).

## Critérios de sucesso

- Ponto central aparece jogando com qualquer classe; some em pause/inventário/gameover.
- Arqueiro: segurar tiro mostra mira + barra enchendo; soltar carregado atira e some a
  mira; soltar cedo ou botão direito cancela e some a mira; ponto volta.
- Nenhuma referência de crosshair sobra no `CombatScript`; crosshair não é mais filho
  do Player; nada de UI é criado em runtime.
