using UnityEngine;

public class game_flow_loop : MonoBehaviour
{
    [Header("game pages")]
    public GameObject Landing_page;
    public GameObject How_to_play_page;
    public GameObject Main_game_page;
    public GameObject training_page;
    public GameObject complection_page;

    private void Awake()
    {
        Landing_page.SetActive(true);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        complection_page.SetActive(false);

    }

    public void opening_howtoplay()
    {
        Landing_page.SetActive(false);
        How_to_play_page.SetActive(true);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        complection_page.SetActive(false);
    }


    public void opening_main_game()
    {
        Landing_page.SetActive(false);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(true);
        training_page.SetActive(false);
        complection_page.SetActive(false);
    }

    public void opening_training_page()
    {
        Landing_page.SetActive(false);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(true);
        complection_page.SetActive(false);
    }


    public void opening_complection_page()
    {
        Landing_page.SetActive(false);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        complection_page.SetActive(true);
    }

    public void starting_the_game_loop()
    {
        Landing_page.SetActive(true);
        How_to_play_page.SetActive(false);
        Main_game_page.SetActive(false);
        training_page.SetActive(false);
        complection_page.SetActive(false);
    }

}
