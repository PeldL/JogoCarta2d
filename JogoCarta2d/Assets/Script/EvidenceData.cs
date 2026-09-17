using UnityEngine;

[System.Serializable]
public class EvidenceData
{
    public string id;
    public string evidenceName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Ligação associada (opcional)")]
    [Tooltip("Ligação que essa evidência dispara ao ser coletada. Deixe vazio pra não disparar nenhuma.")]
    public PhoneCallAsset linkedCall;
}