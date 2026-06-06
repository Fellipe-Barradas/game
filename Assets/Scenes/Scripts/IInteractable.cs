public interface IInteractable
{
    string ActionLabel { get; }
    bool CanInteract { get; }
    void Interact();
}
