using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float minLoadingTime = 1f; // pra não piscar se carregar muito rápido

    private void Start()
    {
        StartCoroutine(LoadAsync());
    }

    private IEnumerator LoadAsync()
    {
        float startTime = Time.unscaledTime;
        bool loadUI = SceneLoader.NextScene == "MainScene";

        AsyncOperation op = SceneManager.LoadSceneAsync(SceneLoader.NextScene);
        op.allowSceneActivation = false;

        AsyncOperation uiOp = null;
        if (loadUI)
        {
            uiOp = SceneManager.LoadSceneAsync("UIScene", LoadSceneMode.Additive);
            uiOp.allowSceneActivation = false;
        }

        while (!op.isDone)
        {
            // op.progress vai de 0 a 0.9, depois pula pra 1 ao ativar
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (progressBar != null) progressBar.value = progress;
            if (progressText != null) progressText.text = $"{(int)(progress * 100)}%";

            // quando terminou de carregar e o tempo mínimo passou, ativa as cenas
            if (op.progress >= 0.9f && Time.unscaledTime - startTime >= minLoadingTime)
            {
                if (uiOp != null) uiOp.allowSceneActivation = true;
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}