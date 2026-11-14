using UnityEngine;

// Câmera estilo Sonic: segue o jogador, adianta na direção da corrida e olha para cima/baixo.
public class CameraControl : MonoBehaviour
{
    public PlayerControls player;
    public float smooth = 9f;
    public float lookAhead = 0.22f;             // quanto adianta (fração da velocidade)
    public float heightOffset = 1.5f;           // a câmera fica um pouco acima do jogador
    public Vector2 minLimit = new Vector2(-1000f, -1000f);
    public Vector2 maxLimit = new Vector2(1000f, 1000f);
    public float targetSize = 0f;               // zoom pedido (a arena do chefe se afasta para caber inteira); 0 = não muda

    private Vector2 offset;

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControls>();
        transform.position = new Vector3(player.transform.position.x, player.transform.position.y + heightOffset, -10f);
    }

    void LateUpdate()
    {
        if (targetSize > 0f)
        {
            Camera cam = GetComponent<Camera>();
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, 1f - Mathf.Exp(-3f * Time.deltaTime));
        }
        Vector2 vel = player.GetVelocity() / player.pixelsPerUnit;
        float look = 0f;
        if (player.lookingUp) look = 3.5f;
        if (player.crouching) look = -3f;
        float k = Mathf.Min(1f, 4f * Time.deltaTime);
        offset.x = Mathf.Lerp(offset.x, vel.x * lookAhead, k);
        offset.y = Mathf.Lerp(offset.y, heightOffset + look, k);

        Vector3 target = player.transform.position + (Vector3)offset;
        Vector3 pos = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        pos.x = Mathf.Clamp(pos.x, minLimit.x, maxLimit.x);
        pos.y = Mathf.Clamp(pos.y, minLimit.y, maxLimit.y);
        pos.z = -10f;
        transform.position = pos;
    }
}
