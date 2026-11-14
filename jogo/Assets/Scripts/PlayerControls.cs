using UnityEngine;

// Controle do Metal Sonic.
//
// A física segue o Sonic Physics Guide (mesmos valores do Quill Framework, CharacterPhysicsScriptableObject):
// o jogador NÃO usa a gravidade do Rigidbody 2D. Como no Quill (GroundCollisionInfo), o chão é lido por
// "sensores" (Physics2D.Raycast) contra os colliders dos Tilemaps de colisão. Isso permite correr em
// rampas, paredes e loops. Os valores estão em pixels por quadro (60 quadros por segundo).
//
// Componentes no objeto (como nas aulas): Rigidbody 2D (Body Type = Kinematic), Capsule Collider 2D,
// tag "Player". O Rigidbody kinematic permite que anéis, molas e espinhos usem OnTriggerEnter2D.
public class PlayerControls : MonoBehaviour
{
    // ---------- Controles (GDD + comandos do Quill/Sonic CD) ----------
    public KeyCode moveLeft = KeyCode.A;        // andar para a esquerda
    public KeyCode moveRight = KeyCode.D;       // andar para a direita
    public KeyCode lookUp = KeyCode.W;          // olhar para cima / W + SHIFT parado = Super Peel Out
    public KeyCode down = KeyCode.S;            // rolar / agachar / S + ESPAÇO = Spindash
    public KeyCode jump = KeyCode.Space;        // pular (segurar no ar = hover curto)
    public KeyCode dash = KeyCode.LeftShift;    // investida
    public KeyCode teleport = KeyCode.V;        // teleporte
    public KeyCode swapWorld = KeyCode.X;       // trocar de dimensão

    // ---------- Física (px por quadro, Sonic Physics Guide / Quill) ----------
    public float acceleration = 0.046875f;      // aceleração no chão
    public float deceleration = 0.5f;           // frenagem (apertar para o lado contrário)
    public float friction = 0.046875f;          // atrito (sem apertar nada)
    public float topSpeed = 6f;                 // velocidade máxima correndo
    public float slopeFactor = 0.125f;          // força da rampa andando
    public float slopeRollUp = 0.078125f;       // força da rampa rolando para cima
    public float slopeRollDown = 0.3125f;       // força da rampa rolando para baixo
    public float rollFriction = 0.0234375f;
    public float rollDeceleration = 0.125f;
    public float maxRollSpeed = 16f;
    public float airAcceleration = 0.09375f;
    public float gravity = 0.21875f;
    public float jumpForce = 6.5f;
    public float jumpRelease = 4f;              // ao soltar o pulo, a subida é cortada para este valor
    public float maxFallSpeed = 16f;
    public float slipSpeed = 2.5f;              // abaixo disso, em parede/teto, o jogador cai

    // ---------- Comandos do Quill ----------
    public float spindashMinSpeed = 8f;         // Spindash: velocidade mínima
    public float spindashCharge = 2f;           // Spindash: quanto cada toque no pulo carrega
    public float peelOutSpeed = 12f;            // Super Peel Out: velocidade de saída
    public float peelOutChargeTime = 0.5f;      // Super Peel Out: tempo para carregar (30 quadros)
    public float skidSpeed = 4f;                // derrapa ao inverter acima desta velocidade

    // ---------- Habilidades do Metal Sonic (GDD) ----------
    public float dashSpeed = 12f;               // investida
    public float dashTime = 0.2f;
    public float dashCooldown = 0.55f;
    public float hoverTime = 0.45f;             // hover curto (segurar o pulo no ar)
    public float teleportDistance = 170f;       // em pixels
    public float invulnerableTime = 1.6f;

    // ---------- Tamanho do corpo (px) ----------
    public float bodyHeight = 19f;              // centro até o pé
    public float sensorWidth = 9f;              // meia-largura dos sensores do pé
    public float wallReach = 10f;               // alcance do sensor de parede
    public int cornerCorrection = 6;            // px que o jogador desliza para escapar de uma quina no teto
    public bool onRail = false;                 // dentro de um tubo (Tube.cs): o tubo move o jogador
    public float snapDistance = 12f;            // quanto o chão "puxa" o jogador (rampas)
    public float pixelsPerUnit = 32f;           // 1 unidade da Unity = 32 pixels do jogo

    public float deathY = -40f;                 // cair abaixo disto (unidades) = abismo

