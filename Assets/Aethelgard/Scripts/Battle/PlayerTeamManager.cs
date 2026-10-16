using System.Collections.Generic;
using UnityEngine;

public class PlayerTeamManager : MonoBehaviour
{
    [Header("Moldes")]
    public GameObject prefabTokenBase; // O nosso Prefab Universal
    public Transform pontoSpawnHerois;

    [Header("A Sua Equipa")]
    public List<MonstroData> equipeAtual;
    public List<GameObject> heroisAtivos = new List<GameObject>();

    public void InstanciarEquipe()
    {
        LimparEquipe();

        float offset = 0f;
        for (int i = 0; i < equipeAtual.Count; i++)
        {
            if (equipeAtual[i] == null) continue;

            // 1. Instancia o Prefab (Avançando para a esquerda no spawn)
            Vector3 posicaoSpawn = pontoSpawnHerois.position - new Vector3(offset, 0, 0);
            GameObject novoHeroi = Instantiate(prefabTokenBase, posicaoSpawn, Quaternion.identity);
            novoHeroi.name = "Heroi_" + equipeAtual[i].nomeCarta;

            // 2. Configura o Visual e inverte o eixo X
            SpriteRenderer sr = novoHeroi.GetComponent<SpriteRenderer>();
            sr.sprite = equipeAtual[i].spriteToken;
            sr.flipX = true; // Heróis virados para a direita

            // 3. Injeta a Inteligência e a Matemática (Nível 5 provisório)
            UnidadeEmBatalha unidade = novoHeroi.GetComponent<UnidadeEmBatalha>();
            TokenAnimator animador = novoHeroi.GetComponent<TokenAnimator>();

            unidade.Configurar(equipeAtual[i], animador, 5);
            unidade.posicaoBase = posicaoSpawn;
            animador.IniciarEfeitoSurgimento();

            heroisAtivos.Add(novoHeroi);
            offset += 1.5f;
        }
    }

    public void LimparEquipe()
    {
        foreach (GameObject heroi in heroisAtivos)
        {
            if (heroi != null) Destroy(heroi);
        }
        heroisAtivos.Clear();
    }
}