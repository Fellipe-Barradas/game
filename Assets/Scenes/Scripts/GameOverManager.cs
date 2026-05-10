using UnityEngine;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("Referências da UI")]
    public Slider healthSlider;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AtualizarVidaUI(int vidaAtual, int vidaMaxima)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = vidaMaxima;
            healthSlider.value = vidaAtual;
        }
    }

    // Botões do painel de game over
    public void IrParaMenu()
    {
        GameStateManager.Instance?.ReturnToMainMenu();
    }

    public void ReiniciarJogo()
    {
        GameStateManager.Instance?.RestartGameplay();
    }
}