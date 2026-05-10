using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private GameObject inventoryGroupCanvas;
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private GameObject hotbarCanvas;
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private Camera uiCamera;

    private void Awake()
    {
        Instance = this;
        if (uiCamera != null) uiCamera.gameObject.SetActive(false);

        var state = GameStateManager.Instance?.CurrentState ?? GameState.Playing;
        ApplyGameState(state);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ApplyGameState(GameState state)
    {
        bool isPaused = state == GameState.Pause || state == GameState.GameOver;

        hudCanvas.SetActive(!isPaused);
        inventoryGroupCanvas.SetActive(!isPaused);
        inventoryCanvas.SetActive(state == GameState.InventoryCrafting);
        pauseCanvas.SetActive(isPaused);
    }
}