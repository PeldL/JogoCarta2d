using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuadroDeducao : MonoBehaviour
{
    [System.Serializable]
    public class PistaSlot
    {
        public string tipo; // "Vitima", "Suspeito", "Arma"
        public Transform slotPosition;
        public Image slotImage;
        public PistaItem itemColocado;
        public bool preenchido;
    }

    [System.Serializable]
    public class PistaItem
    {
        public string nome;
        public string tipo; // "Vitima", "Suspeito", "Arma"
        public Sprite icone;
        [TextArea(2, 3)]
        public string descricao;
        public GameObject objetoColetavelAssociado;
    }

    public List<PistaSlot> slots = new List<PistaSlot>();
    public List<PistaItem> todasPistas = new List<PistaItem>();
    public GameObject painelQuadro;
    public Button botaoAbrirQuadro;
    public Text textoResultadoFinal;
    public GameObject painelVitoria;

    private PistaItem pistaSelecionada;

    void Start()
    {
        if (botaoAbrirQuadro != null)
            botaoAbrirQuadro.onClick.AddListener(AbrirQuadro);

        if (painelQuadro != null)
            painelQuadro.SetActive(false);
    }

    void AbrirQuadro()
    {
        painelQuadro.SetActive(!painelQuadro.activeSelf);
    }

    // Chamado quando o jogador clica em uma pista no inventário
    public void SelecionarPista(PistaItem pista)
    {
        pistaSelecionada = pista;
        Debug.Log($"Pista selecionada: {pista.nome}");

        // Destacar slots compatíveis
        DestacarSlotsCompatíveis();
    }

    void DestacarSlotsCompatíveis()
    {
        foreach (var slot in slots)
        {
            if (!slot.preenchido && slot.tipo == pistaSelecionada.tipo)
            {
                if (slot.slotImage != null)
                    slot.slotImage.color = Color.yellow;
            }
        }
    }

    // Chamado quando o jogador clica em um slot
    public void TentarColocarPista(PistaSlot slot)
    {
        if (pistaSelecionada == null) return;

        if (!slot.preenchido && slot.tipo == pistaSelecionada.tipo)
        {
            // Coloca a pista no slot
            slot.itemColocado = pistaSelecionada;
            slot.preenchido = true;

            // Atualiza UI
            if (slot.slotImage != null)
            {
                slot.slotImage.sprite = pistaSelecionada.icone;
                slot.slotImage.color = Color.white;
            }

            Debug.Log($"Pista {pistaSelecionada.nome} colocada no slot {slot.tipo}");

            // Remove pista do inventário
            // (Implementar remoção do inventário aqui)

            pistaSelecionada = null;

            // Verifica se completou o quadro
            VerificarDeducaoCompleta();
        }
        else
        {
            Debug.Log("Slot incompatível ou já preenchido!");
        }

        // Reset cores dos slots
        ResetarCoresSlots();
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
        bool todosPreenchidos = true;

        foreach (var slot in slots)
        {
            if (!slot.preenchido)
            {
                todosPreenchidos = false;
                break;
            }
        }

        if (todosPreenchidos)
        {
            // Verifica se a combinação está correta
            bool deducaoCorreta = VerificarCombinacaoCorreta();

            if (deducaoCorreta)
            {
                FinalizarJogo(true);
            }
            else
            {
                FinalizarJogo(false);
            }
        }
    }

    bool VerificarCombinacaoCorreta()
    {
        // Configuração correta do caso
        // Exemplo: Vitima = "Mulher do Bar", Suspeito = "Homem do Balcão", Arma = "Revólver"

        foreach (var slot in slots)
        {
            if (slot.tipo == "Vitima" && slot.itemColocado.nome != "Mulher do Bar")
                return false;
            if (slot.tipo == "Suspeito" && slot.itemColocado.nome != "Homem do Balcão")
                return false;
            if (slot.tipo == "Arma" && slot.itemColocado.nome != "Revólver")
                return false;
        }

        return true;
    }

    void FinalizarJogo(bool venceu)
    {
        if (venceu)
        {
            textoResultadoFinal.text = "Parabéns! Você resolveu o caso!\nO Homem do Balcão confessou o assassinato da mulher para encobrir o roubo.";
        }
        else
        {
            textoResultadoFinal.text = "Dedução incorreta! O caso continua em aberto... Tente novamente!";
        }

        if (painelVitoria != null)
            painelVitoria.SetActive(true);

        Time.timeScale = 0f; // Pausa o jogo
    }
}