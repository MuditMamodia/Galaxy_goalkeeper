using UnityEngine;
using supergoalkeeper;
using TMPro;
using System.Collections;

public class game_flow_loop : MonoBehaviour
{
    public static game_flow_loop gamflow;

    [Header("game pages")]
    public GameObject Landing_page;
    public GameObject video_playing_window;
    public GameObject How_to_play_page;
    public GameObject Main_game_page;
    //public GameObject finishing_of_game;


    public GameObject training_page;
    //public GameObject complection_page_lose;
    public GameObject complection_page;

    [Header("quection_pages")]
    [Header("quection page manager")]
    public Question_manager quection_manager;
    public int incorrect_count;
    public int number_of_retries = 1;
    public int quection_to_display;
    public int currentqurion_inex;
    public bool onetimeincorrect;
    public GameObject quection_page;
    public GameObject incorrect_page;
    public GameObject incorrect_with_corerctans_page;
    public GameObject correctpage;


    [Header("script reference")]
    public SpawnofBalls ballsapwn;
    public MissionOne missionone;
    public VideoController vc;
    public game_score_manager gsm;



    [Header("paying video variables")]
    public int videonumber;
    public bool firstvideo_played;


    [Header("game loop overvariables and checker")]
    public bool game_level_counted;
    public bool gamefinished;
    public bool halftime;
    public bool complectionofloop;
    public int addingplay;
    public bool how_to_answer_quections;
    public bool cancontinue;

    [Header("window of game")]
    public GameObject main_game_window;

    public TextMeshProUGUI hometeamscore;
    public TextMeshProUGUI awayteamscore;

    [Header("htp text")]
    public TextMeshProUGUI htp_text;
    public string qna_string;
    public string game_string;


    private void Awake()
    {
        if (gamflow == null)
        {
            gamflow = this;
        }

        Landing_page.SetActive(true);
        video_playing_window.SetActive(false);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        //complection_page_lose.SetActive(false);
        complection_page.SetActive(false);
        quection_page.SetActive(false);
        //main_game_window.SetActive(false);
        //missionone.ResetGame();

    }


