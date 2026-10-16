using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Moldes")]
    public GameObject prefabTokenBase; // O nosso Prefab Universal
    public Transform pontoSpawnInimigo;

    [Header("Controle")]
    public List<GameObject> activeEnemies = new List<GameObject>();

    public void SpawnarOnda(FaseData fase, int numeroOnda)
    {
        LimparInimigos();
        List<MonstroData> inimigosSorteados = new List<MonstroData>();

        // 1. REGRA DO BOSS: A última onda é sempre o Boss (ou Miniboss) sozinho
        if (numeroOnda >= fase.totalOndas)
        {
            if (fase.isFaseDoBossFinal && fase.bossFinal != null)
            {
                inimigosSorteados.Add(fase.bossFinal);
            }
            else if (fase.miniboss != null)
            {
                inimigosSorteados.Add(fase.miniboss);
            }
        }
        // 2. REGRA DE ONDAS REGULARES (GERAÇÃO PROCEDURAL)
        else
        {
            // Matemática de Volume: Iniciais + (Onda Atual * Acréscimo)
            int quantidadeNestaOnda = fase.quantidadeInicialInimigos + ((numeroOnda - 1) * fase.acrescimoPorOnda);

            // Calcula o progresso atual da fase (0.0 a 1.0) para definir a variedade
            // Exclui a última onda da conta porque ela é reservada para o Boss
            float progressoFase = (float)numeroOnda / (fase.totalOndas - 1);

            int tiposPermitidos = 1; // Ondas Iniciais
            if (progressoFase > 0.33f) tiposPermitidos = 2; // Ondas Medianas
            if (progressoFase > 0.66f) tiposPermitidos = 3; // Ondas Finais

            // Trava de segurança caso o array tenha menos de 3 monstros configurados
            tiposPermitidos = Mathf.Min(tiposPermitidos, fase.inimigosRegulares.Length);

            // Sorteia os monstros com base no volume e nos tipos permitidos até ao momento
            for (int i = 0; i < quantidadeNestaOnda; i++)
            {
                int indiceSorteado = Random.Range(0, tiposPermitidos);
                MonstroData inimigoEscolhido = fase.inimigosRegulares[indiceSorteado];

                if (inimigoEscolhido != null)
                {
                    inimigosSorteados.Add(inimigoEscolhido);
                }
            }
        }

        // 3. INSTANCIAÇÃO VISUAL (O código de instanciar o prefab que já tínhamos)
        float offset = 0f;
        foreach (MonstroData inimigoData in inimigosSorteados)
        {
            // Instancia o Prefab
            Vector3 posicaoSpawn = pontoSpawnInimigo.position + new Vector3(offset, 0, 0);
            GameObject novoInimigo = Instantiate(prefabTokenBase, posicaoSpawn, Quaternion.identity);
            novoInimigo.name = "Inimigo_" + inimigoData.nomeCarta;

            // Configura o Visual
            SpriteRenderer sr = novoInimigo.GetComponent<SpriteRenderer>();
            sr.sprite = inimigoData.spriteToken;
            sr.flipX = false;

            // Injeta a Inteligência
            UnidadeEmBatalha unidade = novoInimigo.GetComponent<UnidadeEmBatalha>();
            TokenAnimator animador = novoInimigo.GetComponent<TokenAnimator>();

            unidade.Configurar(inimigoData, animador, 1);
            animador.IniciarEfeitoSurgimento();

            activeEnemies.Add(novoInimigo);

            // Espaçamento visual para não nascerem todos uns em cima dos outros
            offset += 0.5f;
        }
    }

    public void LimparInimigos()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();
    }
}