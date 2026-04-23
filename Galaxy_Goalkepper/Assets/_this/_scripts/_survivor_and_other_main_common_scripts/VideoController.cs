using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public VideoPlayer _Video_player;
    public List<string> videoFileNames; // Name of your MP4 in StreamingAssets

    public List<AudioClip> audioClips; // List of audio clips to play with the video
                                       //public GameObject continueButton; // Continue button to enable when video ends



    private bool _audioStarted;

    //mudit adding start here ---------------------------------
    public bool can_show_exit_tab_text;// can be removed
    public GameObject exit_tab_text;// can be removed
    // and ends here --------------------------

    private void Start()
    {
       
    }

    private void OnEnable()
    {
        Play_video(0);
        exit_tab_text.SetActive(false);
    }



    internal void Play_video(int p_index)
    {
        _Video_player.gameObject.SetActive(true); // Activate the VideoPlayer GameObject
        Survivor._S._Loading_obj.SetActive(false);
        //Survivor._S._Video_page_buttons[0].gameObject.SetActive(true);  //  enable skip button

        // Disable continue button at the start of video
        //if (continueButton != null)
        //{
        //    continueButton.SetActive(false);
        //}

        // Set the video URL to StreamingAssets path
        string videoPath = Path.Combine(Application.streamingAssetsPath, videoFileNames[p_index]) + ".webm";
        _Video_player.url = videoPath;

        // Mute the video player's embedded audio (we use AudioSource for audio control)
        _Video_player.SetDirectAudioMute(0, true);

        Survivor._S._Audio_sources[1].clip = audioClips[p_index]; // Set the audio clip to play

        // Prepare and play the video
        _audioStarted = false;
        _Video_player.waitForFirstFrame = true; // ensure first frame is ready before display
        _Video_player.sendFrameReadyEvents = true; // we'll start audio on first rendered frame

        // unsubscribe first to avoid duplicate handlers if Play_Video is called repeatedly
        _Video_player.prepareCompleted -= OnVideoPrepared;
        _Video_player.frameReady -= OnFirstFrameReady;
        _Video_player.loopPointReached -= OnLoopPointReached;

        _Video_player.prepareCompleted += OnVideoPrepared;
        _Video_player.frameReady += OnFirstFrameReady;
        _Video_player.loopPointReached += OnLoopPointReached;

        _Video_player.Prepare();
    }

    internal void Stop_video()
    {
        _Video_player.Stop();

        // Stop the video audio clip
        Survivor._S._Audio_sources[1].Stop();

        //Survivor._S._Audio_sources[0].Play(); // Resume background music

        //_Video_player.gameObject.SetActive(false); // Deactivate the VideoPlayer GameObject
    }

    void OnVideoPrepared(VideoPlayer source)
    {
        // Stop background music right when starting playback
        Time.timeScale = 1f; // ensure normal speed when video starts

        //Survivor._S._Audio_sources[0].Pause(); // Ensure background music is stopped

        _Video_player.Play();
    }

    void OnFirstFrameReady(VideoPlayer source, long frameIdx)
    {
        if (_audioStarted)
        {
            return;
        }

        // First frame reached: start the companion audio now
        _audioStarted = true;
        Survivor._S._Audio_sources[1].Play();

        // No longer need per-frame callbacks
        _Video_player.sendFrameReadyEvents = false;
        _Video_player.frameReady -= OnFirstFrameReady;
    }

    void OnLoopPointReached(VideoPlayer source)
    {
        // Enable continue button when video ends naturally
        //if (continueButton != null)
        //{
        //    continueButton.SetActive(true);
        //}

        //Survivor._S._Video_page_buttons[0].gameObject.SetActive(false);  // disable skip button

        //mudit adding start here ---------------------------------
        //mudit adding start here ---------------------------------
        Debug.Log("Video Ended!");
        exit_tab_text.SetActive(true);

        //if (can_show_exit_tab_text)
        //{
        //    Debug.Log("Showing exit text");

        //}
        // and ends here --------------------------

        Stop_video();
    }

    //void OnEnable()
    //{
    //    //if (Survivor._S._is_entry)
    //    //{
    //    //    Play_video(0);
    //    //}
    //    //else
    //    //{
    //    //    Play_video(1);
    //    //}
    //    Play_video(0);
    //}

    void OnDisable()
    {
        // Clean up event handlers
        if (_Video_player != null)
        {
            _Video_player.prepareCompleted -= OnVideoPrepared;
            _Video_player.frameReady -= OnFirstFrameReady;
            _Video_player.loopPointReached -= OnLoopPointReached;
            _Video_player.sendFrameReadyEvents = false;
        }
    }
}
