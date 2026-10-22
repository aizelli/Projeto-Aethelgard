using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class EfeitoTemporario
{
    public HabilidadeData.StatusBuff atributo; // PONTO 3: Agora usa o Enum correto
    public float valorModificador;
    public float tempoRestante;
}

public class UnidadeEmBatalha : MonoBehaviour
{
    [HideInInspector] public Vector3 posicaoBase;

    [Header("Identidade")]
    public MonstroData dadosOriginais;
    public TokenAnimator animador;
    public int nivelAtual = 1;

    [Header("Interface (UI)")]
    public Image barraDeVida;

    [Header("Atributos de Combate")]
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

    private List<EfeitoTemporario> efeitosAtivos = new List<EfeitoTemporario>();
    private Dictionary<HabilidadeData, float> recargas = new Dictionary<HabilidadeData, float>();

    public void Configurar(MonstroData dados, TokenAnimator anim, int nivel = 1)
    {
        dadosOriginais = dados;
        animador = anim;
        nivelAtual = nivel;

        // PONTO 6: Limpeza completa de estados ao configurar
        paAtual = 0f;
        cronometroAtaque = 0f;
        recargas.Clear();
        efeitosAtivos.Clear();

        hpMaximo = dados.hpBase * Mathf.Pow(dados.multiplicadorHpPorNivel, nivelAtual - 1);
        hpAtual = hpMaximo;

        RecalcularAtributos();
        AtualizarBarra();
    }

    void Update()
    {
        cronometroAtaque += Time.deltaTime;
        ProcessarEfeitosTemporarios();
        ProcessarRecargas();
    }

    private void ProcessarRecargas()
    {
        if (recargas.Count == 0) return;
        List<HabilidadeData> chaves = new List<HabilidadeData>(recargas.Keys);
        foreach (var magia in chaves)
        {
            if (recargas[magia] > 0) recargas[magia] -= Time.deltaTime;
        }
    }

    private void ProcessarEfeitosTemporarios()
    {
        if (efeitosAtivos.Count == 0) return;
        bool precisaRecalcular = false;
        for (int i = efeitosAtivos.Count - 1; i >= 0; i--)
        {
            efeitosAtivos[i].tempoRestante -= Time.deltaTime;
            if (efeitosAtivos[i].tempoRestante <= 0)
            {
                efeitosAtivos.RemoveAt(i);
                precisaRecalcular = true;
            }
        }
        if (precisaRecalcular) RecalcularAtributos();
    }

    // PONTO 3: Assinatura do Buff atualizada para usar o Enum
    public void AplicarBuff(HabilidadeData.StatusBuff atributo, float valor, float duracaoEmSegundos)
    {
        efeitosAtivos.Add(new EfeitoTemporario { atributo = atributo, valorModificador = valor, tempoRestante = duracaoEmSegundos });
        RecalcularAtributos();
    }

    private void RecalcularAtributos()
    {
        danoAtual = dadosOriginais.danoBase * Mathf.Pow(dadosOriginais.multiplicadorDanoPorNivel, nivelAtual - 1);
        velocidade = dadosOriginais.velocidadeMovimento;
        tempoEntreAtaques = dadosOriginais.tempoEntreAtaques;
        alcance = dadosOriginais.alcanceAtaque;

        // PONTO 3: Integração dos atributos do BuffStatus
        foreach (var efeito in efeitosAtivos)
        {
            if (efeito.atributo == HabilidadeData.StatusBuff.DanoFisico) danoAtual += efeito.valorModificador;
            if (efeito.atributo == HabilidadeData.StatusBuff.VelocidadeAtaque) tempoEntreAtaques -= efeito.valorModificador;
            if (efeito.atributo == HabilidadeData.StatusBuff.VelocidadeMovimento) velocidade += efeito.valorModificador;
            // Futuramente: Adicionar lógica para a Armadura aqui
        }
        if (tempoEntreAtaques < 0.1f) tempoEntreAtaques = 0.1f;
    }

    public bool PodeAgir()
    {
        return cronometroAtaque >= tempoEntreAtaques;
    }

    public HabilidadeData ObterHabilidadePronta(List<GameObject> aliadosVivos, List<GameObject> aliadosMortos)
    {
        HabilidadeData magiaDesejada = null;
        int maiorPrioridadeValida = -1;

        if (dadosOriginais.habilidadesAtivas != null)
        {
            foreach (SlotHabilidade slot in dadosOriginais.habilidadesAtivas)
            {
                if (slot.habilidade == null) continue;
                if (slot.habilidade.custoPA > dadosOriginais.paMaximo) continue;
                if (recargas.ContainsKey(slot.habilidade) && recargas[slot.habilidade] > 0) continue;

                bool condicaoAtendida = VerificarCondicao(slot.habilidade, aliadosVivos, aliadosMortos);

                // PONTO 2: Agora verificamos se TEMOS Energia (paAtual) ANTES de eleger como prioridade!
                if (paAtual >= slot.habilidade.custoPA && condicaoAtendida && slot.prioridade > maiorPrioridadeValida)
                {
                    maiorPrioridadeValida = slot.prioridade;
                    magiaDesejada = slot.habilidade;
                }
            }
        }
        return magiaDesejada; // Já garantimos que tem PA suficiente
    }

