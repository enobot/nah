using NUnit.Framework;
using UnityEngine;

// The host's record of where a fighter stood, and the limit on how far back a swing is checked (the owner's
// middle-ground rule for hits across network delay). Plain C#: no network, scene, or assets needed.
public sealed class NahPoseHistoryTest
{
    const float Tolerance = 0.0001f;

    [Test]
    public void EmptyRecordFindsNothing()
    {
        var record = new NAHPoseHistory();
        Assert.IsFalse(record.TrySample(1.0, out _, out _), "An empty record found a pose.");
    }

    [Test]
    public void MomentBetweenTwoPosesIsBlended()
    {
        var record = new NAHPoseHistory();
        record.Record(1.0, new Vector3(0f, 1f, 0f), 10f);
        record.Record(1.1, new Vector3(2f, 1f, 4f), 30f);

        Assert.IsTrue(record.TrySample(1.025, out Vector3 position, out float yaw), "A moment inside the record was not found.");
        Assert.AreEqual(0.5f, position.x, Tolerance, "Wrong position along x a quarter of the way between two poses.");
        Assert.AreEqual(1f, position.y, Tolerance, "Wrong height between two poses.");
        Assert.AreEqual(1f, position.z, Tolerance, "Wrong position along z a quarter of the way between two poses.");
        Assert.AreEqual(15f, yaw, Tolerance, "Wrong facing a quarter of the way between two poses.");
    }

    [Test]
    public void FacingBlendsTheShortWayRound()
    {
        var record = new NAHPoseHistory();
        record.Record(1.0, Vector3.zero, 350f);
        record.Record(2.0, Vector3.zero, 10f);

        record.TrySample(1.5, out _, out float yaw);
        Assert.AreEqual(0f, Mathf.DeltaAngle(yaw, 0f), Tolerance, "Facing turned the long way round, through 180 degrees.");
        Assert.That(yaw, Is.GreaterThanOrEqualTo(0f).And.LessThan(360f), "Facing is not between 0 and 360 degrees.");
    }

    [Test]
    public void ExactPoseTimeGivesThatPose()
    {
        var record = new NAHPoseHistory();
        record.Record(1.0, new Vector3(1f, 0f, 0f), 0f);
        record.Record(1.1, new Vector3(2f, 0f, 0f), 90f);
        record.Record(1.2, new Vector3(3f, 0f, 0f), 180f);

        record.TrySample(1.1, out Vector3 position, out float yaw);
        Assert.AreEqual(2f, position.x, Tolerance, "The pose recorded at exactly that time was not returned.");
        Assert.AreEqual(90f, yaw, Tolerance, "The facing recorded at exactly that time was not returned.");
    }

    [Test]
    public void MomentsOutsideTheRecordUseTheNearestPose()
    {
        var record = new NAHPoseHistory();
        record.Record(1.0, new Vector3(1f, 0f, 0f), 0f);
        record.Record(1.1, new Vector3(2f, 0f, 0f), 0f);

        Assert.IsTrue(record.TrySample(0.5, out Vector3 before, out _), "A moment before the record was refused.");
        Assert.AreEqual(1f, before.x, Tolerance, "A moment before the record did not use the oldest pose.");
        Assert.IsTrue(record.TrySample(5.0, out Vector3 after, out _), "A moment after the record was refused.");
        Assert.AreEqual(2f, after.x, Tolerance, "A moment after the record did not use the newest pose.");
    }

    [Test]
    public void OldPosesAreDroppedButTheEdgeCanStillBeFound()
    {
        var record = new NAHPoseHistory();
        const double tick = 1.0 / 30.0;
        // Three seconds of walking along x at 1 m/s, recorded once per network tick.
        for (int i = 0; i <= 90; i++)
            record.Record(i * tick, new Vector3((float)(i * tick), 0f, 0f), 0f);

        double newest = 90 * tick;
        Assert.LessOrEqual(record.Count, (int)(NAHPoseHistory.KeepSeconds / tick) + 2,
            "Poses older than the keep time were not dropped.");

        double edge = newest - NAHPoseHistory.KeepSeconds + 0.01;
        Assert.IsTrue(record.TrySample(edge, out Vector3 position, out _), "A moment just inside the keep time was not found.");
        Assert.AreEqual((float)edge, position.x, 0.001f, "A moment just inside the keep time was not blended correctly.");
    }

