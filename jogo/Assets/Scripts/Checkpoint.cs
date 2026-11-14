using UnityEngine;

// Ponto de controle (o poste do Sonic CD): depois de passar por ele, se o Metal Sonic morrer a fase recomeça
// daqui (o GameManager guarda a posição). Ao ser tocado, a bola gira em volta do poste e fica vermelha.
// Componentes: Box Collider 2D (Is Trigger), no chão; o desenho do poste é um objeto filho (SpriteRenderer).
public class Checkpoint : MonoBehaviour
{
    public Sprite[] spinFrames;                 // a bola girando (sprites do Sonic CD)
    public Sprite doneSprite;                   // a bola vermelha: já ativado
    public float frameTime = 0.04f;
    public Color activeColor = new Color(1f, 0.85f, 0.2f);   // sem os sprites, só pinta o poste
    private bool active = false;
    private float timer = -1f;
    private SpriteRenderer look;

    void Start()
    {
        look = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (timer < 0f || spinFrames == null || spinFrames.Length == 0) return;
        timer += Time.deltaTime;
        int frame = (int)(timer / frameTime);
        if (frame < spinFrames.Length) look.sprite = spinFrames[frame];
        else
        {
            look.sprite = doneSprite != null ? doneSprite : spinFrames[spinFrames.Length - 1];
            timer = -1f;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (active || !other.CompareTag("Player")) return;
        active = true;
        GameManager.hasCheckpoint = true;
        GameManager.checkpoint = transform.position + Vector3.up * 0.65f;
        if (spinFrames != null && spinFrames.Length > 0) timer = 0f;
        else look.color = activeColor;
        GameManager.PlaySound("ring");
    }
}
