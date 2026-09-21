using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InterrogationChoice
{
    public string choiceText;
    public string nextNodeId; // vazio = encerra o interrogatório

    [Tooltip("Se preenchido, essa pergunta só fica disponível depois que essa evidência for coletada.")]
    public EvidenceData requiredEvidence;
}

[System.Serializable]
public class InterrogationNode
{
    public string nodeId;
    [TextArea(2, 5)] public string message;
    public List<InterrogationChoice> choices = new List<InterrogationChoice>();
}

[CreateAssetMenu(fileName = "NovoSuspeito", menuName = "Investigação/Suspeito")]
public class SuspectAsset : ScriptableObject
{
    public string suspectName;
    public Sprite portrait;
    [TextArea] public string description;
    public string startNodeId = "start";
    public List<InterrogationNode> nodes = new List<InterrogationNode>();

    public InterrogationNode GetNode(string nodeId)
    {
        return nodes.Find(n => n.nodeId == nodeId);
    }
}