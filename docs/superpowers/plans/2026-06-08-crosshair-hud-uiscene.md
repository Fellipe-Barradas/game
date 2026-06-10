# Crosshair na UIScene (HUD_Canvas) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mover o crosshair do arqueiro de filho do Player para `HUD_Canvas` na UIScene, com um ponto central para todas as classes e mira+barra de carga só no arqueiro ao mirar.

**Architecture:** Um novo singleton `CrosshairHUD` (na UIScene) é dono de toda a UI de mira; `CombatScript` (gameplay) fala com ele via `CrosshairHUD.Instance` (cross-scene, null-safe); `UIManager.ApplyGameState` liga/desliga o ponto por estado de jogo.

**Tech Stack:** Unity 6000.3.11f1, C#, uGUI (`UnityEngine.UI`).

> **Sobre testes:** este projeto **não tem test runner** (ver `CLAUDE.md`) e o código é MonoBehaviour/UI que precisa do engine. Portanto a verificação de cada task é **manual no Editor (Play mode)**, observando Console e tela. Não há testes automatizados.

> **Ordem segura:** Tasks 1–3 (código) podem ser feitas antes do setup do Editor (Task 4). Como todas as chamadas usam `CrosshairHUD.Instance?.` (null-safe), entre a Task 3 e a Task 4 o jogo compila e roda **sem crosshair** (sem crash). A verificação visual completa só vale **depois** da Task 4.

---

## File Structure

- **Create** `Assets/Scenes/UI/Scripts/CrosshairHUD.cs` — singleton dono da UI de mira (ponto, mira, barra). Vive na UIScene.
- **Modify** `Assets/Scenes/UI/Scripts/UIManager.cs` — uma linha em `ApplyGameState` para repassar o estado ao `CrosshairHUD`.
- **Modify** `Assets/Scenes/Scripts/CombatScript.cs` — remove refs de UI e criação em runtime da barra; troca por chamadas ao `CrosshairHUD.Instance`.
- **Editor (manual)** `Assets/Scenes/UI/UIScene.unity` e o Player — montar a hierarquia do crosshair e remover o antigo.

---

### Task 1: Criar `CrosshairHUD.cs`

**Files:**
- Create: `Assets/Scenes/UI/Scripts/CrosshairHUD.cs`

- [ ] **Step 1: Criar o arquivo com o conteúdo completo**

```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dono da UI de mira no HUD (UIScene). Mostra um ponto central para todas as classes
/// e, no arqueiro, troca pelo crosshair + barra de carga enquanto mira.
/// O CombatScript fala com este singleton (cross-scene) em vez de referenciar a UI direto.
/// </summary>
public class CrosshairHUD : MonoBehaviour
{
    public static CrosshairHUD Instance { get; private set; }

    [SerializeField] private GameObject dot;          // pontinho central (todas as classes)
    [SerializeField] private GameObject aimGroup;     // arte de mira do arqueiro (contém a barra)
    [SerializeField] private Image chargeBarFill;     // preenchimento da barra (largura por anchorMax.x)

    private bool isPlaying;
    private bool isAiming;

    private void Awake()
    {
        Instance = this;
        // Estado inicial coerente; UIManager.ApplyGameState chama AplicarEstado logo depois.
        isPlaying = GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState == GameState.Playing;
        isAiming = false;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Começa a mirar (arqueiro): esconde o ponto, mostra a mira e zera a barra.</summary>
    public void MostrarMira()
    {
        isAiming = true;
        if (chargeBarFill != null)
            chargeBarFill.rectTransform.anchorMax = new Vector2(0f, 1f);
        Refresh();
    }

    /// <summary>Atualiza o preenchimento da barra de carga (0 = vazio, 1 = cheio).</summary>
    public void SetCarga(float t01)
    {
        if (chargeBarFill != null)
            chargeBarFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(t01), 1f);
    }

    /// <summary>Para de mirar: volta o ponto (se ainda estiver jogando).</summary>
    public void EsconderMira()
    {
        isAiming = false;
        Refresh();
    }

    /// <summary>Liga/desliga conforme o estado do jogo (chamado por UIManager.ApplyGameState).</summary>
    public void AplicarEstado(GameState state)
    {
        isPlaying = state == GameState.Playing;
        if (!isPlaying) isAiming = false;
        Refresh();
    }

    private void Refresh()
    {
        if (dot != null) dot.SetActive(isPlaying && !isAiming);
        if (aimGroup != null) aimGroup.SetActive(isPlaying && isAiming);
    }
}
```

