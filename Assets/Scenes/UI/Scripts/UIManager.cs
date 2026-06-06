using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject hudCanvas;
    [SerializeField] private GameObject inventoryGroupCanvas;
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private GameObject hotbarCanvas;
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private GameObject winCanvas;
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
        bool hideHud = state == GameState.Pause    ||
                       state == GameState.GameOver ||
                       state == GameState.Victory;

        hudCanvas.SetActive(!hideHud);
        inventoryGroupCanvas.SetActive(!hideHud);
        inventoryCanvas.SetActive(state == GameState.InventoryCrafting);
        pauseCanvas.SetActive(state == GameState.Pause);
        gameOverCanvas.SetActive(state == GameState.GameOver);
        if (winCanvas != null) winCanvas.SetActive(state == GameState.Victory);
    }
}