using UnityEngine;

// Tiro dos inimigos: anda com a velocidade dada (com um pouco de gravidade) e machuca o Metal Sonic
// ao encostar. Some depois de alguns segundos. O escudo absorve, como qualquer dano.
// Componentes: SpriteRenderer + Circle Collider 2D (Is Trigger).
public class EnemyShot : MonoBehaviour
{
    public Vector2 velocity;
    public float gravity = 6f;                  // unidades por segundo²
    public float lifeTime = 1.8f;               // some antes de cair numa rota de baixo

    void Update()
    {
        velocity.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<PlayerControls>().Hurt(transform.position.x);
        Destroy(gameObject);
    }
}
