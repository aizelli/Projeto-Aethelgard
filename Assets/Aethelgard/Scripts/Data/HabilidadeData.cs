using UnityEngine;

[CreateAssetMenu(fileName = "NovaHabilidade", menuName = "Aethelgard/Data/Habilidade")]
public class HabilidadeData : ScriptableObject
{
    [Header("Identificação")]
    public string nomeHabilidade;
    [TextArea(2, 3)] public string descricao;

    [Header("Regras de Conjuração")]
    public float custoPA = 50f;

    public enum TipoHabilidade { DanoSingular, AtaqueMultiplo, Cura, BuffStatus, Reviver }
    public TipoHabilidade tipo;

    public enum AlvoHabilidade { InimigoMaisProximo, AliadoMenorHP, SiMesmo, TodosAliados }
    public AlvoHabilidade alvo;

    [Header("Inteligência Artificial (IA)")]
    public float tempoDeRecarga = 0f;
    public CondicaoDeUso condicaoUso = CondicaoDeUso.Sempre;

    // PONTO 4: Nova condição para a IA identificar a necessidade de Reviver
    public enum CondicaoDeUso { Sempre, MeuHPAbaixoDe50, AliadoComHPAbaixoDe50, AliadoMorto }

    [Header("Matemática do Efeito")]
    public TipoEscala escalaDoValor;
    public enum TipoEscala { Fixo, PorcentagemDano, PorcentagemHPMaximo }
    public float multiplicadorOuValorBase = 1.5f;

    [Header("Configurações Específicas")]
    public int numeroDeGolpes = 2;
    public StatusBuff statusAlterado;
    public enum StatusBuff { Nenhum, DanoFisico, VelocidadeAtaque, VelocidadeMovimento, Armadura }
    public float duracaoBuff = 5f;
}