    private bool VerificarCondicao(HabilidadeData hab, List<GameObject> aliadosVivos, List<GameObject> aliadosMortos)
    {
        if (hab.condicaoUso == HabilidadeData.CondicaoDeUso.Sempre) return true;
        if (hab.condicaoUso == HabilidadeData.CondicaoDeUso.MeuHPAbaixoDe50) return (hpAtual / hpMaximo) < 0.5f;

        if (hab.condicaoUso == HabilidadeData.CondicaoDeUso.AliadoComHPAbaixoDe50)
        {
            if (aliadosVivos == null) return false;
            foreach (GameObject aliado in aliadosVivos)
            {
                if (aliado != null && aliado.activeInHierarchy)
                {
                    if ((aliado.GetComponent<UnidadeEmBatalha>().hpAtual / aliado.GetComponent<UnidadeEmBatalha>().hpMaximo) < 0.5f) return true;
                }
            }
            return false;
        }

        // PONTO 4: IA agora verifica ativamente se há mortos
        if (hab.condicaoUso == HabilidadeData.CondicaoDeUso.AliadoMorto)
        {
            return aliadosMortos != null && aliadosMortos.Count > 0;
        }

        return true;
    }

    // PONTO 5: Método limpo, responsável EXCLUSIVAMENTE por cobrar o custo e iniciar CD
    public void ConsumirCustoECooldown(HabilidadeData magia)
    {
        cronometroAtaque = 0f;
        paAtual -= magia.custoPA;
        paAtual = Mathf.Clamp(paAtual, 0, dadosOriginais.paMaximo); // PONTO 6: Proteção

        if (magia.tempoDeRecarga > 0) recargas[magia] = magia.tempoDeRecarga;
        if (animador != null) animador.AnimarAtaque();
    }

    public void ExecutarAtaqueBasico(UnidadeEmBatalha alvo)
    {
        cronometroAtaque = 0f;
        paAtual += dadosOriginais.paGeradoPorAtaque;
        paAtual = Mathf.Clamp(paAtual, 0, dadosOriginais.paMaximo);

        if (animador != null) animador.AnimarAtaque();

        float atraso = (alcance > 2.0f) ? 0.4f : 0.1f;
        StartCoroutine(RotinaAplicarDano(alvo, danoAtual, atraso));
    }

    // Dispara o Projétil / Corrotina da Magia
    public void DispararMagiaDeDano(UnidadeEmBatalha alvo, float danoDaMagia, int golpes)
    {
        float atrasoInicial = (alcance > 2.0f) ? 0.4f : 0.1f;
        StartCoroutine(RotinaMagiaMultipla(alvo, danoDaMagia, golpes, atrasoInicial));
    }

    private IEnumerator RotinaAplicarDano(UnidadeEmBatalha alvo, float danoAplicado, float atraso)
    {
        yield return new WaitForSeconds(atraso);
        // PONTO 1: Se quem disparou (conjurador) morreu durante a viagem do projétil, ou alvo morreu, aborta!
        if (!gameObject.activeInHierarchy || hpAtual <= 0) yield break;

        if (alvo != null && alvo.hpAtual > 0) alvo.ReceberDano(danoAplicado);
    }

    private IEnumerator RotinaMagiaMultipla(UnidadeEmBatalha alvo, float danoPorGolpe, int golpes, float atrasoInicial)
    {
        yield return new WaitForSeconds(atrasoInicial);
        if (!gameObject.activeInHierarchy || hpAtual <= 0) yield break; // PONTO 1 Proteção

        for (int i = 0; i < golpes; i++)
        {
            if (alvo != null && alvo.hpAtual > 0) alvo.ReceberDano(danoPorGolpe);
            yield return new WaitForSeconds(0.2f);
        }
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

    public void ReceberCura(float valorCura)
    {
        if (hpAtual <= 0) return;
        hpAtual += valorCura;
        hpAtual = Mathf.Clamp(hpAtual, 0, hpMaximo); // PONTO 6: Proteção do limite máximo
        AtualizarBarra();
    }

    private void AtualizarBarra()
    {
        if (barraDeVida != null) barraDeVida.fillAmount = hpAtual / hpMaximo;
    }

    private void Morrer()
    {
        AoMorrer?.Invoke(this);
        gameObject.SetActive(false);
    }

    public void Ressuscitar(float porcentagemDeHpRestaurado)
    {
        gameObject.SetActive(true);
        hpAtual = hpMaximo * porcentagemDeHpRestaurado;
        paAtual = 0f;
        efeitosAtivos.Clear();
        recargas.Clear();
        RecalcularAtributos();
        AtualizarBarra();
        if (animador != null) animador.IniciarEfeitoSurgimento();
    }
}