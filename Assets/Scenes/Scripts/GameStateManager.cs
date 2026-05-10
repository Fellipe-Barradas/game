using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum GameState
{
    InitialScreen,
    Playing,
    Pause,
    GameOver,
    InventoryCrafting
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string menuSceneName = "MenuInicial";
    [SerializeField] private string gameplaySceneName = "MainScene";
    [SerializeField] private string uiSceneName = "UIScene";

    [Header("Hotkeys")]
    [SerializeField] private Key pauseKey = Key.Escape;
    [SerializeField] private Key inventoryKey = Key.I;
    [SerializeField] private Key inventoryKeyAlt = Key.Tab;
    [SerializeField] private Key craftingKey = Key.C;

    public GameState CurrentState { get; private set; } = GameState.InitialScreen;
    
    [Header("Classe do Personagem")]
    [Tooltip("Usado só para testes no Editor. Em jogo, a classe vem do GameStateManager.")]
    public PlayerClass SelectedClass { get; set; } = PlayerClass.Lanceiro;
    public bool IsWorldPaused =>
        CurrentState == GameState.Pause ||
        CurrentState == GameState.GameOver;

    public bool CanPlayerMove => CurrentState == GameState.Playing;
    public bool CanPlayerAct => CurrentState == GameState.Playing;
    public bool CanCameraLook => CurrentState == GameState.Playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject(nameof(GameStateManager));
        managerObject.AddComponent<GameStateManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyState(CurrentState);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (!IsGameplaySceneActive())
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (CurrentState != GameState.GameOver && keyboard[pauseKey].wasPressedThisFrame)
        {
            if (CurrentState == GameState.Playing)
            {
                SetState(GameState.Pause);
                return;
            }

            if (CurrentState == GameState.Pause || CurrentState == GameState.InventoryCrafting)
            {
                SetState(GameState.Playing);
                return;
            }
        }

        if ((CurrentState == GameState.Playing || CurrentState == GameState.InventoryCrafting) &&
            (keyboard[inventoryKey].wasPressedThisFrame || keyboard[inventoryKeyAlt].wasPressedThisFrame || keyboard[craftingKey].wasPressedThisFrame))
        {
            SetState(CurrentState == GameState.Playing ? GameState.InventoryCrafting : GameState.Playing);
        }
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        ApplyState(newState);
    }

    public void StartGameplay()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;
        ApplyState(CurrentState);
        
        SceneLoader.NextScene = gameplaySceneName;
        SceneManager.LoadScene("LoadingScene");
    }

    public void RestartGameplay()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;
        SceneManager.LoadScene(gameplaySceneName);
        ApplyState(CurrentState);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.InitialScreen;
        SceneManager.LoadScene(menuSceneName);
        ApplyState(CurrentState);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name == menuSceneName)
        {
            CurrentState = GameState.InitialScreen;
            ApplyState(CurrentState);
        }
        else if (scene.name == gameplaySceneName)
        {
            if (CurrentState == GameState.InitialScreen)
                CurrentState = GameState.Playing;

            EnsureUISceneLoaded();
        }
        else if (scene.name == uiSceneName)
        {
            ApplyState(CurrentState);
        }
    }

    private void EnsureUISceneLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == uiSceneName)
            {
                ApplyState(CurrentState);
                return;
            }
        }

        SceneManager.LoadSceneAsync(uiSceneName, LoadSceneMode.Additive);
        // ApplyState será chamado quando UIScene terminar de carregar (OnSceneLoaded)
    }

    private void ApplyState(GameState state)
    {
        Time.timeScale = IsWorldPaused ? 0f : 1f;

        bool lockCursor = state == GameState.Playing;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;

        UIManager.Instance?.ApplyGameState(state);
    }

    private bool IsGameplaySceneActive()
    {
        return SceneManager.GetActiveScene().name == gameplaySceneName;
    }

}
