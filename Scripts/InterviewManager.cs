using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterviewManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private OpenAIService openAIService;
    [SerializeField] private MicrophoneRecorder microphoneRecorder;
    [SerializeField] private InterviewUI interviewUI;
    [SerializeField] private AudioSource interviewerAudioSource;

    private readonly string[] topics = new string[]
    {
        "Data Structures & Algorithms",
        "Object-Oriented Programming",
        "Database Management Systems",
        "Operating Systems",
        "Computer Networks"
    };

    private int currentTopicIndex = 0;
    private bool isAwaitingFollowUp = false;
    private List<OpenAIService.ChatMessage> conversationHistory = new List<OpenAIService.ChatMessage>();

    [System.Serializable]
    private class FinalFeedbackJson
    {
        public string communication;
        public string technical_knowledge;
        public string confidence;
        public string areas_to_improve;
    }

    private void Start()
    {
        if (interviewUI != null)
        {
            interviewUI.OnRecordButtonClicked += HandleRecordButtonPressed;
            interviewUI.OnRestartButtonClicked += StartInterview;
        }

        StartInterview();
    }

    private void Update()
    {
        // Editor / Standalone debug shortcut: Spacebar toggles recording
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleRecordButtonPressed();
        }
    }

    public void StartInterview()
    {
        currentTopicIndex = 0;
        isAwaitingFollowUp = false;
        conversationHistory.Clear();

        string systemPrompt = 
            "You are an encouraging but sharp Senior Technical Interviewer conducting a 5-question technical interview in VR. " +
            "The 5 topics are: 1. Data Structures & Algorithms, 2. Object-Oriented Programming, 3. Database Management Systems, 4. Operating Systems, 5. Computer Networks. " +
            "For each topic, ask ONE concise, real-world technical question (maximum 2 sentences so it sounds natural when spoken). " +
            "If the candidate's answer is brief, vague, or partially correct, ask ONE short follow-up probing question. " +
            "If the candidate's answer is solid or they already answered the follow-up, conclude that topic with a brief acknowledgement and signal you are ready for the next topic.";

        conversationHistory.Add(new OpenAIService.ChatMessage("system", systemPrompt));

        interviewUI.ShowInterviewPanel();
        interviewUI.SetRecordButtonState(false, false);

        AskCurrentTopicQuestion();
    }

    private void AskCurrentTopicQuestion()
    {
        string topic = topics[currentTopicIndex];
        interviewUI.SetStatus($"Preparing question for {topic}...");
        interviewUI.SetRecordButtonState(false, false);

        string prompt = $"Ask a realistic technical interview question about: {topic}. Keep it direct and under 2 sentences.";
        conversationHistory.Add(new OpenAIService.ChatMessage("user", prompt));

        StartCoroutine(openAIService.SendChat(
            conversationHistory,
            onSuccess: (response) =>
            {
                conversationHistory.Add(new OpenAIService.ChatMessage("assistant", response));
                interviewUI.UpdateQuestionInfo(currentTopicIndex + 1, topics.Length, topic, response);
                SpeakResponse(response, onComplete: () =>
                {
                    interviewUI.SetStatus("Your turn: Press 'Start Speaking' and answer into your microphone.");
                    interviewUI.SetRecordButtonState(false, true);
                });
            },
            onError: (err) =>
            {
                interviewUI.SetStatus($"Error generating question: {err}");
            }
        ));
    }

    private void HandleRecordButtonPressed()
    {
        if (!microphoneRecorder.IsRecording)
        {
            // Start recording
            microphoneRecorder.StartRecording();
            interviewUI.SetStatus("Listening to your answer... Click 'Stop & Submit' when finished.");
            interviewUI.SetRecordButtonState(true, true);
        }
        else
        {
            // Stop recording & transcribe
            interviewUI.SetStatus("Processing your speech with Whisper...");
            interviewUI.SetRecordButtonState(false, false);

            byte[] wavBytes = microphoneRecorder.StopRecording();
            if (wavBytes == null)
            {
                interviewUI.SetStatus("No voice detected. Please try recording again.");
                interviewUI.SetRecordButtonState(false, true);
                return;
            }

            StartCoroutine(openAIService.TranscribeSpeech(
                wavBytes,
                onSuccess: (transcription) =>
                {
                    if (string.IsNullOrWhiteSpace(transcription))
                    {
                        interviewUI.SetStatus("Could not hear clearly. Try again.");
                        interviewUI.SetRecordButtonState(false, true);
                        return;
                    }

                    interviewUI.SetUserTranscript(transcription);
                    EvaluateCandidateAnswer(transcription);
                },
                onError: (err) =>
                {
                    interviewUI.SetStatus($"Transcription error: {err}");
                    interviewUI.SetRecordButtonState(false, true);
                }
            ));
        }
    }

    private void EvaluateCandidateAnswer(string candidateAnswer)
    {
        interviewUI.SetStatus("AI Interviewer is evaluating your response...");
        conversationHistory.Add(new OpenAIService.ChatMessage("user", candidateAnswer));

        if (!isAwaitingFollowUp)
        {
            // Ask LLM whether to ask a follow-up or move on
            string evalPrompt = 
                "Evaluate the candidate's answer above. " +
                "If the answer was incomplete, ask ONE short follow-up question. Begin your response with [FOLLOWUP]. " +
                "If the answer was sufficient, provide a brief 1-sentence acknowledgement. Begin your response with [PASS].";
            
            conversationHistory.Add(new OpenAIService.ChatMessage("system", evalPrompt));

            StartCoroutine(openAIService.SendChat(
                conversationHistory,
                onSuccess: (response) =>
                {
                    if (response.Contains("[FOLLOWUP]"))
                    {
                        isAwaitingFollowUp = true;
                        string cleanResponse = response.Replace("[FOLLOWUP]", "").Trim();
                        conversationHistory.Add(new OpenAIService.ChatMessage("assistant", cleanResponse));
                        interviewUI.UpdateQuestionInfo(currentTopicIndex + 1, topics.Length, $"{topics[currentTopicIndex]} (Follow-up)", cleanResponse);

                        SpeakResponse(cleanResponse, onComplete: () =>
                        {
                            interviewUI.SetStatus("Answer the follow-up question.");
                            interviewUI.SetRecordButtonState(false, true);
                        });
                    }
                    else
                    {
                        string cleanResponse = response.Replace("[PASS]", "").Trim();
                        conversationHistory.Add(new OpenAIService.ChatMessage("assistant", cleanResponse));
                        ProceedToNextQuestionOrFinish(cleanResponse);
                    }
                },
                onError: (err) =>
                {
                    interviewUI.SetStatus($"Evaluation error: {err}");
                }
            ));
        }
        else
        {
            // Follow up was answered, move to next topic
            isAwaitingFollowUp = false;
            string moveOnPrompt = "Briefly acknowledge the candidate's follow-up answer in 1 sentence.";
            conversationHistory.Add(new OpenAIService.ChatMessage("system", moveOnPrompt));

            StartCoroutine(openAIService.SendChat(
                conversationHistory,
                onSuccess: (response) =>
                {
                    conversationHistory.Add(new OpenAIService.ChatMessage("assistant", response));
                    ProceedToNextQuestionOrFinish(response);
                },
                onError: (err) =>
                {
                    ProceedToNextQuestionOrFinish("Thank you. Let's move forward.");
                }
            ));
        }
    }

    private void ProceedToNextQuestionOrFinish(string transitionSpeech)
    {
        currentTopicIndex++;

        SpeakResponse(transitionSpeech, onComplete: () =>
        {
            if (currentTopicIndex < topics.Length)
            {
                AskCurrentTopicQuestion();
            }
            else
            {
                GenerateFinalFeedback();
            }
        });
    }

    private void GenerateFinalFeedback()
    {
        interviewUI.SetStatus("Generating your comprehensive interview evaluation...");

        string feedbackPrompt = 
            "The 5-question technical interview is complete. Analyze the candidate's responses throughout the interview. " +
            "Respond strictly with a JSON object in this exact format (no markdown fences, raw JSON): " +
            "{\n" +
            "  \"communication\": \"Grade (e.g. A-/B+) and 2 sentences on clarity, pace, and articulation.\",\n" +
            "  \"technical_knowledge\": \"Grade and 2 sentences on core technical understanding across topics.\",\n" +
            "  \"confidence\": \"Grade and 2 sentences on assertiveness and hesitation.\",\n" +
            "  \"areas_to_improve\": \"Bullet points of top 2-3 specific concepts or habits to work on.\"\n" +
            "}";

        conversationHistory.Add(new OpenAIService.ChatMessage("system", feedbackPrompt));

        StartCoroutine(openAIService.SendChat(
            conversationHistory,
            onSuccess: (jsonResponse) =>
            {
                string cleaned = jsonResponse.Replace("```json", "").Replace("```", "").Trim();
                try
                {
                    FinalFeedbackJson feedback = JsonUtility.FromJson<FinalFeedbackJson>(cleaned);
                    interviewUI.ShowFeedback(
                        feedback.communication,
                        feedback.technical_knowledge,
                        feedback.confidence,
                        feedback.areas_to_improve
                    );
                }
                catch
                {
                    // Fallback formatting if raw text
                    interviewUI.ShowFeedback(
                        "Good clarity with room for structure.",
                        "Demonstrated foundational understanding.",
                        "Steady tone with minor pauses.",
                        cleaned
                    );
                }
            },
            onError: (err) =>
            {
                interviewUI.SetStatus($"Error generating feedback: {err}");
            }
        ));
    }

    private void SpeakResponse(string text, Action onComplete)
    {
        StartCoroutine(openAIService.TextToSpeech(
            text,
            onSuccess: (clip) =>
            {
                if (interviewerAudioSource != null && clip != null)
                {
                    interviewerAudioSource.clip = clip;
                    interviewerAudioSource.Play();
                    StartCoroutine(WaitForAudioPlayback(clip.length, onComplete));
                }
                else
                {
                    onComplete?.Invoke();
                }
            },
            onError: (err) =>
            {
                Debug.LogWarning($"TTS failed: {err}. Proceeding without voice audio.");
                onComplete?.Invoke();
            }
        ));
    }

    private IEnumerator WaitForAudioPlayback(float duration, Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        onComplete?.Invoke();
    }
}

