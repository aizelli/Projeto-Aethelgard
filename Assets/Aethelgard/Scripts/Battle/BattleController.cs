using System.Collections.Generic;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    public enum EstadoBatalha { Marchando, Combate, VitoriaOnda, Derrota, TransicaoFase }
    public EstadoBatalha estadoAtual;

    [Header("Gerenciadores")]
    public ParallaxManager parallaxManager;
    public SpawnManager spawnManager;
    public PlayerTeamManager playerTeamManager;

    [Header("Configuração Global")]
    public BiomeData biomaAtual;
    public int indiceFaseAtual = 0;
    public int ondaAtual = 1;
    public bool tentativaEmAndamento = false;

    private bool trocandoDeOnda = false;
    private FaseData DadosFaseAtual => biomaAtual.fasesDoBioma[indiceFaseAtual];

    void Start()
    {
        indiceFaseAtual = 0;
        IniciarFase();
    }

    void Update()
    {
        switch (estadoAtual)
        {
            case EstadoBatalha.Marchando: LidarComMarcha(); break;
            case EstadoBatalha.Combate: LidarComCombate(); break;
        }
    }

    public void IniciarFase()
    {
        ondaAtual = 1;
        tentativaEmAndamento = true;
        if (playerTeamManager != null) playerTeamManager.InstanciarEquipe();
        if (parallaxManager != null) parallaxManager.SetupParallax(DadosFaseAtual);
        IniciarNovaOnda();
    }

    private void IniciarNovaOnda()
    {
        spawnManager.SpawnarOnda(DadosFaseAtual, ondaAtual);
        estadoAtual = EstadoBatalha.Marchando;
        if (parallaxManager != null) parallaxManager.isMoving = true;
    }

    private GameObject ObterAlvoMaisProximo(GameObject unidadeOrigem, List<GameObject> listaAlvos)
    {
        GameObject alvoMaisProximo = null;
        float menorDistancia = float.MaxValue;
        foreach (GameObject alvo in listaAlvos)
        {
            if (alvo == null || !alvo.activeInHierarchy) continue;
            float dist = Mathf.Abs(unidadeOrigem.transform.position.x - alvo.transform.position.x);
            if (dist < menorDistancia)
            {
                menorDistancia = dist;
                alvoMaisProximo = alvo;
            }
        }
        return alvoMaisProximo;
    }

    private GameObject ObterAliadoMenorHP(List<GameObject> equipe)
    {
        GameObject aliadoNecessitado = null;
        float menorPorcentagem = float.MaxValue;
        foreach (GameObject aliado in equipe)
        {
            if (aliado == null || !aliado.activeInHierarchy) continue;
            UnidadeEmBatalha un = aliado.GetComponent<UnidadeEmBatalha>();
            float porcentagemHP = un.hpAtual / un.hpMaximo;
            if (porcentagemHP < menorPorcentagem)
            {
                menorPorcentagem = porcentagemHP;
                aliadoNecessitado = aliado;
            }
        }
        return aliadoNecessitado;
    }

    private void LimparMortosAbsolutos()
    {
        if (spawnManager != null && spawnManager.inimigosInstanciados != null)
            spawnManager.inimigosInstanciados.RemoveAll(item => item == null);

        if (playerTeamManager != null && playerTeamManager.heroisInstanciados != null)
            playerTeamManager.heroisInstanciados.RemoveAll(item => item == null);
    }

    private void LidarComMarcha()
    {
        LimparMortosAbsolutos();
        bool encontrouInimigo = false;

        foreach (GameObject inimigo in spawnManager.inimigosAtivos)
        {
            if (inimigo == null) continue;
            UnidadeEmBatalha unidade = inimigo.GetComponent<UnidadeEmBatalha>();

            inimigo.transform.Translate(Vector3.right * unidade.velocidade * Time.deltaTime, Space.World);
            if (unidade.animador != null) unidade.animador.isWalking = true;

            GameObject alvo = ObterAlvoMaisProximo(inimigo, playerTeamManager.heroisAtivos);
            if (alvo != null && Mathf.Abs(inimigo.transform.position.x - alvo.transform.position.x) <= unidade.alcance)
                encontrouInimigo = true;
        }

        foreach (GameObject heroi in playerTeamManager.heroisAtivos)
        {
            if (heroi == null) continue;
            UnidadeEmBatalha unidade = heroi.GetComponent<UnidadeEmBatalha>();
            if (unidade.animador != null) unidade.animador.isWalking = true;

            if (heroi.transform.position.x < unidade.posicaoBase.x)
            {
                heroi.transform.position = Vector3.MoveTowards(
                    heroi.transform.position,
                    new Vector3(unidade.posicaoBase.x, heroi.transform.position.y, heroi.transform.position.z),
                    unidade.velocidade * Time.deltaTime
                );
            }

            GameObject alvo = ObterAlvoMaisProximo(heroi, spawnManager.inimigosAtivos);
            if (alvo != null && Mathf.Abs(heroi.transform.position.x - alvo.transform.position.x) <= unidade.alcance)
                encontrouInimigo = true;
        }

        if (encontrouInimigo && !trocandoDeOnda)
        {
            estadoAtual = EstadoBatalha.Combate;
            if (parallaxManager != null) parallaxManager.isMoving = false;
        }
    }

    private void LidarComCombate()
    {
        LimparMortosAbsolutos();

        if (playerTeamManager.heroisAtivos.Count == 0)
        {
            if (estadoAtual != EstadoBatalha.Derrota) StartCoroutine(RotinaDerrota());
            return;
        }

        if (spawnManager.inimigosAtivos.Count == 0 && !trocandoDeOnda)
        {
            StartCoroutine(RotinaPausaEntreOndas());
            return;
        }

        ProcessarTurnoEquipe(spawnManager.inimigosAtivos, playerTeamManager.heroisAtivos);
        ProcessarTurnoEquipe(playerTeamManager.heroisAtivos, spawnManager.inimigosAtivos);
    }

    // Simplificado para injetar os Aliados e Inimigos dinamicamente com base em quem está atacando
    private void ProcessarTurnoEquipe(List<GameObject> atacantes, List<GameObject> inimigos)
    {
        foreach (GameObject go in atacantes)
        {
            if (go == null || !go.activeInHierarchy) continue;
            UnidadeEmBatalha unidade = go.GetComponent<UnidadeEmBatalha>();
            GameObject inimigoAlvo = ObterAlvoMaisProximo(go, inimigos);

            if (inimigoAlvo != null)
            {
                float dist = Mathf.Abs(go.transform.position.x - inimigoAlvo.transform.position.x);

                if (dist > unidade.alcance)
                {
                    if (unidade.animador != null) unidade.animador.isWalking = true;
                    float direcao = Mathf.Sign(inimigoAlvo.transform.position.x - go.transform.position.x);
                    go.transform.Translate(Vector3.right * direcao * unidade.velocidade * Time.deltaTime, Space.World);
                }
                else
                {
                    if (unidade.animador != null) unidade.animador.isWalking = false;
                    if (unidade.PodeAgir())
                    {
                        ExecutarAcao(unidade, inimigoAlvo);
                    }
                }
            }
        }
    }

    private void ExecutarAcao(UnidadeEmBatalha unidade, GameObject inimigoPadrao)
    {
        // PONTO 4: Identificação de Equipe (Agnóstico)
        bool ehHeroiJogador = playerTeamManager.heroisAtivos.Contains(unidade.gameObject);

        List<GameObject> meusAliadosVivos = ehHeroiJogador ? playerTeamManager.heroisAtivos : spawnManager.inimigosAtivos;
        List<GameObject> meusAliadosMortos = ehHeroiJogador ? playerTeamManager.heroisMortos : spawnManager.inimigosMortos;

        HabilidadeData magia = unidade.ObterHabilidadePronta(meusAliadosVivos, meusAliadosMortos);

        if (magia != null)
        {
            // PONTO 5: Cobra PA e Reseta Cronômetro primeiro (Genérico)
            unidade.ConsumirCustoECooldown(magia);

            float valorEfeito = CalcularValorEfeito(unidade, magia);

            // AÇÃO: DANO
            if (magia.tipo == HabilidadeData.TipoHabilidade.DanoSingular || magia.tipo == HabilidadeData.TipoHabilidade.AtaqueMultiplo)
            {
                int golpes = magia.tipo == HabilidadeData.TipoHabilidade.AtaqueMultiplo ? magia.numeroDeGolpes : 1;
                UnidadeEmBatalha alvoUnidade = inimigoPadrao.GetComponent<UnidadeEmBatalha>();
                unidade.DispararMagiaDeDano(alvoUnidade, valorEfeito, golpes);
            }
            // AÇÃO: CURA
            else if (magia.tipo == HabilidadeData.TipoHabilidade.Cura)
            {
                if (magia.alvo == HabilidadeData.AlvoHabilidade.TodosAliados)
                {
                    foreach (GameObject aliado in meusAliadosVivos)
                        aliado.GetComponent<UnidadeEmBatalha>().ReceberCura(valorEfeito);
                }
                else
                {
                    GameObject alvoCura = magia.alvo == HabilidadeData.AlvoHabilidade.SiMesmo ? unidade.gameObject : ObterAliadoMenorHP(meusAliadosVivos);
                    if (alvoCura != null) alvoCura.GetComponent<UnidadeEmBatalha>().ReceberCura(valorEfeito);
                }
            }
            // AÇÃO: BUFF (PONTO 3: Integrado)
            else if (magia.tipo == HabilidadeData.TipoHabilidade.BuffStatus)
            {
                if (magia.alvo == HabilidadeData.AlvoHabilidade.TodosAliados)
                {
                    foreach (GameObject aliado in meusAliadosVivos)
                        aliado.GetComponent<UnidadeEmBatalha>().AplicarBuff(magia.statusAlterado, valorEfeito, magia.duracaoBuff);
                }
                else
                {
                    GameObject alvoBuff = magia.alvo == HabilidadeData.AlvoHabilidade.SiMesmo ? unidade.gameObject : ObterAliadoMenorHP(meusAliadosVivos);
                    if (alvoBuff != null) alvoBuff.GetComponent<UnidadeEmBatalha>().AplicarBuff(magia.statusAlterado, valorEfeito, magia.duracaoBuff);
                }
            }
            // AÇÃO: RESSURREIÇÃO (PONTO 4)
            else if (magia.tipo == HabilidadeData.TipoHabilidade.Reviver)
            {
                if (meusAliadosMortos.Count > 0)
                {
                    GameObject alvoMorto = meusAliadosMortos[0]; // Futuramente: Podemos ordenar por level/prioridade
                    alvoMorto.GetComponent<UnidadeEmBatalha>().Ressuscitar(magia.multiplicadorOuValorBase);
                }
            }
        }
        else
        {
            UnidadeEmBatalha alvoUnidade = inimigoPadrao.GetComponent<UnidadeEmBatalha>();
            unidade.ExecutarAtaqueBasico(alvoUnidade);
        }
    }

    private float CalcularValorEfeito(UnidadeEmBatalha conjurador, HabilidadeData magia)
    {
        switch (magia.escalaDoValor)
        {
            case HabilidadeData.TipoEscala.Fixo: return magia.multiplicadorOuValorBase;
            case HabilidadeData.TipoEscala.PorcentagemDano: return conjurador.danoAtual * magia.multiplicadorOuValorBase;
            case HabilidadeData.TipoEscala.PorcentagemHPMaximo: return conjurador.hpMaximo * magia.multiplicadorOuValorBase;
            default: return magia.multiplicadorOuValorBase;
        }
    }

    private System.Collections.IEnumerator RotinaPausaEntreOndas()
    {
        trocandoDeOnda = true;
        spawnManager.LimparInimigos();

        estadoAtual = EstadoBatalha.Marchando;
        if (parallaxManager != null) parallaxManager.isMoving = true;

        yield return new WaitForSeconds(2.5f);

        ondaAtual++;
        if (ondaAtual > DadosFaseAtual.totalOndas) VencerFase();
        else IniciarNovaOnda();

        trocandoDeOnda = false;
    }

    private System.Collections.IEnumerator RotinaDerrota()
    {
        estadoAtual = EstadoBatalha.Derrota;
        if (parallaxManager != null) parallaxManager.isMoving = false;

        yield return new WaitForSeconds(3.0f);
        IniciarFase();
    }

    private void VencerFase()
    {
        tentativaEmAndamento = false;
        indiceFaseAtual++;
        if (indiceFaseAtual >= biomaAtual.fasesDoBioma.Length)
        {
            estadoAtual = EstadoBatalha.VitoriaOnda;
            return;
        }
        IniciarFase();
    }

    public void RepetirFaseAtual() => IniciarFase();
}