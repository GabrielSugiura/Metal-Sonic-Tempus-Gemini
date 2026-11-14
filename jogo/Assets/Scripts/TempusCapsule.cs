using UnityEngine;

// Cápsula Tempus: devolve tempo até o máximo da fase.
public class TempusCapsule : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.AddTempus(GameManager.capsuleGain);
            GameManager.PlaySound("ring");
            Destroy(gameObject);
        }
    }
}
