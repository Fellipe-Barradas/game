using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [SerializeField] private bool canCloseAgain = true;

    private Animator anim;
    private bool isOpen;
    private bool locked;

    public string ActionLabel => isOpen ? "close" : "open";
    public bool CanInteract => !isOpen || canCloseAgain;

    /// <summary>Trava/destrava a porta (usado por salas de combate até serem limpas).</summary>
    public void SetLocked(bool value) => locked = value;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void Interact()
    {
        if (locked) return;
        if (!CanInteract) return;

        if (anim == null)
        {
            Debug.LogWarning("DoorController precisa de um Animator no mesmo GameObject.", this);
            return;
        }

        isOpen = !isOpen;
        anim.SetTrigger("change");
    }
}
