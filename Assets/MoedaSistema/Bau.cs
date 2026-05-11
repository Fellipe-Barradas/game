using UnityEngine;
using UnityEngine.InputSystem;

public class Bau : MonoBehaviour
{
    [Header("Configurações Base")]
    public int prataNoBau = 15;
    public int ouroNoBau = 1;
    
    private bool jogadorEstaPerto = false;
    private bool jaFoiAberto = false;

    public GameObject avisoAperteF; 

    void Update()
    {
        // Se o jogador estiver na área, o baú estiver fechado e apertar a tecla F
        if (jogadorEstaPerto && !jaFoiAberto && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Abrir();
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
        if (avisoAperteF != null) avisoAperteF.SetActive(false);
        
        int dropPrata = 0;
        int dropOuro = 0;
        int dropFragmentos = 0;

        // 1. Probabilidade da Prata (80% de chance)
        if (Random.Range(0f, 100f) <= 80f)
        {
            dropPrata = prataNoBau; // Ganha 15
        }

        // 2. Probabilidade do Ouro (50% de chance)
        if (Random.Range(0f, 100f) <= 50f)
        {
            dropOuro = ouroNoBau; // Ganha 1
        }

        // 3. Probabilidade dos Fragmentos (10% para 15, 20% para 10)
        float sorteioFragmento = Random.Range(0f, 100f);
        if (sorteioFragmento <= 10f)
        {
            dropFragmentos = 15; // Caiu nos 10%
        }
        else if (sorteioFragmento <= 30f)
        {
            dropFragmentos = 10; // Caiu nos 20% seguintes (de 10.1 a 30)
        }

        // 4. Verificação de "Azar Supremo"
        if (dropPrata == 0 && dropOuro == 0 && dropFragmentos == 0)
        {
            Debug.Log("o bau esta vazio");
        }
        else
        {
            Debug.Log($"Baú aberto! Drops: {dropPrata} Prata | {dropOuro} Ouro | {dropFragmentos} Fragmentos");
            // Só chama o gerenciador se ele realmente ganhou algo
            GerenciadorMoedas.Instancia?.AdicionarDrops(dropPrata, dropOuro, dropFragmentos);
        }
        
        // Em vez de destruir, apenas desativamos o objeto
        gameObject.SetActive(false); 
    }
}