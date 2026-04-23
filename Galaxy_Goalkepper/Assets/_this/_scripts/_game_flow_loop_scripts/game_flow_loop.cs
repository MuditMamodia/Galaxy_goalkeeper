using Unity.Properties;
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
    public GameObject finishing_of_game;


    public GameObject training_page;
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
        complection_page.SetActive(false);
        quection_page.SetActive(false);
        //main_game_window.SetActive(false);

    }

    public void opening_videopage()
    {
        Landing_page.SetActive(false);
        video_playing_window.SetActive(true);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        complection_page.SetActive(false);
        quection_page.SetActive(false);
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
        }
    }

    public void wrong_ans_page()
    {
        incorrect_count++;
        quection_page.SetActive(false);
        incorrect_page.SetActive(true);
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
        onetimeincorrect = true;
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
            main_game_window.SetActive(true);
            missionone.gameStarted = true;
        }
    }

    // void playing_game_start()
    //{
    //    main_game_window.SetActive(true);
    //    //incorrect_with_corerctans_page.SetActive(false);
    //}

}
