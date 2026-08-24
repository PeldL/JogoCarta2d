using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Fotografar evidências — GDD, lista de ações do jogador durante a investigação:
/// "Fotografar evidências." Não existia nenhuma implementação no projeto.
///
/// Uso: coloque este componente num objeto persistente da cena de investigação.
/// Quando o jogador interage com um item fotografável (SistemaItens já expõe
/// PistaItem/ItemInterativo), chame RegistrarFoto(id, sprite) a partir do
/// próprio evento de clique do item (ex.: um botão "Fotografar" no popup de exame).
/// As fotos tiradas ficam disponíveis num álbum simples que pode ser
/// consultado como referência às pistas coletadas.
/// </summary>
public class SistemaFotografia : MonoBehaviour
{
    [System.Serializable]
    public class Foto
    {
        public string idEvidencia;
        public string descricao;
        public Sprite imagem;
    }

    [Header("Referências")]
    public GameManager gameManager;

    [Header("UI — álbum de fotos")]
    public GameObject painelAlbum;
    public Transform containerMiniaturas;
    public GameObject prefabMiniatura; // precisa de um componente Image filho

    [Header("Feedback de captura")]
    public GameObject painelFlash; // opcional: pisca a tela ao fotografar
    public AudioSource somObturador;

    private List<Foto> fotosTiradas = new List<Foto>();

    /// <summary>Chame isso a partir da interação com o item/evidência interativa.</summary>
    public void RegistrarFoto(string idEvidencia, string descricao, Sprite imagem)
    {
        if (fotosTiradas.Exists(f => f.idEvidencia == idEvidencia))
        {
            Debug.Log($"Evidência '{idEvidencia}' já foi fotografada.");
            return;
        }

        var foto = new Foto { idEvidencia = idEvidencia, descricao = descricao, imagem = imagem };
        fotosTiradas.Add(foto);

        if (somObturador != null) somObturador.Play();
        if (painelFlash != null) StartCoroutine(PiscarFlash());

        AdicionarMiniatura(foto);

        Debug.Log($"Foto registrada: {descricao}");
    }

    System.Collections.IEnumerator PiscarFlash()
    {
        painelFlash.SetActive(true);
        yield return new WaitForSeconds(0.15f);
        painelFlash.SetActive(false);
    }

    void AdicionarMiniatura(Foto foto)
    {
        if (containerMiniaturas == null || prefabMiniatura == null) return;

        GameObject miniatura = Instantiate(prefabMiniatura, containerMiniaturas);
        Image img = miniatura.GetComponentInChildren<Image>();
        if (img != null && foto.imagem != null)
            img.sprite = foto.imagem;
    }

    public void AbrirAlbum()  => painelAlbum?.SetActive(true);
    public void FecharAlbum() => painelAlbum?.SetActive(false);

    public bool FoiFotografada(string idEvidencia) =>
        fotosTiradas.Exists(f => f.idEvidencia == idEvidencia);

    // ── Save/load ─────────────────────────────────────────────────────────────

    public List<string> ObterIdsFotografadas()
    {
        var lista = new List<string>();
        foreach (var f in fotosTiradas) lista.Add(f.idEvidencia);
        return lista;
    }

    /// <summary>
    /// Restaura quais evidências já foram fotografadas a partir do save.
    /// Como sprites não são salvos no JSON, as miniaturas não são recriadas aqui —
    /// apenas o estado "já fotografado" é restaurado para lógica de jogo/checagens.
    /// </summary>
    public void RestaurarIds(List<string> idsFotografadas)
    {
        if (idsFotografadas == null) return;
        foreach (var id in idsFotografadas)
        {
            if (!fotosTiradas.Exists(f => f.idEvidencia == id))
                fotosTiradas.Add(new Foto { idEvidencia = id, descricao = "(restaurado do save)" });
        }
    }
}
