using UnityEngine;
using System.Collections.Generic;

public class SistemaDeducao : MonoBehaviour
{
    public GameManager gameManager;
    private List<string> suspeitosIdentificados = new List<string>();

    public void RegistrarIdentificacao(string nomeSuspeito)
    {
        if (!suspeitosIdentificados.Contains(nomeSuspeito))
        {
            suspeitosIdentificados.Add(nomeSuspeito);
            Debug.Log($"Suspeito identificado: {nomeSuspeito}");

            // Avança o progresso ou desbloqueia algo
            if (gameManager != null)
                gameManager.AvancarHistoria();
        }
    }

    public bool SuspeitoIdentificado(string nomeSuspeito)
    {
        return suspeitosIdentificados.Contains(nomeSuspeito);
    }
}