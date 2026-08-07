using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuadroDeducao : MonoBehaviour
{
    [System.Serializable]
    public class PistaSlot
    {
        public TipoPista tipo;
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
    public string localCerto; // NOVO

    [Header("UI")]
    public GameObject painelQuadro;
    public Button botaoAbrirQuadro;
    public TextMeshProUGUI textoResultadoFinal;
    public GameObject painelVitoria;
    public TextMeshProUGUI textoDicaSolucao; // NOVO: dica para o jogador

    [Header("Referências")]
    public GameManager gameManager;

    private PistaItem pistaSelecionada;
    private bool casoResolvido = false;

    void Start()
    {
        if (botaoAbrirQuadro != null)
            botaoAbrirQuadro.onClick.AddListener(AbrirQuadro);

        if (painelQuadro != null)
            painelQuadro.SetActive(false);

        ResetarCoresSlots();

        // Carregar dados do caso atual
        CarregarCasoAtual();
    }

    void CarregarCasoAtual()
    {
        if (gameManager == null) return;

        var caso = gameManager.GetCasoAtual();
        if (caso != null)
        {
            vitimaCerta = caso.vitimaCerta;
            suspeitoCerto = caso.suspeitoCerto;
            armaCerta = caso.armaCerta;
            localCerto = caso.localCerto;

            // Atualizar dica
            if (textoDicaSolucao != null)
            {
                textoDicaSolucao.text = $"Encontre: Vítima, Suspeito, Arma e Local do crime.";
            }
        }
    }

    void AbrirQuadro()
    {
        if (casoResolvido) return;
        painelQuadro.SetActive(!painelQuadro.activeSelf);
    }

    public void SelecionarPista(PistaItem pista)
    {
        if (casoResolvido) return;
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
        if (pistaSelecionada == null || casoResolvido) return;

        if (!slot.preenchido && slot.tipo == pistaSelecionada.tipo)
        {
            slot.itemColocado = pistaSelecionada;
            slot.preenchido = true;

            if (slot.slotImage != null)
            {
                slot.slotImage.sprite = pistaSelecionada.icone;
                slot.slotImage.color = Color.white;
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
        FinalizarJogo(venceu);
    }

    bool VerificarCombinacaoCorreta()
    {
        foreach (var slot in slots)
        {
            switch (slot.tipo)
            {
                case TipoPista.Vitima:
                    if (slot.itemColocado.nome != vitimaCerta) return false;
                    break;
                case TipoPista.Suspeito:
                    if (slot.itemColocado.nome != suspeitoCerto) return false;
                    break;
                case TipoPista.Arma:
                    if (slot.itemColocado.nome != armaCerta) return false;
                    break;
                case TipoPista.Local: // NOVO
                    if (slot.itemColocado.nome != localCerto) return false;
                    break;
            }
        }
        return true;
    }

    void FinalizarJogo(bool venceu)
    {
        casoResolvido = true;

        if (textoResultadoFinal != null)
        {
            if (venceu)
            {
                textoResultadoFinal.text = "🎉 PARABÉNS! Você resolveu o caso!";
                // Avançar para próximo caso
                gameManager?.AvancarHistoria();
            }
            else
            {
                textoResultadoFinal.text = "❌ Dedução incorreta! Reveja as pistas e tente novamente.";
                // Penalidade
                gameManager?.PenalizarErro();
            }
        }

        if (painelVitoria != null)
            painelVitoria.SetActive(true);
    }

    // Método para restaurar estado do save
    public void RestaurarSlot(TipoPista tipo, string nomeItem)
    {
        if (string.IsNullOrEmpty(nomeItem)) return;

        var pista = todasPistas.Find(p => p.nome == nomeItem);
        if (pista == null) return;

        var slot = slots.Find(s => s.tipo == tipo);
        if (slot == null || slot.preenchido) return;

        slot.itemColocado = pista;
        slot.preenchido = true;

        if (slot.slotImage != null)
        {
            slot.slotImage.sprite = pista.icone;
            slot.slotImage.color = Color.white;
        }
    }
}