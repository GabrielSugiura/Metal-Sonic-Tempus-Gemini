using UnityEngine;

// Buzzer (inimigo aéreo do GDD): voa numa rota curta, de um lado para o outro, e de tempos em tempos
// para e atira na diagonal, para baixo, na direção para onde está virado.
public class Buzzer : Enemy
{
    public float speed = 2f;                    // unidades por segundo
    public float routeDistance = 4f;            // vai até essa distância do ponto inicial, para cada lado
    public float shootInterval = 2.5f;          // segundos entre os tiros
    public float aimTime = 0.6f;                // tempo parado mirando

    private float startX;
    private float direction = -1f;
    private float shootTimer;
    private float aimTimer;

    protected override void Start()
    {
        base.Start();
        startX = transform.position.x;
        shootTimer = shootInterval;
    }

    void Update()
    {
        if (aimTimer > 0f)
        {
            aimTimer -= Time.deltaTime * Rate();
            Animate(attackFrames);
            if (aimTimer <= 0f) Shoot(new Vector2(direction * 4f, -4f));
            return;
        }
        transform.position += new Vector3(direction * speed * Rate() * Time.deltaTime, 0f, 0f);
        spriteRenderer.flipX = direction > 0f;
        Animate(frames);
        if (Mathf.Abs(transform.position.x - startX) >= routeDistance && Mathf.Sign(transform.position.x - startX) == direction)
            direction = -direction;
        shootTimer -= Time.deltaTime * Rate();
        if (shootTimer <= 0f)
        {
            shootTimer = shootInterval;
            aimTimer = aimTime;
        }
    }
}
