using UnityEngine;

// Tubo (como os do Sonic CD): quando o Metal Sonic entra, ele é levado pelos pontos do caminho
// (os objetos filhos, em ordem) até a saída, e sai lançado para cima e para o lado.
// Componentes: Box Collider 2D (Is Trigger) na entrada do tubo. Os pontos são os filhos deste objeto.
public class Tube : MonoBehaviour
{
    public float speed = 10f;                   // px por quadro (60 quadros por segundo)
    public float exitSpeed = 7f;                // impulso para cima ao sair (px por quadro)
    public float exitSide = 1f;                 // 1 = sai para a direita, -1 = para a esquerda
    public float exitHorizontal = 3f;           // velocidade para o lado ao sair (px por quadro)

    private PlayerControls player;
    private Rigidbody2D playerBody;
    private int next;
    private float cooldown;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (player != null || cooldown > 0f || !other.CompareTag("Player")) return;
        player = other.GetComponent<PlayerControls>();
        playerBody = other.GetComponent<Rigidbody2D>();
        player.onRail = true;
        next = 0;
    }

    void FixedUpdate()
    {
        cooldown -= Time.fixedDeltaTime;
        if (player == null) return;
        Vector2 target = transform.GetChild(next).position;
        Vector2 newPos = Vector2.MoveTowards(playerBody.position, target, speed / 32f);
        playerBody.MovePosition(newPos);
        if (Vector2.Distance(newPos, target) < 0.01f)
        {
            next++;
            if (next >= transform.childCount)
            {
                // fim do tubo: devolve o controle e lança o jogador para fora da saída
                player.onRail = false;
                player.Launch(Vector2.up, exitSpeed);
                player.xSpeed = exitSide * exitHorizontal;
                player = null;
                cooldown = 1f;
            }
        }
    }
}
