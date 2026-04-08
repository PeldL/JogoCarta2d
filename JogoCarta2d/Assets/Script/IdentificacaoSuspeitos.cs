using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class IdentificacaoSuspeitos : MonoBehaviour
{
    [System.Serializable]
    public class Suspeito
    {
        public string nome;
        public Sprite foto;
        public string[] pistasIdentificacao; // Diálogos ou documentos que identificam
        public bool identificado;
        public GameObject botaoIdentificar;
        public InputField campoNome;
    }

    public List<Suspeito> suspeitos = new List<Suspeito>();
    public GameObject painelIdentificacao;
    public Text textoFeedback;
    public SistemaDeducao sistemaDeducao; // Referência ao sistema principal

    void Start()
    {
        foreach (var suspeito in suspeitos)
        {
            if (suspeito.botaoIdentificar != null)
            {
                Button btn = suspeito.botaoIdentificar.GetComponent<Button>();
                if (btn != null)
                {
                    // Cada botão corresponde a um suspeito
                    Suspeito localSuspeito = suspeito;
                    btn.onClick.AddListener(() => AbrirIdentificacao(localSuspeito));
                }
            }
        }
    }

    void AbrirIdentificacao(Suspeito suspeito)
    {
        if (suspeito.identificado)
        {
            textoFeedback.text = $"{suspeito.nome} já foi identificado!";
            return;
        }

        painelIdentificacao.SetActive(true);

        // Configurar UI com a foto do suspeito
        Image fotoUI = painelIdentificacao.GetComponentInChildren<Image>();
        if (fotoUI != null && suspeito.foto != null)
            fotoUI.sprite = suspeito.foto;

        // Configurar input field
        InputField input = painelIdentificacao.GetComponentInChildren<InputField>();
        if (input != null)
        {
            input.text = "";

            // Configurar botão confirmar
            Button confirmar = painelIdentificacao.GetComponentInChildren<Button>();
            if (confirmar != null)
            {
                confirmar.onClick.RemoveAllListeners();
                confirmar.onClick.AddListener(() => VerificarIdentificacao(suspeito, input.text));
            }
        }
    }

    void VerificarIdentificacao(Suspeito suspeito, string nomeDigitado)
    {
        if (nomeDigitado.Trim().ToLower() == suspeito.nome.ToLower())
        {
            suspeito.identificado = true;
            textoFeedback.text = $"Correto! Este é {suspeito.nome}!";

            // Desbloquear progresso
            if (sistemaDeducao != null)
                sistemaDeducao.RegistrarIdentificacao(suspeito.nome);

            painelIdentificacao.SetActive(false);
        }
        else
        {
            textoFeedback.text = "Identificação incorreta! Tente novamente após encontrar mais pistas.";
        }
    }

    // Verifica se o jogador tem pistas suficientes para identificar
    public bool PodeIdentificar(Suspeito suspeito, List<string> pistasEncontradas)
    {
        int pistasNecessarias = suspeito.pistasIdentificacao.Length;
        int pistasEncontradasCount = 0;

        foreach (string pista in pistasEncontradas)
        {
            if (System.Array.Exists(suspeito.pistasIdentificacao, p => p == pista))
                pistasEncontradasCount++;
        }

        return pistasEncontradasCount >= pistasNecessarias;
    }
}