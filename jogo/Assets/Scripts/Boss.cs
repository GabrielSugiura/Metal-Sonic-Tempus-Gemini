using UnityEngine;

// Mecha Sonic: o chefe do fim do Stardust Speedway (GDD). Também alterna entre as dimensões: só machuca e
// só leva dano quando está no mesmo mundo que o Metal Sonic (no outro mundo ele aparece apagado).
// Padrão: flutua -> ataca (leque de tiros, investida pelo chão ou o raio laser) -> depois da investida fica tonto
// (vulnerável: acerte com pulo, rolando ou investida) -> ao levar dano, solta anéis e foge para o outro mundo.
// 6 de vida. Se ficar 3 s no outro mundo, ele volta para o mundo do jogador.
// Raio laser: ele vai para uma ponta da arena, mira na altura do Metal Sonic (1,2 s, a faixa pisca) e dispara um
// raio contínuo de parede a parede que sobe e desce atrás dele: pular não adianta, só trocando de dimensão (X).
// A arena (BossArena.cs) chama Begin() quando o jogador entra.
// Componentes: SpriteRenderer + Box Collider 2D (Is Trigger).
public class Boss : MonoBehaviour
{
    public Sprite[] floatFrames;                // flutuando / atirando
    public Sprite[] lungeFrames;                // investida (bola)
    public Sprite[] stunFrames;                 // tonto
    public GameObject shotPrefab;               // tiro (EnemyShot)
    public Sprite[] floatAura, lungeAura, stunAura; // silhuetas brancas dos mesmos quadros
    public SpriteRenderer aura;                 // contorno atrás do chefe: verde no Presente, laranja no Bad Future
    public bool facesLeft = false;              // o desenho original olha para a esquerda
    public int maxHp = 6;
    public float stunTime = 2.8f;
    public float lungeSpeed = 19f;              // unidades por segundo
    public float shotSpeed = 8.5f;
    public SpriteRenderer beam;                 // o raio laser (fica na arena; o Boss posiciona e estica)
    public float laserCharge = 1.2f, laserTime = 2.6f;
    public float beamHeight = 2.2f;             // grossura do raio (unidades)
    public float beamFollow = 4f;               // o raio sobe e desce atrás do Metal Sonic (unidades por segundo)

    public string state = "intro";              // intro, float, shoot, tele, lunge, stun, hit, swap, laserAim, laser, dead
    public string bossWorld = "A";
    public int hp;

