using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PhoneDialogueChoice
{
    public string choiceText;
    public string nextNodeId; // vazio = encerra esse ramo

    [Tooltip("Se marcado, a ligação volta pra fila em vez de ser descartada (ex: 'depois eu vejo').")]
    public bool postponesCall = false;
}

[System.Serializable]
public class PhoneDialogueNode
{
    public string nodeId;
    [TextArea(2, 5)] public string message;
    public List<PhoneDialogueChoice> choices = new List<PhoneDialogueChoice>();
}

[CreateAssetMenu(fileName = "NovaLigacao", menuName = "Investigação/Ligação Telefônica")]
public class PhoneCallAsset : ScriptableObject
{
    public string callerName;
    public string startNodeId = "start";
    public List<PhoneDialogueNode> nodes = new List<PhoneDialogueNode>();

    public PhoneDialogueNode GetNode(string nodeId)
    {
        return nodes.Find(n => n.nodeId == nodeId);
    }
}