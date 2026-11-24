using UnityEngine;

public class SoundManager : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] AudioClip explosionClip, buttonClip;

    // Player audio variables
    [SerializeField] private AudioClip shootClip, jumpClip, footStepClip,
    reloadClip, emptyClip, reviveClip;

    public static SoundManager Instance;


    /// Awake is called when the script instance is being loaded.
    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        
        DontDestroyOnLoad(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GameObject.Find("Sfx Audiosource").GetComponent<AudioSource>(); 
    }

    public void PlayExplosionClip()
    {
        audioSource.PlayOneShot(explosionClip);
    }

    public void PlayCkickSound()
    {
        audioSource.PlayOneShot(buttonClip);
    }

    public void PlayJumpSound()
    {
        audioSource.PlayOneShot(jumpClip, 0.55f);
    }

    public void PlayShootSound()
    {
        audioSource.PlayOneShot(shootClip);
    }

    public void PlayReviveSound()
    {
        audioSource.PlayOneShot(reviveClip, 0.65f);
    }

    public void PlayEmptyClipSound()
    {
        audioSource.PlayOneShot(emptyClip);
    }

    public void PlayReloadSound()
    {
        audioSource.PlayOneShot(reloadClip, 0.65f);
    }
}