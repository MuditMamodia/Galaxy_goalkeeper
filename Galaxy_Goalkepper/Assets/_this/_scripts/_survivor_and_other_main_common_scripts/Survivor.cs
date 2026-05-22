
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AnsweredQuestion // Keep this if quiz part is still used
{
    public string _question, _answer;
    public bool _answered_correctly;
}

public class SaveData
{
    public List<AnsweredQuestion> _Answered_questions = new();
    public float _Time_played;
    public long _Score;
    public int _Lifetime_questions_faced_count,
                _Lifetime_correct_answers_count,
                _Session_questions_faced_count,
                _Session_correct_answers_count,
                _Playthrough_questions_faced_count,
                _Playthrough_correct_answers_count,
                _Level_1_stars_count,
                _Level_2_stars_count,
                _Level_3_stars_count,
                _Level_1_correct_answer_count,
                _Level_2_correct_answer_count,
                _Level_3_correct_answer_count,
                _Bonus_round_stars_count;
    public bool _Did_they_choose_to_move_on_to_the_bonus_round; // Did they choose to move on to the bonus round
    public string _Last_session_id;

    // Question pool management - ensures all questions are seen before repeating
    public List<int> _Shuffled_question_indices = new(); // Shuffled order of question indices
    public int _Current_question_pool_index; // Current position in the shuffled pool
}

public class Survivor : MonoBehaviour
{
    public static Survivor _S;

    [Header("Video Page")]
    public List<Button> _Video_page_buttons;
    internal bool _is_entry = true;
    public VideoController _Video_controller;

    [Header("First Instruction Page")]
    public GameObject _First_instruction_page_obj;

    [Header("QnA")]
    //public QuizManager _Quiz_Manager;
    internal int _question_index;


    public TMP_Text _Version_text;
    public List<AudioSource> _Audio_sources;
    public GameObject _Loading_obj;
    //public List<AudioClip> _Jump_sound;

    [Header("Sound Button Settings")]
    [Tooltip("List of sound button GameObjects (parent buttons)")]
    public List<GameObject> _Sound_buttons;
    [Tooltip("0 for off 1 for on")]
    public List<Sprite> _Sound_sprites;

    //[Header("Testing - Level Unlock")]
    //[Tooltip("Enable to unlock levels for testing")]
    //public bool enableLevelUnlock = false;
    //[Tooltip("Set to 0 to unlock ALL levels, or enter a specific level number (1-50) to unlock up to that level")]
    //[Range(0, 50)]
    //public int unlockUpToLevel = 0;

    //[Header("Testing - Booster Lock")]
    //[Tooltip("Enable to lock (hide) all 4 bottom boosters for all levels")]
    //public bool lockAllBoosters = false;

    //[Header("Testing - Override Moves")]
    //[Tooltip("Override starting moves for testing win/lose screens")]
    //public MovesOverrideMode movesOverride = MovesOverrideMode.None;

    public bool canplay;

    public enum MovesOverrideMode
    {
        None,           // Use normal level moves
        TestFail,       // Start with 3 moves (test out of moves screen)
        TestWin         // Start with 100 moves (test win screen easily)
    }

    [Header("Game Features")]
    [Tooltip("Disable the compliment text popup (Good/Super/Yummy) that shows during cascades")]
    public bool disableComplimentText = false;

    [Tooltip("Enable infinite lives - no life is lost when losing a level")]
    public bool infiniteLives = false;

    [Header("Testing - Striped Candy")]
    [Tooltip("Enable to load a special test level for testing all striped candy colors. When enabled, ANY level you select will load the striped candy test level instead.")]
    public bool testStripedCandies = false;

    [Header("Game Manager Reference")]
    //public GameManagerSpace.GameManager gameManager; // Reference to game manager for audio control

    internal SaveData _save_data = new();

    List<float> _default_audio_volumes = new();
    bool _is_muted, _started;

    internal int _points, _arrow_count = 3, _contestant_index = -1;
    internal bool _is_sound_on = true;
    internal float _game_timer;

