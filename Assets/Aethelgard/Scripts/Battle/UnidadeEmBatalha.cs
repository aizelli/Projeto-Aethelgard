using UnityEngine;
using System;
using UnityEngine.UI;

public class UnidadeEmBatalha : MonoBehaviour
{
    [HideInInspector]
    public Vector3 posicaoBase;

    [Header("Identidade")]
    public MonstroData dadosOriginais;
    public TokenAnimator animador;
    public int nivelAtual = 1;

    [Header("Interface (UI)")]
    public Image barraDeVida;

    [Header("Atributos de Combate (Calculados)")]
    public float hpMaximo;
    public float hpAtual;
    public float danoAtual;
    public float paAtual = 0f;

    [Header("Tática e Movimento")]
    public float velocidade;
    public float alcance;
    public float tempoEntreAtaques;

    private float cronometroAtaque = 0f;
    public event Action<UnidadeEmBatalha> AoMorrer;

    public void Configurar(MonstroData dados, TokenAnimator anim, int nivel = 1)
    {
        dadosOriginais = dados;
        animador = anim;
        nivelAtual = nivel;

        hpMaximo = dados.hpBase * Mathf.Pow(dados.multiplicadorHpPorNivel, nivelAtual - 1);
        danoAtual = dados.danoBase * Mathf.Pow(dados.multiplicadorDanoPorNivel, nivelAtual - 1);
        hpAtual = hpMaximo;

        velocidade = dados.velocidadeMovimento;
        alcance = dados.alcanceAtaque;
        tempoEntreAtaques = dados.tempoEntreAtaques;
        paAtual = 0f;

        AtualizarBarra();
    }

    void Update()
    {
        cronometroAtaque += Time.deltaTime;
    }

    // Renomeado para refletir que a unidade pode atacar ou curar
    public bool PodeAgir()
    {
        return cronometroAtaque >= tempoEntreAtaques;
    }

    // O CÉREBRO DA IA: Varre a lista, filtra o que tem PA suficiente e devolve a de maior prioridade
    public HabilidadeData ObterHabilidadePronta()
    {
        HabilidadeData habilidadeEscolhida = null;
        int maiorPrioridadeEncontrada = -1;

        foreach (SlotHabilidade slot in dadosOriginais.habilidadesAtivas)
        {
            if (slot.habilidade == null) continue;

            // Verifica se tem "Mana" e se a prioridade é maior que a última encontrada
            if (paAtual >= slot.habilidade.custoPA && slot.prioridade > maiorPrioridadeEncontrada)
            {
                maiorPrioridadeEncontrada = slot.prioridade;
                habilidadeEscolhida = slot.habilidade;
            }
        }

        return habilidadeEscolhida; // Retorna nulo se não tiver PA para nenhuma
    }

    public void ReceberDano(float valorDano)
    {
        if (hpAtual <= 0) return;

        hpAtual -= valorDano;
        if (animador != null) animador.PiscarDano();
        AtualizarBarra();

        if (hpAtual <= 0)
        {
            hpAtual = 0;
            Morrer();
        }
    }

    // NOVA FUNÇÃO: Permite que magias de aliados restaurem a vida
    public void ReceberCura(float valorCura)
    {
        if (hpAtual <= 0) return; // Unidades mortas não podem ser curadas

        hpAtual += valorCura;
        if (hpAtual > hpMaximo) hpAtual = hpMaximo; // Trava no teto máximo

        AtualizarBarra();

        // Futuramente: Adicionar animador.PiscarCura() para ficar verde
    }

    private void AtualizarBarra()
    {
        if (barraDeVida != null)
        {
            barraDeVida.fillAmount = hpAtual / hpMaximo;
        }
    }

    // Executa o ataque normal e GERA Pontos de Ação
    public void RegistrarAtaqueBasico()
    {
        cronometroAtaque = 0f;
        paAtual += dadosOriginais.paGeradoPorAtaque;

        if (animador != null) animador.AnimarAtaque();

        if (paAtual > dadosOriginais.paMaximo)
        {
            paAtual = dadosOriginais.paMaximo;
        }
    }

    // Executa a magia e CONSOME os Pontos de Ação
    public void RegistrarUsoHabilidade(HabilidadeData habilidade)
    {
        cronometroAtaque = 0f;
        paAtual -= habilidade.custoPA;

        if (animador != null) animador.AnimarAtaque();
    }

    private void Morrer()
    {
        AoMorrer?.Invoke(this);
        Destroy(gameObject);
    }
}