    // ---------- Estado (aparece no Inspector para depuração) ----------
    public bool grounded;
    public float groundSpeed;                   // velocidade ao longo do chão
    public float xSpeed, ySpeed;                // velocidade no ar
    public Vector2 groundNormal = Vector2.up;
    public int facing = 1;                      // 1 = direita, -1 = esquerda
    public int loopPlane = 1;                   // plano de colisão dos loops (1 ou 2)
    public bool rolling, jumping, dashing, hovering, crouching, lookingUp, skidding;
    public bool spindashCharging, peelOutCharging, dead;

    private Rigidbody2D rb2d;
    private Animator anim;
    private Transform spriteTransform;
    private SpriteRenderer spriteRenderer;
    private Vector2 pos;                        // posição em pixels
    private Vector2 spawnPoint;
    private Collider2D groundCollider;
    private int solidMask, oneWayMask;
    private float controlLock, dashTimer, dashCooldownTimer, hoverLeft, coyote, jumpBuffer;
    private float spinRev, peelOutTimer, invulnerable, stun, drawAngle;
    private bool airDashUsed;
    private string currentAnimation = "";

    // Teclas "apertadas" pelos testes automáticos (Tests/PlayModeTests). No jogo normal fica vazio.
    public static System.Collections.Generic.List<KeyCode> simulatedKeys = new System.Collections.Generic.List<KeyCode>();

    bool Key(KeyCode k)
    {
        return Input.GetKey(k) || simulatedKeys.Contains(k);
    }

    bool KeyDown(KeyCode k)
    {
        return Input.GetKeyDown(k) || simulatedKeysDown.Contains(k);
    }

