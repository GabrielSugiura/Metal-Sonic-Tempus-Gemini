using UnityEngine;

// Parallax Scrolling (aula do SpaceShip), adaptado para seguir a câmera da fase:
// cada faixa do fundo anda uma fração (parallaxEffect) do movimento da câmera e se repete (as cópias ficam
// lado a lado como filhas). Coloque este script em cada faixa do Background.
// parallaxEffect: 1 = fica parada junto da câmera (muito longe), 0 = anda junto com o cenário.
// autoScroll: a faixa anda sozinha para a esquerda (nuvens), em unidades por segundo.
public class Parallax : MonoBehaviour
{
    public float parallaxEffect;
    public float verticalEffect = 0.9f;
    public float autoScroll = 0f;
    private float lenght;
    private float startX, startY, camStartX, camStartY, scroll;
    private Transform cam;

    // Awake (e não Start): roda antes da câmera pular para o ponto de controle quando a fase recomeça,
    // então guarda sempre a posição da câmera no começo da fase
    void Awake()
    {
        cam = Camera.main.transform;
        startX = transform.position.x;
        startY = transform.position.y;
        camStartX = cam.position.x;
        camStartY = cam.position.y;
        lenght = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void LateUpdate()
    {
        scroll += autoScroll * Time.deltaTime;
        // onde a faixa estaria: anda uma fração do que a câmera andou desde o começo
        float x = startX + (cam.position.x - camStartX) * parallaxEffect - scroll;
        // repete a imagem: pula de uma largura em uma largura para a câmera ficar sempre sobre as cópias
        x += Mathf.Floor((cam.position.x - x) / lenght) * lenght;
        transform.position = new Vector3(x, startY + (cam.position.y - camStartY) * verticalEffect, transform.position.z);
    }
}
