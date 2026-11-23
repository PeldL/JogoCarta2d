using UnityEngine;

public class InterrogationManager : MonoBehaviour
{
    public static InterrogationManager Instance { get; private set; }

    public SuspectAsset CurrentSuspect { get; private set; }
    public InterrogationNode CurrentNode { get; private set; }

    public event System.Action<SuspectAsset, InterrogationNode> OnInterrogationUpdated;
    public event System.Action OnInterrogationEnded;

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

    public void StartInterrogation(SuspectAsset suspect)
    {
        if (suspect == null) return;
        CurrentSuspect = suspect;
        GoToNode(suspect.startNodeId);
    }

    public void SelectChoice(InterrogationChoice choice)
    {
        if (choice == null || CurrentSuspect == null || !IsChoiceAvailable(choice)) return;
        if (string.IsNullOrEmpty(choice.nextNodeId))
            EndInterrogation();
        else
            GoToNode(choice.nextNodeId);
    }

    private void GoToNode(string nodeId)
    {
        CurrentNode = CurrentSuspect.GetNode(nodeId);

        if (CurrentNode == null)
        {
            Debug.LogError($"[InterrogationManager] Nó '{nodeId}' não encontrado em '{CurrentSuspect.suspectName}'.");
            EndInterrogation();
            return;
        }

        OnInterrogationUpdated?.Invoke(CurrentSuspect, CurrentNode);
    }

    public void EndInterrogation()
    {
        CurrentSuspect = null;
        CurrentNode = null;
        OnInterrogationEnded?.Invoke();
    }

    /// <summary>Set by CaseZeroGame so question requirements read the saved investigation
    /// (CaseZeroState) instead of a second copy of the collected evidence.</summary>
    public System.Func<string, bool> HasEvidence;

    public bool IsChoiceAvailable(InterrogationChoice choice)
    {
        if (choice == null) return false;
        if (choice.requiredEvidence == null || string.IsNullOrEmpty(choice.requiredEvidence.id))
            return true;
        if (HasEvidence != null) return HasEvidence(choice.requiredEvidence.id);

        return EvidenceManager.Instance != null &&
               EvidenceManager.Instance.HasEvidence(choice.requiredEvidence.id);
    }
}
