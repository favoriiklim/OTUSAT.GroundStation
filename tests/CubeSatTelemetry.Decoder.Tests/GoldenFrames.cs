namespace CubeSatTelemetry.Decoder.Tests;

/// <summary>
/// Reference frames produced outside this codebase.
///
/// The default frame was built in Python with struct.pack("&lt;HIfffffffBBB", ...) and a
/// separate bitwise CRC-16/CCITT, not with the decoder's own offsets or CRC. A wrong constant
/// on the decoder side therefore cannot hide itself by being used on both ends of a test.
/// Field values are the Coder's defaults; the leading bytes match the example in the
/// decoder README.
///
/// When the STM32 side produces a frame of its own, add it here as a second vector.
/// </summary>
internal static class GoldenFrames
{
    public const string DefaultHex =
        "CDAB15CD5B070000C44100507D44CDCCEC403333B33E00004CC10000604000003443020100A6DF";

    public const ushort DefaultCrc = 0xDFA6;

    // Byte offsets as literals from the protocol report, deliberately not the decoder's Off*
    // constants (see above).
    public const int FrameLength = 39;
    public const int PressureOffset = 10;
    public const int ModeOffset = 34;
    public const int CrcOffset = 37;

    public static byte[] Default() => Convert.FromHexString(DefaultHex);

    /// <summary>
    /// The default frame with one byte replaced and the CRC recomputed, so the frame stays
    /// valid. Uses the decoder's CRC, which <see cref="Crc16CcittTests"/> checks on its own
    /// against the standard vector first.
    /// </summary>
    public static byte[] DefaultWithByte(int offset, byte value)
    {
        var frame = Default();
        frame[offset] = value;

        var crc = Crc16Ccitt.Compute(frame.AsSpan(0, CrcOffset));
        frame[CrcOffset] = (byte)(crc & 0xFF);
        frame[CrcOffset + 1] = (byte)(crc >> 8);
        return frame;
    }

    /// <summary>
    /// Filler that cannot contain the sync word pair CD AB, so the only candidates in a test
    /// stream are the ones the test put there.
    /// </summary>
    public static byte[] Noise(int length) => Enumerable.Repeat((byte)0x55, length).ToArray();

    public static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();
}
