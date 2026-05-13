using UnityEngine;
using UnityEngine.InputSystem;

public class LojaPocoes : MonoBehaviour
{
    [Header("Configuração das Poções")]
    public ItemSO pocaoCura;
    public int custoCura = 15;
    public ItemSO pocaoForca;
    public int custoForca = 25;

    [Header("Feedback Visual")]
    public GameObject avisoLoja; 

    private int objetosDentro = 0; // Contador para estabilizar o trigger
    private Inventory inventarioPlayer;
    private Transform cameraPrincipal;

    void Start()
    {
        if (Camera.main != null) 
            cameraPrincipal = Camera.main.transform;
    }

    void Update()
    {
        // 1. Giro do Texto (Billboard) - Só gira se houver alguém dentro
        if (objetosDentro > 0 && avisoLoja != null && cameraPrincipal != null)
        {
            Vector3 direcao = cameraPrincipal.forward;
            direcao.y = 0; 
            avisoLoja.transform.LookAt(avisoLoja.transform.position + direcao);
        }

        // 2. Compra via Teclado
        if (objetosDentro > 0 && inventarioPlayer != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) 
                TentarComprar(pocaoCura, custoCura);
                
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) 
                TentarComprar(pocaoForca, custoForca);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignora triggers internos do player (áreas de detecção)
        if (other.CompareTag("Player") && !other.isTrigger) 
        {
            objetosDentro++;
            inventarioPlayer = other.GetComponent<Inventory>() ?? other.GetComponentInChildren<Inventory>();
            
            if (avisoLoja != null) avisoLoja.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            objetosDentro--;
            
            // Só desliga o aviso se não sobrar nenhum colisor do player dentro da área
            if (objetosDentro <= 0)
            {
                objetosDentro = 0;
                if (avisoLoja != null) avisoLoja.SetActive(false);
            }
        }
    }

    private void TentarComprar(ItemSO item, int custo)
    {
        if (item == null) return;
        
        // Usa o seu sistema de moedas existente
        if (GerenciadorMoedas.Instancia.GastarPrata(custo))
        {
            inventarioPlayer.AddItem(item, 1);
            Debug.Log($"[Loja] Comprou: {item.itemName}");
        }
        else
        {
            Debug.Log("[Loja] Prata insuficiente!");
        }
    }
}