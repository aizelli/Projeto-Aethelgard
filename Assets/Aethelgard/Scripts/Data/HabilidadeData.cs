using UnityEngine;

[CreateAssetMenu(fileName = "NovaHabilidade", menuName = "Aethelgard/Data/Habilidade")]
public class HabilidadeData : ScriptableObject
{
    [Header("Identificação")]
    public string nomeHabilidade;
    [TextArea(2, 3)] public string descricao;

    [Header("Regras de Conjuração")]
    [Tooltip("Quanto de PA (Energia) custa para usar esta magia?")]
    public float custoPA = 50f;

    public enum TipoHabilidade { DanoSingular, AtaqueMultiplo, Cura, BuffStatus, Reviver }
    public TipoHabilidade tipo;

    public enum AlvoHabilidade { InimigoMaisProximo, AliadoMenorHP, SiMesmo, TodosAliados }
    public AlvoHabilidade alvo;

    [Header("Matemática do Efeito")]
    [Tooltip("O valor do efeito será um número fixo ou vai escalar com os atributos do monstro?")]
    public TipoEscala escalaDoValor;    
    public enum TipoEscala { Fixo, PorcentagemDano, PorcentagemHPMaximo }

    [Tooltip("Se for Fixo, 100 = 100 de dano/cura. Se for Porcentagem, 1.5 = 150% do atributo escolhido acima.")]
    public float multiplicadorOuValorBase = 1.5f;

    [Header("Configurações Específicas (Ataque Múltiplo)")]
    [Tooltip("Quantos golpes seguidos a unidade vai dar?")]
    public int numeroDeGolpes = 2;

    [Header("Configurações Específicas (Buffs)")]
    [Tooltip("Qual atributo aliado será melhorado?")]
    public StatusBuff statusAlterado;
    public enum StatusBuff { Nenhum, DanoFisico, VelocidadeAtaque, VelocidadeMovimento, Armadura }    

    [Tooltip("Quantos segundos o bónus ficará ativo?")]
    public float duracaoBuff = 5f;
}