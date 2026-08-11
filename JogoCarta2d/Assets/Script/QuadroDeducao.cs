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

    [Header("UI")]
    public GameObject painelQuadro;
    public Button botaoAbrirQuadro;
    public TextMeshProUGUI textoResultadoFinal; // corrigido: era Text legado
    public GameObject painelVitoria;

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

        FinalizarJogo(VerificarCombinacaoCorreta());
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
            }
        }
        return true;
    }

    void FinalizarJogo(bool venceu)
    {
        if (textoResultadoFinal != null)
        {
            textoResultadoFinal.text = venceu
                ? "Parabéns! Você resolveu o caso!\nO Homem do Balcão confessou o assassinato da mulher para encobrir o roubo."
                : "Dedução incorreta! O caso continua em aberto... Tente novamente!";
        }

        if (painelVitoria != null)
            painelVitoria.SetActive(true);

        Time.timeScale = 0f;
    }
}
