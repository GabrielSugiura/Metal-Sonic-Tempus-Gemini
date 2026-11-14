using UnityEngine;

// Espinhos (perigo do GDD): encostar machuca o Metal Sonic (perde os anéis; sem anéis, morre),
// mesmo pulando ou rolando. O escudo absorve, como qualquer dano.
// Componentes: SpriteRenderer + Box Collider 2D (Is Trigger) + WorldObject.
public class Spikes : MonoBehaviour
{
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<PlayerControls>().Hurt(transform.position.x);
    }
}
