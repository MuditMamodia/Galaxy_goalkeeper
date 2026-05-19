using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Question_manager : MonoBehaviour
{
   

    [System.Serializable]
    public class MCQ
    {
        public string question;
        public string[] wrongAnswers;
        public string correctAnswer;
        public string whyCorrect;
    }

    [Header("Data")]
    public MCQ[] questions;

    [Header("UI")]
    public TextMeshProUGUI question_txt;
    public TextMeshProUGUI[] why_correct_txt;

    [Header("Buttons")]
    public Button[] answerButtons;

    [Header("Answer Selection Modes")]
    [Tooltip("If true, dragging the Answer_ball onto an option selects it.")]
    public bool enableDragBallToOption = true;
    [Tooltip("If true, clicking/tapping an option directly selects it.")]
    public bool enableDirectClick = false;
    [Tooltip("Optional. If left empty, will search the scene for a GameObject named 'Answer_ball'.")]
    public DraggableBall answerBall;

    private MCQ currentQuestion;
    private string correctAnswer;

    private int currentQuestionIndex = -1;

    // 🔥 Track used questions
    [SerializeField] private List<int> usedQuestionIndexes = new List<int>();

    // 🔥 LOAD RANDOM (NO REPEAT)
    public void LoadRandomQuestion()
    {
        if (questions.Length == 0)
        {
            Debug.LogWarning("No questions assigned!");
            return;
        }

        // If all questions used → reset
        if (usedQuestionIndexes.Count >= questions.Length)
        {
            usedQuestionIndexes.Clear();
        }

        int randomIndex;

        // Pick unused question
        do
        {
            randomIndex = Random.Range(0, questions.Length);
        }
        while (usedQuestionIndexes.Contains(randomIndex));

        usedQuestionIndexes.Add(randomIndex);
        currentQuestionIndex = randomIndex;

        SetupQuestion(questions[randomIndex]);
    }

    // 🔁 REPEAT CURRENT QUESTION
    public void RepeatCurrentQuestion()
    {
        if (currentQuestionIndex < 0 || currentQuestionIndex >= questions.Length)
        {
            Debug.LogWarning("No question to repeat!");
            return;
        }

        SetupQuestion(questions[currentQuestionIndex]);
    }

    // 🔧 COMMON SETUP FUNCTION
    private void SetupQuestion(MCQ questionData)
    {
        currentQuestion = questionData;
        question_txt.text = currentQuestion.question;
        correctAnswer = currentQuestion.correctAnswer;

        // Prepare answers
        List<string> allAnswers = new List<string>();
        allAnswers.Add(currentQuestion.correctAnswer);

        foreach (var wrong in currentQuestion.wrongAnswers)
        {
            allAnswers.Add(wrong);
        }

        // 🔀 Shuffle answers
        for (int i = 0; i < allAnswers.Count; i++)
        {
            int rand = Random.Range(0, allAnswers.Count);
            (allAnswers[i], allAnswers[rand]) = (allAnswers[rand], allAnswers[i]);
        }

        // Assign to buttons
        for (int i = 0; i < answerButtons.Length; i++)
        {
            Button btn = answerButtons[i];

            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = allAnswers[i];

            string selectedAnswer = allAnswers[i];

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                OnAnswerClicked(selectedAnswer);
            });

            btn.interactable = enableDirectClick;
            ColorBlock colors = btn.colors;
            colors.disabledColor = colors.normalColor;
            btn.colors = colors;
        }

        ApplyAnswerModeToBall();

        //why_correct_txt.text = "";
    }

    private void ApplyAnswerModeToBall()
    {
        if (answerBall == null)
        {
            GameObject go = GameObject.Find("Answer_ball");
            if (go != null) answerBall = go.GetComponent<DraggableBall>();
        }
        if (answerBall != null) answerBall.enabled = enableDragBallToOption;
    }

    // 🔥 HANDLE CLICK
    private void OnAnswerClicked(string selectedAnswer)
    {
        if (selectedAnswer == correctAnswer)
        {
            Survivor._S.play_audio_by_index(3);
            Debug.Log("✅ Correct Answer!");
            //lcs.opeing_correct_answer_page();
            game_flow_loop.gamflow.correctanspage();


        }
        else
        {
            Survivor._S.play_audio_by_index(4);
            Debug.Log("❌ Wrong Answer!");

            //lcs.incorrect_page_opening();
            game_flow_loop.gamflow.wrong_option_page_opener();
        }

        
        foreach (var whycorrect in why_correct_txt)
        {
            whycorrect.text = currentQuestion.whyCorrect;
        }
    }
}