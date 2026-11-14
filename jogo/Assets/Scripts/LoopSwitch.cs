using UnityEngine;

// Zona invisível que troca o plano de colisão do jogador nos loops (o "Layer Switch" do Quill).
// Cada metade do loop fica numa camada (LoopPlano1 / LoopPlano2); o jogador só enxerga o seu plano.
// fixedPlane = 1 ou 2 força o plano; fixedPlane = 0 escolhe pela direção (topo do loop).
// Componentes: Box Collider 2D (Is Trigger).
public class LoopSwitch : MonoBehaviour
{
    public int fixedPlane = 1;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        // só vale quando o CENTRO do jogador está dentro da zona (no topo do loop isso importa)
        if (!GetComponent<Collider2D>().OverlapPoint(other.transform.position)) return;
        PlayerControls player = other.GetComponent<PlayerControls>();
        if (fixedPlane != 0) player.loopPlane = fixedPlane;
        else if (player.GetVelocity().x < -30f) player.loopPlane = 2;
        else if (player.GetVelocity().x > 30f) player.loopPlane = 1;
    }
}
