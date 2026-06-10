# Desbloqueio e Compra de Classes — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deixar Espadachim como classe inicial gratuita e tornar Arqueiro e Lanceiro bloqueados, compráveis com ouro (compra permanente) na tela de seleção de classe.

**Architecture:** Estado de desbloqueio em `PlayerPrefs` via helper estático `ProgressaoClasses`; ouro lido/gasto pelo saldo global `PlayerPrefs["MoedasDeOuro"]` através de acessores estáticos no `GerenciadorMoedas`; UI estendida em `ClassCard`/`ClassSelectionManager` com um popup de confirmação reutilizável (`PopupConfirmacao`). Responsabilidades separadas: estado, ouro e UI são unidades independentes.

**Tech Stack:** Unity 6000.3.11f1, C#, TextMeshPro, Unity UI (Button/Image), PlayerPrefs.

**Nota sobre testes:** O assembly de testes (`Game.Dungeon.Core.Tests`) não referencia o `Assembly-CSharp`, então não há como cobrir estes scripts com NUnit. Verificação = compilar sem erros no Console + playtest manual no Editor (padrão do projeto). Cada task traz passos de verificação concretos.

**Spec:** `docs/superpowers/specs/2026-06-10-desbloqueio-classes-design.md`

---

## File Structure

- Create: `Assets/Scenes/Scripts/ProgressaoClasses.cs` — estado de desbloqueio (estático).
- Create: `Assets/Scenes/Menu/Scripts/PopupConfirmacao.cs` — painel de confirmação reutilizável.
- Modify: `Assets/MoedaSistema/GerenciadorMoedas.cs` — acessores estáticos de ouro.
- Modify: `Assets/Scenes/Menu/Scripts/ClassCard.cs` — estado de bloqueio + botão comprar.
- Modify: `Assets/Scenes/Menu/Scripts/ClassSelectionManager.cs` — orquestração da compra + saldo.

---

## Task 1: Helper de estado de desbloqueio (`ProgressaoClasses`)

**Files:**
- Create: `Assets/Scenes/Scripts/ProgressaoClasses.cs`

- [ ] **Step 1: Criar o helper estático**

```csharp
using UnityEngine;

// Estado de desbloqueio das classes, persistido em PlayerPrefs.
// Espelha o padrão de chaves por classe usado em WeaponTierManager / GerenciadorMoedas.
public static class ProgressaoClasses
{
    private const string PREFIXO = "ClasseDesbloqueada_";

    public static bool EstaDesbloqueada(PlayerClass classe)
    {
        if (classe == PlayerClass.Espadachim) return true; // inicial, sempre livre
        return PlayerPrefs.GetInt(PREFIXO + classe, 0) == 1;
    }

    public static void Desbloquear(PlayerClass classe)
    {
        PlayerPrefs.SetInt(PREFIXO + classe, 1);
        PlayerPrefs.Save();
    }
}
```

- [ ] **Step 2: Verificar compilação**

No Unity Editor, aguardar o recompile e checar o Console.
Expected: sem erros de compilação.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scenes/Scripts/ProgressaoClasses.cs
git commit -m "feat: helper de estado de desbloqueio de classes"
```

---

## Task 2: Acessores estáticos de ouro no `GerenciadorMoedas`

**Files:**
- Modify: `Assets/MoedaSistema/GerenciadorMoedas.cs`

Contexto: `CHAVE_OURO` já existe como `private const string CHAVE_OURO = "MoedasDeOuro";`. Métodos estáticos da própria classe enxergam essa constante. Estes acessores são usados **apenas no menu** (onde não há instância em cena); no gameplay continua usando a instância.

- [ ] **Step 1: Adicionar os acessores estáticos**

Logo após a linha `private string ChaveFragmentos => ...;` (bloco de chaves), adicionar:

```csharp
    // Acesso ao ouro salvo sem precisar de instância (usado no menu de seleção de classe).
    public static int OuroSalvo => PlayerPrefs.GetInt(CHAVE_OURO, 0);

    public static bool GastarOuroSalvo(int quantidade)
    {
        int atual = PlayerPrefs.GetInt(CHAVE_OURO, 0);
        if (quantidade <= 0 || atual < quantidade) return false;
        PlayerPrefs.SetInt(CHAVE_OURO, atual - quantidade);
        PlayerPrefs.Save();
        return true;
    }
