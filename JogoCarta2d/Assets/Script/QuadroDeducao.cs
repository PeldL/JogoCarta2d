using UnityEngine;
using UnityEngine.UI;
using TMPro; // corrigido: era Text legado
using System.Collections.Generic;

public class QuadroDeducao : MonoBehaviour
{
    [System.Serializable]
    public class PistaSlot
    {
        public TipoPista tipo; // enum — elimina magic strings e erros de digitação
        public Transform slotPosition;
        public Image slotImage;
        public PistaItem itemColocado;
        public bool preenchido;
    }

    [System.Serializable]
    public class PistaItem
    {
        public string nome;
        public TipoPista tipo;
        public Sprite icone;
        [TextArea(2, 3)]
        public string descricao;
        public GameObject objetoColetavelAssociado;
    }

    [Header("Slots e itens")]
    public List<PistaSlot> slots = new List<PistaSlot>();
    public List<PistaItem> todasPistas = new List<PistaItem>();

    [Header("Solução correta — configurar no Inspector")]
    public string vitimaCerta;
    public string suspeitoCerto;
    public string armaCerta;
    public string localCerto; // adicionado — GDD exige Culpado + Arma + Local

    [Header("UI")]
    public GameObject painelQuadro;
    public Button botaoAbrirQuadro;
    public TextMeshProUGUI textoResultadoFinal; // corrigido: era Text legado
    public GameObject painelVitoria;

    [Header("Referências")]
    public SistemaPontuacao sistemaPontuacao; // opcional — registra erros e calcula avaliação final

    private PistaItem pistaSelecionada;

    void Start()
    {
        if (botaoAbrirQuadro != null)
            botaoAbrirQuadro.onClick.AddListener(AbrirQuadro);

        if (painelQuadro != null)
            painelQuadro.SetActive(false);

        ResetarCoresSlots();
    }

    void AbrirQuadro()
    {
        painelQuadro.SetActive(!painelQuadro.activeSelf);
    }

    public void SelecionarPista(PistaItem pista)
    {
        pistaSelecionada = pista;
        ResetarCoresSlots();
        DestacarSlotsCompatíveis();
    }

    void DestacarSlotsCompatíveis()
    {
        if (pistaSelecionada == null) return;
        foreach (var slot in slots)
        {
            if (!slot.preenchido && slot.tipo == pistaSelecionada.tipo)
            {
                if (slot.slotImage != null)
                    slot.slotImage.color = Color.yellow;
            }
        }
    }

    public void TentarColocarPista(PistaSlot slot)
    {
        if (pistaSelecionada == null) return;

        if (!slot.preenchido && slot.tipo == pistaSelecionada.tipo)
        {
            slot.itemColocado = pistaSelecionada;
            slot.preenchido   = true;

            if (slot.slotImage != null)
            {
                slot.slotImage.sprite = pistaSelecionada.icone;
                slot.slotImage.color  = Color.white;
            }

            pistaSelecionada = null;
            ResetarCoresSlots();
            VerificarDeducaoCompleta();
        }
        else
        {
            Debug.Log("Slot incompatível ou já preenchido!");
            ResetarCoresSlots();
        }
    }

    void ResetarCoresSlots()
    {
        foreach (var slot in slots)
        {
            if (slot.slotImage != null && !slot.preenchido)
                slot.slotImage.color = Color.gray;
        }
    }

    void VerificarDeducaoCompleta()
    {
        foreach (var slot in slots)
        {
            if (!slot.preenchido) return;
        }

        bool venceu = VerificarCombinacaoCorreta();

        // Errou a combinação — conta como tentativa incorreta para a pontuação final
        // (GDD: "Quanto menos erros cometer... maior será sua pontuação").
        if (!venceu)
            sistemaPontuacao?.RegistrarErro();

        FinalizarJogo(venceu);
    }

    bool VerificarCombinacaoCorreta()
    {
        foreach (var slot in slots)
        {
            switch (slot.tipo)
            {
                case TipoPista.Vitima:
                    if (slot.itemColocado.nome != vitimaCerta)   return false;
                    break;
                case TipoPista.Suspeito:
                    if (slot.itemColocado.nome != suspeitoCerto) return false;
                    break;
                case TipoPista.Arma:
                    if (slot.itemColocado.nome != armaCerta)     return false;
                    break;
                case TipoPista.Local:
                    if (slot.itemColocado.nome != localCerto)    return false;
                    break;
            }
        }
        return true;
    }

    void FinalizarJogo(bool venceu)
    {
        if (!venceu)
        {
            if (textoResultadoFinal != null)
                textoResultadoFinal.text = "Dedução incorreta! O caso continua em aberto... Tente novamente!";

            // Desfaz os slots preenchidos para permitir nova tentativa,
            // já que "Se qualquer informação estiver incorreta, o caso permanece sem solução" (GDD).
            LimparSlotsParaNovaTentativa();
            return;
        }

        string avaliacao = sistemaPontuacao != null
            ? sistemaPontuacao.FinalizarEObterAvaliacao()
            : "";

        if (textoResultadoFinal != null)
        {
            textoResultadoFinal.text =
                "Parabéns! Você resolveu o caso!\n" +
                "O Homem do Balcão confessou o assassinato da mulher para encobrir o roubo." +
                (string.IsNullOrEmpty(avaliacao) ? "" : $"\n\n{avaliacao}");
        }

        if (painelVitoria != null)
            painelVitoria.SetActive(true);

        Time.timeScale = 0f;
    }

    void LimparSlotsParaNovaTentativa()
    {
        foreach (var slot in slots)
        {
            slot.preenchido   = false;
            slot.itemColocado = null;
            if (slot.slotImage != null)
            {
                slot.slotImage.sprite = null;
                slot.slotImage.color  = Color.gray;
            }
        }
    }
}
