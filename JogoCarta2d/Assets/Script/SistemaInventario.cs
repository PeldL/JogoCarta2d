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

    private List<ItemData> itensInventario = new List<ItemData>();

    // Classe para guardar dados do item
    [System.Serializable]
    public class ItemData
    {
        public string nome;
        public string descricao;
        public Sprite icone;
    }

    // Adiciona um item ao inventário
    public void AdicionarItem(string nome, string descricao, Sprite icone)
    {
        ItemData novoItem = new ItemData();
        novoItem.nome = nome;
        novoItem.descricao = descricao;
        novoItem.icone = icone;

        itensInventario.Add(novoItem);

        // Mostrar na UI
        MostrarItemNaUI(novoItem);
    }

    void MostrarItemNaUI(ItemData item)
    {
        if (prefabItemUI != null && gridInventario != null)
        {
            GameObject novoItem = Instantiate(prefabItemUI, gridInventario);

            // Configura ícone
            Image img = novoItem.GetComponent<Image>();
            if (img != null && item.icone != null)
                img.sprite = item.icone;

            // Configura clique para mostrar descrição
            Button btn = novoItem.GetComponent<Button>();
            if (btn == null) btn = novoItem.AddComponent<Button>();

            string nome = item.nome;
            string desc = item.descricao;
            btn.onClick.AddListener(() => MostrarDescricao(nome, desc));

            // Efeito fade in
            CanvasGroup cg = novoItem.GetComponent<CanvasGroup>();
            if (cg == null) cg = novoItem.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            StartCoroutine(FadeInItem(cg));
        }
    }

    void MostrarDescricao(string nome, string descricao)
    {
        if (textoDescricaoItem != null)
        {
            StartCoroutine(AnimacaoDescricao(nome, descricao));
        }
    }

    IEnumerator AnimacaoDescricao(string nome, string descricao)
    {
        // Fade out
        float elapsed = 0f;
        Color cor = textoDescricaoItem.color;

        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            cor.a = Mathf.Lerp(1f, 0f, elapsed / 0.2f);
            textoDescricaoItem.color = cor;
            yield return null;
        }

        textoDescricaoItem.text = $"<b>{nome}</b>\n{descricao}";

        // Fade in
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            cor.a = Mathf.Lerp(0f, 1f, elapsed / 0.2f);
            textoDescricaoItem.color = cor;
            yield return null;
        }

        yield return new WaitForSeconds(4f);

        // Volta ao padrão
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            cor.a = Mathf.Lerp(1f, 0f, elapsed / 0.2f);
            textoDescricaoItem.color = cor;
            yield return null;
        }

        textoDescricaoItem.text = "Clique nos itens para ver detalhes...";

        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            cor.a = Mathf.Lerp(0f, 1f, elapsed / 0.2f);
            textoDescricaoItem.color = cor;
            yield return null;
        }
    }

    IEnumerator FadeInItem(CanvasGroup cg)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    // Verifica se tem um item no inventário
    public bool TemItem(string nomeItem)
    {
        return itensInventario.Exists(item => item.nome == nomeItem);
    }

    // Remove um item (para usar no quadro de dedução)
    public void RemoverItem(string nomeItem)
    {
        ItemData item = itensInventario.Find(i => i.nome == nomeItem);
        if (item != null)
        {
            itensInventario.Remove(item);
            AtualizarUIInventario();
        }
    }

    void AtualizarUIInventario()
    {
        // Limpa e recria toda a UI
        foreach (Transform child in gridInventario)
        {
            Destroy(child.gameObject);
        }

        foreach (var item in itensInventario)
        {
            MostrarItemNaUI(item);
        }
    }
}