using System;
using UnityEngine;

public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int maxRecordingDurationSec = 60;

    private AudioClip recordingClip;
    private string selectedDevice;
    private bool isRecording = false;

    public bool IsRecording => isRecording;

    private void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            selectedDevice = Microphone.devices[0];
            Debug.Log($"[MicrophoneRecorder] Using device: {selectedDevice}");
        }
        else
        {
            Debug.LogError("[MicrophoneRecorder] No microphone detected!");
        }
    }

    public void StartRecording()
    {
        if (isRecording) return;
        if (string.IsNullOrEmpty(selectedDevice) && Microphone.devices.Length > 0)
        {
            selectedDevice = Microphone.devices[0];
        }

        recordingClip = Microphone.Start(selectedDevice, false, maxRecordingDurationSec, sampleRate);
        isRecording = true;
        Debug.Log("[MicrophoneRecorder] Recording started...");
    }

    public byte[] StopRecording()
    {
        if (!isRecording) return null;

        int position = Microphone.GetPosition(selectedDevice);
        Microphone.End(selectedDevice);
        isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("[MicrophoneRecorder] No audio samples captured.");
            return null;
        }

        Debug.Log($"[MicrophoneRecorder] Recording stopped. Captured {position} samples.");
        return AudioUtils.AudioClipToWav(recordingClip, position);
    }
}

