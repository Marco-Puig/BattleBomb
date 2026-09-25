# M8 Plan 1 — The Wire and the Mirror (Tasks 86–96) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Two copies of the game on one PC play together: the second copy's controller drives Player 2 inside the host's game (Stage A — the remote controller), and the second copy draws the host's whole world — players, enemies, dummies, drops, bolts, hits, the stage streaming — as the host sees it (Stage B — the mirror).

**Architecture:** Host-authoritative (D58). The host's `SimulationDriver` runs exactly as today; the guest's device becomes `PlayerCommand`s sent over an `INetTransport` into a `RemoteCommandSource` on the host (the D10 dividend). The host sends 30 Hz snapshots and reliable events; the guest's driver runs in **replica mode** — it samples and sends its own commands, and applies interpolated snapshots to the same actors every Presentation and UI observer already reads. Pure wire code lives in `Core/Net` (EditMode-tested), transports in `Platform/Net`, the session and the host/guest halves in `Gameplay/Net`. No prediction yet (Plan 3), no menus online yet (Plan 2).

**Tech Stack:** Unity 6.5 (6000.5.8f1), C# (.NET Standard 2.1), `System.Net.Sockets` for the local transport, NUnit EditMode + PlayMode suites, Multiplayer Play Mode, the Unity MCP bridge.

**Source of scope:** `docs/HANDOFF-M8.md` — Stages A and B, planning decisions 1–10, 18 and 20. Decisions D58–D62. Readiness evidence: `docs/team/netcode/readiness.md`.

**Written against:** `main` after **Groundwork** (D57, `docs/superpowers/plans/2026-09-24-groundwork-input.md`) and the **Builder's pre-M8 batch** (destroyed enemies unregistered immediately; the DEBUG grant out of release; seeds from the session). This plan was written before either landed, from their plans and the code at `bd5f110`. **Before each task, re-read every file it modifies** — where Groundwork or the batch changed a line this plan quotes, keep their change and apply this plan's intent on top. Groundwork shapes this plan relies on: `InputSystemCommandSource` with an authored `_seat` (`PlayerId => new PlayerId(_seat)`, `SeatInput` built in `OnEnable`); `CommandButtons` with twelve flags (bits 0–11); `GameSession.Seats`.

