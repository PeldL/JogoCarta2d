using UnityEngine;
using UnityEngine.UI;

public class PhoneIconUI : MonoBehaviour
{
    [SerializeField] private Button phoneButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private float blinkSpeed = 4f;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color ringingColorA = Color.white;
    [SerializeField] private Color ringingColorB = Color.red;

    private bool isRinging = false;
    private float blinkTimer = 0f;
    private bool isSubscribed = false;

    private void Start()
    {
        TrySubscribe();

        if (phoneButton != null)
            phoneButton.onClick.AddListener(OnPhoneClicked);

        SetIdleVisual();
    }

    private void OnEnable() => TrySubscribe();

    private void TrySubscribe()
    {
        if (isSubscribed || PhoneCallManager.Instance == null)
        {
            if (PhoneCallManager.Instance == null)
                Debug.LogWarning("[PhoneIconUI] PhoneCallManager.Instance é null no momento da subscrição.");
            return;
        }

        PhoneCallManager.Instance.OnCallIncoming += HandleCallIncoming;
        PhoneCallManager.Instance.OnCallEnded += HandleCallEnded;
        isSubscribed = true;
        Debug.Log("[PhoneIconUI] Inscrito nos eventos do PhoneCallManager.");
    }

    private void OnDisable()
    {
        if (isSubscribed && PhoneCallManager.Instance != null)
        {
            PhoneCallManager.Instance.OnCallIncoming -= HandleCallIncoming;
            PhoneCallManager.Instance.OnCallEnded -= HandleCallEnded;
            isSubscribed = false;
        }
    }

    private void Update()
    {
        if (!isRinging || iconImage == null)
            return;

        blinkTimer += Time.deltaTime * blinkSpeed;
        float t = (Mathf.Sin(blinkTimer) + 1f) / 2f;
        iconImage.color = Color.Lerp(ringingColorA, ringingColorB, t);
    }

    private void HandleCallIncoming(PhoneCallAsset call)
    {
        isRinging = true;
        Debug.Log($"[PhoneIconUI] Ligação pendente recebida de {call.callerName}.");
    }

    private void HandleCallEnded()
    {
        if (PhoneCallManager.Instance != null && !PhoneCallManager.Instance.HasPendingCall)
        {
            isRinging = false;
            SetIdleVisual();
        }
    }

    private void SetIdleVisual()
    {
        if (iconImage != null)
            iconImage.color = idleColor;
    }

    public void OnPhoneClicked()
    {
        Debug.Log("[PhoneIconUI] Botão clicado.");

        if (PhoneCallManager.Instance == null)
        {
            Debug.LogError("[PhoneIconUI] PhoneCallManager.Instance é null.");
            return;
        }

        Debug.Log($"[PhoneIconUI] HasPendingCall = {PhoneCallManager.Instance.HasPendingCall}");

        if (PhoneCallManager.Instance.HasPendingCall)
        {
            PhoneCallManager.Instance.AnswerPendingCall();
        }
    }
}