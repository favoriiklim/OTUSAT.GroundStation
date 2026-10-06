namespace CubeSatTelemetry.Decoder.Tests;

public class DecodeTests
{
    [Fact]
    public void GoldenFrame_DecodesEveryField()
    {
        var result = TelemetryDecoder.Decode(GoldenFrames.Default());

        Assert.True(result.IsSuccess, result.ToString());
        var p = result.Packet!;

        // Exact comparisons on purpose: the reference frame and C# round the same literals to
        // the same binary32 values, so anything other than bit-for-bit equality is a bug.
        Assert.Equal((ushort)0xABCD, p.SyncWord);
        Assert.Equal(123_456_789u, p.Timestamp);
        Assert.Equal(24.5f, p.Temperature);
        Assert.Equal(1013.25f, p.Pressure);
        Assert.Equal(7.4f, p.BatteryVoltage);
        Assert.Equal(0.35f, p.BatteryCurrent);
        Assert.Equal(-12.75f, p.AttitudeRoll);
        Assert.Equal(3.5f, p.AttitudePitch);
        Assert.Equal(180.0f, p.AttitudeYaw);
        Assert.Equal(SatelliteMode.Nominal, p.Mode);
        Assert.Equal(SubsystemStatus.On, p.AdcsStatus);
        Assert.Equal(SubsystemStatus.Off, p.CameraStatus);
        Assert.Equal(GoldenFrames.DefaultCrc, p.Checksum);
    }

    [Fact]
    public void ByteMapConstants_MatchProtocolReport()
    {
        // The byte map is an external contract. If one of these changes, the comms document
        // has to change first.
        Assert.Equal(39, TelemetryPacket.FrameSize);
        Assert.Equal((ushort)0xABCD, TelemetryPacket.ExpectedSyncWord);
        Assert.Equal(0, TelemetryPacket.OffSyncWord);
        Assert.Equal(2, TelemetryPacket.OffTimestamp);
        Assert.Equal(6, TelemetryPacket.OffTemperature);
        Assert.Equal(10, TelemetryPacket.OffPressure);
        Assert.Equal(14, TelemetryPacket.OffBatteryVoltage);
        Assert.Equal(18, TelemetryPacket.OffBatteryCurrent);
        Assert.Equal(22, TelemetryPacket.OffAttitudeRoll);
        Assert.Equal(26, TelemetryPacket.OffAttitudePitch);
        Assert.Equal(30, TelemetryPacket.OffAttitudeYaw);
        Assert.Equal(34, TelemetryPacket.OffMode);
        Assert.Equal(35, TelemetryPacket.OffAdcsStatus);
        Assert.Equal(36, TelemetryPacket.OffCameraStatus);
        Assert.Equal(37, TelemetryPacket.OffChecksum);
        Assert.Equal(37, TelemetryPacket.CrcCoverage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(38)]
    public void ShortInput_IsTooShort(int length)
    {
        var result = TelemetryDecoder.Decode(GoldenFrames.Default().AsSpan(0, length));

        Assert.Equal(FrameError.TooShort, result.Error);
        Assert.Null(result.Packet);
    }

    [Fact]
    public void SyncWordInBigEndianOrder_IsBadSyncWord()
    {
        // On the wire the sync word is CD AB. AB CD is what a big-endian sender would emit.
        var frame = GoldenFrames.Default();
        (frame[0], frame[1]) = (frame[1], frame[0]);

        var result = TelemetryDecoder.Decode(frame);

        Assert.Equal(FrameError.BadSyncWord, result.Error);
        Assert.Null(result.Packet);
    }

    [Fact]
    public void FlippedPayloadByte_IsChecksumMismatch()
    {
        var frame = GoldenFrames.Default();
        frame[GoldenFrames.PressureOffset] ^= 0x01;

        Assert.Equal(FrameError.ChecksumMismatch, TelemetryDecoder.Decode(frame).Error);
    }

    [Fact]
    public void CorruptedCrcField_IsChecksumMismatch()
    {
        var frame = GoldenFrames.Default();
        frame[GoldenFrames.CrcOffset] ^= 0xFF;

        Assert.Equal(FrameError.ChecksumMismatch, TelemetryDecoder.Decode(frame).Error);
    }

    [Fact]
    public void CrcFieldInBigEndianOrder_IsChecksumMismatch()
    {
        // Pins the CRC byte order: A6 DF on the wire, not DF A6.
        var frame = GoldenFrames.Default();
        (frame[GoldenFrames.CrcOffset], frame[GoldenFrames.CrcOffset + 1]) =
            (frame[GoldenFrames.CrcOffset + 1], frame[GoldenFrames.CrcOffset]);

        Assert.Equal(FrameError.ChecksumMismatch, TelemetryDecoder.Decode(frame).Error);
    }

    [Fact]
    public void InputLongerThanOneFrame_DecodesFirst39Bytes()
    {
        // Current behaviour, pinned on purpose: trailing bytes are ignored, not reported.
        var input = GoldenFrames.Concat(GoldenFrames.Default(), GoldenFrames.Noise(5));

        var result = TelemetryDecoder.Decode(input);

        Assert.True(result.IsSuccess, result.ToString());
        Assert.Equal(24.5f, result.Packet!.Temperature);
    }

    [Fact]
    public void OutOfSpecMode_IsCarriedThroughAsIs()
    {
        // Losing the reading is worse than reporting an unknown value.
        var frame = GoldenFrames.DefaultWithByte(GoldenFrames.ModeOffset, 7);

        var result = TelemetryDecoder.Decode(frame);

        Assert.True(result.IsSuccess, result.ToString());
        Assert.Equal((byte)7, (byte)result.Packet!.Mode);
        Assert.False(Enum.IsDefined(result.Packet.Mode));
    }

    [Fact]
    public void TryParse_ValidFrame_ReturnsPacketAndEmptyError()
    {
        var ok = TelemetryDecoder.TryParse(GoldenFrames.Default(), out var packet, out var error);

        Assert.True(ok);
        Assert.NotNull(packet);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void TryParse_CorruptFrame_ReturnsNullAndMessage()
    {
        var frame = GoldenFrames.Default();
        frame[GoldenFrames.PressureOffset] ^= 0x01;

        var ok = TelemetryDecoder.TryParse(frame, out var packet, out var error);

        Assert.False(ok);
        Assert.Null(packet);
        Assert.False(string.IsNullOrEmpty(error));
    }
}
