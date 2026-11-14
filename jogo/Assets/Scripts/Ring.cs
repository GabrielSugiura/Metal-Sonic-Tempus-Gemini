using UnityEngine;

// Anel: protege de dano e recarrega o teleporte.
// Componentes: Circle Collider 2D (Is Trigger) + Animator com a animação do anel.
public class Ring : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.AddRings(1);
            Destroy(gameObject);
        }
    }
}