Three consequences worth knowing before building (surface them in Task 96's close-out):

1. **Plan 1's guest plays the host's hero.** There is no online lobby until Plan 2; a guest connected when the host launches takes slot 2 with slot 1's `CharacterDefinition`, even over a local Player 2 join. Plan 2 Task 102 replaces this.
2. **Remote players cannot open screens yet.** A guest pressing Light beside a chest does nothing — the press is the chest's, and the host declines to open it — until Plan 2 brings the guest's chest screen online. And the host's settings menu still pauses the host — which freezes the guest's picture until it closes (Plan 2 Task 100 makes nothing pause online).
3. **Dev-only entry.** Hosting and joining is an IMGUI panel in editor and development builds (`NetDevOverlay`), not a menu. The real front door arrives with the lobby (Plan 2) and Steam (Plan 3).

---

## Before you start

- [ ] **The Unity MCP bridge must be connected.** It is the test gate. If `editor_status` does not answer, stop and ask Michael to open the project in Unity.
- [ ] `editor_status` — if the editor is in play mode, `editor_stop` (standing permission).
- [ ] Confirm the prerequisites are on `main`: `git log --oneline | grep -E "^.{8} (G14|G5):"` shows Groundwork's seats and D57 commits, and the Builder batch's commits are present (ask the orchestrator for their hashes if unsure). **Do not start without them.**
- [ ] Record the baseline: `run_tests` mode `EditMode` (record the count — Groundwork raised it past 626), and `run_tests` mode `PlayMode` with `async_tests: true`, poll `test_status` (record the count). Full EditMode output spills to a file: read the summary with `head -c 400 "<path>"` and failures with `grep -B3 -A8 '"Status": "Failed"' "<path>"`.
- [ ] After every PlayMode run, delete Unity's generated `Assets/InitTestScene*.unity` files and their `.meta`s.
- [ ] Bash heredocs break on apostrophes in this harness. Write C# with the Write tool, and data edits with python scripts written to the scratchpad.
- [ ] Two-editor runs (Task 90 onward) are heavy on the machine: ask the orchestrator for `QUIET ON` before starting one, and say when you are done.

**How to run a single EditMode fixture** (used throughout): `run_tests` with `mode: EditMode`, `filter_type: testName`, `filter: <FixtureName>`. Valid `filter_type` values are `testName`, `assembly`, `category` — anything else wedges the bridge.

**New folders need no `.meta` by hand** — Unity writes them on recompile. Create `Assets/_BattleBomb/Core/Net/`, `Assets/_BattleBomb/Platform/Net/`, `Assets/_BattleBomb/Gameplay/Net/`, `Assets/_BattleBomb/Tests/EditMode/Net/` by writing the first file into each; all four sit inside existing assemblies, so no `.asmdef` changes are needed.

**Commit convention:** one commit per task, on `main`, subject `<task number>: <what>`, body explaining why, ending with:

```
Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

Write the message to a scratchpad file with python and commit with `git commit -F <file>`. (In the team setup the sim holder sends `DONE` and the orchestrator commits — follow `docs/team/PROTOCOL.md` if it applies when you execute this.)

---

## File map

**Core** (`Assets/_BattleBomb/Core/`)
- Create `Net/NetFormatException.cs` — a malformed or oversized message.
- Create `Net/NetWriter.cs`, `Net/NetReader.cs` — little-endian bytes, bounded counts.
- Create `Net/NetQuantize.cs` — the stick as 16 bits per axis.
- Create `Net/NetProtocol.cs` — the version and every paper number from HANDOFF-M8.
- Create `Net/NetMessageKind.cs` — the first byte of every message.
- Create `Net/WireCommand.cs`, `Net/CommandCodec.cs` — commands on the wire.
- Create `Net/InputBuffer.cs`, `Net/RemoteCommandStream.cs` — the host's side of a remote pad.
- Create `Net/HandshakeCodec.cs` — Hello, Welcome, Refuse, Launch.
- Create `Net/EntityRef.cs`, `Net/Snapshots.cs` — the snapshot model.
- Create `Net/StateCodec.cs` — every travelling Core struct, field by field.
- Create `Net/SnapshotCodec.cs`, `Net/ReplicatedEvents.cs`, `Net/EventCodec.cs`, `Net/StageCodec.cs`, `Net/ItemWire.cs`.
- Create `Net/SnapshotBuffer.cs`, `Net/RenderClock.cs` — the guest's timeline.
- Modify `Combat/Health.cs`, `Stats/ManaPool.cs`, `Combat/AttackTuning.cs`, `Combat/StatusTrack.cs` — factories only (planning decision 2).

**Platform** (`Assets/_BattleBomb/Platform/Net/`)
- Create `NetChannel.cs`, `NetPeer.cs`, `NetEvent.cs`, `INetTransport.cs`.
- Create `LoopbackTransport.cs`, `LagProfile.cs`, `LagSimulator.cs`, `LocalSocketTransport.cs`.

**Gameplay** (`Assets/_BattleBomb/Gameplay/`)
- Create `Net/NetRole.cs`, `Net/NetSession.cs`, `Net/RemoteCommandSource.cs`, `Net/NetHost.cs`, `Net/NetGuest.cs`, `Net/ReplicaWorld.cs`, `Net/NetSeats.cs`.
- Modify `Session/GameSession.cs` — `Net`, `Roster`.
- Modify `Session/SessionBinder.cs` — online binding in `Awake`; no restore on a guest.
- Modify `Session/SaveService.cs` — never writes on a guest.
- Modify `Simulation/SimulationDriver.cs` — replica mode, the launch hold, `MayOpenScreen`, `SpawnPickup`, replica setters.
- Modify `Characters/CharacterActor.cs` — `BindSource`, `CaptureReplica`, `ApplyReplica`.
- Modify `Characters/EnemyActor.cs` — `NetId`, origin, `CaptureReplica`, `ApplyReplica`.
- Modify `Characters/TrainingDummy.cs` — `PropIndex`, `ApplyReplica`.
- Modify `Combat/EnemySpawner.cs` — network ids, origin, replica spawn/despawn.
- Modify `Loot/DropPickup.cs` — `NetId`.
- Modify `World/LoadedStage.cs` — dummy prop indices.
- Modify `World/StageRunner.cs` — load/hand-over hooks, the remote-ready gate, replica following.
- Modify `Players/InputSystemCommandSource.cs` — `UseSeat` (after Groundwork's rewrite).

**UI** — Create `Assets/_BattleBomb/UI/Debug/NetDevOverlay.cs` (no Platform reference: the session builds the local transport). Modify `UI/Frontend/FrontendFlow.cs` — one line, the roster onto the session.

**Packages** — `Packages/manifest.json`: `com.unity.multiplayer.playmode` (Task 90).

**Tests**
- Create `Tests/EditMode/Net/NetWireTests.cs`, `CommandCodecTests.cs`, `LoopbackTransportTests.cs`, `LagSimulatorTests.cs`, `LocalSocketTransportTests.cs`, `InputBufferTests.cs`, `RemoteCommandStreamTests.cs`, `HandshakeCodecTests.cs`, `FieldCoverage.cs`, `StateCodecCoverageTests.cs`, `SnapshotCodecTests.cs`, `EventCodecTests.cs`, `ItemWireTests.cs`, `SnapshotBufferTests.cs`, `RenderClockTests.cs`.
- Create `Tests/PlayMode/HeadlessGuest.cs`, `Tests/PlayMode/OnlineHostSmokeTests.cs`, `Tests/PlayMode/PlaybackTransport.cs`, `Tests/PlayMode/ReplicaReplaySmokeTests.cs` (which also carries the guest's `Stepped`-subscriber check — subscriptions only exist at runtime, so HANDOFF-M8's "acceptance test" for it is a PlayMode case).

**Docs** — `docs/HANDOFF-M8.md` (build log rows, Task 96 close-out), `docs/ROADMAP.md` (§4 M8 progress line).

---
## Stage A — The remote controller

### Task 86: The wire

Pure bytes, in Core, before anything touches a socket: a writer and a reader that never trust a count they read, the stick as 16 bits per axis, and the command packet. Everything later is built on these two classes.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/NetFormatException.cs`, `NetWriter.cs`, `NetReader.cs`, `NetQuantize.cs`, `NetProtocol.cs`, `NetMessageKind.cs`, `WireCommand.cs`, `CommandCodec.cs`
- Test: `Assets/_BattleBomb/Tests/EditMode/Net/NetWireTests.cs`, `CommandCodecTests.cs`

- [ ] **Step 1: Write the failing wire tests**

`Assets/_BattleBomb/Tests/EditMode/Net/NetWireTests.cs`:

```csharp
using BattleBomb.Core.Net;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class NetWireTests
    {
        [Test]
        public void Every_primitive_round_trips()
        {
            var writer = new NetWriter(4);
            writer.WriteByte(200);
            writer.WriteBool(true);
            writer.WriteBool(false);
            writer.WriteShort(-12345);
            writer.WriteUShort(54321);
            writer.WriteInt(-123456789);
            writer.WriteUInt(3000000000u);
            writer.WriteFloat(-1.5e-7f);
            writer.WriteVector2(new Vector2(0.25f, -8f));
            writer.WriteVector3(new Vector3(1f, 2.5f, -3.75f));
            writer.WriteString("Fire ↑ héros");

            var reader = new NetReader(writer.ToArray());
            Assert.That(reader.ReadByte(), Is.EqualTo(200));
            Assert.That(reader.ReadBool(), Is.True);
            Assert.That(reader.ReadBool(), Is.False);
            Assert.That(reader.ReadShort(), Is.EqualTo(-12345));
            Assert.That(reader.ReadUShort(), Is.EqualTo(54321));
            Assert.That(reader.ReadInt(), Is.EqualTo(-123456789));
            Assert.That(reader.ReadUInt(), Is.EqualTo(3000000000u));
            Assert.That(reader.ReadFloat(), Is.EqualTo(-1.5e-7f));
            Assert.That(reader.ReadVector2(), Is.EqualTo(new Vector2(0.25f, -8f)));
            Assert.That(reader.ReadVector3(), Is.EqualTo(new Vector3(1f, 2.5f, -3.75f)));
            Assert.That(reader.ReadString(), Is.EqualTo("Fire ↑ héros"));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void A_writer_grows_past_its_first_capacity()
        {
            var writer = new NetWriter(16);
            for (int i = 0; i < 1000; i++)
            {
                writer.WriteInt(i);
            }

            var reader = new NetReader(writer.Buffer, writer.Length);
            for (int i = 0; i < 1000; i++)
            {
                Assert.That(reader.ReadInt(), Is.EqualTo(i));
            }
        }

        [Test]
        public void Reading_past_the_end_is_a_format_error_not_a_crash()
        {
            var writer = new NetWriter();
            writer.WriteUShort(7);

            var reader = new NetReader(writer.ToArray());
            Assert.Throws<NetFormatException>(() => reader.ReadInt());
        }

        [Test]
        public void A_count_over_its_bound_is_refused_on_both_sides()
        {
            var writer = new NetWriter();
            Assert.Throws<NetFormatException>(() => writer.WriteCount(9, 8));

            writer.WriteUShort(9);
            var reader = new NetReader(writer.ToArray());
            Assert.Throws<NetFormatException>(() => reader.ReadCount(8));
        }

        [Test]
        public void A_string_longer_than_the_readers_limit_is_refused()
        {
            var writer = new NetWriter();
            writer.WriteString(new string('x', 300));

            var reader = new NetReader(writer.ToArray());
            Assert.Throws<NetFormatException>(() => reader.ReadString(256));
        }

        [Test]
        public void A_bool_byte_other_than_zero_or_one_is_malformed()
        {
            var reader = new NetReader(new byte[] { 2 });
            Assert.Throws<NetFormatException>(() => reader.ReadBool());
        }

        [Test]
        public void Reset_reuses_the_buffer_from_the_start()
        {
            var writer = new NetWriter();
            writer.WriteInt(1);
            writer.Reset();
            writer.WriteByte(9);

            Assert.That(writer.Length, Is.EqualTo(1));
            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 9 }));
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/CommandCodecTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class CommandCodecTests
    {
        [Test]
        public void Every_command_button_fits_the_sixteen_bits_the_wire_gives_it()
        {
            foreach (CommandButtons button in Enum.GetValues(typeof(CommandButtons)))
            {
                Assert.That((uint)button, Is.LessThan(1u << 16),
                    $"{button} does not fit a ushort. Widen CommandCodec's held field before adding it.");
            }
        }

        [Test]
        public void Quantising_the_stick_is_stable()
        {
            var stick = new Vector2(0.123456f, -0.987654f);
            Vector2 once = NetQuantize.Move(stick);
            Vector2 twice = NetQuantize.Move(once);

            Assert.That(twice, Is.EqualTo(once),
                "The guest predicts with the quantised stick and the host simulates with the decoded one; they must be the same number.");
            Assert.That(Mathf.Abs(once.x - stick.x), Is.LessThan(1e-4f));
            Assert.That(NetQuantize.Move(new Vector2(2f, -3f)), Is.EqualTo(new Vector2(1f, -1f)));
        }

        [Test]
        public void Two_players_commands_round_trip_with_the_quantised_stick()
        {
            var one = new List<WireCommand>
            {
                WireCommand.From(PlayerCommand.FromState(10, new Vector2(0.5f, 0f), CommandButtons.Light, CommandButtons.None)),
                WireCommand.From(PlayerCommand.FromState(11, new Vector2(1f, 0f), CommandButtons.None, CommandButtons.Light)),
            };
            var two = new List<WireCommand>
            {
                WireCommand.From(PlayerCommand.FromState(40, new Vector2(-0.3f, 0.7f), CommandButtons.Jump | CommandButtons.Heavy, CommandButtons.None)),
            };

            var decodedOne = RoundTrip(99, one, out int ackOne);
            var decodedTwo = RoundTrip(7, two, out int ackTwo);

            Assert.That(ackOne, Is.EqualTo(99));
            Assert.That(ackTwo, Is.EqualTo(7));
            Assert.That(decodedOne, Is.EqualTo(one));
            Assert.That(decodedTwo, Is.EqualTo(two));
        }

        [Test]
        public void Only_the_newest_four_commands_travel()
        {
            var sent = new List<WireCommand>();
            for (int frame = 1; frame <= 9; frame++)
            {
                sent.Add(new WireCommand(frame, Vector2.zero, CommandButtons.None));
            }

            var decoded = RoundTrip(0, sent, out _);

            Assert.That(decoded.Count, Is.EqualTo(NetProtocol.CommandRedundancy));
            Assert.That(decoded[0].Frame, Is.EqualTo(6));
            Assert.That(decoded[3].Frame, Is.EqualTo(9));
        }

        [Test]
        public void A_packet_claiming_more_than_four_commands_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteInt(0);
            writer.WriteByte(5);
            Assert.Throws<NetFormatException>(() => CommandCodec.Read(new NetReader(writer.ToArray()), new List<WireCommand>()));
        }

        [Test]
        public void A_button_bit_nothing_defines_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteInt(0);
            writer.WriteByte(1);
            writer.WriteInt(1);
            writer.WriteShort(0);
            writer.WriteShort(0);
            writer.WriteUShort(1 << 15);
            Assert.Throws<NetFormatException>(() => CommandCodec.Read(new NetReader(writer.ToArray()), new List<WireCommand>()));
        }

        private static List<WireCommand> RoundTrip(int ack, List<WireCommand> commands, out int readAck)
        {
            var writer = new NetWriter();
            CommandCodec.Write(writer, ack, commands);
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Commands));
            var decoded = new List<WireCommand>();
            readAck = CommandCodec.Read(reader, decoded);
            Assert.That(reader.Remaining, Is.Zero);
            return decoded;
        }
    }
}
```

- [ ] **Step 2: Run them to see them fail**

`recompile`, `recompile_status`. Expected: compile errors — `BattleBomb.Core.Net` does not exist. That is the failing state.

- [ ] **Step 3: Write the wire**

`Assets/_BattleBomb/Core/Net/NetFormatException.cs`:

```csharp
using System;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// A message that is truncated, oversized, or names something that cannot exist. The wire never
    /// trusts a count it read (HANDOFF-M8 planning decision 20): anything past the bytes it was
    /// given, or past the bound the reader was told, is this — never an out-of-range crash.
    /// </summary>
    public sealed class NetFormatException : Exception
    {
        public NetFormatException(string message) : base(message)
        {
        }
    }
}
```

`Assets/_BattleBomb/Core/Net/NetWriter.cs`:

```csharp
using System;
using System.Text;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Little-endian bytes for one message. Reused across messages with <see cref="Reset"/>, so a
    /// 30 Hz snapshot does not allocate a buffer every time it is written.
    /// </summary>
    public sealed class NetWriter
    {
        private byte[] _buffer;

        public NetWriter(int capacity = 256)
        {
            _buffer = new byte[Math.Max(16, capacity)];
        }

        public int Length { get; private set; }

        /// <summary>The bytes written so far live in [0, <see cref="Length"/>).</summary>
        public byte[] Buffer => _buffer;

        public void Reset() => Length = 0;

        public byte[] ToArray()
        {
            var copy = new byte[Length];
            Array.Copy(_buffer, copy, Length);
            return copy;
        }

        public void WriteByte(byte value)
        {
            Ensure(1);
            _buffer[Length++] = value;
        }

        public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        public void WriteShort(short value) => WriteUShort(unchecked((ushort)value));

        public void WriteUShort(ushort value)
        {
            Ensure(2);
            _buffer[Length++] = (byte)value;
            _buffer[Length++] = (byte)(value >> 8);
        }

        public void WriteInt(int value) => WriteUInt(unchecked((uint)value));

        public void WriteUInt(uint value)
        {
            Ensure(4);
            _buffer[Length++] = (byte)value;
            _buffer[Length++] = (byte)(value >> 8);
            _buffer[Length++] = (byte)(value >> 16);
            _buffer[Length++] = (byte)(value >> 24);
        }

        public void WriteFloat(float value) => WriteInt(BitConverter.SingleToInt32Bits(value));

        public void WriteVector2(Vector2 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
        }

        public void WriteVector3(Vector3 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
        }

        /// <summary>UTF-8 behind a 16-bit length. Null writes as empty.</summary>
        public void WriteString(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            if (bytes.Length > ushort.MaxValue)
            {
                throw new NetFormatException($"A {bytes.Length}-byte string does not fit a message.");
            }

            WriteUShort((ushort)bytes.Length);
            Ensure(bytes.Length);
            Array.Copy(bytes, 0, _buffer, Length, bytes.Length);
            Length += bytes.Length;
        }

        /// <summary>A list length, refused here if the reader would refuse it there.</summary>
        public void WriteCount(int count, int max)
        {
            if (count < 0 || count > max || max > ushort.MaxValue)
            {
                throw new NetFormatException($"Count {count} is outside 0..{max}.");
            }

            WriteUShort((ushort)count);
        }

        private void Ensure(int extra)
        {
            int needed = Length + extra;
            if (needed <= _buffer.Length)
            {
                return;
            }

            int size = _buffer.Length;
            while (size < needed)
            {
                size *= 2;
            }

            Array.Resize(ref _buffer, size);
        }
    }
}
```

`Assets/_BattleBomb/Core/Net/NetReader.cs`:

```csharp
using System;
using System.Text;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Reads what <see cref="NetWriter"/> wrote. Every read checks the bytes are there and every
    /// count is checked against a bound the caller names, so a malformed or hostile message is a
    /// <see cref="NetFormatException"/> at the first bad byte.
    /// </summary>
    public sealed class NetReader
    {
        private readonly byte[] _data;
        private readonly int _end;

        public NetReader(byte[] data) : this(data, data != null ? data.Length : 0)
        {
        }

        public NetReader(byte[] data, int length)
        {
            _data = data ?? Array.Empty<byte>();
            _end = Math.Max(0, Math.Min(length, _data.Length));
        }

        public int Position { get; private set; }

        public int Remaining => _end - Position;

        public byte ReadByte()
        {
            Need(1);
            return _data[Position++];
        }

        public bool ReadBool()
        {
            byte value = ReadByte();
            if (value > 1)
            {
                throw new NetFormatException($"A bool byte of {value}.");
            }

            return value == 1;
        }

        public short ReadShort() => unchecked((short)ReadUShort());

        public ushort ReadUShort()
        {
            Need(2);
            int at = Position;
            Position += 2;
            return (ushort)(_data[at] | (_data[at + 1] << 8));
        }

        public int ReadInt() => unchecked((int)ReadUInt());

        public uint ReadUInt()
        {
            Need(4);
            int at = Position;
            Position += 4;
            return unchecked((uint)(_data[at] | (_data[at + 1] << 8) | (_data[at + 2] << 16) | (_data[at + 3] << 24)));
        }

        public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());

        public Vector2 ReadVector2() => new Vector2(ReadFloat(), ReadFloat());

        public Vector3 ReadVector3() => new Vector3(ReadFloat(), ReadFloat(), ReadFloat());

        public string ReadString(int maxBytes = 4096)
        {
            int length = ReadUShort();
            if (length > maxBytes)
            {
                throw new NetFormatException($"A {length}-byte string where at most {maxBytes} is allowed.");
            }

            Need(length);
            string value = Encoding.UTF8.GetString(_data, Position, length);
            Position += length;
            return value;
        }

        public int ReadCount(int max)
        {
            int count = ReadUShort();
            if (count > max)
            {
                throw new NetFormatException($"A count of {count} where at most {max} is allowed.");
            }

            return count;
        }

        private void Need(int bytes)
        {
            if (bytes < 0 || Position + bytes > _end)
            {
                throw new NetFormatException(
                    $"Wanted {bytes} byte(s) at {Position} of a {_end}-byte message.");
            }
        }
    }
}
```

`Assets/_BattleBomb/Core/Net/NetQuantize.cs`:

```csharp
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The stick as 16 bits per axis (HANDOFF-M8 paper numbers). Applied on the guest <em>before</em>
    /// it sends or predicts, so the number its own prediction uses is the number the host simulates.
    /// </summary>
    public static class NetQuantize
    {
        public const float AxisScale = 32767f;

        public static short Axis(float value) =>
            (short)Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * AxisScale);

        public static float FromAxis(short value) => Mathf.Clamp(value / AxisScale, -1f, 1f);

        public static Vector2 Move(Vector2 move) =>
            new Vector2(FromAxis(Axis(move.x)), FromAxis(Axis(move.y)));
    }
}
```

`Assets/_BattleBomb/Core/Net/NetProtocol.cs`:

```csharp
namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The protocol version and HANDOFF-M8's paper numbers, in one place. Tuned live; a change to
    /// anything that alters the bytes on the wire bumps <see cref="Version"/>.
    /// </summary>
    public static class NetProtocol
    {
        /// <summary>A mismatch refuses the join with a readable reason (planning decision 20).</summary>
        public const int Version = 1;

        /// <summary>Each command packet carries this many of the newest commands, so one lost
        /// packet costs nothing.</summary>
        public const int CommandRedundancy = 4;

        /// <summary>A snapshot every second step: 30 Hz at the 60 Hz simulation.</summary>
        public const int SnapshotEverySteps = 2;

        /// <summary>How far behind the newest snapshot the guest draws: two snapshots and margin.</summary>
        public const int InterpolationDelaySteps = 6;

        /// <summary>The host's input buffer: commands held back to absorb jitter, and the depth past
        /// which it catches up by merging two into one step.</summary>
        public const int InputBufferTarget = 2;
        public const int InputBufferMax = 6;

        public const float KeepAliveSeconds = 0.25f;
        public const float ProblemAfterSeconds = 1f;

        /// <summary>Michael's call (design §1): about ten seconds of silence is a drop.</summary>
        public const float DropAfterSeconds = 10f;

        public const int MaxMessageBytes = 256 * 1024;
        public const int MaxEntities = 256;
        public const int MaxStatuses = 16;
        public const int MaxEvents = 512;
        public const int MaxItemJsonBytes = 8192;

        /// <summary>The local socket transport's port for two editors on one machine.</summary>
        public const int DevPort = 7777;
    }
}
```

`Assets/_BattleBomb/Core/Net/NetMessageKind.cs`:

```csharp
namespace BattleBomb.Core.Net
{
    /// <summary>The first byte of every message. Values are wire format: never renumber, only add.</summary>
    public enum NetMessageKind : byte
    {
        Hello = 1,
        Welcome = 2,
        Refuse = 3,
        Commands = 4,
        Launch = 5,
        Snapshot = 6,
        Events = 7,
        LoadStage = 8,
        StageReady = 9,
        HandOver = 10,
        KeepAlive = 11,
        SessionEnd = 12,
        Bye = 13,
    }
}
```

`Assets/_BattleBomb/Core/Net/WireCommand.cs`:

```csharp
using System;
using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// One command as it crosses the wire: the sender's frame, the quantised stick, and what was
    /// held. Press and release edges are not sent — the host re-derives them from consecutive held
    /// states (<see cref="RemoteCommandStream"/>), which is what lets a redundant copy arrive twice
    /// without pressing twice.
    /// </summary>
    public readonly struct WireCommand : IEquatable<WireCommand>
    {
        public readonly int Frame;
        public readonly Vector2 Move;
        public readonly CommandButtons Held;

        public WireCommand(int frame, Vector2 move, CommandButtons held)
        {
            Frame = frame;
            Move = move;
            Held = held;
        }

        public static WireCommand From(in PlayerCommand command) =>
            new WireCommand(command.Frame, NetQuantize.Move(command.Move), command.Held);

        public bool Equals(WireCommand other) =>
            Frame == other.Frame && Move == other.Move && Held == other.Held;

        public override bool Equals(object obj) => obj is WireCommand other && Equals(other);

        public override int GetHashCode() => Frame;

        public override string ToString() => $"#{Frame} {Move} [{Held}]";
    }
}
```

`Assets/_BattleBomb/Core/Net/CommandCodec.cs`:

```csharp
using System;
using System.Collections.Generic;
using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The guest's command packet: the newest host frame it has seen (for Plan 3's prediction) and
    /// its newest <see cref="NetProtocol.CommandRedundancy"/> commands, oldest first.
    /// </summary>
    public static class CommandCodec
    {
        private static readonly uint KnownButtons = AllButtons();

        /// <summary>The command with its stick quantised and its edges kept — what the guest both
        /// sends and (Plan 3) predicts with.</summary>
        public static PlayerCommand Quantized(in PlayerCommand command) => new PlayerCommand(
            command.Frame, NetQuantize.Move(command.Move), command.Held, command.Pressed, command.Released);

        public static void Write(NetWriter writer, int ackHostFrame, IReadOnlyList<WireCommand> newestLast)
        {
            writer.WriteByte((byte)NetMessageKind.Commands);
            writer.WriteInt(ackHostFrame);

            int count = Math.Min(newestLast.Count, NetProtocol.CommandRedundancy);
            writer.WriteByte((byte)count);
            for (int i = newestLast.Count - count; i < newestLast.Count; i++)
            {
                WireCommand command = newestLast[i];
                writer.WriteInt(command.Frame);
                writer.WriteShort(NetQuantize.Axis(command.Move.x));
                writer.WriteShort(NetQuantize.Axis(command.Move.y));
                writer.WriteUShort((ushort)(uint)command.Held);
            }
        }

        /// <summary>Reads a packet positioned after its kind byte. Returns the acknowledged host frame.</summary>
        public static int Read(NetReader reader, List<WireCommand> into)
        {
            into.Clear();
            int ack = reader.ReadInt();
            int count = reader.ReadByte();
            if (count > NetProtocol.CommandRedundancy)
            {
                throw new NetFormatException($"A command packet claiming {count} commands.");
            }

            for (int i = 0; i < count; i++)
            {
                int frame = reader.ReadInt();
                float x = NetQuantize.FromAxis(reader.ReadShort());
                float y = NetQuantize.FromAxis(reader.ReadShort());
                uint held = reader.ReadUShort();
                if ((held & ~KnownButtons) != 0)
                {
                    throw new NetFormatException($"Button bits 0x{held:X4} include ones nothing defines.");
                }

                into.Add(new WireCommand(frame, new Vector2(x, y), (CommandButtons)held));
            }

            return ack;
        }

        private static uint AllButtons()
        {
            uint all = 0;
            foreach (CommandButtons button in Enum.GetValues(typeof(CommandButtons)))
            {
                all |= (uint)button;
            }

            return all;
        }
    }
}
```

- [ ] **Step 4: Run the tests to see them pass**

`recompile`, `recompile_status`, `console` with `level: error` — clean. `run_tests` EditMode, `filter_type: testName`, `filter: NetWireTests`, then `CommandCodecTests`. Expected: all pass.

- [ ] **Step 5: Run the whole EditMode suite**

Expected: the baseline count plus the new tests, all green. `ArchitectureFitnessTests` must stay green — Core/Net uses only `System` and `UnityEngine` value types.

- [ ] **Step 6: Commit** — subject `86: the wire — bytes, the stick, the command packet`. Body: why Core owns the wire (EditMode-testable, planning decision 2), and why edges are re-derived rather than sent.

---

### Task 87: The transport seam

The Platform layer gets the seam every transport sits behind (planning decision 3), and the two that need no Steam: an in-memory loopback for tests, and a TCP socket on localhost for two editors. A lag simulator wraps either one.

**Files:**
- Create: `Assets/_BattleBomb/Platform/Net/NetChannel.cs`, `NetPeer.cs`, `NetEvent.cs`, `INetTransport.cs`, `LoopbackTransport.cs`, `LagProfile.cs`, `LagSimulator.cs`, `LocalSocketTransport.cs`
- Test: `Assets/_BattleBomb/Tests/EditMode/Net/LoopbackTransportTests.cs`, `LagSimulatorTests.cs`, `LocalSocketTransportTests.cs`

- [ ] **Step 1: Write the failing transport tests**

`Assets/_BattleBomb/Tests/EditMode/Net/LoopbackTransportTests.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Platform.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class LoopbackTransportTests
    {
        [Test]
        public void Connecting_announces_each_side_to_the_other()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport host, out LoopbackTransport guest);
            host.Listen();
            guest.Connect("loopback");

            Assert.That(Drain(host), Is.EqualTo(new[] { NetEvent.Connected(LoopbackTransport.GuestPeer) }));
            Assert.That(Drain(guest), Is.EqualTo(new[] { NetEvent.Connected(LoopbackTransport.HostPeer) }));
        }

        [Test]
        public void Connecting_to_nobody_listening_fails_as_a_disconnect()
        {
            LoopbackTransport.CreatePair(out LoopbackTransport host, out LoopbackTransport guest);
            guest.Connect("loopback");

            List<NetEvent> events = Drain(guest);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(NetEventKind.Disconnected));
            Assert.That(Drain(host), Is.Empty);
        }

        [Test]
        public void Messages_arrive_in_order_as_copies()
        {
            Connected(out LoopbackTransport host, out LoopbackTransport guest);
            var bytes = new byte[] { 1, 2, 3 };
            guest.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, bytes, 3);
            bytes[0] = 9;
            guest.Send(LoopbackTransport.HostPeer, NetChannel.Unreliable, bytes, 2);

            List<NetEvent> events = Drain(host);
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(events[0].Channel, Is.EqualTo(NetChannel.Reliable));
            Assert.That(events[1].Payload, Is.EqualTo(new byte[] { 9, 2 }));
            Assert.That(events[1].Peer, Is.EqualTo(LoopbackTransport.GuestPeer));
        }

        [Test]
        public void Disconnecting_tells_both_sides_and_stops_delivery()
        {
            Connected(out LoopbackTransport host, out LoopbackTransport guest);
            host.Disconnect(LoopbackTransport.GuestPeer);
            guest.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, new byte[] { 1 }, 1);

            Assert.That(Drain(host), Is.EqualTo(new[] { NetEvent.Disconnected(LoopbackTransport.GuestPeer) }));
            Assert.That(Drain(guest), Is.EqualTo(new[] { NetEvent.Disconnected(LoopbackTransport.HostPeer) }));
        }

        internal static void Connected(out LoopbackTransport host, out LoopbackTransport guest)
        {
            LoopbackTransport.CreatePair(out host, out guest);
            host.Listen();
            guest.Connect("loopback");
            Drain(host);
            Drain(guest);
        }

        internal static List<NetEvent> Drain(INetTransport transport)
        {
            var events = new List<NetEvent>();
            while (transport.TryReceive(out NetEvent netEvent))
            {
                events.Add(netEvent);
            }

            return events;
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/LagSimulatorTests.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Platform.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class LagSimulatorTests
    {
        [Test]
        public void A_message_arrives_half_a_round_trip_later()
        {
            Lagged(new LagProfile("Test", 100f, 0f, 0f), 1u, out LagSimulator sender, out LoopbackTransport receiver);

            sender.Update(0.0);
            sender.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, new byte[] { 7 }, 1);

            sender.Update(0.049);
            Assert.That(LoopbackTransportTests.Drain(receiver), Is.Empty, "Delivered before half the round trip.");

            sender.Update(0.051);
            List<NetEvent> events = LoopbackTransportTests.Drain(receiver);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Payload, Is.EqualTo(new byte[] { 7 }));
        }

        [Test]
        public void Reliable_messages_keep_their_order_through_jitter()
        {
            Lagged(new LagProfile("Test", 100f, 80f, 0f), 42u, out LagSimulator sender, out LoopbackTransport receiver);

            for (int i = 0; i < 50; i++)
            {
                sender.Update(i * 0.001);
                sender.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, new[] { (byte)i }, 1);
            }

            sender.Update(10.0);
            List<NetEvent> events = LoopbackTransportTests.Drain(receiver);
            Assert.That(events.Count, Is.EqualTo(50));
            for (int i = 0; i < 50; i++)
            {
                Assert.That(events[i].Payload[0], Is.EqualTo((byte)i), "Jitter reordered the reliable channel.");
            }
        }

        [Test]
        public void Loss_only_ever_touches_the_unreliable_channel()
        {
            Lagged(new LagProfile("Test", 0f, 0f, 1f), 3u, out LagSimulator sender, out LoopbackTransport receiver);

            sender.Update(0.0);
            sender.Send(LoopbackTransport.HostPeer, NetChannel.Unreliable, new byte[] { 1 }, 1);
            sender.Send(LoopbackTransport.HostPeer, NetChannel.Reliable, new byte[] { 2 }, 1);
            sender.Update(1.0);

            List<NetEvent> events = LoopbackTransportTests.Drain(receiver);
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Payload, Is.EqualTo(new byte[] { 2 }));
        }

        [Test]
        public void The_same_seed_loses_the_same_packets()
        {
            Assert.That(Survivors(99u), Is.EqualTo(Survivors(99u)));
            Assert.That(Survivors(99u), Is.Not.EqualTo(Survivors(100u)));
        }

        [Test]
        public void The_bad_profile_is_the_one_the_handoff_names()
        {
            Assert.That(LagProfile.Bad.RoundTripMs, Is.EqualTo(200f));
            Assert.That(LagProfile.Bad.JitterMs, Is.EqualTo(30f));
            Assert.That(LagProfile.Bad.UnreliableLoss, Is.EqualTo(0.02f));
            Assert.That(LagProfile.Normal.RoundTripMs, Is.EqualTo(100f));
            Assert.That(LagProfile.None.IsNone, Is.True);
        }

        private static List<byte> Survivors(uint seed)
        {
            Lagged(new LagProfile("Test", 0f, 0f, 0.5f), seed, out LagSimulator sender, out LoopbackTransport receiver);
            sender.Update(0.0);
            for (int i = 0; i < 64; i++)
            {
                sender.Send(LoopbackTransport.HostPeer, NetChannel.Unreliable, new[] { (byte)i }, 1);
            }

            sender.Update(1.0);
            var survivors = new List<byte>();
            foreach (NetEvent netEvent in LoopbackTransportTests.Drain(receiver))
            {
                survivors.Add(netEvent.Payload[0]);
            }

            return survivors;
        }

        /// <summary>A guest whose sends go through the simulator, and the host that receives them.</summary>
        private static void Lagged(LagProfile profile, uint seed, out LagSimulator sender, out LoopbackTransport receiver)
        {
            LoopbackTransport.CreatePair(out LoopbackTransport host, out LoopbackTransport guest);
            sender = new LagSimulator(guest, profile, seed);
            host.Listen();
            sender.Connect("loopback");
            LoopbackTransportTests.Drain(host);
            LoopbackTransportTests.Drain(sender);
            receiver = host;
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/LocalSocketTransportTests.cs`:

```csharp
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BattleBomb.Platform.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>A real TCP pair on localhost. Port 0 lets the OS choose, so a busy 7777 cannot fail it.</summary>
    public sealed class LocalSocketTransportTests
    {
        [Test]
        public void Two_sockets_connect_exchange_and_part()
        {
            var host = new LocalSocketTransport(0);
            var guest = new LocalSocketTransport(0);
            try
            {
                host.Listen();
                guest.Connect($"127.0.0.1:{host.BoundPort}");

                List<NetEvent> hostEvents = PumpUntil(host, guest, h => h.Count >= 1, out List<NetEvent> guestEvents);
                Assert.That(hostEvents[0].Kind, Is.EqualTo(NetEventKind.Connected));
                Assert.That(guestEvents.Exists(e => e.Kind == NetEventKind.Connected), Is.True);

                var big = new byte[70000];
                big[69999] = 42;
                guest.Send(guest.Peer, NetChannel.Reliable, new byte[] { 1, 2, 3 }, 3);
                guest.Send(guest.Peer, NetChannel.Unreliable, big, big.Length);

                hostEvents = PumpUntil(host, guest, h => h.Count >= 2, out _);
                Assert.That(hostEvents[0].Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.That(hostEvents[1].Channel, Is.EqualTo(NetChannel.Unreliable));
                Assert.That(hostEvents[1].Payload.Length, Is.EqualTo(70000), "A frame split across reads was not reassembled.");
                Assert.That(hostEvents[1].Payload[69999], Is.EqualTo(42));

                guest.Disconnect(guest.Peer);
                hostEvents = PumpUntil(host, guest, h => h.Count >= 1, out _);
                Assert.That(hostEvents[0].Kind, Is.EqualTo(NetEventKind.Disconnected));
            }
            finally
            {
                guest.Dispose();
                host.Dispose();
            }
        }

        private static List<NetEvent> PumpUntil(
            LocalSocketTransport host, LocalSocketTransport guest,
            System.Func<List<NetEvent>, bool> done, out List<NetEvent> guestEvents)
        {
            var hostEvents = new List<NetEvent>();
            guestEvents = new List<NetEvent>();
            Stopwatch clock = Stopwatch.StartNew();
            while (clock.Elapsed.TotalSeconds < 3.0)
            {
                host.Update(clock.Elapsed.TotalSeconds);
                guest.Update(clock.Elapsed.TotalSeconds);
                while (host.TryReceive(out NetEvent e))
                {
                    hostEvents.Add(e);
                }

                while (guest.TryReceive(out NetEvent e))
                {
                    guestEvents.Add(e);
                }

                if (done(hostEvents))
                {
                    return hostEvents;
                }

                Thread.Sleep(5);
            }

            Assert.Fail($"Timed out: the host saw {hostEvents.Count} event(s).");
            return hostEvents;
        }
    }
}
```

- [ ] **Step 2: Run them to see them fail**

`recompile`. Expected: compile errors — `BattleBomb.Platform.Net` does not exist.

- [ ] **Step 3: Write the seam**

`Assets/_BattleBomb/Platform/Net/NetChannel.cs`:

```csharp
namespace BattleBomb.Platform.Net
{
    /// <summary>Reliable is ordered and always arrives; unreliable may be lost, and newer wins.</summary>
    public enum NetChannel : byte
    {
        Reliable = 0,
        Unreliable = 1,
    }
}
```

`Assets/_BattleBomb/Platform/Net/NetPeer.cs`:

```csharp
using System;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// The other end of a connection, as the transport names it — a Steam ID under Steam, a small
    /// fixed number on the local transports. Opaque above the seam: nothing in Gameplay reads it
    /// as anything but "the peer" (D58 — nothing above the seam may assume Steam).
    /// </summary>
    public readonly struct NetPeer : IEquatable<NetPeer>
    {
        public readonly ulong Id;

        public NetPeer(ulong id)
        {
            Id = id;
        }

        public static NetPeer None => default;

        public bool IsNone => Id == 0;

        public bool Equals(NetPeer other) => Id == other.Id;

        public override bool Equals(object obj) => obj is NetPeer other && Equals(other);

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(NetPeer a, NetPeer b) => a.Id == b.Id;

        public static bool operator !=(NetPeer a, NetPeer b) => a.Id != b.Id;

        public override string ToString() => $"peer {Id}";
    }
}
```

`Assets/_BattleBomb/Platform/Net/NetEvent.cs`:

```csharp
using System;

namespace BattleBomb.Platform.Net
{
    public enum NetEventKind
    {
        Connected = 0,
        Disconnected = 1,
        Data = 2,
    }

    /// <summary>One thing a transport received. <see cref="Payload"/> is the receiver's own copy.</summary>
    public readonly struct NetEvent : IEquatable<NetEvent>
    {
        public readonly NetEventKind Kind;
        public readonly NetPeer Peer;
        public readonly NetChannel Channel;
        public readonly byte[] Payload;

        private NetEvent(NetEventKind kind, NetPeer peer, NetChannel channel, byte[] payload)
        {
            Kind = kind;
            Peer = peer;
            Channel = channel;
            Payload = payload;
        }

        public static NetEvent Connected(NetPeer peer) =>
            new NetEvent(NetEventKind.Connected, peer, NetChannel.Reliable, null);

        public static NetEvent Disconnected(NetPeer peer) =>
            new NetEvent(NetEventKind.Disconnected, peer, NetChannel.Reliable, null);

        public static NetEvent Data(NetPeer peer, NetChannel channel, byte[] payload) =>
            new NetEvent(NetEventKind.Data, peer, channel, payload);

        public bool Equals(NetEvent other) =>
            Kind == other.Kind && Peer == other.Peer && Channel == other.Channel && Payload == other.Payload;

        public override bool Equals(object obj) => obj is NetEvent other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 397) ^ Peer.GetHashCode();

        public override string ToString() => $"{Kind} {Peer} {Channel} {(Payload != null ? Payload.Length : 0)}B";
    }
}
```

`Assets/_BattleBomb/Platform/Net/INetTransport.cs`:

```csharp
using System;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// Everything the game asks of a network (HANDOFF-M8 planning decision 3). A host listens for
    /// one peer; a guest connects to an address the transport understands. Time is handed in, never
    /// read, so a lag simulator and a test can drive it. Implementations: <see cref="LoopbackTransport"/>
    /// (tests), <see cref="LocalSocketTransport"/> (two editors), and Steam's (Plan 3).
    /// </summary>
    public interface INetTransport : IDisposable
    {
        void Listen();

        void Connect(string address);

        /// <summary>Sends <paramref name="length"/> bytes of <paramref name="payload"/>. The
        /// transport copies what it needs; the caller may reuse the buffer at once.</summary>
        void Send(NetPeer peer, NetChannel channel, byte[] payload, int length);

        /// <summary>Pumps the network. Call once per frame, before draining <see cref="TryReceive"/>.</summary>
        void Update(double nowSeconds);

        bool TryReceive(out NetEvent netEvent);

        void Disconnect(NetPeer peer);
    }
}
```

`Assets/_BattleBomb/Platform/Net/LoopbackTransport.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// Two transports joined in memory, for tests: what one sends, the other receives on its next
    /// <see cref="TryReceive"/>, in order and as a copy. No time passes unless a
    /// <see cref="LagSimulator"/> wraps it.
    /// </summary>
    public sealed class LoopbackTransport : INetTransport
    {
        /// <summary>How the guest sees the host, and how the host sees the guest.</summary>
        public static readonly NetPeer HostPeer = new NetPeer(1);
        public static readonly NetPeer GuestPeer = new NetPeer(2);

        private readonly Queue<NetEvent> _inbox = new Queue<NetEvent>();
        private readonly NetPeer _self;
        private LoopbackTransport _partner;
        private bool _listening;
        private bool _connected;

        private LoopbackTransport(NetPeer self)
        {
            _self = self;
        }

        public static void CreatePair(out LoopbackTransport host, out LoopbackTransport guest)
        {
            host = new LoopbackTransport(HostPeer);
            guest = new LoopbackTransport(GuestPeer);
            host._partner = guest;
            guest._partner = host;
        }

        public bool IsConnected => _connected;

        public void Listen() => _listening = true;

        public void Connect(string address)
        {
            if (_connected || !_partner._listening)
            {
                _inbox.Enqueue(NetEvent.Disconnected(_partner._self));
                return;
            }

            _connected = true;
            _partner._connected = true;
            _partner._inbox.Enqueue(NetEvent.Connected(_self));
            _inbox.Enqueue(NetEvent.Connected(_partner._self));
        }

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            if (!_connected || peer != _partner._self)
            {
                return;
            }

            var copy = new byte[length];
            Array.Copy(payload, copy, length);
            _partner._inbox.Enqueue(NetEvent.Data(_self, channel, copy));
        }

        public void Update(double nowSeconds)
        {
        }

        public bool TryReceive(out NetEvent netEvent)
        {
            if (_inbox.Count > 0)
            {
                netEvent = _inbox.Dequeue();
                return true;
            }

            netEvent = default;
            return false;
        }

        public void Disconnect(NetPeer peer)
        {
            if (!_connected)
            {
                return;
            }

            _connected = false;
            _partner._connected = false;
            _partner._inbox.Enqueue(NetEvent.Disconnected(_self));
            _inbox.Enqueue(NetEvent.Disconnected(_partner._self));
        }

        public void Dispose() => Disconnect(_partner._self);
    }
}
```

`Assets/_BattleBomb/Platform/Net/LagProfile.cs`:

```csharp
namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// A made-up connection, for testing feel on one machine (HANDOFF-M8 paper numbers). The round
    /// trip is split evenly each way; jitter is spread either side of it; loss only ever hits the
    /// unreliable channel, because a real reliable channel resends until it arrives.
    /// </summary>
    public readonly struct LagProfile
    {
        public readonly string Name;
        public readonly float RoundTripMs;
        public readonly float JitterMs;
        public readonly float UnreliableLoss;

        public LagProfile(string name, float roundTripMs, float jitterMs, float unreliableLoss)
        {
            Name = name;
            RoundTripMs = roundTripMs > 0f ? roundTripMs : 0f;
            JitterMs = jitterMs > 0f ? jitterMs : 0f;
            UnreliableLoss = unreliableLoss < 0f ? 0f : unreliableLoss > 1f ? 1f : unreliableLoss;
        }

        public static LagProfile None => new LagProfile("None", 0f, 0f, 0f);

        /// <summary>A decent home connection.</summary>
        public static LagProfile Normal => new LagProfile("Normal", 100f, 10f, 0f);

        /// <summary>The one to make feel acceptable.</summary>
        public static LagProfile Bad => new LagProfile("Bad", 200f, 30f, 0.02f);

        public bool IsNone => RoundTripMs <= 0f && JitterMs <= 0f && UnreliableLoss <= 0f;

        public override string ToString() => $"{Name} ({RoundTripMs:0} ms ±{JitterMs * 0.5f:0}, {UnreliableLoss:P0} loss)";
    }
}
```

`Assets/_BattleBomb/Platform/Net/LagSimulator.cs`:

```csharp
using System;
using System.Collections.Generic;
using BattleBomb.Core.Loot;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// Wraps any transport and holds each outgoing message back by half a <see cref="LagProfile"/>
    /// round trip, give or take jitter, dropping some unreliable ones. Seeded, so a test that loses
    /// packets loses the same ones every run. Each machine wraps its own transport, so the two
    /// one-way delays add up to the profile's round trip.
    /// </summary>
    public sealed class LagSimulator : INetTransport
    {
        private readonly INetTransport _inner;
        private readonly List<Pending> _pending = new List<Pending>();
        private DeterministicRandom _random;
        private double _now;
        private double _lastReliableAt;
        private long _order;

        public LagSimulator(INetTransport inner, LagProfile profile, uint seed)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            Profile = profile;
            _random = new DeterministicRandom(seed);
        }

        public LagProfile Profile { get; }

        public INetTransport Inner => _inner;

        public void Listen() => _inner.Listen();

        public void Connect(string address) => _inner.Connect(address);

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            if (Profile.IsNone)
            {
                _inner.Send(peer, channel, payload, length);
                return;
            }

            _random = _random.NextFloat(out float lossRoll);
            if (channel == NetChannel.Unreliable && lossRoll < Profile.UnreliableLoss)
            {
                return;
            }

            _random = _random.NextFloat(out float jitterRoll);
            double oneWay = (Profile.RoundTripMs * 0.5 + (jitterRoll - 0.5) * Profile.JitterMs) / 1000.0;
            double at = _now + Math.Max(0.0, oneWay);
            if (channel == NetChannel.Reliable)
            {
                at = Math.Max(at, _lastReliableAt);
                _lastReliableAt = at;
            }

            var copy = new byte[length];
            Array.Copy(payload, copy, length);
            var pending = new Pending(at, _order++, peer, channel, copy);

            int index = _pending.Count;
            while (index > 0 && _pending[index - 1].At > at)
            {
                index--;
            }

            _pending.Insert(index, pending);
        }

        public void Update(double nowSeconds)
        {
            _now = nowSeconds;
            int due = 0;
            while (due < _pending.Count && _pending[due].At <= nowSeconds)
            {
                Pending pending = _pending[due];
                _inner.Send(pending.Peer, pending.Channel, pending.Payload, pending.Payload.Length);
                due++;
            }

            _pending.RemoveRange(0, due);
            _inner.Update(nowSeconds);
        }

        public bool TryReceive(out NetEvent netEvent) => _inner.TryReceive(out netEvent);

        /// <summary>A disconnect drops whatever was still in flight, as a real one does.</summary>
        public void Disconnect(NetPeer peer)
        {
            _pending.Clear();
            _inner.Disconnect(peer);
        }

        public void Dispose()
        {
            _pending.Clear();
            _inner.Dispose();
        }

        private readonly struct Pending
        {
            public readonly double At;
            public readonly long Order;
            public readonly NetPeer Peer;
            public readonly NetChannel Channel;
            public readonly byte[] Payload;

            public Pending(double at, long order, NetPeer peer, NetChannel channel, byte[] payload)
            {
                At = at;
                Order = order;
                Peer = peer;
                Channel = channel;
                Payload = payload;
            }
        }
    }
}
```

`Assets/_BattleBomb/Platform/Net/LocalSocketTransport.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BattleBomb.Core.Net;

namespace BattleBomb.Platform.Net
{
    /// <summary>
    /// TCP on localhost, for two editors on one machine under Multiplayer Play Mode (HANDOFF-M8
    /// planning decision 4). TCP because a development transport gets its reliable channel for
    /// free; the unreliable channel rides the same stream, and a <see cref="LagSimulator"/> supplies
    /// the loss a real one would have. Each message is framed as a 4-byte length, a channel byte,
    /// and the payload. One peer at a time: a host accepts the next one only once the last has gone.
    /// </summary>
    public sealed class LocalSocketTransport : INetTransport
    {
        private const int HeaderBytes = 5;

        private static readonly NetPeer TheHost = new NetPeer(1);
        private static readonly NetPeer TheGuest = new NetPeer(2);

        private readonly int _port;
        private readonly Queue<NetEvent> _inbox = new Queue<NetEvent>();
        private readonly byte[] _header = new byte[HeaderBytes];
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Task _connecting;
        private byte[] _received = new byte[64 * 1024];
        private int _receivedLength;

        public LocalSocketTransport(int port)
        {
            _port = port;
        }

        /// <summary>The port actually listened on — the OS's choice when constructed with 0.</summary>
        public int BoundPort => _listener != null ? ((IPEndPoint)_listener.LocalEndpoint).Port : _port;

        /// <summary>Who is on the other end right now, or <see cref="NetPeer.None"/>.</summary>
        public NetPeer Peer { get; private set; }

        public void Listen()
        {
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start(1);
        }

        public void Connect(string address)
        {
            int colon = address != null ? address.LastIndexOf(':') : -1;
            if (colon <= 0 || !int.TryParse(address.Substring(colon + 1), out int port))
            {
                throw new ArgumentException($"'{address}' is not host:port.", nameof(address));
            }

            _client = new TcpClient { NoDelay = true };
            _connecting = _client.ConnectAsync(address.Substring(0, colon), port);
        }

        public void Update(double nowSeconds)
        {
            if (_listener != null && _stream == null && _listener.Pending())
            {
                _client = _listener.AcceptTcpClient();
                _client.NoDelay = true;
                _stream = _client.GetStream();
                Peer = TheGuest;
                _inbox.Enqueue(NetEvent.Connected(Peer));
            }

            if (_connecting != null && _connecting.IsCompleted)
            {
                bool ok = !_connecting.IsFaulted && !_connecting.IsCanceled && _client != null && _client.Connected;
                _connecting = null;
                if (ok)
                {
                    _stream = _client.GetStream();
                    Peer = TheHost;
                    _inbox.Enqueue(NetEvent.Connected(Peer));
                }
                else
                {
                    Close();
                    _inbox.Enqueue(NetEvent.Disconnected(TheHost));
                }
            }

            if (_stream != null)
            {
                Pump();
            }
        }

        public bool TryReceive(out NetEvent netEvent)
        {
            if (_inbox.Count > 0)
            {
                netEvent = _inbox.Dequeue();
                return true;
            }

            netEvent = default;
            return false;
        }

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
            if (_stream == null || peer != Peer)
            {
                return;
            }

            _header[0] = (byte)length;
            _header[1] = (byte)(length >> 8);
            _header[2] = (byte)(length >> 16);
            _header[3] = (byte)(length >> 24);
            _header[4] = (byte)channel;
            try
            {
                _stream.Write(_header, 0, HeaderBytes);
                _stream.Write(payload, 0, length);
            }
            catch (Exception e) when (IsSocketFailure(e))
            {
                Lost();
            }
        }

        public void Disconnect(NetPeer peer)
        {
            if (_stream == null || peer != Peer)
            {
                return;
            }

            Lost();
        }

        public void Dispose()
        {
            Close();
            _listener?.Stop();
            _listener = null;
        }

        private void Pump()
        {
            try
            {
                while (_stream != null && _stream.DataAvailable)
                {
                    if (_receivedLength == _received.Length)
                    {
                        Array.Resize(ref _received, _received.Length * 2);
                    }

                    int read = _stream.Read(_received, _receivedLength, _received.Length - _receivedLength);
                    if (read <= 0)
                    {
                        Lost();
                        return;
                    }

                    _receivedLength += read;
                    SplitFrames();
                }

                if (_client != null && _client.Client.Poll(0, SelectMode.SelectRead) && _client.Client.Available == 0)
                {
                    Lost();
                }
            }
            catch (Exception e) when (IsSocketFailure(e))
            {
                Lost();
            }
        }

        private void SplitFrames()
        {
            int offset = 0;
            while (_stream != null && _receivedLength - offset >= HeaderBytes)
            {
                int length = _received[offset] | (_received[offset + 1] << 8)
                    | (_received[offset + 2] << 16) | (_received[offset + 3] << 24);
                if (length < 0 || length > NetProtocol.MaxMessageBytes)
                {
                    Lost();
                    return;
                }

                if (_receivedLength - offset - HeaderBytes < length)
                {
                    break;
                }

                var channel = (NetChannel)_received[offset + 4];
                var payload = new byte[length];
                Array.Copy(_received, offset + HeaderBytes, payload, 0, length);
                _inbox.Enqueue(NetEvent.Data(Peer, channel, payload));
                offset += HeaderBytes + length;
            }

            if (offset > 0 && _stream != null)
            {
                Array.Copy(_received, offset, _received, 0, _receivedLength - offset);
                _receivedLength -= offset;
            }
        }

        private void Lost()
        {
            NetPeer lost = Peer;
            Close();
            _inbox.Enqueue(NetEvent.Disconnected(lost));
        }

        private void Close()
        {
            _stream?.Dispose();
            _client?.Close();
            _stream = null;
            _client = null;
            _connecting = null;
            _receivedLength = 0;
            Peer = NetPeer.None;
        }

        private static bool IsSocketFailure(Exception e) =>
            e is IOException || e is SocketException || e is ObjectDisposedException || e is InvalidOperationException;
    }
}
```

- [ ] **Step 4: Run the tests to see them pass**

`recompile`, `console` `level: error` — clean. `run_tests` EditMode for `LoopbackTransportTests`, `LagSimulatorTests`, `LocalSocketTransportTests`. Expected: all pass. If the socket test times out, check `console` for a firewall or socket error — on Windows, loopback needs no firewall rule; if one appears, stop and tell Michael rather than changing system settings (a prohibited action for Claude).

- [ ] **Step 5: Run the whole EditMode suite.** Expected: green.

- [ ] **Step 6: Commit** — subject `87: the transport seam — loopback, local socket, lag`. Body: the seam is Platform's (rule 6, D58: nothing above it assumes Steam); TCP for the development transport because reliable comes free; the lag simulator is seeded so tests that lose packets are repeatable.

---

### Task 88: The input buffer and the remote source

The host's side of a remote pad: commands arrive late, twice, out of order, or not at all, and the simulation still gets exactly one command per step. Presses are re-derived from held states, so a redundant copy never presses twice and a starved step never invents a press.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/InputBuffer.cs`, `Assets/_BattleBomb/Core/Net/RemoteCommandStream.cs`, `Assets/_BattleBomb/Gameplay/Net/RemoteCommandSource.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs` (`BindSource`)
- Test: `Assets/_BattleBomb/Tests/EditMode/Net/InputBufferTests.cs`, `RemoteCommandStreamTests.cs`

- [ ] **Step 1: Write the failing tests**

`Assets/_BattleBomb/Tests/EditMode/Net/InputBufferTests.cs`:

```csharp
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class InputBufferTests
    {
        [Test]
        public void Out_of_order_arrivals_come_out_in_frame_order()
        {
            var buffer = new InputBuffer();
            buffer.Add(At(3));
            buffer.Add(At(1));
            buffer.Add(At(2));

            Assert.That(Take(buffer), Is.EqualTo(1));
            Assert.That(Take(buffer), Is.EqualTo(2));
            Assert.That(Take(buffer), Is.EqualTo(3));
        }

        [Test]
        public void A_redundant_copy_is_kept_once()
        {
            var buffer = new InputBuffer();
            Assert.That(buffer.Add(At(5)), Is.True);
            Assert.That(buffer.Add(At(5)), Is.False);
            Assert.That(buffer.Count, Is.EqualTo(1));
        }

        [Test]
        public void A_command_older_than_one_already_taken_is_ignored()
        {
            var buffer = new InputBuffer();
            buffer.Add(At(5));
            Take(buffer);

            Assert.That(buffer.Add(At(5)), Is.False);
            Assert.That(buffer.Add(At(4)), Is.False);
            Assert.That(buffer.LastTakenFrame, Is.EqualTo(5));
        }

        [Test]
        public void A_flood_is_bounded_by_dropping_the_oldest()
        {
            var buffer = new InputBuffer();
            for (int frame = 1; frame <= InputBuffer.MaxQueued + 10; frame++)
            {
                buffer.Add(At(frame));
            }

            Assert.That(buffer.Count, Is.EqualTo(InputBuffer.MaxQueued));
            Assert.That(Take(buffer), Is.EqualTo(11));
        }

        private static WireCommand At(int frame) => new WireCommand(frame, Vector2.zero, CommandButtons.None);

        private static int Take(InputBuffer buffer)
        {
            Assert.That(buffer.TryTake(out WireCommand command), Is.True);
            return command.Frame;
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/RemoteCommandStreamTests.cs`:

```csharp
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class RemoteCommandStreamTests
    {
        [Test]
        public void Nothing_plays_until_the_buffer_holds_its_target_depth()
        {
            var stream = new RemoteCommandStream(target: 2, max: 6);
            stream.Receive(new WireCommand(1, Vector2.right, CommandButtons.None));

            Assert.That(stream.Next(100).Move, Is.EqualTo(Vector2.zero), "Played before the buffer was primed.");

            stream.Receive(new WireCommand(2, Vector2.right, CommandButtons.None));
            Assert.That(stream.Next(101).Move, Is.EqualTo(Vector2.right));
            Assert.That(stream.LastConsumedFrame, Is.EqualTo(1));
        }

        [Test]
        public void A_press_is_one_edge_even_when_its_packet_arrives_twice()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.zero, CommandButtons.Light));
            stream.Receive(new WireCommand(3, Vector2.zero, CommandButtons.Light));
            stream.Receive(new WireCommand(4, Vector2.zero, CommandButtons.Light));

            PlayerCommand first = stream.Next(12);
            PlayerCommand second = stream.Next(13);

            Assert.That(first.WasPressed(CommandButtons.Light), Is.True);
            Assert.That(second.WasPressed(CommandButtons.Light), Is.False);
            Assert.That(second.IsHeld(CommandButtons.Light), Is.True);
        }

        [Test]
        public void A_starved_step_repeats_what_was_held_and_never_a_press()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.left, CommandButtons.Jump));
            Assert.That(stream.Next(12).WasPressed(CommandButtons.Jump), Is.True);

            PlayerCommand starved = stream.Next(13);

            Assert.That(starved.IsHeld(CommandButtons.Jump), Is.True);
            Assert.That(starved.Move, Is.EqualTo(Vector2.left));
            Assert.That(starved.Pressed, Is.EqualTo(CommandButtons.None), "A lost packet became a second jump.");
            Assert.That(stream.StarvedSteps, Is.EqualTo(1));
        }

        [Test]
        public void Catching_up_merges_two_steps_and_keeps_both_presses()
        {
            var stream = new RemoteCommandStream(target: 1, max: 2);
            stream.Receive(new WireCommand(1, Vector2.zero, CommandButtons.Light));
            stream.Receive(new WireCommand(2, Vector2.zero, CommandButtons.Heavy));
            stream.Receive(new WireCommand(3, Vector2.zero, CommandButtons.Heavy));
            stream.Receive(new WireCommand(4, Vector2.zero, CommandButtons.Heavy));

            PlayerCommand merged = stream.Next(50);

            Assert.That(merged.WasPressed(CommandButtons.Light), Is.True);
            Assert.That(merged.WasPressed(CommandButtons.Heavy), Is.True);
            Assert.That(merged.WasReleased(CommandButtons.Light), Is.True);
            Assert.That(merged.IsHeld(CommandButtons.Heavy), Is.True);
            Assert.That(stream.MergedSteps, Is.EqualTo(1));
            Assert.That(stream.LastConsumedFrame, Is.EqualTo(2));
        }

        [Test]
        public void Two_remote_players_streams_never_mix()
        {
            var one = Primed();
            var two = Primed();
            one.Receive(new WireCommand(3, Vector2.right, CommandButtons.Light));
            two.Receive(new WireCommand(3, Vector2.left, CommandButtons.None));

            Assert.That(one.Next(12).WasPressed(CommandButtons.Light), Is.True);
            Assert.That(two.Next(12).Move, Is.EqualTo(Vector2.left));
            Assert.That(two.Next(13).Pressed, Is.EqualTo(CommandButtons.None));
        }

        [Test]
        public void Release_forgets_everything_so_a_rejoin_starts_from_frame_zero()
        {
            var stream = Primed();
            stream.Receive(new WireCommand(3, Vector2.right, CommandButtons.Light));
            stream.Next(12);

            stream.Release();
            Assert.That(stream.Next(13).Held, Is.EqualTo(CommandButtons.None));

            stream.Receive(new WireCommand(0, Vector2.up, CommandButtons.None));
            stream.Receive(new WireCommand(1, Vector2.up, CommandButtons.None));
            Assert.That(stream.Next(14).Move, Is.EqualTo(Vector2.up), "A restarted guest's frame 0 was refused as old.");
        }

        /// <summary>A stream with frames 1–2 already played through, so the next command is live.</summary>
        private static RemoteCommandStream Primed()
        {
            var stream = new RemoteCommandStream(target: 2, max: 6);
            stream.Receive(new WireCommand(1, Vector2.zero, CommandButtons.None));
            stream.Receive(new WireCommand(2, Vector2.zero, CommandButtons.None));
            stream.Next(10);
            stream.Next(11);
            return stream;
        }
    }
}
```

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors, the types do not exist.

- [ ] **Step 3: Write the buffer and the stream**

`Assets/_BattleBomb/Core/Net/InputBuffer.cs`:

```csharp
using System.Collections.Generic;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// One remote player's commands waiting to be played: ordered by the sender's frame, each frame
    /// kept once however many redundant copies arrive, and nothing older than what was already
    /// taken. Pure policy on a list, so every edge of it is an EditMode test.
    /// </summary>
    public sealed class InputBuffer
    {
        /// <summary>A second of commands. A sender that floods past this loses its oldest.</summary>
        public const int MaxQueued = 60;

        private readonly List<WireCommand> _queue = new List<WireCommand>();

        public int Count => _queue.Count;

        public int LastTakenFrame { get; private set; } = -1;

        public bool Add(in WireCommand command)
        {
            if (command.Frame <= LastTakenFrame)
            {
                return false;
            }

            int index = _queue.Count;
            while (index > 0 && _queue[index - 1].Frame >= command.Frame)
            {
                if (_queue[index - 1].Frame == command.Frame)
                {
                    return false;
                }

                index--;
            }

            _queue.Insert(index, command);
            if (_queue.Count > MaxQueued)
            {
                TryTake(out _);
            }

            return true;
        }

        public bool TryTake(out WireCommand command)
        {
            if (_queue.Count == 0)
            {
                command = default;
                return false;
            }

            command = _queue[0];
            _queue.RemoveAt(0);
            LastTakenFrame = command.Frame;
            return true;
        }
    }
}
```

`Assets/_BattleBomb/Core/Net/RemoteCommandStream.cs`:

```csharp
using BattleBomb.Core.Players;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Turns a remote player's arrivals into exactly one <see cref="PlayerCommand"/> per host step
    /// (HANDOFF-M8 paper numbers): waits until <c>target</c> commands are buffered, then plays one
    /// per step; if it runs dry it repeats what was held and never a press; if it backs up past
    /// <c>max</c> it plays two in one step, merging their edges so neither press is lost. Edges are
    /// always re-derived from consecutive held states, so a redundant copy cannot press twice.
    /// </summary>
    public sealed class RemoteCommandStream
    {
        private readonly int _target;
        private readonly int _max;
        private InputBuffer _buffer = new InputBuffer();
        private CommandButtons _held;
        private Vector2 _move;
        private bool _primed;

        public RemoteCommandStream(int target = NetProtocol.InputBufferTarget, int max = NetProtocol.InputBufferMax)
        {
            _target = Mathf.Max(1, target);
            _max = Mathf.Max(_target, max);
        }

        public int Buffered => _buffer.Count;

        /// <summary>The sender's frame of the newest command played — what a snapshot acknowledges.</summary>
        public int LastConsumedFrame => _buffer.LastTakenFrame;

        public int StarvedSteps { get; private set; }

        public int MergedSteps { get; private set; }

        public void Receive(in WireCommand command) => _buffer.Add(command);

        /// <summary>Forgets the sender entirely — a disconnect, or a rejoin whose frames start again at zero.</summary>
        public void Release()
        {
            _buffer = new InputBuffer();
            _held = CommandButtons.None;
            _move = Vector2.zero;
            _primed = false;
        }

        public PlayerCommand Next(int hostFrame)
        {
            if (!_primed)
            {
                if (_buffer.Count < _target)
                {
                    return PlayerCommand.Idle(hostFrame);
                }

                _primed = true;
            }

            if (!_buffer.TryTake(out WireCommand first))
            {
                StarvedSteps++;
                return new PlayerCommand(hostFrame, _move, _held, CommandButtons.None, CommandButtons.None);
            }

            CommandButtons previous = _held;
            CommandButtons held = first.Held;
            Vector2 move = first.Move;
            CommandButtons pressed = held & ~previous;
            CommandButtons released = previous & ~held;

            if (_buffer.Count > _max && _buffer.TryTake(out WireCommand second))
            {
                MergedSteps++;
                pressed |= second.Held & ~held;
                released |= held & ~second.Held;
                held = second.Held;
                move = second.Move;
            }

            _held = held;
            _move = move;
            return new PlayerCommand(hostFrame, move, held, pressed, released);
        }
    }
}
```

- [ ] **Step 4: Run the tests to see them pass**

`recompile`; `run_tests` EditMode `InputBufferTests`, then `RemoteCommandStreamTests`. Expected: all pass.

- [ ] **Step 5: The remote source and the actor's binding**

`Assets/_BattleBomb/Gameplay/Net/RemoteCommandSource.cs`:

```csharp
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Simulation;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// A player on another machine, as the host's simulation hears them: one more
    /// <see cref="IPlayerCommandSource"/>, indistinguishable from a local pad (D10's promise, D58).
    /// Added at runtime by <see cref="NetSeats"/>, and registers only once it is bound — adding a
    /// component to an active object runs <c>OnEnable</c> immediately, before any id is known.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RemoteCommandSource : MonoBehaviour, IPlayerCommandSource
    {
        private IPlayerRegistryHost _host;
        private PlayerId _id;
        private bool _bound;

        public PlayerId PlayerId => _id;

        public RemoteCommandStream Stream { get; } = new RemoteCommandStream();

        public PlayerCommand Sample(int frame) => Stream.Next(frame);

        internal void Bind(PlayerId id)
        {
            Unregister();
            _id = id;
            _bound = true;
            if (isActiveAndEnabled)
            {
                Register();
            }
        }

        private void OnEnable()
        {
            if (_bound)
            {
                Register();
            }
        }

        private void OnDisable() => Unregister();

        private void Register()
        {
            _host = FindAnyObjectByType<SimulationDriver>();
            if (_host == null)
            {
                Debug.LogError($"{name}: no SimulationDriver — the remote player will never be sampled.", this);
                return;
            }

            _host.Players.Register(this);
        }

        private void Unregister()
        {
            _host?.Players.Unregister(_id);
            _host = null;
        }
    }
}
```

In `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs`:

(a) Below `private IPlayerCommandSource _source;` add:

```csharp
        private bool _sourceBound;
```

(b) After the `PlayerId` property add:

```csharp
        /// <summary>
        /// Names the source this character answers to, overriding the <c>GetComponent</c> lookup in
        /// <c>OnEnable</c>. Online, a player object can carry both a device source (disabled) and a
        /// <see cref="Net.RemoteCommandSource"/>, and <c>GetComponent</c> would return whichever came
        /// first; the binder decides instead (HANDOFF-M8 planning decision 18).
        /// </summary>
        internal void BindSource(IPlayerCommandSource source)
        {
            _source = source;
            _sourceBound = source != null;
        }
```

(c) In `OnEnable`, replace `_source = GetComponent<IPlayerCommandSource>();` with:

```csharp
            if (!_sourceBound)
            {
                _source = GetComponent<IPlayerCommandSource>();
            }
```

- [ ] **Step 6: Recompile and run the whole EditMode suite.** Expected: green. Nothing calls `BindSource` yet, so behaviour is unchanged.

- [ ] **Step 7: Commit** — subject `88: the input buffer and the remote source`. Body: one command per host step whatever the network does; presses re-derived so redundancy never double-presses and starvation never invents one; the remote player is just another `IPlayerCommandSource`.

---
### Task 89: The session and the handshake

The connection gets an owner that survives scene loads (`NetSession`, on the `GameSession` object — planning decision 5), a handshake that refuses a mismatched build out loud (planning decision 20), and the two halves that make Stage A work: on the host, the guest's slot takes a `RemoteCommandSource`; on the guest, the driver runs in replica mode — sampling and sending its own player's commands, simulating nothing. A development-only panel hosts and joins over the local socket.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/HandshakeCodec.cs`
- Create: `Assets/_BattleBomb/Gameplay/Net/NetRole.cs`, `NetSession.cs`, `NetSeats.cs`, `NetHost.cs`, `NetGuest.cs`
- Create: `Assets/_BattleBomb/UI/Debug/NetDevOverlay.cs` (UI stays off Platform: the session builds the local transport)
- Modify: `Assets/_BattleBomb/Gameplay/Session/GameSession.cs`, `SessionBinder.cs`, `SaveService.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (replica mode)
- Modify: `Assets/_BattleBomb/Gameplay/World/StageRunner.cs` (the guest does not run a stage)
- Modify: `Assets/_BattleBomb/Gameplay/Players/InputSystemCommandSource.cs` (`UseSeat`)
- Modify: `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs` (one line)
- Test: `Assets/_BattleBomb/Tests/EditMode/Net/HandshakeCodecTests.cs`, `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs`, `Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs`

- [ ] **Step 1: Write the failing handshake tests**

`Assets/_BattleBomb/Tests/EditMode/Net/HandshakeCodecTests.cs`:

```csharp
using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class HandshakeCodecTests
    {
        [Test]
        public void Hello_welcome_and_refuse_round_trip()
        {
            var writer = new NetWriter();
            HandshakeCodec.WriteHello(writer, new HelloMessage(NetProtocol.Version, "0.8.0"));
            HelloMessage hello = HandshakeCodec.ReadHello(Body(writer, NetMessageKind.Hello));
            Assert.That(hello.Protocol, Is.EqualTo(NetProtocol.Version));
            Assert.That(hello.BuildId, Is.EqualTo("0.8.0"));

            writer.Reset();
            HandshakeCodec.WriteWelcome(writer, new WelcomeMessage(1));
            Assert.That(HandshakeCodec.ReadWelcome(Body(writer, NetMessageKind.Welcome)).GuestPlayerId, Is.EqualTo(1));

            writer.Reset();
            HandshakeCodec.WriteRefuse(writer, "Different versions of the game.");
            Assert.That(HandshakeCodec.ReadRefuse(Body(writer, NetMessageKind.Refuse)), Is.EqualTo("Different versions of the game."));
        }

        [Test]
        public void A_launch_carries_both_seats_and_the_run()
        {
            var writer = new NetWriter();
            HandshakeCodec.WriteLaunch(writer, new LaunchMessage("fixture", 1, 2, 0, new[] { 0, 3 }, 1));
            LaunchMessage launch = HandshakeCodec.ReadLaunch(Body(writer, NetMessageKind.Launch));

            Assert.That(launch.ChapterId, Is.EqualTo("fixture"));
            Assert.That(launch.StageIndex, Is.EqualTo(1));
            Assert.That(launch.TierIndex, Is.EqualTo(2));
            Assert.That(launch.ResumeCheckpointArena, Is.EqualTo(0));
            Assert.That(launch.RosterPicks, Is.EqualTo(new[] { 0, 3 }));
            Assert.That(launch.GuestPlayerId, Is.EqualTo(1));
        }

        [Test]
        public void A_launch_with_more_seats_than_the_game_has_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteString("fixture");
            writer.WriteInt(0);
            writer.WriteInt(0);
            writer.WriteInt(-1);
            writer.WriteUShort(3);
            Assert.Throws<NetFormatException>(() => HandshakeCodec.ReadLaunch(new NetReader(writer.ToArray())));
        }

        [Test]
        public void A_hello_is_checked_for_protocol_and_build()
        {
            Assert.That(HandshakeCodec.CheckHello(new HelloMessage(NetProtocol.Version, "a"), "a"), Is.Null);
            StringAssert.Contains("protocol", HandshakeCodec.CheckHello(new HelloMessage(NetProtocol.Version + 1, "a"), "a"));
            StringAssert.Contains("version", HandshakeCodec.CheckHello(new HelloMessage(NetProtocol.Version, "b"), "a"));
        }

        private static NetReader Body(NetWriter writer, NetMessageKind expected)
        {
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(expected));
            return reader;
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail** — `recompile`. Expected: compile errors.

- [ ] **Step 3: Write the handshake codec**

`Assets/_BattleBomb/Core/Net/HandshakeCodec.cs`:

```csharp
using BattleBomb.Core.Chapters;

namespace BattleBomb.Core.Net
{
    public readonly struct HelloMessage
    {
        public readonly int Protocol;
        public readonly string BuildId;

        public HelloMessage(int protocol, string buildId)
        {
            Protocol = protocol;
            BuildId = buildId ?? string.Empty;
        }
    }

    public readonly struct WelcomeMessage
    {
        public readonly int GuestPlayerId;

        public WelcomeMessage(int guestPlayerId)
        {
            GuestPlayerId = guestPlayerId;
        }
    }

    /// <summary>
    /// The run the host just launched, for the guest to load the same one: the chapter by its
    /// authored id, the stage, tier and resume point, each seat's roster index (-1 for nobody),
    /// and which seat is the guest's.
    /// </summary>
    public readonly struct LaunchMessage
    {
        public readonly string ChapterId;
        public readonly int StageIndex;
        public readonly int TierIndex;
        public readonly int ResumeCheckpointArena;
        public readonly int[] RosterPicks;
        public readonly int GuestPlayerId;

        public LaunchMessage(
            string chapterId, int stageIndex, int tierIndex, int resumeCheckpointArena,
            int[] rosterPicks, int guestPlayerId)
        {
            ChapterId = chapterId ?? string.Empty;
            StageIndex = stageIndex;
            TierIndex = tierIndex;
            ResumeCheckpointArena = resumeCheckpointArena;
            RosterPicks = rosterPicks ?? new int[0];
            GuestPlayerId = guestPlayerId;
        }
    }

    /// <summary>The messages that open a session and start a run. Readers take a reader positioned
    /// after the kind byte.</summary>
    public static class HandshakeCodec
    {
        public static void WriteHello(NetWriter writer, in HelloMessage hello)
        {
            writer.WriteByte((byte)NetMessageKind.Hello);
            writer.WriteInt(hello.Protocol);
            writer.WriteString(hello.BuildId);
        }

        public static HelloMessage ReadHello(NetReader reader) =>
            new HelloMessage(reader.ReadInt(), reader.ReadString(128));

        public static void WriteWelcome(NetWriter writer, in WelcomeMessage welcome)
        {
            writer.WriteByte((byte)NetMessageKind.Welcome);
            writer.WriteInt(welcome.GuestPlayerId);
        }

        public static WelcomeMessage ReadWelcome(NetReader reader) => new WelcomeMessage(reader.ReadInt());

        public static void WriteRefuse(NetWriter writer, string reason)
        {
            writer.WriteByte((byte)NetMessageKind.Refuse);
            writer.WriteString(reason);
        }

        public static string ReadRefuse(NetReader reader) => reader.ReadString(512);

        public static void WriteLaunch(NetWriter writer, in LaunchMessage launch)
        {
            writer.WriteByte((byte)NetMessageKind.Launch);
            writer.WriteString(launch.ChapterId);
            writer.WriteInt(launch.StageIndex);
            writer.WriteInt(launch.TierIndex);
            writer.WriteInt(launch.ResumeCheckpointArena);
            writer.WriteCount(launch.RosterPicks.Length, FrontendState.Slots);
            for (int i = 0; i < launch.RosterPicks.Length; i++)
            {
                writer.WriteInt(launch.RosterPicks[i]);
            }

            writer.WriteInt(launch.GuestPlayerId);
        }

        public static LaunchMessage ReadLaunch(NetReader reader)
        {
            string chapter = reader.ReadString(256);
            int stage = reader.ReadInt();
            int tier = reader.ReadInt();
            int resume = reader.ReadInt();
            var picks = new int[reader.ReadCount(FrontendState.Slots)];
            for (int i = 0; i < picks.Length; i++)
            {
                picks[i] = reader.ReadInt();
            }

            return new LaunchMessage(chapter, stage, tier, resume, picks, reader.ReadInt());
        }

        /// <summary>A bare message: its kind is its whole content.</summary>
        public static void WriteBare(NetWriter writer, NetMessageKind kind) => writer.WriteByte((byte)kind);

        /// <summary>Null when the guest may join; otherwise the reason both screens show.</summary>
        public static string CheckHello(in HelloMessage hello, string localBuildId)
        {
            if (hello.Protocol != NetProtocol.Version)
            {
                return $"Different network protocol (host {NetProtocol.Version}, guest {hello.Protocol}). Update both games.";
            }

            if (hello.BuildId != (localBuildId ?? string.Empty))
            {
                return $"Different versions of the game (host {localBuildId}, guest {hello.BuildId}).";
            }

            return null;
        }
    }
}
```

- [ ] **Step 4: Run it** — `recompile`; `run_tests` EditMode `HandshakeCodecTests`. Expected: pass.

- [ ] **Step 5: The session**

`Assets/_BattleBomb/Gameplay/Net/NetRole.cs`:

```csharp
namespace BattleBomb.Gameplay.Net
{
    public enum NetRole
    {
        Offline = 0,
        Host = 1,
        Guest = 2,
    }
}
```

`Assets/_BattleBomb/Gameplay/Net/NetSession.cs`:

```csharp
using System;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Session;
using BattleBomb.Platform.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The connection, owned by the object that survives every scene (HANDOFF-M8 planning decision
    /// 5): the role, the transport, the peer, the handshake, keep-alives and the drop timer. Runs
    /// ahead of the driver each frame so a guest's commands are buffered before the host samples
    /// them. It moves bytes and scenes; it decides nothing about the game — that is
    /// <see cref="NetHost"/> and <see cref="NetGuest"/>, which live in the Gameplay scene.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class NetSession : MonoBehaviour
    {
        public const string GameplayScene = "Gameplay";
        public const string FrontendScene = "Frontend";

        private readonly NetWriter _writer = new NetWriter(512);
        private INetTransport _transport;
        private double _lastReceived;
        private double _lastSent;
        private bool _welcomed;

        public NetRole Role { get; private set; }

        public NetPeer Peer { get; private set; }

        /// <summary>The handshake is done: the host has welcomed a guest, or the guest was welcomed.</summary>
        public bool IsConnected => Role != NetRole.Offline && _welcomed && !Peer.IsNone;

        /// <summary>The guest's seat. Fixed while D11 caps the game at two: the host is Player 1.</summary>
        public PlayerId GuestPlayerId => PlayerId.Two;

        /// <summary>One line for the development panel.</summary>
        public string Status { get; private set; } = "Offline";

        public string LastRefusal { get; private set; }

        /// <summary>Nothing heard for a second — the "connection problem" banner's condition.</summary>
        public bool HasProblem => IsConnected && Now - _lastReceived > NetProtocol.ProblemAfterSeconds;

        /// <summary>Every message past the handshake, still positioned after its kind byte.</summary>
        public event Action<NetMessageKind, NetReader> MessageReceived;

        public event Action PeerJoined;

        public event Action PeerLeft;

        private static double Now => Time.unscaledTimeAsDouble;

        public static NetRole RoleOf(GameSession session)
        {
            NetSession net = session != null ? session.Net : null;
            return net != null ? net.Role : NetRole.Offline;
        }

        public static NetSession FindOrCreate()
        {
            GameSession session = GameSession.FindOrCreate();
            NetSession net = session.Net;
            return net != null ? net : session.gameObject.AddComponent<NetSession>();
        }

        public void Host(INetTransport transport)
        {
            Close();
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Role = NetRole.Host;
            _transport.Listen();
            Status = "Hosting — waiting for a guest";
        }

        public void Join(INetTransport transport, string address)
        {
            Close();
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Role = NetRole.Guest;
            Status = $"Joining {address}";
            _transport.Connect(address);
        }

        /// <summary>A clean leave: the other side hears it at once rather than waiting out the drop timer.</summary>
        public void Leave()
        {
            if (!Peer.IsNone)
            {
                _writer.Reset();
                HandshakeCodec.WriteBare(_writer, NetMessageKind.Bye);
                Send(NetChannel.Reliable, _writer);
            }

            Close();
        }

        public void Send(NetChannel channel, NetWriter writer)
        {
            if (_transport == null || Peer.IsNone)
            {
                return;
            }

            _transport.Send(Peer, channel, writer.Buffer, writer.Length);
            _lastSent = Now;
        }

        private void Update()
        {
            if (_transport == null)
            {
                return;
            }

            _transport.Update(Now);
            while (_transport != null && _transport.TryReceive(out NetEvent netEvent))
            {
                Handle(netEvent);
            }

            if (_transport == null || Peer.IsNone)
            {
                return;
            }

            if (Now - _lastReceived > NetProtocol.DropAfterSeconds)
            {
                Status = "The connection went silent.";
                _transport.Disconnect(Peer);
                return;
            }

            if (_welcomed && Now - _lastSent > NetProtocol.KeepAliveSeconds)
            {
                _writer.Reset();
                HandshakeCodec.WriteBare(_writer, NetMessageKind.KeepAlive);
                Send(NetChannel.Unreliable, _writer);
            }
        }

        private void Handle(in NetEvent netEvent)
        {
            switch (netEvent.Kind)
            {
                case NetEventKind.Connected:
                    if (!Peer.IsNone)
                    {
                        // D11: one guest. A second connection is turned away at the door.
                        _transport.Disconnect(netEvent.Peer);
                        return;
                    }

                    Peer = netEvent.Peer;
                    _lastReceived = Now;
                    if (Role == NetRole.Guest)
                    {
                        _writer.Reset();
                        HandshakeCodec.WriteHello(_writer, new HelloMessage(NetProtocol.Version, Application.version));
                        Send(NetChannel.Reliable, _writer);
                        Status = "Saying hello";
                    }

                    break;

                case NetEventKind.Disconnected:
                    if (!Peer.IsNone && netEvent.Peer != Peer)
                    {
                        return;
                    }

                    Lost("left");
                    break;

                case NetEventKind.Data:
                    if (netEvent.Peer != Peer)
                    {
                        return;
                    }

                    _lastReceived = Now;
                    Dispatch(netEvent.Payload);
                    break;
            }
        }

        private void Dispatch(byte[] payload)
        {
            var reader = new NetReader(payload);
            try
            {
                var kind = (NetMessageKind)reader.ReadByte();
                switch (kind)
                {
                    case NetMessageKind.Hello when Role == NetRole.Host && !_welcomed:
                        Greet(HandshakeCodec.ReadHello(reader));
                        return;

                    case NetMessageKind.Welcome when Role == NetRole.Guest && !_welcomed:
                        HandshakeCodec.ReadWelcome(reader);
                        _welcomed = true;
                        Status = "Joined — waiting for the host to launch";
                        PeerJoined?.Invoke();
                        return;

                    case NetMessageKind.Refuse when Role == NetRole.Guest:
                        LastRefusal = HandshakeCodec.ReadRefuse(reader);
                        Close();
                        Status = $"Refused: {LastRefusal}";
                        return;

                    case NetMessageKind.KeepAlive:
                        return;

                    case NetMessageKind.Bye:
                        Lost("left");
                        return;

                    case NetMessageKind.Launch when Role == NetRole.Guest && _welcomed:
                        FollowLaunch(HandshakeCodec.ReadLaunch(reader));
                        return;

                    case NetMessageKind.SessionEnd when Role == NetRole.Guest && _welcomed:
                        ReturnToFrontend();
                        return;
                }

                if (_welcomed)
                {
                    MessageReceived?.Invoke(kind, reader);
                }
            }
            catch (NetFormatException e)
            {
                Debug.LogWarning($"{name}: a malformed message from {Peer} — {e.Message}. Dropping the connection.", this);
                _transport?.Disconnect(Peer);
            }
        }

        private void Greet(in HelloMessage hello)
        {
            string refusal = HandshakeCodec.CheckHello(hello, Application.version);
            if (refusal != null)
            {
                _writer.Reset();
                HandshakeCodec.WriteRefuse(_writer, refusal);
                Send(NetChannel.Reliable, _writer);
                Status = $"Refused a guest: {refusal}";
                _transport.Disconnect(Peer);
                return;
            }

            _welcomed = true;
            _writer.Reset();
            HandshakeCodec.WriteWelcome(_writer, new WelcomeMessage(GuestPlayerId.Value));
            Send(NetChannel.Reliable, _writer);
            Status = "A guest joined";
            PeerJoined?.Invoke();
        }

        /// <summary>The guest loads the run the host just launched. The chapter and heroes are found by
        /// id and roster index in what this machine's front door put on the session.</summary>
        private void FollowLaunch(in LaunchMessage launch)
        {
            GameSession session = GetComponent<GameSession>();
            ChapterDefinition chapter = null;
            for (int i = 0; i < session.Chapters.Length; i++)
            {
                if (session.Chapters[i] != null && session.Chapters[i].Id == launch.ChapterId)
                {
                    chapter = session.Chapters[i];
                }
            }

            if (chapter == null)
            {
                Debug.LogError($"{name}: the host launched chapter '{launch.ChapterId}', which this build does not have.", this);
                Leave();
                return;
            }

            session.Chapter = chapter;
            session.StageIndex = launch.StageIndex;
            session.TierIndex = launch.TierIndex;
            session.ResumeCheckpointArena = launch.ResumeCheckpointArena;
            for (int i = 0; i < session.Characters.Length; i++)
            {
                int pick = i < launch.RosterPicks.Length ? launch.RosterPicks[i] : -1;
                session.Characters[i] = pick >= 0 && pick < session.Roster.Length ? session.Roster[pick] : null;
            }

            Status = "Playing as the guest";
            SceneManager.LoadScene(GameplayScene, LoadSceneMode.Single);
        }

        private void ReturnToFrontend()
        {
            if (SceneManager.GetActiveScene().name != FrontendScene)
            {
                SceneManager.LoadScene(FrontendScene, LoadSceneMode.Single);
            }
        }

        private void Lost(string why)
        {
            bool wasJoined = _welcomed;
            Peer = NetPeer.None;
            _welcomed = false;

            if (Role == NetRole.Guest)
            {
                Close();
                Status = $"The host {why}.";
                if (wasJoined)
                {
                    PeerLeft?.Invoke();
                    ReturnToFrontend();
                }

                return;
            }

            Status = $"Hosting — the guest {why}; waiting for another";
            if (wasJoined)
            {
                PeerLeft?.Invoke();
            }
        }

        private void Close()
        {
            _transport?.Dispose();
            _transport = null;
            Role = NetRole.Offline;
            Peer = NetPeer.None;
            _welcomed = false;
            Status = "Offline";
        }

        private void OnDestroy() => Leave();
    }
}
```

- [ ] **Step 6: The session on `GameSession`, and the roster**

In `Assets/_BattleBomb/Gameplay/Session/GameSession.cs`, add `using BattleBomb.Gameplay.Net;` and, after the `Tiers` property:

```csharp
        /// <summary>The front door's roster, so a hero can cross the wire as an index (M8). Set by
        /// <c>FrontendFlow</c> alongside <see cref="Chapters"/> and <see cref="Tiers"/>.</summary>
        public CharacterDefinition[] Roster { get; set; } = new CharacterDefinition[0];

        /// <summary>This machine's connection, when there is one. It lives on this object because this
        /// is the object that survives every scene (HANDOFF-M8 planning decision 5).</summary>
        public NetSession Net => GetComponent<NetSession>();
```

In `Assets/_BattleBomb/UI/Frontend/FrontendFlow.cs`, directly after `_session.Tiers = _tiers;` add:

```csharp
            _session.Roster = _roster;
```

- [ ] **Step 7: Seats online, and the guest's device speaking as Player 2**

In `Assets/_BattleBomb/Gameplay/Players/InputSystemCommandSource.cs` (Groundwork's version, Task 5 of its plan): add a field below `private int _seat;`'s declaration block:

```csharp
        private int _speakAs = -1;
```

replace the `PlayerId` property with:

```csharp
        public PlayerId PlayerId => new PlayerId(_speakAs >= 0 ? _speakAs : _seat);
```

and add, after `Sample`:

```csharp
        /// <summary>
        /// Online (HANDOFF-M8 planning decision 18): the device rules of one seat, speaking as
        /// another player. The guest's machine gives its one local player seat 0's rule — every
        /// device is theirs — while they are Player 2 in the host's game. Called before
        /// <c>OnEnable</c> (the binder runs first), because the seat's controls are built there.
        /// </summary>
        internal void UseSeat(int seat, int speakAs)
        {
            _seat = seat;
            _speakAs = speakAs;
        }
```

`Assets/_BattleBomb/Gameplay/Net/NetSeats.cs`:

```csharp
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Players;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// Who answers to what, online (HANDOFF-M8 planning decision 18). Called by the binder in
    /// <c>Awake</c>, before any player object's sources have enabled, so a disabled device never
    /// registers and a re-seated one builds its controls with the right seat.
    /// </summary>
    internal static class NetSeats
    {
        /// <summary>Host: the guest's body takes orders from the wire and from no local device.</summary>
        internal static RemoteCommandSource MakeRemote(CharacterActor actor, PlayerId id)
        {
            DisableDevices(actor);
            RemoteCommandSource remote = actor.GetComponent<RemoteCommandSource>();
            if (remote == null)
            {
                remote = actor.gameObject.AddComponent<RemoteCommandSource>();
            }

            remote.Bind(id);
            actor.BindSource(remote);
            return remote;
        }

        /// <summary>Guest: the host's body is drawn from snapshots and takes orders from nobody here.
        /// Its disabled device still answers the actor's <c>PlayerId</c> with its authored seat.</summary>
        internal static void MakeReplica(CharacterActor actor) => DisableDevices(actor);

        /// <summary>Guest: this machine's one player owns every local device and speaks as the guest.</summary>
        internal static void MakeLocalGuest(CharacterActor actor, PlayerId speakAs)
        {
            InputSystemCommandSource device = actor.GetComponent<InputSystemCommandSource>();
            if (device == null)
            {
                Debug.LogError($"{actor.name}: no InputSystemCommandSource — the guest cannot play.", actor);
                return;
            }

            device.UseSeat(0, speakAs.Value);
        }

        private static void DisableDevices(CharacterActor actor)
        {
            foreach (InputSystemCommandSource device in actor.GetComponents<InputSystemCommandSource>())
            {
                device.enabled = false;
            }
        }
    }
}
```

- [ ] **Step 8: The driver's replica mode**

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs`:

(a) With the other private fields, add:

```csharp
        /// <summary>The guest's driver (D58): it samples and sends its own player's commands and
        /// draws what the host sends, and never runs <see cref="RunStep"/>.</summary>
        private bool _replica;
```

(b) Near the other events, add:

```csharp
        /// <summary>Raised on a replica driver once per local step, after this machine's commands are
        /// sampled and before <see cref="Stepped"/> — where the guest sends its command and applies
        /// what the host sent.</summary>
        internal event Action<int> ReplicaStepping;

        internal bool IsReplica => _replica;

        /// <summary>Set by the binder in <c>Awake</c>, before the first frame.</summary>
        internal void EnterReplicaMode() => _replica = true;
```

(c) At the very top of `Update()`, before the `if (PausedForScreen)` block, add:

```csharp
            if (_replica)
            {
                // The guest never pauses its own clock: the world it draws is the host's, and the
                // host decides when that stops (D60). Its steps exist to sample and send its own
                // commands at the simulation's rate, and to pace the picture.
                _clock.Accumulate(Time.deltaTime);
                while (_clock.TryConsumeStep(out int replicaFrame))
                {
                    SampleCommands(replicaFrame);
                    ReplicaStepping?.Invoke(replicaFrame);
                    Stepped?.Invoke(replicaFrame);
                }

                return;
            }
```

- [ ] **Step 9: The host's and the guest's halves**

`Assets/_BattleBomb/Gameplay/Net/NetHost.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The host's half of a match, in the Gameplay scene: tells the guest which run to load, and
    /// feeds the guest's commands into their <see cref="RemoteCommandSource"/>. Added by the binder
    /// when the machine boots as a host with a guest connected.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetHost : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(4096);
        private readonly List<WireCommand> _commands = new List<WireCommand>(NetProtocol.CommandRedundancy);
        private NetSession _net;
        private GameSession _session;
        private SimulationDriver _driver;
        private RemoteCommandSource _remote;

        internal void Begin(NetSession net, GameSession session, SimulationDriver driver, RemoteCommandSource remote)
        {
            _net = net;
            _session = session;
            _driver = driver;
            _remote = remote;
            _net.MessageReceived += OnMessage;
            _net.PeerLeft += OnPeerLeft;
            SendLaunch();
        }

        private void SendLaunch()
        {
            var picks = new int[_session.Characters.Length];
            for (int i = 0; i < picks.Length; i++)
            {
                picks[i] = IndexInRoster(_session.Characters[i]);
            }

            _writer.Reset();
            HandshakeCodec.WriteLaunch(_writer, new LaunchMessage(
                _session.Chapter != null ? _session.Chapter.Id : string.Empty,
                _session.StageIndex, _session.TierIndex, _session.ResumeCheckpointArena,
                picks, _net.GuestPlayerId.Value));
            _net.Send(NetChannel.Reliable, _writer);
        }

        private int IndexInRoster(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return -1;
            }

            for (int i = 0; i < _session.Roster.Length; i++)
            {
                if (_session.Roster[i] == definition)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnMessage(NetMessageKind kind, NetReader reader)
        {
            if (kind != NetMessageKind.Commands || _remote == null)
            {
                return;
            }

            CommandCodec.Read(reader, _commands);
            for (int i = 0; i < _commands.Count; i++)
            {
                _remote.Stream.Receive(_commands[i]);
            }
        }

        /// <summary>The guest is gone: their body stops taking orders rather than running on with the
        /// last stick it heard. What happens to it next is Plan 2's (D61 — the host carries on solo).</summary>
        private void OnPeerLeft()
        {
            if (_remote != null)
            {
                _remote.Stream.Release();
            }
        }

        private void OnDestroy()
        {
            if (_net == null)
            {
                return;
            }

            _net.MessageReceived -= OnMessage;
            _net.PeerLeft -= OnPeerLeft;

            // The machine is being torn down — results, or return to chapter select. The guest's copy
            // goes back to the front door with it and stays connected for the next launch.
            if (_net.IsConnected)
            {
                _writer.Reset();
                HandshakeCodec.WriteBare(_writer, NetMessageKind.SessionEnd);
                _net.Send(NetChannel.Reliable, _writer);
            }
        }
    }
}
```

`Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The guest's half of a match, in the Gameplay scene: every local step it sends this machine's
    /// newest commands to the host (with redundancy, D58). Stage B teaches it to draw the host's
    /// world as well.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetGuest : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(256);
        private readonly List<WireCommand> _recent = new List<WireCommand>(NetProtocol.CommandRedundancy + 1);
        private NetSession _net;
        private SimulationDriver _driver;
        private PlayerId _local;
        private int _latestHostFrame;

        internal void Begin(NetSession net, SimulationDriver driver, PlayerId local)
        {
            _net = net;
            _driver = driver;
            _local = local;
            _driver.ReplicaStepping += OnLocalStep;
            _net.MessageReceived += OnMessage;
        }

        private void OnLocalStep(int frame)
        {
            PlayerCommand command = CommandCodec.Quantized(_driver.CommandFor(_local.Value));
            _recent.Add(WireCommand.From(command));
            if (_recent.Count > NetProtocol.CommandRedundancy)
            {
                _recent.RemoveAt(0);
            }

            _writer.Reset();
            CommandCodec.Write(_writer, _latestHostFrame, _recent);
            _net.Send(NetChannel.Unreliable, _writer);
        }

        private void OnMessage(NetMessageKind kind, NetReader reader)
        {
        }

        private void OnDestroy()
        {
            if (_driver != null)
            {
                _driver.ReplicaStepping -= OnLocalStep;
            }

            if (_net != null)
            {
                _net.MessageReceived -= OnMessage;
            }
        }
    }
}
```

- [ ] **Step 10: The binder brings them up**

In `Assets/_BattleBomb/Gameplay/Session/SessionBinder.cs`:

(a) Add `using BattleBomb.Gameplay.Net;`.

(b) In `Awake`, directly after the `_players.Length == 0` guard, add:

```csharp
            NetSession net = _session.Net;
            bool hosting = net != null && net.Role == NetRole.Host && net.IsConnected;
            bool guesting = net != null && net.Role == NetRole.Guest && net.IsConnected;

            // Plan 1's stand-in until the lobby (HANDOFF-M8 Task 102): a connected guest takes the
            // second seat as the host's own hero — over a local Player 2, because D59 lets a couch pair
            // and an online guest never both be here.
            if (hosting && _session.Characters.Length > 1)
            {
                _session.Characters[net.GuestPlayerId.Value] = _session.Characters[0];
            }
```

(c) At the end of `Awake`, after the slot loop, add:

```csharp
            if (_driver == null)
            {
                _driver = FindAnyObjectByType<SimulationDriver>();
            }

            if (hosting)
            {
                BindHost(net);
            }
            else if (guesting)
            {
                BindGuest(net);
            }
```

(d) In `Start`, directly after its first guard, add:

```csharp
            // The guest's gear is the host's to hold during a match (D61); nothing of this machine's
            // save is restored into a world it only draws.
            if (NetSession.RoleOf(_session) == NetRole.Guest)
            {
                return;
            }
```

(e) Add the two methods:

```csharp
        private void BindHost(NetSession net)
        {
            int slot = net.GuestPlayerId.Value;
            if (slot >= _players.Length || _players[slot] == null)
            {
                Debug.LogError($"{name}: a guest is connected but there is no player object for seat {slot}.", this);
                return;
            }

            RemoteCommandSource remote = NetSeats.MakeRemote(_players[slot], net.GuestPlayerId);
            gameObject.AddComponent<NetHost>().Begin(net, _session, _driver, remote);
        }

        private void BindGuest(NetSession net)
        {
            for (int i = 0; i < _players.Length; i++)
            {
                if (_players[i] == null || !_players[i].gameObject.activeSelf)
                {
                    continue;
                }

                if (i == net.GuestPlayerId.Value)
                {
                    NetSeats.MakeLocalGuest(_players[i], net.GuestPlayerId);
                }
                else
                {
                    NetSeats.MakeReplica(_players[i]);
                }
            }

            _driver.EnterReplicaMode();
            gameObject.AddComponent<NetGuest>().Begin(net, _driver, net.GuestPlayerId);
        }
```

- [ ] **Step 11: The guest runs no stage and writes no save**

In `Assets/_BattleBomb/Gameplay/World/StageRunner.cs`:

(a) Add `using BattleBomb.Gameplay.Net;`, and a field with the others:

```csharp
        /// <summary>The guest's runner (D58): it follows the host's stages and decides nothing.
        /// Task 94 teaches it to follow; until then it simply does not run.</summary>
        private bool _replica;
```

(b) In `OnEnable`, directly after the three `_driver.… +=` subscriptions, add:

```csharp
            _replica = NetSession.RoleOf(Session.GameSession.Find()) == NetRole.Guest;
            if (_replica)
            {
                return;
            }
```

(c) At the top of `OnStepped`, add:

```csharp
            if (_replica)
            {
                return;
            }
```

In `Assets/_BattleBomb/Gameplay/Session/SaveService.cs`:

(a) Add `using BattleBomb.Gameplay.Net;`.

(b) At the top of `SaveNow`, add:

```csharp
            // The guest writes its own save only when the host says a D52 moment happened (Plan 2,
            // D61). Its own copy of the session is a picture of the host's run, and saving a picture
            // over a real file is how a guest loses their gear.
            if (NetSession.RoleOf(_session) == NetRole.Guest)
            {
                return;
            }
```

- [ ] **Step 12: The development panel**

UI does not reference Platform (ARCHITECTURE §1: UI → Core, Gameplay), so the panel asks the session to build the local transport. In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`, add after `Join`:

```csharp
        /// <summary>The lag profiles the development panel cycles through, by index.</summary>
        public static readonly string[] LocalLagNames = { "None", "Normal", "Bad" };

        /// <summary>Development: host on this machine's local socket (two editors, HANDOFF-M8 Task 90).</summary>
        public void HostLocal(int lagIndex) =>
            Host(WrapLocal(new LocalSocketTransport(NetProtocol.DevPort), lagIndex));

        /// <summary>Development: join a host on this machine's local socket.</summary>
        public void JoinLocal(int lagIndex) =>
            Join(WrapLocal(new LocalSocketTransport(0), lagIndex), $"127.0.0.1:{NetProtocol.DevPort}");

        private static INetTransport WrapLocal(INetTransport transport, int lagIndex)
        {
            LagProfile profile = lagIndex == 1 ? LagProfile.Normal : lagIndex == 2 ? LagProfile.Bad : LagProfile.None;
            return profile.IsNone ? transport : new LagSimulator(transport, profile, 1u);
        }
```

`Assets/_BattleBomb/UI/Debug/NetDevOverlay.cs`:

```csharp
#if DEVELOPMENT_BUILD || UNITY_EDITOR
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Session;
using UnityEngine;

namespace BattleBomb.UI.Debug
{
    /// <summary>
    /// Development only: host or join over the local socket, with a made-up connection (HANDOFF-M8
    /// Task 90). Two editors under Multiplayer Play Mode: one clicks Host local at the title, the
    /// other Join local. Not a menu — the real front door is Plan 2's lobby and Plan 3's Steam.
    /// Installs itself, so no scene carries it and a release build has none.
    /// </summary>
    public sealed class NetDevOverlay : MonoBehaviour
    {
        private int _lag;
        private bool _folded;
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("Net Dev Overlay");
            DontDestroyOnLoad(go);
            go.AddComponent<NetDevOverlay>();
        }

        private void OnGUI()
        {
            int fontSize = Mathf.Max(12, Screen.height / 60);
            if (_style == null || _style.fontSize != fontSize)
            {
                _style = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
            }

            float width = fontSize * 22f;
            float row = fontSize + 10f;
            var area = new Rect(Screen.width - width - 8f, Screen.height - row * 5f - 8f, width, row * 5f);
            GUILayout.BeginArea(area, GUI.skin.box);

            GameSession session = GameSession.Find();
            NetSession net = session != null ? session.Net : null;
            NetRole role = net != null ? net.Role : NetRole.Offline;

            if (GUILayout.Button(_folded ? "Net ▲" : "Net ▼", _style))
            {
                _folded = !_folded;
            }

            if (!_folded)
            {
                GUILayout.Label(net != null ? net.Status : "Offline", _style);
                if (role == NetRole.Offline)
                {
                    if (GUILayout.Button($"Lag: {NetSession.LocalLagNames[_lag]}", _style))
                    {
                        _lag = (_lag + 1) % NetSession.LocalLagNames.Length;
                    }

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Host local", _style))
                    {
                        NetSession.FindOrCreate().HostLocal(_lag);
                    }

                    if (GUILayout.Button("Join local", _style))
                    {
                        NetSession.FindOrCreate().JoinLocal(_lag);
                    }

                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (net.HasProblem)
                    {
                        GUILayout.Label("Connection problem…", _style);
                    }

                    if (GUILayout.Button("Leave", _style))
                    {
                        net.Leave();
                    }
                }
            }

            GUILayout.EndArea();
        }
    }
}
#endif
```

- [ ] **Step 13: Recompile and run EditMode.** `recompile`, `console` `level: error` — clean; full EditMode green. Nothing is online by default, so no existing test may change.

- [ ] **Step 14: The headless guest (test helper)**

`Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Core.Simulation;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A guest with no scene: it speaks the protocol over a transport, sends the stick and buttons a
    /// test sets at the simulation's rate, and keeps every message the host sends. Two Gameplay scenes
    /// cannot share one process — their wiring finds "the" driver — so the automated guest has none
    /// (HANDOFF-M8, Testing).
    /// </summary>
    internal sealed class HeadlessGuest : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(256);
        private readonly List<WireCommand> _recent = new List<WireCommand>();
        private readonly SimulationClock _clock = new SimulationClock();
        private INetTransport _transport;
        private NetPeer _host;
        private CommandButtons _previous;
        private int _frame;

        internal bool IsWelcomed { get; private set; }

        internal LaunchMessage? Launch { get; private set; }

        internal bool SessionEnded { get; private set; }

        /// <summary>Every message the host sent after the handshake, in arrival order, with its kind byte.</summary>
        internal List<byte[]> Received { get; } = new List<byte[]>();

        internal int SentCommands { get; private set; }

        /// <summary>The newest host frame this guest has seen a snapshot for — what it acknowledges.</summary>
        internal int LatestHostFrame { get; set; }

        internal Vector2 Move { get; set; }

        internal CommandButtons Held { get; set; }

        /// <summary>False stops the command stream, as a frozen or hitching guest would.</summary>
        internal bool Sending { get; set; } = true;

        internal static HeadlessGuest Join(INetTransport transport)
        {
            var go = new GameObject("Headless Guest");
            DontDestroyOnLoad(go);
            var guest = go.AddComponent<HeadlessGuest>();
            guest._transport = transport;
            transport.Connect("loopback");
            return guest;
        }

        internal void Leave()
        {
            _writer.Reset();
            HandshakeCodec.WriteBare(_writer, NetMessageKind.Bye);
            _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
            _transport.Disconnect(_host);
        }

        internal void SendRaw(NetChannel channel, NetWriter writer) =>
            _transport.Send(_host, channel, writer.Buffer, writer.Length);

        private void Update()
        {
            _transport.Update(Time.unscaledTimeAsDouble);
            while (_transport.TryReceive(out NetEvent netEvent))
            {
                Handle(netEvent);
            }

            if (!IsWelcomed || !Launch.HasValue)
            {
                return;
            }

            _clock.Accumulate(Time.deltaTime);
            while (_clock.TryConsumeStep(out _))
            {
                if (!Sending)
                {
                    continue;
                }

                var command = PlayerCommand.FromState(_frame++, Move, Held, _previous);
                _previous = Held;
                _recent.Add(WireCommand.From(command));
                if (_recent.Count > NetProtocol.CommandRedundancy)
                {
                    _recent.RemoveAt(0);
                }

                _writer.Reset();
                CommandCodec.Write(_writer, LatestHostFrame, _recent);
                _transport.Send(_host, NetChannel.Unreliable, _writer.Buffer, _writer.Length);
                SentCommands++;
            }
        }

        private void Handle(in NetEvent netEvent)
        {
            switch (netEvent.Kind)
            {
                case NetEventKind.Connected:
                    _host = netEvent.Peer;
                    _writer.Reset();
                    HandshakeCodec.WriteHello(_writer, new HelloMessage(NetProtocol.Version, Application.version));
                    _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
                    break;

                case NetEventKind.Disconnected:
                    _host = NetPeer.None;
                    IsWelcomed = false;
                    break;

                case NetEventKind.Data:
                    var reader = new NetReader(netEvent.Payload);
                    var kind = (NetMessageKind)reader.ReadByte();
                    if (kind == NetMessageKind.Welcome)
                    {
                        IsWelcomed = true;
                    }
                    else if (kind == NetMessageKind.Launch)
                    {
                        Launch = HandshakeCodec.ReadLaunch(reader);
                        Received.Add(netEvent.Payload);
                    }
                    else if (kind == NetMessageKind.SessionEnd)
                    {
                        SessionEnded = true;
                        Received.Add(netEvent.Payload);
                    }
                    else if (kind != NetMessageKind.KeepAlive)
                    {
                        Received.Add(netEvent.Payload);
                    }

                    break;
            }
        }

        private void OnDestroy() => _transport?.Dispose();
    }
}
```

- [ ] **Step 15: Write the failing hosted smoke test**

`Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D45's tripwire, online (HANDOFF-M8): the real Gameplay scene hosted, with Player 2 played by a
    /// headless guest over the loopback transport. It asks whether anything is completely broken —
    /// never how it feels; that is Michael's, with two editors (Task 96). Paced by
    /// <see cref="SimulationDriver.Frame"/>, as its siblings are, and with a partner in it always.
    /// </summary>
    public sealed class OnlineHostSmokeTests
    {
        private const int PatienceSteps = 1200;
        private const int FrameCeiling = 30000;
        private const int LoadFrameCeiling = 1500;
        private const string MachineScene = "Gameplay";

        private SimulationDriver _driver;
        private StageRunner _runner;
        private CharacterActor _host;
        private CharacterActor _guestBody;
        private ScriptedCommandSource _hostInput;
        private HeadlessGuest _guest;

        [UnitySetUp]
        public IEnumerator HostWithAGuest()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "online-smoke";

            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            _guest = HeadlessGuest.Join(guestSide);
            yield return UntilFrames(() => net.IsConnected && _guest.IsWelcomed, "the handshake never finished");

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            yield return UntilFrames(() => SceneManager.GetActiveScene().name == MachineScene, "the machine never loaded");
            yield return null;
            yield return null;

            _driver = Object.FindAnyObjectByType<SimulationDriver>();
            _runner = Object.FindAnyObjectByType<StageRunner>();
            _runner.SpawnsEnabled = false;
            foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsInactive.Include))
            {
                Object.Destroy(enemy.gameObject);
            }

            yield return UntilFrames(() => _runner.IsStageLoaded, "the stage never streamed in");

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            Assert.That(actors.Count, Is.EqualTo(2), "A connected guest must wake the second seat (D59).");
            _host = actors[0];
            _guestBody = actors[1];

            // The host's own devices come out of the loop, as in every smoke suite; the guest's seat
            // already answers only to the wire.
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            _driver.Players.Unregister(_host.PlayerId);
            _hostInput = _host.gameObject.AddComponent<ScriptedCommandSource>();
            _hostInput.Bind(_host.PlayerId.Value);
            _driver.Players.Register(_hostInput);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            if (_guest != null)
            {
                Object.Destroy(_guest.gameObject);
            }

            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator The_guest_is_told_which_run_to_load()
        {
            Assert.That(_guest.Launch.HasValue, Is.True, "The host never sent the guest a Launch.");
            Assert.That(_guest.Launch.Value.ChapterId, Is.EqualTo("fixture"));
            Assert.That(_guest.Launch.Value.GuestPlayerId, Is.EqualTo(1));
            Assert.That(_guest.Launch.Value.RosterPicks[1], Is.GreaterThanOrEqualTo(0),
                "The guest's seat went out with no hero in it.");
            yield break;
        }

        [UnityTest]
        public IEnumerator The_second_seat_answers_to_the_wire_not_to_a_device()
        {
            Assert.That(_driver.Players.TryGet(PlayerId.Two, out IPlayerCommandSource source), Is.True,
                "Nothing is registered for Player 2.");
            Assert.That(source, Is.InstanceOf<RemoteCommandSource>(),
                "Player 2 is still a local device — the host's own pad would drive the guest's body.");
            Assert.That(_guestBody.PlayerId, Is.EqualTo(PlayerId.Two));
            yield break;
        }

        [UnityTest]
        public IEnumerator The_guests_stick_moves_player_two_and_nobody_else()
        {
            Vector3 hostStart = _host.Position;
            Vector3 guestStart = _guestBody.Position;

            _guest.Move = Vector2.right;
            yield return Steps(60);
            _guest.Move = Vector2.zero;
            yield return Steps(10);

            Assert.That(_guestBody.Position.x, Is.GreaterThan(guestStart.x + 0.5f),
                "A second of the guest's stick moved Player 2 nowhere.");
            Assert.That(Vector3.Distance(_host.Position, hostStart), Is.LessThan(0.5f),
                "The guest's stick moved the host.");
        }

        [UnityTest]
        public IEnumerator A_guests_jump_is_one_jump()
        {
            _guest.Held = CommandButtons.Jump;
            yield return Steps(4);
            _guest.Held = CommandButtons.None;

            bool leftTheGround = false;
            for (int i = 0; i < 60 && !leftTheGround; i++)
            {
                yield return Steps(1);
                leftTheGround |= !_guestBody.IsGrounded;
            }

            Assert.That(leftTheGround, Is.True, "The guest pressed Jump and Player 2 never left the ground.");
            yield return Until(() => _guestBody.IsGrounded, "Player 2 never landed");
        }

        [UnityTest]
        public IEnumerator A_guest_who_leaves_stops_driving_their_body()
        {
            _guest.Move = Vector2.left;
            yield return Steps(20);
            _guest.Leave();
            yield return Steps(40);

            Vector3 settled = _guestBody.Position;
            yield return Steps(30);
            Assert.That(Vector3.Distance(_guestBody.Position, settled), Is.LessThan(0.05f),
                "Player 2 kept running on the last stick heard after the guest left.");
        }

        private IEnumerator Steps(int steps)
        {
            int target = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < target; guard++)
            {
                yield return null;
            }
        }

        private IEnumerator Until(System.Func<bool> condition, string failure)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {PatienceSteps} steps).");
        }

        private static IEnumerator UntilFrames(System.Func<bool> condition, string failure)
        {
            for (int guard = 0; guard < LoadFrameCeiling; guard++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{failure} (waited {LoadFrameCeiling} frames).");
        }
    }
}
```

`OnlineHostSmokeTests` uses `ScriptedCommandSource.Bind`, which is `internal` in the same test assembly — fine.

- [ ] **Step 16: Run the PlayMode suite**

`run_tests` mode `PlayMode`, `async_tests: true`, `filter_type: testName`, `filter: OnlineHostSmokeTests`; poll `test_status`. Expected: all five pass. Then the full PlayMode suite: the M6/M7 tripwires must be unchanged. Delete `Assets/InitTestScene*`.

Failure guide: *"A connected guest must wake the second seat"* — `SessionBinder.Awake` did not see the session connected; check that the handshake finished before the launch (the set-up waits on it) and that `hosting` reads `net.IsConnected`. *"Player 2 is still a local device"* — `NetSeats.MakeRemote` ran after the device's `OnEnable`; confirm `SessionBinder` keeps `[DefaultExecutionOrder(-100)]`.

- [ ] **Step 17: Commit** — subject `89: the session and the handshake — a remote Player 2`. Body: the session lives on the object that survives scenes; the handshake refuses a mismatched build with a reason; the host's second seat takes a `RemoteCommandSource`; the guest's driver samples and sends and simulates nothing; the development panel is editor/dev-only; Plan 1's guest plays the host's hero until the lobby.

---

### Task 90: Two editors

Multiplayer Play Mode runs a second copy of the game beside the first. With the local socket and the panel from Task 89, that is the day-to-day online loop — and Stage A's first thing Michael can try.

**Files:**
- Modify: `Packages/manifest.json` (through the Package Manager, not by hand)
- Modify: `ProjectSettings/ProjectSettings.asset` (`runInBackground`)

- [ ] **Step 1: Add the package**

With `eval` (the editor out of play mode) — it only asks; the Package Manager works in the background, and blocking the editor's main thread to wait for it would wedge the bridge:

```csharp
UnityEditor.PackageManager.Client.Add("com.unity.multiplayer.playmode");
return "requested";
```

Then check on disk every few seconds until it appears (a minute at most): `grep -n "multiplayer.playmode" Packages/manifest.json`, and `recompile_status` until the resolve and recompile finish. The Package Manager picks the version that matches Unity 6.5. If it never appears, read `console` `level: error` and stop and report — do not hand-edit a guessed version into the manifest.

- [ ] **Step 2: Keep the host running when its window is not in front**

A host who clicks into the other editor, or alt-tabs, must not freeze their guest's game. With `eval`:

```csharp
UnityEditor.PlayerSettings.runInBackground = true;
UnityEditor.AssetDatabase.SaveAssets();
return "runInBackground=" + UnityEditor.PlayerSettings.runInBackground;
```

Verify on disk: `grep -n "runInBackground" ProjectSettings/ProjectSettings.asset` → `runInBackground: 1`.

- [ ] **Step 3: Recompile and run both suites.** Expected: green; the package adds no tests of ours.

- [ ] **Step 4: Live check (slow, settled state — drive it yourself, with `QUIET ON`)**

Ask the orchestrator for `QUIET ON — two editors`. Open *Window → Multiplayer → Multiplayer Play Mode*, activate **Player 2**, and enter play mode from `Frontend.unity`. In the main editor click **Host local**; in Player 2's window click **Join local**. Read both panels' status lines with `eval` or a capture: *"A guest joined"* and *"Joined — waiting for the host to launch"*. Exit play mode, deactivate Player 2, and send `QUIET OFF` word to the orchestrator.

- [ ] **Step 5: Commit** — subject `90: two editors — Multiplayer Play Mode and runInBackground`. Body: the package is the daily online loop (HANDOFF-M8, Testing); `runInBackground` because a host who looks away must not freeze the guest.

- [ ] **Step 6: Michael tries it (Stage A — the remote controller)**

Hand Michael this, and treat his report as the verification:

1. Open *Window → Multiplayer → Multiplayer Play Mode* and tick **Player 2**. A second game window appears when you press Play.
2. Press **Play**. In the **main** window, click **Host local** (bottom right). In the **Player 2** window, click **Join local**. The main window's panel reads *"A guest joined"*.
3. In the main window, start a game as usual — title, character select, chapter select — and launch. The Player 2 window loads the game too.
4. Use the **keyboard only** for this check, and click into the window you want to drive first (a controller would drive both windows at once). Click into the **Player 2** window and move with WASD: **Player 2 moves in the main window.** Jump with it. The main window's own player stands still.
5. Click back into the main window and move: only Player 1 moves.

The Player 2 window itself shows nothing useful yet — that is the next stage.

---
## Stage B — The mirror

### Task 91: Ids and the snapshot

Everything the guest must draw, as plain Core data with a codec — and a network id for everything that comes and goes. Every Core struct that travels gets a **field-coverage test** (planning decision 2): it fills every field by reflection, private ones included, round-trips it, and names any field that came back different. A field added in M9 that nobody taught the wire fails the suite instead of silently freezing on the guest's screen.

**Files:**
- Modify: `Assets/_BattleBomb/Core/Combat/Health.cs`, `Core/Stats/ManaPool.cs`, `Core/Combat/AttackTuning.cs`, `Core/Combat/StatusTrack.cs`
- Create: `Assets/_BattleBomb/Core/Net/EntityRef.cs`, `Snapshots.cs`, `StateCodec.cs`, `SnapshotCodec.cs`, `ItemWire.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Characters/EnemyActor.cs`, `Gameplay/Combat/EnemySpawner.cs`, `Gameplay/World/StageRunner.cs` (one call), `Gameplay/Loot/DropPickup.cs`, `Gameplay/Simulation/SimulationDriver.cs`, `Gameplay/Characters/TrainingDummy.cs`, `Gameplay/World/LoadedStage.cs`
- Test: `Assets/_BattleBomb/Tests/EditMode/Net/FieldCoverage.cs`, `StateCodecCoverageTests.cs`, `SnapshotCodecTests.cs`, `ItemWireTests.cs`

- [ ] **Step 1: The coverage helper and the failing tests**

`Assets/_BattleBomb/Tests/EditMode/Net/FieldCoverage.cs`:

```csharp
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>
    /// Fills every field of a struct — nested structs and private fields included — with a
    /// distinctive, valid, non-default value by reflection, going around every constructor; and
    /// compares two values field by field. A codec that forgets a field hands back its default, and
    /// <see cref="AssertSame"/> names the path (HANDOFF-M8 planning decision 2).
    /// </summary>
    internal static class FieldCoverage
    {
        private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static T Filled<T>(int seed) where T : struct =>
            (T)Fill(typeof(T), new System.Random(seed), typeof(T).Name);

        internal static void AssertSame(object expected, object actual, string path)
        {
            string difference = FirstDifference(expected, actual, path);
            Assert.That(difference, Is.Null, $"{difference} did not survive the wire.");
        }

        /// <summary>The path of the first field that differs, or null when every field matches. A plain
        /// value rather than an assertion, so the helper can be tested against a broken codec.</summary>
        internal static string FirstDifference(object expected, object actual, string path)
        {
            if (expected == null || actual == null)
            {
                return Equals(expected, actual) ? null : path;
            }

            Type type = expected.GetType();
            if (type.IsArray)
            {
                var a = (Array)expected;
                var b = (Array)actual;
                if (a.Length != b.Length)
                {
                    return $"{path}.Length";
                }

                for (int i = 0; i < a.Length; i++)
                {
                    string inner = FirstDifference(a.GetValue(i), b.GetValue(i), $"{path}[{i}]");
                    if (inner != null)
                    {
                        return inner;
                    }
                }

                return null;
            }

            if (type.IsPrimitive || type.IsEnum || type == typeof(Vector3) || type == typeof(Vector2) || type == typeof(string))
            {
                return expected.Equals(actual) ? null : path;
            }

            foreach (FieldInfo field in type.GetFields(AllInstance))
            {
                string inner = FirstDifference(field.GetValue(expected), field.GetValue(actual), $"{path}.{field.Name}");
                if (inner != null)
                {
                    return inner;
                }
            }

            return null;
        }

        private static object Fill(Type type, System.Random random, string path)
        {
            if (type == typeof(int))
            {
                return random.Next(1, 1000);
            }

            if (type == typeof(uint))
            {
                return (uint)random.Next(1, 1000);
            }

            // Inside (0, 1): every float the wire carries is valid there, including the clamped ones.
            if (type == typeof(float))
            {
                return (float)(0.01 + random.NextDouble() * 0.98);
            }

            if (type == typeof(bool))
            {
                return true;
            }

            if (type == typeof(Vector3))
            {
                return new Vector3((float)Fill(typeof(float), random, path), (float)Fill(typeof(float), random, path),
                    (float)Fill(typeof(float), random, path));
            }

            if (type == typeof(Vector2))
            {
                return new Vector2((float)Fill(typeof(float), random, path), (float)Fill(typeof(float), random, path));
            }

            if (type.IsEnum)
            {
                // Never the zero default, so a dropped enum field is caught like any other.
                Array values = Enum.GetValues(type);
                return values.GetValue(values.Length - 1);
            }

            if (type.IsValueType && !type.IsPrimitive)
            {
                object boxed = Activator.CreateInstance(type);
                foreach (FieldInfo field in type.GetFields(AllInstance))
                {
                    field.SetValue(boxed, Fill(field.FieldType, random, $"{path}.{field.Name}"));
                }

                return boxed;
            }

            throw new NotSupportedException($"{path}: FieldCoverage cannot fill a {type.Name}.");
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/StateCodecCoverageTests.cs`:

```csharp
using System;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    /// <summary>Every Core struct that crosses the wire comes back whole — every field, private ones too.</summary>
    public sealed class StateCodecCoverageTests
    {
        [Test]
        public void MotorState_travels_whole() =>
            RoundTrip<MotorState>((w, v) => StateCodec.Write(w, v), StateCodec.ReadMotor);

        [Test]
        public void AttackTuning_travels_whole_including_its_private_radial_flag() =>
            RoundTrip<AttackTuning>((w, v) => StateCodec.Write(w, v), StateCodec.ReadAttack);

        [Test]
        public void CombatState_travels_whole() =>
            RoundTrip<CombatState>((w, v) => StateCodec.Write(w, v), StateCodec.ReadCombat);

        [Test]
        public void Health_travels_whole() =>
            RoundTrip<Health>((w, v) => StateCodec.Write(w, v), StateCodec.ReadHealth);

        [Test]
        public void PlayerCondition_travels_whole() =>
            RoundTrip<PlayerCondition>((w, v) => StateCodec.Write(w, v), StateCodec.ReadCondition);

        [Test]
        public void ReviveChannel_travels_whole() =>
            RoundTrip<ReviveChannel>((w, v) => StateCodec.Write(w, v), StateCodec.ReadRevive);

        [Test]
        public void ManaPool_travels_whole() =>
            RoundTrip<ManaPool>((w, v) => StateCodec.Write(w, v), StateCodec.ReadMana);

        [Test]
        public void EnemyState_travels_whole() =>
            RoundTrip<EnemyState>((w, v) => StateCodec.Write(w, v), StateCodec.ReadBrain);

        [Test]
        public void StatusInstance_travels_whole() =>
            RoundTrip<StatusInstance>((w, v) => StateCodec.Write(w, v), StateCodec.ReadStatus);

        [Test]
        public void ProjectileState_travels_whole() =>
            RoundTrip<ProjectileState>((w, v) => StateCodec.Write(w, v), StateCodec.ReadProjectile);

        [Test]
        public void ArenaBounds_travels_whole()
        {
            var bounds = new ArenaBounds(-3.25f, 41.5f, 0.125f);
            var writer = new NetWriter();
            StateCodec.Write(writer, bounds);
            ArenaBounds back = StateCodec.ReadBounds(new NetReader(writer.ToArray()));

            Assert.That(back.MinX, Is.EqualTo(bounds.MinX));
            Assert.That(back.MaxX, Is.EqualTo(bounds.MaxX));
            Assert.That(back.GroundY, Is.EqualTo(bounds.GroundY));
        }

        [Test]
        public void Inverted_bounds_on_the_wire_are_malformed()
        {
            var writer = new NetWriter();
            writer.WriteFloat(5f);
            writer.WriteFloat(1f);
            writer.WriteFloat(0f);
            Assert.Throws<NetFormatException>(() => StateCodec.ReadBounds(new NetReader(writer.ToArray())));
        }

        [Test]
        public void The_coverage_check_itself_catches_a_forgotten_field()
        {
            MotorState full = FieldCoverage.Filled<MotorState>(7);
            var forgetful = new MotorState(full.Position, full.Velocity, full.Facing, full.IsGrounded, full.StepsSinceGrounded, 0);

            Assert.That(FieldCoverage.FirstDifference(full, forgetful, "MotorState"), Is.EqualTo("MotorState.JumpBufferedFor"),
                "The helper passed a value with a field missing — it would pass a broken codec too.");
        }

        private static void RoundTrip<T>(Action<NetWriter, T> write, Func<NetReader, T> read) where T : struct
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                T value = FieldCoverage.Filled<T>(seed);
                var writer = new NetWriter();
                write(writer, value);
                var reader = new NetReader(writer.ToArray());
                T back = read(reader);

                FieldCoverage.AssertSame(value, back, typeof(T).Name);
                Assert.That(reader.Remaining, Is.Zero, $"{typeof(T).Name} read back fewer bytes than it wrote.");
            }
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/SnapshotCodecTests.cs`:

```csharp
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class SnapshotCodecTests
    {
        [Test]
        public void A_world_with_two_players_round_trips()
        {
            WorldSnapshot sent = World();
            var writer = new NetWriter();
            SnapshotCodec.Write(writer, sent);

            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Snapshot));
            var received = new WorldSnapshot();
            SnapshotCodec.Read(reader, received);

            Assert.That(received.HostFrame, Is.EqualTo(sent.HostFrame));
            Assert.That(received.AckGuestFrame, Is.EqualTo(sent.AckGuestFrame));
            Assert.That(received.AttemptStepsAllDown, Is.EqualTo(sent.AttemptStepsAllDown));
            Assert.That(received.Bounds.MaxX, Is.EqualTo(sent.Bounds.MaxX));
            Assert.That(received.Players.Count, Is.EqualTo(2));
            for (int i = 0; i < 2; i++)
            {
                FieldCoverage.AssertSame(sent.Players[i], received.Players[i], $"Players[{i}]");
            }

            FieldCoverage.AssertSame(sent.Enemies[0], received.Enemies[0], "Enemies[0]");
            FieldCoverage.AssertSame(sent.Dummies[0], received.Dummies[0], "Dummies[0]");
            FieldCoverage.AssertSame(sent.Projectiles[1], received.Projectiles[1], "Projectiles[1]");
            Assert.That(received.DropIds, Is.EqualTo(sent.DropIds));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void Reading_reuses_the_snapshot_it_is_handed()
        {
            WorldSnapshot sent = World();
            var writer = new NetWriter();
            SnapshotCodec.Write(writer, sent);
            var received = World();
            received.Enemies.Add(received.Enemies[0]);

            var reader = new NetReader(writer.ToArray());
            reader.ReadByte();
            SnapshotCodec.Read(reader, received);

            Assert.That(received.Enemies.Count, Is.EqualTo(sent.Enemies.Count), "Stale entries survived a read.");
        }

        [Test]
        public void Lookups_find_players_enemies_and_dummies()
        {
            WorldSnapshot world = World();
            Assert.That(world.TryGetPlayer(1, out PlayerSnapshot two), Is.True);
            Assert.That(two.PlayerId, Is.EqualTo(1));
            Assert.That(world.TryGetEnemy(12, out _), Is.True);
            Assert.That(world.TryGetEnemy(99, out _), Is.False);
            Assert.That(world.TryGetDummy(0, 3, out _), Is.True);
            Assert.That(world.TryGetDummy(1, 3, out _), Is.False, "A dummy of another stage answered.");
        }

        private static WorldSnapshot World()
        {
            var world = new WorldSnapshot
            {
                HostFrame = 1200,
                AckGuestFrame = 1187,
                Bounds = new ArenaBounds(-8f, 24f, 0f),
                AttemptStepsAllDown = 17,
            };

            for (int id = 0; id < 2; id++)
            {
                world.Players.Add(new PlayerSnapshot(
                    id,
                    FieldCoverage.Filled<MotorState>(10 + id),
                    FieldCoverage.Filled<CombatState>(20 + id),
                    FieldCoverage.Filled<PlayerCondition>(30 + id),
                    FieldCoverage.Filled<ReviveChannel>(40 + id),
                    FieldCoverage.Filled<ManaPool>(50 + id),
                    id == 0,
                    new[] { FieldCoverage.Filled<StatusInstance>(60 + id) },
                    id == 1 ? 0 : -1,
                    3 + id,
                    id * 45));
            }

            world.Enemies.Add(new EnemySnapshot(
                12, 0, 1, true, 5,
                FieldCoverage.Filled<MotorState>(70), FieldCoverage.Filled<EnemyState>(71),
                FieldCoverage.Filled<Health>(72), 1, 0, new[] { FieldCoverage.Filled<StatusInstance>(73) }));
            world.Enemies.Add(new EnemySnapshot(
                13, 0, 0, false, -1,
                FieldCoverage.Filled<MotorState>(74), FieldCoverage.Filled<EnemyState>(75),
                FieldCoverage.Filled<Health>(76), -1, 12, new StatusInstance[0]));
            world.Dummies.Add(new DummySnapshot(0, 3, FieldCoverage.Filled<MotorState>(80), FieldCoverage.Filled<Health>(81)));
            world.Projectiles.Add(FieldCoverage.Filled<ProjectileState>(90));
            world.Projectiles.Add(FieldCoverage.Filled<ProjectileState>(91));
            world.DropIds.Add(4);
            world.DropIds.Add(9);
            return world;
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/ItemWireTests.cs`:

```csharp
using BattleBomb.Core.Combat;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Stats;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class ItemWireTests
    {
        [Test]
        public void An_item_crosses_with_every_rolled_value()
        {
            var item = new ItemInstance(
                new ItemIdentity(7, "Clean Knife", ItemSlot.Weapon, WeaponClass.Sword, PetClass.None),
                QualityRank.Clean,
                new GearContribution(weaponDamage: 12f, critChance: 0.25f),
                new[] { new AffixRoll((AffixId)1, 0.5f, new ElementId(2)) },
                4,
                new ItemInvestment(3, 1, true),
                0f,
                default,
                new ActivePayload(0.5f, new ElementId(1), 2f, 180));

            var writer = new NetWriter();
            ItemWire.Write(writer, item);
            ItemInstance back = ItemWire.Read(new NetReader(writer.ToArray()), null);

            Assert.That(back.DefinitionId, Is.EqualTo(7));
            Assert.That(back.DisplayName, Is.EqualTo("Clean Knife"));
            Assert.That(back.Quality, Is.EqualTo(QualityRank.Clean));
            Assert.That(back.CoreStats.WeaponDamage, Is.EqualTo(12f));
            Assert.That(back.CoreStats.CritChance, Is.EqualTo(0.25f));
            Assert.That(back.AffixCount, Is.EqualTo(1));
            Assert.That(back.Affixes[0].Magnitude, Is.EqualTo(0.5f));
            Assert.That(back.Affixes[0].Element.Value, Is.EqualTo(2));
            Assert.That(back.RequiredLevel, Is.EqualTo(4));
            Assert.That(back.Locked, Is.True);
            Assert.That(back.UpgradeCapacity, Is.EqualTo(3));
            Assert.That(back.ActiveCooldownSteps, Is.EqualTo(180));
        }

        [Test]
        public void An_empty_item_stays_empty()
        {
            var writer = new NetWriter();
            ItemWire.Write(writer, default);
            Assert.That(ItemWire.Read(new NetReader(writer.ToArray()), null).IsEmpty, Is.True);
        }

        [Test]
        public void Text_that_is_not_an_item_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteString("{ not json");
            Assert.Throws<NetFormatException>(() => ItemWire.Read(new NetReader(writer.ToArray()), null));
        }

        [Test]
        public void A_stand_in_carries_only_its_quality()
        {
            ItemInstance standIn = ItemWire.StandIn(QualityRank.Legendary);
            Assert.That(standIn.IsEmpty, Is.False);
            Assert.That(standIn.Quality, Is.EqualTo(QualityRank.Legendary));
        }
    }
}
```

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors.

- [ ] **Step 3: The Core factories** (additions only — no behaviour changes)

In `Assets/_BattleBomb/Core/Combat/Health.cs`, after the public constructor:

```csharp
        /// <summary>A pool exactly as another machine had it (M8's wire). No clamping: the host's
        /// numbers are the truth (D58).</summary>
        public static Health FromValues(float max, float current) => new Health(max, current);
```

In `Assets/_BattleBomb/Core/Stats/ManaPool.cs`, after `Full`:

```csharp
        /// <summary>A pool exactly as another machine had it (M8's wire).</summary>
        public static ManaPool FromValues(float max, float current) => new ManaPool(max, current);
```

In `Assets/_BattleBomb/Core/Combat/AttackTuning.cs`, after `IsRadial`:

```csharp
        /// <summary>The authored radial flag alone — <see cref="IsRadial"/> also counts
        /// <see cref="ResolvesOnLanding"/>. The wire needs this one to rebuild an equal tuning,
        /// because equality is what tells a chain Light from any other attack.</summary>
        public bool IsRadialAuthored => _radial;
```

In `Assets/_BattleBomb/Core/Combat/StatusTrack.cs`, inside `StatusTrack` after `TryGet`:

```csharp
        /// <summary>Every mark, in order — the first is the one presentation tints with.</summary>
        public StatusInstance[] ToArray() => _active.ToArray();

        /// <summary>Replaces every mark with another machine's (M8's replica), order kept.</summary>
        public void Restore(IReadOnlyList<StatusInstance> statuses)
        {
            _active.Clear();
            if (statuses == null)
            {
                return;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                _active.Add(statuses[i]);
            }
        }
```

- [ ] **Step 4: The snapshot model and its codecs**

`Assets/_BattleBomb/Core/Net/EntityRef.cs`:

```csharp
using System;

namespace BattleBomb.Core.Net
{
    public enum EntityKind : byte
    {
        None = 0,
        Player = 1,
        Enemy = 2,
        Dummy = 3,
    }

    /// <summary>
    /// Something in the world, named the same way on both machines: a player by id, an enemy by its
    /// session-unique network id, a dummy by its prop index in the current stage (HANDOFF-M8 planning
    /// decisions 8 and 9). What a <c>HitEvent</c>'s component references become on the wire.
    /// </summary>
    public readonly struct EntityRef : IEquatable<EntityRef>
    {
        public readonly EntityKind Kind;
        public readonly int Id;

        public EntityRef(EntityKind kind, int id)
        {
            Kind = kind;
            Id = id;
        }

        public static EntityRef None => default;

        public static EntityRef Player(int playerId) => new EntityRef(EntityKind.Player, playerId);

        public static EntityRef Enemy(int netId) => new EntityRef(EntityKind.Enemy, netId);

        public static EntityRef Dummy(int propIndex) => new EntityRef(EntityKind.Dummy, propIndex);

        public bool Equals(EntityRef other) => Kind == other.Kind && Id == other.Id;

        public override bool Equals(object obj) => obj is EntityRef other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 397) ^ Id;

        public override string ToString() => $"{Kind} {Id}";
    }
}
```

`Assets/_BattleBomb/Core/Net/Snapshots.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Net
{
    /// <summary>One player as the host simulates them: everything the guest's copy of the actor holds.
    /// Stats derived from gear never travel — only what they are derived from does (Plan 2).</summary>
    public readonly struct PlayerSnapshot
    {
        public readonly int PlayerId;
        public readonly MotorState Motor;
        public readonly CombatState Combat;
        public readonly PlayerCondition Condition;
        public readonly ReviveChannel Revive;
        public readonly ManaPool Mana;
        public readonly bool LeapAvailable;
        public readonly StatusInstance[] Statuses;

        /// <summary>The screen this player has open, as the driver's interaction kind, or -1.</summary>
        public readonly int OpenScreen;

        public readonly int GrabCount;
        public readonly int RefusedSteps;

        public PlayerSnapshot(
            int playerId, in MotorState motor, in CombatState combat, in PlayerCondition condition,
            in ReviveChannel revive, in ManaPool mana, bool leapAvailable, StatusInstance[] statuses,
            int openScreen, int grabCount, int refusedSteps)
        {
            PlayerId = playerId;
            Motor = motor;
            Combat = combat;
            Condition = condition;
            Revive = revive;
            Mana = mana;
            LeapAvailable = leapAvailable;
            Statuses = statuses ?? new StatusInstance[0];
            OpenScreen = openScreen;
            GrabCount = grabCount;
            RefusedSteps = refusedSteps;
        }

        public PlayerSnapshot WithMotor(in MotorState motor) => new PlayerSnapshot(
            PlayerId, motor, Combat, Condition, Revive, Mana, LeapAvailable, Statuses, OpenScreen, GrabCount, RefusedSteps);
    }

    /// <summary>One enemy. Its archetype travels as the stage and roster index it was spawned from,
    /// which both machines resolve against the same authored chapter.</summary>
    public readonly struct EnemySnapshot
    {
        public readonly int NetId;
        public readonly int StageIndex;
        public readonly int RosterIndex;
        public readonly bool IsElite;

        /// <summary>The quality of the piece an elite wears (D22), or -1 — all the guest's tint needs.</summary>
        public readonly int CarriedQuality;

        public readonly MotorState Motor;
        public readonly EnemyState Brain;
        public readonly Health Health;
        public readonly int TargetIndex;
        public readonly int DepletedSteps;
        public readonly StatusInstance[] Statuses;

        public EnemySnapshot(
            int netId, int stageIndex, int rosterIndex, bool isElite, int carriedQuality,
            in MotorState motor, in EnemyState brain, in Health health, int targetIndex, int depletedSteps,
            StatusInstance[] statuses)
        {
            NetId = netId;
            StageIndex = stageIndex;
            RosterIndex = rosterIndex;
            IsElite = isElite;
            CarriedQuality = carriedQuality;
            Motor = motor;
            Brain = brain;
            Health = health;
            TargetIndex = targetIndex;
            DepletedSteps = depletedSteps;
            Statuses = statuses ?? new StatusInstance[0];
        }

        public EnemySnapshot WithMotor(in MotorState motor) => new EnemySnapshot(
            NetId, StageIndex, RosterIndex, IsElite, CarriedQuality, motor, Brain, Health, TargetIndex, DepletedSteps, Statuses);
    }

    /// <summary>A checkpoint room's training dummy, by the stage it stands in and its prop index there.</summary>
    public readonly struct DummySnapshot
    {
        public readonly int StageIndex;
        public readonly int PropIndex;
        public readonly MotorState Motor;
        public readonly Health Health;

        public DummySnapshot(int stageIndex, int propIndex, in MotorState motor, in Health health)
        {
            StageIndex = stageIndex;
            PropIndex = propIndex;
            Motor = motor;
            Health = health;
        }
    }

    /// <summary>
    /// The host's world at one step, as the guest needs it. A class with lists so a buffer can reuse
    /// instances rather than allocate 30 a second.
    /// </summary>
    public sealed class WorldSnapshot
    {
        public int HostFrame;

        /// <summary>The guest command frame the host had played when this was taken (Plan 3's replay point).</summary>
        public int AckGuestFrame;

        public ArenaBounds Bounds = ArenaBounds.Default;
        public int AttemptStepsAllDown;

        public readonly List<PlayerSnapshot> Players = new List<PlayerSnapshot>(2);
        public readonly List<EnemySnapshot> Enemies = new List<EnemySnapshot>(32);
        public readonly List<DummySnapshot> Dummies = new List<DummySnapshot>(4);
        public readonly List<ProjectileState> Projectiles = new List<ProjectileState>(32);
        public readonly List<int> DropIds = new List<int>(16);

        public void Clear()
        {
            HostFrame = 0;
            AckGuestFrame = 0;
            Bounds = ArenaBounds.Default;
            AttemptStepsAllDown = 0;
            Players.Clear();
            Enemies.Clear();
            Dummies.Clear();
            Projectiles.Clear();
            DropIds.Clear();
        }

        public bool TryGetPlayer(int playerId, out PlayerSnapshot player)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i].PlayerId == playerId)
                {
                    player = Players[i];
                    return true;
                }
            }

            player = default;
            return false;
        }

        public bool TryGetEnemy(int netId, out EnemySnapshot enemy)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i].NetId == netId)
                {
                    enemy = Enemies[i];
                    return true;
                }
            }

            enemy = default;
            return false;
        }

        public bool TryGetDummy(int stageIndex, int propIndex, out DummySnapshot dummy)
        {
            for (int i = 0; i < Dummies.Count; i++)
            {
                if (Dummies[i].StageIndex == stageIndex && Dummies[i].PropIndex == propIndex)
                {
                    dummy = Dummies[i];
                    return true;
                }
            }

            dummy = default;
            return false;
        }
    }
}
```

`Assets/_BattleBomb/Core/Net/StateCodec.cs`:

```csharp
using BattleBomb.Core.Combat;
using BattleBomb.Core.Enemies;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Players;
using BattleBomb.Core.Spatial;
using BattleBomb.Core.Stats;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// Every Core struct that travels, field by field, in declaration order. Each writer has a reader
    /// that reads the same fields in the same order and rebuilds through the public constructor or a
    /// named factory. The field-coverage tests hold every pair to "nothing forgotten".
    /// </summary>
    public static class StateCodec
    {
        public static void Write(NetWriter w, in MotorState s)
        {
            w.WriteVector3(s.Position);
            w.WriteVector3(s.Velocity);
            w.WriteByte(s.Facing == Facing.Left ? (byte)0 : (byte)1);
            w.WriteBool(s.IsGrounded);
            w.WriteInt(s.StepsSinceGrounded);
            w.WriteInt(s.JumpBufferedFor);
        }

        public static MotorState ReadMotor(NetReader r) => new MotorState(
            r.ReadVector3(), r.ReadVector3(), r.ReadByte() == 0 ? Facing.Left : Facing.Right,
            r.ReadBool(), r.ReadInt(), r.ReadInt());

        public static void Write(NetWriter w, in AttackTuning a)
        {
            w.WriteInt(a.StartupSteps);
            w.WriteInt(a.ActiveSteps);
            w.WriteInt(a.RecoverySteps);
            w.WriteFloat(a.Damage);
            w.WriteFloat(a.ReachX);
            w.WriteFloat(a.DepthTolerance);
            w.WriteFloat(a.LungeDistance);
            w.WriteInt(a.MaxTargets);
            w.WriteFloat(a.KnockbackSpeed);
            w.WriteFloat(a.LaunchSpeed);
            w.WriteInt(a.HitstopSteps);
            w.WriteFloat(a.MoveSpeedScale);
            w.WriteBool(a.ResolvesOnLanding);
            w.WriteBool(a.IsRadialAuthored);
            w.WriteInt(a.StunSteps);
        }

        public static AttackTuning ReadAttack(NetReader r) => new AttackTuning(
            startupSteps: r.ReadInt(),
            activeSteps: r.ReadInt(),
            recoverySteps: r.ReadInt(),
            damage: r.ReadFloat(),
            reachX: r.ReadFloat(),
            depthTolerance: r.ReadFloat(),
            lungeDistance: r.ReadFloat(),
            maxTargets: r.ReadInt(),
            knockbackSpeed: r.ReadFloat(),
            launchSpeed: r.ReadFloat(),
            hitstopSteps: r.ReadInt(),
            moveSpeedScale: r.ReadFloat(),
            resolvesOnLanding: r.ReadBool(),
            isRadial: r.ReadBool(),
            stunSteps: r.ReadInt());

        public static void Write(NetWriter w, in CombatState c)
        {
            w.WriteByte((byte)c.Phase);
            w.WriteInt(c.StepsInPhase);
            w.WriteInt(c.ComboIndex);
            w.WriteInt(c.ComboWindowLeft);
            w.WriteUInt((uint)c.Buffered);
            w.WriteInt(c.BufferedFor);
            w.WriteInt(c.ChargeSteps);
            w.WriteInt(c.HitstopSteps);
            Write(w, c.CurrentAttack);
            w.WriteByte((byte)c.BufferedCast);
            w.WriteByte((byte)c.CurrentCast);
        }

        public static CombatState ReadCombat(NetReader r) => new CombatState(
            (AttackPhase)r.ReadByte(), r.ReadInt(), r.ReadInt(), r.ReadInt(), (CommandButtons)r.ReadUInt(),
            r.ReadInt(), r.ReadInt(), r.ReadInt(), ReadAttack(r), (MagicCastKind)r.ReadByte(), (MagicCastKind)r.ReadByte());

        public static void Write(NetWriter w, in Health h)
        {
            w.WriteFloat(h.Max);
            w.WriteFloat(h.Current);
        }

        public static Health ReadHealth(NetReader r) => Health.FromValues(r.ReadFloat(), r.ReadFloat());

        public static void Write(NetWriter w, in PlayerCondition c)
        {
            Write(w, c.Health);
            w.WriteInt(c.StaggerSteps);
            w.WriteInt(c.GraceSteps);
        }

        public static PlayerCondition ReadCondition(NetReader r) =>
            new PlayerCondition(ReadHealth(r), r.ReadInt(), r.ReadInt());

        public static void Write(NetWriter w, in ReviveChannel v)
        {
            w.WriteInt(v.TargetIndex);
            w.WriteFloat(v.Progress);
            w.WriteInt(v.StepsElapsed);
            w.WriteInt(v.StepsSincePump);
            w.WriteFloat(v.AccuracySum);
            w.WriteInt(v.Pumps);
        }

        public static ReviveChannel ReadRevive(NetReader r) => new ReviveChannel(
            r.ReadInt(), r.ReadFloat(), r.ReadInt(), r.ReadInt(), r.ReadFloat(), r.ReadInt());

        public static void Write(NetWriter w, in ManaPool m)
        {
            w.WriteFloat(m.Max);
            w.WriteFloat(m.Current);
        }

        public static ManaPool ReadMana(NetReader r) => ManaPool.FromValues(r.ReadFloat(), r.ReadFloat());

        public static void Write(NetWriter w, in EnemyState s)
        {
            w.WriteByte((byte)s.Phase);
            w.WriteInt(s.StepsInPhase);
            w.WriteInt(s.HitstopSteps);
            w.WriteInt(s.Seed);
            w.WriteInt(s.AgeSteps);
        }

        public static EnemyState ReadBrain(NetReader r) =>
            new EnemyState((EnemyPhase)r.ReadByte(), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadInt());

        public static void Write(NetWriter w, in StatusInstance s)
        {
            w.WriteInt(s.Element.Value);
            w.WriteInt(s.RemainingSteps);
            w.WriteInt(s.StepsToTick);
            w.WriteInt(s.TickSteps);
            w.WriteFloat(s.DamagePerTick);
            w.WriteFloat(s.MoveScale);
        }

        public static StatusInstance ReadStatus(NetReader r) => new StatusInstance(
            new ElementId(r.ReadInt()), r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadFloat(), r.ReadFloat());

        public static void Write(NetWriter w, StatusInstance[] statuses)
        {
            w.WriteCount(statuses.Length, NetProtocol.MaxStatuses);
            for (int i = 0; i < statuses.Length; i++)
            {
                Write(w, statuses[i]);
            }
        }

        public static StatusInstance[] ReadStatuses(NetReader r)
        {
            var statuses = new StatusInstance[r.ReadCount(NetProtocol.MaxStatuses)];
            for (int i = 0; i < statuses.Length; i++)
            {
                statuses[i] = ReadStatus(r);
            }

            return statuses;
        }

        public static void Write(NetWriter w, in ProjectileState p)
        {
            w.WriteVector3(p.Position);
            w.WriteVector3(p.Velocity);
            w.WriteFloat(p.Damage);
            w.WriteInt(p.Element.Value);
            w.WriteInt(p.LifeSteps);
            w.WriteInt(p.OwnerPlayerId);
        }

        public static ProjectileState ReadProjectile(NetReader r) => new ProjectileState(
            r.ReadVector3(), r.ReadVector3(), r.ReadFloat(), new ElementId(r.ReadInt()), r.ReadInt(), r.ReadInt());

        public static void Write(NetWriter w, in ArenaBounds b)
        {
            w.WriteFloat(b.MinX);
            w.WriteFloat(b.MaxX);
            w.WriteFloat(b.GroundY);
        }

        public static ArenaBounds ReadBounds(NetReader r)
        {
            float minX = r.ReadFloat();
            float maxX = r.ReadFloat();
            float groundY = r.ReadFloat();
            if (!(maxX >= minX))
            {
                throw new NetFormatException($"Arena bounds {minX}..{maxX} are inverted.");
            }

            return new ArenaBounds(minX, maxX, groundY);
        }
    }
}
```

`Assets/_BattleBomb/Core/Net/SnapshotCodec.cs`:

```csharp
namespace BattleBomb.Core.Net
{
    /// <summary>The host's 30 Hz picture of the world (HANDOFF-M8 paper numbers). Unreliable: a lost
    /// one is simply replaced by the next.</summary>
    public static class SnapshotCodec
    {
        public const int MaxPlayers = 4;

