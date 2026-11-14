using UnityEngine;
using UnityEngine.SceneManagement;

// Cena final: mostra o resultado e a pontuação, com botão para jogar de novo (como no exercício do Arkanoid).
public class EndScreen : MonoBehaviour
{
    public GUISkin layout;

    void OnGUI()
    {
        GUI.skin = layout;
        GUIStyle big = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, Screen.height / 2 - 140, Screen.width, 60), GameManager.result, big);
        GUI.Label(new Rect(0, Screen.height / 2 - 80, Screen.width, 60), "PONTUAÇÃO: " + GameManager.score, big);
        if (GUI.Button(new Rect(Screen.width / 2 - 80, Screen.height / 2, 160, 45), "Jogar de novo"))
        {
            GameManager.StartRun();
            SceneManager.LoadScene("Fase1_PalmtreePanic");
        }
        if (GUI.Button(new Rect(Screen.width / 2 - 80, Screen.height / 2 + 60, 160, 45), "Menu"))
            SceneManager.LoadScene("Menu");
    }
}
