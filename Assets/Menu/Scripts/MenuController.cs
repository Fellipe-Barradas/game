using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour {

    public GameObject menuInicial, menuOpcoes;

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
        IrParaMenuInicial();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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

    public void IrParaMenuInicial()
    {
        menuInicial.SetActive(true);
        menuOpcoes.SetActive(false);
    }

    public void IrParaOpcoes()
    {
        menuInicial.SetActive(false);
        menuOpcoes.SetActive(true);
    }

    public void salvarConfiguracoesOpcoes()
    {
        SceneManager.LoadScene("MenuPrincipal");
    }

    public void SairDoJogo()
    {
        Application.Quit();
    }


}
