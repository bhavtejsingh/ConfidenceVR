using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OpenAIService : MonoBehaviour
{
    [Header("OpenAI API Configuration")]
    [Tooltip("Enter your OpenAI API key starting with sk-...")]
    [SerializeField] private string apiKey = "";

    private const string WhisperEndpoint = "https://api.openai.com/v1/audio/transcriptions";
    private const string ChatEndpoint = "https://api.openai.com/v1/chat/completions";
    private const string TTSEndpoint = "https://api.openai.com/v1/audio/speech";

    [System.Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [System.Serializable]
    private class ChatPayload
    {
        public string model;
        public List<ChatMessage> messages;
        public float temperature;
    }

    [System.Serializable]
    private class WhisperResponse
    {
        public string text;
    }

    [System.Serializable]
    private class ChatChoice
    {
        public ChatMessage message;
    }

    [System.Serializable]
    private class ChatResponse
    {
        public List<ChatChoice> choices;
    }

    [System.Serializable]
    private class TTSPayload
    {
        public string model;
        public string input;
        public string voice;
        public string response_format;
    }

    public void SetApiKey(string key)
    {
        apiKey = key;
    }

    // 1. Speech to Text (Whisper)
    public IEnumerator TranscribeSpeech(byte[] wavData, Action<string> onSuccess, Action<string> onError)
    {
        if (wavData == null || wavData.Length == 0)
        {
            onError?.Invoke("No audio data provided.");
            yield break;
        }

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", wavData, "recording.wav", "audio/wav"),
            new MultipartFormDataSection("model", "whisper-1")
        };

        using (UnityWebRequest req = UnityWebRequest.Post(WhisperEndpoint, formData))
        {
            req.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"STT Error ({req.responseCode}): {req.error}\n{req.downloadHandler.text}");
            }
            else
            {
                WhisperResponse resp = JsonUtility.FromJson<WhisperResponse>(req.downloadHandler.text);
                onSuccess?.Invoke(resp != null ? resp.text : "");
            }
        }
    }

    // 2. Chat Completion (GPT-4o-mini)
    public IEnumerator SendChat(List<ChatMessage> conversation, Action<string> onSuccess, Action<string> onError)
    {
        ChatPayload payload = new ChatPayload
        {
            model = "gpt-4o-mini",
            messages = conversation,
            temperature = 0.7f
        };

        string json = JsonUtility.ToJson(payload);

        using (UnityWebRequest req = new UnityWebRequest(ChatEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"LLM Error ({req.responseCode}): {req.error}\n{req.downloadHandler.text}");
            }
            else
            {
                ChatResponse resp = JsonUtility.FromJson<ChatResponse>(req.downloadHandler.text);
                if (resp != null && resp.choices != null && resp.choices.Count > 0)
                {
                    onSuccess?.Invoke(resp.choices[0].message.content);
                }
                else
                {
                    onError?.Invoke("Empty response from LLM.");
                }
            }
        }
    }

    // 3. Text to Speech (TTS-1)
    public IEnumerator TextToSpeech(string text, Action<AudioClip> onSuccess, Action<string> onError)
    {
        TTSPayload payload = new TTSPayload
        {
            model = "tts-1",
            input = text,
            voice = "onyx",
            response_format = "wav"
        };

        string json = JsonUtility.ToJson(payload);
        string tempPath = Path.Combine(Application.persistentDataPath, "tts_response.wav");

        using (UnityWebRequest req = new UnityWebRequest(TTSEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerFile(tempPath);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"TTS Error ({req.responseCode}): {req.error}");
                yield break;
            }
        }

        // Load the downloaded WAV file into an AudioClip
        string uri = "file://" + tempPath;
        using (UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
        {
            yield return audioReq.SendWebRequest();

            if (audioReq.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Failed to load TTS AudioClip: {audioReq.error}");
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(audioReq);
                onSuccess?.Invoke(clip);
            }
        }
    }
}

