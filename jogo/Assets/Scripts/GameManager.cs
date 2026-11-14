using UnityEngine;
using UnityEngine.SceneManagement;

// Gerência da partida (como o GameManager do Pong): placar, anéis, Tempus, teleporte,
// dimensão ativa, HUD com OnGUI, pausa e troca de cenas (como no Arkanoid).
// Fica num objeto vazio chamado "GameManager" em cada fase.
public class GameManager : MonoBehaviour
{
    // ---------- BALANCEAMENTO (valores "a definir no protótipo" do GDD) ----------
    public static float swapCost = 6f;          // Tempus gasto por troca de mundo (segundos)
    public static float capsuleGain = 15f;      // Tempus devolvido por Cápsula Tempus
    public static float teleportMax = 100f;     // barra de teleporte cheia
    public static float teleportRecharge = 25f; // por segundo (4 s para encher)
    public static float energyPerRing = 12f;    // cada anel recarrega o teleporte
    public static int ringMonitorAmount = 10;

    // ---------- ESTADO DA PARTIDA ----------
    public static string world = "A";           // dimensão ativa: "A" ou "B"
    public static float tempus = 100f;
    public static float tempusMax = 100f;
    public static float carriedTempus = -1f;   // Tempus que sobrou da fase anterior (-1 = começar com o da fase)
    public static float minLevelTempus = 60f;   // uma fase nova começa com pelo menos isto (para não ficar impossível)
    public static float levelStartTempus;       // o Tempus com que se entrou nesta fase (morrer recomeça com ele)
    private static string levelScene = "";
    public static float teleportEnergy = 100f;
    public static int rings = 0;
    public static int score = 0;
    public static bool shield = false;
    public static bool paused = false;
    public static string message = "";
    public static float messageTimer = 0f;
    public static bool levelFinished = false;
    public static string result = "";           // lido pela cena "Fim"
    public static bool hasCheckpoint = false;   // passou por um ponto de controle (Checkpoint.cs)?
    public static Vector2 checkpoint;           // onde o Metal Sonic renasce depois de morrer
    public static bool bossActive = false;      // luta com o chefe (Boss.cs) em andamento: mostra a vida dele
    public static int bossHp, bossHpMax = 6;
    public static string bossWorld = "A";      // mundo em que o chefe está (a cor da aura e da barra)

    public float startTempus = 420f;            // Tempus de quem abre esta fase direto (o jogo começa com 7 minutos)
    public string nextScene = "Fim";            // cena carregada ao terminar a fase
    public bool infiniteTempus = false;         // ligue para testar a fase com calma
    public GUISkin layout;                      // fonte do HUD (opcional, como no Pong)
    public GameObject lostRingPrefab;           // anel que quica ao tomar dano

    static GameManager instance;
    static AudioSource audioSource;
    public AudioClip[] sounds;                  // jump, dash, spring, ring, hurt, teleport, swap, deny...
    public AudioClip music;                     // música da fase (toca em loop)
    public AudioClip bossMusic;                 // música da luta com o chefe (Boss.Begin troca para ela)
    private static AudioSource musicSource;

    // Roda uma vez quando o jogo começa (e a cada vez que se aperta Play no Editor). O projeto não recarrega
    // os scripts ao entrar no Play, então sem isto o ponto de controle e o placar da última partida ficavam
    // guardados e a fase começava no checkpoint em vez do início.
    // partida nova (Jogar no Menu, Jogar de novo no Fim): placar zerado e o Tempus cheio na Fase 1
    public static void StartRun()
    {
        score = 0;
        hasCheckpoint = false;
        carriedTempus = -1f;
        levelScene = "";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void NewGame()
    {
        carriedTempus = -1f;
        levelScene = "";
        Cutscene.playing = false;
        Cutscene.autoSkip = false;
        hasCheckpoint = false;
        score = 0;
        bossActive = false;
        world = "A";
        result = "";
        message = "";
        messageTimer = 0f;
    }

    void Start()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();
        AudioConfig.Load();                     // os volumes salvos (geral, música, efeitos)
        // música: uma segunda fonte de som, em loop
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.45f * AudioConfig.music;
        PlayMusic(music);
        world = "A";
        // o Tempus que sobrou da fase anterior continua; morrer recomeça a fase com o Tempus com que se entrou nela
        string scene = SceneManager.GetActiveScene().name;
        if (carriedTempus >= 0f) { levelStartTempus = Mathf.Max(carriedTempus, minLevelTempus); carriedTempus = -1f; }
        else if (scene != levelScene) levelStartTempus = startTempus;
        levelScene = scene;
        tempus = levelStartTempus;
        tempusMax = Mathf.Max(startTempus, levelStartTempus);
        teleportEnergy = teleportMax;
        rings = 0;
        shield = false;
        paused = false;
        levelFinished = false;
        bossActive = false;
        Time.timeScale = 1f;
        ApplyWorld();
    }

