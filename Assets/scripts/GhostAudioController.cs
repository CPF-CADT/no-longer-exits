using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GhostAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip idleSound;      // Looping whispers/chains
    public AudioClip chaseSound;     // Looping heartbeat/aggressive
    public AudioClip scareSound;     // One-shot Scream
    public AudioClip banishSound;    // One-shot Pain/Vanish

    [Header("3D Settings (Tweak these while playing)")]
    [Tooltip("Inside this radius, sound is 100% loud. Keep small (e.g. 1-3).")]
    public float minDistance = 2f;
    [Tooltip("At this radius, sound becomes completely silent. (e.g. 10-15).")]
    public float maxDistance = 25f;
    [Tooltip("Volume multiplier (0 to 1)")]
    [Range(0f, 1f)] public float volume = 1f;

    private AudioSource audioSource;
    private bool isScaringOrBanished = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Force 3D settings
        audioSource.spatialBlend = 1.0f; // 1.0 = Fully 3D sound
        audioSource.rolloffMode = AudioRolloffMode.Linear; // Linear fades out completely at MaxDistance
        audioSource.loop = true;
    }

    private void Start()
    {
        PlayIdle();
    }

    private void Update()
    {
        // This allows you to change distance settings in the Inspector while the game plays
        if (audioSource != null)
        {
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.volume = volume;
        }
    }

    public void UpdateState(bool isChasing)
    {
        if (isScaringOrBanished) return;

        if (isChasing)
        {
            if (audioSource.clip != chaseSound)
            {
                audioSource.clip = chaseSound;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.clip != idleSound)
            {
                audioSource.clip = idleSound;
                audioSource.Play();
            }
        }
    }

    public void PlayScare()
    {
        isScaringOrBanished = true;
        audioSource.Stop();
        if (scareSound != null) audioSource.PlayOneShot(scareSound);
    }

    public void PlayBanish()
    {
        isScaringOrBanished = true;
        audioSource.Stop();
        if (banishSound != null) audioSource.PlayOneShot(banishSound);
    }

    public void StopAudio()
    {
        audioSource.Stop();
    }

    public void ResetAudio()
    {
        isScaringOrBanished = false;
        PlayIdle();
    }

    private void PlayIdle()
    {
        if (idleSound != null)
        {
            audioSource.clip = idleSound;
            audioSource.Play();
        }
    }
    
    // Shows the sound range as a Blue Sphere in the Scene View
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0, 1, 0.3f); // Blue transparent
        Gizmos.DrawWireSphere(transform.position, maxDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}