```

- [ ] **Step 2: Verificar compilação**

No Console do Unity, aguardar recompile.
Expected: sem erros.

- [ ] **Step 3: Commit**

```bash
git add Assets/MoedaSistema/GerenciadorMoedas.cs
git commit -m "feat: acessores estaticos de ouro para uso no menu"
```

---

## Task 3: Estado de bloqueio no `ClassCard`

**Files:**
- Modify: `Assets/Scenes/Menu/Scripts/ClassCard.cs`

- [ ] **Step 1: Adicionar o using do TextMeshPro**

No topo do arquivo, junto aos outros `using`:

```csharp
using TMPro;
```

- [ ] **Step 2: Adicionar campos de bloqueio/compra**

Após o bloco `[Header("Identificação")]` (depois de `public WeaponData classWeapon;`), adicionar:

```csharp
    [Header("Bloqueio / Compra")]
    public int preco = 100;
    [SerializeField] private GameObject lockOverlay;   // cadeado / escurecido
    [SerializeField] private TMP_Text precoLabel;       // texto "N ouro"
    [SerializeField] private Button comprarButton;      // botão Comprar

    public System.Action<ClassCard> OnComprarClicked;
    private bool desbloqueada;
    public bool Desbloqueada => desbloqueada;
```

- [ ] **Step 3: Inicializar o estado de bloqueio no `Start()`**

No fim do método `Start()` existente (depois de `if (topAccent != null) topAccent.SetActive(false);`), adicionar:

```csharp
        desbloqueada = ProgressaoClasses.EstaDesbloqueada(playerClass);
        AtualizarVisualBloqueio();

        if (comprarButton != null)
            comprarButton.onClick.AddListener(() => OnComprarClicked?.Invoke(this));
```

- [ ] **Step 4: Adicionar `AtualizarVisualBloqueio()` e `MarcarComoDesbloqueada()`**

Adicionar como novos métodos na classe (ex.: antes de `SetSelected`):

```csharp
    private void AtualizarVisualBloqueio()
    {
        if (lockOverlay != null) lockOverlay.SetActive(!desbloqueada);
        if (comprarButton != null) comprarButton.gameObject.SetActive(!desbloqueada);
        if (precoLabel != null)
        {
            precoLabel.gameObject.SetActive(!desbloqueada);
            precoLabel.text = preco + " ouro";
        }
    }

    public void MarcarComoDesbloqueada()
    {
        desbloqueada = true;
        AtualizarVisualBloqueio();
    }
```

- [ ] **Step 5: Bloquear seleção de card travado**

Substituir o método `OnPointerClick` existente por:

```csharp
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!desbloqueada) return; // card travado não seleciona; só o botão Comprar age
        OnCardClicked?.Invoke(this);
    }
```

- [ ] **Step 6: Verificar compilação**

Console do Unity após recompile.
Expected: sem erros.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scenes/Menu/Scripts/ClassCard.cs
git commit -m "feat: estado de bloqueio e botao comprar no ClassCard"
```

---

## Task 4: Popup de confirmação reutilizável

**Files:**
- Create: `Assets/Scenes/Menu/Scripts/PopupConfirmacao.cs`

- [ ] **Step 1: Criar o componente**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Painel simples de confirmação Sim/Não. Não conhece classes nem ouro.
public class PopupConfirmacao : MonoBehaviour
{
    [SerializeField] private GameObject painel;
    [SerializeField] private TMP_Text mensagemLabel;
    [SerializeField] private Button simButton;
    [SerializeField] private Button naoButton;