    void Update()
    {
        // o ESC que pula uma cena de corte não abre a pausa
        if (musicSource != null) musicSource.volume = 0.45f * AudioConfig.music;
        if (Input.GetKeyDown(KeyCode.Escape) && !Cutscene.playing && Cutscene.endedFrame != Time.frameCount) TogglePause();
        if (paused || levelFinished) return;

        // Tempus: contagem regressiva contínua; zero = derrota
        if (!infiniteTempus) tempus -= Time.deltaTime;
        if (tempus <= 0f)
        {
            tempus = 0f;
            EndLevel(false, "O Tempus acabou!");
        }
        teleportEnergy = Mathf.Min(teleportMax, teleportEnergy + teleportRecharge * Time.deltaTime);
        messageTimer -= Time.deltaTime;
    }

    // ---------- Itens ----------
    public static void AddRings(int amount)
    {
        rings += amount;
        score += 10 * amount;
        teleportEnergy = Mathf.Min(teleportMax, teleportEnergy + energyPerRing * amount);
        PlaySound("ring");
    }

    public static void AddTempus(float seconds)
    {
        tempus = Mathf.Min(tempusMax, tempus + seconds);
    }

    // Ao tomar dano, até 10 anéis saem quicando
    public static void LoseRings(Vector3 position)
    {
        int n = Mathf.Min(rings, 10);
        rings = 0;
        if (n > 0) PlaySound("ringloss");
        if (instance == null || instance.lostRingPrefab == null) return;
        for (int i = 0; i < n; i++)
        {
            GameObject r = Instantiate(instance.lostRingPrefab, position, Quaternion.identity);
            r.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(Random.Range(-7f, 7f), Random.Range(6f, 13f));
        }
    }

    // ---------- Dimensões ----------
    public static void SwapWorld(string newWorld)
    {
        tempus -= swapCost;
        world = newWorld;
        PlaySound("swap");
        ApplyWorld();
    }

