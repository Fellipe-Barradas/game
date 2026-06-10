# Design — Desbloqueio e compra de classes com ouro

**Data:** 2026-06-10
**Status:** Aprovado (design) — aguardando revisão do spec

## Objetivo

Tornar **Espadachim** a classe inicial (sempre disponível) e deixar **Arqueiro** e
**Lanceiro** bloqueados, compráveis com ouro na tela de seleção de classe. A compra é
permanente (persiste entre jogatinas) e usa o mesmo saldo global de ouro do jogo.

## Decisões (requisitos confirmados)

- **Fluxo de compra:** botão "Comprar" no próprio card bloqueado (tudo na tela de seleção).
- **Preços:** configuráveis por classe, no Inspector de cada card (preços diferentes entre classes).
- **Confirmação:** popup "Comprar X por N ouro?" antes de descontar.
- **Persistência:** desbloqueio permanente em `PlayerPrefs`.
- **Ouro:** sai do saldo global `PlayerPrefs["MoedasDeOuro"]` (mesmo do gameplay).
- **Espadachim:** sempre desbloqueado, sem UI de compra.

## Abordagem escolhida

**A — PlayerPrefs + estender o existente.** Segue os padrões já adotados no projeto
(`WeaponTier_<classe>`, `Fragmentos_<classe>`). Responsabilidades separadas: estado de
desbloqueio, acesso ao ouro e UI são unidades independentes e testáveis.

Rejeitadas: **B** (ScriptableObject data-driven — YAGNI para 3 classes) e
**C** (tornar `GerenciadorMoedas` singleton global — refator arriscado num componente do player).

## Componentes

### 1. `ProgressaoClasses` (novo) — estado de desbloqueio

Arquivo: `Assets/Scenes/Scripts/ProgressaoClasses.cs`. Classe estática, sem MonoBehaviour.

```csharp
public static class ProgressaoClasses
{
    private const string PREFIXO = "ClasseDesbloqueada_";

    public static bool EstaDesbloqueada(PlayerClass classe)
    {
        if (classe == PlayerClass.Espadachim) return true;   // inicial, sempre livre
        return PlayerPrefs.GetInt(PREFIXO + classe, 0) == 1;
    }

    public static void Desbloquear(PlayerClass classe)
    {
        PlayerPrefs.SetInt(PREFIXO + classe, 1);
        PlayerPrefs.Save();
    }
}
```

- Responsabilidade única: responder "está desbloqueada?" e "desbloqueia".
- Não conhece ouro nem UI.
- Espadachim é livre por código (não depende de PlayerPrefs) → save limpo já começa correto.

### 2. `GerenciadorMoedas` — acessores estáticos de ouro

O menu não tem instância de `GerenciadorMoedas`, mas o ouro vive em
`PlayerPrefs["MoedasDeOuro"]`. Para não duplicar a chave, expor acessores estáticos:

```csharp
public static int OuroSalvo => PlayerPrefs.GetInt(CHAVE_OURO, 0);

// Usado SÓ no menu (onde não há instância em cena)
public static bool GastarOuroSalvo(int quantidade)
{
    int atual = PlayerPrefs.GetInt(CHAVE_OURO, 0);
    if (quantidade <= 0 || atual < quantidade) return false;
    PlayerPrefs.SetInt(CHAVE_OURO, atual - quantidade);
    PlayerPrefs.Save();
    return true;
}
```

- `CHAVE_OURO` deixa de ser `private const` → fonte única da chave para gameplay e menu.
- O caminho estático mexe direto no disco e é **exclusivo do menu**. No gameplay usa-se a
  instância (que mantém o valor em memória). Menu e gameplay nunca rodam juntos → sem dessincronia.

### 3. `ClassCard` — estado de bloqueio

Novos campos no Inspector:
- `int preco`
- `GameObject lockOverlay` (cadeado / escurecido)
- `TMP_Text precoLabel`
- `Button comprarButton`

Novo evento: `Action<ClassCard> OnComprarClicked`.

