using System;
using System.Collections.Generic;
using UnityEngine;

// The host keeps where each fighter stood over the last moment. Every screen shows the other fighters slightly in the
// past, so two screens can disagree about a swing. The owner's middle-ground rule (GAME_DESIGN.md, "Multiplayer and
// security") checks a swing against where the target stood on the attacker's screen, but never further back than a
// fixed limit. This record answers "where was this fighter at that moment"; RewindTime applies the limit.
// Plain C# so it can be tested without a network.
public sealed class NAHPoseHistory
{
    // Longer than any rewind limit the owner is considering (100 to 150 ms). Older poses are dropped, except the newest
    // one older than this, so a moment near the edge can still be found between two poses.
    public const double KeepSeconds = 1.0;

    // A safety limit in case poses are recorded every frame at a very high frame rate.
    public const int MaxPoses = 4096;

    struct Pose
    {
        public double Time;
        public Vector3 Position;
        public float Yaw;
    }

    readonly List<Pose> poses = new List<Pose>();

    public int Count => poses.Count;

    // Where the fighter stood, and which way its body faced (degrees around the vertical axis), at this host time.
    // A pose older than the newest one is ignored, so a late call cannot rewrite the past. A pose at the same time as
    // the newest replaces it. Values that are not numbers are ignored.
    public void Record(double time, Vector3 position, float yaw)
    {
        if (!double.IsFinite(time) || !IsFinite(position) || !float.IsFinite(yaw))
            return;

        var pose = new Pose { Time = time, Position = position, Yaw = Mathf.Repeat(yaw, 360f) };
        if (poses.Count > 0)
        {
            double newest = poses[poses.Count - 1].Time;
            if (time < newest)
                return;
            if (time == newest)
            {
                poses[poses.Count - 1] = pose;
                return;
            }
        }

        poses.Add(pose);

        int firstNeeded = 0;
        while (firstNeeded + 1 < poses.Count && poses[firstNeeded + 1].Time <= time - KeepSeconds)
            firstNeeded++;
        if (poses.Count - firstNeeded > MaxPoses)
            firstNeeded = poses.Count - MaxPoses;
        if (firstNeeded > 0)
            poses.RemoveRange(0, firstNeeded);
    }

    // Where the fighter stood at a past moment, blended between the two poses around it. Facing blends the short way
    // round. A moment before the oldest pose gives the oldest, because the fighter was not recorded earlier (for
    // example just after spawning or a teleport). A moment after the newest gives the newest. Returns false only when
    // nothing is recorded or the moment is not a number.
    public bool TrySample(double time, out Vector3 position, out float yaw)
    {
        position = Vector3.zero;
        yaw = 0f;
        if (poses.Count == 0 || !double.IsFinite(time))
            return false;

        // A rewind looks only a short way back, so search from the newest pose.
        int after = poses.Count - 1;
        while (after >= 0 && poses[after].Time > time)
            after--;
        after++;

        Pose result;
        if (after == poses.Count)
        {
            result = poses[poses.Count - 1];
        }
        else if (after == 0)
        {
            result = poses[0];
        }
        else
        {
            Pose a = poses[after - 1];
            Pose b = poses[after];
            float t = (float)((time - a.Time) / (b.Time - a.Time));
            result = new Pose
            {
                Time = time,
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Yaw = Mathf.Repeat(Mathf.LerpAngle(a.Yaw, b.Yaw, t), 360f)
            };
        }

        position = result.Position;
        yaw = result.Yaw;
        return true;
    }

    // A teleport, such as the return to the spawn point, must not be blended with where the fighter stood before it.
    public void Clear()
    {
        poses.Clear();
    }

    // The host time a swing is checked against. The attacker's screen reports which host time it was showing. The host
    // never goes further back than limitSeconds and never into the future. A missing or broken report, or a limit that
    // is zero or not a number, gives the host's own present: no rewind.
    public static double RewindTime(double hostNow, double attackerViewTime, double limitSeconds)
    {
        if (!double.IsFinite(attackerViewTime) || !double.IsFinite(limitSeconds) || limitSeconds <= 0.0)
            return hostNow;
        return Math.Max(hostNow - limitSeconds, Math.Min(hostNow, attackerViewTime));
    }

    static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
