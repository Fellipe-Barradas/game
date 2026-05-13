using UnityEngine;
using TMPro;

public class DoorController : MonoBehaviour
{
    private const float DefaultInteractionDistance = 2f;
    private const string DefaultLockedText = "Porta fechada";
    private const string DefaultOpenedText = "Porta aberta";

    [Min(0.1f)]
    public float distanceToDetect = 2f;
    public GameObject objTextDoor;
    public string textLockedDoor = "Porta fechada";
    public string textOpenedDoor = "Porta aberta";

    private TextMeshProUGUI textDoor;
    private Transform player;
    private Animator anim;
    private bool isOpen;

    private void OnDisable()
    {
        if (objTextDoor != null)
        {
            objTextDoor.SetActive(false);
        }
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        TryFindPlayer();

        if (objTextDoor != null)
        {
            textDoor = objTextDoor.GetComponent<TextMeshProUGUI>();
            objTextDoor.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null)
        {
            TryFindPlayer();
        }

        bool shouldBeOpen = CheckProximity();
        UpdateInteractionText(shouldBeOpen);

        if (shouldBeOpen)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        if (isOpen)
        {
            return;
        }

        if (anim == null)
        {
            Debug.LogWarning("DoorController precisa de um Animator no mesmo GameObject.", this);
            return;
        }

        anim.SetTrigger("change");
        isOpen = true;
    }

    private void CloseDoor()
    {
        if (!isOpen || anim == null)
        {
            return;
        }

        anim.SetTrigger("change");
        isOpen = false;
    }

    private void UpdateInteractionText(bool canInteract)
    {
        if (objTextDoor == null)
        {
            return;
        }

        objTextDoor.SetActive(canInteract);

        if (canInteract && textDoor != null)
        {
            textDoor.text = GetDoorText();
        }
    }

    private bool CheckProximity()
    {
        return player != null && DistanceToPlayer() <= GetInteractionDistance();
    }

    private float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    private float GetInteractionDistance()
    {
        return distanceToDetect > 0f ? distanceToDetect : DefaultInteractionDistance;
    }

    private string GetDoorText()
    {
        if (isOpen)
        {
            return string.IsNullOrWhiteSpace(textOpenedDoor) ? DefaultOpenedText : textOpenedDoor;
        }

        return string.IsNullOrWhiteSpace(textLockedDoor) ? DefaultLockedText : textLockedDoor;
    }

    private void TryFindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }
}
