using UnityEngine;
using UnityEngine.SceneManagement;

// Cenas de corte (GDD): cada página é uma ilustração animada (cenário em camadas, personagens com os quadros
// do jogo, partículas, clarões) e uma caixa de texto que aparece letra por letra, com o retrato de quem fala.
// ENTER, ESPAÇO ou clique avançam; ESC pula a cena inteira. Feita com OnGUI, como o placar do Pong.
// - Numa cena própria (abertura, entre as fases, final): no fim carrega nextScene.
// - Dentro da fase (overlay = true, antes do chefe): pausa o jogo (Time.timeScale = 0) e, no fim, chama
//   BossArena.StartFight().
// Tudo é desenhado numa tela virtual de 1280 x 720, escalada para a resolução do jogo.
public class Cutscene : MonoBehaviour
{
    public string id = "intro";                 // intro, after1, after2, boss, ending
    public string nextScene = "Fase1_PalmtreePanic";
    public bool overlay = false;
    public BossArena arena;                     // só no overlay do chefe

    // arte (arrastada no Inspector)
    public Texture2D levelA, levelB;            // a nossa Fase 1 nos dois mundos: fotos do jogo (o chão fica a 13% de baixo)
    public Texture2D quartzA, quartzB, stardustA, stardustB;
    public Texture2D planet, emblem, banner, faceA, faceB, sign, ring;
    public Texture2D mecha, mechaHot;           // Mecha Sonic: os 2 quadros do chefe (o segundo com o brilho)
    public Texture2D[] metal;                   // Metal Sonic da tela de título (sonic_0..6)
    public Texture2D[] arrive;                  // Metal Sonic voando (trans_0..3)
    public Texture2D[] metalIdle, metalRun, metalFly, metalHurt;   // quadros do jogo
    public Texture2D quartzGround, quartzGroundB, crystal, crystal2;
    public Texture2D stardustGround, stardustGroundB, horn;
    public GUISkin layout;

    public static bool playing = false;         // o GameManager não abre a pausa com o ESC da cena
    public static int endedFrame = -1;
    public static bool autoSkip = false;        // testes: pula as cenas sozinho

    class Page
    {
        public string art, who, text;
        public Page(string art, string who, string text) { this.art = art; this.who = who; this.text = text; }
    }
    private Page[] pages;
    private int page = 0;
    private float shown = 0f, t = 0f, pageTime = 0f, leaving = -1f;
    private bool done = false;

    void Start()
    {
        pages = Pages(id);
        playing = true;
        if (overlay) Time.timeScale = 0f;
    }

    // ------------------------------------------------------------------
    // roteiro
    // ------------------------------------------------------------------
    Page[] Pages(string which)
    {
        const string M = "Metal Sonic", E = "Dr. Eggman (mensagem)";
        switch (which)
        {
            case "intro":
                return new[] {
                    new Page("lab", "", "Little Planet. Numa base escondida perto de Palmtree Panic, o Dr. Eggman prepara um novo experimento."),
                    new Page("capsule", "", "No centro do laboratório, o protótipo Mecha Sonic recebe a energia de um núcleo temporal instável."),
                    new Page("alarm", "", "Os medidores disparam. O núcleo sobrecarrega e o Mecha Sonic desperta fora de controle."),
                    new Page("rift", "", "Um clarão... e a realidade se rasga ao meio."),
                    new Page("split", "", "Desde então, dois mundos ocupam o mesmo lugar: o Presente (Mundo A) e um Futuro arruinado (Mundo B)."),
                    new Page("wreck", "", "Entre os destroços, uma unidade esquecida volta a ligar."),
                    new Page("robot", M, "Sistemas reativados. Anomalia detectada... Contagem regressiva em meu núcleo: TEMPUS."),
                    new Page("powers", M, "Posso trocar de dimensão e me teleportar por curtas distâncias. Mas cada troca de dimensão consome Tempus."),
                    new Page("runPalm", M, "Destino: Palmtree Panic. Preciso encontrar a origem da anomalia antes que o Tempus acabe."),
                };
            case "after1":
                return new[] {
                    new Page("goal", "", "Palmtree Panic fica para trás, mas as dimensões continuam instáveis."),
                    new Page("radar", M, "Detecto um sinal de energia muito forte... vindo de dentro da terra. Quartz Quadrant."),
                    new Page("flyQuartz", "", "Metal Sonic segue o sinal até as minas de cristal."),
                    new Page("crystals", M, "Os cristais pulsam no mesmo ritmo da ruptura. Alguém está usando a energia deles."),
                    new Page("runQuartz", M, "Seguindo o sinal. Não há Tempus para hesitar."),
                };
            case "after2":
                return new[] {
                    new Page("drained", "", "No fundo do Quartz Quadrant, os cristais estão apagados. Algo drenou toda a energia deles."),
                    new Page("analysis", M, "Os cristais alimentavam o experimento de Eggman. A ruptura começou aqui."),
                    new Page("message", E, "Se está ouvindo isto, Metal Sonic... o Mecha Sonic saiu do meu controle!"),
                    new Page("message", E, "Ele fugiu para Stardust Speedway levando o núcleo temporal. Detenha-o antes que as dimensões se partam de vez!"),
                    new Page("transport", "", "Um transportador de Eggman leva Metal Sonic até Stardust Speedway."),
                    new Page("speedway", M, "Mecha Sonic está aqui. Consigo sentir a ruptura daqui."),
                };
            case "boss":
                return new[] {
                    new Page("arena", "", "Metal Sonic chega à área final de Stardust Speedway e encontra Mecha Sonic."),
                    new Page("arena", M, "Você é a origem da ruptura. Vou interromper isto, e o Tempus continua correndo."),
                };
            default:   // ending
                return new[] {
                    new Page("defeat", "", "Mecha Sonic cai. O núcleo temporal se apaga."),
                    new Page("closing", "", "Sem energia, a ruptura começa a se fechar. As duas dimensões voltam a ser uma só."),
                    new Page("stable", "", "O Presente se estabiliza. O Tempus finalmente para de contar."),
                    new Page("robotCalm", M, "Missão concluída. Anomalia eliminada."),
                    new Page("leave", "", "Metal Sonic deixa Little Planet em silêncio, do mesmo jeito que chegou."),
                    new Page("credits", "", "Obrigado por jogar Tempus Gemini!"),
                    new Page("end", "", "FIM  -  Obrigado por jogar! Tente de novo com mais anéis e menos Tempus gasto."),
                };
        }
    }

