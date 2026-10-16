using UnityEngine;

[CreateAssetMenu(fileName = "NovoBioma", menuName = "Aethelgard/Data/Bioma")]
public class BiomeData : ScriptableObject
{
    [Header("Informações do Mundo")]
    public string nomeMundo;

    [Header("Estrutura de Fases")]
    [Tooltip("Arraste os arquivos de Fase (Fase 1, Fase 2...) na ordem correta")]
    public FaseData[] fasesDoBioma;
}