        public static void Write(NetWriter w, WorldSnapshot s)
        {
            w.WriteByte((byte)NetMessageKind.Snapshot);
            w.WriteInt(s.HostFrame);
            w.WriteInt(s.AckGuestFrame);
            StateCodec.Write(w, s.Bounds);
            w.WriteInt(s.AttemptStepsAllDown);

            w.WriteCount(s.Players.Count, MaxPlayers);
            for (int i = 0; i < s.Players.Count; i++)
            {
                PlayerSnapshot p = s.Players[i];
                w.WriteInt(p.PlayerId);
                StateCodec.Write(w, p.Motor);
                StateCodec.Write(w, p.Combat);
                StateCodec.Write(w, p.Condition);
                StateCodec.Write(w, p.Revive);
                StateCodec.Write(w, p.Mana);
                w.WriteBool(p.LeapAvailable);
                StateCodec.Write(w, p.Statuses);
                w.WriteInt(p.OpenScreen);
                w.WriteInt(p.GrabCount);
                w.WriteInt(p.RefusedSteps);
            }

            w.WriteCount(s.Enemies.Count, NetProtocol.MaxEntities);
            for (int i = 0; i < s.Enemies.Count; i++)
            {
                EnemySnapshot e = s.Enemies[i];
                w.WriteInt(e.NetId);
                w.WriteInt(e.StageIndex);
                w.WriteInt(e.RosterIndex);
                w.WriteBool(e.IsElite);
                w.WriteInt(e.CarriedQuality);
                StateCodec.Write(w, e.Motor);
                StateCodec.Write(w, e.Brain);
                StateCodec.Write(w, e.Health);
                w.WriteInt(e.TargetIndex);
                w.WriteInt(e.DepletedSteps);
                StateCodec.Write(w, e.Statuses);
            }

            w.WriteCount(s.Dummies.Count, NetProtocol.MaxEntities);
            for (int i = 0; i < s.Dummies.Count; i++)
            {
                DummySnapshot d = s.Dummies[i];
                w.WriteInt(d.StageIndex);
                w.WriteInt(d.PropIndex);
                StateCodec.Write(w, d.Motor);
                StateCodec.Write(w, d.Health);
            }

            w.WriteCount(s.Projectiles.Count, NetProtocol.MaxEntities);
            for (int i = 0; i < s.Projectiles.Count; i++)
            {
                StateCodec.Write(w, s.Projectiles[i]);
            }

            w.WriteCount(s.DropIds.Count, NetProtocol.MaxEntities);
            for (int i = 0; i < s.DropIds.Count; i++)
            {
                w.WriteInt(s.DropIds[i]);
            }
        }

        /// <summary>Reads into <paramref name="into"/>, clearing it first. The reader sits after the kind byte.</summary>
        public static void Read(NetReader r, WorldSnapshot into)
        {
            into.Clear();
            into.HostFrame = r.ReadInt();
            into.AckGuestFrame = r.ReadInt();
            into.Bounds = StateCodec.ReadBounds(r);
            into.AttemptStepsAllDown = r.ReadInt();

            int players = r.ReadCount(MaxPlayers);
            for (int i = 0; i < players; i++)
            {
                into.Players.Add(new PlayerSnapshot(
                    r.ReadInt(), StateCodec.ReadMotor(r), StateCodec.ReadCombat(r), StateCodec.ReadCondition(r),
                    StateCodec.ReadRevive(r), StateCodec.ReadMana(r), r.ReadBool(), StateCodec.ReadStatuses(r),
                    r.ReadInt(), r.ReadInt(), r.ReadInt()));
            }

            int enemies = r.ReadCount(NetProtocol.MaxEntities);
            for (int i = 0; i < enemies; i++)
            {
                into.Enemies.Add(new EnemySnapshot(
                    r.ReadInt(), r.ReadInt(), r.ReadInt(), r.ReadBool(), r.ReadInt(),
                    StateCodec.ReadMotor(r), StateCodec.ReadBrain(r), StateCodec.ReadHealth(r),
                    r.ReadInt(), r.ReadInt(), StateCodec.ReadStatuses(r)));
            }

            int dummies = r.ReadCount(NetProtocol.MaxEntities);
            for (int i = 0; i < dummies; i++)
            {
                into.Dummies.Add(new DummySnapshot(r.ReadInt(), r.ReadInt(), StateCodec.ReadMotor(r), StateCodec.ReadHealth(r)));
            }

            int projectiles = r.ReadCount(NetProtocol.MaxEntities);
            for (int i = 0; i < projectiles; i++)
            {
                into.Projectiles.Add(StateCodec.ReadProjectile(r));
            }

            int drops = r.ReadCount(NetProtocol.MaxEntities);
            for (int i = 0; i < drops; i++)
            {
                into.DropIds.Add(r.ReadInt());
            }
        }
    }
}
```

`Assets/_BattleBomb/Core/Net/ItemWire.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using BattleBomb.Core.Items;
using BattleBomb.Core.Saves;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// An item on the wire, as the save already writes it (D52: identity id plus rolled values, never
    /// by name) — the save's own DTO through <c>JsonUtility</c>, so M8 adds no second item format to
    /// keep in step. Drops are rare, so the size does not matter; Plan 2's inventory state may want a
    /// binary form if a full sack turns out heavy.
    /// </summary>
    public static class ItemWire
    {
        public static void Write(NetWriter w, in ItemInstance item)
        {
            string json = item.IsEmpty ? string.Empty : JsonUtility.ToJson(SaveMapper.ToSave(item));
            if (Encoding.UTF8.GetByteCount(json) > NetProtocol.MaxItemJsonBytes)
            {
                throw new NetFormatException($"An item of {json.Length} characters is too large to send.");
            }

            w.WriteString(json);
        }

        public static ItemInstance Read(NetReader r, IReadOnlyList<ItemSpec> catalog)
        {
            string json = r.ReadString(NetProtocol.MaxItemJsonBytes);
            if (json.Length == 0)
            {
                return default;
            }

            ItemSave save;
            try
            {
                save = JsonUtility.FromJson<ItemSave>(json);
            }
            catch (ArgumentException e)
            {
                throw new NetFormatException($"An item that is not an item: {e.Message}");
            }

            if (save == null)
            {
                throw new NetFormatException("An item that is not an item.");
            }

            return SaveMapper.ToInstance(save, catalog);
        }

        /// <summary>A stand-in that carries only a quality — all an elite's armor tint reads on the
        /// guest (<c>EnemyVisual</c>). Not empty, and never grabbed: the real piece drops from the host.</summary>
        public static ItemInstance StandIn(QualityRank quality) =>
            new ItemInstance(default, quality, default, Array.Empty<AffixRoll>(), 1);
    }
}
```

- [ ] **Step 5: Run the Core tests** — `recompile`; `run_tests` EditMode `StateCodecCoverageTests`, `SnapshotCodecTests`, `ItemWireTests`. Expected: all pass. A coverage failure names the struct and field, e.g. `CombatState.CurrentAttack._radial did not survive the wire` — fix the codec, never the test.

- [ ] **Step 6: Network ids in Gameplay**

In `Assets/_BattleBomb/Gameplay/Characters/EnemyActor.cs`, after `IsElite`:

```csharp
        /// <summary>This enemy's id on the wire: unique for the whole session and never reused
        /// (HANDOFF-M8 planning decision 8). Zero for an enemy placed by hand in a scene.</summary>
        public int NetId { get; private set; }

        /// <summary>The stage and roster slot this enemy was spawned from — how the guest finds the
        /// same authored archetype.</summary>
        internal int OriginStage { get; private set; } = -1;

        internal int OriginRoster { get; private set; } = -1;

        internal void SetOrigin(int netId, int stageIndex, int rosterIndex)
        {
            NetId = netId;
            OriginStage = stageIndex;
            OriginRoster = rosterIndex;
        }
```

In `Assets/_BattleBomb/Gameplay/Combat/EnemySpawner.cs`:

(a) Below `private int _serial;` add:

```csharp
        /// <summary>Network ids. Unlike <see cref="_serial"/> this never resets: a wipe or an airlock
        /// must not hand a new enemy an id an old one still holds on the guest's screen.</summary>
        private int _nextNetId;
```

(b) Give `SpawnWave` two trailing parameters — `int stageIndex = -1, int rosterIndex = -1` — and inside the loop, directly after `actor.Configure(...)`, add:

```csharp
                    actor.SetOrigin(++_nextNetId, stageIndex, rosterIndex);
```

In `Assets/_BattleBomb/Gameplay/World/StageRunner.cs`, `SpawnWave`'s last statement becomes:

```csharp
            return _spawner.SpawnWave(
                definition, wave.Count, arena.SpawnPoints,
                _tier.HealthMultiplier, _tier.DamageMultiplier, _current.StageIndex, wave.EnemyIndex);
```

In `Assets/_BattleBomb/Gameplay/Loot/DropPickup.cs`:

(a) After `public ItemInstance Item => _item;` add:

```csharp
        /// <summary>This drop's id on the wire, unique for the session (HANDOFF-M8 planning decision 8).</summary>
        public int NetId { get; private set; }
```

(b) `Spawn` gains a trailing parameter `int netId = 0`, and before `return pickup;` add `pickup.NetId = netId;`.

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs`:

(a) With the private fields:

```csharp
        private int _nextPickupId;
```

(b) With the events:

```csharp
        /// <summary>Raised inside the step when a drop appears — the host sends it, with its item (M8).</summary>
        internal event Action<DropPickup> PickupSpawned;
```

(c) Add the one place drops are born:

```csharp
        /// <summary>Every drop enters the world here, with a session-unique id, so the host can tell the
        /// guest about it once and name it in every snapshot after.</summary>
        private void SpawnPickup(Vector3 at, in ItemInstance item)
        {
            DropPickup pickup = DropPickup.Spawn(at, item, ++_nextPickupId);
            _pickups.Add(pickup);
            PickupSpawned?.Invoke(pickup);
        }
```

(d) Replace the three `_pickups.Add(DropPickup.Spawn(...))` calls — in `SpawnDebugDrop`, and both in `ResolveDeaths` — with `SpawnPickup(...)` using the same arguments.

In `Assets/_BattleBomb/Gameplay/Characters/TrainingDummy.cs`, after `IsDepleted`:

```csharp
        /// <summary>Which of its stage's props this dummy is — both machines place props from the same
        /// markers in the same order, so the index names the same dummy on each (planning decision 8).
        /// -1 for a dummy placed by hand in a scene.</summary>
        public int PropIndex { get; private set; } = -1;

        internal void SetPropIndex(int index) => PropIndex = index;
```

In `Assets/_BattleBomb/Gameplay/World/LoadedStage.cs`:

(a) Add `using BattleBomb.Gameplay.Characters;`.

(b) `Place` returns what it made:

```csharp
        private GameObject Place(GameObject prefab, Vector3 at)
        {
            if (prefab == null)
            {
                return null;
            }

            at.y += prefab.transform.localPosition.y;
            GameObject go = Object.Instantiate(prefab, at, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(go, Scene);
            _props.Add(go);
            return go;
        }
```

(c) In `SpawnProps`, replace `Place(dummy, room.DummyPosition);` with:

```csharp
                GameObject placed = Place(dummy, room.DummyPosition);
                TrainingDummy target = placed != null ? placed.GetComponent<TrainingDummy>() : null;
                if (target != null)
                {
                    target.SetPropIndex(_props.Count - 1);
                }
```

(d) Add:

```csharp
        internal TrainingDummy DummyAt(int propIndex)
        {
            if (propIndex < 0 || propIndex >= _props.Count || _props[propIndex] == null)
            {
                return null;
            }

            TrainingDummy dummy = _props[propIndex].GetComponent<TrainingDummy>();
            return dummy != null && dummy.PropIndex == propIndex ? dummy : null;
        }
```