    // ------------------------------------------------------------------
    // controle
    // ------------------------------------------------------------------
    void Update()
    {
        if (done) return;
        float dt = Time.unscaledDeltaTime;
        t += dt;
        pageTime += dt;
        shown += dt * 48f;
        if (leaving >= 0f)
        {
            leaving += dt;
            if (leaving >= 0.3f) NextPage();
            return;
        }
        if (autoSkip || Input.GetKeyDown(KeyCode.Escape)) Finish();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) Advance();
    }

    void Advance()
    {
        if (shown < pages[page].text.Length) { shown = pages[page].text.Length; return; }   // primeiro mostra o texto todo
        leaving = 0f;                                   // escurece e troca de página
    }

    void NextPage()
    {
        leaving = -1f;
        page++;
        shown = 0f;
        pageTime = 0f;
        if (page >= pages.Length) Finish();
    }

    void Finish()
    {
        done = true;
        playing = false;
        endedFrame = Time.frameCount;
        GameManager.PlaySound("ui");
        if (overlay)
        {
            Time.timeScale = 1f;
            arena.StartFight();
            enabled = false;
        }
        else SceneManager.LoadScene(nextScene);
    }

    // ------------------------------------------------------------------
    // desenho
    // ------------------------------------------------------------------
    void OnGUI()
    {
        if (done || pages == null || page >= pages.Length) return;
        GUI.skin = layout;
        GUI.depth = -10;
        float k = Screen.height / 720f;
        float w = Screen.width / k, h = 720f;
        Page pg = pages[page];

        // ilustração (com tremor nas páginas agitadas)
        Vector2 shake = Vector2.zero;
        if (pg.art == "alarm" || pg.art == "rift" || pg.art == "defeat")
            shake = new Vector2(Mathf.Sin(t * 53f), Mathf.Cos(t * 47f)) * 4f * Mathf.Clamp01(1.5f - pageTime * 0.4f);
        GUI.matrix = Matrix4x4.TRS(new Vector3(shake.x * k, shake.y * k, 0f), Quaternion.identity, new Vector3(k, k, 1f));
        Rect pic = new Rect(0, 0, w, h - 180);
        Art(pg.art, pic);
        Vignette(pic);
        GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));

        // tarja de cinema em cima e a faixa do texto embaixo
        Fill(new Rect(0, 0, w, 34), Color.black);
        Fill(new Rect(0, h - 180, w, 180), new Color(0.01f, 0.02f, 0.06f));
        TextBox(pg, new Rect(24, h - 166, w - 48, 136), w, h);

        // escurecer ao entrar e ao sair da página
        float fade = Mathf.Clamp01(1f - pageTime / 0.35f);
        if (leaving >= 0f) fade = Mathf.Clamp01(leaving / 0.3f);
        if (fade > 0f) Fill(new Rect(0, 0, w, h - 180), new Color(0f, 0f, 0f, fade));
    }

    void TextBox(Page pg, Rect box, float w, float h)
    {
        Fill(box, new Color(0.03f, 0.06f, 0.15f, 0.96f));
        Frame(box, new Color(0.4f, 0.8f, 1f), 2);
        float x = box.x + 18;
        // retrato de quem fala
        if (pg.who != "")
        {
            Rect por = new Rect(box.x + 12, box.y + 12, 112, 112);
            Fill(por, new Color(0.08f, 0.12f, 0.25f));
            Frame(por, new Color(1f, 0.85f, 0.4f), 2);
            if (pg.who == "Metal Sonic") Sprite((int)(t * 1.2f) % 5 == 0 ? faceB : faceA, Shrink(por, 6), 1f);
            else Monitor(Shrink(por, 10), 0.7f);
            x = por.xMax + 18;
            GUI.Label(new Rect(x, box.y + 8, box.xMax - x - 18, 30), pg.who, Style(20, new Color(1f, 0.85f, 0.4f)));
        }
        string s = pg.text.Substring(0, Mathf.Min(pg.text.Length, (int)shown));
        GUIStyle body = Style(22, Color.white);
        body.wordWrap = true;
        GUI.Label(new Rect(x, box.y + (pg.who != "" ? 40 : 18), box.xMax - x - 18, box.height - 46), s, body);
        // indicador de "pode avançar"
        if (shown >= pg.text.Length && (int)(t * 3f) % 2 == 0)
            GUI.Label(new Rect(box.xMax - 40, box.yMax - 34, 30, 30), "▼", Style(20, new Color(0.6f, 0.9f, 1f)));
        GUIStyle hint = Style(13, new Color(0.6f, 0.7f, 0.8f));
        hint.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(w - 440, h - 28, 410, 24), "ENTER: avançar   ESC: pular     " + (page + 1) + " / " + pages.Length, hint);
    }

    // uma ilustração por página
    void Art(string kind, Rect r)
    {
        Vector2 c = r.center;
        float ground = r.yMax - 70;
        switch (kind)
        {
            // ---------------- abertura ----------------
            case "lab":        // a base de Eggman: o cenário industrial com luzes de varredura
                Pan(stardustB, r, 0.03f);
                Fill(r, new Color(0.05f, 0f, 0.1f, 0.45f));
                for (int i = 0; i < 3; i++)
                {
                    float sx = c.x + Mathf.Sin(t * 0.7f + i * 2.1f) * r.width * 0.4f;
                    Fill(new Rect(sx - 40, r.y, 80, r.height), new Color(1f, 0.2f, 0.3f, 0.10f));
                }
                Title(new Rect(r.x, r.y + 60, r.width, 60), "LITTLE PLANET", new Color(1f, 1f, 1f, Mathf.Clamp01(pageTime - 0.3f)), 46);
                Title(new Rect(r.x, r.y + 115, r.width, 40), "base secreta do Dr. Eggman", new Color(1f, 0.6f, 0.6f, Mathf.Clamp01(pageTime - 0.8f)), 24);
                break;
            case "capsule":    // o Mecha Sonic dentro da cápsula, com bolhas e energia
            {
                Pan(stardustB, r, 0.02f);
                Fill(r, new Color(0f, 0f, 0.05f, 0.55f));
                Rect tube = new Rect(c.x - 110, r.y + 70, 220, r.height - 100);
                Fill(tube, new Color(0.3f, 0.9f, 1f, 0.15f + 0.05f * Mathf.Sin(t * 3f)));
                Character(Mecha(), c.x, tube.yMax - 30 + Mathf.Sin(t * 1.5f) * 6f, 3.9f, false, 1f);
                for (int i = 0; i < 14; i++)
                {
                    float by = tube.yMax - Mathf.Repeat(t * 60f + i * 37f, tube.height);
                    Fill(new Rect(tube.x + 20 + (i * 53) % 180, by, 6, 6), new Color(0.7f, 1f, 1f, 0.6f));
                }
                Frame(tube, new Color(0.7f, 0.9f, 1f, 0.8f), 4);
                Fill(new Rect(tube.x - 20, tube.yMax, tube.width + 40, 24), new Color(0.35f, 0.35f, 0.45f));
                Fill(new Rect(tube.x - 20, tube.y - 24, tube.width + 40, 24), new Color(0.35f, 0.35f, 0.45f));
                Motes(tube, new Color(1f, 0.9f, 0.3f), 20);
                break;
            }
            case "alarm":      // sobrecarga: alarme vermelho, rachaduras e o Mecha Sonic acordando
                Pan(stardustB, r, 0.02f);
                Fill(r, new Color(1f, 0f, 0f, 0.25f + 0.2f * Mathf.Sin(t * 10f)));
                Character(Mecha(), c.x + Mathf.Sin(t * 30f) * 3f, ground + 30, 3.9f, false, 1f);
                Glow(new Vector2(c.x, ground - 170), 60f + 20f * Mathf.Sin(t * 12f), new Color(1f, 0.8f, 0.2f, 0.35f));
                Title(new Rect(r.x, r.y + 60, r.width, 60), "! ALERTA !", new Color(1f, 0.9f, 0.2f, (int)(t * 4f) % 2 == 0 ? 1f : 0.3f), 48);
                Crack(r, Color.white, Mathf.Clamp01(pageTime * 0.5f));
                break;
            case "rift":       // o clarão e o rasgo na realidade, com a energia girando
                Pan(stardustB, r, 0.04f);
                Fill(r, new Color(0.15f, 0f, 0.25f, 0.6f));
                Swirl(c, 40);
                Crack(r, Color.white, 1f);
                Sprite(planet, new Rect(c.x - 90, c.y - 80, 180, 160), 0.5f + 0.3f * Mathf.Sin(t * 3f));
                Fill(r, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - pageTime * 0.8f)));    // clarão
                break;
            case "split":      // Mundo A e Mundo B lado a lado
            {
                Rect left = new Rect(r.x, r.y, r.width / 2, r.height), right = new Rect(c.x, r.y, r.width / 2, r.height);
                Level(levelA, left, LevelAStart);
                Level(levelB, right, LevelBStart);
                Crack(r, Color.white, 1f);
                Title(new Rect(r.x, r.y + 40, r.width / 2, 50), "MUNDO A  -  PRESENTE", new Color(0.49f, 0.99f, 0.49f), 32);
                Title(new Rect(c.x, r.y + 40, r.width / 2, 50), "MUNDO B  -  BAD FUTURE", new Color(1f, 0.63f, 0.19f), 32);
                break;
            }
            case "wreck":      // o Metal Sonic caído entre os destroços, com faíscas
                PalmScene(r, ground, 0f, 0.45f);
                for (int i = 0; i < 4; i++) Fill(new Rect(c.x - 260 + i * 150, ground - 22, 80, 26), new Color(0.3f, 0.3f, 0.35f));
                // caído de lado
                Matrix4x4 keep = GUI.matrix;
                Vector3 pivot = new Vector3(c.x - 40, ground - 50, 0f);
                GUI.matrix = keep * Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -90f)) * Matrix4x4.Translate(-pivot);
                Character(Frame(metalHurt, 2f), c.x - 40, ground - 50 + 84, 4f, false, 1f);
                GUI.matrix = keep;
                if ((int)(t * 6f) % 3 == 0) Sparks(new Vector2(c.x - 30, ground - 80), new Color(0.6f, 0.9f, 1f), 10);
                break;
            case "robot":      // Metal Sonic acorda; o relógio do Tempus começa a contar
            case "robotCalm":
            case "analysis":
            {
                Fill(r, new Color(0.02f, 0.03f, 0.1f));
                for (int ln = 0; ln < 30; ln++)
                    Fill(new Rect(r.x, r.y + ln * 17 + Mathf.Repeat(t * 30f, 17f), r.width, 1), new Color(0.3f, 0.6f, 1f, 0.08f));
                Sprite((int)(t * 1.5f) % 4 == 0 ? faceB : faceA, new Rect(c.x - 400, c.y - 170, 340, 340), 1f);
                bool calm = kind == "robotCalm";
                Clock(new Vector2(c.x + 230, c.y - 10), 130f, calm ? 0f : t);
                string time = calm ? "PAROU" : (180f - t % 180f).ToString("000.0");
                Title(new Rect(c.x + 80, c.y + 135, 300, 50), kind == "analysis" ? "ENERGIA: 0%" : "TEMPUS  " + time, calm ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.5f, 0.4f), 30);
                break;
            }
            case "powers":     // trocar de mundo e teleportar: o Metal Sonic e as cópias do teleporte
            {
                Rect left = new Rect(r.x, r.y, r.width / 2, r.height), right = new Rect(c.x, r.y, r.width / 2, r.height);
                Level(levelA, left, LevelAStart);
                Level(levelB, right, LevelBStart);
                Crack(r, new Color(1f, 1f, 1f, 0.6f), 0.6f);
                float hop = Mathf.Repeat(t * 0.7f, 1f);
                for (int i = 0; i < 4; i++)   // rastro do teleporte
                    Character(Frame(metalIdle, 8f), c.x - 300 + i * 70 + hop * 70, ground, 4f, false, 0.15f + i * 0.2f);
                Character(Frame(metalIdle, 8f), c.x + 220, ground, 4f, true, 1f);
                Title(new Rect(r.x, r.y + 40, r.width / 2, 40), "V: teleporte", new Color(1f, 1f, 1f, 0.9f), 26);
                Title(new Rect(c.x, r.y + 40, r.width / 2, 40), "X: trocar de mundo", new Color(1f, 1f, 1f, 0.9f), 26);
                break;
            }
            case "runPalm":    // correndo para Palmtree Panic
                PalmScene(r, ground, t * 300f, 0f);
                Character(Frame(metalRun, 14f), c.x - 60, ground, 4.5f, false, 1f);
                SpeedLines(r, new Color(1f, 1f, 1f, 0.25f));
                Title(new Rect(r.x, r.y + 40, r.width, 50), "PALMTREE PANIC", new Color(1f, 0.95f, 0.6f, Mathf.Clamp01(pageTime - 0.5f)), 40);
                break;

            // ---------------- entre as fases ----------------
            case "goal":       // a placa do fim do Palmtree e os anéis brilhando
                PalmScene(r, ground, 0f, 0f);
                Character(sign, c.x + 150, ground, 3f, false, 1f);
                Character(Frame(metalIdle, 8f), c.x - 100, ground, 4f, false, 1f);
                for (int i = 0; i < 7; i++)
                    Sprite(ring, new Rect(c.x - 330 + i * 70, ground - 200 + Mathf.Sin(t * 3f + i) * 10f, 32, 32), 1f);
                break;
            case "radar":      // o radar aponta para o Quartz Quadrant
            {
                PalmScene(r, ground, 0f, 0.55f);
                Vector2 o = new Vector2(c.x - 160, ground - 80);
                for (int i = 0; i < 4; i++)
                {
                    float rad = Mathf.Repeat(t * 120f + i * 70f, 280f);
                    Ring(o, rad, new Color(0.4f, 1f, 0.9f, 1f - rad / 280f), 3);
                }
                Character(Frame(metalIdle, 8f), o.x, ground, 4f, false, 1f);
                float ax = Mathf.Repeat(t * 160f, 220f);
                Title(new Rect(c.x + 80 + ax * 0.3f, c.y - 60, 380, 50), "SINAL  >>>", new Color(0.4f, 1f, 0.9f), 34);
                Title(new Rect(c.x + 80, c.y, 380, 40), "QUARTZ QUADRANT", new Color(1f, 1f, 1f, 0.9f), 26);
                break;
            }
            case "flyQuartz":  // voando por cima do Quartz Quadrant
                Pan(quartzA, r, 0.08f);
                Character(Frame(metalFly, 14f), c.x, c.y + 60 + Mathf.Sin(t * 4f) * 10f, 4.5f, false, 1f);
                SpeedLines(r, new Color(1f, 1f, 1f, 0.35f));
                Title(new Rect(r.x, r.y + 40, r.width, 50), "QUARTZ QUADRANT", new Color(0.8f, 1f, 0.9f, Mathf.Clamp01(pageTime - 0.5f)), 40);
                break;
            case "crystals":   // a caverna de cristais pulsando
            case "drained":    // ... e depois, os cristais apagados
            {
                bool dim = kind == "drained";
                Pan(quartzB, r, 0.02f);
                Fill(r, new Color(0f, 0.05f, 0.05f, dim ? 0.6f : 0.25f));
                Ground(quartzGround, r, ground, 0f, dim ? new Color(0.5f, 0.5f, 0.55f) : Color.white);
                Color tint = dim ? new Color(0.55f, 0.55f, 0.6f) : Color.Lerp(Color.white, new Color(0.6f, 1f, 0.8f), 0.5f + 0.5f * Mathf.Sin(t * 3f));
                for (int i = 0; i < 6; i++)
                {
                    float cx = r.x + 100 + i * (r.width - 200) / 5f;
                    if (!dim) Glow(new Vector2(cx, ground - 50), 70f, new Color(0.3f, 1f, 0.8f, 0.18f + 0.1f * Mathf.Sin(t * 3f + i)));
                    Character(i % 2 == 0 ? crystal : crystal2, cx, ground, 2.5f, false, 1f, tint);
                }
                if (!dim) Motes(r, new Color(0.5f, 1f, 0.9f), 30);
                Character(Frame(metalIdle, 8f), c.x, ground, 4f, false, 1f);
                break;
            }
            case "runQuartz":  // correndo pelas minas de cristal
                Pan(quartzB, r, 0.06f);
                Fill(r, new Color(0f, 0.05f, 0.05f, 0.25f));
                Ground(quartzGround, r, ground, t * 320f, Color.white);
                for (int i = 0; i < 4; i++)
                    Character(crystal, Mathf.Repeat(r.xMax - t * 320f + i * 360f, r.width + 200) - 100, ground, 2.5f, false, 1f);
                Character(Frame(metalRun, 14f), c.x - 60, ground, 4.5f, false, 1f);
                SpeedLines(r, new Color(0.6f, 1f, 0.9f, 0.25f));
                break;
            case "message":    // a mensagem do Eggman num monitor
                Fill(r, new Color(0.02f, 0.03f, 0.06f));
                Monitor(new Rect(c.x - 300, c.y - 160, 600, 320), 1f);
                break;
            case "transport":  // o transportador leva o Metal Sonic até o Stardust Speedway
                Pan(stardustA, r, 0.02f);
                Fill(r, new Color(0f, 0f, 0.1f, 0.35f));
                Fill(new Rect(c.x - 70, r.y, 140, r.height), new Color(0.5f, 0.8f, 1f, 0.25f + 0.1f * Mathf.Sin(t * 8f)));
                Fill(new Rect(c.x - 20, r.y, 40, r.height), new Color(0.9f, 1f, 1f, 0.4f));
                for (int i = 0; i < 24; i++)
                {
                    float py = r.yMax - Mathf.Repeat(t * 200f + i * 41f, r.height);
                    Fill(new Rect(c.x - 60 + (i * 37) % 120, py, 4, 10), new Color(0.8f, 1f, 1f, 0.7f));
                }
                Sprite(arrive[(int)(t * 4f) % arrive.Length], new Rect(c.x - 80, c.y - 110 - Mathf.Repeat(t * 40f, 60f), 160, 190), 1f);
                break;
            case "speedway":   // a rodovia do Stardust e, ao longe, o Mecha Sonic
                Pan(stardustA, r, 0.03f);
                Fill(r, new Color(0f, 0f, 0.15f, 0.3f));
                Ground(stardustGround, r, ground, 0f, Color.white);
                Character(horn, r.x + 160, ground, 3f, false, 1f);
                Character(horn, r.xMax - 200, ground, 3f, true, 1f);
                Character(Mecha(), c.x + 260, ground - 160 + Mathf.Sin(t * 2f) * 8f, 2.4f, false, 1f, new Color(0.15f, 0.15f, 0.25f));
                Glow(new Vector2(c.x + 248, ground - 259 + Mathf.Sin(t * 2f) * 8f), 18f, new Color(1f, 0.5f, 0.1f, 0.7f));
                Character(Frame(metalIdle, 8f), c.x - 220, ground, 4.5f, false, 1f);
                Title(new Rect(r.x, r.y + 40, r.width, 50), "STARDUST SPEEDWAY", new Color(1f, 0.85f, 1f, Mathf.Clamp01(pageTime - 0.5f)), 40);
                break;
            case "arena":      // Metal Sonic diante do Mecha Sonic
                Pan(stardustA, r, 0.02f);
                Ground(stardustGround, r, ground, 0f, Color.white);
                Character(Frame(metalIdle, 8f), c.x - 280, ground, 5f, false, 1f);
                Character(Mecha(), c.x + 260, ground - 20 + Mathf.Sin(t * 3f) * 8f, 5f, false, 1f);   // o sprite já olha para a esquerda (para o Metal Sonic)
                break;

            // ---------------- final ----------------
            case "defeat":     // o Mecha Sonic cai, entre explosões
                Pan(stardustA, r, 0.02f);
                Fill(r, new Color(1f, 0.5f, 0.1f, 0.15f));
                Ground(stardustGround, r, ground, 0f, Color.white);
                Fallen(mecha, c.x + 120, ground + 4, 3.9f);
                for (int i = 0; i < 3; i++)
                {
                    float ph = Mathf.Repeat(t * 1.3f + i * 0.33f, 1f);
                    Glow(new Vector2(c.x + 60 + i * 70, ground - 80 - i * 40), 30f + ph * 70f, new Color(1f, 0.7f - ph * 0.4f, 0.2f, 0.7f * (1f - ph)));
                }
                Character(Frame(metalIdle, 8f), c.x - 300, ground, 4.5f, false, 1f);
                Fill(r, new Color(1f, 1f, 1f, Mathf.Clamp01(0.8f - pageTime)));
                break;
            case "closing":    // o rasgo se fecha: o Bad Future encolhe até sumir
            {
                float close = Mathf.Clamp01(pageTime / 4f);
                float seam = r.x + r.width * Mathf.Lerp(0.5f, 1f, close);
                Level(levelA, r, LevelAStart);
                Rect right = new Rect(seam, r.y, r.xMax - seam, r.height);
                if (right.width > 1f) Level(levelB, right, LevelBStart);
                Crack(new Rect(seam - r.width / 2, r.y, r.width, r.height), Color.white, 1f - close);
                Glow(new Vector2(seam, c.y), 80f * (1f - close) + 10f, new Color(1f, 1f, 1f, 0.4f * (1f - close)));
                break;
            }
            case "stable":     // o Presente de volta, com o Metal Sonic
                PalmScene(r, ground, 0f, 0f);
                Motes(r, new Color(1f, 1f, 0.8f), 18);
                Title(new Rect(r.x, r.y + 40, r.width, 60), "ESTÁVEL", Color.white, 48);
                Sprite(metal[(int)(t * 8f) % metal.Length], new Rect(c.x - 120, ground - 270, 240, 280), 1f);
                break;
            case "leave":      // o Metal Sonic vai embora voando
            {
                PalmScene(r, ground, 0f, 0f);
                float up = Mathf.Repeat(pageTime * 110f, r.height + 300f);
                for (int i = 1; i < 6; i++)
                    Fill(new Rect(c.x - 20 + (up - i * 30) * 0.6f, ground - 120 - (up - i * 30), 40, 8), new Color(0.5f, 0.7f, 1f, 0.5f - i * 0.08f));
                Sprite(arrive[(int)(t * 4f) % arrive.Length], new Rect(c.x - 90 + up * 0.6f, ground - 210 - up, 180, 210), 1f);
                break;
            }
            case "credits":    // créditos: a equipe e o obrigado
            {
                Fill(r, new Color(0.02f, 0.03f, 0.1f));
                Stars(r, 70);
                // os nomes da equipe e o agradecimento, parados no meio da tela (aparecendo devagar)
                string[] lines = {
                    "Lorenzo Colonnese Chiganças",
                    "Orlando Nagrockis Bertholdo",
                    "Paulo Gabriel Gonçalves Leme",
                    "Morgana Rodriguez Zanetti",
                    "",
                    "Obrigado por jogar Tempus Gemini!" };
                float y0 = r.center.y - lines.Length * 46f / 2f;
                for (int i = 0; i < lines.Length; i++)
                {
                    bool thanks = i == lines.Length - 1;
                    Color col = thanks ? new Color(0.7f, 1f, 1f) : Color.white;
                    col.a = Mathf.Clamp01(pageTime * 1.5f - i * 0.25f);
                    GUI.Label(new Rect(r.x, y0 + i * 46, r.width, 46), lines[i], Center(thanks ? 34 : 28, col));
                }
                break;
            }
            default:           // fim: o emblema com o Metal Sonic, o logotipo e a pontuação
                Fill(r, new Color(0.02f, 0.04f, 0.12f));
                Stars(r, 60);
                Swirl(new Vector2(c.x, c.y - 20), 24);
                EmblemTitle(new Vector2(c.x, c.y - 60), 1.4f);
                Title(new Rect(r.x, r.yMax - 64, r.width, 50), "PONTUAÇÃO FINAL: " + GameManager.score, new Color(1f, 0.9f, 0.4f), 32);
                break;
        }
    }

    // ------------------------------------------------------------------
    // peças de cenário
    // ------------------------------------------------------------------
    // Palmtree Panic: um pedaço da nossa Fase 1 no Mundo A (foto do jogo)
    // (scroll = quanto o chão já andou: com scroll a foto passa inteira, correndo; dark = escurecer a cena)
    void PalmScene(Rect r, float ground, float scroll, float dark)
    {
        if (scroll > 0f) Level(levelA, r, scroll * 0.3f);
        else Level(levelA, r, LevelAStart);
        if (dark > 0f) Fill(r, new Color(0.05f, 0.02f, 0.1f, dark));
    }

    // onde as fotos da fase começam (em pixels da foto): trechos com chão (o buraco das pontes fica de fora)
    const float LevelAStart = 1300f, LevelBStart = 50f;

    // foto da fase cobrindo o retângulo na altura toda, a partir do pixel x da foto
    void Level(Texture2D tex, Rect r, float x)
    {
        if (tex == null) return;
        float vw = r.width * tex.height / r.height;     // quanto da largura da foto cabe no retângulo
        x = Mathf.Repeat(x, Mathf.Max(1f, tex.width - vw));
        GUI.DrawTextureWithTexCoords(r, tex, new Rect(x / tex.width, 0f, vw / tex.width, 1f));
    }

    // chão de blocos da zona (Quartz, Stardust): o bloco de 64 px repetido a partir de "ground"
    void Ground(Texture2D tex, Rect r, float ground, float scroll, Color tint)
    {
        if (tex == null) return;
        Color old = GUI.color;
        GUI.color = tint;
        float s = 96f, off = Mathf.Repeat(scroll, s);
        for (float x = -off; x < r.width; x += s)
            for (float y = ground; y < r.yMax; y += s) GUI.DrawTexture(new Rect(r.x + x, y, s, s), tex);
        GUI.color = old;
        Fill(new Rect(r.x, ground, r.width, 4), new Color(1f, 1f, 1f, 0.35f));
    }

    // fundo da zona cobrindo o retângulo, deslizando devagar de um lado para o outro
    void Pan(Texture2D tex, Rect r, float speed)
    {
        if (tex == null) return;
        float texAspect = (float)tex.width / tex.height, aspect = r.width / r.height;
        Rect uv;
        if (texAspect > aspect)
        {
            float uw = aspect / texAspect;
            uv = new Rect(Mathf.PingPong(t * speed, 1f - uw), 0f, uw, 1f);
        }
        else
        {
            float uh = texAspect / aspect;
            uv = new Rect(0f, Mathf.PingPong(t * speed, 1f - uh), 1f, uh);
        }
        GUI.DrawTextureWithTexCoords(r, tex, uv);
    }

    void Sky(Rect r, Color top, Color bottom)
    {
        for (int i = 0; i < 16; i++)
            Fill(new Rect(r.x, r.y + r.height * i / 16f, r.width, r.height / 16f + 1), Color.Lerp(top, bottom, i / 15f));
    }

    // personagem em pé no chão (feet = y do pé), na escala dada (pixels da arte x escala)
    void Character(Texture2D tex, float x, float feet, float scale, bool flip, float alpha, Color? tint = null)
    {
        if (tex == null) return;
        float w = tex.width * scale, h = tex.height * scale;
        Color old = GUI.color;
        Color c = tint ?? Color.white;
        c.a = alpha;
        GUI.color = c;
        Rect rr = new Rect(x - w / 2f, feet - h, w, h);
        if (flip) GUI.DrawTextureWithTexCoords(rr, tex, new Rect(1f, 0f, -1f, 1f));
        else GUI.DrawTexture(rr, tex);
        GUI.color = old;
    }

    Texture2D Frame(Texture2D[] list, float fps)
    {
        if (list == null || list.Length == 0) return null;
        return list[(int)(t * fps) % list.Length];
    }

    // quadro do Mecha Sonic: alterna o normal e o com brilho, como o chefe na fase
    Texture2D Mecha()
    {
        if (mechaHot == null || (int)(t * 4f) % 2 == 0) return mecha;
        return mechaHot;
    }

    // o Mecha Sonic caído: o mesmo sprite deitado no chão (girado 90° em volta do pé), piscando escuro
    void Fallen(Texture2D tex, float x, float feet, float scale)
    {
        if (tex == null) return;
        float w = tex.width * scale;
        Matrix4x4 old = GUI.matrix;
        Vector3 p = new Vector3(x, feet - w / 2f, 0f);     // gira na tela virtual (o RotateAroundPivot usa pixels da tela)
        GUI.matrix = old * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, 90f), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
        float k = 0.55f + 0.3f * Mathf.Abs(Mathf.Sin(t * 6f));
        Character(tex, x, feet, scale, false, 1f, new Color(k, k, k));
        GUI.matrix = old;
    }

    // a montagem da tela de título: o emblema com asas, o Metal Sonic dentro dele e o logotipo na frente
    void EmblemTitle(Vector2 c, float s)
    {
        Sprite(emblem, new Rect(c.x - 164 * s, c.y - 95 * s, 328 * s, 190 * s), 1f);
        Sprite(metal[(int)(t * 8f) % metal.Length], new Rect(c.x - 65.5f * s, c.y - 83 * s, 131 * s, 152 * s), 1f);
        Sprite(banner, new Rect(c.x - 139 * s, c.y + 45 * s, 278 * s, 95 * s), 1f);
    }

    // monitor com chiado e ondas de voz (a mensagem do Eggman)
    void Monitor(Rect sr, float strength)
    {
        Fill(sr, new Color(0.04f, 0.1f, 0.08f));
        for (int i = 0; i < sr.height / 4; i++)
            Fill(new Rect(sr.x, sr.y + i * 4, sr.width, 1), new Color(0.4f, 1f, 0.7f, 0.06f));
        int bars = Mathf.Max(6, (int)(sr.width / 14));
        float bw = (sr.width - 16) / bars;
        for (int b = 0; b < bars; b++)
        {
            float amp = Mathf.Abs(Mathf.Sin(t * 9f + b * 0.8f) * Mathf.Sin(t * 2.3f + b * 0.3f)) * sr.height * 0.3f * strength;
            Fill(new Rect(sr.x + 8 + b * bw, sr.center.y + 10 - amp, bw - 3, amp * 2), new Color(0.5f, 1f, 0.75f, 0.8f));
        }
        Frame(sr, new Color(0.5f, 0.9f, 0.7f), 4);
        if (sr.width > 200)
            Title(new Rect(sr.x, sr.y + 16, sr.width, 40), "MENSAGEM RECEBIDA", new Color(0.6f, 1f, 0.8f, 0.6f + 0.4f * Mathf.Sin(t * 6f)), 28);
    }

    // relógio do Tempus (o ponteiro gira com "turn")
    void Clock(Vector2 o, float rad, float turn)
    {
        Ring(o, rad, new Color(0.4f, 1f, 0.9f, 0.9f), 4);
        Ring(o, rad - 20, new Color(0.4f, 1f, 0.9f, 0.35f), 2);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI / 6f;
            Vector2 p = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (rad - 10);
            Fill(new Rect(p.x - 3, p.y - 3, 6, 6), new Color(0.7f, 1f, 1f));
        }
        float ha = -Mathf.PI / 2f + turn * 1.4f;
        for (int i = 0; i < 20; i++)
        {
            Vector2 p = o + new Vector2(Mathf.Cos(ha), Mathf.Sin(ha)) * (i * (rad - 30) / 20f);
            Fill(new Rect(p.x - 3, p.y - 3, 6, 6), new Color(1f, 0.4f, 0.3f));
        }
    }

    // ------------------------------------------------------------------
    // efeitos
    // ------------------------------------------------------------------
    // o rasgo entre as dimensões: uma linha que ondula de cima a baixo (amount = 0 a 1)
    void Crack(Rect r, Color col, float amount)
    {
        if (amount <= 0f) return;
        float bottom = r.y + r.height * amount;
        for (float y = r.y; y < bottom; y += 6f)
        {
            float x = r.center.x + Mathf.Sin(y * 0.09f + t * 2f) * 18f + Mathf.Sin(y * 0.31f) * 8f;
            Fill(new Rect(x - 9, y, 18, 6), new Color(col.r, col.g, col.b, 0.25f * col.a));
            Fill(new Rect(x - 2, y, 4, 6), col);
        }
    }

    void Ring(Vector2 o, float rad, Color col, float thick)
    {
        int n = Mathf.Max(24, (int)(rad * 0.6f));
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector2 p = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
            Fill(new Rect(p.x - thick / 2, p.y - thick / 2, thick, thick), col);
        }
    }

    // brilho redondo (discos transparentes, um dentro do outro)
    void Glow(Vector2 o, float rad, Color col)
    {
        Color c = col;
        c.a = col.a * 0.35f;
        for (int i = 4; i >= 1; i--)
        {
            float rr = rad * i / 4f;
            for (float j = -rr; j <= rr; j += 6f)
            {
                float half = Mathf.Sqrt(Mathf.Max(0f, rr * rr - j * j));
                Fill(new Rect(o.x - half, o.y + j, half * 2, 6), c);
            }
        }
    }

    // pontinhos de energia subindo
    void Motes(Rect r, Color col, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float x = r.x + Mathf.Repeat(i * 97.3f + Mathf.Sin(t + i) * 20f, r.width);
            float y = r.yMax - Mathf.Repeat(t * (30f + i % 5 * 12f) + i * 53f, r.height);
            float a = 0.4f + 0.4f * Mathf.Sin(t * 4f + i);
            Fill(new Rect(x, y, 4, 4), new Color(col.r, col.g, col.b, a));
        }
    }

    // faíscas saindo de um ponto
    void Sparks(Vector2 o, Color col, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float a = i * 2.4f + t * 7f, d = Mathf.Repeat(t * 160f + i * 13f, 60f);
            Fill(new Rect(o.x + Mathf.Cos(a) * d, o.y + Mathf.Sin(a) * d, 4, 4), col);
        }
    }

    // energia girando em espiral
    void Swirl(Vector2 o, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float a = t * 0.8f + i * 2.399f, d = 40f + i * 8f;
            Fill(new Rect(o.x + Mathf.Cos(a) * d, o.y + Mathf.Sin(a * 1.3f) * d * 0.5f, 5, 5), new Color(1f, 0.9f, 0.6f, 0.7f));
        }
    }

    void Stars(Rect r, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float x = r.x + Mathf.Repeat(i * 211.7f, r.width), y = r.y + Mathf.Repeat(i * 97.1f, r.height);
            Fill(new Rect(x, y, 2, 2), new Color(1f, 1f, 0.85f, 0.4f + 0.5f * Mathf.Abs(Mathf.Sin(t * 2f + i))));
        }
    }

    // linhas de velocidade passando da direita para a esquerda
    void SpeedLines(Rect r, Color col)
    {
        for (int i = 0; i < 18; i++)
        {
            float x = r.xMax - Mathf.Repeat(t * 900f + i * 173f, r.width + 300);
            Fill(new Rect(x, r.y + 30 + (i * 61) % (r.height - 60), 120 + i % 3 * 60, 3), col);
        }
    }

    // bordas escurecidas da ilustração
    void Vignette(Rect r)
    {
        Color c = new Color(0f, 0f, 0f, 0.07f);
        for (int i = 0; i < 8; i++)
        {
            float d = i * 10f;
            Fill(new Rect(r.x, r.y + d, r.width, 10), c);
            Fill(new Rect(r.x, r.yMax - d - 10, r.width, 10), c);
            Fill(new Rect(r.x + d, r.y, 10, r.height), c);
            Fill(new Rect(r.xMax - d - 10, r.y, 10, r.height), c);
        }
    }

    // ------------------------------------------------------------------
    // utilidades de desenho
    // ------------------------------------------------------------------
    // imagem inteira dentro do retângulo, sem distorcer (espelhada se flip)
    void Sprite(Texture2D tex, Rect r, float alpha, bool flip = false)
    {
        if (tex == null) return;
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        float s = Mathf.Min(r.width / tex.width, r.height / tex.height);
        Rect fit = new Rect(r.center.x - tex.width * s / 2f, r.center.y - tex.height * s / 2f, tex.width * s, tex.height * s);
        if (flip) GUI.DrawTextureWithTexCoords(fit, tex, new Rect(1f, 0f, -1f, 1f));
        else GUI.DrawTexture(fit, tex);
        GUI.color = old;
    }

    void Fill(Rect r, Color col)
    {
        Color old = GUI.color;
        GUI.color = col;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void Frame(Rect r, Color col, float k)
    {
        Fill(new Rect(r.x, r.y, r.width, k), col);
        Fill(new Rect(r.x, r.yMax - k, r.width, k), col);
        Fill(new Rect(r.x, r.y, k, r.height), col);
        Fill(new Rect(r.xMax - k, r.y, k, r.height), col);
    }

    Rect Shrink(Rect r, float d) => new Rect(r.x + d, r.y + d, r.width - 2 * d, r.height - 2 * d);

    GUIStyle Style(int size, Color col)
    {
        GUIStyle st = new GUIStyle(GUI.skin.label) { fontSize = size };
        st.normal.textColor = col;
        return st;
    }

    GUIStyle Center(int size, Color col)
    {
        GUIStyle st = Style(size, col);
        st.alignment = TextAnchor.MiddleCenter;
        return st;
    }

    // título com sombra
    void Title(Rect r, string s, Color col, int size = 40)
    {
        GUIStyle st = Center(size, new Color(0f, 0f, 0f, col.a * 0.7f));
        st.fontStyle = FontStyle.Bold;
        GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), s, st);
        st.normal.textColor = col;
        GUI.Label(r, s, st);
    }
}
