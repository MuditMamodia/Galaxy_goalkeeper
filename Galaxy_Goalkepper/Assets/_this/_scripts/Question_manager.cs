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

    // Latches true the moment an answer registers and is cleared by SetupQuestion when the
    // next (or retried) question loads. Guards against double-tap / rapid spam so the same
    // question never advances the game flow twice.
    private bool answerLocked;

    // Fisher-Yates shuffled order. Every question appears exactly once per cycle;
    // a new cycle reshuffles only when the previous one is exhausted.
    [SerializeField] private List<int> shuffledQuestionOrder = new List<int>();
    [SerializeField] private int shuffledQuestionCursor = 0;

    public void LoadRandomQuestion()
    {
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("No questions assigned!");
            return;
        }

        if (shuffledQuestionOrder.Count != questions.Length || shuffledQuestionCursor >= shuffledQuestionOrder.Count)
        {
            ReshuffleQuestionOrder();
        }

        int nextIndex = shuffledQuestionOrder[shuffledQuestionCursor];
        shuffledQuestionCursor++;

        currentQuestionIndex = nextIndex;
        SetupQuestion(questions[nextIndex]);
    }

    // Fisher-Yates: walks i from last index down to 1, swapping arr[i] with arr[Random.Range(0, i+1)].
    private void ReshuffleQuestionOrder()
    {
        shuffledQuestionOrder.Clear();
        for (int i = 0; i < questions.Length; i++)
        {
            shuffledQuestionOrder.Add(i);
        }
        for (int i = shuffledQuestionOrder.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = shuffledQuestionOrder[i];
            shuffledQuestionOrder[i] = shuffledQuestionOrder[j];
            shuffledQuestionOrder[j] = tmp;
        }
        shuffledQuestionCursor = 0;
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

        // New question — accept input again.
        answerLocked = false;

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
        // Spam guard: ignore every tap after the first one for this question. Cleared by
        // SetupQuestion the next time a question loads / is retried.
        if (answerLocked) return;
        answerLocked = true;

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