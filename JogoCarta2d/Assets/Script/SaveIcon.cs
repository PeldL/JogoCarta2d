using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Exibe um ícone de "salvando..." na tela durante o auto-save.
/// 
/// Setup na UI:
///   - Crie um painel com ícone de disquete/nuvem + texto "Salvando..."
///   - Adicione um CanvasGroup nesse painel
///   - Arraste o painel para o campo "painelSaveIcon" no Inspector
/// </summary>
public class SaveIcon : MonoBehaviour
{
    [Header("UI do ícone")]
    public GameObject painelSaveIcon;       // painel com ícone + texto
    public TextMeshProUGUI textoSalvando;   // texto "Salvando..." (opcional)

    [Header("Configurações")]
    public float duracaoExibicao = 2f;      // quanto tempo fica visível
    public float velocidadeFade  = 0.3f;    // velocidade do fade in/out

    CanvasGroup cg;
    Coroutine exibicaoCoroutine;

    void Awake()
    {
        if (painelSaveIcon != null)
        {
            cg = painelSaveIcon.GetComponent<CanvasGroup>()
              ?? painelSaveIcon.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            painelSaveIcon.SetActive(false);
        }
    }

    /// <summary>Chame este método após salvar para mostrar o ícone.</summary>
    public void MostrarIcone()
    {
        if (painelSaveIcon == null) return;

        // Cancela exibição anterior se ainda estiver ativa
        if (exibicaoCoroutine != null)
            StopCoroutine(exibicaoCoroutine);

        exibicaoCoroutine = StartCoroutine(ExibirIcone());
    }

    IEnumerator ExibirIcone()
    {
        if (textoSalvando != null)
            textoSalvando.text = "Salvando...";

        painelSaveIcon.SetActive(true);

        // Fade in
        yield return Fade(0f, 1f);

        yield return new WaitForSecondsRealtime(duracaoExibicao); // usa Realtime pois o jogo pode estar pausado

        // Fade out
        yield return Fade(1f, 0f);

        painelSaveIcon.SetActive(false);
        exibicaoCoroutine = null;
    }

    IEnumerator Fade(float de, float para)
    {
        float t = 0f;
        while (t < velocidadeFade)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(de, para, t / velocidadeFade);
            yield return null;
        }
        cg.alpha = para;
    }
}
