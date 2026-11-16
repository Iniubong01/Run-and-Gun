using UnityEngine;

public class SoundManager : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] AudioClip explosionClip, buttonClip;
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
}