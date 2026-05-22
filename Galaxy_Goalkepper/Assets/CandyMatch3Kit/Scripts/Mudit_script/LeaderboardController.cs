using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the leaderboard panel: cell generation, API data population, open/close flow.
/// Attach to _7_leaderboard. The gameObject can start active OR inactive — the script handles both.
/// </summary>
public class LeaderboardController : MonoBehaviour
{
    [Header("Cell Setup")]
    [SerializeField] private GameObject templateCell;
    [SerializeField] private Transform contentParent;

    [Header("Settings")]
    [SerializeField][Range(1, 100)] private int numberOfRanks = 10;

    [Header("Navigation")]
    [SerializeField] private Button backButton;
    //[SerializeField] private GameObject homepagePanel;

    [Header("API")]
    [SerializeField] private APIManager apiManager;

    [Header("Runtime")]
    [SerializeField] private GameObject[] cells;

    private bool isOpen;
    private bool initialized;

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);

        SetChildrenVisible(false);
    }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Called when leaderboard button is pressed on home screen.
    /// </summary>
    public void OnOpenPressed()
    {
        if (isOpen) return;

        // Activate gameObject so coroutine can run, then ensure initialized
        //gameObject.SetActive(true);
        Initialize();

        StartCoroutine(OpenLeaderboard());
    }

    private void OnEnable()
    {
        // Activate gameObject so coroutine can run, then ensure initialized
        //gameObject.SetActive(true);
        Initialize();

        StartCoroutine(OpenLeaderboard());
    }

    private IEnumerator OpenLeaderboard()
    {
        isOpen = true;

        // Hide homepage
        //if (homepagePanel != null)
        //    homepagePanel.SetActive(false);

        // Show leaderboard UI immediately (empty cells) so it's visible behind loading screen
        GenerateCells();
        SetChildrenVisible(true);

        // Show loading screen on top
        if (Survivor._S != null && Survivor._S._Loading_obj != null)

            Survivor._S._Loading_obj.SetActive(true);


        // Call API
        if (apiManager != null)
        {
            //yield return apiManager.Cor_get_request();
        }

        // Populate cells with API data
        PopulateFromAPI();

        // Hide loading — leaderboard is already visible and now populated
        if (Survivor._S != null && Survivor._S._Loading_obj != null)

            Survivor._S._Loading_obj.SetActive(false);

        yield break;
    }

    /// <summary>
    /// Called when back button is pressed.
    /// </summary>
    public void OnBackPressed()
    {
        isOpen = false;
        //SetChildrenVisible(false);  //change done by mudit

        //if (homepagePanel != null)
        //    homepagePanel.SetActive(true);
    }

    private void SetChildrenVisible(bool visible)
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(visible);
    }

    /// <summary>
    /// Populates leaderboard cells from API response data.
    /// </summary>
    private void PopulateFromAPI()
    {
        if (apiManager == null || apiManager._leaderboard_data_full == null)
            return;

        List<GamingScore> scores = null;
        foreach (var category in apiManager._leaderboard_data_full)
        {
            if (category.TopScores != null)
            {
                foreach (var topScore in category.TopScores)
                {
                    if (topScore.TopGamingScores != null && topScore.TopGamingScores.Count > 0)
                    {
                        scores = topScore.TopGamingScores;
                        break;
                    }
                }
            }
            if (scores != null) break;
        }

        if (scores == null) return;

        for (int i = 0; i < scores.Count && i < numberOfRanks; i++)
        {
            var entry = scores[i];
            string playerName = !string.IsNullOrEmpty(entry.User) ? entry.User : "-";
            string score = entry.Score.ToString();
            SetRow(i + 1, playerName, score);
        }
    }

    /// <summary>
    /// Destroys existing clones and regenerates all rank cells.
    /// </summary>
    public void GenerateCells()
    {
        if (templateCell == null || contentParent == null)
            return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            var child = contentParent.GetChild(i);
            if (child.gameObject != templateCell)
            {
                Destroy(child.gameObject);
            }
        }

        cells = new GameObject[numberOfRanks];

        for (int i = 0; i < numberOfRanks; i++)
        {
            GameObject cell = Instantiate(templateCell, contentParent);
            cell.name = $"cell ({i + 1})";
            cell.SetActive(true);

            var texts = cell.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0)
            {
                texts[0].text = (i + 1).ToString();
            }

            cells[i] = cell;
        }

        templateCell.SetActive(false);
    }

    /// <summary>
    /// Sets a row's name and score. rank is 1-based.
    /// </summary>
    public void SetRow(int rank, string playerName, string score)
    {
        if (cells == null || rank < 1 || rank > cells.Length)
            return;

        var texts = cells[rank - 1].GetComponentsInChildren<TextMeshProUGUI>(true);
        if (texts.Length > 1) texts[1].text = playerName;
        if (texts.Length > 2) texts[2].text = score;
    }
}
