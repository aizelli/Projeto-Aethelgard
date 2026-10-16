using System.Collections.Generic;
using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
    [Header("Velocidades")]
    public float speedSky = 0.2f;
    public float speedFar = 0.5f;
    public float speedMid = 1.5f;
    public float speedGround = 3.0f;

    public Transform skyContainer;
    public Transform farContainer;
    public Transform midContainer;
    public Transform groundContainer;

    public float chunkWidth = 20f;

    [Tooltip("Limite na direita onde o chão é destruído (Ex: 25)")]
    public float limitRightX = 25f;
    public int initialChunksCount = 3;

    private FaseData faseAtual;

    [HideInInspector]
    public bool isMoving = false;

    private GameObject[] skyClones = new GameObject[2];
    private GameObject[] farClones = new GameObject[2];
    private GameObject[] midClones = new GameObject[2];
    private List<GameObject> activeChunks = new List<GameObject>();

    public void SetupParallax(FaseData fase)
    {
        faseAtual = fase;

        // O Clone 1 agora nasce na ESQUERDA do Clone 0 (posição negativa)
        SetupBackgroundLayer(fase.prefabCeu, fase.larguraCeu, skyContainer, skyClones);
        SetupBackgroundLayer(fase.prefabFundo, fase.larguraFundo, farContainer, farClones);
        SetupBackgroundLayer(fase.prefabMeio, fase.larguraMeio, midContainer, midClones);

        foreach (var chunk in activeChunks) { Destroy(chunk); }
        activeChunks.Clear();

        for (int i = 0; i < initialChunksCount; i++)
        {
            // Os chunks iniciais são gerados para trás (esquerda) para preencher a tela
            SpawnChunk(-i * chunkWidth);
        }
    }

    private void SetupBackgroundLayer(GameObject prefab, float width, Transform container, GameObject[] cloneArray)
    {
        for (int i = 0; i < cloneArray.Length; i++) { if (cloneArray[i] != null) Destroy(cloneArray[i]); }
        if (prefab == null) return;

        cloneArray[0] = Instantiate(prefab, container.position, Quaternion.identity, container);
        cloneArray[1] = Instantiate(prefab, container.position + new Vector3(-width, 0, 0), Quaternion.identity, container);
    }

    void Update()
    {
        if (!isMoving) return;
        MoveChunks();
        MoveBackgroundLayer(skyClones, faseAtual.larguraCeu, speedSky);
        MoveBackgroundLayer(farClones, faseAtual.larguraFundo, speedFar);
        MoveBackgroundLayer(midClones, faseAtual.larguraMeio, speedMid);
    }

    private void MoveBackgroundLayer(GameObject[] clones, float width, float speed)
    {
        if (clones[0] == null || clones[1] == null) return;

        for (int i = 0; i < clones.Length; i++)
        {
            // Movendo para a DIREITA
            clones[i].transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);

            // Se o clone sair da tela pela DIREITA, ele pula para trás (esquerda)
            if (clones[i].transform.localPosition.x >= width)
            {
                int outroIndex = (i == 0) ? 1 : 0;
                float posicaoOutroX = clones[outroIndex].transform.localPosition.x;
                clones[i].transform.localPosition = new Vector3(posicaoOutroX - width, clones[i].transform.localPosition.y, clones[i].transform.localPosition.z);
            }
        }
    }

    private void MoveChunks()
    {
        for (int i = 0; i < activeChunks.Count; i++)
        {
            // Movendo o chão para a DIREITA
            activeChunks[i].transform.Translate(Vector3.right * speedGround * Time.deltaTime, Space.World);
        }

        if (activeChunks.Count == 0) return;
        GameObject primeiroChunk = activeChunks[0];

        // Se o chunk mais à direita sair da tela
        if (primeiroChunk.transform.position.x >= limitRightX)
        {
            float ultimoX = activeChunks[activeChunks.Count - 1].transform.position.x;
            activeChunks.RemoveAt(0);
            Destroy(primeiroChunk);

            // O novo chunk é criado na cauda (mais à esquerda)
            SpawnChunk(ultimoX - chunkWidth);
        }
    }

    private void SpawnChunk(float positionX)
    {
        if (faseAtual == null || faseAtual.poolDeChunks.Length == 0) return;
        int randomIndex = Random.Range(0, faseAtual.poolDeChunks.Length);
        GameObject chunkPrefab = faseAtual.poolDeChunks[randomIndex];
        GameObject novoChunk = Instantiate(chunkPrefab, new Vector3(positionX, groundContainer.position.y, groundContainer.position.z), Quaternion.identity, groundContainer);
        activeChunks.Add(novoChunk);
    }
}