- [ ] **Step 2: Verificar compilação no Editor**

Volte ao Unity (ele recompila ao focar a janela). Abra o Console.
Esperado: **sem erros de compilação**. Não há comportamento visível ainda (o objeto na cena vem na Task 4).

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scenes/UI/Scripts/CrosshairHUD.cs"
git commit -m "feat: CrosshairHUD (singleton dono da UI de mira no HUD)"
```

---

### Task 2: Repassar estado do jogo ao `CrosshairHUD` no `UIManager`

**Files:**
- Modify: `Assets/Scenes/UI/Scripts/UIManager.cs` (método `ApplyGameState`)

- [ ] **Step 1: Adicionar a chamada no fim de `ApplyGameState`**

Localize o fim do método `ApplyGameState`:

```csharp
        gameOverCanvas.SetActive(state == GameState.GameOver);
        if (winCanvas != null) winCanvas.SetActive(state == GameState.Victory);
    }
```

Troque por (adiciona a última linha antes do `}`):

```csharp
        gameOverCanvas.SetActive(state == GameState.GameOver);
        if (winCanvas != null) winCanvas.SetActive(state == GameState.Victory);

        // Ponto/mira: visível só em Playing (some em pause/inventário/gameover/vitória).
        CrosshairHUD.Instance?.AplicarEstado(state);
    }
```

- [ ] **Step 2: Verificar compilação no Editor**

No Unity, abra o Console.
Esperado: **sem erros**. (CrosshairHUD.Instance será null até a Task 4 — a chamada é null-safe.)

- [ ] **Step 3: Commit**

```bash
git add "Assets/Scenes/UI/Scripts/UIManager.cs"
git commit -m "feat: UIManager repassa estado ao CrosshairHUD"
```

---

### Task 3: Migrar o `CombatScript` para o `CrosshairHUD`

**Files:**
- Modify: `Assets/Scenes/Scripts/CombatScript.cs`

- [ ] **Step 1: Remover o `using` de UI (não mais usado)**

Localize no topo:

```csharp
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.InputSystem;
```

Troque por (remove a linha `using UnityEngine.UI;`):

```csharp
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
```

- [ ] **Step 2: Remover os campos de UI do crosshair (mantendo `camScript`)**

Localize:

```csharp
    [Header("Mira e Câmera (Arqueiro)")]
    public GameObject crosshairUI; // Arraste a UI da mira aqui
    public ThirdPersonCamera camScript; // Arraste a Câmera Principal aqui

    public Image chargeBarFill; // NOVO: A imagem que vai "encher"
    public CanvasGroup crosshairCanvasGroup; // NOVO: Para fazer a mira aparecer suavemente
```

Troque por:

```csharp
    [Header("Mira e Câmera (Arqueiro)")]
    public ThirdPersonCamera camScript; // Arraste a Câmera Principal aqui
```

- [ ] **Step 3: Remover, no `Start`, a inicialização da UI e a criação da barra**

Localize:

```csharp
        // 1. Esconde a UI da mira por padrão
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (crosshairCanvasGroup != null) crosshairCanvasGroup.alpha = 0f;

        CriarChargeBarSeNecessario();

        // 2. PEGA A ARMA DO MENU
```

Troque por:

```csharp
        // 2. PEGA A ARMA DO MENU
```

- [ ] **Step 4: Remover o método `CriarChargeBarSeNecessario` inteiro**

Apague este bloco completo:

```csharp
    private void CriarChargeBarSeNecessario()
    {
        if (chargeBarFill != null || crosshairUI == null) return;

        // Fundo escuro — filho do crosshairUI para sumir/aparecer junto
        GameObject fundo = new GameObject("ChargeBar_BG");
        fundo.transform.SetParent(crosshairUI.transform, false);
        RectTransform fundoRect = fundo.AddComponent<RectTransform>();
        fundoRect.anchorMin = new Vector2(0.5f, 0.5f);
        fundoRect.anchorMax = new Vector2(0.5f, 0.5f);
        fundoRect.sizeDelta = new Vector2(120f, 12f);
        fundoRect.anchoredPosition = new Vector2(0f, -50f);
        Image fundoImg = fundo.AddComponent<Image>();
        fundoImg.color = new Color(0f, 0f, 0f, 0.6f);

        // Preenchimento amarelo — largura controlada por anchorMax.x (0 = vazio, 1 = cheio)
        GameObject fill = new GameObject("ChargeBar_Fill");
        fill.transform.SetParent(fundo.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f); // começa com largura 0
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        chargeBarFill = fill.AddComponent<Image>();
        chargeBarFill.color = new Color(1f, 0.85f, 0f);
    }