- [ ] **Step 7: Recompile; run both suites.** Expected: green — ids are assigned but nothing reads them yet, so no behaviour changes. Delete `Assets/InitTestScene*`.

- [ ] **Step 8: Commit** — subject `91: ids and the snapshot`. Body: the snapshot model and codecs in Core; every travelling struct under a field-coverage test so a forgotten field fails the suite; session-unique ids for enemies and drops, prop indices for dummies; items travel as the save's own DTO.

---

### Task 92: The host speaks

The host sends what it simulates: a snapshot every second step, and the moments that happen once — a hit, a drop — as reliable events stamped with their step. `HitEvent`'s component references cross as entity references (planning decision 9). And until Plan 2 brings the guest's screens online, the host declines to open one for the guest.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/ReplicatedEvents.cs`, `Assets/_BattleBomb/Core/Net/EventCodec.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs`, `EnemyActor.cs`, `TrainingDummy.cs` (`CaptureReplica`)
- Modify: `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (`RefusedStepsFor`, `AttemptStepsAllDown`, `MayOpenScreen`)
- Modify: `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`, `Assets/_BattleBomb/Gameplay/Session/SessionBinder.cs` (hand the runner over)
- Test: `Assets/_BattleBomb/Tests/EditMode/Net/EventCodecTests.cs`; extend `Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs`

- [ ] **Step 1: The failing event-codec test**

`Assets/_BattleBomb/Tests/EditMode/Net/EventCodecTests.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Items;
using BattleBomb.Core.Net;
using BattleBomb.Core.Stats;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class EventCodecTests
    {
        [Test]
        public void Hits_and_drops_round_trip_with_their_steps()
        {
            var knife = new ItemInstance(
                new ItemIdentity(7, "Knife", ItemSlot.Weapon, default, default), QualityRank.Shiny,
                new GearContribution(weaponDamage: 9f), new AffixRoll[0], 2);
            var sent = new List<ReplicatedEvent>
            {
                ReplicatedEvent.OfHit(new HitRecord(
                    EntityRef.Player(0), EntityRef.Player(1), 0f, new Vector3(1f, 0f, 2f), true, false, false)).At(300),
                ReplicatedEvent.OfHit(new HitRecord(
                    EntityRef.None, EntityRef.Enemy(42), 3.5f, Vector3.one, false, false, true)).At(301),
                ReplicatedEvent.OfDrop(new DropRecord(5, new Vector3(4f, 0.35f, -1f), knife)).At(301),
            };

            var writer = new NetWriter();
            EventCodec.Write(writer, sent);
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.Events));
            var received = new List<ReplicatedEvent>();
            EventCodec.Read(reader, null, received);

            Assert.That(received.Count, Is.EqualTo(3));
            Assert.That(received[0].HostFrame, Is.EqualTo(300));
            Assert.That(received[0].Hit.Attacker, Is.EqualTo(EntityRef.Player(0)));
            Assert.That(received[0].Hit.Target, Is.EqualTo(EntityRef.Player(1)));
            Assert.That(received[0].Hit.IsPartner, Is.True);
            Assert.That(received[1].Hit.Target, Is.EqualTo(EntityRef.Enemy(42)));
            Assert.That(received[1].Hit.Damage, Is.EqualTo(3.5f));
            Assert.That(received[1].Hit.IsDamageOverTime, Is.True);
            Assert.That(received[2].Kind, Is.EqualTo(ReplicatedEventKind.DropSpawned));
            Assert.That(received[2].Drop.NetId, Is.EqualTo(5));
            Assert.That(received[2].Drop.Item.DefinitionId, Is.EqualTo(7));
            Assert.That(received[2].Drop.Item.Quality, Is.EqualTo(QualityRank.Shiny));
        }

        [Test]
        public void An_event_kind_nothing_defines_is_malformed()
        {
            var writer = new NetWriter();
            writer.WriteUShort(1);
            writer.WriteByte(99);
            writer.WriteInt(0);
            Assert.Throws<NetFormatException>(() => EventCodec.Read(new NetReader(writer.ToArray()), null, new List<ReplicatedEvent>()));
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail** — `recompile`. Expected: compile errors.

- [ ] **Step 3: The events and their codec**

`Assets/_BattleBomb/Core/Net/ReplicatedEvents.cs`:

```csharp
using BattleBomb.Core.Items;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>Values are wire format: never renumber, only add.</summary>
    public enum ReplicatedEventKind : byte
    {
        Hit = 1,
        DropSpawned = 2,
    }

    /// <summary>A landed hit, with its two parties as entity references (HANDOFF-M8 planning decision 9).</summary>
    public readonly struct HitRecord
    {
        public readonly EntityRef Attacker;
        public readonly EntityRef Target;
        public readonly float Damage;
        public readonly Vector3 Position;
        public readonly bool IsPartner;
        public readonly bool IsCrit;
        public readonly bool IsDamageOverTime;

        public HitRecord(
            EntityRef attacker, EntityRef target, float damage, Vector3 position,
            bool isPartner, bool isCrit, bool isDamageOverTime)
        {
            Attacker = attacker;
            Target = target;
            Damage = damage;
            Position = position;
            IsPartner = isPartner;
            IsCrit = isCrit;
            IsDamageOverTime = isDamageOverTime;
        }
    }

    /// <summary>A drop appearing, with the item it carries — sent once; snapshots name it by id after.</summary>
    public readonly struct DropRecord
    {
        public readonly int NetId;
        public readonly Vector3 Position;
        public readonly ItemInstance Item;

        public DropRecord(int netId, Vector3 position, in ItemInstance item)
        {
            NetId = netId;
            Position = position;
            Item = item;
        }
    }

    /// <summary>Something that happened once, at a host step. The guest raises it when its picture
    /// reaches that step, so a hit spark lands on the frame the hit is drawn.</summary>
    public readonly struct ReplicatedEvent
    {
        public readonly ReplicatedEventKind Kind;
        public readonly int HostFrame;
        public readonly HitRecord Hit;
        public readonly DropRecord Drop;

        private ReplicatedEvent(ReplicatedEventKind kind, int hostFrame, in HitRecord hit, in DropRecord drop)
        {
            Kind = kind;
            HostFrame = hostFrame;
            Hit = hit;
            Drop = drop;
        }

        public static ReplicatedEvent OfHit(in HitRecord hit) =>
            new ReplicatedEvent(ReplicatedEventKind.Hit, 0, hit, default);

        public static ReplicatedEvent OfDrop(in DropRecord drop) =>
            new ReplicatedEvent(ReplicatedEventKind.DropSpawned, 0, default, drop);

        /// <summary>The same event stamped with the step it happened in.</summary>
        public ReplicatedEvent At(int hostFrame) => new ReplicatedEvent(Kind, hostFrame, Hit, Drop);
    }
}
```

`Assets/_BattleBomb/Core/Net/EventCodec.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Items;

namespace BattleBomb.Core.Net
{
    /// <summary>One step's (or several steps') events, on the reliable channel.</summary>
    public static class EventCodec
    {
        public static void Write(NetWriter w, IReadOnlyList<ReplicatedEvent> events)
        {
            w.WriteByte((byte)NetMessageKind.Events);
            w.WriteCount(events.Count, NetProtocol.MaxEvents);
            for (int i = 0; i < events.Count; i++)
            {
                ReplicatedEvent e = events[i];
                w.WriteByte((byte)e.Kind);
                w.WriteInt(e.HostFrame);
                switch (e.Kind)
                {
                    case ReplicatedEventKind.Hit:
                        WriteRef(w, e.Hit.Attacker);
                        WriteRef(w, e.Hit.Target);
                        w.WriteFloat(e.Hit.Damage);
                        w.WriteVector3(e.Hit.Position);
                        w.WriteBool(e.Hit.IsPartner);
                        w.WriteBool(e.Hit.IsCrit);
                        w.WriteBool(e.Hit.IsDamageOverTime);
                        break;

                    case ReplicatedEventKind.DropSpawned:
                        w.WriteInt(e.Drop.NetId);
                        w.WriteVector3(e.Drop.Position);
                        ItemWire.Write(w, e.Drop.Item);
                        break;
                }
            }
        }

        /// <summary>Reads a batch positioned after its kind byte. <paramref name="catalog"/> names items
        /// as this build knows them; null keeps the names they were sent with.</summary>
        public static void Read(NetReader r, IReadOnlyList<ItemSpec> catalog, List<ReplicatedEvent> into)
        {
            into.Clear();
            int count = r.ReadCount(NetProtocol.MaxEvents);
            for (int i = 0; i < count; i++)
            {
                var kind = (ReplicatedEventKind)r.ReadByte();
                int frame = r.ReadInt();
                switch (kind)
                {
                    case ReplicatedEventKind.Hit:
                        into.Add(ReplicatedEvent.OfHit(new HitRecord(
                            ReadRef(r), ReadRef(r), r.ReadFloat(), r.ReadVector3(),
                            r.ReadBool(), r.ReadBool(), r.ReadBool())).At(frame));
                        break;

                    case ReplicatedEventKind.DropSpawned:
                        into.Add(ReplicatedEvent.OfDrop(new DropRecord(
                            r.ReadInt(), r.ReadVector3(), ItemWire.Read(r, catalog))).At(frame));
                        break;

                    default:
                        throw new NetFormatException($"An event of kind {(byte)kind}.");
                }
            }
        }

        private static void WriteRef(NetWriter w, in EntityRef entity)
        {
            w.WriteByte((byte)entity.Kind);
            w.WriteInt(entity.Id);
        }

        private static EntityRef ReadRef(NetReader r) => new EntityRef((EntityKind)r.ReadByte(), r.ReadInt());
    }
}
```

- [ ] **Step 4: Run it** — `recompile`; `run_tests` EditMode `EventCodecTests`. Expected: pass.

- [ ] **Step 5: What each actor hands the host**

In `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs`, add `using BattleBomb.Core.Net;` and:

```csharp
        /// <summary>This player as the host simulates them, for the snapshot (D58). The derived stats
        /// never travel — only what they are derived from does, in Plan 2's inventory state.</summary>
        internal PlayerSnapshot CaptureReplica(int openScreen, int grabCount, int refusedSteps) =>
            new PlayerSnapshot(
                PlayerId.Value, _state, _combat, _condition, _revive, _mana, _leapAvailable,
                Statuses.ToArray(), openScreen, grabCount, refusedSteps);
```

In `Assets/_BattleBomb/Gameplay/Characters/EnemyActor.cs`, add `using BattleBomb.Core.Net;` and:

```csharp
        internal EnemySnapshot CaptureReplica() => new EnemySnapshot(
            NetId, OriginStage, OriginRoster, _isElite,
            _carriedDrop.IsEmpty ? -1 : (int)_carriedDrop.Quality,
            _state, _brain, _health, _targetIndex, _depletedSteps, Statuses.ToArray());
```

In `Assets/_BattleBomb/Gameplay/Characters/TrainingDummy.cs`, add `using BattleBomb.Core.Net;` and:

```csharp
        internal DummySnapshot CaptureReplica(int stageIndex) => new DummySnapshot(stageIndex, PropIndex, _state, _health);
```

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs`:

(a) Add, near `WasGrabRefused`:

```csharp
        internal int RefusedStepsFor(int playerIdValue) =>
            _refusedGrabs.TryGetValue(playerIdValue, out int steps) ? steps : 0;

        internal int AttemptStepsAllDown => _attempt.StepsAllDown;

        /// <summary>
        /// Plan 1 only (HANDOFF-M8 Task 99 retires it): until the guest's screens are online, the host
        /// declines to open a chest or shop for a player whose screen would be on another machine. The
        /// press is still the chest's, so nothing else happens either. Null lets everyone open.
        /// </summary>
        internal Func<int, bool> MayOpenScreen { get; set; }
```

(b) In `StepPlayers`, the open becomes:

```csharp
                if (result.OpenedInteractable && interactable >= 0 && interactable < _interactables.Count
                    && (MayOpenScreen == null || MayOpenScreen(playerId)))
```

- [ ] **Step 6: The host sends snapshots and events**

Replace `Assets/_BattleBomb/Gameplay/Net/NetHost.cs` with:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Loot;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The host's half of a match, in the Gameplay scene: tells the guest which run to load, feeds the
    /// guest's commands into their <see cref="RemoteCommandSource"/>, and sends the world back — a
    /// snapshot every second step and the step's events on the reliable channel (D58). Added by the
    /// binder when the machine boots as a host with a guest connected. It reads the simulation after
    /// each step and never writes to it, apart from the Plan 1 screen guard.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetHost : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(4096);
        private readonly List<WireCommand> _commands = new List<WireCommand>(NetProtocol.CommandRedundancy);
        private readonly List<ReplicatedEvent> _pending = new List<ReplicatedEvent>();
        private readonly List<ReplicatedEvent> _stamped = new List<ReplicatedEvent>();
        private readonly WorldSnapshot _snapshot = new WorldSnapshot();
        private NetSession _net;
        private GameSession _session;
        private SimulationDriver _driver;
        private StageRunner _runner;
        private RemoteCommandSource _remote;

        internal void Begin(
            NetSession net, GameSession session, SimulationDriver driver, StageRunner runner, RemoteCommandSource remote)
        {
            _net = net;
            _session = session;
            _driver = driver;
            _runner = runner;
            _remote = remote;
            _net.MessageReceived += OnMessage;
            _net.PeerLeft += OnPeerLeft;
            _driver.Stepped += OnStepped;
            _driver.HitLanded += OnHit;
            _driver.PickupSpawned += OnPickup;
            _driver.MayOpenScreen = id => id != _net.GuestPlayerId.Value;
            SendLaunch();
        }

        private void SendLaunch()
        {
            var picks = new int[_session.Characters.Length];
            for (int i = 0; i < picks.Length; i++)
            {
                picks[i] = IndexInRoster(_session.Characters[i]);
            }

            _writer.Reset();
            HandshakeCodec.WriteLaunch(_writer, new LaunchMessage(
                _session.Chapter != null ? _session.Chapter.Id : string.Empty,
                _session.StageIndex, _session.TierIndex, _session.ResumeCheckpointArena,
                picks, _net.GuestPlayerId.Value));
            _net.Send(NetChannel.Reliable, _writer);
        }

        private int IndexInRoster(CharacterDefinition definition)
        {
            if (definition == null)
            {
                return -1;
            }

            for (int i = 0; i < _session.Roster.Length; i++)
            {
                if (_session.Roster[i] == definition)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnMessage(NetMessageKind kind, NetReader reader)
        {
            if (kind != NetMessageKind.Commands || _remote == null)
            {
                return;
            }

            CommandCodec.Read(reader, _commands);
            for (int i = 0; i < _commands.Count; i++)
            {
                _remote.Stream.Receive(_commands[i]);
            }
        }

        private void OnHit(HitEvent hit) =>
            _pending.Add(ReplicatedEvent.OfHit(new HitRecord(
                RefOf(hit.Attacker), RefOf(hit.Target), hit.Damage, hit.Position,
                hit.IsPartner, hit.IsCrit, hit.IsDamageOverTime)));

        private void OnPickup(DropPickup pickup) =>
            _pending.Add(ReplicatedEvent.OfDrop(new DropRecord(pickup.NetId, pickup.Position, pickup.Item)));

        /// <summary>After every host step: this step's events, then — every second step — the world.</summary>
        private void OnStepped(int frame)
        {
            if (!_net.IsConnected)
            {
                _pending.Clear();
                return;
            }

            if (_pending.Count > 0)
            {
                _stamped.Clear();
                for (int i = 0; i < _pending.Count; i++)
                {
                    _stamped.Add(_pending[i].At(frame));
                }

                _pending.Clear();
                _writer.Reset();
                EventCodec.Write(_writer, _stamped);
                _net.Send(NetChannel.Reliable, _writer);
            }

            if (frame % NetProtocol.SnapshotEverySteps != 0)
            {
                return;
            }

            Capture(frame);
            _writer.Reset();
            SnapshotCodec.Write(_writer, _snapshot);
            _net.Send(NetChannel.Unreliable, _writer);
        }

        private void Capture(int frame)
        {
            _snapshot.Clear();
            _snapshot.HostFrame = frame;
            _snapshot.AckGuestFrame = _remote != null ? _remote.Stream.LastConsumedFrame : -1;
            _snapshot.Bounds = _driver.Bounds;
            _snapshot.AttemptStepsAllDown = _driver.AttemptStepsAllDown;

            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                int id = actors[i].PlayerId.Value;
                int open = _driver.TryGetOpenScreen(id, out InteractionKind kind) ? (int)kind : -1;
                _snapshot.Players.Add(actors[i].CaptureReplica(open, _driver.GrabCountFor(id), _driver.RefusedStepsFor(id)));
            }

            int stage = _runner != null ? _runner.StageIndex : -1;
            IReadOnlyList<ISimTarget> targets = _driver.Targets.Ordered;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is EnemyActor enemy && enemy.IsConfigured)
                {
                    _snapshot.Enemies.Add(enemy.CaptureReplica());
                }
                else if (targets[i] is TrainingDummy dummy && dummy.PropIndex >= 0)
                {
                    _snapshot.Dummies.Add(dummy.CaptureReplica(stage));
                }
            }

            _snapshot.Projectiles.AddRange(_driver.Projectiles);

            IReadOnlyList<DropPickup> pickups = _driver.Pickups;
            for (int i = 0; i < pickups.Count; i++)
            {
                if (pickups[i] != null)
                {
                    _snapshot.DropIds.Add(pickups[i].NetId);
                }
            }
        }

        private static EntityRef RefOf(Component component)
        {
            switch (component)
            {
                case CharacterActor player:
                    return EntityRef.Player(player.PlayerId.Value);
                case EnemyActor enemy:
                    return EntityRef.Enemy(enemy.NetId);
                case TrainingDummy dummy:
                    return EntityRef.Dummy(dummy.PropIndex);
                default:
                    return EntityRef.None;
            }
        }

        /// <summary>The guest is gone: their body stops taking orders rather than running on with the
        /// last stick it heard. What happens to it next is Plan 2's (D61 — the host carries on solo).</summary>
        private void OnPeerLeft()
        {
            if (_remote != null)
            {
                _remote.Stream.Release();
            }
        }

        private void OnDestroy()
        {
            if (_driver != null)
            {
                _driver.Stepped -= OnStepped;
                _driver.HitLanded -= OnHit;
                _driver.PickupSpawned -= OnPickup;
                _driver.MayOpenScreen = null;
            }

            if (_net == null)
            {
                return;
            }

            _net.MessageReceived -= OnMessage;
            _net.PeerLeft -= OnPeerLeft;

            // The machine is being torn down — results, or return to chapter select. The guest's copy
            // goes back to the front door with it and stays connected for the next launch.
            if (_net.IsConnected)
            {
                _writer.Reset();
                HandshakeCodec.WriteBare(_writer, NetMessageKind.SessionEnd);
                _net.Send(NetChannel.Reliable, _writer);
            }
        }
    }
}
```

In `Assets/_BattleBomb/Gameplay/Session/SessionBinder.cs`, `BindHost`'s last line becomes:

```csharp
            gameObject.AddComponent<NetHost>().Begin(
                net, _session, _driver, FindAnyObjectByType<World.StageRunner>(), remote);
```

- [ ] **Step 7: Extend the hosted smoke test**

In `Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs`, add `using BattleBomb.Core.Items;` and `using BattleBomb.Core.Net;`, a constant `private const int KnifeDefinitionId = 7;`, these cases, and these helpers:

```csharp
        [UnityTest]
        public IEnumerator Snapshots_carry_both_players_as_the_host_sees_them()
        {
            _guest.Move = Vector2.right;
            yield return Steps(40);
            _guest.Move = Vector2.zero;
            yield return Steps(6);

            WorldSnapshot latest = LatestSnapshot();
            Assert.That(latest, Is.Not.Null, "No snapshot reached the guest.");
            Assert.That(latest.HostFrame % NetProtocol.SnapshotEverySteps, Is.Zero);
            Assert.That(latest.Players.Count, Is.EqualTo(2));
            Assert.That(latest.TryGetPlayer(1, out PlayerSnapshot two), Is.True);
            Assert.That(Mathf.Abs(two.Motor.Position.x - _guestBody.Position.x), Is.LessThan(0.5f),
                "The snapshot's Player 2 is not where the host has them.");
            Assert.That(latest.AckGuestFrame, Is.GreaterThan(0), "The host never acknowledged a guest command.");
        }

        [UnityTest]
        public IEnumerator A_partner_shove_reaches_the_guest_as_a_hit_between_players()
        {
            float side = _host.Position.x <= _guestBody.Position.x ? -1f : 1f;
            yield return WalkHostTo(_guestBody.Position + new Vector3(side * 1.4f, 0f, 0f));

            // Face the partner before swinging: the walk may have ended facing away.
            _hostInput.Set(new Vector2(-side * 0.2f, 0f), CommandButtons.None);
            yield return Steps(2);
            _hostInput.Set(Vector2.zero, CommandButtons.Heavy);
            yield return Steps(3);
            _hostInput.Release();
            yield return Steps(40);

            bool found = false;
            foreach (ReplicatedEvent e in AllEvents())
            {
                found |= e.Kind == ReplicatedEventKind.Hit && e.Hit.IsPartner
                    && e.Hit.Attacker.Equals(EntityRef.Player(0)) && e.Hit.Target.Equals(EntityRef.Player(1));
            }

            Assert.That(found, Is.True, "The host's swing shoved Player 2 and the guest never heard about it.");
        }

        [UnityTest]
        public IEnumerator A_drop_reaches_the_guest_with_its_item_and_is_named_after()
        {
            ItemInstance knife = _driver.RollDebugItem(KnifeDefinitionId, 1f);
            _driver.SpawnDebugDrop(_host.Position + new Vector3(3f, 0f, 0f), knife);
            yield return Steps(6);

            ReplicatedEvent drop = default;
            foreach (ReplicatedEvent e in AllEvents())
            {
                if (e.Kind == ReplicatedEventKind.DropSpawned)
                {
                    drop = e;
                }
            }

            Assert.That(drop.Kind, Is.EqualTo(ReplicatedEventKind.DropSpawned), "The drop never reached the guest.");
            Assert.That(drop.Drop.Item.DefinitionId, Is.EqualTo(KnifeDefinitionId));
            Assert.That(drop.Drop.NetId, Is.GreaterThan(0));
            Assert.That(LatestSnapshot().DropIds, Does.Contain(drop.Drop.NetId),
                "Snapshots do not name the drop the guest was told about.");
        }

        private WorldSnapshot LatestSnapshot()
        {
            for (int i = _guest.Received.Count - 1; i >= 0; i--)
            {
                var reader = new NetReader(_guest.Received[i]);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Snapshot)
                {
                    var snapshot = new WorldSnapshot();
                    SnapshotCodec.Read(reader, snapshot);
                    return snapshot;
                }
            }

            return null;
        }

        private List<ReplicatedEvent> AllEvents()
        {
            var all = new List<ReplicatedEvent>();
            var batch = new List<ReplicatedEvent>();
            foreach (byte[] message in _guest.Received)
            {
                var reader = new NetReader(message);
                if ((NetMessageKind)reader.ReadByte() == NetMessageKind.Events)
                {
                    EventCodec.Read(reader, _driver.ItemSpecs, batch);
                    all.AddRange(batch);
                }
            }

            return all;
        }

        private IEnumerator WalkHostTo(Vector3 target)
        {
            int deadline = _driver.Frame + PatienceSteps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                Vector3 to = target - _host.Position;
                to.y = 0f;
                if (to.magnitude <= 0.3f)
                {
                    _hostInput.Release();
                    yield return Steps(2);
                    yield break;
                }

                _hostInput.Set(new Vector2(Mathf.Clamp(to.x, -1f, 1f), Mathf.Clamp(to.z, -1f, 1f)), CommandButtons.None);
                yield return null;
            }

            _hostInput.Release();
            Assert.Fail($"The host never reached {target} — stopped {Vector3.Distance(target, _host.Position):F2} away.");
        }
```

If `RollDebugItem` was put behind `#if UNITY_EDITOR || DEVELOPMENT_BUILD` by the Builder's batch, this still compiles in the editor, where tests run.

- [ ] **Step 8: Run both suites.** `recompile`; EditMode green; PlayMode `OnlineHostSmokeTests` — all eight pass; then the full PlayMode suite. Delete `Assets/InitTestScene*`.

- [ ] **Step 9: Commit** — subject `92: the host speaks — snapshots and events`. Body: 30 Hz unreliable snapshots, reliable events stamped with their step, hits as entity references, drops sent once with their item; the Plan 1 guard that keeps a guest's screen from opening where they cannot see it.

---
### Task 93: Replica mode

The guest draws the host's world. Snapshots land in a buffer; a render clock runs a few steps behind the newest one and eases itself to hold that distance; every local step the guest's actors are set to the state interpolated between the two snapshots around it — the same actors every Presentation and UI observer already reads (planning decisions 6 and 7). Events are raised when the picture reaches the step they happened in.

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/SnapshotBuffer.cs`, `Assets/_BattleBomb/Core/Net/RenderClock.cs`
- Create: `Assets/_BattleBomb/Gameplay/Net/ReplicaWorld.cs`
- Modify: `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs`, `EnemyActor.cs`, `TrainingDummy.cs` (`ApplyReplica`)
- Modify: `Assets/_BattleBomb/Gameplay/Combat/EnemySpawner.cs` (replica spawn and despawn)
- Modify: `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (replica setters)
- Modify: `Assets/_BattleBomb/Gameplay/World/StageRunner.cs` (`ReplicaDummy`)
- Modify: `Assets/_BattleBomb/Gameplay/Net/NetSession.cs` (hold messages across the scene load), `NetGuest.cs`
- Test: `Assets/_BattleBomb/Tests/EditMode/Net/SnapshotBufferTests.cs`, `RenderClockTests.cs`

- [ ] **Step 1: The failing timeline tests**

`Assets/_BattleBomb/Tests/EditMode/Net/SnapshotBufferTests.cs`:

```csharp
using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class SnapshotBufferTests
    {
        [Test]
        public void Snapshots_are_kept_in_frame_order_and_once()
        {
            var buffer = new SnapshotBuffer();
            Assert.That(buffer.Add(At(buffer, 20)), Is.True);
            Assert.That(buffer.Add(At(buffer, 10)), Is.True);
            Assert.That(buffer.Add(At(buffer, 20)), Is.False, "A duplicate was kept.");

            Assert.That(buffer.Count, Is.EqualTo(2));
            Assert.That(buffer.NewestFrame, Is.EqualTo(20));
        }

        [Test]
        public void Sampling_between_two_snapshots_gives_the_pair_and_how_far_along()
        {
            var buffer = new SnapshotBuffer();
            buffer.Add(At(buffer, 10));
            buffer.Add(At(buffer, 12));
            buffer.Add(At(buffer, 14));

            Assert.That(buffer.TrySample(11f, out WorldSnapshot from, out WorldSnapshot to, out float t), Is.True);
            Assert.That(from.HostFrame, Is.EqualTo(10));
            Assert.That(to.HostFrame, Is.EqualTo(12));
            Assert.That(t, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Sampling_outside_the_buffer_holds_its_edge_rather_than_guessing()
        {
            var buffer = new SnapshotBuffer();
            buffer.Add(At(buffer, 10));
            buffer.Add(At(buffer, 12));

            buffer.TrySample(4f, out WorldSnapshot from, out WorldSnapshot to, out float t);
            Assert.That(from.HostFrame, Is.EqualTo(10));
            Assert.That(to.HostFrame, Is.EqualTo(10));

            buffer.TrySample(30f, out from, out to, out t);
            Assert.That(from.HostFrame, Is.EqualTo(12));
            Assert.That(to.HostFrame, Is.EqualTo(12));
            Assert.That(t, Is.Zero);
        }

        [Test]
        public void Discarding_keeps_the_snapshot_the_picture_starts_from_and_recycles_the_rest()
        {
            var buffer = new SnapshotBuffer();
            WorldSnapshot first = At(buffer, 10);
            buffer.Add(first);
            buffer.Add(At(buffer, 12));
            buffer.Add(At(buffer, 14));

            buffer.DiscardBefore(13f);

            Assert.That(buffer.Count, Is.EqualTo(2));
            buffer.TrySample(13f, out WorldSnapshot from, out _, out _);
            Assert.That(from.HostFrame, Is.EqualTo(12));
            Assert.That(buffer.Rent(), Is.SameAs(first), "A discarded snapshot was not recycled.");
        }

        [Test]
        public void The_buffer_never_grows_past_its_capacity()
        {
            var buffer = new SnapshotBuffer();
            for (int frame = 0; frame < SnapshotBuffer.Capacity * 3; frame += 2)
            {
                buffer.Add(At(buffer, frame));
            }

            Assert.That(buffer.Count, Is.EqualTo(SnapshotBuffer.Capacity));
        }

        private static WorldSnapshot At(SnapshotBuffer buffer, int frame)
        {
            WorldSnapshot snapshot = buffer.Rent();
            snapshot.HostFrame = frame;
            return snapshot;
        }
    }
}
```

`Assets/_BattleBomb/Tests/EditMode/Net/RenderClockTests.cs`:

```csharp
using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class RenderClockTests
    {
        [Test]
        public void The_first_advance_starts_the_delay_behind_the_newest()
        {
            var clock = new RenderClock(delaySteps: 6);
            Assert.That(clock.IsRunning, Is.False);
            Assert.That(clock.Advance(100), Is.EqualTo(94f));
        }

        [Test]
        public void In_step_with_the_host_it_advances_one_step_per_step()
        {
            var clock = new RenderClock(delaySteps: 6);
            clock.Advance(100);
            for (int newest = 101; newest < 200; newest++)
            {
                clock.Advance(newest);
            }

            Assert.That(clock.Frame, Is.EqualTo(193f).Within(0.01f));
        }

        [Test]
        public void Behind_it_hurries_and_ahead_it_waits_but_never_by_more_than_a_quarter_step()
        {
            var behind = new RenderClock(delaySteps: 6);
            behind.Advance(100);
            float before = behind.Frame;
            behind.Advance(120);
            float stride = behind.Frame - before;
            Assert.That(stride, Is.GreaterThan(1f));
            Assert.That(stride, Is.LessThanOrEqualTo(1.25f + 1e-4f));

            var ahead = new RenderClock(delaySteps: 6);
            ahead.Advance(100);
            before = ahead.Frame;
            ahead.Advance(90);
            stride = ahead.Frame - before;
            Assert.That(stride, Is.LessThan(1f));
            Assert.That(stride, Is.GreaterThanOrEqualTo(0.75f - 1e-4f));
        }

        [Test]
        public void It_never_draws_past_the_newest_snapshot()
        {
            var clock = new RenderClock(delaySteps: 0);
            clock.Advance(50);
            for (int i = 0; i < 20; i++)
            {
                clock.Advance(50);
            }

            Assert.That(clock.Frame, Is.LessThanOrEqualTo(50f));
        }

        [Test]
        public void A_long_stall_snaps_rather_than_crawling_to_catch_up()
        {
            var clock = new RenderClock(delaySteps: 6);
            clock.Advance(100);
            Assert.That(clock.Advance(400), Is.EqualTo(394f));
        }
    }
}
```

- [ ] **Step 2: Run them to see them fail** — `recompile`. Expected: compile errors.

- [ ] **Step 3: The buffer and the clock**

`Assets/_BattleBomb/Core/Net/SnapshotBuffer.cs`:

```csharp
using System.Collections.Generic;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The guest's recent snapshots in host-frame order, with a pool so 30 a second allocate nothing
    /// once warm. Sampling finds the pair around a (fractional) render frame; outside the buffer it
    /// holds the nearest edge rather than extrapolating a guess.
    /// </summary>
    public sealed class SnapshotBuffer
    {
        /// <summary>About a second of snapshots — far more than the interpolation delay needs.</summary>
        public const int Capacity = 32;

        private readonly List<WorldSnapshot> _snapshots = new List<WorldSnapshot>(Capacity);
        private readonly Stack<WorldSnapshot> _pool = new Stack<WorldSnapshot>();

        public int Count => _snapshots.Count;

        public int NewestFrame => _snapshots.Count > 0 ? _snapshots[_snapshots.Count - 1].HostFrame : -1;

        public WorldSnapshot Rent() => _pool.Count > 0 ? _pool.Pop() : new WorldSnapshot();

        /// <summary>Keeps it in order. A duplicate, or one older than a full buffer's oldest, goes back
        /// to the pool and false is returned.</summary>
        public bool Add(WorldSnapshot snapshot)
        {
            int index = _snapshots.Count;
            while (index > 0 && _snapshots[index - 1].HostFrame >= snapshot.HostFrame)
            {
                if (_snapshots[index - 1].HostFrame == snapshot.HostFrame)
                {
                    Recycle(snapshot);
                    return false;
                }

                index--;
            }

            if (index == 0 && _snapshots.Count >= Capacity)
            {
                Recycle(snapshot);
                return false;
            }

            _snapshots.Insert(index, snapshot);
            while (_snapshots.Count > Capacity)
            {
                Recycle(_snapshots[0]);
                _snapshots.RemoveAt(0);
            }

            return true;
        }

        public bool TrySample(float frame, out WorldSnapshot from, out WorldSnapshot to, out float t)
        {
            from = null;
            to = null;
            t = 0f;
            if (_snapshots.Count == 0)
            {
                return false;
            }

            if (frame <= _snapshots[0].HostFrame)
            {
                from = _snapshots[0];
                to = from;
                return true;
            }

            for (int i = _snapshots.Count - 1; i >= 0; i--)
            {
                if (_snapshots[i].HostFrame > frame)
                {
                    continue;
                }

                from = _snapshots[i];
                if (i + 1 < _snapshots.Count)
                {
                    to = _snapshots[i + 1];
                    t = (frame - from.HostFrame) / (to.HostFrame - from.HostFrame);
                }
                else
                {
                    to = from;
                }

                return true;
            }

            return false;
        }

        /// <summary>Recycles every snapshot older than the one the picture now starts from.</summary>
        public void DiscardBefore(float frame)
        {
            while (_snapshots.Count > 1 && _snapshots[1].HostFrame <= frame)
            {
                Recycle(_snapshots[0]);
                _snapshots.RemoveAt(0);
            }
        }

        private void Recycle(WorldSnapshot snapshot)
        {
            snapshot.Clear();
            _pool.Push(snapshot);
        }
    }
}
```

