using System.Collections;
using UnityEngine;

public class TokenAnimator : MonoBehaviour
{
    public bool isWalking = false;

    [Header("Configurações do Paper Wobble")]
    public float velocidadeBalanco = 12f;
    public float anguloMaximo = 15f;

    private SpriteRenderer sr;
    private Vector3 escalaOriginal;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        escalaOriginal = transform.localScale;
    }

    void Update()
    {
        if (isWalking)
        {
            float anguloZ = Mathf.Sin(Time.time * velocidadeBalanco) * anguloMaximo;
            transform.localRotation = Quaternion.Euler(0, 0, anguloZ);
        }
        else
        {
            transform.localRotation = Quaternion.identity;
        }
    }

    // --- EFEITOS ESPECIAIS ---

    public void IniciarEfeitoSurgimento()
    {
        StartCoroutine(SurgimentoRotina());
    }

    public void PiscarDano()
    {
        StartCoroutine(RotinaPiscarDano());
    }

    public void AnimarAtaque()
    {
        StartCoroutine(RotinaAtaque());
    }

    // --- COROTINAS ---

    private IEnumerator SurgimentoRotina()
    {
        float tempoAnimacao = 0.5f;
        float tempoAtual = 0f;

        Vector3 escalaGigante = escalaOriginal * 2.5f;
        transform.localScale = escalaGigante;

        if (sr != null) sr.color = new Color(1, 1, 1, 0f);

        while (tempoAtual < tempoAnimacao)
        {
            tempoAtual += Time.deltaTime;
            float progresso = tempoAtual / tempoAnimacao;
            float curva = 1f - Mathf.Pow(1f - progresso, 3f);

            transform.localScale = Vector3.Lerp(escalaGigante, escalaOriginal, curva);

            if (sr != null) sr.color = new Color(1, 1, 1, Mathf.Lerp(0f, 1f, curva));

            yield return null;
        }

        transform.localScale = escalaOriginal;
        if (sr != null) sr.color = Color.white;
    }

    private IEnumerator RotinaPiscarDano()
    {
        if (sr != null)
        {
            sr.color = Color.yellow;
            yield return new WaitForSeconds(0.15f);

            // Garante que só volta a branco se não tiver sido destruído entretanto
            if (sr != null) sr.color = Color.white;
        }
    }

    private IEnumerator RotinaAtaque()
    {
        if (sr == null) yield break;

        Vector3 posOriginal = transform.position;

        // Lê a direção visual: se flipX for true (herói), ataca para a esquerda (-1). Senão, para a direita (+1).
        float direcao = sr.flipX ? -1f : 1f;
        Vector3 posAtaque = posOriginal + new Vector3(direcao * 0.5f, 0, 0);

        float tempoIda = 0.08f;  // Salta rápido
        float tempoVolta = 0.15f; // Recupera um pouco mais devagar
        float tempo = 0;

        // Ida
        while (tempo < tempoIda)
        {
            tempo += Time.deltaTime;
            transform.position = Vector3.Lerp(posOriginal, posAtaque, tempo / tempoIda);
            yield return null;
        }

        tempo = 0;
        // Volta
        while (tempo < tempoVolta)
        {
            tempo += Time.deltaTime;
            transform.position = Vector3.Lerp(posAtaque, posOriginal, tempo / tempoVolta);
            yield return null;
        }

        transform.position = posOriginal;
    }
}