    internal List<float> _default_volumes = new();

    /// <summary>
    /// mudit cahnges for the muting and unmutting the bg music
    /// </summary>
    private Coroutine bgMuteCoroutine;

    private float bg0OriginalVolume;
    private float bg5OriginalVolume;
    private bool bgMusicForcedMuted = false;

    private void OnEnable()
    {
        PlayBackgroundMusic();
    }


    void Awake()
    {
        if (_S == null)
        {
            _S = this;

            //StartCoroutine(AutoMuteUnmute());

            // DontDestroyOnLoad only works on ROOT GameObjects
            // Find the topmost parent and persist that instead
            Transform root = transform.root;
            DontDestroyOnLoad(root.gameObject);
            Debug.Log($"[Survivor] DontDestroyOnLoad called on root: {root.name}");

            _Audio_sources = GetComponentsInChildren<AudioSource>().ToList();
            if (_Version_text != null)
                _Version_text.text = Application.version;

            _Audio_sources.ForEach(x => _default_audio_volumes.Add(x.volume)); // Store default audio volumes

            // Find GameManager if not assigned
            /*if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManagerSpace.GameManager>();
            }*/

            // Load mute state from PlayerPrefs
            // Default sound state (not using PlayerPrefs anymore)
            _is_muted = false;

            // Apply initial sound state
            ApplySoundState();
            UpdateSoundButtonSprites();


        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // TEMPORARILY DISABLED - will implement API later
        // if (_is_entry)
        // {
        //     _Loading_obj.SetActive(true);
        // }
        if (_Loading_obj != null)
        {
            _Loading_obj.SetActive(false);
        }

        // Video button handling is done by main_menu_start_game_video.cs
        // Do NOT add listeners here - it conflicts with the proper video flow controller

        // Initialize the question pool system - ensures all questions are seen before repeating
    }

    void Update()
    {
        _game_timer += Time.deltaTime;
    }

    public void Button_Toggle_Sound()
    {
        // Toggle mute state
        _is_muted = !_is_muted;

        // Apply sound state
        ApplySoundState();

        // If BG music is force muted,
        // keep it muted no matter what
        if (bgMusicForcedMuted)
        {
            if (_Audio_sources.Count > 0)
            {
                _Audio_sources[0].volume = 0f;
            }

            if (_Audio_sources.Count > 5)
            {
                _Audio_sources[5].volume = 0f;
            }
        }

        // Update button sprites
        UpdateSoundButtonSprites();
    }

    //mudit function added
    public void play_audio_by_index(int index)
    {
        if (!canplay) return;
        AudioSource source = _Audio_sources[index];
        source.PlayOneShot(source.clip);

    }

    public void stop_audio_by_index(int index)
    {
        if (_Audio_sources == null || index < 0 || index >= _Audio_sources.Count) return;
        AudioSource source = _Audio_sources[index];
        if (source != null) source.Stop();
    }

    void ApplySoundState()
    {
        if (_is_muted)
        {
            _Audio_sources.ForEach(x => x.volume = 0);
        }
        else
        {
            for (int i = 0; i < _Audio_sources.Count; i++)
            {
                if (i < _default_audio_volumes.Count)
                {
                    // If BG is force muted
                    if (bgMusicForcedMuted && (i == 0 || i == 5))
                    {
                        _Audio_sources[i].volume = 0f;
                    }
                    else
                    {
                        _Audio_sources[i].volume = _default_audio_volumes[i];
                    }
                }
            }
        }
    }//mudit function closed







    /// <summary>
    /// Applies level unlock settings for testing purposes.
    /// Call this in Awake after singleton setup.
    /// </summary>
    //private void ApplyLevelUnlock()
    //{
    //    if (!enableLevelUnlock) return;

    //    if (unlockUpToLevel == 0)
    //    {
    //        // Unlock ALL levels
    //        PlayerPrefs.SetInt("next_level", 999);
    //        Debug.Log("<color=cyan>[TEST] All levels unlocked (next_level = 999)</color>");
    //    }
    //    else
    //    {
    //        // Unlock up to specific level
    //        PlayerPrefs.SetInt("next_level", unlockUpToLevel);
    //        Debug.Log($"<color=cyan>[TEST] Levels unlocked up to level {unlockUpToLevel}</color>");
    //    }
    //    PlayerPrefs.Save();
    //}

    private void UpdateSoundButtonSprites()
    {
        if (_Sound_sprites == null || _Sound_sprites.Count < 2)
        {
            Debug.LogError("Sound sprites not set! Please assign 2 sprites (0=off, 1=on) in Inspector");
            return;
        }

        // Update all sound button child images
        for (int i = 0; i < _Sound_buttons.Count; i++)
        {
            if (_Sound_buttons[i] == null)
            {
                Debug.LogWarning($"Sound button {i} is NULL!");
                continue;
            }

            // Get ALL Image components in this button and its children
            Image[] allImages = _Sound_buttons[i].GetComponentsInChildren<Image>();

            if (allImages.Length == 0)
            {
                Debug.LogError($"No Image components found in button {i} or its children!");
                continue;
            }

            // Try to find the child image (not the button itself)
            Image targetImage = null;

            // If there's more than one Image, assume the first child is the icon
            if (allImages.Length > 1)
            {
                targetImage = allImages[1]; // Skip first (parent button), use second (child icon)
            }
            else
            {
                targetImage = allImages[0]; // Only one image, use it
            }

            if (targetImage != null)
            {
                Sprite newSprite = _is_muted ? _Sound_sprites[0] : _Sound_sprites[1];
                targetImage.sprite = newSprite;
            }
        }
    }

    //internal IEnumerator Cor_load_scene_asynchronously(int p_index)
    //{
    //    // TEMPORARILY DISABLED - will implement API later
    //    // _Loading_obj.SetActive(true);
    //    AsyncOperation t_progress = SceneManager.LoadSceneAsync(p_index);

    //    while (!t_progress.isDone)
    //    {
    //        yield return null;
    //    }

    //    // _Loading_obj.SetActive(false);
    //}

    IEnumerator DuckBGMForAudio(AudioSource sfx, float duckPercentage = 0.3f)
    {
        // Get the BGM audio source (index 9)
        AudioSource bgm = _Audio_sources[0];

        // Use default volume as reference (not current volume which could be 0 if muted)
        float defaultBGMVolume = _default_audio_volumes[0];

        // Check if audio is currently muted (BGM volume is 0)
        bool isMutedAtStart = (bgm.volume == 0);

        if (!isMutedAtStart)
        {
            // Duck the BGM to specified percentage of default volume
            bgm.volume = defaultBGMVolume * duckPercentage;
        }

        // Play the sound effect
        sfx.Play();

        // Wait for the sound effect to finish playing
        yield return new WaitWhile(() => sfx.isPlaying);

        // Check if audio is currently muted (user may have toggled sound during ducking)
        // We check audio source 0 since Button_Toggle_Sound() sets all sources to 0 when muting
        bool isCurrentlyMuted = (_Audio_sources[0].volume == 0);

        // Restore BGM based on current mute state (not the saved state from start)
        bgm.volume = isCurrentlyMuted ? 0 : defaultBGMVolume;
    }



    public void PlayBackgroundMusic()
    {
        if (_Audio_sources == null || _Audio_sources.Count == 0)
            return;

        AudioSource bgm = _Audio_sources[0];

        if (bgm == null)
            return;

        if (!bgm.isPlaying)
        {
            bgm.loop = true;   // Ensure looping
            bgm.Play();
            Debug.Log("BGM Started (Index 0)");
        }
    }

    public void changing_sound_smoothly(int sound_1, int sound_2)
    {
        
        StartCoroutine(smoothsound_changer(sound_1, sound_2));
    }
    IEnumerator smoothsound_changer(int sound_1, int sound_2)
    {
        // Safety check
        if (sound_1 < 0 || sound_1 >= _Audio_sources.Count ||
            sound_2 < 0 || sound_2 >= _Audio_sources.Count)
        {
            yield break;
        }

        AudioSource oldSound = _Audio_sources[sound_1];
        AudioSource newSound = _Audio_sources[sound_2];

        // If muted → both sounds volume = 0
        if (_is_muted)
        {
            oldSound.volume = 0f;
            newSound.volume = 0f;

            if (!newSound.isPlaying)
            {
                newSound.Play();
            }

            oldSound.Stop();

            yield break;
        }

        // Store original volumes
        float oldStartVolume = oldSound.volume;
        float newTargetVolume = _default_audio_volumes[sound_2];

        // Start new sound with 0 volume
        newSound.volume = 0f;

        // Play new sound if not already playing
        if (!newSound.isPlaying)
        {
            newSound.Play();
        }

        float duration = 3f;
        float timer = 0f;

        while (timer < duration)
        {
            // If muted during transition
            if (_is_muted)
            {
                oldSound.volume = 0f;
                newSound.volume = 0f;

                yield return null;
                continue;
            }

            timer += Time.deltaTime;

            float t = timer / duration;

            // Fade out old sound
            oldSound.volume = Mathf.Lerp(oldStartVolume, 0f, t);

            // Fade in new sound
            newSound.volume = Mathf.Lerp(0f, newTargetVolume, t);

            yield return null;
        }

        // Final values
        oldSound.volume = 0f;
        newSound.volume = newTargetVolume;

        // Stop old sound
        oldSound.Stop();
    }

    //IEnumerator AutoMuteUnmute()
    //{
    //    yield return new WaitForSeconds(0.2f);

    //    // Mute
    //    _is_muted = true;
    //    ApplySoundState();
    //    UpdateSoundButtonSprites();
    //    Debug.Log("Auto Muted");

    //    yield return new WaitForSeconds(0.8f);

    //    // Unmute
    //    _is_muted = false;
    //    ApplySoundState();
    //    UpdateSoundButtonSprites();
    //    Debug.Log("Auto Unmuted");
    //}
    public void Mute_BG_Music_Smoothly()
    {
        bgMusicForcedMuted = true;

        if (bgMuteCoroutine != null)
        {
            StopCoroutine(bgMuteCoroutine);
        }

        bgMuteCoroutine = StartCoroutine(MuteBGCoroutine());
    }

    IEnumerator MuteBGCoroutine()
    {
        AudioSource bg1 = _Audio_sources[0];
        AudioSource bg2 = _Audio_sources[5];

        // Save original volumes
        bg0OriginalVolume = _default_audio_volumes[0];
        bg5OriginalVolume = _default_audio_volumes[5];

        float startVol1 = bg1.volume;
        float startVol2 = bg2.volume;

        float duration = 1.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;

            bg1.volume = Mathf.Lerp(startVol1, 0f, t);
            bg2.volume = Mathf.Lerp(startVol2, 0f, t);

            yield return null;
        }

        bg1.volume = 0f;
        bg2.volume = 0f;
    }
    public void Restore_BG_Music_Smoothly()
    {
        bgMusicForcedMuted = false;

        if (bgMuteCoroutine != null)
        {
            StopCoroutine(bgMuteCoroutine);
        }

        bgMuteCoroutine = StartCoroutine(RestoreBGCoroutine());
    }

    IEnumerator RestoreBGCoroutine()
    {
        AudioSource bg1 = _Audio_sources[0];
        AudioSource bg2 = _Audio_sources[5];

        float startVol1 = bg1.volume;
        float startVol2 = bg2.volume;

        float duration = 1.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;

            bg1.volume = Mathf.Lerp(startVol1, bg0OriginalVolume, t);
            bg2.volume = Mathf.Lerp(startVol2, bg5OriginalVolume, t);

            yield return null;
        }

        bg1.volume = bg0OriginalVolume;
        bg2.volume = bg5OriginalVolume;
    }
}
