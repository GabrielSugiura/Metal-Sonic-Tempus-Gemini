using UnityEngine;

// Anel que quica depois do dano. Usa o Rigidbody 2D da engine (Dynamic) e some depois de alguns segundos.
public class LostRing : MonoBehaviour
{
    public float lifeTime = 4f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 1.5f) GetComponent<SpriteRenderer>().enabled = Mathf.FloorToInt(timer * 12f) % 2 == 0;
        if (timer > lifeTime) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (timer > 0.6f && other.CompareTag("Player"))
        {
            GameManager.AddRings(1);
            Destroy(gameObject);
        }
    }
}
