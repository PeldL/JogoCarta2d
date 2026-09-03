using UnityEngine;

public class VaiEVoltaZSuave : MonoBehaviour
{
    [SerializeField] private float anguloInicial = -30f;
    [SerializeField] private float anguloFinal = 30f;
    [SerializeField] private float velocidade = 2f;

    void Update()
    {
        float t = (Mathf.Sin(Time.time * velocidade) + 1f) / 2f; // vai de 0 a 1
        float anguloZ = Mathf.Lerp(anguloInicial, anguloFinal, t);

        Vector3 rotacaoAtual = transform.eulerAngles;
        transform.eulerAngles = new Vector3(rotacaoAtual.x, rotacaoAtual.y, anguloZ);
    }
}