    [Test]
    public void RecordStaysUnderTheSafetyLimitAtVeryHighFrameRates()
    {
        var record = new NAHPoseHistory();
        // 10,000 frames a second for one second.
        for (int i = 0; i < 10000; i++)
            record.Record(i * 0.0001, Vector3.zero, 0f);

        Assert.AreEqual(NAHPoseHistory.MaxPoses, record.Count, "The record grew past its safety limit.");
    }

    [Test]
    public void LateOrBrokenPosesAreIgnored()
    {
        var record = new NAHPoseHistory();
        record.Record(1.0, new Vector3(1f, 0f, 0f), 0f);

        record.Record(0.5, new Vector3(9f, 0f, 0f), 0f);
        record.TrySample(0.5, out Vector3 position, out _);
        Assert.AreEqual(1f, position.x, Tolerance, "A late pose rewrote the past.");

        record.Record(double.NaN, Vector3.zero, 0f);
        record.Record(2.0, new Vector3(float.NaN, 0f, 0f), 0f);
        record.Record(2.0, new Vector3(0f, float.PositiveInfinity, 0f), 0f);
        record.Record(2.0, Vector3.zero, float.NaN);
        Assert.AreEqual(1, record.Count, "A pose that is not a number was recorded.");
        Assert.IsFalse(record.TrySample(double.NaN, out _, out _), "A moment that is not a number was answered.");

        record.Record(1.0, new Vector3(4f, 0f, 0f), 0f);
        record.TrySample(1.0, out Vector3 replaced, out _);
        Assert.AreEqual(1, record.Count, "A pose at the same time was added instead of replacing the newest.");
        Assert.AreEqual(4f, replaced.x, Tolerance, "A pose at the same time did not replace the newest.");
    }

    [Test]
    public void ClearedRecordDoesNotBlendWithTheOldPlace()
    {
        var record = new NAHPoseHistory();
        record.Record(1.0, new Vector3(10f, 0f, 0f), 0f);
        record.Record(1.1, new Vector3(11f, 0f, 0f), 0f);

        // A teleport back to the spawn point.
        record.Clear();
        Assert.IsFalse(record.TrySample(1.1, out _, out _), "A cleared record still found a pose.");

        record.Record(1.2, Vector3.zero, 0f);
        record.TrySample(1.15, out Vector3 position, out _);
        Assert.AreEqual(0f, position.x, Tolerance, "The pose after a teleport was blended with the place before it.");
    }

    [Test]
    public void RewindStaysWithinTheLimit()
    {
        const double now = 10.0;
        const double limit = 0.15;

        Assert.AreEqual(9.9, NAHPoseHistory.RewindTime(now, 9.9, limit), 1e-9, "A rewind inside the limit was changed.");
        Assert.AreEqual(now - limit, NAHPoseHistory.RewindTime(now, 9.5, limit), 1e-9, "A rewind went past the limit.");
        Assert.AreEqual(now, NAHPoseHistory.RewindTime(now, 10.2, limit), 1e-9, "A report from the future was believed.");
        Assert.AreEqual(now, NAHPoseHistory.RewindTime(now, double.NaN, limit), 1e-9, "A broken report caused a rewind.");
        Assert.AreEqual(now, NAHPoseHistory.RewindTime(now, 9.9, 0.0), 1e-9, "A zero limit still rewound.");
        Assert.AreEqual(now, NAHPoseHistory.RewindTime(now, 9.9, -1.0), 1e-9, "A negative limit rewound.");
        Assert.AreEqual(now, NAHPoseHistory.RewindTime(now, 9.9, double.PositiveInfinity), 1e-9, "An endless limit rewound.");
    }
}
