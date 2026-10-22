using System.Collections.Generic;
using UnityEngine;

public class PlayerTeamManager : MonoBehaviour
{
    [Header("Moldes")]
    public GameObject prefabTokenBase;
    public Transform pontoSpawnHerois;

    [Header("A Sua Equipa")]
    public List<MonstroData> equipeAtual;
    public List<GameObject> heroisInstanciados = new List<GameObject>(); // Mudamos o nome para refletir que eles existem, vivos ou mortos

    // Propriedade que filtra rapidamente quem está vivo para a marcha e combate normal
    public List<GameObject> heroisAtivos
    {
        get
        {
            List<GameObject> vivos = new List<GameObject>();
            foreach (GameObject heroi in heroisInstanciados)
            {
                if (heroi != null && heroi.activeInHierarchy)
                {
                    vivos.Add(heroi);
                }
            }
            return vivos;
        }
    }

    // P02.03: Propriedade que filtra rapidamente quem precisa de ser ressuscitado
    public List<GameObject> heroisMortos
    {
        get
        {
            List<GameObject> mortos = new List<GameObject>();
            foreach (GameObject heroi in heroisInstanciados)
            {
                if (heroi != null && !heroi.activeInHierarchy)
                {
                    mortos.Add(heroi);
                }
            }
            return mortos;
        }
    }

    public void InstanciarEquipe()
    {
        LimparEquipe();

        float offset = 0f;
        for (int i = 0; i < equipeAtual.Count; i++)
        {
            if (equipeAtual[i] == null) continue;

            Vector3 posicaoSpawn = pontoSpawnHerois.position - new Vector3(offset, 0, 0);
            GameObject novoHeroi = Instantiate(prefabTokenBase, posicaoSpawn, Quaternion.identity);
            novoHeroi.name = "Heroi_" + equipeAtual[i].nomeCarta;

            SpriteRenderer sr = novoHeroi.GetComponent<SpriteRenderer>();
            sr.sprite = equipeAtual[i].spriteToken;
            sr.flipX = true;

            UnidadeEmBatalha unidade = novoHeroi.GetComponent<UnidadeEmBatalha>();
            TokenAnimator animador = novoHeroi.GetComponent<TokenAnimator>();

            unidade.Configurar(equipeAtual[i], animador, 5);
            unidade.posicaoBase = posicaoSpawn;
            animador.IniciarEfeitoSurgimento();

            heroisInstanciados.Add(novoHeroi);
            offset += 0.6f;
        }
    }

    public void LimparEquipe()
    {
        foreach (GameObject heroi in heroisInstanciados)
        {
            if (heroi != null) Destroy(heroi);
        }
        heroisInstanciados.Clear();
    }
}