    private Action onConfirmar;

    private void Awake()
    {
        if (simButton != null) simButton.onClick.AddListener(Confirmar);
        if (naoButton != null) naoButton.onClick.AddListener(Esconder);
        if (painel != null) painel.SetActive(false);
    }

    // podeConfirmar = false desabilita o botão "Sim" (ex.: ouro insuficiente).
    public void Mostrar(string mensagem, bool podeConfirmar, Action onConfirmar)
    {
        this.onConfirmar = onConfirmar;
        if (mensagemLabel != null) mensagemLabel.text = mensagem;
        if (simButton != null) simButton.interactable = podeConfirmar;
        if (painel != null) painel.SetActive(true);
    }

    private void Confirmar()
    {
        onConfirmar?.Invoke();
        Esconder();
    }

    private void Esconder()
    {
        if (painel != null) painel.SetActive(false);
    }
}
```

- [ ] **Step 2: Verificar compilação**

Console do Unity após recompile.
Expected: sem erros.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scenes/Menu/Scripts/PopupConfirmacao.cs
git commit -m "feat: popup de confirmacao reutilizavel"
```

---

## Task 5: Orquestração da compra no `ClassSelectionManager`

**Files:**
- Modify: `Assets/Scenes/Menu/Scripts/ClassSelectionManager.cs`

- [ ] **Step 1: Adicionar o using do TextMeshPro**

No topo do arquivo:

```csharp
using TMPro;
```

- [ ] **Step 2: Adicionar campos de compra/saldo**

Após o bloco `[Header("Referências")]` (depois de `private MenuController menuController;`... na verdade após o campo `menuController`), adicionar antes de `private ClassCard selectedCard;`:

```csharp
    [Header("Compra de Classes")]
    [SerializeField] private PopupConfirmacao popupConfirmacao;
    [SerializeField] private TMP_Text ouroLabel;
```

- [ ] **Step 3: Assinar o evento de compra e mostrar o saldo no `Start()`**

Substituir o loop existente no `Start()`:

```csharp
        foreach (var card in cards)
            card.OnCardClicked += HandleCardClicked;
```

por:

```csharp
        foreach (var card in cards)
        {
            card.OnCardClicked += HandleCardClicked;
            card.OnComprarClicked += HandleComprarClicked;
        }

        AtualizarOuroLabel();
```

- [ ] **Step 4: Adicionar os métodos de compra**

Adicionar como novos métodos na classe (ex.: antes de `OnReturnClicked`):

```csharp
    private void HandleComprarClicked(ClassCard card)
    {
        bool podeComprar = GerenciadorMoedas.OuroSalvo >= card.preco;
        string msg = podeComprar
            ? $"Comprar {card.playerClass} por {card.preco} ouro?"
            : $"Ouro insuficiente ({GerenciadorMoedas.OuroSalvo}/{card.preco})";

        popupConfirmacao.Mostrar(msg, podeComprar, () => ComprarClasse(card));
    }

    private void ComprarClasse(ClassCard card)
    {
        if (!GerenciadorMoedas.GastarOuroSalvo(card.preco)) return;

        ProgressaoClasses.Desbloquear(card.playerClass);
        card.MarcarComoDesbloqueada();
        AtualizarOuroLabel();
    }

    private void AtualizarOuroLabel()
    {
        if (ouroLabel != null)
            ouroLabel.text = GerenciadorMoedas.OuroSalvo + " ouro";
    }
```

- [ ] **Step 5: Verificar compilação**