`Assets/_BattleBomb/Core/Net/RenderClock.cs`:

```csharp
using System;
using UnityEngine;

namespace BattleBomb.Core.Net
{
    /// <summary>
    /// The host frame the guest draws: <c>delay</c> steps behind the newest snapshot (HANDOFF-M8 paper
    /// numbers). It advances one step per local step and eases the gap back toward the delay by at
    /// most a quarter step per step — invisible — so jitter never shows as a stutter; a long stall
    /// snaps instead of crawling. It never draws past the newest snapshot.
    /// </summary>
    public sealed class RenderClock
    {
        public const float SnapAfterSteps = 30f;
        public const float CorrectionRate = 0.05f;
        public const float MaxCorrection = 0.25f;

        private readonly int _delay;

        public RenderClock(int delaySteps = NetProtocol.InterpolationDelaySteps)
        {
            _delay = Math.Max(0, delaySteps);
            Frame = -1f;
        }

        public float Frame { get; private set; }

        public bool IsRunning => Frame >= 0f;

        public float Advance(int newestHostFrame)
        {
            float target = newestHostFrame - _delay;
            if (!IsRunning || Mathf.Abs(target - Frame) > SnapAfterSteps)
            {
                Frame = Mathf.Max(0f, target);
                return Frame;
            }

            // One step forward first, then the correction measured from where that lands — measured
            // before the step, an in-step clock would read a one-step gap every step and creep ahead.
            Frame += 1f;
            Frame += Mathf.Clamp((target - Frame) * CorrectionRate, -MaxCorrection, MaxCorrection);
            if (Frame > newestHostFrame)
            {
                Frame = newestHostFrame;
            }

            return Frame;
        }

        public void Reset() => Frame = -1f;
    }
}
```

- [ ] **Step 4: Run them** — `recompile`; EditMode `SnapshotBufferTests`, `RenderClockTests`. Expected: pass.

- [ ] **Step 5: Actors take state from the wire**

In `Assets/_BattleBomb/Gameplay/Characters/CharacterActor.cs`:

```csharp
        /// <summary>
        /// The guest's copy of this player, set to what the host simulated (D58, HANDOFF-M8 planning
        /// decision 7). <c>_previous</c> takes the last applied state, so <c>InterpolatedVisual</c>'s
        /// per-step lerp is exactly as smooth as it is on the host. Never called on a host.
        /// </summary>
        internal void ApplyReplica(in PlayerSnapshot snapshot)
        {
            _previous = _state;
            _state = snapshot.Motor;
            _combat = snapshot.Combat;
            _condition = snapshot.Condition;
            _revive = snapshot.Revive;
            _mana = snapshot.Mana;
            _leapAvailable = snapshot.LeapAvailable;
            Statuses.Restore(snapshot.Statuses);
            transform.position = _state.Position;
        }
```

In `Assets/_BattleBomb/Gameplay/Characters/EnemyActor.cs`:

```csharp
        /// <summary>The guest's copy of this enemy, set to what the host simulated.</summary>
        internal void ApplyReplica(in EnemySnapshot snapshot)
        {
            _previous = _state;
            _state = snapshot.Motor;
            _brain = snapshot.Brain;
            _health = snapshot.Health;
            _targetIndex = snapshot.TargetIndex;
            _depletedSteps = snapshot.DepletedSteps;
            Statuses.Restore(snapshot.Statuses);
            transform.position = _state.Position;
        }
```

In `Assets/_BattleBomb/Gameplay/Characters/TrainingDummy.cs`:

```csharp
        internal void ApplyReplica(in DummySnapshot snapshot)
        {
            _previous = _state;
            _state = snapshot.Motor;
            _health = snapshot.Health;
            transform.position = _state.Position;
        }
```

In `Assets/_BattleBomb/Gameplay/Combat/EnemySpawner.cs`, add `using BattleBomb.Core.Items;` and `using BattleBomb.Core.Net;`, and:

```csharp
        /// <summary>
        /// The guest's copy of an enemy the host spawned: the same prefab and archetype, configured at
        /// face value — health, tier and elite toughness all arrive in every snapshot, so none of them
        /// is recomputed here. An elite wears a stand-in carrying only its piece's quality, which is
        /// all its tint reads; the real piece drops from the host.
        /// </summary>
        internal EnemyActor ReplicaSpawn(EnemyDefinition definition, in EnemySnapshot snapshot)
        {
            if (_enemyPrefab == null || definition == null)
            {
                return null;
            }

            GameObject go = Instantiate(_enemyPrefab, snapshot.Motor.Position, Quaternion.identity, transform);
            go.name = $"{definition.name} {snapshot.NetId:000}";
            EnemyActor actor = go.GetComponent<EnemyActor>();
            if (actor == null)
            {
                Destroy(go);
                return null;
            }

            ItemInstance carried = snapshot.CarriedQuality >= 0
                ? ItemWire.StandIn((QualityRank)snapshot.CarriedQuality)
                : default;
            actor.Configure(definition, 0, snapshot.IsElite, carried, 1f, 1f);
            actor.SetOrigin(snapshot.NetId, snapshot.StageIndex, snapshot.RosterIndex);
            _brood.Add(go);
            return actor;
        }

        /// <summary>Gone from the host's world: gone from the guest's, at once — deactivated first so it
        /// leaves every registry now, not at the end of the frame.</summary>
        internal void ReplicaDespawn(EnemyActor actor)
        {
            if (actor == null)
            {
                return;
            }

            _brood.Remove(actor.gameObject);
            actor.gameObject.SetActive(false);
            Destroy(actor.gameObject);
        }
```

- [ ] **Step 6: The driver's replica setters**

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs`, add (all `internal`, all no-ops on a host because nothing on a host calls them):

```csharp
        // ── Replica (the guest's driver, D58): state set from the host's snapshots ──────────

        internal void ApplyReplicaPlayerSide(int playerIdValue, int openScreen, int grabCount, int refusedSteps)
        {
            if (openScreen >= 0)
            {
                _openScreens[playerIdValue] = (InteractionKind)openScreen;
            }
            else
            {
                _openScreens.Remove(playerIdValue);
            }

            _grabCounts[playerIdValue] = grabCount;
            _refusedGrabs[playerIdValue] = refusedSteps;
        }

        internal void ApplyReplicaWorld(in ArenaBounds bounds, int attemptStepsAllDown)
        {
            SetArena(bounds);
            _attempt = new AttemptCountdown(attemptStepsAllDown);
        }

        /// <summary>Bolts are drawn from the snapshot nearest the picture, carried forward along their
        /// own velocity to the render frame — they have no ids to interpolate between.</summary>
        internal void ApplyReplicaProjectiles(IReadOnlyList<ProjectileState> projectiles, float secondsAhead)
        {
            _projectiles.Clear();
            for (int i = 0; i < projectiles.Count; i++)
            {
                ProjectileState p = projectiles[i];
                _projectiles.Add(new ProjectileState(
                    p.Position + p.Velocity * secondsAhead, p.Velocity, p.Damage, p.Element, p.LifeSteps, p.OwnerPlayerId));
            }
        }

        internal void ApplyReplicaPickup(int netId, Vector3 position, in ItemInstance item)
        {
            for (int i = 0; i < _pickups.Count; i++)
            {
                if (_pickups[i] != null && _pickups[i].NetId == netId)
                {
                    return;
                }
            }

            _pickups.Add(DropPickup.Spawn(position, item, netId));
        }

        internal void RemoveReplicaPickup(int netId)
        {
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                if (_pickups[i] == null || _pickups[i].NetId != netId)
                {
                    continue;
                }

                _pickups[i].gameObject.SetActive(false);
                Destroy(_pickups[i].gameObject);
                _pickups.RemoveAt(i);
            }
        }

        internal void RaiseReplicatedHit(in HitEvent hit) => HitLanded?.Invoke(hit);
```

In `Assets/_BattleBomb/Gameplay/World/StageRunner.cs`:

```csharp
        /// <summary>The guest's dummy for a host dummy — only if it stands in the stage the guest has
        /// under its feet right now, so a snapshot from across an airlock cannot move the wrong one.</summary>
        internal TrainingDummy ReplicaDummy(int stageIndex, int propIndex) =>
            _current != null && _current.StageIndex == stageIndex ? _current.DummyAt(propIndex) : null;
```

- [ ] **Step 7: The replica world**

`Assets/_BattleBomb/Gameplay/Net/ReplicaWorld.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Movement;
using BattleBomb.Core.Net;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Combat;
using BattleBomb.Gameplay.Data;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// Sets the guest's world to the host's, one local step at a time: positions interpolated between
    /// the two snapshots around the render frame, everything discrete from the nearer of them. Enemies
    /// and drops appear and vanish as the host's do. A teleport — a respawn, an airlock — is shown as
    /// a teleport, never as a slide across the arena.
    /// </summary>
    internal sealed class ReplicaWorld
    {
        private const float TeleportDistanceSq = 3f * 3f;

        private readonly SimulationDriver _driver;
        private readonly StageRunner _runner;
        private readonly EnemySpawner _spawner;
        private readonly GameSession _session;
        private readonly Dictionary<int, EnemyActor> _enemies = new Dictionary<int, EnemyActor>();
        private readonly Dictionary<int, int> _dropBorn = new Dictionary<int, int>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();

        internal ReplicaWorld(SimulationDriver driver, StageRunner runner, EnemySpawner spawner, GameSession session)
        {
            _driver = driver;
            _runner = runner;
            _spawner = spawner;
            _session = session;
        }

        internal void Apply(WorldSnapshot from, WorldSnapshot to, float t, float renderFrame)
        {
            WorldSnapshot near = t < 0.5f ? from : to;
            ApplyPlayers(from, to, t, near);
            ApplyEnemies(from, to, t, near);
            ApplyDummies(from, to, t, near);
            ApplyDrops(near);
            _driver.ApplyReplicaProjectiles(near.Projectiles, (renderFrame - near.HostFrame) * _driver.StepDuration);
            _driver.ApplyReplicaWorld(near.Bounds, near.AttemptStepsAllDown);
        }

        /// <summary>A drop the host announced, placed when the picture reaches the step it appeared in.</summary>
        internal void SpawnDrop(in DropRecord drop, int hostFrame)
        {
            _driver.ApplyReplicaPickup(drop.NetId, drop.Position, drop.Item);
            _dropBorn[drop.NetId] = hostFrame;
        }

        internal EnemyActor EnemyByNetId(int netId) =>
            _enemies.TryGetValue(netId, out EnemyActor enemy) && enemy != null ? enemy : null;

        internal CharacterActor PlayerById(int playerId)
        {
            IReadOnlyList<CharacterActor> actors = _driver.Characters.Ordered;
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i].PlayerId.Value == playerId)
                {
                    return actors[i];
                }
            }

            return null;
        }

        private void ApplyPlayers(WorldSnapshot from, WorldSnapshot to, float t, WorldSnapshot near)
        {
            for (int i = 0; i < near.Players.Count; i++)
            {
                PlayerSnapshot player = near.Players[i];
                CharacterActor actor = PlayerById(player.PlayerId);
                if (actor == null)
                {
                    continue;
                }

                MotorState motor = player.Motor;
                if (from.TryGetPlayer(player.PlayerId, out PlayerSnapshot a) && to.TryGetPlayer(player.PlayerId, out PlayerSnapshot b))
                {
                    motor = Blend(a.Motor, b.Motor, t);
                }

                actor.ApplyReplica(player.WithMotor(motor));
                _driver.ApplyReplicaPlayerSide(player.PlayerId, player.OpenScreen, player.GrabCount, player.RefusedSteps);
            }
        }

        private void ApplyEnemies(WorldSnapshot from, WorldSnapshot to, float t, WorldSnapshot near)
        {
            _seen.Clear();
            for (int i = 0; i < near.Enemies.Count; i++)
            {
                EnemySnapshot enemy = near.Enemies[i];
                _seen.Add(enemy.NetId);

                EnemyActor actor = EnemyByNetId(enemy.NetId);
                if (actor == null)
                {
                    actor = _spawner != null ? _spawner.ReplicaSpawn(DefinitionOf(enemy), enemy) : null;
                    if (actor == null)
                    {
                        continue;
                    }

                    _enemies[enemy.NetId] = actor;
                }

                MotorState motor = enemy.Motor;
                if (from.TryGetEnemy(enemy.NetId, out EnemySnapshot a) && to.TryGetEnemy(enemy.NetId, out EnemySnapshot b))
                {
                    motor = Blend(a.Motor, b.Motor, t);
                }

                actor.ApplyReplica(enemy.WithMotor(motor));
            }

            _gone.Clear();
            foreach (KeyValuePair<int, EnemyActor> entry in _enemies)
            {
                if (!_seen.Contains(entry.Key))
                {
                    _gone.Add(entry.Key);
                }
            }

            for (int i = 0; i < _gone.Count; i++)
            {
                if (_spawner != null)
                {
                    _spawner.ReplicaDespawn(_enemies[_gone[i]]);
                }

                _enemies.Remove(_gone[i]);
            }
        }

        private void ApplyDummies(WorldSnapshot from, WorldSnapshot to, float t, WorldSnapshot near)
        {
            if (_runner == null)
            {
                return;
            }

            for (int i = 0; i < near.Dummies.Count; i++)
            {
                DummySnapshot dummy = near.Dummies[i];
                TrainingDummy target = _runner.ReplicaDummy(dummy.StageIndex, dummy.PropIndex);
                if (target == null)
                {
                    continue;
                }

                MotorState motor = dummy.Motor;
                if (from.TryGetDummy(dummy.StageIndex, dummy.PropIndex, out DummySnapshot a)
                    && to.TryGetDummy(dummy.StageIndex, dummy.PropIndex, out DummySnapshot b))
                {
                    motor = Blend(a.Motor, b.Motor, t);
                }

                target.ApplyReplica(new DummySnapshot(dummy.StageIndex, dummy.PropIndex, motor, dummy.Health));
            }
        }

        /// <summary>
        /// A drop leaves when the picture's snapshot no longer names it — but only one born before that
        /// snapshot. Snapshots come every second step and events every step, so a drop born on an odd
        /// step is legitimately missing from the snapshot just before it.
        /// </summary>
        private void ApplyDrops(WorldSnapshot near)
        {
            _gone.Clear();
            foreach (KeyValuePair<int, int> entry in _dropBorn)
            {
                if (entry.Value < near.HostFrame && !near.DropIds.Contains(entry.Key))
                {
                    _gone.Add(entry.Key);
                }
            }

            for (int i = 0; i < _gone.Count; i++)
            {
                _driver.RemoveReplicaPickup(_gone[i]);
                _dropBorn.Remove(_gone[i]);
            }
        }

        private EnemyDefinition DefinitionOf(in EnemySnapshot enemy)
        {
            ChapterDefinition chapter = _session != null ? _session.Chapter : null;
            StageDefinition stage = chapter != null ? chapter.StageAt(enemy.StageIndex) : null;
            return stage != null ? stage.EnemyAt(enemy.RosterIndex) : null;
        }

        private static MotorState Blend(in MotorState a, in MotorState b, float t)
        {
            MotorState near = t < 0.5f ? a : b;
            if ((b.Position - a.Position).sqrMagnitude > TeleportDistanceSq)
            {
                return near;
            }

            return new MotorState(
                Vector3.Lerp(a.Position, b.Position, t), Vector3.Lerp(a.Velocity, b.Velocity, t),
                near.Facing, near.IsGrounded, near.StepsSinceGrounded, near.JumpBufferedFor);
        }
    }
}
```

- [ ] **Step 8: The session holds messages across the scene load**

Between the guest's `Launch` and its Gameplay scene waking up, the host keeps talking — a `LoadStage` and snapshots arrive while nothing is listening. In `Assets/_BattleBomb/Gameplay/Net/NetSession.cs`:

(a) Add fields:

```csharp
        private readonly System.Collections.Generic.List<byte[]> _held = new System.Collections.Generic.List<byte[]>();
        private bool _holding;
```

(b) In `Dispatch`, replace the final `if (_welcomed) { MessageReceived?.Invoke(kind, reader); }` with:

```csharp
                if (!_welcomed)
                {
                    return;
                }

                if (_holding)
                {
                    _held.Add(payload);
                    return;
                }

                MessageReceived?.Invoke(kind, reader);
```

(c) In `FollowLaunch`, directly before `SceneManager.LoadScene(...)`:

```csharp
            _held.Clear();
            _holding = true;
```

(d) In `ReturnToFrontend`, at the top:

```csharp
            _held.Clear();
            _holding = false;
```

(e) Add:

```csharp
        /// <summary>
        /// Plays back what arrived while the guest's machine was loading. Called by
        /// <see cref="NetGuest"/> in <c>Start</c> — after every object in the new scene has enabled, so
        /// the stage runner a <c>LoadStage</c> reaches is ready to take it.
        /// </summary>
        public void ReleaseHeld()
        {
            _holding = false;
            if (_held.Count == 0)
            {
                return;
            }

            byte[][] held = _held.ToArray();
            _held.Clear();
            for (int i = 0; i < held.Length; i++)
            {
                Dispatch(held[i]);
            }
        }
```

- [ ] **Step 9: The guest draws**

Replace `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs` with:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Core.Players;
using BattleBomb.Gameplay.Combat;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform.Net;
using UnityEngine;

namespace BattleBomb.Gameplay.Net
{
    /// <summary>
    /// The guest's half of a match, in the Gameplay scene: every local step it sends this machine's
    /// newest commands to the host (with redundancy), advances the render clock, sets the world to the
    /// host's snapshots around it, and raises the host's events when the picture reaches them (D58).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetGuest : MonoBehaviour
    {
        private readonly NetWriter _writer = new NetWriter(256);
        private readonly List<WireCommand> _recent = new List<WireCommand>(NetProtocol.CommandRedundancy + 1);
        private readonly SnapshotBuffer _buffer = new SnapshotBuffer();
        private readonly RenderClock _clock = new RenderClock();
        private readonly List<ReplicatedEvent> _incoming = new List<ReplicatedEvent>();
        private readonly List<ReplicatedEvent> _pending = new List<ReplicatedEvent>();
        private NetSession _net;
        private SimulationDriver _driver;
        private StageRunner _runner;
        private ReplicaWorld _world;
        private PlayerId _local;

        /// <summary>The host frame the picture is showing right now, or -1 before the first snapshot.</summary>
        public float RenderFrame => _clock.Frame;

        public int NewestHostFrame => _buffer.NewestFrame;

        internal void Begin(NetSession net, SimulationDriver driver, PlayerId local)
        {
            _net = net;
            _driver = driver;
            _local = local;
            _runner = FindAnyObjectByType<StageRunner>();
            _world = new ReplicaWorld(_driver, _runner, FindAnyObjectByType<EnemySpawner>(), GameSession.Find());
            _driver.ReplicaStepping += OnLocalStep;
            _net.MessageReceived += OnMessage;
        }

        private void Start() => _net.ReleaseHeld();

        private void OnLocalStep(int frame)
        {
            PlayerCommand command = CommandCodec.Quantized(_driver.CommandFor(_local.Value));
            _recent.Add(WireCommand.From(command));
            if (_recent.Count > NetProtocol.CommandRedundancy)
            {
                _recent.RemoveAt(0);
            }

            _writer.Reset();
            CommandCodec.Write(_writer, Mathf.Max(0, _buffer.NewestFrame), _recent);
            _net.Send(NetChannel.Unreliable, _writer);

            if (_buffer.Count == 0)
            {
                return;
            }

            float render = _clock.Advance(_buffer.NewestFrame);
            if (_buffer.TrySample(render, out WorldSnapshot from, out WorldSnapshot to, out float t))
            {
                _world.Apply(from, to, t, render);
            }

            RaiseDue(render);
            _buffer.DiscardBefore(render);
        }

        private void OnMessage(NetMessageKind kind, NetReader reader)
        {
            switch (kind)
            {
                case NetMessageKind.Snapshot:
                    WorldSnapshot snapshot = _buffer.Rent();
                    SnapshotCodec.Read(reader, snapshot);
                    _buffer.Add(snapshot);
                    break;

                case NetMessageKind.Events:
                    EventCodec.Read(reader, _driver.ItemSpecs, _incoming);
                    _pending.AddRange(_incoming);
                    break;
            }
        }

        /// <summary>Raises every event whose step the picture has reached, in the order they happened.</summary>
        private void RaiseDue(float render)
        {
            int due = 0;
            while (due < _pending.Count && _pending[due].HostFrame <= render)
            {
                ReplicatedEvent e = _pending[due];
                if (e.Kind == ReplicatedEventKind.Hit)
                {
                    Component target = Resolve(e.Hit.Target);
                    if (target != null)
                    {
                        _driver.RaiseReplicatedHit(new HitEvent(
                            Resolve(e.Hit.Attacker), target, e.Hit.Damage, e.Hit.Position,
                            e.Hit.IsPartner, e.Hit.IsCrit, e.Hit.IsDamageOverTime));
                    }
                }
                else if (e.Kind == ReplicatedEventKind.DropSpawned)
                {
                    _world.SpawnDrop(e.Drop, e.HostFrame);
                }

                due++;
            }

            _pending.RemoveRange(0, due);
        }

        private Component Resolve(in EntityRef entity)
        {
            switch (entity.Kind)
            {
                case EntityKind.Player:
                    return _world.PlayerById(entity.Id);
                case EntityKind.Enemy:
                    return _world.EnemyByNetId(entity.Id);
                case EntityKind.Dummy:
                    return _runner != null ? _runner.ReplicaDummy(_runner.StageIndex, entity.Id) : null;
                default:
                    return null;
            }
        }

        private void OnDestroy()
        {
            if (_driver != null)
            {
                _driver.ReplicaStepping -= OnLocalStep;
            }

            if (_net != null)
            {
                _net.MessageReceived -= OnMessage;
            }
        }
    }
}
```

The guest's `Commands` now acknowledge the newest snapshot it holds (Plan 3's prediction replays from the host's `AckGuestFrame`, which is the other direction; this acknowledgement is for diagnostics and delta compression later).

- [ ] **Step 10: Recompile; run both suites.** Expected: green. The replica path is exercised end to end by Task 95's replay test; until then, the next step is the check.

- [ ] **Step 11: Live check (settled state — drive it yourself, `QUIET ON`)**

Two editors as in Task 90 (Host local / Join local, launch from the main window). With `eval` in the **Player 2** editor, read every `CharacterActor`'s `PlayerId` and `Position` and every `EnemyActor`'s `NetId` and `Position` (use `UnityEngine.Object.FindObjectsByType`); in the main editor, read the same. Expected: the same players, and the same enemy ids, at positions within about half a unit (the guest draws 100 ms behind). Walk Player 1 in the main window and re-read: Player 1 has moved in both. Stages do not stream on the guest yet (Task 94) — expect the characters standing in empty space there. `QUIET OFF`.

- [ ] **Step 12: Commit** — subject `93: replica mode — the guest draws the host's world`. Body: snapshots buffered and drawn 100 ms behind with an eased render clock; interpolated positions, discrete state from the nearer snapshot, teleports shown as teleports; enemies and drops appear and vanish with the host's; events raised when the picture reaches them; messages held across the guest's scene load.

---

### Task 94: The stage follows

