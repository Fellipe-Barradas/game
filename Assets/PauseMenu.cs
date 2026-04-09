using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public static bool JogoPausado = false;
    public GameObject pausePainel;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (JogoPausado) Continuar();
            else Pausar();
        }
    }

    // Função do botão RESUME
    public void Continuar()
    {
        pausePainel.SetActive(false); 
        Time.timeScale = 1f;          
        JogoPausado = false;
    }

    void Pausar()
    {
        pausePainel.SetActive(true);  
        Time.timeScale = 0f;          
        JogoPausado = true;
    }

    // Função do botão OPTIONS
    public void AbrirOpcoes()
    {
        // Como o projeto ainda está em construção, vamos imprimir uma mensagem no console
        // Depois você pode criar um painel novo de opções e ligá-lo aqui!
        Debug.Log("Menu de opções clicado!");
    }

    // Função do botão EXIT
    public void SairDoJogo()
    {
        Debug.Log("Saindo do jogo..."); // Esta mensagem aparece no Unity para provar que funcionou
        Application.Quit(); // Este é o comando real que fecha o jogo quando ele estiver compilado
    }
}