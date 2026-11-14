using UnityEngine;

// Acelerador de pista: empurra o jogador a 12 px/quadro na direção indicada.
// Componentes: Box Collider 2D (Is Trigger).
public class Booster : MonoBehaviour
{
    public float directionX = 1f;               // 1 = direita, -1 = esquerda
    public float speed = 12f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerControls>().Boost(directionX, speed);
    }
}
