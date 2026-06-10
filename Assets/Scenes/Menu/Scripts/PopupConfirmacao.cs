using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Painel simples de confirmação Sim/Não. Não conhece classes nem ouro.
public class PopupConfirmacao : MonoBehaviour
{
    [SerializeField] private GameObject painel;
    [SerializeField] private TMP_Text mensagemLabel;
    [SerializeField] private Button simButton;
    [SerializeField] private Button naoButton;

    private Action onConfirmar;

    private void Awake()
    {
        if (simButton != null) simButton.onClick.AddListener(Confirmar);
        if (naoButton != null) naoButton.onClick.AddListener(Esconder);
        if (painel != null) painel.SetActive(false);
    }

    // podeConfirmar = false desabilita o botão "Sim" (ex.: ouro insuficiente).
    public void Mostrar(string mensagem, bool podeConfirmar, Action onConfirmar)
    {
        this.onConfirmar = onConfirmar;
        if (mensagemLabel != null) mensagemLabel.text = mensagem;
        if (simButton != null) simButton.interactable = podeConfirmar;
        if (painel != null) painel.SetActive(true);
    }

    private void Confirmar()
    {
        onConfirmar?.Invoke();
        Esconder();
    }

    private void Esconder()
    {
        if (painel != null) painel.SetActive(false);
    }
}