```

- [ ] **Step 5: Trocar o show da mira no início do tiro (`HandleArcherInput`)**

Localize:

```csharp
            if (crosshairUI != null) crosshairUI.SetActive(true);
            if (crosshairCanvasGroup != null) crosshairCanvasGroup.alpha = 1f;
            if (chargeBarFill != null) chargeBarFill.rectTransform.anchorMax = new Vector2(0f, 1f);

            if (camScript != null) camScript.SetAimingCamera(true);
```

Troque por:

```csharp
            CrosshairHUD.Instance?.MostrarMira();

            if (camScript != null) camScript.SetAimingCamera(true);
```

- [ ] **Step 6: Trocar a atualização da barra durante a carga**

Localize:

```csharp
            if (chargeBarFill != null)
            {
                float t = Mathf.Clamp01(currentChargeTime / bowChargeDuration);
                chargeBarFill.rectTransform.anchorMax = new Vector2(t, 1f);
            }
```

Troque por:

```csharp
            CrosshairHUD.Instance?.SetCarga(currentChargeTime / bowChargeDuration);
```

- [ ] **Step 7: Trocar o hide da mira no `ExecuteShot`**

Localize (dentro de `ExecuteShot`):

```csharp
        // Esconde UI e Zoom
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (camScript != null) camScript.SetAimingCamera(false);

        float rate = currentWeapon != null ? currentWeapon.attackRate : 1f;
```

Troque por:

```csharp
        // Esconde UI e Zoom
        CrosshairHUD.Instance?.EsconderMira();
        if (camScript != null) camScript.SetAimingCamera(false);

        float rate = currentWeapon != null ? currentWeapon.attackRate : 1f;
```

- [ ] **Step 8: Trocar o hide da mira no `CancelAim`**

Localize (dentro de `CancelAim`, é o bloco final do método):

```csharp
        // Esconde UI e Zoom
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (camScript != null) camScript.SetAimingCamera(false);
    }
```

Troque por:

```csharp
        // Esconde UI e Zoom
        CrosshairHUD.Instance?.EsconderMira();
        if (camScript != null) camScript.SetAimingCamera(false);
    }
