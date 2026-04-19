using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void IrParaJogo()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartGameplay();
            return;
        }
    }
}
