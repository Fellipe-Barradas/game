using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private Camera uiCamera;

    private void Awake()
    {
        Instance = this;
        if (uiCamera != null) uiCamera.gameObject.SetActive(false);
        ApplyGameState(GameState.Playing);
    }

    public void ApplyGameState(GameState state)
    {
        hudCanvas.SetActive(true);
        pauseCanvas.SetActive(state == GameState.Pause || state == GameState.GameOver);
        inventoryCanvas.SetActive(state == GameState.InventoryCrafting);
    }
}