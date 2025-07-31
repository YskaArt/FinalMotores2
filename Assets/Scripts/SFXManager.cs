using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    public AudioSource sfxSource;

    [Header("Clips de sonido")]
    public AudioClip doorOpenClip;
    public AudioClip addKeyClip;
    public AudioClip gameOverClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantener entre escenas si lo necesitás
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // Métodos convenientes para reproducir sonidos específicos
    public void PlayDoorOpen() => PlaySFX(doorOpenClip);
    public void PlayAddKey() => PlaySFX(addKeyClip);
    public void PlayGameOver() => PlaySFX(gameOverClip);
}
