using UnityEngine;

// Crabmeat (inimigo terrestre do GDD): anda de um lado para o outro entre dois pontos e, a cada
// virada, para e atira duas bolas em arco (uma para cada lado).
public class Crabmeat : Enemy
{
    public float speed = 1.2f;                  // unidades por segundo
    public float patrolDistance = 3f;           // anda até essa distância do ponto inicial, para cada lado
    public float stopTime = 1f;                 // tempo parado atirando

    private float startX;
    private float direction = -1f;
    private float stopTimer;
    private bool shot;

    protected override void Start()
    {
        base.Start();
        startX = transform.position.x;
    }

    void Update()
    {
        if (stopTimer > 0f)
        {
            // parado: atira na metade da parada
            stopTimer -= Time.deltaTime * Rate();
            Animate(attackFrames);
            if (!shot && stopTimer < stopTime * 0.5f)
            {
                Shoot(new Vector2(-2.5f, 6f));
                Shoot(new Vector2(2.5f, 6f));
                shot = true;
            }
            return;
        }
        transform.position += new Vector3(direction * speed * Rate() * Time.deltaTime, 0f, 0f);
        Animate(frames);
        if (Mathf.Abs(transform.position.x - startX) >= patrolDistance && Mathf.Sign(transform.position.x - startX) == direction)
        {
            direction = -direction;             // chegou na ponta: vira e para para atirar
            stopTimer = stopTime;
            shot = false;
        }
    }
}
