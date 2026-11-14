using UnityEngine;

// Decoração viva do cenário: troca os quadros (flores que giram e balançam, como no Palmtree Panic) e/ou
// pulsa o brilho (cristais do Quartz Quadrant, cornetas do Stardust Speedway).
// Componentes: SpriteRenderer.
public class DecorAnim : MonoBehaviour
{
    public Sprite[] frames;                     // quadros da animação (vazio = só o brilho)
    public float frameTime = 0.1f;
    public float pulse = 0f;                    // quanto o brilho varia (0 = nada)
    public float pulseSpeed = 2f;
    public Color glow = Color.white;            // cor no vale do brilho (o pico é a cor normal)
    private SpriteRenderer spriteRenderer;
    private float timer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        timer = Random.Range(0f, 10f);          // cada uma num ponto diferente da animação
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[(int)(timer / frameTime) % frames.Length];
        if (pulse > 0f)
        {
            float k = (Mathf.Sin(timer * pulseSpeed) + 1f) / 2f * pulse;
            spriteRenderer.color = Color.Lerp(Color.white, glow, k);
        }
    }
}