The host streams stages; the guest streams the same ones at the same offsets. The host holds its first step until the guest has the launch stage loaded, and the airlock waits until the guest has the next stage loaded — so nobody ever walks into empty space on either screen (Michael's call, design §1; HANDOFF-M8 planning decision 10).

**Files:**
- Create: `Assets/_BattleBomb/Core/Net/StageCodec.cs`
- Modify: `Assets/_BattleBomb/Gameplay/World/StageRunner.cs` (hooks, the gate, replica following)
- Modify: `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs` (`HoldForPeer`)
- Modify: `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`, `NetGuest.cs`
- Modify: `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs` (answers `LoadStage`)
- Test: `Assets/_BattleBomb/Tests/EditMode/Net/StageCodecTests.cs`; extend `Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs` (and a second fixture in the same file)

- [ ] **Step 1: The failing codec test**

`Assets/_BattleBomb/Tests/EditMode/Net/StageCodecTests.cs`:

```csharp
using BattleBomb.Core.Net;
using NUnit.Framework;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class StageCodecTests
    {
        [Test]
        public void Load_ready_and_hand_over_round_trip()
        {
            var writer = new NetWriter();
            StageCodec.WriteLoad(writer, new LoadStageMessage(1, false, 118.5f, -1));
            var reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.LoadStage));
            LoadStageMessage load = StageCodec.ReadLoad(reader);
            Assert.That(load.StageIndex, Is.EqualTo(1));
            Assert.That(load.IsLaunch, Is.False);
            Assert.That(load.FirstArenaMinX, Is.EqualTo(118.5f));
            Assert.That(load.ResumeCheckpointArena, Is.EqualTo(-1));

            writer.Reset();
            StageCodec.WriteReady(writer, 1);
            reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.StageReady));
            Assert.That(StageCodec.ReadStage(reader), Is.EqualTo(1));

            writer.Reset();
            StageCodec.WriteHandOver(writer, 1);
            reader = new NetReader(writer.ToArray());
            Assert.That((NetMessageKind)reader.ReadByte(), Is.EqualTo(NetMessageKind.HandOver));
            Assert.That(StageCodec.ReadStage(reader), Is.EqualTo(1));
        }
    }
}
```

- [ ] **Step 2: Run it to see it fail**, then write `Assets/_BattleBomb/Core/Net/StageCodec.cs`:

```csharp
namespace BattleBomb.Core.Net
{
    /// <summary>A stage the guest must load: the launch stage (unload everything first, then load
    /// with its resume point) or the next stage behind the airlock (load beside the current one,
    /// shifted so its first arena begins at the host's exit line).</summary>
    public readonly struct LoadStageMessage
    {
        public readonly int StageIndex;
        public readonly bool IsLaunch;
        public readonly float FirstArenaMinX;
        public readonly int ResumeCheckpointArena;

        public LoadStageMessage(int stageIndex, bool isLaunch, float firstArenaMinX, int resumeCheckpointArena)
        {
            StageIndex = stageIndex;
            IsLaunch = isLaunch;
            FirstArenaMinX = firstArenaMinX;
            ResumeCheckpointArena = resumeCheckpointArena;
        }
    }

    public static class StageCodec
    {
        public static void WriteLoad(NetWriter w, in LoadStageMessage load)
        {
            w.WriteByte((byte)NetMessageKind.LoadStage);
            w.WriteInt(load.StageIndex);
            w.WriteBool(load.IsLaunch);
            w.WriteFloat(load.FirstArenaMinX);
            w.WriteInt(load.ResumeCheckpointArena);
        }

        public static LoadStageMessage ReadLoad(NetReader r) =>
            new LoadStageMessage(r.ReadInt(), r.ReadBool(), r.ReadFloat(), r.ReadInt());

        public static void WriteReady(NetWriter w, int stageIndex)
        {
            w.WriteByte((byte)NetMessageKind.StageReady);
            w.WriteInt(stageIndex);
        }

        public static void WriteHandOver(NetWriter w, int stageIndex)
        {
            w.WriteByte((byte)NetMessageKind.HandOver);
            w.WriteInt(stageIndex);
        }

        /// <summary>Reads a <c>StageReady</c> or <c>HandOver</c> positioned after its kind byte.</summary>
        public static int ReadStage(NetReader r) => r.ReadInt();
    }
}
```

`recompile`; `run_tests` EditMode `StageCodecTests`. Expected: pass.

- [ ] **Step 3: The host's runner announces, and waits**

In `Assets/_BattleBomb/Gameplay/World/StageRunner.cs`:

(a) With the events:

```csharp
        /// <summary>
        /// Raised when this runner asks for a stage scene — a launch (<c>isLaunch</c>, with its resume
        /// point) or the preload behind the airlock (with the x its first arena is shifted to). The
        /// host forwards it so the guest streams the same stage at the same place (M8).
        /// </summary>
        internal event Action<int, bool, float, int> StageLoadRequested;

        /// <summary>Raised when the stage behind the airlock becomes the stage.</summary>
        internal event Action<int> StageHandedOver;

        /// <summary>Online: whether the guest has this stage loaded. Null — offline — is always yes.
        /// The airlock opens only when both machines have the stage behind it (planning decision 10).</summary>
        internal Func<int, bool> RemoteStageReady { get; set; }

        /// <summary>The guest's runner: it loads what it is told and reports each stage as ready.</summary>
        internal event Action<int> ReplicaStageReady;
```

(b) Add:

```csharp
        private bool NextStageReady =>
            _next != null && _next.IsReady && (RemoteStageReady == null || RemoteStageReady(_next.StageIndex));

        /// <summary>The clamp again, when something outside the runner changed what it depends on — the
        /// guest finishing a load.</summary>
        internal void RefreshBounds()
        {
            if (_current != null && _current.IsReady && !_replica)
            {
                ApplyBounds();
            }
        }
```

(c) In `Launch`, directly before `BeginLoad(_current, null);`:

```csharp
            StageLoadRequested?.Invoke(stageIndex, true, 0f, resumeCheckpointArena);
```

(d) In `PreloadNextStage`, directly before `BeginLoad(_next, _current.Exit.X);`:

```csharp
            StageLoadRequested?.Invoke(_next.StageIndex, false, _current.Exit.X, -1);
```

(e) In `FinishStage`'s hand-over, directly after `_next = null;`:

```csharp
            StageHandedOver?.Invoke(_current.StageIndex);
```

(f) In `TryLeaveStage`, the readiness becomes:

```csharp
            bool ready = _next == null || NextStageReady;
```

(g) In `ApplyBounds`, the airlock condition becomes:

```csharp
            if (ExitIsNext(run) && NextStageReady)
```

- [ ] **Step 4: The guest's runner follows**

In `Assets/_BattleBomb/Gameplay/World/StageRunner.cs`:

(a) In `OnEnable`, replace Task 89's replica early-return with:

```csharp
            _replica = NetSession.RoleOf(Session.GameSession.Find()) == NetRole.Guest;
            if (_replica)
            {
                // The guest runs no stage of its own: it takes the chapter and tier the host launched and
                // waits to be told which stage scenes to stream (planning decision 10).
                Session.GameSession session = Session.GameSession.Find();
                TierSpec[] tiers = TierDefinition.ToRuntime(_tiers);
                _chapter = session != null ? session.Chapter : null;
                _tierIndex = Mathf.Clamp(session != null ? session.TierIndex : 0, 0, Mathf.Max(0, tiers.Length - 1));
                _tier = tiers[_tierIndex];
                return;
            }
```

(b) In `BeginLoad`'s completion callback, directly after the `if (firstArenaMinX.HasValue) ... else ...` adopt block, add:

```csharp
                if (_replica)
                {
                    if (stage == _current)
                    {
                        _current.SpawnProps(_chestPrefab, _dummyPrefab, _shopkeeperPrefab);
                        _driver.SetEncounter(EncounterInputs.From(_tier, _current.Run.Spec));
                    }

                    ReplicaStageReady?.Invoke(stage.StageIndex);
                    return;
                }
```

(c) Add:

```csharp
        /// <summary>The guest streams a stage the host asked for, at the same place.</summary>
        internal void ReplicaLoad(in LoadStageMessage load)
        {
            StageDefinition definition = _chapter != null ? _chapter.StageAt(load.StageIndex) : null;
            if (definition == null)
            {
                Debug.LogError($"{name}: the host streamed stage {load.StageIndex}, which this chapter does not have.", this);
                return;
            }

            if (load.IsLaunch)
            {
                UnloadEverything();
                _current = new LoadedStage(
                    definition, new StageRun(definition.ToRuntime(), load.ResumeCheckpointArena), load.StageIndex, _generation);
                BeginLoad(_current, null);
                return;
            }

            _next?.Unload();
            _next = new LoadedStage(definition, new StageRun(definition.ToRuntime()), load.StageIndex, _generation);
            BeginLoad(_next, load.FirstArenaMinX);
        }

        /// <summary>The host walked through the airlock: the stage behind it becomes the guest's too.</summary>
        internal void ReplicaHandOver(int stageIndex)
        {
            if (_next == null || _next.StageIndex != stageIndex || !_next.IsReady)
            {
                Debug.LogWarning($"{name}: the host handed over to stage {stageIndex}, which is not loaded here.", this);
                return;
            }

            LoadedStage previous = _current;
            _current = _next;
            _next = null;
            _current.SpawnProps(_chestPrefab, _dummyPrefab, _shopkeeperPrefab);
            _driver.SetEncounter(EncounterInputs.From(_tier, _current.Run.Spec));
            previous?.Unload();
        }
```

Add `using BattleBomb.Core.Net;` to the runner.

- [ ] **Step 5: The launch hold**

In `Assets/_BattleBomb/Gameplay/Simulation/SimulationDriver.cs`:

```csharp
        /// <summary>
        /// Online, the host's first step waits until the guest has the launch stage loaded (planning
        /// decision 10): nothing is running yet, so waiting costs nobody anything, and it means the
        /// guest never arrives in a fight already under way. The drop rule is the backstop.
        /// </summary>
        internal bool HoldForPeer { get; set; }
```

and in `Update`, directly after the replica block and before `if (PausedForScreen)`:

```csharp
            if (HoldForPeer)
            {
                _clock.Reset();
                return;
            }
```

- [ ] **Step 6: The host forwards, the guest answers**

In `Assets/_BattleBomb/Gameplay/Net/NetHost.cs`:

(a) A field:

```csharp
        private readonly HashSet<int> _guestReady = new HashSet<int>();
```

(b) At the end of `Begin`, before `SendLaunch();`:

```csharp
            if (_runner != null)
            {
                _runner.StageLoadRequested += OnStageLoadRequested;
                _runner.StageHandedOver += OnStageHandedOver;
                _runner.RemoteStageReady = stage => !_net.IsConnected || _guestReady.Contains(stage);
            }
```

(c) Add:

```csharp
        private void OnStageLoadRequested(int stage, bool isLaunch, float firstArenaMinX, int resumeCheckpointArena)
        {
            if (isLaunch)
            {
                _guestReady.Clear();
                _driver.HoldForPeer = _net.IsConnected;
            }

            _writer.Reset();
            StageCodec.WriteLoad(_writer, new LoadStageMessage(stage, isLaunch, firstArenaMinX, resumeCheckpointArena));
            _net.Send(NetChannel.Reliable, _writer);
        }

        private void OnStageHandedOver(int stage)
        {
            _writer.Reset();
            StageCodec.WriteHandOver(_writer, stage);
            _net.Send(NetChannel.Reliable, _writer);
        }

        private void OnGuestStageReady(int stage)
        {
            _guestReady.Add(stage);
            if (_driver.HoldForPeer && _runner != null && stage == _runner.StageIndex)
            {
                _driver.HoldForPeer = false;
            }

            _runner?.RefreshBounds();
        }
```

(d) In `OnMessage`, before the commands handling:

```csharp
            if (kind == NetMessageKind.StageReady)
            {
                OnGuestStageReady(StageCodec.ReadStage(reader));
                return;
            }
```

(e) In `OnPeerLeft`, add:

```csharp
            _driver.HoldForPeer = false;
            _runner?.RefreshBounds();
```

(f) In `OnDestroy`, inside the `_driver != null` block add `_driver.HoldForPeer = false;`, and add:

```csharp
            if (_runner != null)
            {
                _runner.StageLoadRequested -= OnStageLoadRequested;
                _runner.StageHandedOver -= OnStageHandedOver;
                _runner.RemoteStageReady = null;
            }
```

In `Assets/_BattleBomb/Gameplay/Net/NetGuest.cs`:

(a) In `Begin`, after finding the runner:

```csharp
            if (_runner != null)
            {
                _runner.ReplicaStageReady += OnStageReady;
            }
```

(b) In `OnMessage`, add cases:

```csharp
                case NetMessageKind.LoadStage:
                    _runner?.ReplicaLoad(StageCodec.ReadLoad(reader));
                    break;

                case NetMessageKind.HandOver:
                    _runner?.ReplicaHandOver(StageCodec.ReadStage(reader));
                    break;
```

(c) Add:

```csharp
        private void OnStageReady(int stage)
        {
            _writer.Reset();
            StageCodec.WriteReady(_writer, stage);
            _net.Send(NetChannel.Reliable, _writer);
        }
```

(d) In `OnDestroy`, add `if (_runner != null) { _runner.ReplicaStageReady -= OnStageReady; }`.

- [ ] **Step 7: The headless guest answers `LoadStage`**

In `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs`, add:

```csharp
        /// <summary>Answer every <c>LoadStage</c> with <c>StageReady</c> at once — a guest with no scene
        /// loads instantly. Off, the test decides when with <see cref="Ready"/>.</summary>
        internal bool AutoReady { get; set; } = true;

        internal List<int> LoadRequests { get; } = new List<int>();

        internal void Ready(int stage)
        {
            _writer.Reset();
            StageCodec.WriteReady(_writer, stage);
            _transport.Send(_host, NetChannel.Reliable, _writer.Buffer, _writer.Length);
        }
```

and in `Handle`'s data branch, before the final `else if (kind != NetMessageKind.KeepAlive)`:

```csharp
                    else if (kind == NetMessageKind.LoadStage)
                    {
                        int stage = StageCodec.ReadLoad(reader).StageIndex;
                        LoadRequests.Add(stage);
                        Received.Add(netEvent.Payload);
                        if (AutoReady)
                        {
                            Ready(stage);
                        }
                    }
```

- [ ] **Step 8: The airlock and the hold, proven**

In `Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs`:

(a) Add `using BattleBomb.Gameplay.World.Markers;`.

(b) In the set-up, after `yield return UntilFrames(() => _runner.IsStageLoaded, ...)`, add:

```csharp
            yield return UntilFrames(() => _driver.Frame > 5, "the launch hold never released — the host is still waiting for its guest");
```

(c) Add the case and its helper:

```csharp
        [UnityTest]
        public IEnumerator The_airlock_waits_for_the_guest_to_have_the_next_stage()
        {
            _guest.AutoReady = false;
            StageExitMarker exit = Object.FindObjectsByType<StageExitMarker>(FindObjectsInactive.Include)[0];
            foreach (StageExitMarker candidate in Object.FindObjectsByType<StageExitMarker>(FindObjectsInactive.Include))
            {
                if (candidate.gameObject.scene == _runner.StageScene)
                {
                    exit = candidate;
                }
            }

            yield return PushBothRight(() => _guest.LoadRequests.Contains(1) && _host.Position.x >= exit.X - 0.05f
                && _guestBody.Position.x >= exit.X - 0.05f, 4000, "both players to stage one's exit");

            // At the line with the guest not ready: the clamp must not reach past it, and no hand-over.
            yield return PushBothRight(() => false, 240, null);
            Assert.That(_runner.StageIndex, Is.Zero, "The host walked through the airlock before the guest had the stage behind it.");
            Assert.That(_host.Position.x, Is.LessThanOrEqualTo(exit.X + 0.01f),
                "The clamp opened into a stage the guest had not loaded.");

            _guest.Ready(1);
            yield return PushBothRight(() => _runner.StageIndex == 1, 1200, "the hand-over once the guest was ready");

            bool handedOver = false;
            foreach (byte[] message in _guest.Received)
            {
                var reader = new NetReader(message);
                handedOver |= (NetMessageKind)reader.ReadByte() == NetMessageKind.HandOver && StageCodec.ReadStage(reader) == 1;
            }

            Assert.That(handedOver, Is.True, "The guest was never told the stage was handed over.");
        }

        /// <summary>Both players push right — the host by script, the guest over the wire — until done,
        /// or the step budget runs out (null <paramref name="what"/> means running out is the point).</summary>
        private IEnumerator PushBothRight(System.Func<bool> done, int steps, string what)
        {
            _guest.Move = Vector2.right;
            _hostInput.Set(Vector2.right, CommandButtons.None);
            int deadline = _driver.Frame + steps;
            for (int guard = 0; guard < FrameCeiling && _driver.Frame < deadline; guard++)
            {
                if (done())
                {
                    break;
                }

                yield return null;
            }

            _guest.Move = Vector2.zero;
            _hostInput.Release();
            if (what != null && !done())
            {
                Assert.Fail($"Pushing right never reached {what} (host x={_host.Position.x:F2}, guest x={_guestBody.Position.x:F2}).");
            }
        }
```

(d) Add a second fixture at the end of the same file, inside the namespace — a launch whose guest has not loaded:

```csharp
    /// <summary>The host's first step waits for the guest (HANDOFF-M8 planning decision 10).</summary>
    public sealed class OnlineLaunchHoldSmokeTests
    {
        [UnityTest]
        public IEnumerator The_host_holds_its_first_step_until_the_guest_has_loaded()
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "online-hold";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession net = NetSession.FindOrCreate();
            net.Host(hostSide);
            HeadlessGuest guest = HeadlessGuest.Join(guestSide);
            guest.AutoReady = false;
            try
            {
                for (int i = 0; i < 300 && !(net.IsConnected && guest.IsWelcomed); i++)
                {
                    yield return null;
                }

                FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
                flow.State.Confirm(0);
                flow.State.Confirm(0);
                flow.State.Launch(flow.Selection.CanLaunch);
                for (int i = 0; i < 1500 && SceneManager.GetActiveScene().name != "Gameplay"; i++)
                {
                    yield return null;
                }

                var driver = Object.FindAnyObjectByType<SimulationDriver>();
                var runner = Object.FindAnyObjectByType<StageRunner>();
                for (int i = 0; i < 1500 && !runner.IsStageLoaded; i++)
                {
                    yield return null;
                }

                for (int i = 0; i < 120; i++)
                {
                    yield return null;
                }

                Assert.That(driver.Frame, Is.Zero, "The host started the run without its guest.");
                Assert.That(guest.LoadRequests, Does.Contain(0), "The guest was never asked to load the launch stage.");

                guest.Ready(0);
                for (int i = 0; i < 600 && driver.Frame <= 10; i++)
                {
                    yield return null;
                }

                Assert.That(driver.Frame, Is.GreaterThan(10), "The guest reported ready and the host never started.");
            }
            finally
            {
                Object.Destroy(guest.gameObject);
                Object.Destroy(GameSession.FindOrCreate().gameObject);
            }
        }
    }
```

- [ ] **Step 9: Run both suites.** `recompile`; EditMode green; PlayMode `OnlineHostSmokeTests` and `OnlineLaunchHoldSmokeTests` pass; the full PlayMode suite green (offline, `RemoteStageReady` is null and `HoldForPeer` is never set, so the M7 airlock tests must not change). Delete `Assets/InitTestScene*`.

- [ ] **Step 10: Live check (`QUIET ON`)** — two editors, launch, and walk Player 1 (main window, keyboard) and Player 2 (Player 2 window, keyboard) together to stage one's last room. With `eval` in each editor read `StageRunner.StageIndex` and which stage scenes are loaded (`SceneManager.sceneCount`, each `GetSceneAt(i).name`): both have `FixtureStage2` loaded before either crosses the exit; after crossing, both report stage 1 and `FixtureStage1` unloaded. `QUIET OFF`.

- [ ] **Step 11: Commit** — subject `94: the stage follows — both machines stream, the airlock waits for both`. Body: stage loads and hand-overs forwarded; the launch holds for the guest; the airlock's clamp and exit both wait until the guest has the stage behind them; offline nothing changes.

---
### Task 95: Proof

Two tripwires that make Stage B permanent (D45; HANDOFF-M8 Testing). **Record and replay:** a hosted run with a real fight is recorded — every message the host sent, with the step it went out on, and where every player and enemy truly was — then played back into a fresh guest machine, whose replica is compared with the truth step by step. And **the whole chapter online:** a remote Player 2 walks the fixture chapter to its end with the host. The replay fixture also carries the guest's `Stepped`-subscriber check (HANDOFF-M8 standing watch — subscriptions exist only at runtime, so it is a PlayMode case).

**Files:**
- Create: `Assets/_BattleBomb/Tests/PlayMode/PlaybackTransport.cs`, `Assets/_BattleBomb/Tests/PlayMode/ReplicaReplaySmokeTests.cs`
- Modify: `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs` (timestamps), `Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs` (the whole chapter)

- [ ] **Step 1: The headless guest stamps what it receives**

In `Assets/_BattleBomb/Tests/PlayMode/HeadlessGuest.cs`, add:

```csharp
        /// <summary>Where the host's clock was when each message arrived — set by a recording test.</summary>
        internal System.Func<int> Clock { get; set; }

        /// <summary>Every message after the handshake, with the host step it arrived on, for playback.</summary>
        internal List<(int Frame, byte[] Payload)> Recorded { get; } = new List<(int Frame, byte[] Payload)>();
```

and at the top of `Handle`'s `NetEventKind.Data` branch, before the kind is read:

```csharp
                    if (IsWelcomed)
                    {
                        Recorded.Add((Clock != null ? Clock() : 0, netEvent.Payload));
                    }
```

- [ ] **Step 2: The playback transport**

`Assets/_BattleBomb/Tests/PlayMode/PlaybackTransport.cs`:

```csharp
using System.Collections.Generic;
using BattleBomb.Core.Net;
using BattleBomb.Platform.Net;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// A host that is a recording: it welcomes whoever connects, then plays back what a real host
    /// sent, each message on the step it originally went out, at 60 steps a second of real time.
    /// What the guest sends back is ignored — the recording cannot answer, and does not need to.
    /// </summary>
    internal sealed class PlaybackTransport : INetTransport
    {
        private static readonly NetPeer Host = new NetPeer(1);

        private readonly List<(int Frame, byte[] Payload)> _recording;
        private readonly Queue<NetEvent> _inbox = new Queue<NetEvent>();
        private readonly int _firstFrame;
        private double _start = -1.0;
        private int _next;
        private bool _connected;

        internal PlaybackTransport(List<(int Frame, byte[] Payload)> recording)
        {
            _recording = recording;
            _firstFrame = recording.Count > 0 ? recording[0].Frame : 0;
        }

        internal bool Finished => _next >= _recording.Count;

        public void Listen()
        {
        }

        public void Connect(string address)
        {
            _connected = true;
            _inbox.Enqueue(NetEvent.Connected(Host));
            var writer = new NetWriter();
            HandshakeCodec.WriteWelcome(writer, new WelcomeMessage(1));
            _inbox.Enqueue(NetEvent.Data(Host, NetChannel.Reliable, writer.ToArray()));
        }

        public void Send(NetPeer peer, NetChannel channel, byte[] payload, int length)
        {
        }

        public void Update(double nowSeconds)
        {
            if (!_connected)
            {
                return;
            }

            if (_start < 0.0)
            {
                _start = nowSeconds;
            }

            double frame = _firstFrame + (nowSeconds - _start) * 60.0;
            while (_next < _recording.Count && _recording[_next].Frame <= frame)
            {
                _inbox.Enqueue(NetEvent.Data(Host, NetChannel.Reliable, _recording[_next].Payload));
                _next++;
            }
        }

        public bool TryReceive(out NetEvent netEvent)
        {
            if (_inbox.Count > 0)
            {
                netEvent = _inbox.Dequeue();
                return true;
            }

            netEvent = default;
            return false;
        }

        public void Disconnect(NetPeer peer)
        {
            if (!_connected)
            {
                return;
            }

            _connected = false;
            _inbox.Enqueue(NetEvent.Disconnected(Host));
        }

        public void Dispose() => _connected = false;
    }
}
```

- [ ] **Step 3: Record, replay, compare**

`Assets/_BattleBomb/Tests/PlayMode/ReplicaReplaySmokeTests.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BattleBomb.Gameplay.Characters;
using BattleBomb.Gameplay.Net;
using BattleBomb.Gameplay.Players;
using BattleBomb.Gameplay.Session;
using BattleBomb.Gameplay.Simulation;
using BattleBomb.Gameplay.World;
using BattleBomb.Platform;
using BattleBomb.Platform.Net;
using BattleBomb.UI.Frontend;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BattleBomb.Tests.PlayMode
{
    /// <summary>
    /// D45's tripwire for the mirror (HANDOFF-M8 Task 95): a hosted fight is recorded — what the host
    /// sent and where everything truly was — then replayed into a guest machine, whose picture must
    /// match the truth at the frame it is drawing. Tolerances allow for drawing between snapshots two
    /// steps apart; a forgotten field, a wrong id, or a stuck replica is far outside them.
    /// </summary>
    public sealed class ReplicaReplaySmokeTests
    {
        private const int RecordSteps = 600;
        private const float PlayerTolerance = 0.3f;
        private const float EnemyTolerance = 0.5f;

        /// <summary>The <c>Stepped</c> subscribers a guest is known to carry, each vetted: it either
        /// only reads, or returns early on a replica (the runner). A new one fails this until someone
        /// decides which it is (HANDOFF-M8 standing watch).</summary>
        private static readonly string[] VettedGuestSubscribers =
        {
            "StageRunner", "ChestScreenHost", "SettingsMenu", "ResultsScreen", "CastTell",
        };

        private readonly Dictionary<int, Truth> _truth = new Dictionary<int, Truth>();

        [UnityTest]
        public IEnumerator A_replayed_host_run_draws_what_the_host_simulated()
        {
            List<(int Frame, byte[] Payload)> recording = null;
            yield return Record(r => recording = r);
            Assert.That(recording.Count, Is.GreaterThan(RecordSteps / 4), "Almost nothing was recorded.");

            GameSession fresh = GameSession.FindOrCreate();
            fresh.Store = new MemorySaveStore();
            fresh.SaveName = "replay-guest";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var playback = new PlaybackTransport(recording);
            NetSession.FindOrCreate().Join(playback, "playback");

            NetGuest guest = null;
            for (int i = 0; i < 1500 && guest == null; i++)
            {
                yield return null;
                guest = Object.FindAnyObjectByType<NetGuest>();
            }

            Assert.That(guest, Is.Not.Null, "The guest never loaded the host's run.");
            var driver = Object.FindAnyObjectByType<SimulationDriver>();

            AssertSteppedSubscribersAreVetted(driver);

            int compared = 0;
            float worstPlayer = 0f;
            float worstEnemy = 0f;
            for (int guard = 0; guard < 6000 && !(playback.Finished && guest.RenderFrame >= guest.NewestHostFrame - 1); guard++)
            {
                yield return null;
                float render = guest.RenderFrame;
                if (render < 0f || !TryTruthAt(render, out Vector3 one, out Vector3 two, out Dictionary<int, Vector3> enemies))
                {
                    continue;
                }

                foreach (CharacterActor actor in driver.Characters.Ordered)
                {
                    Vector3 expected = actor.PlayerId.Value == 0 ? one : two;
                    worstPlayer = Mathf.Max(worstPlayer, Planar(actor.Position - expected));
                }

                foreach (EnemyActor enemy in Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
                {
                    if (enemy.NetId > 0 && enemies.TryGetValue(enemy.NetId, out Vector3 at))
                    {
                        worstEnemy = Mathf.Max(worstEnemy, Planar(enemy.Position - at));
                    }
                }

                compared++;
            }

            Assert.That(compared, Is.GreaterThan(100), "The replica drew almost nothing worth comparing.");
            Assert.That(worstPlayer, Is.LessThan(PlayerTolerance), $"A replica player was {worstPlayer:F2} from the truth.");
            Assert.That(worstEnemy, Is.LessThan(EnemyTolerance), $"A replica enemy was {worstEnemy:F2} from the truth.");
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            foreach (HeadlessGuest guest in Object.FindObjectsByType<HeadlessGuest>(FindObjectsSortMode.None))
            {
                Object.Destroy(guest.gameObject);
            }

            GameSession session = GameSession.Find();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }

            yield return null;
        }

        /// <summary>A hosted fight with spawns on and nobody pressing anything but the guest's stick:
        /// enemies walk, swing, stagger players, maybe wipe them — everything a snapshot must carry.</summary>
        private IEnumerator Record(System.Action<List<(int Frame, byte[] Payload)>> done)
        {
            GameSession stale = GameSession.Find();
            if (stale != null)
            {
                Object.Destroy(stale.gameObject);
                yield return null;
            }

            GameSession session = GameSession.FindOrCreate();
            session.Store = new MemorySaveStore();
            session.SaveName = "replay-host";
            SceneManager.LoadScene("Frontend", LoadSceneMode.Single);
            yield return null;
            yield return null;
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            LoopbackTransport.CreatePair(out LoopbackTransport hostSide, out LoopbackTransport guestSide);
            NetSession.FindOrCreate().Host(hostSide);
            HeadlessGuest guest = HeadlessGuest.Join(guestSide);
            for (int i = 0; i < 300 && !guest.IsWelcomed; i++)
            {
                yield return null;
            }

            SimulationDriver driver = null;
            guest.Clock = () => driver != null ? driver.Frame : 0;

            FrontendFlow flow = Object.FindAnyObjectByType<FrontendFlow>();
            flow.State.Confirm(0);
            flow.State.Confirm(0);
            flow.State.Launch(flow.Selection.CanLaunch);
            for (int i = 0; i < 1500 && SceneManager.GetActiveScene().name != "Gameplay"; i++)
            {
                yield return null;
            }

            driver = Object.FindAnyObjectByType<SimulationDriver>();
            foreach (InputSystemCommandSource device in
                Object.FindObjectsByType<InputSystemCommandSource>(FindObjectsInactive.Include))
            {
                device.enabled = false;
            }

            driver.Stepped += frame => _truth[frame] = Truth.Of(driver);
            guest.Move = new Vector2(0.4f, 0.2f);

            int stop = 0;
            for (int i = 0; i < 20000 && (stop == 0 || driver.Frame < stop); i++)
            {
                if (stop == 0 && driver.Frame > 0)
                {
                    stop = driver.Frame + RecordSteps;
                }

                yield return null;
            }

            done(new List<(int Frame, byte[] Payload)>(guest.Recorded));
            Object.Destroy(guest.gameObject);
            Object.Destroy(GameSession.Find().gameObject);
            yield return null;
        }

        private bool TryTruthAt(float render, out Vector3 one, out Vector3 two, out Dictionary<int, Vector3> enemies)
        {
            int from = Mathf.FloorToInt(render);
            one = default;
            two = default;
            enemies = null;
            if (!_truth.TryGetValue(from, out Truth a) || !_truth.TryGetValue(from + 1, out Truth b))
            {
                return false;
            }

            float t = render - from;
            one = Vector3.Lerp(a.One, b.One, t);
            two = Vector3.Lerp(a.Two, b.Two, t);
            enemies = new Dictionary<int, Vector3>();
            foreach (KeyValuePair<int, Vector3> entry in a.Enemies)
            {
                enemies[entry.Key] = b.Enemies.TryGetValue(entry.Key, out Vector3 next)
                    ? Vector3.Lerp(entry.Value, next, t)
                    : entry.Value;
            }

            return true;
        }

        private static void AssertSteppedSubscribersAreVetted(SimulationDriver driver)
        {
            FieldInfo backing = typeof(SimulationDriver).GetField("Stepped", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(backing, Is.Not.Null, "SimulationDriver.Stepped is no longer a field-like event; update this check.");
            var stepped = backing.GetValue(driver) as System.Delegate;
            if (stepped == null)
            {
                return;
            }

            foreach (System.Delegate handler in stepped.GetInvocationList())
            {
                string type = handler.Target != null ? handler.Target.GetType().Name : handler.Method.DeclaringType.Name;
                Assert.That(VettedGuestSubscribers, Does.Contain(type),
                    $"{type} subscribes to Stepped on a guest. Decide whether it changes the simulation: if it " +
                    "does, it must be host-only or return early on a replica; then add it to VettedGuestSubscribers.");
            }
        }

        /// <summary>Distance on the ground plane — the replica's height between snapshots is the one
        /// thing interpolation legitimately shaves off an arc.</summary>
        private static float Planar(Vector3 delta) => new Vector2(delta.x, delta.z).magnitude;

        private readonly struct Truth
        {
            public readonly Vector3 One;
            public readonly Vector3 Two;
            public readonly Dictionary<int, Vector3> Enemies;

            private Truth(Vector3 one, Vector3 two, Dictionary<int, Vector3> enemies)
            {
                One = one;
                Two = two;
                Enemies = enemies;
            }

            public static Truth Of(SimulationDriver driver)
            {
                Vector3 one = default;
                Vector3 two = default;
                foreach (CharacterActor actor in driver.Characters.Ordered)
                {
                    if (actor.PlayerId.Value == 0)
                    {
                        one = actor.Position;
                    }
                    else
                    {
                        two = actor.Position;
                    }
                }

                var enemies = new Dictionary<int, Vector3>();
                foreach (ISimTarget target in driver.Targets.Ordered)
                {
                    if (target is EnemyActor enemy && enemy.NetId > 0)
                    {
                        enemies[enemy.NetId] = enemy.Position;
                    }
                }

                return new Truth(one, two, enemies);
            }
        }
    }
}
```

If `VettedGuestSubscribers` fails on a type Groundwork added (a prompt row ticking on `Stepped`, say), read that type: if it only reads, add its name with a one-line comment; if it writes simulation state, stop and report — it needs a replica guard first.

- [ ] **Step 4: The whole chapter with a remote Player 2**

In `Assets/_BattleBomb/Tests/PlayMode/OnlineHostSmokeTests.cs`, add `using BattleBomb.Core.Chapters;` and:

```csharp
        [UnityTest]
        public IEnumerator A_remote_player_two_walks_the_fixture_chapter_to_its_end()
        {
            yield return PushBothRight(() => _runner.StageIndex == 1, 5000, "stage two, through the airlock");
            yield return PushBothRight(() => _runner.Phase == StagePhase.Complete, 5000, "the end of the chapter");

            Assert.That(_guest.LoadRequests, Is.EqualTo(new[] { 0, 1 }),
                "The guest was not asked to stream exactly the chapter's two stages.");
            Assert.That(_guest.Received.Exists(m => new NetReader(m).ReadByte() == (byte)NetMessageKind.HandOver), Is.True,
                "The guest was never told about the hand-over.");
            Assert.That(_driver.Characters.Ordered.Count, Is.EqualTo(2), "Player 2 did not finish the chapter with the host.");
        }
```

- [ ] **Step 5: Run the PlayMode suite.** `run_tests` PlayMode, `async_tests: true`, `filter: ReplicaReplaySmokeTests` then `OnlineHostSmokeTests`; then the full PlayMode suite. Expected: all green. Report the replay's worst player and enemy errors from the assertion messages even on a pass (add a `Debug.Log` of both before the asserts while running it once, then remove it). Delete `Assets/InitTestScene*`.

Failure guide: a worst player error near 1–3 units is a replica stuck on an old snapshot (the render clock, or `DiscardBefore`); enemies failing while players pass is an id or origin problem (`SetOrigin`, `DefinitionOf`); `compared` near zero is the held-messages path (`ReleaseHeld` in `NetGuest.Start`).

- [ ] **Step 6: Commit** — subject `95: proof — replay into a replica, and a whole chapter online`. Body: the mirror compared with the truth frame by frame; a remote Player 2 through both stages and the airlock; the guest's `Stepped` subscribers pinned so a new one is a decision, not an accident.

---

### Task 96: Michael's pass (Stage B), and Plan 1's close-out

- [ ] **Step 1: Both gates, one last time.** Full EditMode and PlayMode runs; record exact counts; delete `Assets/InitTestScene*`; `git status` clean apart from what this task writes.

- [ ] **Step 2: Michael's pass (fast motion — his eyes, not sampling)**

Hand him this checklist and treat his report as the verification. `QUIET ON` for its duration.

1. **Two windows.** *Window → Multiplayer → Multiplayer Play Mode*, Player 2 ticked, press Play. Main window: **Host local**. Player 2 window: **Join local**. Launch a game from the main window as usual.
2. **The mirror.** The Player 2 window now shows the game — both heroes, the stage, the enemies. (Keyboard only, click into the window you drive; a controller would drive both.)
3. **Watching the host.** Drive Player 1 in the main window and watch the Player 2 window: Player 1 moves smoothly there, a moment behind. Fight something: hit flashes and damage numbers appear in both windows.
4. **Being the guest.** Drive Player 2 from its own window. Everything works — but there is a small delay between your press and your hero moving *in that window*. That delay is what Stage E removes; for now, notice how big it feels.
5. **Loot.** Kill enemies until something drops. Grab it with either player: it vanishes in both windows. (The guest cannot open chests yet — Plan 2.)
6. **The stage.** Walk both players to the end of the first stage: the second stage streams in on both, and the exit does not open until both windows have it.
7. **Lag.** In both windows press **Leave**, set **Lag: Normal**, Host local / Join local again, and repeat 4. Then **Lag: Bad**, and repeat 4.

**What to tell me** — for each of *None*, *Normal*, *Bad*, for the guest's own hero: moving feels *fine / noticeable but OK / too late*; attacking feels *fine / noticeable but OK / too late*. That answer sizes Stage E (prediction) — Plan 3 is written from it.

- [ ] **Step 3: Record it.** In `docs/HANDOFF-M8.md`: add a **Build log** section (after *The build — stages and tasks*) with one row per task — task, commit, what landed — for 86–96; Michael's Stage A and Stage B verdicts, including the lag table from Step 2; and **Plan 1 close-out notes** — what felt wrong to build, what the plan got wrong (M7's close-out warned plans are wrong more often than right about the hard parts; say where this one was), anything deferred. In `docs/ROADMAP.md` §4 M8, add one line: *Stages A–B (the remote controller, the mirror) complete — `<first>`..`<last>`.*

- [ ] **Step 4: Commit** — subject `96: M8 stages A–B — Michael's pass and Plan 1's close-out`. Then tell the orchestrator that Plan 2 (Tasks 97–105) can be written — the Netcode lane writes it.

---

## Self-review (done while writing)

- **Spec coverage.** HANDOFF-M8 Stage A — the wire (86), the transport seam with loopback, local socket and lag (87), the input buffer and remote source (88), the session and handshake with version refusal and timeouts, the host swapping the guest slot's source, the dev entry (89), two editors (90). Stage B — ids and the snapshot with field-coverage tests (91), snapshots every second step and batched events with `HitEvent` as entity references (92), replica mode with `SnapshotBuffer` and interpolation, `ApplyReplica`, replica spawn/despawn, replicated events, and replica guards on `StageRunner`, `EnemySpawner` and `SaveService` (89 and 93), `LoadStage`/`StageReady`, the both-ready airlock and the launch hold (94), the replay proof and the whole-chapter tripwire (95), Michael's pass and close-out (96). Planning decisions: 1 (host untouched — only hooks and an opt-in hold), 2 (Core/Net and coverage, the four factories), 3 (the Platform seam; `PlatformRegistry` deliberately deferred to Plan 3, when there is a second platform to register), 4 (three transports — Steam's is Plan 3), 5 (`NetSession` on the session object, early execution order), 6 (replica mode), 7 (`ApplyReplica`), 8 (ids), 9 (entity references), 10 (stage flow and readiness), 18 (seat versus player), 20 (versioning, bounded counts). Out of Plan 1 by design: 11–17 and 19 (Plans 2 and 3).
- **Two stand-ins, both named and both temporary:** the guest plays the host's hero (Plan 2 Task 102), and `SimulationDriver.MayOpenScreen` (retired by Plan 2 Task 99).
- **Type consistency.** `NetWriter`/`NetReader` (`WriteCount`/`ReadCount`, `WriteString`/`ReadString(max)`); `NetMessageKind` (Hello, Welcome, Refuse, Commands, Launch, Snapshot, Events, LoadStage, StageReady, HandOver, KeepAlive, SessionEnd, Bye); `WireCommand.From`; `CommandCodec.{Quantized, Write, Read}`; `InputBuffer.{Add, TryTake, LastTakenFrame, MaxQueued}`; `RemoteCommandStream.{Receive, Next, Release, LastConsumedFrame, StarvedSteps, MergedSteps}`; `HandshakeCodec.{WriteHello, ReadHello, WriteWelcome, ReadWelcome, WriteRefuse, ReadRefuse, WriteLaunch, ReadLaunch, WriteBare, CheckHello}`; `StageCodec.{WriteLoad, ReadLoad, WriteReady, WriteHandOver, ReadStage}`; `StateCodec` writer/reader pairs; `SnapshotCodec.{Write, Read}`; `EventCodec.{Write, Read}`; `ItemWire.{Write, Read, StandIn}`; `SnapshotBuffer.{Rent, Add, TrySample, DiscardBefore, NewestFrame, Capacity}`; `RenderClock.{Advance, Frame, IsRunning, Reset}`; `INetTransport.{Listen, Connect, Send, Update, TryReceive, Disconnect}`; `NetSession.{Host, Join, HostLocal, JoinLocal, Leave, Send, ReleaseHeld, RoleOf, FindOrCreate, IsConnected, GuestPlayerId, MessageReceived, PeerJoined, PeerLeft}`; `CharacterActor.{BindSource, CaptureReplica, ApplyReplica}`; `EnemyActor.{NetId, SetOrigin, CaptureReplica, ApplyReplica}`; `TrainingDummy.{PropIndex, SetPropIndex, CaptureReplica, ApplyReplica}`; `EnemySpawner.{ReplicaSpawn, ReplicaDespawn}`; `SimulationDriver.{EnterReplicaMode, ReplicaStepping, HoldForPeer, MayOpenScreen, PickupSpawned, RefusedStepsFor, AttemptStepsAllDown, ApplyReplica*, RemoveReplicaPickup, RaiseReplicatedHit}`; `StageRunner.{StageLoadRequested, StageHandedOver, RemoteStageReady, ReplicaStageReady, RefreshBounds, ReplicaLoad, ReplicaHandOver, ReplicaDummy}` — each defined once, used with the same signature everywhere.
- **Known risks for the executor.** (1) Every quoted line predates Groundwork and the Builder batch — re-read before editing (the header says so). (2) `SessionBinder`'s `[DefaultExecutionOrder(-100)]` is load-bearing four ways now: seats, the remote source, the guest's device, replica mode — all must happen before any player object's `OnEnable`. (3) The replay test's tolerances are paper values; if a pass shows a healthy replica just over them, widen with a comment saying what the measured worst was — never to hide a replica that is visibly wrong.
