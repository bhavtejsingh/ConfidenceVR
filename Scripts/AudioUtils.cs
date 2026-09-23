using System;
using System.IO;
using UnityEngine;

public static class AudioUtils
{
    public static byte[] AudioClipToWav(AudioClip clip, int samplesRecorded)
    {
        if (clip == null || samplesRecorded <= 0) return null;

        int channels = clip.channels;
        int frequency = clip.frequency;

        float[] rawData = new float[samplesRecorded * channels];
        clip.GetData(rawData, 0);

        byte[] pcmData = new byte[rawData.Length * 2];
        int pcmIndex = 0;

        for (int i = 0; i < rawData.Length; i++)
        {
            short sample = (short)Mathf.Clamp(rawData[i] * 32767f, short.MinValue, short.MaxValue);
            byte[] bytes = BitConverter.GetBytes(sample);
            pcmData[pcmIndex++] = bytes[0];
            pcmData[pcmIndex++] = bytes[1];
        }

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // RIFF header
            writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + pcmData.Length);
            writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

            // fmt subchunk
            writer.Write(new char[4] { 'f', 'm', 't', ' ' });
            writer.Write(16); // Subchunk1Size for PCM
            writer.Write((short)1); // AudioFormat 1 = PCM
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2); // ByteRate
            writer.Write((short)(channels * 2)); // BlockAlign
            writer.Write((short)16); // BitsPerSample

            // data subchunk
            writer.Write(new char[4] { 'd', 'a', 't', 'a' });
            writer.Write(pcmData.Length);
            writer.Write(pcmData);

            writer.Flush();
            return stream.ToArray();
        }
    }
}

