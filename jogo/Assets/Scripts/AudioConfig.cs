using UnityEngine;

// Volumes do jogo (geral, música e efeitos), de 0 a 1. Ficam salvos no computador (PlayerPrefs) e valem em toda
// cena: o GameManager toca a música e os efeitos com eles, e o MusicVolume ajusta a música do Menu e das cenas.
// São mudados na tela de pausa (ESC).
public static class AudioConfig
{
    public static float master = 1f, music = 1f, effects = 1f;
    private static bool loaded = false;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        master = PlayerPrefs.GetFloat("volume_geral", 1f);
        music = PlayerPrefs.GetFloat("volume_musica", 1f);
        effects = PlayerPrefs.GetFloat("volume_efeitos", 1f);
        AudioListener.volume = master;
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat("volume_geral", master);
        PlayerPrefs.SetFloat("volume_musica", music);
        PlayerPrefs.SetFloat("volume_efeitos", effects);
        PlayerPrefs.Save();
        AudioListener.volume = master;
    }
}
