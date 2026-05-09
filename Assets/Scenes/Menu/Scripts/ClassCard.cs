using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClassCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Identificação")]
    public PlayerClass playerClass;

    [Header("Referências")]
    [SerializeField] private RectTransform cardVisual; // o filho CardVisual que anima
    [SerializeField] private Image background;         // Image do card raiz
    [SerializeField] private Outline border;           // Outline do card raiz
    [SerializeField] private GameObject topAccent;     // linha laranja superior

    [Header("Cores")]
    [SerializeField] private Color normalBg       = new Color32(0x0A, 0x0A, 0x0A, 0xFF);
    [SerializeField] private Color hoverBg        = new Color32(0x14, 0x14, 0x14, 0xFF);
    [SerializeField] private Color selectedBg     = new Color32(0x1A, 0x0D, 0x05, 0xFF);
    [SerializeField] private Color normalBorder   = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
    [SerializeField] private Color hoverBorder    = new Color32(0xC2, 0x41, 0x0C, 0xFF);
    [SerializeField] private Color selectedBorder = new Color32(0xFF, 0x6B, 0x1A, 0xFF);

    [Header("Animação")]
    [SerializeField] private float hoverLiftY = 4f;
    [SerializeField] private float animSpeed  = 8f;

    private bool isHovered = false;
    private bool isSelected = false;
    private float currentOffsetY = 0f;

    public System.Action<ClassCard> OnCardClicked;

    void Start()
    {
        background.color = normalBg;
        if (border != null) border.effectColor = normalBorder;
        if (topAccent != null) topAccent.SetActive(false);
    }

    void Update()
    {
        // Animação de hover (sobe o CardVisual)
        float targetOffset = (isHovered && !isSelected) ? hoverLiftY : 0f;
        currentOffsetY = Mathf.Lerp(currentOffsetY, targetOffset, Time.deltaTime * animSpeed);

        Vector2 pos = cardVisual.anchoredPosition;
        pos.y = currentOffsetY;
        cardVisual.anchoredPosition = pos;

        // Animação de cores
        Color targetBg     = normalBg;
        Color targetBorder = normalBorder;

        if (isSelected)
        {
            targetBg     = selectedBg;
            targetBorder = selectedBorder;
        }
        else if (isHovered)
        {
            targetBg     = hoverBg;
            targetBorder = hoverBorder;
        }

        background.color = Color.Lerp(background.color, targetBg, Time.deltaTime * animSpeed);
        if (border != null)
            border.effectColor = Color.Lerp(border.effectColor, targetBorder, Time.deltaTime * animSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (topAccent != null) topAccent.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (topAccent != null && !isSelected) topAccent.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardClicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (topAccent != null) topAccent.SetActive(selected || isHovered);
    }
}