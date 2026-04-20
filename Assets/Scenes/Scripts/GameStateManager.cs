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

    [Header("Hotkeys")]
    [SerializeField] private Key pauseKey = Key.Escape;
    [SerializeField] private Key inventoryKey = Key.I;
    [SerializeField] private Key craftingKey = Key.C;

    public GameState CurrentState { get; private set; } = GameState.InitialScreen;

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
            (keyboard[inventoryKey].wasPressedThisFrame || keyboard[craftingKey].wasPressedThisFrame))
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
        SceneManager.LoadScene(gameplaySceneName);
        ApplyState(CurrentState);
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
        }
        else if (scene.name == gameplaySceneName && CurrentState == GameState.InitialScreen)
        {
            CurrentState = GameState.Playing;
        }

        ApplyState(CurrentState);
    }

    private void ApplyState(GameState state)
    {
        Time.timeScale = IsWorldPaused ? 0f : 1f;

        bool lockCursor = state == GameState.Playing;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    private bool IsGameplaySceneActive()
    {
        return SceneManager.GetActiveScene().name == gameplaySceneName;
    }

    private void OnGUI()
    {
        if (!IsGameplaySceneActive())
        {
            return;
        }

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18
        };

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16
        };

        Rect panel = new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 110f, 340f, 220f);

        if (CurrentState == GameState.Pause)
        {
            GUI.Box(panel, "Pause\n\nESC para voltar", boxStyle);

            if (GUI.Button(new Rect(panel.x + 35f, panel.y + 115f, 270f, 35f), "Continuar", buttonStyle))
            {
                SetState(GameState.Playing);
            }

            if (GUI.Button(new Rect(panel.x + 35f, panel.y + 160f, 270f, 35f), "Voltar ao Menu", buttonStyle))
            {
                ReturnToMainMenu();
            }
        }
        else if (CurrentState == GameState.GameOver)
        {
            GUI.Box(panel, "Game Over", boxStyle);

            if (GUI.Button(new Rect(panel.x + 35f, panel.y + 115f, 270f, 35f), "Tentar Novamente", buttonStyle))
            {
                RestartGameplay();
            }

            if (GUI.Button(new Rect(panel.x + 35f, panel.y + 160f, 270f, 35f), "Voltar ao Menu", buttonStyle))
            {
                ReturnToMainMenu();
            }
        }
        else if (CurrentState == GameState.InventoryCrafting)
        {
            GUI.Box(
                new Rect(Screen.width * 0.5f - 210f, 20f, 420f, 110f),
                "Inventario e Crafting\nI/C ou ESC para fechar\nO mundo continua ativo, mas o jogador fica parado.",
                boxStyle);
        }
    }
}
