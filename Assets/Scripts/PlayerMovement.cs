using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimentação")]
    public float velocidadeNormal = 6f;
    public float velocidadeCorrida = 10f;
    public float aceleracao = 18f;
    public float desaceleracaoDerrapagem = 20f;

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

    [Header("Inclinação")]
    public float inclinacaoLateral = 20f;
    public float suavidadeInclinacao = 10f;
    public float suavidadeRetorno = 25f;

    [Header("VFX")]
    public ParticleSystem vfxPulo;

    [Header("Referências")]
    public Transform cameraTransform;
    public Transform modeloVisual;

    private CharacterController controller;
    private Animator animator;

    private Vector3 velocidadeHorizontal;
    private Vector3 impulsoMomentum;

    private float velocidadeVertical;
    private float momentum;

    private bool estaEmImpulso;
    private bool segundoPuloUsado;

    private Quaternion rotacaoVisualOriginal;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (modeloVisual == null)
        {
            modeloVisual = transform.parent;
        }

        if (modeloVisual != null)
        {
            rotacaoVisualOriginal = modeloVisual.localRotation;
        }

        if (vfxPulo == null)
        {
            vfxPulo = GetComponentInChildren<ParticleSystem>(true);
        }

        if (vfxPulo != null)
        {
            vfxPulo.gameObject.SetActive(true);

            vfxPulo.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
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

        bool correndo =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        float velocidadeDesejada =
            correndo
                ? velocidadeCorrida
                : velocidadeNormal;

        float velocidadeAntes =
            velocidadeHorizontal.magnitude;

        if (estaMovendo && !estaEmImpulso)
        {
            velocidadeHorizontal =
                Vector3.MoveTowards(
                    velocidadeHorizontal,
                    direcao * velocidadeDesejada,
                    aceleracao * Time.deltaTime
                );

            Quaternion rotacaoAlvo =
                Quaternion.LookRotation(direcao);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    rotacaoAlvo,
                    12f * Time.deltaTime
                );
        }
        else if (!estaMovendo && !estaEmImpulso)
        {
            velocidadeHorizontal =
                Vector3.MoveTowards(
                    velocidadeHorizontal,
                    Vector3.zero,
                    desaceleracaoDerrapagem *
                    Time.deltaTime
                );
        }

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

        if (!estaEmImpulso &&
            !controller.isGrounded &&
            velocidadeVertical < 0f)
        {
            momentum +=
                Mathf.Abs(velocidadeVertical) *
                ganhoMomentumQueda *
                Time.deltaTime;

            momentum =
                Mathf.Clamp(
                    momentum,
                    0f,
                    momentumMaximo
                );
        }

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

        if (modeloVisual != null)
        {
            float inclinacaoAlvo = 0f;

            if (estaMovendo)
            {
                inclinacaoAlvo =
                    x *
                    inclinacaoLateral;
            }

            Quaternion rotacaoAlvo =
                rotacaoVisualOriginal *
                Quaternion.Euler(
                    0f,
                    0f,
                    inclinacaoAlvo
                );

            float suavidade =
                inclinacaoAlvo != 0f
                    ? suavidadeInclinacao
                    : suavidadeRetorno;

            modeloVisual.localRotation =
                Quaternion.Slerp(
                    modeloVisual.localRotation,
                    rotacaoAlvo,
                    suavidade *
                    Time.deltaTime
                );
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
            impulsoMomentum = Vector3.zero;
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