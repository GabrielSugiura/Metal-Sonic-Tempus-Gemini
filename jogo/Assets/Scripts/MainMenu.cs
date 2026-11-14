using UnityEngine;
using UnityEngine.SceneManagement;

// Tela de título (GDD): Jogar, Controles, Sair. Feita com OnGUI, como o placar do Pong.
// A arte (emblema com o Metal Sonic dentro, o logotipo, céu, nuvens, horizonte e mar) são sprites na cena
// (LevelBuilder.BuildMenu); aqui o Metal Sonic é animado e as nuvens e o mar andam devagar.
public class MainMenu : MonoBehaviour
{
    public string firstLevel = "Cena_Abertura";
    public GUISkin layout;
    public SpriteRenderer metal;                // Metal Sonic dentro do emblema
    public Sprite[] metalFrames;                // balançando o dedo (sonic_0..6)
    public Transform clouds, water;             // camadas que andam
    public float frameTime = 0.12f;
    private bool showControls = false;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (metal != null && metalFrames.Length > 0) metal.sprite = metalFrames[(int)(timer / frameTime) % metalFrames.Length];
        // nuvens e mar andam e voltam ao começo quando passam um pedaço (as cópias ficam lado a lado)
        if (clouds != null) clouds.position = new Vector3(-Mathf.Repeat(timer * 0.4f, 31.2f), clouds.position.y, 0f);
        if (water != null) water.position = new Vector3(-Mathf.Repeat(timer * 1.2f, 20.1875f), water.position.y, 0f);
    }

    void OnGUI()
    {
        GUI.skin = layout;
        // tela virtual de 720 de altura, escalada para a resolução do jogo
        float k = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));
        float w = Screen.width / k;
        GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 24 };
        GUIStyle text = new GUIStyle(GUI.skin.label) { fontSize = 22 };

        if (showControls)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(w / 2 - 340, 120, 680, 470), Texture2D.whiteTexture);
            GUI.color = old;
            string controls =
                "A / D  -  mover\n" +
                "W  -  olhar para cima   (parado: W + SHIFT = Super Peel Out)\n" +
                "S  -  rolar / agachar   (S + ESPAÇO = Spindash)\n" +
                "ESPAÇO  -  pular (segure no ar: hover curto)\n" +
                "SHIFT  -  investida\n" +
                "V  -  teleporte (W + V: para cima)\n" +
                "X  -  trocar de dimensão\n" +
                "ESC  -  pausa";
            GUI.Label(new Rect(w / 2 - 310, 150, 640, 330), controls, text);
            if (GUI.Button(new Rect(w / 2 - 100, 510, 200, 50), "Voltar", button)) showControls = false;
            return;
        }

        if (GUI.Button(new Rect(w / 2 - 100, 470, 200, 50), "Jogar", button))
        {
            GameManager.score = 0;
            GameManager.StartRun();
            SceneManager.LoadScene(firstLevel);
        }
        if (GUI.Button(new Rect(w / 2 - 100, 530, 200, 50), "Controles", button)) showControls = true;
        if (GUI.Button(new Rect(w / 2 - 100, 590, 200, 50), "Sair", button)) Application.Quit();
    }
}
