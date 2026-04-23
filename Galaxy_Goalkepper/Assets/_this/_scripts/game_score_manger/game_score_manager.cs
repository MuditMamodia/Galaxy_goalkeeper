using supergoalkeeper;
using UnityEngine;

public class game_score_manager : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public goalController goalController;
    public MissionOne missionOne;   // reference to check gameOver

    private bool hasPrinted = false; // prevent multiple logs

    void Start()
    {
     
        // 🔹 Auto find MissionOne if not assigned
        if (missionOne == null)
        {
            missionOne = FindObjectOfType<MissionOne>();
        }
    }

    void Update()
    {
        if (missionOne == null) return;

        // 🔥 Get references AFTER player is spawned
        if (playerController == null && missionOne.player != null)
        {
            playerController = missionOne.player.ComponentBehaviour;
        }

        if (goalController == null)
        {
            goalController = FindObjectOfType<goalController>();
        }

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

    // 🔹 Get saved balls
    public int GetSavedBalls()
    {
        if (playerController == null) return 0;
        return playerController.totalCollectedObjects;
    }

    // 🔹 Get missed goals
    public int GetMissedGoals()
    {
        if (goalController == null) return 0;
        return goalController.goals;
    }

    // 🔹 Reset stats
    public void ResetStats()
    {
        if (playerController != null)
        {
            playerController.collectedObjects = 0;
            playerController.totalCollectedObjects = 0;
            playerController.coins = 0;
        }

        if (goalController != null)
        {
            goalController.goals = 0;
        }

        hasPrinted = false; // allow printing again next round

        Debug.Log("Stats Reset!");
    }
}