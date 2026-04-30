using UnityEngine;
using UnityEngine.InputSystem; // Necessário para detectar o teclado

public class Bau : MonoBehaviour
{
    public int prataNoBau = 15;
    public int ouroNoBau = 1;
    
    private bool jogadorEstaPerto = false;
    private bool jaFoiAberto = false;

    // Se quiser, crie um pequeno texto na tela que diz "Aperte F" e arraste para cá
    public GameObject avisoAperteF; 

    void Update()
    {
        // Se o jogador estiver na área e apertar a tecla F
        if (jogadorEstaPerto && !jaFoiAberto)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                Abrir();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorEstaPerto = true;
            if (avisoAperteF != null) avisoAperteF.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jogadorEstaPerto = false;
            if (avisoAperteF != null) avisoAperteF.SetActive(false);
        }
    }

    void Abrir()
    {
        jaFoiAberto = true;
        GerenciadorMoedas.Instancia.AdicionarMoedas(prataNoBau, ouroNoBau);
        
        if (avisoAperteF != null) avisoAperteF.SetActive(false);
        
        // Em vez de destruir, você pode tocar uma animação aqui depois
        gameObject.SetActive(false); 
    }
}