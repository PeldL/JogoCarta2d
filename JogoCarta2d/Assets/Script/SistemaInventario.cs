using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SistemaInventario : MonoBehaviour
{
    [Header("Configurações")]
    public Transform gridInventario;
    public GameObject prefabItemUI;
    public TextMeshProUGUI textoDescricaoItem;

    [Header("Efeitos")]
    public float fadeDuration = 0.3f;

    [System.Serializable]
    public class ItemData
    {
        public string nome;
        public string descricao;
        public Sprite icone;
    }

    private List<ItemData> itensInventario = new List<ItemData>();

    // Dicionário para remoção eficiente — evita recriar toda a UI ao remover um item
    private Dictionary<string, GameObject> itemUIMap = new Dictionary<string, GameObject>();

    public void AdicionarItem(string nome, string descricao, Sprite icone)
    {
        var novoItem = new ItemData { nome = nome, descricao = descricao, icone = icone };
        itensInventario.Add(novoItem);
        MostrarItemNaUI(novoItem);
    }

    void MostrarItemNaUI(ItemData item)
    {
        if (prefabItemUI == null || gridInventario == null) return;

        GameObject go = Instantiate(prefabItemUI, gridInventario);

        Image img = go.GetComponent<Image>();
        if (img != null && item.icone != null)
            img.sprite = item.icone;

        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        string nome = item.nome;
        string desc = item.descricao;
        btn.onClick.AddListener(() => MostrarDescricao(nome, desc));

        // Registra no mapa para remoção rápida (usa nome como chave)
        itemUIMap[item.nome] = go;

        CanvasGroup cg = FadeUtils.GetOrAddCanvasGroup(go);
        cg.alpha = 0f;
        StartCoroutine(FadeUtils.FadeCanvasGroup(this, cg, 0f, 1f, fadeDuration));
    }

    void MostrarDescricao(string nome, string descricao)
    {
        if (textoDescricaoItem != null)
            StartCoroutine(AnimacaoDescricao(nome, descricao));
    }

    IEnumerator AnimacaoDescricao(string nome, string descricao)
    {
        yield return StartCoroutine(FadeUtils.FadeTMP(this, textoDescricaoItem, 1f, 0f, 0.2f));

        textoDescricaoItem.text = $"<b>{nome}</b>\n{descricao}";

        yield return StartCoroutine(FadeUtils.FadeTMP(this, textoDescricaoItem, 0f, 1f, 0.2f));
        yield return new WaitForSeconds(4f);

        yield return StartCoroutine(FadeUtils.FadeTMP(this, textoDescricaoItem, 1f, 0f, 0.2f));
        textoDescricaoItem.text = "Clique nos itens para ver detalhes...";
        yield return StartCoroutine(FadeUtils.FadeTMP(this, textoDescricaoItem, 0f, 1f, 0.2f));
    }

    public bool TemItem(string nomeItem) =>
        itensInventario.Exists(item => item.nome == nomeItem);

    public void RemoverItem(string nomeItem)
    {
        ItemData item = itensInventario.Find(i => i.nome == nomeItem);
        if (item == null) return;

        itensInventario.Remove(item);

        // Remove apenas o GameObject deste item — sem recriar toda a UI
        if (itemUIMap.TryGetValue(nomeItem, out GameObject go))
        {
            StartCoroutine(RemoverComFade(go, nomeItem));
        }
    }

    IEnumerator RemoverComFade(GameObject go, string nomeItem)
    {
        CanvasGroup cg = FadeUtils.GetOrAddCanvasGroup(go);
        yield return StartCoroutine(FadeUtils.FadeCanvasGroup(this, cg, 1f, 0f, fadeDuration));
        itemUIMap.Remove(nomeItem);
        Destroy(go);
    }
}
