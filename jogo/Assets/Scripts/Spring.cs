using UnityEngine;

// Mola: amarela lança a 10 px/quadro, vermelha a 16. Para cima ou para os lados.
// Componentes: Box Collider 2D (Is Trigger).
public class Spring : MonoBehaviour
{
    public Vector2 direction = Vector2.up;      // (0,1) para cima, (1,0) direita, (-1,0) esquerda
    public bool red = false;
    public Transform head;                      // parte que "amassa" ao disparar
    public SpriteRenderer look;                 // desenho da mola (sprite do Sonic CD)
    public Sprite idle, launched;               // parada e esticada (logo depois de lançar)

    private float cooldown = 0f;
    private Vector3 headStart;

    void Start()
    {
        if (head != null) headStart = head.localPosition;
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
        if (head != null)
            head.localPosition = cooldown > 0.05f ? headStart - (Vector3)direction * 0.15f : headStart;
        if (look != null && idle != null) look.sprite = cooldown > 0.05f && launched != null ? launched : idle;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (cooldown > 0f || !other.CompareTag("Player")) return;
        PlayerControls player = other.GetComponent<PlayerControls>();
        // mola para cima só dispara se o jogador estiver no chão ou caindo sobre ela
        if (direction.y > 0.5f && player.ySpeed > 1f && !player.grounded) return;
        player.Launch(direction, red ? 16f : 10f);
        cooldown = 0.25f;
    }
}
