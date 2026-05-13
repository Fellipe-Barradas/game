using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [SerializeField] private bool canCloseAgain = true;

    private Animator anim;
    private bool isOpen;

    public string ActionLabel => isOpen ? "close" : "open";
    public bool CanInteract => !isOpen || canCloseAgain;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void Interact()
    {
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
