using UnityEngine;

// Monitor de item: "rings" dá 10 anéis; "shield" dá escudo (absorve 1 golpe, não acumula).
// Abre com pulo, rolamento ou investida. Componentes: Box Collider 2D (Is Trigger).
public class ItemMonitor : MonoBehaviour
{
    public string item = "rings";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerControls player = other.GetComponent<PlayerControls>();
        if (!player.IsAttacking()) return;
        GameManager.PlaySound("monitor");
        if (item == "shield") GameManager.shield = true;
        else GameManager.AddRings(GameManager.ringMonitorAmount);
        player.Bounce();
        Destroy(gameObject);
    }
}
