using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Moldes")]
    public GameObject prefabTokenBase;
    public Transform pontoSpawnInimigo;

    [Header("Controle")]
    public List<GameObject> inimigosInstanciados = new List<GameObject>();

    // PONTO 4: Propriedades para separar quem está vivo de quem está morto
    public List<GameObject> inimigosAtivos
    {
        get
        {
            List<GameObject> vivos = new List<GameObject>();
            foreach (GameObject ini in inimigosInstanciados)
                if (ini != null && ini.activeInHierarchy) vivos.Add(ini);
            return vivos;
        }
    }

    public List<GameObject> inimigosMortos
    {
        get
        {
            List<GameObject> mortos = new List<GameObject>();
            foreach (GameObject ini in inimigosInstanciados)
                if (ini != null && !ini.activeInHierarchy) mortos.Add(ini);
            return mortos;
        }
    }

    public void SpawnarOnda(FaseData fase, int numeroOnda)
    {
        LimparInimigos();
        List<MonstroData> inimigosSorteados = new List<MonstroData>();

        if (numeroOnda >= fase.totalOndas)
        {
            MonstroData bossPrincipal = fase.isFaseDoBossFinal && fase.bossFinal != null ? fase.bossFinal : fase.miniboss;
            if (bossPrincipal != null)
            {
                for (int i = 0; i < fase.quantidadeAcompanhantesBoss; i++)
                {
                    if (fase.lacaioBossFixo != null) inimigosSorteados.Add(fase.lacaioBossFixo);
                    else if (fase.inimigosRegulares != null && fase.inimigosRegulares.Length > 0)
                        inimigosSorteados.Add(fase.inimigosRegulares[Random.Range(0, fase.inimigosRegulares.Length)]);
                }
                inimigosSorteados.Add(bossPrincipal);
            }
        }
        else
        {
            int quantidadeNestaOnda = fase.quantidadeInicialInimigos + ((numeroOnda - 1) * fase.acrescimoPorOnda);
            float progressoFase = (float)numeroOnda / Mathf.Max(1, fase.totalOndas - 1);

            int tiposPermitidos = 1;
            if (progressoFase > 0.33f) tiposPermitidos = 2;
            if (progressoFase > 0.66f) tiposPermitidos = 3;

            tiposPermitidos = Mathf.Min(tiposPermitidos, fase.inimigosRegulares.Length);

            for (int i = 0; i < quantidadeNestaOnda; i++)
            {
                MonstroData inimigoEscolhido = fase.inimigosRegulares[Random.Range(0, tiposPermitidos)];
                if (inimigoEscolhido != null) inimigosSorteados.Add(inimigoEscolhido);
            }
        }

        float offset = 0f;
        foreach (MonstroData inimigoData in inimigosSorteados)
        {
            Vector3 posicaoSpawn = pontoSpawnInimigo.position + new Vector3(offset, 0, 0);
            GameObject novoInimigo = Instantiate(prefabTokenBase, posicaoSpawn, Quaternion.identity);
            novoInimigo.name = "Inimigo_" + inimigoData.nomeCarta;

            SpriteRenderer sr = novoInimigo.GetComponent<SpriteRenderer>();
            sr.sprite = inimigoData.spriteToken;
            sr.flipX = false;

            UnidadeEmBatalha unidade = novoInimigo.GetComponent<UnidadeEmBatalha>();
            TokenAnimator animador = novoInimigo.GetComponent<TokenAnimator>();

            unidade.Configurar(inimigoData, animador, 1);
            animador.IniciarEfeitoSurgimento();

            inimigosInstanciados.Add(novoInimigo);
            offset += 0.6f;
        }
    }

    public void LimparInimigos()
    {
        foreach (GameObject enemy in inimigosInstanciados)
        {
            if (enemy != null) Destroy(enemy);
        }
        inimigosInstanciados.Clear();
    }
}