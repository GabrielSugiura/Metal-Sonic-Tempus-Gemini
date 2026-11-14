using UnityEngine;

// Arena do chefe: quando o Metal Sonic entra, as paredes dos lados fecham, a câmera para no meio da arena
// e, depois da cena de corte (Cutscene), o Mecha Sonic (Boss.cs) começa a luta. A vitória (Boss.Win) termina a fase.
// Durante a luta, de tempos em tempos aparece uma Cápsula Tempus num ponto da arena (no chão, que existe nos dois
// mundos, ou em cima de uma plataforma, só no mundo dela).
// Componentes: Box Collider 2D (Is Trigger) na entrada da arena.
public class BossArena : MonoBehaviour
{
    public Boss boss;
    public GameObject walls;                    // paredes da arena (desligadas até a luta começar)
    public float x0, x1, floorY;                // bordas e chão da arena
    public GameObject capsulePrefab;            // Cápsula Tempus
    public float capsuleEvery = 12f;            // segundos entre uma cápsula e outra
    public int maxCapsules = 2;                 // no máximo tantas na arena ao mesmo tempo
    private bool fighting = false;
    private float capsuleTimer;
    private System.Collections.Generic.List<GameObject> capsules = new System.Collections.Generic.List<GameObject>();
    private bool started = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (started || !other.CompareTag("Player")) return;
        started = true;
        walls.SetActive(true);
        CameraControl cam = Camera.main.GetComponent<CameraControl>();
        cam.minLimit.x = cam.maxLimit.x = (x0 + x1) / 2f;
        // a câmera se afasta até caber a arena inteira, com as duas paredes
        cam.targetSize = ((x1 - x0) / 2f + 0.8f) / Camera.main.aspect;
        cam.minLimit.y = Mathf.Min(cam.minLimit.y, floorY + cam.targetSize - 2f);
        // cena de corte antes da luta (Cutscene com overlay): ela chama StartFight() no fim
        Cutscene scene = GetComponent<Cutscene>();
        if (scene != null) scene.enabled = true;
        else StartFight();
    }

    public void StartFight()
    {
        fighting = true;
        capsuleTimer = capsuleEvery / 2f;       // a primeira aparece logo
        boss.gameObject.SetActive(true);
        boss.Begin(x0, x1, floorY);
        GameManager.ShowMessage("MECHA SONIC! Acerte-o depois da investida, quando ele ficar tonto");
    }

    void Update()
    {
        if (!fighting || capsulePrefab == null) return;
        if (!GameManager.bossActive && boss.state == "dead") { fighting = false; return; }
        capsules.RemoveAll(c => c == null);
        capsuleTimer -= Time.deltaTime;
        if (capsuleTimer > 0f || capsules.Count >= maxCapsules) return;
        capsuleTimer = capsuleEvery;
        // pontos da arena: no chão (os dois mundos) ou em cima das plataformas (Presente: perto das paredes;
        // Bad Future: a do meio)
        float w = x1 - x0;
        var spots = new (float x, float y, string world)[]
        {
            (x0 + w * 0.2f, floorY, "AB"), (x0 + w * 0.5f, floorY, "AB"), (x0 + w * 0.8f, floorY, "AB"),
            (x0 + 5f, floorY + 3.5f, "A"), (x1 - 5f, floorY + 3.5f, "A"), (x0 + w / 2f, floorY + 3.5f, "B"),
        };
        var spot = spots[Random.Range(0, spots.Length)];
        GameObject c = Instantiate(capsulePrefab, new Vector3(spot.x, spot.y + 0.8f, 0f), Quaternion.identity);
        WorldObject wo = c.GetComponent<WorldObject>();
        if (wo != null) { wo.world = spot.world; wo.Refresh(); }
        capsules.Add(c);
        GameManager.PlaySound("capsule");
    }
}
