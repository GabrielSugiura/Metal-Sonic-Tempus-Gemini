using UnityEngine;

// Placa de fim de fase: vitória e carrega a próxima cena (GameManager.nextScene).
public class Goal : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) GameManager.EndLevel(true, "FASE COMPLETA!");
    }
}
