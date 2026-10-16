using System.Collections.Generic;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    public enum EstadoBatalha { Marchando, Combate, VitoriaOnda, TransicaoFase }
    public EstadoBatalha estadoAtual;

    [Header("Gerenciadores")]
    public ParallaxManager parallaxManager;
    public SpawnManager spawnManager;
    public PlayerTeamManager playerTeamManager;

    [Header("Configuração Global")]
    public BiomeData biomaAtual;
    public int indiceFaseAtual = 0;
    public int ondaAtual = 1;
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

    private void IniciarFase()
    {
        ondaAtual = 1;
        if (playerTeamManager != null) playerTeamManager.InstanciarEquipe();
        parallaxManager.SetupParallax(DadosFaseAtual);
        IniciarNovaOnda();
    }

    private void IniciarNovaOnda()
    {
        spawnManager.SpawnarOnda(DadosFaseAtual, ondaAtual);
        estadoAtual = EstadoBatalha.Marchando;
        parallaxManager.isMoving = true;
    }

    // --- SISTEMAS DE VISÃO (TARGETING) ---

    private GameObject ObterAlvoMaisProximo(GameObject unidadeOrigem, List<GameObject> listaAlvos)
    {
        GameObject alvoMaisProximo = null;
        float menorDistancia = float.MaxValue;
        foreach (GameObject alvo in listaAlvos)
        {
            if (alvo == null) continue;
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
            if (aliado == null) continue;
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

    private void LimparMortos()
    {
        spawnManager.activeEnemies.RemoveAll(item => item == null);
        playerTeamManager.heroisAtivos.RemoveAll(item => item == null);
    }

    private void LidarComMarcha()
    {
        LimparMortos();
        bool encontrouInimigo = false;

        // 1. INIMIGOS AVANÇAM (Da esquerda para a direita)
        foreach (GameObject inimigo in spawnManager.activeEnemies)
        {
            if (inimigo == null) continue;
            UnidadeEmBatalha unidade = inimigo.GetComponent<UnidadeEmBatalha>();

            inimigo.transform.Translate(Vector3.right * unidade.velocidade * Time.deltaTime, Space.World);
            unidade.animador.isWalking = true;

            GameObject alvo = ObterAlvoMaisProximo(inimigo, playerTeamManager.heroisAtivos);
            if (alvo != null && Mathf.Abs(inimigo.transform.position.x - alvo.transform.position.x) <= unidade.alcance)
                encontrouInimigo = true;
        }

        // 2. HERÓIS RECUAM / MOONWALK (Da esquerda para a direita, rumo à base)
        foreach (GameObject heroi in playerTeamManager.heroisAtivos)
        {
            if (heroi == null) continue;
            UnidadeEmBatalha unidade = heroi.GetComponent<UnidadeEmBatalha>();
            unidade.animador.isWalking = true;

            // Se o herói tiver avançado (X ficou menor que a base), ele desliza de volta para a base
            if (heroi.transform.position.x < unidade.posicaoBase.x)
            {
                heroi.transform.position = Vector3.MoveTowards(
                    heroi.transform.position,
                    new Vector3(unidade.posicaoBase.x, heroi.transform.position.y, heroi.transform.position.z),
                    unidade.velocidade * Time.deltaTime
                );
            }

            GameObject alvo = ObterAlvoMaisProximo(heroi, spawnManager.activeEnemies);
            if (alvo != null && Mathf.Abs(heroi.transform.position.x - alvo.transform.position.x) <= unidade.alcance)
                encontrouInimigo = true;
        }

        // Só para de marchar se encontrar inimigo E não estiver naquele intervalo de descanso
        if (encontrouInimigo && !trocandoDeOnda)
        {
            estadoAtual = EstadoBatalha.Combate;
            parallaxManager.isMoving = false;
        }
    }
    private void LidarComCombate()
    {
        LimparMortos();

        // GATILHO: Se matámos todos e não estamos a trocar de onda, começa o delay!
        if (spawnManager.activeEnemies.Count == 0 && !trocandoDeOnda)
        {
            StartCoroutine(RotinaPausaEntreOndas());
            return;
        }

        if (playerTeamManager.heroisAtivos.Count == 0)
        {
            Debug.Log("[BATTLE] Game Over!");
            estadoAtual = EstadoBatalha.VitoriaOnda;
            return;
        }

        ProcessarTurnoEquipe(spawnManager.activeEnemies, playerTeamManager.heroisAtivos, spawnManager.activeEnemies);
        ProcessarTurnoEquipe(playerTeamManager.heroisAtivos, spawnManager.activeEnemies, playerTeamManager.heroisAtivos);
    }

    private void ProcessarTurnoEquipe(List<GameObject> atacantes, List<GameObject> inimigos, List<GameObject> aliados)
    {
        foreach (GameObject go in atacantes)
        {
            if (go == null) continue;
            UnidadeEmBatalha unidade = go.GetComponent<UnidadeEmBatalha>();
            GameObject inimigoAlvo = ObterAlvoMaisProximo(go, inimigos);

            if (inimigoAlvo != null)
            {
                float dist = Mathf.Abs(go.transform.position.x - inimigoAlvo.transform.position.x);

                // SE ESTIVER FORA DE ALCANCE: Anda na direção do inimigo!
                if (dist > unidade.alcance)
                {
                    unidade.animador.isWalking = true;

                    // Calcula se tem que andar para a direita (+1) ou esquerda (-1) para chegar ao alvo
                    float direcao = Mathf.Sign(inimigoAlvo.transform.position.x - go.transform.position.x);
                    go.transform.Translate(Vector3.right * direcao * unidade.velocidade * Time.deltaTime, Space.World);
                }
                // SE ESTIVER NO ALCANCE: Para e ataca
                else
                {
                    unidade.animador.isWalking = false;
                    if (unidade.PodeAgir())
                    {
                        ExecutarAcao(unidade, inimigoAlvo, aliados, inimigos);
                    }
                }
            }
        }
    }

    // --- CÉREBRO DAS MAGIAS ---

    private void ExecutarAcao(UnidadeEmBatalha unidade, GameObject inimigoPadrao, List<GameObject> aliados, List<GameObject> inimigos)
    {
        HabilidadeData magia = unidade.ObterHabilidadePronta();

        // 1. TEM MAGIA PRONTA? USA MAGIA.
        if (magia != null)
        {
            float valorEfeito = CalcularValorEfeito(unidade, magia);

            // TIPO: Dano ou Ataque Múltiplo
            if (magia.tipo == HabilidadeData.TipoHabilidade.DanoSingular || magia.tipo == HabilidadeData.TipoHabilidade.AtaqueMultiplo)
            {
                int golpes = magia.tipo == HabilidadeData.TipoHabilidade.AtaqueMultiplo ? magia.numeroDeGolpes : 1;
                UnidadeEmBatalha alvoUnidade = inimigoPadrao.GetComponent<UnidadeEmBatalha>();

                for (int i = 0; i < golpes; i++)
                {
                    if (alvoUnidade.hpAtual > 0) alvoUnidade.ReceberDano(valorEfeito);
                }
                Debug.Log($"[MAGIA] {unidade.dadosOriginais.nomeCarta} usou {magia.nomeHabilidade}! ({golpes}x de {valorEfeito} dano)");
            }
            // TIPO: Cura
            else if (magia.tipo == HabilidadeData.TipoHabilidade.Cura)
            {
                if (magia.alvo == HabilidadeData.AlvoHabilidade.TodosAliados)
                {
                    foreach (GameObject aliado in aliados)
                    {
                        if (aliado != null) aliado.GetComponent<UnidadeEmBatalha>().ReceberCura(valorEfeito);
                    }
                    Debug.Log($"[MAGIA] {unidade.dadosOriginais.nomeCarta} curou TODOS os aliados em {valorEfeito} HP!");
                }
                else
                {
                    GameObject alvoCura = magia.alvo == HabilidadeData.AlvoHabilidade.SiMesmo ? unidade.gameObject : ObterAliadoMenorHP(aliados);
                    if (alvoCura != null)
                    {
                        alvoCura.GetComponent<UnidadeEmBatalha>().ReceberCura(valorEfeito);
                        Debug.Log($"[MAGIA] {unidade.dadosOriginais.nomeCarta} curou {alvoCura.name} em {valorEfeito} HP!");
                    }
                }
            }
            // (Buffs e Reviver serão tratados aqui futuramente)

            unidade.RegistrarUsoHabilidade(magia);
        }
        // 2. NÃO TEM MAGIA? ATAQUE BÁSICO.
        else
        {
            UnidadeEmBatalha alvoUnidade = inimigoPadrao.GetComponent<UnidadeEmBatalha>();
            alvoUnidade.ReceberDano(unidade.danoAtual);
            unidade.RegistrarAtaqueBasico();
            // Debug.Log($"[ATAQUE] {unidade.dadosOriginais.nomeCarta} causou {unidade.danoAtual} dano básico.");
        }
    }

    private float CalcularValorEfeito(UnidadeEmBatalha conjurador, HabilidadeData magia)
    {
        switch (magia.escalaDoValor)
        {
            case HabilidadeData.TipoEscala.Fixo:
                return magia.multiplicadorOuValorBase;
            case HabilidadeData.TipoEscala.PorcentagemDano:
                return conjurador.danoAtual * magia.multiplicadorOuValorBase;
            case HabilidadeData.TipoEscala.PorcentagemHPMaximo:
                return conjurador.hpMaximo * magia.multiplicadorOuValorBase;
            default:
                return magia.multiplicadorOuValorBase;
        }
    }

    // --- CONTROLE DE ONDAS ---

    private System.Collections.IEnumerator RotinaPausaEntreOndas()
    {
        trocandoDeOnda = true;
        spawnManager.LimparInimigos(); // Garante que a lista está vazia

        // 1. Liberta os heróis para recuarem e o cenário rolar
        estadoAtual = EstadoBatalha.Marchando;
        parallaxManager.isMoving = true;

        // 2. Espera 2.5 segundos (pode ajustar este tempo como preferir!)
        yield return new WaitForSeconds(2.5f);

        // 3. Avança para a próxima onda ou finaliza a fase
        ondaAtual++;
        if (ondaAtual > DadosFaseAtual.totalOndas)
        {
            VencerFase();
        }
        else
        {
            IniciarNovaOnda();
        }

        trocandoDeOnda = false;
    }

    private void VencerFase()
    {
        indiceFaseAtual++;
        if (indiceFaseAtual >= biomaAtual.fasesDoBioma.Length)
        {
            estadoAtual = EstadoBatalha.VitoriaOnda;
            return;
        }
        IniciarFase();
    }
}