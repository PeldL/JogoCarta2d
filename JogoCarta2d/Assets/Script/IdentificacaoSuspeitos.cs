using UnityEngine;
using UnityEngine.UI;
using TMPro; // corrigido: era Text/InputField legados
using System.Collections.Generic;

public class IdentificacaoSuspeitos : MonoBehaviour
{
    [System.Serializable]
    public class Suspeito
    {
        public string nome;
        public Sprite foto;
        public string[] pistasIdentificacao;
        public bool identificado;
        public GameObject botaoIdentificar;
    }

    [Header("Suspeitos")]
    public List<Suspeito> suspeitos = new List<Suspeito>();

    [Header("UI — painel de identificação")]
    public GameObject painelIdentificacao;
    public Image fotoUI;
    public TMP_InputField campoNome;      // corrigido: TMP_InputField em vez de InputField legado
    public Button botaoConfirmar;
    public TextMeshProUGUI textoFeedback; // corrigido: era Text legado

    [Header("Referências")]
    public SistemaDeducao sistemaDeducao;

    void Start()
    {
        foreach (var suspeito in suspeitos)
        {
            if (suspeito.botaoIdentificar != null)
            {
                Button btn = suspeito.botaoIdentificar.GetComponent<Button>();
                if (btn != null)
                {
                    Suspeito local = suspeito;
                    btn.onClick.AddListener(() => AbrirIdentificacao(local));
                }
            }
        }

        if (painelIdentificacao != null)
            painelIdentificacao.SetActive(false);
    }

    void AbrirIdentificacao(Suspeito suspeito)
    {
        if (suspeito.identificado)
        {
            MostrarFeedback($"{suspeito.nome} já foi identificado!");
            return;
        }

        painelIdentificacao.SetActive(true);

        if (fotoUI != null && suspeito.foto != null)
            fotoUI.sprite = suspeito.foto;

        if (campoNome != null)
            campoNome.text = "";

        if (botaoConfirmar != null)
        {
            botaoConfirmar.onClick.RemoveAllListeners();
            botaoConfirmar.onClick.AddListener(() => VerificarIdentificacao(suspeito));
        }
    }

    void VerificarIdentificacao(Suspeito suspeito)
    {
        string digitado = campoNome != null ? campoNome.text.Trim() : "";

        if (string.Equals(digitado, suspeito.nome, System.StringComparison.OrdinalIgnoreCase))
        {
            suspeito.identificado = true;
            MostrarFeedback($"Correto! Este é {suspeito.nome}!");

            sistemaDeducao?.RegistrarIdentificacao(suspeito.nome);

            painelIdentificacao.SetActive(false);
        }
        else
        {
            MostrarFeedback("Identificação incorreta! Tente novamente após encontrar mais pistas.");
        }
    }

    void MostrarFeedback(string mensagem)
    {
        if (textoFeedback != null)
            textoFeedback.text = mensagem;
    }

    // Verifica se o jogador coletou pistas suficientes para identificar o suspeito
    public bool PodeIdentificar(Suspeito suspeito, List<string> pistasEncontradas)
    {
        foreach (string pista in suspeito.pistasIdentificacao)
        {
            if (!pistasEncontradas.Contains(pista)) return false;
        }
        return true;
    }
}
