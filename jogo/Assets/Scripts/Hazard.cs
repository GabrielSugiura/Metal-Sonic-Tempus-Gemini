using UnityEngine;

// Perigo (espinhos, cristais, eletricidade): machuca ao tocar.
// Abismo: marque "isPit" — mata direto (o escudo não protege).
public class Hazard : MonoBehaviour
{
    public bool isPit = false;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerControls player = other.GetComponent<PlayerControls>();
        if (isPit) player.Die();
        else player.Hurt(transform.position.x);
    }
}
