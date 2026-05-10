using UnityEngine;

// Callbacks dos botões do pause menu.
// Visibilidade do painel: UIManager.ApplyGameState()
// Tecla Escape e Time.timeScale: GameStateManager
public class PauseMenu : MonoBehaviour
{
    public void Resume()
    {
        GameStateManager.Instance.SetState(GameState.Playing);
    }

    public void OpenOptions()
    {
        Debug.Log("Abrindo opções...");
    }

    public void ReturnToMainMenu()
    {
        GameStateManager.Instance.ReturnToMainMenu();
    }
}