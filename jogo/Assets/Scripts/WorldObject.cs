using UnityEngine;

// Marca em qual dimensão o objeto existe: "A", "B" ou "AB" (os dois mundos).
// O GameManager chama Refresh() a cada troca de mundo.
// - Tilemaps de cenário: só aparecem no seu mundo.
// - Objetos (anéis, inimigos, molas): somem e param de colidir fora do seu mundo.
// - Tilemaps de colisão não precisam deste script: o jogador escolhe a camada (MundoA / MundoB).
public class WorldObject : MonoBehaviour
{
    public string world = "AB";

    void Start()
    {
        Refresh();
    }

    public bool IsActive()
    {
        return world == "AB" || world == GameManager.world;
    }

    public void Refresh()
    {
        bool active = IsActive();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) r.enabled = active;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true))
            if (c.isTrigger) c.enabled = active;
    }
}
