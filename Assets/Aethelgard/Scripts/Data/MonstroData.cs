using System.Collections.Generic;
using UnityEngine;

// 1. CRIAMOS A ESTRUTURA DO SLOT
[System.Serializable]
public struct SlotHabilidade
{
    [Tooltip("Arraste a magia criada aqui")]
    public HabilidadeData habilidade;

    [Tooltip("A IA escolhe sempre a magia com o MAIOR número de prioridade que tiver PA suficiente")]
    public int prioridade;
}

[CreateAssetMenu(fileName = "NovoMonstro", menuName = "Aethelgard/Data/Monstro")]
public class MonstroData : ScriptableObject
{
    [Header("Identificação e Sistema")]
    public string idLocal;
    public string nomeCarta;

    public enum ElementoTipo { Fogo, Agua, Terra, Eletrico, Neutro }
    public ElementoTipo elemento;

    public int tier = 1;
    public MonstroData evoluiPara;

    [Header("Referências Visuais")]
    public Sprite arteInventario;
    public Sprite spriteToken;

    [Header("Atributos Base (Nível 1)")]
    public float hpBase = 100f;
    public float danoBase = 10f;
    public float velocidadeMovimento = 2f;
    public float alcanceAtaque = 1.5f;
    public float tempoEntreAtaques = 1.5f;

    [Header("Progressão e Tática")]
    public float multiplicadorHpPorNivel = 1.2f;
    public float multiplicadorDanoPorNivel = 1.15f;

    public enum PapelCombate { Tanque, CorpoACorpo, Atirador, Suporte }
    public PapelCombate papelTatico;

    [Header("Sistema de Ação e Magias")]
    [Tooltip("Capacidade máxima do tanque de energia do monstro")]
    public float paMaximo = 100f;
    [Tooltip("Quanto de PA (Energia) a unidade acumula a cada ataque básico desferido")]
    public float paGeradoPorAtaque = 20f;

    [Tooltip("Lista de magias que este monstro sabe usar nesta evolução.")]
    public List<SlotHabilidade> habilidadesAtivas = new List<SlotHabilidade>();
}