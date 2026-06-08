using UnityEngine;

public class Bau : MonoBehaviour, IInteractable
{
    [Tooltip("Dados de loot. Injetado pelo RoomPopulator no spawn; pode servir de padrão no prefab.")]
    public ChestSO data;

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

        if (data == null)
        {
            Debug.LogWarning("[Bau] sem ChestSO 'data' — baú vazio.", this);
            gameObject.SetActive(false);
            return;
        }

        int dropPrata = RollCoin(data.prata);
        int dropOuro = RollCoin(data.ouro);
        int dropFragmentos = RollCoin(data.fragmentos);

        if (dropPrata > 0 || dropOuro > 0 || dropFragmentos > 0)
        {
            Debug.Log($"Baú aberto! Drops: {dropPrata} Prata | {dropOuro} Ouro | {dropFragmentos} Fragmentos");
            GerenciadorMoedas.Instancia?.AdicionarDrops(dropPrata, dropOuro, dropFragmentos);
        }

        if (data.itens != null && data.itens.Count > 0)
        {
            Inventory inv = FindPlayerInventory();
            foreach (ItemDrop d in data.itens)
            {
                if (d.item == null || d.amount <= 0) continue;
                if (Random.Range(0f, 100f) <= d.chance)
                {
                    if (inv != null) inv.AddItem(d.item, d.amount);
                    else Debug.LogWarning("[Bau] item dropado mas player sem Inventory.", this);
                }
            }
        }

        gameObject.SetActive(false);
    }

    private static int RollCoin(CoinDrop c)
    {
        if (c.amount <= 0) return 0;
        return Random.Range(0f, 100f) <= c.chance ? c.amount : 0;
    }

    private static Inventory FindPlayerInventory()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.GetComponent<Inventory>() : null;
    }
}
