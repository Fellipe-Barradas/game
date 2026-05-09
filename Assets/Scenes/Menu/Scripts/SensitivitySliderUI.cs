using UnityEngine;
using UnityEngine.UI;

public class SensitivitySliderUI : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        float saved = PlayerPrefs.GetFloat(ThirdPersonCamera.SENSITIVITY_KEY, 0.08f);
        slider.value = saved;
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        PlayerPrefs.SetFloat(ThirdPersonCamera.SENSITIVITY_KEY, value);
        PlayerPrefs.Save();

        var cam = FindObjectOfType<ThirdPersonCamera>();
        if (cam != null) cam.SetSensitivity(value);
    }
}
