using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform alvo;

    [Header("Posição")]
    public float distancia = 6f;
    public float altura = 3f;

    [Header("Câmera")]
    public float sensibilidade = 0.2f;
    public float suavidade = 10f;

    private float rotacaoY;

    void Start()
    {
        if (alvo == null)
            return;

        // Descobre a rotação inicial da câmera
        Vector3 direcao = transform.position - alvo.position;

        rotacaoY = Mathf.Atan2(direcao.x, direcao.z) * Mathf.Rad2Deg;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (alvo == null)
            return;

        GirarCamera();
        SeguirPlayer();
    }

    void GirarCamera()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouse = Mouse.current.delta.ReadValue();

        // SOMENTE rotação horizontal
        rotacaoY += mouse.x * sensibilidade;
    }

    void SeguirPlayer()
    {
        Quaternion rotacao = Quaternion.Euler(0f, rotacaoY, 0f);

        Vector3 offset = rotacao * new Vector3(
            0f,
            altura,
            -distancia
        );

        Vector3 posicaoDesejada = alvo.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            posicaoDesejada,
            suavidade * Time.deltaTime
        );

        // Olha para o personagem
        Vector3 pontoDeOlhar = alvo.position + Vector3.up * 1f;

        Quaternion rotacaoDesejada = Quaternion.LookRotation(
            pontoDeOlhar - transform.position
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotacaoDesejada,
            suavidade * Time.deltaTime
        );
    }
}