Comportamento:
- `Start()` consulta `ProgressaoClasses.EstaDesbloqueada(playerClass)`:
  - **Desbloqueada:** esconde `lockOverlay`, `precoLabel`, `comprarButton`; card funciona
    como hoje (clique seleciona).
  - **Bloqueada:** mostra cadeado + preço + botão Comprar; `OnPointerClick` **só** dispara
    seleção se a classe estiver desbloqueada.
- `MarcarComoDesbloqueada()` — refaz o visual para o estado livre após a compra, sem
  recarregar a cena.

### 4. `ClassSelectionManager` — orquestração da compra

- Assina `OnComprarClicked` de cada card.
- Exibe o saldo de ouro num `TMP_Text` (via `GerenciadorMoedas.OuroSalvo`), atualizado ao
  abrir a tela e após cada compra.
- Clique em Comprar → abre o popup com nome da classe + preço.
- Confirmação:
  - Se `OuroSalvo >= preco` → `GastarOuroSalvo(preco)` + `ProgressaoClasses.Desbloquear()`
    + `card.MarcarComoDesbloqueada()` + atualiza saldo na tela.
  - Senão → mostra "Ouro insuficiente", sem desconto.
- Card bloqueado nunca habilita o botão Confirmar (jogar).

### 5. `PopupConfirmacao` (novo) — confirmação reutilizável

Arquivo: `Assets/Scenes/Menu/Scripts/PopupConfirmacao.cs`. Painel simples.

- `Mostrar(string mensagem, bool podeComprar, Action onConfirmar)` — preenche o texto,
  habilita/desabilita o botão "Sim" conforme `podeComprar`, mostra/esconde o painel.
- Botões Sim/Não. Responsabilidade única: confirmar uma ação; não conhece classes nem ouro.

## Fluxo de dados

```
Comprar (card bloqueado)
  → Manager abre PopupConfirmacao("Comprar Arqueiro por 150 ouro?", podeComprar)
  → Sim → GastarOuroSalvo(preco) → ProgressaoClasses.Desbloquear(classe)
        → card.MarcarComoDesbloqueada() → atualiza saldo na tela
  → card agora selecionável → Confirmar → joga
```

## Casos de borda

- **Ouro insuficiente:** popup abre com "Sim" desabilitado + texto "Ouro insuficiente". Sem desconto.
- **Espadachim:** sempre livre; sem UI de compra.
- **Já desbloqueada (run anterior):** `Start()` abre o card no estado livre.
- **Selecionar card bloqueado:** clique de seleção ignorado; só o botão Comprar age.
- **Reset para teste:** `PlayerPrefs.DeleteKey("ClasseDesbloqueada_Arqueiro")` (e `_Lanceiro`)
  no Editor volta ao estado bloqueado.

## Setup manual no Unity (não automatizável por código)

- No prefab/objeto de cada card: adicionar o overlay de cadeado, o texto de preço e o botão
  Comprar; arrastar para os campos novos do `ClassCard`; definir `preco`.
- Na cena de seleção: criar o painel de `PopupConfirmacao` (texto + botões Sim/Não) e o
  `TMP_Text` de saldo de ouro; ligá-los no `ClassSelectionManager`.

## Testes

- Sem test runner no projeto (validação pelo Editor). Roteiro manual:
  1. Save limpo → só Espadachim selecionável; Arqueiro/Lanceiro com cadeado + preço.
  2. Comprar com ouro suficiente → desconta, desbloqueia, fica selecionável; saldo atualiza.
  3. Comprar sem ouro → bloqueado, sem desconto.
  4. Voltar ao menu e reabrir seleção → classe comprada continua desbloqueada.
  5. `DeleteKey` da classe → volta a bloquear.

## Arquivos afetados

- Novo: `Assets/Scenes/Scripts/ProgressaoClasses.cs`
- Novo: `Assets/Scenes/Menu/Scripts/PopupConfirmacao.cs`
- Editar: `Assets/MoedaSistema/GerenciadorMoedas.cs` (acessores estáticos)
- Editar: `Assets/Scenes/Menu/Scripts/ClassCard.cs`
- Editar: `Assets/Scenes/Menu/Scripts/ClassSelectionManager.cs`
