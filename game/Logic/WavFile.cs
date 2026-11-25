// Purpose: writes samples as a .wav file, the simplest sound file there is: a 44-byte label
// saying how to read it, then every sample as a 16-bit whole number. The game itself never needs
// one (Unity plays the samples directly); it is for listening to the made sounds on a computer or
// phone (rules_dotnet/Bodhi.Audio.Preview), and for handing them to a sound designer to improve.
using System;
using System.IO;
using System.Text;

namespace Bodhi.Game
{
    public static class WavFile
    {
        // The whole file for mono samples at this sample rate. Samples past -1 or 1 (or NaN) are
        // held at the edge instead of wrapping round, which would sound like a loud crack.
        public static byte[] to_bytes(float[] samples, int sample_rate)
        {
            samples = samples ?? Array.Empty<float>();
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int data_bytes = samples.Length * 2;
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + data_bytes);              // Bytes in the file after this number.
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);                            // Bytes in this "fmt " part.
                writer.Write((short)1);                      // 1 = plain samples, not compressed.
                writer.Write((short)1);                      // One channel (mono).
                writer.Write(sample_rate);
                writer.Write(sample_rate * 2);               // Bytes a second.
                writer.Write((short)2);                      // Bytes per sample, all channels.
                writer.Write((short)16);                     // Bits per sample.
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(data_bytes);
                foreach (float sample in samples)
                {
                    float held = float.IsNaN(sample) ? 0 : Math.Max(-1f, Math.Min(1f, sample));
                    writer.Write((short)Math.Round(held * short.MaxValue));
                }
                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