```

- [ ] **Step 9: Verificar compilação no Editor**

No Unity, abra o Console.
Esperado: **sem erros**. O Inspector do Player (componente CombatScript) agora **não** mostra mais `crosshairUI`, `chargeBarFill`, `crosshairCanvasGroup`. `camScript` continua lá.

- [ ] **Step 10: Commit**

```bash
git add "Assets/Scenes/Scripts/CombatScript.cs"
git commit -m "refactor: CombatScript usa CrosshairHUD.Instance (mira fora do Player)"
```

---

### Task 4: Montar a hierarquia na UIScene e remover o crosshair do Player (Editor — manual)

**Files:**
- Modify (editor): `Assets/Scenes/UI/UIScene.unity`
- Modify (editor): o Player na cena/prefab de gameplay (`Assets/Scenes/MainScene/MainScene.unity` ou o prefab do Player)

- [ ] **Step 1: Abrir a UIScene**

No Unity: abra `Assets/Scenes/UI/UIScene.unity`. Encontre o objeto `HUD_Canvas`.

- [ ] **Step 2: Criar o objeto `CrosshairHUD` sob `HUD_Canvas`**

- Botão direito em `HUD_Canvas` → `Create Empty` → renomeie para `CrosshairHUD`.
- No RectTransform: âncora central (preset Alt+Shift → middle-center), `anchoredPosition = (0,0)`, `sizeDelta = (0,0)`.
- Adicione o componente `CrosshairHUD` (Add Component → CrosshairHUD).

- [ ] **Step 3: Criar o `Ponto` (mira de todas as classes)**

- Botão direito em `CrosshairHUD` → `UI > Image` → renomeie para `Ponto`.
- RectTransform centralizado, `sizeDelta` pequeno (ex.: `(8, 8)`), cor branca (ou a que preferir). Pode trocar o Sprite por um círculo se tiver.

- [ ] **Step 4: Criar o `MiraArqueiro` (arte de mira + barra)**

- Botão direito em `CrosshairHUD` → `Create Empty` → renomeie para `MiraArqueiro`. RectTransform centralizado (`anchoredPosition (0,0)`).
- Reaproveite a arte de crosshair que estava no Player: arraste-a para dentro de `MiraArqueiro` (ou crie uma `UI > Image` nova com o sprite da mira), centralizada.
- Dentro de `MiraArqueiro`, recrie a barra de carga (igual ao que o código fazia):
  - `UI > Image` chamado `ChargeBar_BG`: âncora central, `sizeDelta (120, 12)`, `anchoredPosition (0, -50)`, cor `(0,0,0,0.6)`.
  - Filho `UI > Image` chamado `ChargeBar_Fill`: âncoras `min (0,0)` e `max (0,1)`, `offsetMin/offsetMax = 0`, cor `(1, 0.85, 0)` (amarelo).

- [ ] **Step 5: Ligar as referências no componente `CrosshairHUD`**

Selecione o objeto `CrosshairHUD` e arraste para os campos:
- `dot` ← `Ponto`
- `aimGroup` ← `MiraArqueiro`
- `chargeBarFill` ← `ChargeBar_Fill`

- [ ] **Step 6: Remover o crosshair antigo do Player**

Abra a cena/prefab de gameplay. No Player, encontre o objeto de crosshair antigo (o que estava ligado em `CombatScript.crosshairUI`) e **apague-o**. Confirme que o componente `CombatScript` ainda tem `camScript` preenchido.

- [ ] **Step 7: Salvar e commitar as cenas/prefab**

Salve as cenas (Ctrl+S em cada). Depois:

```bash
git add "Assets/Scenes/UI/UIScene.unity" "Assets/Scenes/MainScene/MainScene.unity"
git commit -m "feat(scene): crosshair no HUD_Canvas e removido do Player"
```

> Se o crosshair antigo estava num **prefab** do Player (não na cena), inclua o `.prefab` correspondente no `git add`.

---

### Task 5: Verificação manual (Play mode)

**Files:** nenhum (verificação).

- [ ] **Step 1: Testar classe melee (Espadachim/Lanceiro)**

Inicie o jogo com Espadachim (ou Lanceiro). Esperado:
- Um **ponto** central aparece e fica fixo durante o jogo.
- Atacar não muda o ponto.

- [ ] **Step 2: Testar o Arqueiro — fluxo de mira**

Inicie com Arqueiro. Esperado:
- Ponto central visível parado.
- **Segurar** o tiro: o ponto some, aparece a **mira + barra** enchendo do zero.
- **Soltar carregado** (barra cheia): atira a flecha e a mira some, volta o ponto.
- **Soltar cedo** (barra incompleta): cancela, mira some, volta o ponto.
- **Botão direito** enquanto carrega: cancela, mira some, volta o ponto.

- [ ] **Step 3: Testar visibilidade por estado**

Com qualquer classe, em `Playing`:
- Abrir **inventário** (I/Tab) → o ponto **some**. Fechar → ponto volta.
- **Pause** (Esc) → ponto some. Voltar → ponto volta.
- (Se der pra testar) GameOver/Victory → ponto some.

- [ ] **Step 4: Conferir o Console**

Esperado: **sem NullReference** nem warnings novos relacionados a crosshair/mira.

---

## Self-Review

**Spec coverage:**
- Crosshair sob HUD_Canvas na UIScene → Tasks 1, 4. ✓
- Ponto para todas as classes → `CrosshairHUD.dot` + `Refresh()` (Task 1), verificação Task 5 Step 1. ✓
- Arqueiro: mira+barra ao segurar, some ao soltar/cancelar → Task 3 Steps 5–8, verificação Task 5 Step 2. ✓
- Cross-scene via `CrosshairHUD.Instance` → Tasks 1, 3. ✓
- Ponto some em pause/inventário → Task 2 + `AplicarEstado` (Task 1), verificação Task 5 Step 3. ✓
- Remover refs de UI do CombatScript e criação em runtime → Task 3 Steps 1–4. ✓
- Remover crosshair de filho do Player → Task 4 Step 6. ✓

**Placeholder scan:** sem TBD/TODO; todo passo de código mostra o código exato. ✓

**Type/nome consistency:** `MostrarMira()`, `SetCarga(float)`, `EsconderMira()`, `AplicarEstado(GameState)`, campos `dot`/`aimGroup`/`chargeBarFill` — usados de forma idêntica nas Tasks 1, 2 e 3. `GameState`/`GameStateManager` são tipos globais já existentes no projeto. ✓
