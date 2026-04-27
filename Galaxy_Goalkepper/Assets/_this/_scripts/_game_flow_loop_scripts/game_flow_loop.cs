using UnityEngine;
using supergoalkeeper;

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
    public GameObject complection_page_lose;
    public GameObject complection_page_win;

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

    [Header("window of game")]
    public GameObject main_game_window;




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
        complection_page_lose.SetActive(false);
        complection_page_win.SetActive(false);
        quection_page.SetActive(false);
        //main_game_window.SetActive(false);

    }


    private void Update()
    {
        if (missionone.gameOver && !game_level_counted&& !gamefinished)
        {
            videonumber = 1;
            Invoke(nameof(opening_videopage), 2f);

            game_level_counted = true;

        }
        else if (missionone.gameOver && gamefinished)
        {
            if (gsm.GetSavedBalls() > gsm.GetMissedGoals())
            {
                Landing_page.SetActive(false);
                video_playing_window.SetActive(false);
                How_to_play_page.SetActive(false);
                Main_game_page.SetActive(false);
                training_page.SetActive(false);
                //complection_page.SetActive(false);
                quection_page.SetActive(false);
                complection_page_win.SetActive(true);
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
                complection_page_lose.SetActive(true);
            }
        }
    }


    public void opening_videopage()
    {
        //game_level_counted+=1;
        Landing_page.SetActive(false);
        video_playing_window.SetActive(true);
        vc.Play_video(videonumber);
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
    }


    public void opeing_of_how_to_play()
    {
        Landing_page.SetActive(false);
        video_playing_window.SetActive(false);
        How_to_play_page.SetActive(true);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        //complection_page.SetActive(false);
        quection_page.SetActive(false);
        gsm.ResetStats();
    }


    // quection page logic starts-------------------------------------------------------------------------------------------
    public void quection_page_opening()
    {
        video_playing_window.SetActive(false);
        quection_manager.LoadRandomQuestion();
        quection_page.SetActive(true);
    }

   public  void correctanspage()
    {
        currentqurion_inex++;
        quection_page.SetActive(false);
        correctpage.SetActive(true);
        
        if (onetimeincorrect)
        {
            ballsapwn.totalBallsToSpawn += 1;
            onetimeincorrect =false;
            incorrect_count = 0;
        }
        else
        {
            ballsapwn.totalBallsToSpawn += 2;
        }
    }


   public void wrong_option_page_opener()
    {
        if (incorrect_count != number_of_retries)
        {
            wrong_ans_page();
        }
        else
        {
            wrong_ans_pagenspage_with_answer();
            incorrect_count = 0;
        }
    }

    public void wrong_ans_page()
    {
        incorrect_count++;
        quection_page.SetActive(false);
        incorrect_page.SetActive(true);
        onetimeincorrect = true;
    }

     void wrong_ans_pagenspage_with_answer()
    {
        //quection_counted++;
        onetimeincorrect = false;
        currentqurion_inex++;
        quection_page.SetActive(false);
        incorrect_with_corerctans_page.SetActive(true);
    }

    public void retry_quection_or_play( )
    {
        quection_page.SetActive(true);
        quection_manager.RepeatCurrentQuestion();
        incorrect_page.SetActive(false);
        
    }

    public void quection_completed_check_and_play(GameObject obj)
    {
        if (currentqurion_inex < quection_to_display)
        {
            obj.SetActive(false);
            
            quection_page.SetActive(true);
            quection_manager.LoadRandomQuestion();
        }
        else
        {
            obj.SetActive(false);
            opeing_of_how_to_play();


        }
    }

    public void opening_of_maingame()
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

    // void playing_game_start()
    //{
    //    main_game_window.SetActive(true);
    //    //incorrect_with_corerctans_page.SetActive(false);
    //}

}
