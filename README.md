# OTUSAT Ground Station

Reception and storage service for the OTUSAT CubeSat ground segment. Architecture and
decisions: `OTUSAT_Mimari_Karar_Ozeti.pdf`.

## Current state — step 1: characterization tests

`src/CubeSatTelemetry.Decoder` is a verbatim copy of the decoder library from
https://github.com/favoriiklim/Telemetry-Decoder at commit
`7ddba8e3f8eae00b7510f25427518b7ff28bf933`. Not a single line of it has been changed.

`tests/CubeSatTelemetry.Decoder.Tests` pins the decoder's current behaviour, so that moving
it into `OTUSAT.Protocol` (step 2) can be shown not to change anything: the same tests must
pass, unchanged, after the move.

## Running

Requires the .NET 10 SDK.

```bash
dotnet test OTUSAT.GroundStation.slnx
```

## Test design

The tests do not build their expected bytes with the decoder's own `Off*` constants or CRC.
If a constant were wrong, a frame built from it would be decoded with the same wrong
constant and the test would still pass. Instead, the reference frame in `GoldenFrames.cs`
was produced outside this codebase (Python, `struct.pack` + a separate bitwise CRC), and the
offsets used in the tests are literals taken from the protocol report.

The CRC function is checked on its own against the standard check vector first; only after
that do some tests use it to re-seal a deliberately edited frame.
