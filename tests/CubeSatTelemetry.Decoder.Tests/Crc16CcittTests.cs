namespace CubeSatTelemetry.Decoder.Tests;

public class Crc16CcittTests
{
    [Fact]
    public void StandardCheckVector_Gives29B1()
    {
        Assert.Equal((ushort)0x29B1, Crc16Ccitt.Compute("123456789"u8));
    }

    [Fact]
    public void SelfTest_Passes()
    {
        Assert.True(Crc16Ccitt.SelfTest());
    }

    [Fact]
    public void EmptyInput_ReturnsInitValue()
    {
        // init = 0xFFFF and no final xor, so nothing in means the init value out.
        Assert.Equal((ushort)0xFFFF, Crc16Ccitt.Compute(ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void GoldenFrame_CrcOverFirst37Bytes_MatchesIndependentValue()
    {
        var frame = GoldenFrames.Default();

        Assert.Equal(GoldenFrames.DefaultCrc, Crc16Ccitt.Compute(frame.AsSpan(0, GoldenFrames.CrcOffset)));
    }
}
