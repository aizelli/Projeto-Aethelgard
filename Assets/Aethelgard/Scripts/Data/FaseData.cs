using UnityEngine;

[CreateAssetMenu(fileName = "NovaFase", menuName = "Aethelgard/Data/Fase")]
public class FaseData : ScriptableObject
{
    [Header("Configuração da Fase")]
    public string nomeFase = "Fase 1-1";
    public int totalOndas = 5;

    [Tooltip("Se marcado, a última onda terá o Boss Final. Se desmarcado, terá o Miniboss.")]
    public bool isFaseDoBossFinal = false;

    [Header("Cenário Específico desta Fase")]
    public GameObject prefabCeu;
    public float larguraCeu = 30f;
    public GameObject prefabFundo;
    public float larguraFundo = 30f;
    public GameObject prefabMeio;
    public float larguraMeio = 30f;
    public GameObject[] poolDeChunks;

    [Header("Pool de Inimigos (Geração Procedural)")]
    [Tooltip("Coloca exatamente 3 monstros diferentes que povoarão as ondas normais")]
    public MonstroData[] inimigosRegulares = new MonstroData[3];

    public MonstroData miniboss;
    public MonstroData bossFinal;

    [Header("Matemática de Escalonamento")]
    public int quantidadeInicialInimigos = 2;

    [Tooltip("Quantos inimigos a mais nascem a cada onda? (Ex: +2 por onda)")]
    public int acrescimoPorOnda = 2;

    // Se preferires crescimento exponencial em vez de soma linear, podes usar um multiplicador:
    // public float multiplicadorExponencial = 1.2f; 
}