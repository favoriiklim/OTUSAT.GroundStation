namespace CubeSatTelemetry.Decoder.Tests;

public class StreamDecodeTests
{
    [Fact]
    public void EmptyStream_FindsNothing()
    {
        Assert.Empty(TelemetryDecoder.DecodeAll(ReadOnlySpan<byte>.Empty));
        Assert.Equal(-1, TelemetryDecoder.FindFrameStart(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void NoiseAroundFrame_IsSkipped()
    {
        var stream = GoldenFrames.Concat(GoldenFrames.Noise(7), GoldenFrames.Default(), GoldenFrames.Noise(11));

        var frames = TelemetryDecoder.DecodeAll(stream);

        var frame = Assert.Single(frames);
        Assert.Equal(7, frame.Offset);
        Assert.Equal(24.5f, frame.Packet.Temperature);
        Assert.Equal(7, TelemetryDecoder.FindFrameStart(stream));
    }

    [Fact]
    public void BackToBackFrames_AreAllReturned()
    {
        var stream = GoldenFrames.Concat(GoldenFrames.Default(), GoldenFrames.Default());

        var frames = TelemetryDecoder.DecodeAll(stream);

        Assert.Equal(new[] { 0, 39 }, frames.Select(f => f.Offset));
    }

    [Fact]
    public void FalseSyncInNoise_IsOnlyACandidate()
    {
        // CD AB followed by junk: the sync word matches, the CRC does not. The decoder must
        // step one byte forward rather than a whole frame, or it would jump over the real one.
        var falseSync = GoldenFrames.Concat([0xCD, 0xAB], GoldenFrames.Noise(10));
        var stream = GoldenFrames.Concat(falseSync, GoldenFrames.Default());

        var frames = TelemetryDecoder.DecodeAll(stream);

        var frame = Assert.Single(frames);
        Assert.Equal(12, frame.Offset);
    }

    [Fact]
    public void CorruptFrameBetweenValidOnes_IsSilentlySkipped()
    {
        // Current behaviour: a corrupt frame is neither returned nor reported. Step 3's
        // FrameAssembler adds the reporting; this test pins what the decoder alone does.
        var corrupt = GoldenFrames.Default();
        corrupt[GoldenFrames.PressureOffset] ^= 0x01;
        var stream = GoldenFrames.Concat(GoldenFrames.Default(), corrupt, GoldenFrames.Default());

        var frames = TelemetryDecoder.DecodeAll(stream);

        Assert.Equal(new[] { 0, 78 }, frames.Select(f => f.Offset));
    }

    [Fact]
    public void PartialFrameAtEnd_IsDiscarded()
    {
        // Current behaviour: nothing is carried over to a later call. Step 3's FrameAssembler
        // keeps the tail so a frame split across two serial reads is not lost.
        var stream = GoldenFrames.Concat(GoldenFrames.Default(), GoldenFrames.Default().AsSpan(0, 20).ToArray());

        var frames = TelemetryDecoder.DecodeAll(stream);

        var frame = Assert.Single(frames);
        Assert.Equal(0, frame.Offset);
    }
}
