using UnityEngine;
using System.Collections.Generic;

public class SistemaDeducao : MonoBehaviour
{
    public GameManager gameManager;

    private List<string> suspeitosIdentificados = new List<string>();

    public void RegistrarIdentificacao(string nomeSuspeito)
    {
        if (suspeitosIdentificados.Contains(nomeSuspeito)) return;

        suspeitosIdentificados.Add(nomeSuspeito);
        Debug.Log($"Suspeito identificado: {nomeSuspeito}");

        gameManager?.AvancarHistoria();
    }

    public bool SuspeitoIdentificado(string nomeSuspeito) =>
        suspeitosIdentificados.Contains(nomeSuspeito);
}
