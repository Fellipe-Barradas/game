using UnityEngine;
using TMPro;

public class FireTextEffect : MonoBehaviour
{
    private Material mat;

    public float speed = 3f;

    void Start()
    {
        var tmp = GetComponent<TextMeshProUGUI>();
        mat = tmp.fontMaterial;
    }

    void Update()
    {
        float t = Mathf.Abs(Mathf.Sin(Time.time * speed));

        mat.SetFloat("_GlowPower", Mathf.Lerp(0.3f, 1f, t));
        // mat.SetFloat("_GlowOuter", Mathf.Lerp(0.2f, 0.5f, t));
    }
}