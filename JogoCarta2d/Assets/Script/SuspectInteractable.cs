using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SuspectInteractable : MonoBehaviour, IClickable2D
{
    [Header("Dados do suspeito")]
    public SuspectAsset suspect;

    [Header("Feedback visual (opcional)")]
    public GameObject highlightEffect;

    public void OnHoverEnter()
    {
        if (highlightEffect != null)
            highlightEffect.SetActive(true);
    }

    public void OnHoverExit()
    {
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }

    public void OnClicked()
    {
        if (suspect == null)
        {
            Debug.LogWarning($"[SuspectInteractable] '{gameObject.name}' não tem SuspectAsset atribuído.");
            return;
        }

        if (InterrogationManager.Instance == null)
        {
            Debug.LogError("[SuspectInteractable] Nenhum InterrogationManager encontrado na cena.");
            return;
        }

        InterrogationManager.Instance.StartInterrogation(suspect);
    }
}