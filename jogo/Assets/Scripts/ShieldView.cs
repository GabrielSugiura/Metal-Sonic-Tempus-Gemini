using UnityEngine;

// Escudo: a bolha animada em volta do Metal Sonic aparece só enquanto ele tem o escudo (GameManager.shield).
// Componentes: SpriteRenderer + DecorAnim (os quadros da bolha), num objeto filho do Metal Sonic.
public class ShieldView : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        spriteRenderer.enabled = GameManager.shield;
    }
}
