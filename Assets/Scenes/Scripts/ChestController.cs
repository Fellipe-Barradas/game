using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ChestController : MonoBehaviour
{
    private const float DefaultInteractionDistance = 2f;
    private const string DefaultClosedText = "Pressione R para abrir";
    private const string DefaultOpenedText = "Bau aberto";
    private const string OpenTriggerName = "open";

    [Min(0.1f)]
    public float distanceToDetect = 2f;
    public GameObject objTextChest;
    public string textClosedChest = "Pressione R para abrir";
    public string textOpenedChest = "Bau aberto";

    private TextMeshProUGUI textChest;
    private Transform player;
    private Animator anim;
    private bool isOpen;

    private void OnDisable()
    {
        if (objTextChest != null)
        {
            objTextChest.SetActive(false);
        }
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        TryFindPlayer();

        if (objTextChest != null)
        {
            textChest = objTextChest.GetComponent<TextMeshProUGUI>();
            objTextChest.SetActive(false);
        }
    }

    private void Update()
    {
        if (player == null)
        {
            TryFindPlayer();
        }

        bool playerIsNear = CheckProximity();
        UpdateInteractionText(playerIsNear);

        if (!isOpen && playerIsNear && WasOpenKeyPressed())
        {
            OpenChest();
        }
    }

    private void OpenChest()
    {
        if (anim == null)
        {
            Debug.LogWarning("ChestController precisa de um Animator no mesmo GameObject.", this);
            return;
        }

        anim.SetTrigger(OpenTriggerName);
        isOpen = true;
        UpdateInteractionText(CheckProximity());
    }

    private void UpdateInteractionText(bool playerIsNear)
    {
        if (objTextChest == null)
        {
            return;
        }

        objTextChest.SetActive(playerIsNear);

        if (playerIsNear && textChest != null)
        {
            textChest.text = GetChestText();
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

    private string GetChestText()
    {
        if (isOpen)
        {
            return string.IsNullOrWhiteSpace(textOpenedChest) ? DefaultOpenedText : textOpenedChest;
        }

        return string.IsNullOrWhiteSpace(textClosedChest) ? DefaultClosedText : textClosedChest;
    }

    private bool WasOpenKeyPressed()
    {
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
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