Console do Unity após recompile.
Expected: sem erros.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scenes/Menu/Scripts/ClassSelectionManager.cs
git commit -m "feat: compra de classes na tela de selecao"
```

---

## Task 6: Wiring no Unity Editor + playtest manual

**Files:** nenhum script — alterações de cena/prefab (`.unity` / `.prefab`).

Esta task é manual no Editor (não automatizável por código). Executar com o Unity aberto.

- [ ] **Step 1: Configurar os cards bloqueados**

Para os cards de **Arqueiro** e **Lanceiro** (objeto que tem o `ClassCard`):
1. Adicionar um filho de overlay de cadeado (Image escurecida + ícone) → arrastar em `Lock Overlay`.
2. Adicionar um `TMP_Text` de preço → arrastar em `Preco Label`.
3. Adicionar um `Button` "Comprar" → arrastar em `Comprar Button`.
4. Definir `Preco` (ex.: Arqueiro 150, Lanceiro 100).

O card de **Espadachim** não precisa desses campos (pode deixá-los vazios).

- [ ] **Step 2: Configurar o popup de confirmação**

1. Na cena de seleção, criar um painel (GameObject) com: `TMP_Text` de mensagem, botão "Sim", botão "Não".
2. Adicionar o componente `PopupConfirmacao` ao painel raiz.
3. Arrastar: o painel em `Painel`, o texto em `Mensagem Label`, e os botões em `Sim Button` / `Nao Button`.

- [ ] **Step 3: Configurar o `ClassSelectionManager`**

No objeto que tem o `ClassSelectionManager`:
1. Arrastar o `PopupConfirmacao` em `Popup Confirmacao`.
2. Criar/arrastar um `TMP_Text` de saldo em `Ouro Label`.

- [ ] **Step 4: Garantir ouro para teste**

Jogar uma partida e coletar ouro (já persiste após a correção anterior), **ou** no Editor
definir um saldo temporário: criar um script de teste rápido ou usar o menu
`Edit > Clear All PlayerPrefs` para resetar, e via um `MonoBehaviour` temporário chamar
`PlayerPrefs.SetInt("MoedasDeOuro", 999); PlayerPrefs.Save();`.

- [ ] **Step 5: Playtest — roteiro completo**

Entrar em Play na cena de Menu e validar:
1. **Save limpo:** Espadachim selecionável; Arqueiro/Lanceiro com cadeado + preço + botão Comprar; clicar neles (fora do botão) não seleciona.
2. **Saldo:** o `Ouro Label` mostra o ouro atual.
3. **Comprar com ouro suficiente:** clicar Comprar → popup "Comprar X por N ouro?" com "Sim" habilitado → Sim → ouro desconta, cadeado some, card vira selecionável, saldo atualiza.
4. **Comprar sem ouro:** popup mostra "Ouro insuficiente (X/N)" com "Sim" desabilitado; nada é descontado.
5. **Selecionar e jogar** a classe recém-comprada normalmente.
6. **Persistência:** voltar ao menu e reabrir a seleção → a classe comprada continua desbloqueada.

Expected: todos os passos conforme descrito; sem erros no Console.

- [ ] **Step 6: Commit das alterações de cena/prefab**

```bash
git add Assets/Scenes
git commit -m "feat: wiring da UI de desbloqueio de classes na cena de selecao"
```

---

## Self-Review (preenchido pelo autor do plano)

- **Cobertura do spec:** estado de desbloqueio (T1), ouro estático (T2), ClassCard bloqueio/compra (T3), popup (T4), orquestração + saldo (T5), wiring + casos de borda no playtest (T6). ✓
- **Casos de borda:** ouro insuficiente (T5/T6 passo 4), Espadachim livre (T1 + T3), já desbloqueada (T3 Start), selecionar card travado (T3 OnPointerClick), reset para teste (T6 passo 4). ✓
- **Consistência de tipos/nomes:** `OnComprarClicked`, `MarcarComoDesbloqueada()`, `Desbloqueada`, `OuroSalvo`, `GastarOuroSalvo()`, `ProgressaoClasses.EstaDesbloqueada/Desbloquear`, `PopupConfirmacao.Mostrar(string,bool,Action)` — usados de forma idêntica entre tasks. ✓
- **Placeholders:** nenhum; todo código está completo. ✓
