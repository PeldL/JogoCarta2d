using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NovoCaso", menuName = "Detetive/CasoData")]
public class CasoData : ScriptableObject
{
    [Header("Identificação")]
    public string nomeCaso;
    public int numeroCaso;
    [TextArea(3, 5)]
    public string descricao;
    public int nivelDificuldade = 1;

    [Header("Solução do Caso")]
    public string vitimaCerta;
    public string suspeitoCerto;
    public string armaCerta;
    public string localCerto;

    [Header("Pistas do Caso")]
    public List<PistaItemData> pistasDisponiveis;
    public List<SuspeitoData> suspeitos;

    [Header("Cenários")]
    public List<string> cenasLiberadas;

    [Header("Configurações")]
    public bool temPistasFalsas = false;
    public int ligacoesIniciais = 3;
    public float tempoLimite = 0; // 0 = sem limite

    [Header("Diálogos")]
    public TextAsset dialogoInicial;
    public TextAsset dialogoFinal;
}

[System.Serializable]
public class PistaItemData
{
    public string id;
    public string nome;
    public TipoPista tipo;
    public Sprite icone;
    [TextArea(2, 3)]
    public string descricao;
    public bool ehPistaFalsa;
    public string localizacao;
    public GameObject prefabMundo;
}

[System.Serializable]
public class SuspeitoData
{
    public string nome;
    public Sprite retrato;
    public List<string> dialogos;
    public string alibi;
    public bool alibiVerdadeiro;
    public string motivos;
    public List<string> pistasParaIdentificar;
}