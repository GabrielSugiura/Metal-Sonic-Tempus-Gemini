using UnityEngine;

// Base dos inimigos (Crabmeat e Buzzer): encostar no Metal Sonic.
//  - Se ele estiver atacando (pulo, rolando, spindash ou investida), o inimigo é destruído e ele quica.
//  - Senão, o Metal Sonic se machuca (perde os anéis; sem anéis, morre). O teleporte não causa dano.
// No Bad Future (Mundo B) os inimigos ficam mais rápidos e avermelhados, como pede o GDD.
// Componentes: SpriteRenderer + Box Collider 2D (Is Trigger) + WorldObject.
public class Enemy : MonoBehaviour
{
    public Sprite[] frames;                     // quadros da animação normal
    public Sprite[] attackFrames;               // quadros atirando
    public GameObject shotPrefab;               // tiro (EnemyShot)
    public float frameTime = 0.12f;
    public int points = 100;

    protected SpriteRenderer spriteRenderer;
    private float frameTimer;
    private int frame;

    protected virtual void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 1 no Presente, mais rápido no Bad Future
    protected float Rate()
    {
        return GameManager.world == "B" ? 1.6f : 1f;
    }

    // troca de quadro e cor do mundo
    protected void Animate(Sprite[] list)
    {
        if (list == null || list.Length == 0) return;
        frameTimer += Time.deltaTime * Rate();
        if (frameTimer >= frameTime)
        {
            frameTimer = 0f;
            frame = (frame + 1) % list.Length;
        }
        spriteRenderer.sprite = list[frame % list.Length];
        spriteRenderer.color = GameManager.world == "B" ? new Color(1f, 0.55f, 0.45f) : Color.white;
    }

    protected void Shoot(Vector2 velocity)
    {
        // inimigo do outro mundo não atira; o tiro existe só no mundo de quem atirou
        WorldObject myWorld = GetComponent<WorldObject>();
        if (shotPrefab == null || !myWorld.IsActive()) return;
        GameObject shot = Instantiate(shotPrefab, transform.position, Quaternion.identity);
        shot.GetComponent<EnemyShot>().velocity = velocity;    // o tiro tem o mesmo alcance nos dois mundos
        shot.GetComponent<WorldObject>().world = myWorld.world;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerControls player = other.GetComponent<PlayerControls>();
        if (player.IsAttacking())
        {
            GameManager.score += points;
            GameManager.PlaySound("explosion");
            player.Bounce();
            Destroy(gameObject);
        }
        else player.Hurt(transform.position.x);
    }
}
