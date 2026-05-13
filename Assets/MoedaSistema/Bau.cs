using UnityEngine;

public class Bau : MonoBehaviour, IInteractable
{
    [Header("Configurações Base")]
    public int prataNoBau = 15;
    public int ouroNoBau = 1;

    private bool jaFoiAberto = false;

    public string ActionLabel => "open";
    public bool CanInteract => !jaFoiAberto;

    public void Interact()
    {
        if (jaFoiAberto) return;
        Abrir();
    }

    private void Abrir()
    {
        jaFoiAberto = true;

        int dropPrata = 0;
        int dropOuro = 0;
        int dropFragmentos = 0;

        if (Random.Range(0f, 100f) <= 80f)
            dropPrata = prataNoBau;

        if (Random.Range(0f, 100f) <= 50f)
            dropOuro = ouroNoBau;

        float sorteioFragmento = Random.Range(0f, 100f);
        if (sorteioFragmento <= 10f)
            dropFragmentos = 15;
        else if (sorteioFragmento <= 30f)
            dropFragmentos = 10;

        if (dropPrata == 0 && dropOuro == 0 && dropFragmentos == 0)
            Debug.Log("o bau esta vazio");
        else
        {
            Debug.Log($"Baú aberto! Drops: {dropPrata} Prata | {dropOuro} Ouro | {dropFragmentos} Fragmentos");
            GerenciadorMoedas.Instancia?.AdicionarDrops(dropPrata, dropOuro, dropFragmentos);
        }

        gameObject.SetActive(false);
    }
}
