using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referências")]
    [SerializeField] private RectTransform textTransform;  // o filho Text (TMP)
    [SerializeField] private TextMeshProUGUI buttonText;   // o componente TMP do filho
    [SerializeField] private GameObject sideAccent;        // a barrinha laranja lateral
    [SerializeField] private Outline buttonOutline;        // o Outline do botão

    [Header("Animação de deslocamento")]
    [SerializeField] private float hoverOffsetX = 10f;     // quanto o texto desloca à direita
    [SerializeField] private float animSpeed = 10f;        // velocidade da interpolação

    [Header("Cores do texto")]
    [SerializeField] private Color normalTextColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    [SerializeField] private Color hoverTextColor  = new Color32(0xFF, 0x6B, 0x1A, 0xFF);

    [Header("Cores da borda (Outline)")]
    [SerializeField] private Color normalBorderColor = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
    [SerializeField] private Color hoverBorderColor  = new Color32(0xC2, 0x41, 0x0C, 0xFF);

    private Vector2 originalTextPos;
    private bool isHovered = false;

    void Start()
    {
        if (textTransform != null)
            originalTextPos = textTransform.anchoredPosition;

        if (sideAccent != null)
            sideAccent.SetActive(false);

        if (buttonOutline != null)
            buttonOutline.effectColor = normalBorderColor;

        if (buttonText != null)
            buttonText.color = normalTextColor;
    }

    void Update()
    {
        // Animação do deslocamento do texto
        if (textTransform != null)
        {
            Vector2 targetPos = originalTextPos;
            if (isHovered)
                targetPos.x = originalTextPos.x + hoverOffsetX;

            textTransform.anchoredPosition = Vector2.Lerp(
                textTransform.anchoredPosition,
                targetPos,
                Time.unscaledDeltaTime * animSpeed
            );
        }

        // Animação suave da cor do texto
        if (buttonText != null)
        {
            Color targetColor = isHovered ? hoverTextColor : normalTextColor;
            buttonText.color = Color.Lerp(buttonText.color, targetColor, Time.unscaledDeltaTime * animSpeed);
        }

        // Animação suave da cor da borda
        if (buttonOutline != null)
        {
            Color targetBorder = isHovered ? hoverBorderColor : normalBorderColor;
            buttonOutline.effectColor = Color.Lerp(buttonOutline.effectColor, targetBorder, Time.unscaledDeltaTime * animSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (sideAccent != null) sideAccent.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (sideAccent != null) sideAccent.SetActive(false);
    }
}