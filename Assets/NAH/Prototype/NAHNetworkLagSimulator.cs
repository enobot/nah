#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Globalization;
using Unity.Collections;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;

// Development builds only. Adds delay, jitter, and packet loss to this peer's network traffic, so a test on
// one PC behaves more like the internet. Started with --nah-lag delay,jitter,drop (see NAHNetworkSession).
//
// UnityTransport.DebugSimulator in Netcode for GameObjects 2.13 no longer does anything; its notice points to the
// Multiplayer Tools package. Instead this uses the transport's documented INetworkStreamDriverConstructor hook and
// Unity Transport's own SimulatorPipelineStage, so no extra package is needed.
//
// In the editor and in development builds, Netcode already puts a SimulatorPipelineStage in its default pipelines
// (see the #define at the top of its UnityTransport.cs) but leaves its delay at zero. This hook sets the delay and
// adds the stage only where it is missing. A second stage would apply every delay twice (measured on 2026-11-01:
// "75 ms each way" became about 150 ms each way).
//
// The simulator delays packets in both directions on this peer, so the round trip grows by about twice the delay.
// Heartbeats and the connection handshake do not travel through pipelines, so they are not delayed or dropped.
public sealed class NAHNetworkLagSimulator : INetworkStreamDriverConstructor
{
    public const int MaxDelayMilliseconds = 1000;
    public const int MaxDropPercent = 50;
    // How many packets the simulator can hold per connection while they wait out their delay.
    const int MaxHeldPackets = 1000;

    public int DelayMilliseconds { get; }
    public int JitterMilliseconds { get; }
    public int DropPercent { get; }

    NAHNetworkLagSimulator(int delayMilliseconds, int jitterMilliseconds, int dropPercent)
    {
        DelayMilliseconds = delayMilliseconds;
        JitterMilliseconds = jitterMilliseconds;
        DropPercent = dropPercent;
    }

    public override string ToString()
    {
        return DelayMilliseconds + " ms each way, give or take " + JitterMilliseconds + " ms, " + DropPercent + "% of packets lost";
    }

    // Reads "delay,jitter,drop" in whole numbers, for example "75,15,3": 75 ms each way, give or take 15 ms,
    // with 3 percent of packets lost. Jitter may not exceed the delay, because a packet cannot arrive early.
    public static bool TryParse(string text, out NAHNetworkLagSimulator simulator)
    {
        simulator = null;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        string[] parts = text.Split(',');
        if (parts.Length != 3)
            return false;
        var values = new int[3];
        for (int i = 0; i < 3; i++)
        {
            if (!int.TryParse(parts[i].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out values[i]))
                return false;
        }
        if (values[0] > MaxDelayMilliseconds || values[1] > values[0] || values[2] > MaxDropPercent)
            return false;

        simulator = new NAHNetworkLagSimulator(values[0], values[1], values[2]);
        return true;
    }

    // Same as UnityTransport's own driver setup, with simulator settings and, where missing, the simulator stage at
    // the end of each pipeline. That is where Netcode puts it, so the reliable pipeline resends packets it drops.
    public void CreateDriver(
        UnityTransport transport,
        out NetworkDriver driver,
        out NetworkPipeline unreliableFragmentedPipeline,
        out NetworkPipeline unreliableSequencedFragmentedPipeline,
        out NetworkPipeline reliableSequencedPipeline)
    {
        var settings = transport.GetDefaultNetworkSettings();
        settings.WithSimulatorStageParameters(
            maxPacketCount: MaxHeldPackets,
            mode: ApplyMode.AllPackets,
            packetDelayMs: DelayMilliseconds,
            packetJitterMs: JitterMilliseconds,
            packetDropPercentage: DropPercent,
            randomSeed: (uint)System.Diagnostics.Stopwatch.GetTimestamp());
        driver = NetworkDriver.Create(new UDPNetworkInterface(), settings);

        transport.GetDefaultPipelineConfigurations(out var unreliable, out var unreliableSequenced, out var reliable);
        unreliableFragmentedPipeline = driver.CreatePipeline(WithSimulator(unreliable));
        unreliableSequencedFragmentedPipeline = driver.CreatePipeline(WithSimulator(unreliableSequenced));
        reliableSequencedPipeline = driver.CreatePipeline(WithSimulator(reliable));
    }

    static NativeArray<NetworkPipelineStageId> WithSimulator(NativeArray<NetworkPipelineStageId> stages)
    {
        var simulator = NetworkPipelineStageId.Get<SimulatorPipelineStage>();
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].Equals(simulator))
                return stages;
        }

        var result = new NativeArray<NetworkPipelineStageId>(stages.Length + 1, Allocator.Temp);
        NativeArray<NetworkPipelineStageId>.Copy(stages, result, stages.Length);
        result[stages.Length] = simulator;
        return result;
    }
}
#endif
