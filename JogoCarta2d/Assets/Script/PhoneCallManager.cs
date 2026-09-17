using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhoneCallManager : MonoBehaviour
{
    public static PhoneCallManager Instance { get; private set; }

    [Header("Configuração de disparo automático")]
    [SerializeField] private float delayAfterEvidence = 30f;

    private readonly Queue<PhoneCallAsset> pendingCalls = new Queue<PhoneCallAsset>();
    private bool isCallActive = false;

    public PhoneCallAsset CurrentCall { get; private set; }
    public PhoneDialogueNode CurrentNode { get; private set; }

    public event System.Action<PhoneCallAsset> OnCallIncoming;
    public event System.Action<PhoneCallAsset, PhoneDialogueNode> OnCallScreenUpdated;
    public event System.Action OnCallEnded;

    public bool HasPendingCall => !isCallActive && pendingCalls.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (EvidenceManager.Instance != null)
            EvidenceManager.Instance.OnEvidenceCollected += HandleEvidenceCollected;
        else
            Debug.LogWarning("[PhoneCallManager] EvidenceManager não encontrado no Start.");
    }

    private void OnDestroy()
    {
        if (EvidenceManager.Instance != null)
            EvidenceManager.Instance.OnEvidenceCollected -= HandleEvidenceCollected;
    }

    private void HandleEvidenceCollected(EvidenceData evidence)
    {
        if (evidence.linkedCall == null)
        {
            Debug.Log($"[PhoneCallManager] '{evidence.evidenceName}' não tem ligação associada.");
            return;
        }

        StartCoroutine(ScheduleCall(evidence.linkedCall, delayAfterEvidence));
    }

    private IEnumerator ScheduleCall(PhoneCallAsset call, float delay)
    {
        yield return new WaitForSeconds(delay);
        QueueCall(call);
    }

    public void QueueCall(PhoneCallAsset call)
    {
        pendingCalls.Enqueue(call);
        if (!isCallActive)
            OnCallIncoming?.Invoke(call);
    }

    public void AnswerPendingCall()
    {
        if (isCallActive || pendingCalls.Count == 0)
            return;

        CurrentCall = pendingCalls.Dequeue();
        isCallActive = true;
        GoToNode(CurrentCall.startNodeId);
    }

    public void SelectChoice(PhoneDialogueChoice choice)
    {
        if (string.IsNullOrEmpty(choice.nextNodeId))
        {
            if (choice.postponesCall)
                RequeueCall();
            else
                EndCall();
        }
        else
        {
            GoToNode(choice.nextNodeId);
        }
    }

    private void GoToNode(string nodeId)
    {
        CurrentNode = CurrentCall.GetNode(nodeId);

        if (CurrentNode == null)
        {
            Debug.LogError($"[PhoneCallManager] Nó '{nodeId}' não encontrado em '{CurrentCall.name}'.");
            EndCall();
            return;
        }

        OnCallScreenUpdated?.Invoke(CurrentCall, CurrentNode);
    }

    private void RequeueCall()
    {
        isCallActive = false;

        PhoneCallAsset callToRequeue = CurrentCall;
        CurrentCall = null;
        CurrentNode = null;

        OnCallEnded?.Invoke();
        pendingCalls.Enqueue(callToRequeue);
        OnCallIncoming?.Invoke(callToRequeue);
    }

    public void EndCall()
    {
        isCallActive = false;
        CurrentCall = null;
        CurrentNode = null;
        OnCallEnded?.Invoke();

        if (pendingCalls.Count > 0)
            OnCallIncoming?.Invoke(pendingCalls.Peek());
    }
}