
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
                    _Audio_sources[i].volume = _default_audio_volumes[i];
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

}
