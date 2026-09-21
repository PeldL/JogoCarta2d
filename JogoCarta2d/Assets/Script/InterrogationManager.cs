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
        CurrentSuspect = suspect;
        GoToNode(suspect.startNodeId);
    }

    public void SelectChoice(InterrogationChoice choice)
    {
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

    public bool IsChoiceAvailable(InterrogationChoice choice)
    {
        if (choice.requiredEvidence == null)
            return true;

        return EvidenceManager.Instance != null &&
               EvidenceManager.Instance.HasEvidence(choice.requiredEvidence.id);
    }
}