    private void Update()
    {
        if (missionone.gameOver && !game_level_counted && !gamefinished)
        {
            videonumber++;
            Invoke(nameof(opening_videopage), 2f);
            //addingplay++;
            game_level_counted = true;
            halftime = true;
            Debug.Log("player hu aa");

        }
        else if (missionone.gameOver && gamefinished && !complectionofloop)
        {
            complectionofloop = true;
            hometeamscore.text = gsm.GetSavedBalls().ToString();
            awayteamscore.text = gsm.GetMissedGoals().ToString();

            if (gsm.GetSavedBalls() > gsm.GetMissedGoals())
            {
                Debug.Log("win");
                StartCoroutine(transition_win());
               
            }
            else if (gsm.GetSavedBalls() == gsm.GetMissedGoals())
            {
                Debug.Log("draw");
                StartCoroutine(transition_tie());
            }
            else
            {
                Debug.Log("lose");
                StartCoroutine(transition_lose());
            }
        }
    }
    IEnumerator transition_win()
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        Landing_page.SetActive(false);
        videonumber = 2;
        vc.Play_video(videonumber);
        video_playing_window.SetActive(true);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        //complection_page.SetActive(false);
        quection_page.SetActive(false);
        //complection_page_win.SetActive(true);
        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
        
    }
    IEnumerator transition_lose()
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        videonumber = 3;
        Landing_page.SetActive(false);
        vc.Play_video(videonumber);
        video_playing_window.SetActive(true);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        //complection_page.SetActive(false);
        quection_page.SetActive(false);
        //complection_page_lose.SetActive(true);
        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
    }
    IEnumerator transition_tie()
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        
        Landing_page.SetActive(false);
        videonumber = 4;
        vc.Play_video(videonumber);
        video_playing_window.SetActive(true);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        //complection_page.SetActive(false);
        quection_page.SetActive(false);
        //complection_page_lose.SetActive(true);
        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
        
    }







    public void opening_videopage()
    {
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;
        StartCoroutine(transition_opening_video_page());
        //Survivor._S.play_audio_by_index(2);

    }
    IEnumerator transition_opening_video_page()
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        Debug.Log("videopage");
        //game_level_counted+=1;
        Landing_page.SetActive(false);
        video_playing_window.SetActive(true);

        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        //complection_page.SetActive(false);
        quection_page.SetActive(false);
        ballsapwn.totalBallsToSpawn = 0;
        missionone.ResetGame();

        if (game_level_counted)
        {

            gamefinished = true;
        }
        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
        transition_effects.instance.IsBusy = false;
        vc.Play_video(videonumber);
    }


    public void opeing_of_how_to_play()
    {
        Survivor._S.play_audio_by_index(2);
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;
        StartCoroutine(transition_opeing_of_how_to_play());
    }
    IEnumerator transition_opeing_of_how_to_play()
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        //addingplay++;
        Landing_page.SetActive(false);
        video_playing_window.SetActive(false);
        How_to_play_page.SetActive(true);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        //complection_page.SetActive(false);
        quection_page.SetActive(false);
        gsm.ResetStats();
        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
        transition_effects.instance.IsBusy = false;
    }



    // quection page logic starts-------------------------------------------------------------------------------------------
    public void quection_page_opening()
    {
        Survivor._S.play_audio_by_index(2);
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;
        StartCoroutine(transition_quection_page_opening());

    }

    IEnumerator transition_quection_page_opening()
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        ApplyQuectionPageOpeningState();
        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
        transition_effects.instance.IsBusy = false;
    }

    void ApplyQuectionPageOpeningState()
    {
        if (addingplay < 2 && how_to_answer_quections)
        {
            video_playing_window.SetActive(false);
            if (currentqurion_inex == 0 && QnaCardTracker.instance != null)
            {
                QnaCardTracker.instance.ResetAll();
            }
            quection_manager.LoadRandomQuestion();
            quection_page.SetActive(true);
        }
        else if (addingplay < 2 && !how_to_answer_quections)
        {
            htp_text.text = qna_string;
            how_to_answer_quections = true;
            Landing_page.SetActive(false);
            video_playing_window.SetActive(false);
            How_to_play_page.SetActive(true);
            Main_game_page.SetActive(false);
            training_page.SetActive(false);
            //complection_page.SetActive(false);
            quection_page.SetActive(false);
            //complection_page.SetActive(true);
        }
        else
        {
            Landing_page.SetActive(false);
            video_playing_window.SetActive(false);
            How_to_play_page.SetActive(false);
            Main_game_page.SetActive(false);
            training_page.SetActive(false);
            //complection_page.SetActive(false);
            quection_page.SetActive(false);
            complection_page.SetActive(true);
        }
    }

    public void correctanspage()
    {
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;
        StartCoroutine(transition_correctanspage());
    }

    IEnumerator transition_correctanspage()
    {
        int qIdx = currentqurion_inex;
        bool firstTry = !onetimeincorrect;

        System.Action onCovered = () =>
        {
            currentqurion_inex++;
            quection_page.SetActive(false);
            correctpage.SetActive(true);

            if (onetimeincorrect)
            {
                ballsapwn.totalBallsToSpawn += 1;
                onetimeincorrect = false;
                incorrect_count = 0;
            }
            else
            {
                ballsapwn.totalBallsToSpawn += 2;
            }

            if (QnaCardTracker.instance != null)
            {
                if (firstTry) QnaCardTracker.instance.MarkCorrectFirstTry(qIdx);
                else QnaCardTracker.instance.MarkCorrectSecondTry(qIdx);
            }
        };

        if (QnaFlashOverlay.instance != null)
        {
            yield return StartCoroutine(QnaFlashOverlay.instance.FlashGreen(onCovered));
        }
        else
        {
            yield return StartCoroutine(transition_effects.instance.FadeOut());
            onCovered();
            yield return StartCoroutine(transition_effects.instance.FadeIn());
        }
        transition_effects.instance.IsBusy = false;
    }


    public void wrong_option_page_opener()
    {
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;

        if (incorrect_count != number_of_retries)
        {
            StartCoroutine(transition_wrong_ans_page());
        }
        else
        {
            incorrect_count = 0;
            StartCoroutine(transition_wrong_ans_pagenspage_with_answer());
        }
    }

    public void wrong_ans_page()
    {
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;
        StartCoroutine(transition_wrong_ans_page());
    }
    IEnumerator transition_wrong_ans_page()
    {
        System.Action onCovered = () =>
        {
            incorrect_count++;
            quection_page.SetActive(false);
            incorrect_page.SetActive(true);
            onetimeincorrect = true;
        };

        if (QnaFlashOverlay.instance != null)
        {
            yield return StartCoroutine(QnaFlashOverlay.instance.FlashYellow(onCovered));
        }
        else
        {
            yield return StartCoroutine(transition_effects.instance.FadeOut());
            onCovered();
            yield return StartCoroutine(transition_effects.instance.FadeIn());
        }
        transition_effects.instance.IsBusy = false;
    }

    IEnumerator transition_wrong_ans_pagenspage_with_answer()
    {
        int qIdx = currentqurion_inex;

        System.Action onCovered = () =>
        {
            onetimeincorrect = false;
            currentqurion_inex++;
            quection_page.SetActive(false);
            incorrect_with_corerctans_page.SetActive(true);

            if (QnaCardTracker.instance != null)
            {
                QnaCardTracker.instance.MarkFailed(qIdx);
            }
        };

        if (QnaFlashOverlay.instance != null)
        {
            yield return StartCoroutine(QnaFlashOverlay.instance.FlashRed(onCovered));
        }
        else
        {
            yield return StartCoroutine(transition_effects.instance.FadeOut());
            onCovered();
            yield return StartCoroutine(transition_effects.instance.FadeIn());
        }
        transition_effects.instance.IsBusy = false;
    }

    public void retry_quection_or_play()
    {
        Survivor._S.play_audio_by_index(2);
        //Survivor._S.stop_audio_by_index(3);
        Survivor._S.stop_audio_by_index(4);
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;
        StartCoroutine(transition_retry_quection_or_play());
    }
    IEnumerator transition_retry_quection_or_play()
    {
        yield return StartCoroutine(transition_effects.instance.FadeOut());
        quection_page.SetActive(true);
        quection_manager.RepeatCurrentQuestion();
        incorrect_page.SetActive(false);
        yield return StartCoroutine(transition_effects.instance.FadeIn());
        transition_effects.instance.IsBusy = false;
    }

    public void quection_completed_check_and_play(GameObject obj)
    {
        Survivor._S.play_audio_by_index(2);
        Survivor._S.stop_audio_by_index(3);
        Survivor._S.stop_audio_by_index(4);
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;

        if (currentqurion_inex < quection_to_display)
        {
            StartCoroutine(transition_changeofquestion(obj));
        }
        else
        {
            StartCoroutine(transition_quection_completed(obj));
        }
    }
    IEnumerator transition_quection_completed(GameObject obj)
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        if (ballsapwn.totalBallsToSpawn == 0)
        {
            ballsapwn.totalBallsToSpawn = 2;
        }

        obj.SetActive(false);

        // Inline opeing_of_how_to_play state changes so we don't trigger a second transition
        Landing_page.SetActive(false);
        video_playing_window.SetActive(false);
        How_to_play_page.SetActive(true);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        quection_page.SetActive(false);
        if (gsm != null) gsm.ResetStats();

        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
        transition_effects.instance.IsBusy = false;
    }


    IEnumerator transition_changeofquestion(GameObject obj)
    {
        yield return StartCoroutine(transition_effects.instance.FadeOut());
        obj.SetActive(false);

        quection_page.SetActive(true);
        quection_manager.LoadRandomQuestion();
        yield return StartCoroutine(transition_effects.instance.FadeIn());
        transition_effects.instance.IsBusy = false;
    }


    //-------------------------------------------------------------------------------------------------------------
    public void opening_of_maingame()
    {
        Survivor._S.play_audio_by_index(2);
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;
        StartCoroutine(transition_opening_of_maingame());

    }
    IEnumerator transition_opening_of_maingame()
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        if (cancontinue)
        {
            Landing_page.SetActive(false);
            video_playing_window.SetActive(false);
            How_to_play_page.SetActive(false);
            Main_game_page.SetActive(true);
            training_page.SetActive(false);
            //complection_page.SetActive(false);
            quection_page.SetActive(false);
            main_game_window.SetActive(true);
            missionone.gameStarted = true;
            currentqurion_inex = 0;
        }
        else
        {
            htp_text.text = game_string;
            cancontinue = true;
            // Inline quection_page_opening state changes so we don't trigger a second transition
            ApplyQuectionPageOpeningState();
            Landing_page.SetActive(false);
            video_playing_window.SetActive(false);
            How_to_play_page.SetActive(false);
           
            //complection_page.SetActive(false);
           
        }
        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
        transition_effects.instance.IsBusy = false;
    }

    //----------------------------------------------------------------------------------------------------------------

    public void playagain()
    {
        Survivor._S.play_audio_by_index(2);
        if (transition_effects.instance != null && transition_effects.instance.IsBusy) return;
        if (transition_effects.instance != null) transition_effects.instance.IsBusy = true;
        StartCoroutine(transition_playagain());
    }
    IEnumerator transition_playagain()
    {
        yield return StartCoroutine(transition_effects.instance.FootballSlideIn());
        game_level_counted = false;
        gamefinished = false;
        halftime = false;
        complectionofloop = false;
        addingplay = 0;
        how_to_answer_quections = false;
        cancontinue = false;
        videonumber = 0;
        firstvideo_played = false;

        incorrect_count = 0;
        currentqurion_inex = 0;
        onetimeincorrect = false;

        CancelInvoke();

        if (ballsapwn != null)
            ballsapwn.totalBallsToSpawn = 0;

        if (missionone != null)
            missionone.ResetGame();

        if (gsm != null)
            gsm.ResetStats();

        if (vc != null)
            vc.Stop_video();

        if (hometeamscore != null) hometeamscore.text = "0";
        if (awayteamscore != null) awayteamscore.text = "0";

        if (QnaCardTracker.instance != null) QnaCardTracker.instance.ResetAll();

        Landing_page.SetActive(true);
        video_playing_window.SetActive(false);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        complection_page.SetActive(false);
        quection_page.SetActive(false);
        if (incorrect_page != null) incorrect_page.SetActive(false);
        if (incorrect_with_corerctans_page != null) incorrect_with_corerctans_page.SetActive(false);
        if (correctpage != null) correctpage.SetActive(false);
        if (main_game_window != null) main_game_window.SetActive(false);
        yield return StartCoroutine(transition_effects.instance.FootballSlideOut());
        transition_effects.instance.IsBusy = false;
    }

    // void playing_game_start()
    //{
    //    main_game_window.SetActive(true);
    //    //incorrect_with_corerctans_page.SetActive(false);
    //}

}
