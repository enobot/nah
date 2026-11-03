using System.Collections.Generic;
using UnityEngine;

// A joining player's screen predicts its own fighter's movement instead of waiting for the host (NAHNetworkPlayer).
// This keeps a short record of that prediction. When the host reports where the fighter really is, it also says which
// input it was moving with and for how long. That names one moment in this record, so the difference between the two
// is a real disagreement (another body was in the way, a lost message) and not just network delay.
// Plain C# so it can be tested without a network.
public sealed class NAHMotionHistory
{
    // About two seconds at 1,000 frames a second. The oldest entries are dropped first.
    public const int MaxSamples = 2048;
    const int MaxSequences = 256;

    struct Sample
    {
        public double Time;
        public Vector3 Position;
        public Vector3 Horizontal;
        public float Vertical;
    }

    struct SequenceStart
    {
        public uint Sequence;
        public double Time;
    }

    readonly List<Sample> samples = new List<Sample>();
    readonly List<SequenceStart> starts = new List<SequenceStart>();

    public int SampleCount => samples.Count;

    // The first prediction step that moved with this input began at this time.
    public void NoteSequenceStart(uint sequence, double time)
    {
        starts.Add(new SequenceStart { Sequence = sequence, Time = time });
        if (starts.Count > MaxSequences)
            starts.RemoveAt(0);
    }

    // Where the prediction put the fighter, and how fast it was moving, at the end of a step.
    public void Record(double time, Vector3 position, Vector3 horizontal, float vertical)
    {
        samples.Add(new Sample { Time = time, Position = position, Horizontal = horizontal, Vertical = vertical });
        if (samples.Count > MaxSamples)
            samples.RemoveAt(0);
    }

    // The host had been moving with input "sequence" for "sinceSequence" seconds when it took its state.
    // Gives how far that state is from this prediction at the same moment. Returns false when that moment is not
    // in the record: before the first input, or after so long a stall that the record no longer reaches back.
    public bool TryFindError(uint sequence, float sinceSequence, Vector3 position, Vector3 horizontal, float vertical,
        out Vector3 positionError, out Vector3 horizontalError, out float verticalError)
    {
        positionError = Vector3.zero;
        horizontalError = Vector3.zero;
        verticalError = 0f;
        if (!float.IsFinite(sinceSequence) || sinceSequence < 0f)
            return false;

        int startIndex = -1;
        for (int i = starts.Count - 1; i >= 0; i--)
        {
            if (starts[i].Sequence == sequence)
            {
                startIndex = i;
                break;
            }
        }
        if (startIndex < 0)
            return false;

        double moment = starts[startIndex].Time + sinceSequence;
        int after = 0;
        while (after < samples.Count && samples[after].Time < moment)
            after++;
        if (after == 0 && (samples.Count == 0 || samples[0].Time > moment))
            return false;

        Sample predicted;
        if (after == samples.Count)
        {
            // The host's moment is past the newest step, which a slow frame here can cause. The newest step is closest.
            predicted = samples[samples.Count - 1];
        }
        else if (after == 0)
        {
            predicted = samples[0];
        }
        else
        {
            Sample a = samples[after - 1];
            Sample b = samples[after];
            float t = (float)((moment - a.Time) / (b.Time - a.Time));
            predicted = new Sample
            {
                Time = moment,
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Horizontal = Vector3.Lerp(a.Horizontal, b.Horizontal, t),
                Vertical = Mathf.Lerp(a.Vertical, b.Vertical, t)
            };
        }

        positionError = position - predicted.Position;
        horizontalError = horizontal - predicted.Horizontal;
        verticalError = vertical - predicted.Vertical;

        // The host does not report older inputs again, so the record before this moment is no longer needed.
        // Keep one step before it, for the next comparison.
        starts.RemoveRange(0, startIndex);
        if (after > 1)
            samples.RemoveRange(0, after - 1);
        return true;
    }

    // After the prediction is corrected, the record is corrected too, so the same disagreement is not counted twice.
    public void Shift(Vector3 positionError, Vector3 horizontalError, float verticalError)
    {
        for (int i = 0; i < samples.Count; i++)
        {
            Sample s = samples[i];
            s.Position += positionError;
            s.Horizontal += horizontalError;
            s.Vertical += verticalError;
            samples[i] = s;
        }
    }

    public void Clear()
    {
        samples.Clear();
        starts.Clear();
    }
}
