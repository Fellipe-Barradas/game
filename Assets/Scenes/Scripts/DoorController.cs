using UnityEngine;

/// <summary>
/// Porta entre salas: abre por presença do player (gatilho de proximidade) e fica aberta.
/// Tranca/fecha sob lockdown de combate (RoomController.SetLocked via contador).
/// </summary>
public class DoorController : MonoBehaviour
{
    private Animator anim;
    private bool isOpen;
    private int lockCount;        // trancada enquanto > 0
    private bool playerInRange;

    private void Awake() => anim = GetComponent<Animator>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (lockCount == 0) Open();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;   // não fecha — fica aberta
    }

    /// <summary>Trancar fecha a porta; destrancar reabre se o player ainda estiver perto.</summary>
    public void SetLocked(bool value)
    {
        if (value)
        {
            lockCount++;
            if (lockCount == 1) Close();
        }
        else
        {
            lockCount = Mathf.Max(0, lockCount - 1);
            if (lockCount == 0 && playerInRange) Open();
        }
    }

    private void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (anim != null) anim.SetTrigger("change");
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (anim != null) anim.SetTrigger("change");
    }
}
