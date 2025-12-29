using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Clips")]
    public AudioClip pourSound;
    public AudioClip winSound;
    public AudioClip pickUpSound;

    [Header("Source")]
    public AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        GameEvents.OnPourStarted += PlayPourSound;
        GameEvents.OnLevelCompleted += PlayWinSound;
    }

    private void OnDestroy()
    {
        GameEvents.OnPourStarted -= PlayPourSound;
        GameEvents.OnLevelCompleted -= PlayWinSound;
    }

    public void PlayPickUp()
    {
        sfxSource.PlayOneShot(pickUpSound);
    }

    public void PlayPourSound() 
    {
        sfxSource.PlayOneShot(pourSound);
    }

    public void PlayWinSound()
    {
        sfxSource.PlayOneShot(winSound);
    }
}