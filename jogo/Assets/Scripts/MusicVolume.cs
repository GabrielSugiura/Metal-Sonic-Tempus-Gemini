using UnityEngine;

// Música de uma cena sem GameManager (Menu, cenas de corte, Fim): segue o volume da música salvo (AudioConfig).
// Componentes: AudioSource (com a música em loop).
public class MusicVolume : MonoBehaviour
{
    public float baseVolume = 0.45f;
    private AudioSource source;

    void Start()
    {
        AudioConfig.Load();
        source = GetComponent<AudioSource>();
    }

    void Update()
    {
        source.volume = baseVolume * AudioConfig.music;
    }
}
