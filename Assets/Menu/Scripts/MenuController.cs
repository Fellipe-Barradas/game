using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour {

    public GameObject menuInicial, menuOpcoes;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
        VoltarParaMenu();
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

    public void IrParaOpcoes()
    {
        menuInicial.SetActive(false);
        menuOpcoes.SetActive(true);
    }

    public void VoltarParaMenu()
    {
        menuInicial.SetActive(true);
        menuOpcoes.SetActive(false);
    }
}