    private float arenaX0, arenaX1, floorY;
    private float stateTimer, anim, offWorldTimer, hitFlash;
    private float beamY;                        // altura do meio do raio
    private int volleys, facing = -1, lungeDir = 1, laserSide = 1;
    private string lastAttack = "";
    private bool laserTold = false;
    private string targetWorld = "A";
    private SpriteRenderer spriteRenderer;
    private PlayerControls player;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hp = maxHp;
    }

    public void Begin(float x0, float x1, float floor)
    {
        arenaX0 = x0;
        arenaX1 = x1;
        floorY = floor;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControls>();
        if (beam == null && transform.parent != null)             // o raio fica na arena, ao lado do chefe
        {
            Transform t = transform.parent.Find("Raio Laser");
            if (t != null) beam = t.GetComponent<SpriteRenderer>();
        }
        bossWorld = GameManager.world;
        transform.position = new Vector3((x0 + x1) / 2f, floor + 8f, 0f);
        GameManager.bossActive = true;
        GameManager.bossHp = hp;
        GameManager.bossHpMax = maxHp;
        GameManager.PlayBossMusic();
        Enter("float");
    }

    bool Present()
    {
        return bossWorld == GameManager.world;
    }

    void Enter(string s)
    {
        state = s;
        stateTimer = 0f;
    }

    void Update()
    {
        anim += Time.deltaTime;
        hitFlash -= Time.deltaTime;
        if (state == "intro" || state == "dead" || GameManager.paused) { Draw(); return; }
        stateTimer += Time.deltaTime;
        if (player != null && !player.dead && Mathf.Abs(player.transform.position.x - transform.position.x) > 0.25f
            && (state == "float" || state == "shoot" || state == "tele"))
            facing = player.transform.position.x > transform.position.x ? 1 : -1;

        switch (state)
        {
            case "float":
                FloatMove();
                if (Present()) offWorldTimer = 0f;
                else
                {
                    offWorldTimer += Time.deltaTime;
                    if (offWorldTimer > 3f) SwapTo(GameManager.world);
                }
                if (stateTimer > 1.7f && Present()) ChooseAttack();
                break;
            case "shoot":
                FloatMove();
                if (stateTimer > 0.55f && volleys > 0)
                {
                    FireFan();
                    volleys--;
                    stateTimer = 0f;
                }
                else if (volleys == 0 && stateTimer > 0.5f) Enter("float");
                break;
            case "tele":
                // prepara a investida: desce até o chão piscando em vermelho
                MoveTo(new Vector2(transform.position.x, floorY), 8f);
                if (stateTimer > 0.8f)
                {
                    lungeDir = transform.position.x < (arenaX0 + arenaX1) / 2f ? 1 : -1;
                    facing = lungeDir;
                    GameManager.PlaySound("dash");
                    Enter("lunge");
                }
                break;
            case "lunge":
                transform.position += Vector3.right * lungeDir * lungeSpeed * Time.deltaTime;
                if ((lungeDir > 0 && transform.position.x >= arenaX1 - 2.6f) || (lungeDir < 0 && transform.position.x <= arenaX0 + 2.6f))
                {
                    GameManager.PlaySound("land");
                    Enter("stun");
                }
                break;
            case "stun":
                MoveTo(new Vector2(transform.position.x, floorY), 10f);
                if (stateTimer > stunTime) SwapTo(bossWorld == "A" ? "B" : "A");
                break;
            case "hit":
                if (stateTimer > 0.6f) SwapTo(bossWorld == "A" ? "B" : "A");
                break;
            case "laserAim":
                // vai para a ponta da arena, virado para o meio, e mira na altura do Metal Sonic: a faixa do raio pisca
                beamY = Mathf.MoveTowards(beamY, BeamTarget(), beamFollow * 2f * Time.deltaTime);
                MoveTo(new Vector2(laserSide < 0 ? arenaX0 + 1.6f : arenaX1 - 1.6f, beamY), 6f);
                facing = -laserSide;
                if (stateTimer > laserCharge)
                {
                    GameManager.PlaySound("teleport");
                    Enter("laser");
                }
                break;
            case "laser":
                // o raio contínuo vai atrás do Metal Sonic (pular não adianta): machuca se ele estiver no mesmo mundo
                beamY = Mathf.MoveTowards(beamY, BeamTarget(), beamFollow * Time.deltaTime);
                MoveTo(new Vector2(transform.position.x, beamY), 6f);
                if (Present() && player != null && !player.dead && InBeam(player.transform.position)) player.Hurt(transform.position.x);
                if (stateTimer > laserTime)
                {
                    lastAttack = "laser";
                    Enter("float");
                }
                break;
            case "swap":
                if (stateTimer > 0.35f)
                {
                    bossWorld = targetWorld;
                    transform.position = new Vector3(Mathf.Clamp(transform.position.x, arenaX0 + 2.5f, arenaX1 - 2.5f), floorY + 3.5f, 0f);
                    GameManager.PlaySound("teleport");
                    Enter("float");
                }
                break;
        }
        Draw();
    }

    void MoveTo(Vector2 target, float rate)
    {
        transform.position = Vector2.Lerp(transform.position, target, Mathf.Min(1f, rate * Time.deltaTime));
    }

    // vai e volta pela arena, balançando no ar
    void FloatMove()
    {
        float mid = (arenaX0 + arenaX1) / 2f;
        float span = (arenaX1 - arenaX0) / 2f - 3.4f;
        MoveTo(new Vector2(mid + Mathf.Sin(anim * 0.9f) * span, floorY + 3.5f + Mathf.Sin(anim * 2.2f) * 0.7f), 2.2f);
    }

    void ChooseAttack()
    {
        float r = Random.value;
        // o laser nunca duas vezes seguidas, e fica mais frequente quando ele está mais fraco
        if (lastAttack != "laser" && r < (hp <= 4 ? 0.4f : 0.3f)) { Laser(); return; }
        lastAttack = "";
        if (r < 0.65f)
        {
            volleys = hp > 3 ? 2 : 3;
            Enter("shoot");
        }
        else Enter("tele");
    }

    // começa o raio laser (vai para a ponta mais perto da arena)
    public void Laser()
    {
        laserSide = transform.position.x < (arenaX0 + arenaX1) / 2f ? -1 : 1;
        beamY = BeamTarget();
        GameManager.PlaySound("alarm");
        if (!laserTold)
        {
            GameManager.ShowMessage("RAIO LASER! Só dá para escapar trocando de dimensão (X)");
            laserTold = true;
        }
        Enter("laserAim");
    }

    // a faixa do raio: da ponta onde ele está até a parede do outro lado, na altura beamY
    Rect BeamRect()
    {
        float x = transform.position.x;
        float x0 = laserSide < 0 ? x : arenaX0, x1 = laserSide < 0 ? arenaX1 : x;
        return new Rect(x0, beamY - beamHeight / 2f, x1 - x0, beamHeight);
    }

    // a altura que o raio quer: a do Metal Sonic (sem descer abaixo do chão nem subir demais)
    float BeamTarget()
    {
        float y = player != null ? player.transform.position.y : floorY + 1f;
        return Mathf.Clamp(y, floorY + 0.8f, floorY + 9f);
    }

    bool InBeam(Vector2 p)
    {
        return BeamRect().Contains(p);
    }

    // três tiros em leque na direção do jogador
    void FireFan()
    {
        Vector2 center = (Vector2)transform.position + Vector2.up * 1.2f;
        Vector2 aim = player != null ? ((Vector2)player.transform.position - center).normalized : new Vector2(facing, -0.3f).normalized;
        foreach (float angle in new[] { -20f, 0f, 20f })
        {
            GameObject shot = Instantiate(shotPrefab, center, Quaternion.identity);
            EnemyShot s = shot.GetComponent<EnemyShot>();
            s.velocity = (Vector2)(Quaternion.Euler(0f, 0f, angle) * aim) * shotSpeed;
            s.gravity = 0f;
            s.lifeTime = 3f;
            shot.GetComponent<WorldObject>().world = bossWorld;
        }
        GameManager.PlaySound("shot");
    }

    void SwapTo(string w)
    {
        targetWorld = w;
        Enter("swap");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !Present()) return;
        if (state == "intro" || state == "swap" || state == "dead" || state == "hit") return;
        PlayerControls p = other.GetComponent<PlayerControls>();
        if (state == "stun" && p.IsAttacking()) Damage(p);
        else if (state != "stun")
        {
            if (p.IsAttacking()) p.Bounce();
            p.Hurt(transform.position.x);
        }
    }

    void Damage(PlayerControls p)
    {
        hp--;
        GameManager.bossHp = hp;
        hitFlash = 0.5f;
        GameManager.score += 500;
        GameManager.PlaySound("explosion");
        p.Bounce();
        GameManager.AddRings(3);                // o chefe solta anéis para o jogador se reabastecer
        if (hp <= 0)
        {
            Enter("dead");
            GameManager.bossActive = false;
            GameManager.score += 5000;
            spriteRenderer.enabled = false;
            GetComponent<Collider2D>().enabled = false;
            Invoke("Win", 2.2f);
        }
        else Enter("hit");
    }

    void Win()
    {
        GameManager.EndLevel(true, "Mecha Sonic derrotado!");
    }

    // o raio: na mira, a faixa inteira pisca fraquinha; disparando, a faixa forte tremendo
    // (no outro mundo ele aparece apagado: lá o raio não pega)
    void DrawBeam()
    {
        if (beam == null) return;
        bool aim = state == "laserAim", fire = state == "laser";
        beam.enabled = aim || fire;
        if (!beam.enabled) return;
        Rect r = BeamRect();
        if (fire) r = new Rect(r.x, r.y + Mathf.Sin(anim * 40f) * 0.08f, r.width, r.height);
        Vector2 size = beam.sprite.bounds.size;
        beam.transform.localScale = new Vector3(r.width / size.x, r.height / size.y, 1f);
        // põe o meio do desenho no meio da faixa (vale para qualquer pivô do sprite)
        Vector2 pivot = Vector2.Scale(beam.sprite.bounds.center, beam.transform.localScale);
        beam.transform.position = new Vector3(r.center.x - pivot.x, r.center.y - pivot.y, -0.5f);
        // o sprite do laser já é verde (com o núcleo branco): só a transparência muda, e o brilho treme ao disparar
        Color col = Color.white;
        if (aim) col.a = (int)(anim * 10f) % 2 == 0 ? 0.3f : 0.1f;
        else col = new Color(1f, 1f, 1f) * (0.85f + 0.15f * Mathf.Sin(anim * 50f));
        if (fire) col.a = 0.9f;
        if (!Present()) col.a *= 0.15f;
        beam.color = col;
    }

    // quadro, lado, piscadas e transparência (apagado no outro mundo)
    void Draw()
    {
        if (state == "dead") { if (aura != null) aura.enabled = false; if (beam != null) beam.enabled = false; return; }
        Sprite[] list = state == "lunge" ? lungeFrames : state == "stun" ? stunFrames : floatFrames;
        if (list != null && list.Length > 0) spriteRenderer.sprite = list[(int)(anim * 10f) % list.Length];
        spriteRenderer.flipX = facesLeft ? facing > 0 : facing < 0;
        // a investida: inclinado para a frente; tonto: balançando
        float tilt = state == "lunge" ? -18f * lungeDir : state == "stun" ? Mathf.Sin(anim * 10f) * 12f : 0f;
        transform.rotation = Quaternion.Euler(0f, 0f, tilt);

        // aura na cor do mundo dele (as mesmas cores do MUNDO no canto da tela), pulsando
        Sprite[] glow = state == "lunge" ? lungeAura : state == "stun" ? stunAura : floatAura;
        if (aura != null && glow != null && glow.Length > 0)
        {
            aura.sprite = glow[(int)(anim * 10f) % glow.Length];
            aura.flipX = spriteRenderer.flipX;
            aura.transform.localPosition = new Vector3(0f, -(aura.transform.localScale.y - 1f) * aura.sprite.bounds.center.y, 0f);
            Color wc = bossWorld == "A" ? new Color(0.45f, 1f, 0.45f) : new Color(1f, 0.6f, 0.15f);
            wc.a = (Present() ? 0.85f : 0.75f) + 0.15f * Mathf.Sin(anim * 6f);
            if (state == "swap") wc.a *= Mathf.Clamp01(1f - stateTimer / 0.35f);
            aura.color = wc;
        }
        GameManager.bossWorld = bossWorld;
        DrawBeam();
        Color c = Color.white;
        if (state == "tele" && (int)(anim * 20f) % 2 == 0) c = new Color(1f, 0.35f, 0.35f);
        if (state == "stun") c = (int)(anim * 8f) % 2 == 0 ? new Color(1f, 1f, 0.45f) : Color.white;   // tonto: pisca amarelo
        if (hitFlash > 0f && (int)(anim * 30f) % 2 == 0) c = new Color(1f, 1f, 1f, 0.3f);
        float alpha = Present() ? 1f : 0.25f;
        if (state == "swap") alpha *= Mathf.Clamp01(1f - stateTimer / 0.35f);
        c.a *= alpha;
        spriteRenderer.color = c;
    }
}
