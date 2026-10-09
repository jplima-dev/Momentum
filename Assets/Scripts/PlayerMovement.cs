using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimentação")]
    public float velocidadeNormal = 6f;
    public float velocidadeCorrida = 10f;
    public float aceleracao = 18f;
    public float desaceleracaoDerrapagem = 20f;
    public float desaceleracaoCorrida = 45f;
    public float tempoParaCorrer = 5f;

    [Header("Inclinação da Rampa")]
    public float distanciaDeteccaoRampa = 1.5f;
    public float alturaOrigemRaycast = 0.3f;
    public float suavidadeRampa = 8f;
    public float limiteInclinacaoRampa = 60f;

    [Header("Pulo")]
    public float forcaPulo = 8f;
    public float gravidade = -25f;
    public float multiplicadorMomentumPulo = 0f;

    [Header("Momentum")]
    public float momentumMaximo = 30f;
    public float ganhoMomentumDerrapagem = 0.35f;
    public float ganhoMomentumQueda = 0.2f;
    public float momentumMinimoDash = 5f;
    public float momentumMaximoDash = 15f;

    [Header("Dash")]
    public float multiplicadorImpulso = 1.5f;
    public float desaceleracaoImpulso = 30f;

    [Header("Inclinação Lateral Visual")]
    public float inclinacaoLateral = 20f;
    public float velocidadeInclinacao = 180f;
    public float velocidadeRetorno = 250f;

    [Header("VFX")]
    public ParticleSystem vfxPulo;

    [Header("Referências")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Animator animator;

    private Vector3 velocidadeHorizontal;
    private Vector3 impulsoMomentum;

    private float velocidadeVertical;
    private float momentum;
    private float tempoAndando;

    private bool estaEmImpulso;
    private bool segundoPuloUsado;
    private bool correndo;
    private bool correrAposDash;

    // =========================
    // ROTAÇÃO PRINCIPAL
    // =========================

    private float rotacaoY;

    // =========================
    // RAMPA
    // =========================

    private Quaternion rotacaoRampaAtual;

    // =========================
    // INCLINAÇÃO VISUAL
    // =========================

    private float inclinacaoZ;
    private float inclinacaoAlvoZ;

    // =========================
    // PROTEÇÃO
    // =========================

    private Vector3 posicaoProtegida;
    private Quaternion rotacaoProtegida;

    // =========================
    // PARTES VISUAIS
    // =========================

    private readonly List<Transform> partesVisuais =
        new List<Transform>();

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (controller == null)
        {
            Debug.LogError(
                "PlayerMovement precisa estar no mesmo objeto que o CharacterController."
            );
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        ConfigurarPartesVisuais();

        if (vfxPulo == null)
        {
            vfxPulo =
                GetComponentInChildren<ParticleSystem>(true);
        }

        if (vfxPulo != null)
        {
            vfxPulo.gameObject.SetActive(true);

            vfxPulo.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        // Rotação Y inicial
        rotacaoY = transform.eulerAngles.y;

        // Começa sem inclinação da rampa
        rotacaoRampaAtual =
            Quaternion.Euler(
                0f,
                rotacaoY,
                0f
            );

        // Inclinação lateral começa zerada
        inclinacaoZ = 0f;
        inclinacaoAlvoZ = 0f;

        posicaoProtegida = transform.position;
        rotacaoProtegida = transform.rotation;
    }

    void Update()
    {
        if (controller.isGrounded)
        {
            if (velocidadeVertical < 0f)
            {
                velocidadeVertical = -2f;
            }

            segundoPuloUsado = false;
        }

        ProcessarMovimento();
        ProcessarPulo();
        ProcessarDash();
        AtualizarImpulso();
        AplicarGravidade();

        Vector3 movimento =
            velocidadeHorizontal +
            impulsoMomentum +
            Vector3.up * velocidadeVertical;

        CollisionFlags colisao =
            controller.Move(
                movimento * Time.deltaTime
            );

        if (estaEmImpulso &&
            (colisao & CollisionFlags.Sides) != 0)
        {
            impulsoMomentum = Vector3.zero;
            estaEmImpulso = false;
        }

        // Atualiza o alinhamento da rampa
        AtualizarAlinhamentoRampa();

        // Proteção
        posicaoProtegida = transform.position;
        rotacaoProtegida = transform.rotation;
    }

    void LateUpdate()
    {
        // Protege o Personagem contra alterações externas
        transform.position = posicaoProtegida;
        transform.rotation = rotacaoProtegida;

        // Inclinação lateral continua sendo
        // apenas visual.
        AtualizarInclinacaoVisual();
    }

    void ConfigurarPartesVisuais()
    {
        partesVisuais.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform filho =
                transform.GetChild(i);

            if (filho.GetComponent<ParticleSystem>() != null)
            {
                continue;
            }

            if (filho.GetComponent<CharacterController>() != null)
            {
                continue;
            }

            partesVisuais.Add(filho);
        }
    }

    void ProcessarMovimento()
    {
        if (Keyboard.current == null ||
            cameraTransform == null)
        {
            return;
        }

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed)
            x = -1f;

        if (Keyboard.current.dKey.isPressed)
            x = 1f;

        if (Keyboard.current.wKey.isPressed)
            z = 1f;

        if (Keyboard.current.sKey.isPressed)
            z = -1f;

        Vector3 frente =
            cameraTransform.forward;

        Vector3 direita =
            cameraTransform.right;

        frente.y = 0f;
        direita.y = 0f;

        frente.Normalize();
        direita.Normalize();

        Vector3 direcao =
            frente * z +
            direita * x;

        if (direcao.sqrMagnitude > 1f)
        {
            direcao.Normalize();
        }

        bool estaMovendo =
            direcao.sqrMagnitude > 0.001f;

        // =========================
        // CORRIDA AUTOMÁTICA
        // =========================

        if (estaMovendo &&
            controller.isGrounded &&
            !estaEmImpulso)
        {
            if (!correrAposDash)
            {
                tempoAndando += Time.deltaTime;

                if (tempoAndando >= tempoParaCorrer)
                {
                    correndo = true;
                }
            }
            else
            {
                correndo = true;
                tempoAndando = tempoParaCorrer;
            }
        }
        else if (!estaMovendo)
        {
            tempoAndando = 0f;
            correndo = false;
            correrAposDash = false;
        }

        float velocidadeDesejada =
            correndo
                ? velocidadeCorrida
                : velocidadeNormal;

        float velocidadeAntes =
            velocidadeHorizontal.magnitude;

        // =========================
        // MOVIMENTO
        // =========================

        if (estaMovendo && !estaEmImpulso)
        {
            velocidadeHorizontal =
                Vector3.MoveTowards(
                    velocidadeHorizontal,
                    direcao * velocidadeDesejada,
                    aceleracao *
                    Time.deltaTime
                );

            // Apenas atualiza o Y.
            // A inclinação da rampa será adicionada
            // separadamente.
            if (controller.isGrounded)
            {
                float anguloDesejado =
                    Mathf.Atan2(
                        direcao.x,
                        direcao.z
                    ) * Mathf.Rad2Deg;

                rotacaoY =
                    Mathf.LerpAngle(
                        rotacaoY,
                        anguloDesejado,
                        12f *
                        Time.deltaTime
                    );
            }
        }
        else if (!estaMovendo &&
                 !estaEmImpulso)
        {
            float desaceleracaoAtual;

            if (velocidadeHorizontal.magnitude >
                velocidadeNormal)
            {
                desaceleracaoAtual =
                    desaceleracaoCorrida;
            }
            else
            {
                desaceleracaoAtual =
                    desaceleracaoDerrapagem;
            }

            velocidadeHorizontal =
                Vector3.MoveTowards(
                    velocidadeHorizontal,
                    Vector3.zero,
                    desaceleracaoAtual *
                    Time.deltaTime
                );
        }

        // =========================
        // MOMENTUM DA DERRAPAGEM
        // =========================

        float velocidadeDepois =
            velocidadeHorizontal.magnitude;

        float velocidadePerdida =
            velocidadeAntes -
            velocidadeDepois;

        if (!estaEmImpulso &&
            !estaMovendo &&
            velocidadePerdida > 0f)
        {
            momentum +=
                velocidadePerdida *
                ganhoMomentumDerrapagem;

            momentum =
                Mathf.Clamp(
                    momentum,
                    0f,
                    momentumMaximo
                );
        }

        // =========================
        // MOMENTUM DA QUEDA
        // =========================

        if (!estaEmImpulso &&
            !controller.isGrounded &&
            velocidadeVertical < 0f)
        {
            momentum +=
                Mathf.Abs(
                    velocidadeVertical
                ) *
                ganhoMomentumQueda *
                Time.deltaTime;

            momentum =
                Mathf.Clamp(
                    momentum,
                    0f,
                    momentumMaximo
                );
        }

        // =========================
        // ANIMAÇÕES
        // =========================

        if (animator != null)
        {
            bool andando =
                estaMovendo &&
                controller.isGrounded &&
                !estaEmImpulso;

            animator.SetBool(
                "Andando",
                andando
            );

            animator.SetBool(
                "Correndo",
                correndo && andando
            );
        }

        // =========================
        // INCLINAÇÃO LATERAL
        // =========================

        if (controller.isGrounded)
        {
            if (estaMovendo &&
                !estaEmImpulso)
            {
                inclinacaoAlvoZ =
                    -x *
                    inclinacaoLateral;
            }
            else
            {
                inclinacaoAlvoZ = 0f;
            }
        }
    }

    void AtualizarAlinhamentoRampa()
    {
        // No ar não muda a rotação.
        if (!controller.isGrounded)
            return;

        RaycastHit hit;
        bool encontrouChao =
            EncontrarChao(
                out hit
            );

        Quaternion rotacaoHorizontal =
            Quaternion.Euler(
                0f,
                rotacaoY,
                0f
            );

        Quaternion alvo;

        if (!encontrouChao)
        {
            // Sem chão detectado:
            // volta para a posição reta.
            alvo =
                rotacaoHorizontal;
        }
        else
        {
            float anguloRampa =
                Vector3.Angle(
                    hit.normal,
                    Vector3.up
                );

            if (anguloRampa >
                limiteInclinacaoRampa)
            {
                alvo =
                    rotacaoHorizontal;
            }
            else
            {
                // Direção do personagem projetada
                // sobre a superfície da rampa.
                Vector3 frente =
                    rotacaoHorizontal *
                    Vector3.forward;

                Vector3 frenteNaRampa =
                    Vector3.ProjectOnPlane(
                        frente,
                        hit.normal
                    );

                if (frenteNaRampa.sqrMagnitude <
                    0.001f)
                {
                    frenteNaRampa =
                        frente;
                }

                frenteNaRampa.Normalize();

                // Mantém o personagem seguindo
                // a superfície da rampa.
                alvo =
                    Quaternion.LookRotation(
                        frenteNaRampa,
                        hit.normal
                    );
            }
        }

        rotacaoRampaAtual =
            Quaternion.Slerp(
                transform.rotation,
                alvo,
                suavidadeRampa *
                Time.deltaTime
            );

        transform.rotation =
            rotacaoRampaAtual;
    }

    bool EncontrarChao(out RaycastHit melhorHit)
    {
        Vector3 origem =
            transform.position +
            Vector3.up *
            alturaOrigemRaycast;

        RaycastHit[] hits =
            Physics.RaycastAll(
                origem,
                Vector3.down,
                distanciaDeteccaoRampa,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

        float menorDistancia =
            float.MaxValue;

        bool encontrou = false;

        melhorHit = new RaycastHit();

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            RaycastHit hit =
                hits[i];

            if (hit.collider == null)
                continue;

            Transform objeto =
                hit.collider.transform;

            // Ignora o próprio Personagem
            if (objeto == transform)
                continue;

            // Ignora qualquer coisa filha dele
            if (objeto.IsChildOf(transform))
                continue;

            if (hit.distance <
                menorDistancia)
            {
                menorDistancia =
                    hit.distance;

                melhorHit =
                    hit;

                encontrou = true;
            }
        }

        return encontrou;
    }

    void AtualizarInclinacaoVisual()
    {
        if (partesVisuais.Count == 0)
            return;

        // No ar mantém a última inclinação.
        if (!controller.isGrounded)
            return;

        float velocidade;

        if (Mathf.Abs(
                inclinacaoAlvoZ
            ) > 0.01f)
        {
            velocidade =
                velocidadeInclinacao;
        }
        else
        {
            velocidade =
                velocidadeRetorno;
        }

        inclinacaoZ =
            Mathf.MoveTowards(
                inclinacaoZ,
                inclinacaoAlvoZ,
                velocidade *
                Time.deltaTime
            );

        Quaternion inclinacao =
            Quaternion.Euler(
                0f,
                0f,
                inclinacaoZ
            );

        Vector3 centro =
            transform.position;

        Quaternion rotacaoMundo =
            transform.rotation *
            inclinacao *
            Quaternion.Inverse(
                transform.rotation
            );

        for (int i = 0;
             i < partesVisuais.Count;
             i++)
        {
            Transform parte =
                partesVisuais[i];

            if (parte == null)
                continue;

            Vector3 offset =
                parte.position -
                centro;

            Vector3 novoOffset =
                rotacaoMundo *
                offset;

            parte.position =
                centro +
                novoOffset;

            parte.rotation =
                rotacaoMundo *
                parte.rotation;
        }

        if (Mathf.Abs(
                inclinacaoAlvoZ
            ) < 0.01f &&
            Mathf.Abs(inclinacaoZ) < 0.05f)
        {
            inclinacaoZ = 0f;
        }
    }

    void ProcessarPulo()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        if (!controller.isGrounded)
            return;

        velocidadeVertical =
            forcaPulo;

        TocarVfx();

        if (animator != null)
        {
            animator.SetTrigger(
                "Pular"
            );
        }
    }

    void ProcessarDash()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (estaEmImpulso)
            return;

        // =========================
        // NO AR
        // =========================

        if (!controller.isGrounded)
        {
            if (!TemDirecaoNoInput())
            {
                FazerSegundoPulo();
                return;
            }

            FazerDashHorizontal();
            return;
        }

        // =========================
        // NO CHÃO
        // =========================

        FazerDashHorizontal();
    }

    void FazerSegundoPulo()
    {
        if (segundoPuloUsado)
            return;

        if (momentum < momentumMinimoDash)
            return;

        float momentumUsado =
            Mathf.Min(
                momentum,
                momentumMaximoDash
            );

        velocidadeVertical =
            forcaPulo +
            momentumUsado *
            multiplicadorMomentumPulo;

        momentum -= momentumUsado;

        segundoPuloUsado = true;

        TocarVfx();

        if (animator != null)
        {
            animator.SetTrigger(
                "DashCima"
            );
        }
    }

    void FazerDashHorizontal()
    {
        if (momentum < momentumMinimoDash)
            return;

        float momentumAntesDoDash =
            momentum;

        Vector3 direcaoDash =
            PegarDirecaoDash();

        float momentumUsado =
            Mathf.Min(
                momentum,
                momentumMaximoDash
            );

        impulsoMomentum =
            direcaoDash *
            momentumUsado *
            multiplicadorImpulso;

        momentum -= momentumUsado;

        estaEmImpulso = true;

        correrAposDash =
            momentumAntesDoDash > 15f;

        if (correrAposDash)
        {
            correndo = true;
            tempoAndando = tempoParaCorrer;
        }
        else
        {
            correndo = false;
            tempoAndando = 0f;
        }

        inclinacaoAlvoZ = 0f;

        TocarVfx();

        if (animator != null)
        {
            animator.SetBool(
                "Andando",
                false
            );

            animator.SetBool(
                "Correndo",
                false
            );

            animator.SetTrigger(
                "Dash"
            );
        }
    }

    bool TemDirecaoNoInput()
    {
        if (Keyboard.current == null)
            return false;

        return
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed;
    }

    Vector3 PegarDirecaoDash()
    {
        if (Keyboard.current == null ||
            cameraTransform == null)
        {
            return transform.forward;
        }

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed)
            x = -1f;

        if (Keyboard.current.dKey.isPressed)
            x = 1f;

        if (Keyboard.current.wKey.isPressed)
            z = 1f;

        if (Keyboard.current.sKey.isPressed)
            z = -1f;

        Vector3 frente =
            cameraTransform.forward;

        Vector3 direita =
            cameraTransform.right;

        frente.y = 0f;
        direita.y = 0f;

        frente.Normalize();
        direita.Normalize();

        Vector3 direcao =
            frente * z +
            direita * x;

        if (direcao.sqrMagnitude < 0.001f)
        {
            return transform.forward;
        }

        return direcao.normalized;
    }

    void AtualizarImpulso()
    {
        if (!estaEmImpulso)
            return;

        impulsoMomentum =
            Vector3.MoveTowards(
                impulsoMomentum,
                Vector3.zero,
                desaceleracaoImpulso *
                Time.deltaTime
            );

        if (impulsoMomentum.magnitude < 0.05f)
        {
            impulsoMomentum =
                Vector3.zero;

            estaEmImpulso = false;
        }
    }

    void AplicarGravidade()
    {
        if (controller.isGrounded &&
            velocidadeVertical < 0f)
        {
            velocidadeVertical = -2f;
        }

        velocidadeVertical +=
            gravidade *
            Time.deltaTime;
    }

    void TocarVfx()
    {
        if (vfxPulo == null)
            return;

        if (!vfxPulo.gameObject.activeSelf)
        {
            vfxPulo.gameObject.SetActive(true);
        }

        Vector3 posicao =
            transform.position;

        posicao.y =
            controller.bounds.min.y +
            0.02f;

        vfxPulo.transform.position =
            posicao;

        vfxPulo.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        vfxPulo.Clear();
        vfxPulo.Emit(12);
        vfxPulo.Play();
    }
}