using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance { get; private set; }

    [Header("Evidências coletadas nesta sessão")]
    [SerializeField] private List<EvidenceData> collectedEvidence = new List<EvidenceData>();

    // Evento simples pra UI/painel investigativo escutar futuramente
    public delegate void EvidenceCollectedHandler(EvidenceData evidence);
    public event EvidenceCollectedHandler OnEvidenceCollected;

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

    public bool HasEvidence(string evidenceId)
    {
        return collectedEvidence.Exists(e => e.id == evidenceId);
    }

    public void CollectEvidence(EvidenceData evidence)
    {
        if (HasEvidence(evidence.id))
        {
            Debug.Log($"[EvidenceManager] Evidência '{evidence.evidenceName}' já foi coletada.");
            return;
        }

        collectedEvidence.Add(evidence);
        Debug.Log($"[EvidenceManager] Evidência coletada: {evidence.evidenceName}");
        OnEvidenceCollected?.Invoke(evidence);
    }

    public List<EvidenceData> GetCollectedEvidence()
    {
        return collectedEvidence;
    }
}