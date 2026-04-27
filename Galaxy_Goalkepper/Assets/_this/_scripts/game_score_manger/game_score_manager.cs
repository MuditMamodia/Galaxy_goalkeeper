using supergoalkeeper;
using TMPro;
using UnityEngine;

public class game_score_manager : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public goalController goalController;
    public MissionOne missionOne;   // reference to check gameOver

    


    public TextMeshProUGUI saved_representer;
    public TextMeshProUGUI not_saved_representer;

    private bool hasPrinted = false;
    private int lastSaved = -1;
    private int lastMissed = -1;


    void Start()
    {

        // 🔹 Auto find MissionOne if not assigned
        if (missionOne == null)
        {
            missionOne = FindObjectOfType<MissionOne>();
        }

        if (missionOne != null)
        {
            if (playerController == null && missionOne.player != null)
                playerController = missionOne.player.ComponentBehaviour;

            if (goalController == null)
                goalController = missionOne.goalCtrl;
        }
    }

    void Update()
    {
        if (missionOne == null)
            return;

        if (playerController == null && missionOne.player != null)
        {
            playerController = missionOne.player.ComponentBehaviour;
        }

        if (goalController == null)
        {
            goalController = missionOne.goalCtrl;
        }

        UpdateUI();

        if (missionOne.gameOver && !hasPrinted)
        {
            hasPrinted = true;

            Debug.Log(
                "GAME OVER\n" +
                "Saved Balls: " + GetSavedBalls() + "\n" +
                "Missed Goals: " + GetMissedGoals()
            );
        }
    }

    public int GetSavedBalls()
    {
        if (playerController == null) return 0;
        return playerController.totalCollectedObjects;
    }

    public int GetMissedGoals()
    {
        if (goalController == null) return 0;
        return goalController.goals;
    }

    public void ResetStats()
    {
        if (playerController != null)
        {
            playerController.collectedObjects = 0;
            playerController.totalCollectedObjects = 0;
            //playerController.coins = 0;
        }

        if (goalController != null)
        {
            goalController.goals = 0;
        }

        hasPrinted = false;
        lastSaved = -1;
        lastMissed = -1;
    }

    void UpdateUI()
    {
        int saved = GetSavedBalls();
        int missed = GetMissedGoals();

        if (saved != lastSaved && saved_representer != null)
        {
            saved_representer.text = "Saved:- " + saved;
            lastSaved = saved;
        }

        if (missed != lastMissed && not_saved_representer != null)
        {
            not_saved_representer.text = "Not Saved:- " + missed;
            lastMissed = missed;
        }
    }
}