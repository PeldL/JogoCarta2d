using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SistemaPericia : MonoBehaviour
{
    [System.Serializable]
    public class Dica
    {
        public string idDica;
        [TextArea(3, 5)]
        public string textoDica;
        public bool revelada;
        public GameObject iconeDica;
    }

    [Header("Configurações")]
    public List<Dica> dicas = new List<Dica>();
    public int ligacoesDisponiveis = 3;

    [Header("UI Elements")]
    public Button botaoLigarPericia;
    public GameObject painelDica;
    public TextMeshProUGUI textoDicaRevelada;
    public TextMeshProUGUI textoLigacoesRestantes;
    public TextMeshProUGUI textoBotaoPericia;

    [Header("Fade Config")]
    public float fadeDuration = 0.5f;
    public float tempoExibicaoDica = 4f;

    private CanvasGroup painelCanvasGroup;

    void Start()
    {
        painelCanvasGroup = painelDica.GetComponent<CanvasGroup>();
        if (painelCanvasGroup == null)
            painelCanvasGroup = painelDica.AddComponent<CanvasGroup>();

        painelCanvasGroup.alpha = 0f;
        painelDica.SetActive(false);

        botaoLigarPericia.onClick.AddListener(LigarParaPericia);
        AtualizarUILigacoes();
    }

    void LigarParaPericia()
    {
        if (ligacoesDisponiveis <= 0)
        {
            StartCoroutine(MostrarMensagemTemporaria("Sem ligações disponíveis!", textoBotaoPericia, 1.5f));
            return;
        }

        Dica dicaParaRevelar = dicas.Find(d => !d.revelada);

        if (dicaParaRevelar != null)
        {
            dicaParaRevelar.revelada = true;
            ligacoesDisponiveis--;

            StartCoroutine(MostrarDicaComFade(dicaParaRevelar));

            if (dicaParaRevelar.iconeDica != null)
                dicaParaRevelar.iconeDica.SetActive(true);

            AtualizarUILigacoes();
        }
        else
        {
            StartCoroutine(MostrarMensagemTemporaria("Todas as dicas já foram reveladas!", textoBotaoPericia, 1.5f));
        }
    }

    IEnumerator MostrarDicaComFade(Dica dica)
    {
        painelDica.SetActive(true);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            painelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        painelCanvasGroup.alpha = 1f;

        textoDicaRevelada.text = dica.textoDica;

        yield return new WaitForSeconds(tempoExibicaoDica);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            painelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        painelCanvasGroup.alpha = 0f;
        painelDica.SetActive(false);
    }

    IEnumerator MostrarMensagemTemporaria(string mensagem, TextMeshProUGUI tmp, float duracao)
    {
        string textoOriginal = tmp.text;
        Color corOriginal = tmp.color;

        tmp.text = mensagem;
        tmp.color = Color.yellow;

        yield return new WaitForSeconds(duracao);

        tmp.text = textoOriginal;
        tmp.color = corOriginal;
    }

    void AtualizarUILigacoes()
    {
        if (textoLigacoesRestantes != null)
            textoLigacoesRestantes.text = $"📞 Ligações: {ligacoesDisponiveis}";
    }

    public void AdicionarLigacao()
    {
        ligacoesDisponiveis++;
        AtualizarUILigacoes();
        StartCoroutine(MostrarMensagemTemporaria("+1 Ligação disponível!", textoBotaoPericia, 1.5f));
    }
}