    public static System.Collections.Generic.List<KeyCode> simulatedKeysDown = new System.Collections.Generic.List<KeyCode>();
    // teclas apertadas guardadas até o próximo FixedUpdate (Update roda mais vezes que FixedUpdate)
    private bool jumpPressed, dashPressed, teleportPressed, swapPressed, peelOutRequest;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // o objeto "Visual" gira com o chão; o sprite fica dentro dele, com o pé no centro do corpo
        spriteTransform = spriteRenderer.transform.parent != transform ? spriteRenderer.transform.parent : spriteRenderer.transform;
        Time.fixedDeltaTime = 1f / 60f;         // a física do Sonic roda a 60 quadros por segundo
        Physics2D.queriesStartInColliders = false;   // sensores que começam dentro de algo não batem nele
        if (GameManager.hasCheckpoint) transform.position = GameManager.checkpoint;   // renasce no ponto de controle
        pos = (Vector2)transform.position * pixelsPerUnit;
        spawnPoint = pos;
        hoverLeft = hoverTime;
    }

    void Update()
    {
        // Lê as teclas "apertadas agora" e guarda para o FixedUpdate
        if (KeyDown(jump)) jumpPressed = true;
        if (KeyDown(dash)) dashPressed = true;
        if (KeyDown(teleport)) teleportPressed = true;
        if (KeyDown(swapWorld)) swapPressed = true;
        simulatedKeysDown.Clear();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        pos = rb2d.position * pixelsPerUnit;    // aceita mudanças de posição feitas por outros scripts
        if (dead)
        {
            ySpeed -= gravity;
            pos.y += ySpeed;
            rb2d.MovePosition(pos / pixelsPerUnit);
            return;
        }

        if (onRail)
        {
            rolling = true;
            grounded = false;
            xSpeed = ySpeed = groundSpeed = 0f;
            return;
        }

        dashCooldownTimer -= dt;
        invulnerable -= dt;
        stun -= dt;
        solidMask = GetSolidMask(GameManager.world);
        oneWayMask = GameManager.world == "A" ? LayerMask.GetMask("PlataformaA") : LayerMask.GetMask("PlataformaB");

        bool canAct = stun <= 0f && !GameManager.paused;
        float dir = 0f;
        if (canAct && Key(moveLeft)) dir -= 1f;
        if (canAct && Key(moveRight)) dir += 1f;

        if (canAct && jumpPressed) jumpBuffer = 0.1f;
        else jumpBuffer -= dt;

        if (canAct && swapPressed) TrySwapWorld();
        if (canAct && teleportPressed) TryTeleport();
        // parado olhando para cima, o SHIFT carrega o Super Peel Out em vez da investida (o ESPAÇO sempre pula)
        if (canAct && dashPressed && grounded && Key(lookUp) && Mathf.Abs(groundSpeed) < 1f) peelOutRequest = true;
        else if (canAct && dashPressed && dashCooldownTimer <= 0f && !dashing && !(airDashUsed && !grounded)) StartDash();
        jumpPressed = dashPressed = teleportPressed = swapPressed = false;

        if (dashing)
        {
            dashTimer -= dt;
            if (dashTimer <= 0f)
            {
                dashing = false;
                if (grounded) groundSpeed = facing * Mathf.Min(Mathf.Abs(groundSpeed), 8.5f);
                else xSpeed = facing * Mathf.Min(Mathf.Abs(xSpeed), 8.5f);
            }
        }

        if (grounded) GroundMovement(dt, dir, canAct);
        else AirMovement(dt, dir, canAct);

        if (pos.y < deathY * pixelsPerUnit) Die();
        rb2d.MovePosition(pos / pixelsPerUnit);
    }

    // ==================================================================
    // Camadas de colisão
    // ==================================================================
    // Chão comum + o mundo atual + o plano atual do loop + barreiras de energia
    int GetSolidMask(string world)
    {
        string worldLayer = world == "A" ? "MundoA" : "MundoB";
        string planeLayer = loopPlane == 1 ? "LoopPlano1" : "LoopPlano2";
        return LayerMask.GetMask("Chao", worldLayer, planeLayer, "Barreira");
    }

    // Tangente do chão: para a direita quando o chão é plano
    Vector2 Tangent(Vector2 n)
    {
        return new Vector2(n.y, -n.x);
    }

    // Um raio (sensor) entre dois pontos em pixels
    RaycastHit2D Sensor(Vector2 from, Vector2 to, int mask)
    {
        return Physics2D.Linecast(from / pixelsPerUnit, to / pixelsPerUnit, mask);
    }

    // Procura chão na direção contrária à normal n (sensores A e B do Sonic Physics Guide).
    // Devolve true e a nova posição/normal quando encontra.
    bool SenseGround(Vector2 n, float reach, bool includeOneWay, out Vector2 newPos, out Vector2 newNormal, out Collider2D col)
    {
        newPos = pos;
        newNormal = n;
        col = null;
        Vector2 t = Tangent(n);
        Vector2 a0 = pos + t * sensorWidth, b0 = pos - t * sensorWidth;
        int mask = solidMask | (includeOneWay ? oneWayMask : 0);
        RaycastHit2D a = Sensor(a0, a0 - n * reach, mask);
        RaycastHit2D b = Sensor(b0, b0 - n * reach, mask);
        bool hitA = a && Vector2.Dot(a.normal, n) >= 0.3f;   // paredes e tetos não contam como chão
        bool hitB = b && Vector2.Dot(b.normal, n) >= 0.3f;
        if (!hitA && !hitB) return false;

        Vector2 pa = a.point * pixelsPerUnit, pb = b.point * pixelsPerUnit;
        Vector2 surface;
        if (hitA && hitB && (pa - pb).magnitude > 4f)
        {
            // normal pela reta entre os dois pontos (curvas ficam suaves)
            Vector2 tl = (pa - pb).normalized;
            Vector2 nl = new Vector2(-tl.y, tl.x);
            if (Vector2.Dot(nl, a.normal) > 0.85f && Vector2.Dot(nl, b.normal) > 0.85f && Vector2.Dot(nl, n) > 0.25f)
            {
                newNormal = nl;
                surface = (pa + pb) * 0.5f;
                col = a.collider;
                return Finish(n, surface, ref newNormal, ref newPos);
            }
        }
        // usa o sensor que achou o chão mais alto (mais perto do corpo)
        RaycastHit2D h;
        Vector2 hp;
        if (!hitA) { h = b; hp = pb; }
        else if (!hitB) { h = a; hp = pa; }
        else if (Vector2.Dot(pa - a0, n) > Vector2.Dot(pb - b0, n)) { h = a; hp = pa; }
        else { h = b; hp = pb; }
        // aceita a inclinação nova até 75° de diferença (de uma descida de 63° para o chão reto, por exemplo);
        // antes ficava a inclinação antiga e o jogador "andava deitado"
        if (Vector2.Dot(h.normal, n) > 0.26f) newNormal = h.normal;
        col = h.collider;
        return Finish(n, hp, ref newNormal, ref newPos);
    }

    bool Finish(Vector2 n, Vector2 surface, ref Vector2 newNormal, ref Vector2 newPos)
    {
        if (Vector2.Dot(newNormal, n) < 0.26f) return false;   // mudança brusca demais
        float d = Vector2.Dot(surface - pos, newNormal);
        newPos = pos + newNormal * (bodyHeight + d);
        return true;
    }

    // ==================================================================
    // No chão
    // ==================================================================
    void GroundMovement(float dt, float dir, bool canAct)
    {
        coyote = 0.07f;
        hoverLeft = hoverTime;
        airDashUsed = false;
        jumping = hovering = false;
        controlLock = Mathf.Max(0f, controlLock - 1f);

        float sinA = -groundNormal.x;
        float angle = Mathf.Acos(Mathf.Clamp(groundNormal.y, -1f, 1f)) * Mathf.Rad2Deg;
        bool allow = canAct && controlLock <= 0f;
        float d = allow ? dir : 0f;
        bool holdDown = allow && Key(down);
        bool holdUp = allow && Key(lookUp);
        bool slow = Mathf.Abs(groundSpeed) < 1f;

        // ---- Spindash (Quill: Crouch + Spindash) ----
        crouching = holdDown && slow && !rolling && angle < 30f;
        if (crouching && jumpBuffer > 0f)
        {
            spindashCharging = true;
            spinRev = Mathf.Min(spinRev + spindashCharge, 8f);
            GameManager.PlaySound("spindash");
            jumpBuffer = 0f;
        }
        if (spindashCharging)
        {
            spinRev -= spinRev / 32f;           // a carga cai aos poucos
            if (!holdDown)
            {
                groundSpeed = facing * (spindashMinSpeed + Mathf.Floor(spinRev) / 2f);
                spindashCharging = false;
                rolling = true;
                spinRev = 0f;
                GameManager.PlaySound("dash");
            }
            else
            {
                groundSpeed = 0f;
                StickToGround();
                return;
            }
        }

        // ---- Super Peel Out (Sonic CD) ----
        lookingUp = holdUp && slow && !rolling && !peelOutCharging;
        if (holdUp && slow && peelOutRequest && !peelOutCharging)
        {
            peelOutCharging = true;
            peelOutTimer = 0f;
        }
        peelOutRequest = false;
        if (peelOutCharging)
        {
            peelOutTimer += dt;
            if (!holdUp)
            {
                peelOutCharging = false;
                if (peelOutTimer >= peelOutChargeTime)
                {
                    groundSpeed = facing * peelOutSpeed;
                    GameManager.PlaySound("dash");
                }
            }
            else
            {
                groundSpeed = 0f;
                StickToGround();
                return;
            }
        }

        if (!crouching && TryJump(canAct)) return;                // olhando para cima também pula

        // ---- Correr / frear / derrapar ----
        skidding = false;
        if (dashing)
        {
            groundSpeed = facing * dashSpeed;
        }
        else if (!rolling)
        {
            if (holdDown && Mathf.Abs(groundSpeed) >= 1f)
            {
                rolling = true;                 // S correndo = rolar
                GameManager.PlaySound("dash");
            }
            if (d != 0f)
            {
                facing = d > 0f ? 1 : -1;
                if (Mathf.Sign(groundSpeed) == -d && groundSpeed != 0f)
                {
                    skidding = Mathf.Abs(groundSpeed) >= skidSpeed;
                    groundSpeed += d * deceleration;
                    if (Mathf.Sign(groundSpeed) == d) groundSpeed = d * 0.5f;
                }
                else if (groundSpeed * d < topSpeed)
                {
                    groundSpeed += d * acceleration;
                    if (groundSpeed * d > topSpeed) groundSpeed = d * topSpeed;
                }
            }
            else
            {
                groundSpeed -= Mathf.Sign(groundSpeed) * Mathf.Min(Mathf.Abs(groundSpeed), friction);
            }
            groundSpeed -= slopeFactor * sinA;
        }
        if (rolling && !dashing)
        {
            if (d != 0f && Mathf.Sign(groundSpeed) == -d) groundSpeed += d * rollDeceleration;
            else groundSpeed -= Mathf.Sign(groundSpeed) * Mathf.Min(Mathf.Abs(groundSpeed), rollFriction);
            float slope = Mathf.Sign(groundSpeed) == Mathf.Sign(sinA) ? slopeRollUp : slopeRollDown;
            groundSpeed -= slope * sinA;
            groundSpeed = Mathf.Clamp(groundSpeed, -maxRollSpeed, maxRollSpeed);
            if (Mathf.Abs(groundSpeed) < 0.5f) rolling = false;
        }
        else if (!dashing)
        {
            groundSpeed = Mathf.Clamp(groundSpeed, -maxRollSpeed, maxRollSpeed);
        }

        // parede ou teto com pouca velocidade: perde o controle e cai
        if (!dashing && Mathf.Abs(groundSpeed) < slipSpeed && angle >= 35f && controlLock <= 0f)
        {
            controlLock = 30f;
            if (angle >= 90f)
            {
                BecomeAirborne();
                return;
            }
        }

        MoveOnGround();
    }

    void MoveOnGround()
    {
        // esteira: o chão leva o jogador junto
        if (groundCollider != null)
        {
            Conveyor belt = groundCollider.GetComponent<Conveyor>();
            if (belt != null) pos += Tangent(groundNormal) * belt.speed / 60f;
        }

        // anda em passos de no máximo 4 px para não atravessar nada em alta velocidade
        float distance = Mathf.Abs(groundSpeed);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 4f));
        float step = groundSpeed / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 t = Tangent(groundNormal);
            if (step != 0f)
            {
                Vector2 dirV = t * Mathf.Sign(step);
                Vector2 from = pos;
                if (groundNormal.y > 0.97f) from += new Vector2(0f, -8f);   // pega degraus baixos
                RaycastHit2D wall = Sensor(from, from + dirV * (wallReach + Mathf.Abs(step)), solidMask);
                // só é parede o que fica quase em pé em relação ao chão atual (uma rampa à frente não é parede)
                // e o que for mais alto que um degrau baixo (esse o jogador sobe)
                if (wall && (Vector2.Dot(wall.normal, groundNormal) > 0.3f || LowStep(wall, dirV))) wall = default;
                if (wall)
                {
                    float gap = Mathf.Max(0f, Vector2.Dot(wall.point * pixelsPerUnit - from, dirV) - wallReach);
                    pos += dirV * Mathf.Min(gap, Mathf.Abs(step));
                    groundSpeed = 0f;
                    dashing = false;
                    StickToGround();
                    return;
                }
                pos += t * step;
            }
            if (!StickToGround())
            {
                BecomeAirborne();
                return;
            }
        }
    }

    // degrau baixo à frente (até 12 px acima do pé, como uma emenda entre peças): o sensor de chão sobe nele
    bool LowStep(RaycastHit2D wall, Vector2 dirV)
    {
        if (groundNormal.y < 0.9f) return false;
        Vector2 probe = wall.point * pixelsPerUnit + dirV * 2f;
        float feet = pos.y - bodyHeight;
        // se a parede continua acima da altura do degrau, é parede mesmo (barreira, parede de mundo, torre)
        Vector2 above = new Vector2(probe.x, feet + 14f);
        if (Physics2D.OverlapPoint(above / pixelsPerUnit, solidMask)) return false;
        if (Sensor(new Vector2(pos.x, feet + 14f), above + dirV * 2f, solidMask)) return false;
        RaycastHit2D top = Sensor(new Vector2(probe.x, feet + 14f), new Vector2(probe.x, feet - 2f), solidMask);
        return top && top.point.y * pixelsPerUnit - feet <= 12f && Vector2.Dot(top.normal, Vector2.up) > 0.5f;
    }

    // Reencosta o jogador no chão (segue rampas e curvas). false = não há chão
    bool StickToGround()
    {
        Vector2 p, n;
        Collider2D c;
        if (!SenseGround(groundNormal, bodyHeight + snapDistance, groundNormal.y > 0.7f, out p, out n, out c)) return false;
        pos = p;
        groundNormal = n;
        groundCollider = c;
        return true;
    }

    void BecomeAirborne()
    {
        if (!grounded) return;
        Vector2 t = Tangent(groundNormal);
        xSpeed = groundSpeed * t.x;
        ySpeed = groundSpeed * t.y;
        grounded = false;
        crouching = lookingUp = spindashCharging = peelOutCharging = skidding = false;
        groundCollider = null;
    }

    bool TryJump(bool canAct)
    {
        if (!(jumpBuffer > 0f && canAct && !dashing)) return false;
        if (grounded)
        {
            Vector2 n = groundNormal, t = Tangent(n);
            xSpeed = groundSpeed * t.x + jumpForce * n.x;
            ySpeed = groundSpeed * t.y + jumpForce * n.y;
            grounded = false;
            groundCollider = null;
        }
        else
        {
            ySpeed = jumpForce;
        }
        jumping = true;
        rolling = false;
        coyote = 0f;
        jumpBuffer = 0f;
        GameManager.PlaySound("jump");
        return true;
    }

    // ==================================================================
    // No ar
    // ==================================================================
    void AirMovement(float dt, float dir, bool canAct)
    {
        coyote -= dt;
        if (coyote > 0f && !jumping) TryJump(canAct);
        bool holdJump = Key(jump);
        if (jumping && ySpeed > jumpRelease && !holdJump) ySpeed = jumpRelease;   // pulo de altura variável

        // hover curto: segurar o pulo depois do ponto mais alto
        hovering = !dashing && canAct && holdJump && ySpeed < 0.7f && hoverLeft > 0f;
        if (hovering) hoverLeft -= dt;

        if (dashing)
        {
            xSpeed = facing * dashSpeed;
            ySpeed = 0f;
        }
        else
        {
            if (dir != 0f)
            {
                facing = dir > 0f ? 1 : -1;
                if (dir < 0f && xSpeed > -topSpeed) xSpeed = Mathf.Max(xSpeed - airAcceleration, -topSpeed);
                else if (dir > 0f && xSpeed < topSpeed) xSpeed = Mathf.Min(xSpeed + airAcceleration, topSpeed);
            }
            if (ySpeed > 0f && ySpeed < 4f && Mathf.Abs(xSpeed) >= 0.125f) xSpeed *= 0.96875f;   // resistência do ar
            if (hovering) ySpeed = Mathf.MoveTowards(ySpeed, -0.5f, 0.9f);
            else ySpeed = Mathf.Max(ySpeed - gravity, -maxFallSpeed);
        }

        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(xSpeed), Mathf.Abs(ySpeed)) / 4f));
        float dx = xSpeed / steps, dy = ySpeed / steps;
        for (int i = 0; i < steps; i++)
        {
            // sensores de parede (dois, um acima e um abaixo do centro)
            if (dx != 0f)
            {
                Vector2 dirV = new Vector2(Mathf.Sign(dx), 0f);
                bool blocked = false;
                for (int k = -1; k <= 1 && !blocked; k += 2)
                {
                    Vector2 from = pos + new Vector2(0f, 8f * k);
                    RaycastHit2D wall = Sensor(from, from + dirV * (wallReach + Mathf.Abs(dx)), solidMask);
                    if (wall)
                    {
                        float gap = Mathf.Max(0f, Vector2.Dot(wall.point * pixelsPerUnit - from, dirV) - wallReach);
                        pos += dirV * Mathf.Min(gap, Mathf.Abs(dx));
                        blocked = true;
                    }
                }
                if (blocked)
                {
                    xSpeed = dx = 0f;
                    dashing = false;
                }
                else pos.x += dx;
            }
            // subindo: sensores de teto (plataformas atravessáveis não contam)
            if (dy > 0f)
            {
                RaycastHit2D left = CeilingSensor(pos, -1, dy), right = CeilingSensor(pos, 1, dy);
                // correção de quina: se só um lado bate numa quina pequena, desliza até 6 px para o lado livre
                bool hitLeft = left, hitRight = right;
                if (hitLeft != hitRight)
                {
                    int away = hitLeft ? 1 : -1;
                    for (int s = 1; s <= cornerCorrection; s++)
                    {
                        Vector2 test = pos + new Vector2(away * s, 0f);
                        if (CeilingSensor(test, -1, dy) || CeilingSensor(test, 1, dy)) continue;
                        if (Sensor(pos, pos + new Vector2(away * (s + wallReach), 0f), solidMask)) break;
                        pos = test;
                        left = right = new RaycastHit2D();
                        break;
                    }
                }
                RaycastHit2D top = left ? left : right;
                if (top)
                {
                    pos.y = top.point.y * pixelsPerUnit - bodyHeight;
                    ySpeed = dy = 0f;
                }
                else pos.y += dy;
            }
            else
            {
                // descendo: sensores de chão (plataformas atravessáveis contam)
                pos.y += dy;
                if (ySpeed <= 0f)
                {
                    Vector2 p, n;
                    Collider2D c;
                    if (SenseGround(Vector2.up, bodyHeight + 1f, true, out p, out n, out c) && n.y > 0.5f)
                    {
                        Land(p, n, c);
                        return;
                    }
                }
            }
        }
    }

    void Land(Vector2 p, Vector2 n, Collider2D c)
    {
        pos = p;
        float sinA = -n.x;
        float angle = Mathf.Acos(Mathf.Clamp(n.y, -1f, 1f)) * Mathf.Rad2Deg;
        float fall = -ySpeed;
        // velocidade ao pousar depende do ângulo do chão (Sonic Physics Guide)
        if (angle < 23f) groundSpeed = xSpeed;
        else if (angle < 45f) groundSpeed = Mathf.Abs(xSpeed) > fall ? xSpeed : fall * 0.5f * -Mathf.Sign(sinA);
        else groundSpeed = Mathf.Abs(xSpeed) > fall ? xSpeed : fall * -Mathf.Sign(sinA);
        groundNormal = n;
        groundCollider = c;
        grounded = true;
        xSpeed = ySpeed = 0f;
        jumping = false;
        rolling = rolling && Mathf.Abs(groundSpeed) >= 0.5f && Key(down);
        if (dashing) groundSpeed = facing * dashSpeed;
    }

    // ==================================================================
    // Molas, aceleradores, loops (chamados pelos scripts desses objetos)
    // ==================================================================
    // sensor de teto de um lado do jogador (k = -1 esquerda, 1 direita)
    RaycastHit2D CeilingSensor(Vector2 at, int k, float dy)
    {
        Vector2 from = at + new Vector2(sensorWidth * k, 0f);
        return Sensor(from, from + new Vector2(0f, dy + bodyHeight), solidMask);
    }

    public void Launch(Vector2 direction, float speed)
    {
        dashing = rolling = false;
        if (Mathf.Abs(direction.y) > 0.5f)
        {
            BecomeAirborne();
            ySpeed = direction.y * speed;
            jumping = false;
            hoverLeft = hoverTime;
        }
        else
        {
            if (grounded)
            {
                groundSpeed = direction.x * speed;
                controlLock = 16f;
            }
            else xSpeed = direction.x * speed;
            facing = direction.x > 0f ? 1 : -1;
        }
        GameManager.PlaySound("jump");
    }

    public void Boost(float dirX, float speed)
    {
        if (grounded)
        {
            if (groundSpeed * dirX < speed) groundSpeed = dirX * speed;
        }
        else if (xSpeed * dirX < speed) xSpeed = dirX * speed;
        facing = dirX > 0f ? 1 : -1;
    }

    // Velocidade em pixels por segundo (usada pela câmera e pelas zonas do loop)
    public Vector2 GetVelocity()
    {
        if (grounded) return Tangent(groundNormal) * groundSpeed * 60f;
        return new Vector2(xSpeed, ySpeed) * 60f;
    }

    // Ataca inimigos se estiver rolando, pulando ou na investida
    public bool IsAttacking()
    {
        return !dead && (dashing || rolling || spindashCharging || (jumping && !grounded));
    }

    // ==================================================================
    // Habilidades do GDD
    // ==================================================================
    void StartDash()
    {
        dashing = true;
        rolling = false;
        dashTimer = dashTime;
        dashCooldownTimer = dashCooldown;
        if (!grounded) airDashUsed = true;
        GameManager.PlaySound("dash");
    }

    // A forma do corpo está livre de paredes nesta posição?
    bool IsFree(Vector2 p, int mask)
    {
        Vector2 size = new Vector2(19f, 34f) / pixelsPerUnit;
        return Physics2D.OverlapCapsule(p / pixelsPerUnit, size, CapsuleDirection2D.Vertical, 0f, mask) == null;
    }

    void TrySwapWorld()
    {
        string other = GameManager.world == "A" ? "B" : "A";
        if (GameManager.tempus <= GameManager.swapCost)
        {
            GameManager.ShowMessage("Tempus insuficiente para trocar de mundo!");
            return;
        }
        if (!IsFree(pos, GetSolidMask(other)))
        {
            GameManager.ShowMessage("Troca bloqueada: há uma parede nessa posição no outro mundo");
            return;
        }
        GameManager.SwapWorld(other);
        solidMask = GetSolidMask(other);
    }

    void TryTeleport()
    {
        if (GameManager.teleportEnergy < GameManager.teleportMax)
        {
            GameManager.ShowMessage("Teleporte recarregando...");
            return;
        }
        Vector2 d = new Vector2(facing, 0f);
        if (Key(lookUp))
        {
            float h = (Key(moveRight) ? 1f : 0f) - (Key(moveLeft) ? 1f : 0f);
            d = h != 0f ? new Vector2(h, 1f).normalized : Vector2.up;
        }
        int walls = solidMask & ~LayerMask.GetMask("Barreira");   // o teleporte atravessa barreiras
        float[] lifts = { 0f, 16f, 32f };
        foreach (float lift in lifts)
        {
            Vector2 target = pos + d * teleportDistance + new Vector2(0f, lift);
            if (IsFree(target, solidMask) && PathClear(pos, target, walls))
            {
                BecomeAirborne();
                pos = target;
                if (d.y != 0f) ySpeed = 0f;
                GameManager.teleportEnergy = 0f;
                GameManager.PlaySound("teleport");
                return;
            }
        }
        GameManager.ShowMessage("Destino bloqueado: o teleporte só atravessa barreiras de energia");
    }

    bool PathClear(Vector2 from, Vector2 to, int mask)
    {
        Vector2 d = to - from;
        Vector2 side = new Vector2(-d.y, d.x).normalized * 10f;
        for (int k = -1; k <= 1; k++)
            if (Sensor(from + side * k, to + side * k, mask)) return false;
        return true;
    }

    // ==================================================================
    // Dano e morte
    // ==================================================================
    public void Hurt(float fromX)
    {
        if (invulnerable > 0f || dead) return;
        if (GameManager.shield) GameManager.shield = false;
        else if (GameManager.rings > 0) GameManager.LoseRings(transform.position);
        else
        {
            Die();
            return;
        }
        invulnerable = invulnerableTime;
        stun = 0.3f;
        dashing = rolling = false;
        BecomeAirborne();
        xSpeed = (transform.position.x >= fromX ? 1f : -1f) * 2f;
        ySpeed = 4f;
        GameManager.PlaySound("hurt");
    }

    // Quica depois de acertar um inimigo de cima
    public void Bounce()
    {
        if (!grounded && ySpeed < 0f) ySpeed = -ySpeed;
    }

    public void Die()
    {
        if (dead) return;
        dead = true;
        dashing = false;
        ySpeed = 7f;
        GameManager.PlayerDied();
    }

    public void Respawn()
    {
        pos = spawnPoint;
        grounded = rolling = jumping = dashing = hovering = dead = false;
        groundSpeed = xSpeed = ySpeed = 0f;
        groundNormal = Vector2.up;
        loopPlane = 1;
        rb2d.position = pos / pixelsPerUnit;
    }

    // ==================================================================
    // Animação (Animator com um estado por animação) e giro do sprite
    // ==================================================================
    void UpdateAnimation()
    {
        if (anim == null) return;
        string state;
        float speed = 1f;
        if (dead || stun > 0f) state = "hurt";
        else if (dashing) state = "dash";
        else if (spindashCharging) { state = "ball"; speed = 2f; }
        else if (peelOutCharging) state = peelOutTimer >= peelOutChargeTime ? "boost" : "run";
        else if (hovering) state = "hover";
        else if (!grounded || rolling) state = "ball";
        else if (skidding) state = "skid";
        else if (crouching) state = "crouch";
        else if (lookingUp) state = "lookup";
        else if (Mathf.Abs(groundSpeed) >= 10f) state = "boost";
        else if (Mathf.Abs(groundSpeed) > 0.5f)
        {
            state = "run";
            speed = Mathf.Clamp(Mathf.Abs(groundSpeed) / topSpeed, 0.5f, 2f);
        }
        else state = "idle";
        if (state != currentAnimation)
        {
            anim.Play(state);
            currentAnimation = state;
        }
        anim.speed = speed;

        // gira o sprite junto com o chão e vira para o lado certo
        float target = grounded && !crouching ? Mathf.Atan2(-groundNormal.x, groundNormal.y) * Mathf.Rad2Deg : 0f;
        drawAngle = Mathf.LerpAngle(drawAngle, target, 0.4f);
        spriteTransform.localRotation = Quaternion.Euler(0f, 0f, drawAngle);
        spriteRenderer.flipX = facing < 0;
        spriteRenderer.enabled = invulnerable <= 0f || Mathf.FloorToInt(Time.time * 14f) % 2 == 0;
    }
}
