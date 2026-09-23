using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InterviewUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject interviewPanel;
    [SerializeField] private GameObject feedbackPanel;

    [Header("Interview Panel Elements")]
    [SerializeField] private TextMeshProUGUI topicText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI questionSubtitleText;
    [SerializeField] private TextMeshProUGUI userTranscriptText;
    [SerializeField] private Button recordButton;
    [SerializeField] private TextMeshProUGUI recordButtonText;

    [Header("Feedback Panel Elements")]
    [SerializeField] private TextMeshProUGUI communicationText;
    [SerializeField] private TextMeshProUGUI technicalKnowledgeText;
    [SerializeField] private TextMeshProUGUI confidenceText;
    [SerializeField] private TextMeshProUGUI areasToImproveText;
    [SerializeField] private Button restartButton;

    public event Action OnRecordButtonClicked;
    public event Action OnRestartButtonClicked;

    private void Awake()
    {
        if (recordButton != null)
        {
            recordButton.onClick.AddListener(() => OnRecordButtonClicked?.Invoke());
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => OnRestartButtonClicked?.Invoke());
        }
    }

    public void ShowInterviewPanel()
    {
        if (interviewPanel != null) interviewPanel.SetActive(true);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    public void UpdateQuestionInfo(int currentQ, int totalQ, string topic, string questionText)
    {
        if (topicText != null) topicText.text = $"Question {currentQ}/{totalQ}: <color=#4FC3F7>{topic}</color>";
        if (questionSubtitleText != null) questionSubtitleText.text = questionText;
        if (userTranscriptText != null) userTranscriptText.text = "<i>Your transcribed response will appear here...</i>";
    }

    public void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    public void SetUserTranscript(string transcript)
    {
        if (userTranscriptText != null) userTranscriptText.text = $"<b>Candidate:</b> {transcript}";
    }

    public void SetRecordButtonState(bool isRecording, bool interactable)
    {
        if (recordButton != null) recordButton.interactable = interactable;
        if (recordButtonText != null)
        {
            recordButtonText.text = isRecording ? "Stop & Submit Answer" : "Start Speaking Answer";
        }
    }

    public void ShowFeedback(string communication, string technical, string confidence, string improvements)
    {
        if (interviewPanel != null) interviewPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(true);

        if (communicationText != null) communicationText.text = communication;
        if (technicalKnowledgeText != null) technicalKnowledgeText.text = technical;
        if (confidenceText != null) confidenceText.text = confidence;
        if (areasToImproveText != null) areasToImproveText.text = improvements;
    }
}

