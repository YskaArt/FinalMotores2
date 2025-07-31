using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Clips")]
    public AudioClip menuMusic;
    public AudioClip standardGameplayMusic;
    public AudioClip warningMusic;

    [Header("Ajustes")]
    public float fadeDuration = 1.5f;

    private AudioSource audioSource;
    private AudioClip currentClip;
    private bool isGameplay = false;
    private bool isAlert = false;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Crear el AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        // Por defecto arrancamos en una escena de menú
        PlayMusic(menuMusic);
    }

    /// <summary>
    /// Define si la escena actual es de gameplay o no (llamado por GameManager)
    /// </summary>
    public void SetSceneType(bool isGameplayScene)
    {
        isGameplay = isGameplayScene;

        if (isGameplay)
        {
            if (!isAlert)
                PlayMusic(standardGameplayMusic);
        }
        else
        {
            isAlert = false;
            PlayMusic(menuMusic);
        }
    }

    /// <summary>
    /// Llamá esto desde tu GameManager cuando suba o baje el nivel de alerta.
    /// </summary>
    public void UpdateAlertLevel(float level)
    {
        if (isGameplay)
        {
            if (level >= 1)
            {
                if (!isAlert)
                {
                    isAlert = true;
                    PlayMusic(warningMusic);
                }
            }
            else
            {
                if (isAlert)
                {
                    isAlert = false;
                    PlayMusic(standardGameplayMusic);
                }
            }
        }
    }

    private void PlayMusic(AudioClip newClip)
    {
        if (newClip == null || newClip == currentClip) return;

        StopAllCoroutines();
        StartCoroutine(SwitchTrack(newClip));
    }

    private System.Collections.IEnumerator SwitchTrack(AudioClip newClip)
    {
        // Fade out
        float time = 0f;
        float startVolume = audioSource.volume;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade in
        time = 0f;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, startVolume, time / fadeDuration);
            yield return null;
        }

        audioSource.volume = startVolume;
        currentClip = newClip;
    }
}
