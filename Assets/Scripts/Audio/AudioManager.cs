using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lazy-singleton audio system. Self-creating — call AudioManager.Instance from
/// anywhere and it spawns itself; no scene/prefab wiring.
///
/// CLIP SETUP (optional — the game runs silently until you add them):
///   Put audio files in  Assets/Resources/Audio/  with these names:
///     music.*          → looping background music
///     pickup.*         → resource collected
///     deposit.*        → resource dropped at a build site
///     build_complete.* → a build site finished
///     steal.*          → wolf stole resources
///     wolf_alert.*     → wolf starts chasing
///     ui_click.*       → button press
///   (.wav / .ogg / .mp3 all work. Free CC0 packs: kenney.nl/assets, freesound.org)
///
/// Missing clips are simply skipped — Play() is always safe to call.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ── Clip name constants (so callers don't pass raw strings) ───────────────
    public const string Music        = "music";
    public const string Pickup       = "pickup";
    public const string Deposit      = "deposit";
    public const string BuildComplete= "build_complete";
    public const string Steal        = "steal";
    public const string WolfAlert    = "wolf_alert";
    public const string UiClick      = "ui_click";

    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume   = 0.9f;

    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AudioManager");
                _instance = go.AddComponent<AudioManager>();
            }
            return _instance;
        }
    }

    private AudioSource _music;
    private AudioSource _sfx;
    private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
    private float _lastPlayTime = -1f;
    private string _lastClip;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        _music = gameObject.AddComponent<AudioSource>();
        _music.loop        = true;
        _music.playOnAwake = false;
        _music.volume      = musicVolume;

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;

        PlayMusic();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Plays a one-shot SFX by name. No-op if the clip isn't present.</summary>
    public void Play(string clipName, float volumeScale = 1f)
    {
        // De-dupe: ignore the exact same clip fired twice in the same frame-ish window
        if (clipName == _lastClip && Time.unscaledTime - _lastPlayTime < 0.04f) return;

        var clip = Load(clipName);
        if (clip == null) return;

        _lastClip = clipName;
        _lastPlayTime = Time.unscaledTime;
        _sfx.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    public void PlayMusic()
    {
        var clip = Load(Music);
        if (clip == null) return;
        _music.clip   = clip;
        _music.volume = musicVolume;
        _music.Play();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        if (_music != null) _music.volume = musicVolume;
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    AudioClip Load(string clipName)
    {
        if (_cache.TryGetValue(clipName, out var cached)) return cached;
        var clip = Resources.Load<AudioClip>("Audio/" + clipName); // null if absent
        _cache[clipName] = clip;
        return clip;
    }
}
