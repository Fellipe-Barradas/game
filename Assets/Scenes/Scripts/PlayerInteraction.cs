using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionMask = ~0;

    private IInteractable currentInteractable;
    private Collider[] selfColliders;

    private void Start()
    {
        selfColliders = GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.CanPlayerAct) return;

        DetectInteractable();

        if (currentInteractable != null && Keyboard.current.eKey.wasPressedThisFrame)
            currentInteractable.Interact();
    }

    private void DetectInteractable()
    {
        IInteractable found = null;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, interactionRange + 2f, interactionMask);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null || Array.IndexOf(selfColliders, hit.collider) >= 0)
                    continue;

                if (Vector3.Distance(transform.position, hit.point) > interactionRange)
                    break;

                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.CanInteract)
                {
                    found = interactable;
                    break;
                }
            }
        }

        if (found != currentInteractable)
        {
            currentInteractable = found;

            if (currentInteractable != null)
                InteractionPromptUI.Instance?.Show(currentInteractable.ActionLabel);
            else
                InteractionPromptUI.Instance?.Hide();
        }
    }

    private void OnDisable()
    {
        currentInteractable = null;
        InteractionPromptUI.Instance?.Hide();
    }
}
