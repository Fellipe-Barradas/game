using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour {

    public GameObject menuInicial, menuOpcoes, classSelectionPanel;

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

    public void IrParaSelecaoDeClasse()
    {
        menuInicial.SetActive(false);
        menuOpcoes.SetActive(false);
        classSelectionPanel.SetActive(true);
    }

    public void IrParaMenuInicial()
    {
        menuInicial.SetActive(true);
        menuOpcoes.SetActive(false);
        classSelectionPanel.SetActive(false);
    }

    public void IrParaOpcoes()
    {
        menuInicial.SetActive(false);
        menuOpcoes.SetActive(true);
        classSelectionPanel.SetActive(false);
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