    // Avisa todos os objetos com WorldObject (tilemaps, inimigos, itens) que o mundo mudou
    static void ApplyWorld()
    {
        foreach (WorldObject w in FindObjectsByType<WorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            w.Refresh();
    }

    // ---------- Fim da fase ----------
    public static void PlayerDied()
    {
        PlaySound("hurt");
        if (instance != null) instance.Invoke("RestartLevel", 2f);
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // a música da luta com o chefe (Boss.Begin)
    public static void PlayBossMusic()
    {
        if (instance != null) PlayMusic(instance.bossMusic);
    }

    // troca a música (a fase, a luta com o chefe)
    public static void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public static void EndLevel(bool victory, string text)
    {
        if (levelFinished) return;
        levelFinished = true;
        result = text;
        if (instance == null) return;
        if (victory)
        {
            score += Mathf.RoundToInt(tempus) * 10;
            instance.Invoke("LoadNextScene", 2f);
        }
        else instance.Invoke("RestartLevel", 3f);
    }

    void LoadNextScene()
    {
        hasCheckpoint = false;                  // a próxima fase começa do início
        carriedTempus = tempus;                 // e com o Tempus que sobrou desta
        SceneManager.LoadScene(nextScene);
    }

    // tela de pausa (ESC): continuar, reiniciar, menu e os volumes (geral, música, efeitos), que ficam salvos.
    // Desenhada numa tela virtual de 720 de altura, escalada para a resolução do jogo.
    void PauseMenu()
    {
        Matrix4x4 old = GUI.matrix;
        float k = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));
        float w = Screen.width / k, cx = w / 2f;
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0.08f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, w, 720), Texture2D.whiteTexture);
        GUI.color = new Color(0.04f, 0.07f, 0.18f, 0.95f);
        GUI.DrawTexture(new Rect(cx - 230, 110, 460, 500), Texture2D.whiteTexture);
        GUI.color = oldColor;

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 44, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 22 };
        GUI.Label(new Rect(cx - 230, 125, 460, 60), "PAUSA", title);
        if (GUI.Button(new Rect(cx - 110, 200, 220, 46), "Continuar", button)) TogglePause();
        if (GUI.Button(new Rect(cx - 110, 256, 220, 46), "Reiniciar", button)) { TogglePause(); RestartLevel(); }
        if (GUI.Button(new Rect(cx - 110, 312, 220, 46), "Menu", button)) { TogglePause(); hasCheckpoint = false; SceneManager.LoadScene("Menu"); }

        // áudio: cada barra vai de 0 a 100%; mudar salva na hora
        GUI.Label(new Rect(cx - 200, 380, 400, 30), "ÁUDIO", label);
        float master = Slider(cx, 420, "Volume geral", AudioConfig.master, label);
        float music = Slider(cx, 470, "Música", AudioConfig.music, label);
        float effects = Slider(cx, 520, "Efeitos", AudioConfig.effects, label);
        if (master != AudioConfig.master || music != AudioConfig.music || effects != AudioConfig.effects)
        {
            bool testEffect = effects != AudioConfig.effects;
            AudioConfig.master = master;
            AudioConfig.music = music;
            AudioConfig.effects = effects;
            AudioConfig.Save();
            // mexendo nos efeitos, toca o som do anel para ouvir o volume (no máximo a cada 0,2 s)
            if (testEffect && Time.unscaledTime - lastEffectTest > 0.2f) { lastEffectTest = Time.unscaledTime; PlaySound("ring"); }
        }
        GUI.matrix = old;
    }

    private float lastEffectTest;

    float Slider(float cx, float y, string name, float value, GUIStyle label)
    {
        GUI.Label(new Rect(cx - 200, y - 4, 150, 30), name, label);
        float v = GUI.HorizontalSlider(new Rect(cx - 40, y + 6, 180, 20), value, 0f, 1f);
        GUI.Label(new Rect(cx + 150, y - 4, 60, 30), Mathf.RoundToInt(v * 100f) + "%", label);
        return v;
    }

    void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    // ---------- Mensagens e sons ----------
    public static void ShowMessage(string text)
    {
        message = text;
        messageTimer = 2.5f;
        PlaySound("deny");
    }

    public static void PlaySound(string name)
    {
        if (instance == null || audioSource == null || instance.sounds == null) return;
        foreach (AudioClip c in instance.sounds)
            if (c != null && c.name == name) audioSource.PlayOneShot(c, AudioConfig.effects);
    }

    // ---------- HUD ----------
    void OnGUI()
    {
        GUI.skin = layout;
        GUIStyle big = new GUIStyle(GUI.skin.label) { fontSize = 22, richText = true };
        GUI.Label(new Rect(20, 10, 400, 30), "PONTOS  " + score, big);
        GUI.Label(new Rect(20, 40, 400, 30), "ANÉIS   " + rings + (shield ? "   <color=#66CCFF>[ESCUDO]</color>" : ""), big);
        string tempusColor = tempus < tempusMax * 0.2f ? "#FF5555" : "#FFFFFF";
        GUI.Label(new Rect(20, 70, 400, 30), "TEMPUS  <color=" + tempusColor + ">" + tempus.ToString("0.0") + "</color>", big);
        string worldColor = world == "A" ? "#7CFC7C" : "#FFA030";
        GUI.Label(new Rect(20, 100, 400, 30), "MUNDO   <color=" + worldColor + ">" + world + "</color>", big);

        // barra de teleporte
        GUI.Label(new Rect(20, 130, 120, 30), "TELEPORTE", big);
        GUI.Box(new Rect(150, 138, 200, 16), "");
        Color old = GUI.color;
        GUI.color = teleportEnergy >= teleportMax ? Color.cyan : new Color(0.3f, 0.5f, 1f);
        GUI.DrawTexture(new Rect(150, 138, 200 * teleportEnergy / teleportMax, 16), Texture2D.whiteTexture);
        GUI.color = old;

        // vida do chefe
        if (bossActive)
        {
            string bossColor = bossWorld == "A" ? "#7CFC7C" : "#FFA030";
            string where = bossWorld == "A" ? "PRESENTE (A)" : "BAD FUTURE (B)";
            GUI.Label(new Rect(Screen.width / 2 - 150, 10, 600, 30), "MECHA SONIC  <color=" + bossColor + ">no " + where + "</color>" + (bossWorld != world ? "   aperte X" : ""), big);
            GUI.Box(new Rect(Screen.width / 2 - 150, 42, 300, 16), "");
            GUI.color = bossWorld == "A" ? new Color(0.45f, 1f, 0.45f) : new Color(1f, 0.6f, 0.15f);
            GUI.DrawTexture(new Rect(Screen.width / 2 - 150, 42, 300f * bossHp / bossHpMax, 16), Texture2D.whiteTexture);
            GUI.color = old;
        }

        if (messageTimer > 0f)
            GUI.Label(new Rect(Screen.width / 2 - 300, Screen.height - 80, 600, 40), message, big);

        if (paused) PauseMenu();
        if (levelFinished)
            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 40, 600, 60), "<size=34>" + result + "</size>", big);
    }
}
