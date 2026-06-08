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
