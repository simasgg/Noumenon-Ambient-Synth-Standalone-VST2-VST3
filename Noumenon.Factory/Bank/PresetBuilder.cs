using Noumenon.Dsp.Oscillators;
using Noumenon.Dsp.Shared;
using Noumenon.Engine;
using Noumenon.Engine.Lanes;
using Noumenon.Engine.Parameters;
using Noumenon.Engine.Presets;

namespace Noumenon.Factory.Bank;

/// <summary>
/// The vocabulary the factory recipes are written in: it starts from the init patch (the table
/// defaults) and names things the way the panel does — slots, sections, the combine stage, the
/// chain blocks, the sampler, lanes, the matrix — so a recipe reads as a patch sheet. Choices
/// are given by their display names and checked against the table, so a renamed or reordered
/// list fails the build of the bank instead of silently picking another entry. Lanes are drawn
/// either as smooth seamless loops (the randomizer's three-sine loops, from the recipe's own seed)
/// or as held steps for melodies. <see cref="Build"/> writes every parameter, not only the
/// changed ones, so a later change of a default never alters a factory preset.
/// </summary>
internal sealed class PresetBuilder
{
    private readonly float[] values = new float[ParameterTable.Count];
    private readonly List<LaneState> lanes = [];
    private readonly List<SampleReference> samples = [];
    private readonly Xoshiro128 rng;
    private readonly ulong seed;
    private int nextModSlot;

    public PresetBuilder(ulong seed)
    {
        this.seed = seed;
        rng = new Xoshiro128(seed);
        foreach (var info in ParameterTable.All)
            values[info.Index] = info.Default;
    }

    public float this[ParamId id] => values[(int)id];

    public PresetBuilder Set(ParamId id, float value)
    {
        values[(int)id] = ParameterTable.Get(id).Clamp(value);

        return this;
    }

    public PresetBuilder Set(ParamId id, bool value) => Set(id, value ? 1f : 0f);

    /// <summary>A choice parameter by its display name.</summary>
    public PresetBuilder Choose(ParamId id, string choice)
    {
        var info = ParameterTable.Get(id);
        var index = -1;
        for (var i = 0; i < info.Choices.Count; i++)
        {
            if (string.Equals(info.Choices[i], choice, StringComparison.OrdinalIgnoreCase))
                index = i;
        }

        if (index < 0)
            throw new ArgumentException($"{info.Name} has no choice '{choice}' ({string.Join(", ", info.Choices)}).");

        return Set(id, index);
    }

    // ---- Sections ----

    /// <summary>One oscillator slot, switched on; <paramref name="slot"/> counts from 1 as on the panel.</summary>
    public PresetBuilder Osc(SectionId section, int slot, Waveform wave, int semitone, float fine, float level, float shape = 0.5f, float noiseCutoff = 2000f, float trim = 1f)
    {
        Set(ParameterTable.Osc(section, slot - 1, OscParam.On), true);
        Set(ParameterTable.Osc(section, slot - 1, OscParam.Wave), (int)wave);
        Set(ParameterTable.Osc(section, slot - 1, OscParam.Semitone), semitone);
        Set(ParameterTable.Osc(section, slot - 1, OscParam.Fine), fine);
        Set(ParameterTable.Osc(section, slot - 1, OscParam.Level), level);
        Set(ParameterTable.Osc(section, slot - 1, OscParam.Shape), shape);
        Set(ParameterTable.Osc(section, slot - 1, OscParam.NoiseCutoff), noiseCutoff);

        return Set(ParameterTable.Osc(section, slot - 1, OscParam.Trim), trim);
    }

    public PresetBuilder A(int slot, Waveform wave, int semitone, float fine, float level, float shape = 0.5f, float noiseCutoff = 2000f) => Osc(SectionId.A, slot, wave, semitone, fine, level, shape, noiseCutoff);

    public PresetBuilder B(int slot, Waveform wave, int semitone, float fine, float level, float shape = 0.5f, float noiseCutoff = 2000f) => Osc(SectionId.B, slot, wave, semitone, fine, level, shape, noiseCutoff);

    public PresetBuilder Off(SectionId section, params int[] slots)
    {
        foreach (var slot in slots)
            Set(ParameterTable.Osc(section, slot - 1, OscParam.On), false);

        return this;
    }

    public PresetBuilder Silence(SectionId section) => Off(section, 1, 2, 3, 4, 5, 6);

    /// <summary>Which P faders reach a slot's pitch (both do by default).</summary>
    public PresetBuilder Routes(SectionId section, int slot, bool p1, bool p2)
    {
        Set(ParameterTable.Osc(section, slot - 1, OscParam.P1Route), p1);

        return Set(ParameterTable.Osc(section, slot - 1, OscParam.P2Route), p2);
    }

    public PresetBuilder RoutesAll(bool p1, bool p2)
    {
        for (var s = 0; s < ParameterTable.SectionCount; s++)
        {
            for (var slot = 1; slot <= ParameterTable.SlotsPerSection; slot++)
                Routes((SectionId)s, slot, p1, p2);
        }

        return this;
    }

    public PresetBuilder Filter(SectionId section, float cutoff, float reso = 0.1f, string type = "LP", bool slave = false)
    {
        Set(ParameterTable.Section(section, SectionParam.Cutoff), cutoff);
        Set(ParameterTable.Section(section, SectionParam.Reso), reso);
        Choose(ParameterTable.Section(section, SectionParam.FilterType), type);

        return Set(ParameterTable.Section(section, SectionParam.Slave), slave);
    }

    public PresetBuilder Level(SectionId section, float level) => Set(ParameterTable.Section(section, SectionParam.Level), level);

    /// <summary>The combine stage: 0 = the sum, 1 = the ring modulation.</summary>
    public PresetBuilder Combine(float crossfade, float balance = 0f, float ring = 1f)
    {
        Set(ParamId.MixCrossfade, crossfade);
        Set(ParamId.MixSumBalance, balance);

        return Set(ParamId.MixRingLevel, ring);
    }

    // ---- Pitch layer ----

    public PresetBuilder Pitch(int fader, float semitones, bool midi = false, float glide = 0.1f, int bend = 2)
    {
        Set(ParameterTable.Pitch(fader - 1, PitchParam.Value), semitones);
        Set(ParameterTable.Pitch(fader - 1, PitchParam.Midi), midi);
        Set(ParameterTable.Pitch(fader - 1, PitchParam.Glide), glide);

        return Set(ParameterTable.Pitch(fader - 1, PitchParam.BendRange), bend);
    }

    public PresetBuilder Gated(float attack, float release)
    {
        Choose(ParamId.PlayMode, "Gated");
        Set(ParamId.GateAttack, attack);

        return Set(ParamId.GateRelease, release);
    }

    public PresetBuilder Scale(string scale, string root = "C")
    {
        Choose(ParamId.Scale, scale);

        return Choose(ParamId.ScaleRoot, root);
    }

    // ---- Sampler ----

    /// <summary>Loads a factory sample (a file under content/samples) onto a map key.</summary>
    public PresetBuilder Sample(int key, string file, float gain = 1f, int rootNote = 60)
    {
        samples.RemoveAll(sample => sample.Key == key);
        samples.Add(new SampleReference { Key = key, Path = FactoryContent.Sample(file), Gain = gain, RootNote = rootNote });

        return this;
    }

    public PresetBuilder Sampler(int key, string mode = "Classic", float amp = 0.5f, float dir = 1f, int pitch = 0, float master = 0.8f, string mod = "None")
    {
        Set(ParamId.SamplerSelect, key);
        Choose(ParamId.SamplerMode, mode);
        Set(ParamId.SamplerAmp, amp);
        Set(ParamId.SamplerDir, dir);
        Set(ParamId.SamplerPitch, pitch);
        Set(ParamId.SamplerMaster, master);

        return Choose(ParamId.SamplerModSource, mod);
    }

    public PresetBuilder Loop(float start, float end, float crossfadeMs = 60f)
    {
        Set(ParamId.SamplerLoopStart, start);
        Set(ParamId.SamplerLoopEnd, end);

        return Set(ParamId.SamplerLoopCrossfade, crossfadeMs);
    }

    public PresetBuilder Grains(float sizeMs, float density, float position, float spray, float pitchSpread = 0f, float window = 0.3f, float width = 0.6f, string scan = "Free")
    {
        Set(ParamId.GrainSize, sizeMs);
        Set(ParamId.GrainDensity, density);
        Set(ParamId.GrainPosition, position);
        Set(ParamId.GrainSpray, spray);
        Set(ParamId.GrainPitchSpread, pitchSpread);
        Set(ParamId.GrainWindow, window);
        Set(ParamId.GrainWidth, width);

        return Choose(ParamId.GrainScan, scan);
    }

    public PresetBuilder Stretch(float amount, string window = "Long")
    {
        Set(ParamId.StretchAmount, amount);

        return Choose(ParamId.StretchWindow, window);
    }

    /// <summary>The oscillators' Wavetable wave reads the sample on this key.</summary>
    public PresetBuilder Wavetable(int key) => Set(ParamId.WavetableKey, key);

    // ---- Chain ----

    public PresetBuilder MasterFilter(float cutoff, float reso = 0.1f, float morph = 0f, float mix = 1f)
    {
        Set(ParamId.MasterFilterOn, true);
        Set(ParamId.MasterFilterCutoff, cutoff);
        Set(ParamId.MasterFilterReso, reso);
        Set(ParamId.MasterFilterMorph, morph);

        return Set(ParamId.MasterFilterMix, mix);
    }

    public PresetBuilder Distortion(float mix, float drive, string curve = "Tanh")
    {
        Set(ParamId.DistortionOn, true);
        Set(ParamId.DistortionMix, mix);
        Set(ParamId.DistortionDrive, drive);

        return Choose(ParamId.DistortionCurve, curve);
    }

    public PresetBuilder Eq(float lowDb, float highDb)
    {
        Set(ParamId.EqLow, lowDb);

        return Set(ParamId.EqHigh, highDb);
    }

    /// <summary>The DRIFT block (the through-delay): times in ms, feedback 0–0.95.</summary>
    public PresetBuilder Drift(float t1, float t2, float feedback, float mix = 1f, bool on = true)
    {
        Set(ParamId.SpinOn, on);
        Set(ParamId.SpinTime1, t1);
        Set(ParamId.SpinTime2, t2);
        Set(ParamId.SpinFeedback, feedback);

        return Set(ParamId.SpinMix, mix);
    }

    /// <summary>The RESONATOR block: a chord from the list (or "Voices" for the six voice notes), feedback, mix, transposition.</summary>
    public PresetBuilder Resonator(string chord, float feedback, float mix, int pitch = 0, float fine = 0f, bool on = true)
    {
        Set(ParamId.ResochordOn, on);
        Choose(ParamId.ResochordChord, chord);
        Set(ParamId.ResochordFeedback, feedback);
        Set(ParamId.ResochordMix, mix);
        Set(ParamId.ResochordPitch, pitch);

        return Set(ParamId.ResochordFine, fine);
    }

    public PresetBuilder Voice(int voice, int note, float feedback = 0.9f, float damping = 0.3f, float level = 0.8f, float pan = 0f)
    {
        Set(ParameterTable.ResoVoice(voice - 1, ResoVoiceParam.Note), note);
        Set(ParameterTable.ResoVoice(voice - 1, ResoVoiceParam.Feedback), feedback);
        Set(ParameterTable.ResoVoice(voice - 1, ResoVoiceParam.Damping), damping);
        Set(ParameterTable.ResoVoice(voice - 1, ResoVoiceParam.Level), level);

        return Set(ParameterTable.ResoVoice(voice - 1, ResoVoiceParam.Pan), pan);
    }

    public PresetBuilder Reverb(string room, float mix, float size = 0.7f, float damping = 0.4f, bool freeze = false, string order = "Post", bool on = true)
    {
        Set(ParamId.ReverbOn, on);
        Choose(ParamId.ReverbRoom, room);
        Set(ParamId.ReverbMix, mix);
        Set(ParamId.ReverbSize, size);
        Set(ParamId.ReverbDamping, damping);
        Set(ParamId.ReverbFreeze, freeze);

        return Choose(ParamId.ReverbOrder, order);
    }

    public PresetBuilder Output(float volumeDb, float width = 1f)
    {
        Set(ParamId.OutputVolume, volumeDb);

        return Set(ParamId.OutputWidth, width);
    }

    // ---- Modulation ----

    public PresetBuilder Lfo(int index, float rate, string shape = "Sine", float depth = 1f)
    {
        Set(ParameterTable.Lfo(index - 1, LfoParam.Rate), rate);
        Choose(ParameterTable.Lfo(index - 1, LfoParam.Shape), shape);

        return Set(ParameterTable.Lfo(index - 1, LfoParam.Depth), depth);
    }

    /// <summary>The next free matrix slot: a source by its name on the panel, a target parameter, a normalized depth.</summary>
    public PresetBuilder Mod(string source, ParamId target, float depth)
    {
        if (nextModSlot >= ParameterTable.ModSlotCount)
            throw new InvalidOperationException("Every matrix slot is in use.");

        Choose(ParameterTable.ModSlot(nextModSlot, ModSlotParam.Source), source);
        Set(ParameterTable.ModSlot(nextModSlot, ModSlotParam.Target), (int)target + 1);
        Set(ParameterTable.ModSlot(nextModSlot, ModSlotParam.Depth), depth);
        nextModSlot++;

        return this;
    }

    public PresetBuilder Speed(string step, float multiplier = 1f)
    {
        Choose(ParamId.SpeedStep, step);

        return Set(ParamId.SpeedMultiplier, multiplier);
    }

    // ---- Lanes ----

    /// <summary>A smooth seamless loop of <paramref name="seconds"/> around the parameter's current value, ±<paramref name="spread"/> of its normalized range.</summary>
    public PresetBuilder Lane(ParamId id, float seconds, float spread, float speed = 1f)
    {
        var info = LaneInfo(id);
        var count = Math.Max(2, (int)(seconds * AutomationLane.PointsPerSecond));
        var normalized = Randomizer.RandomLoop(rng, count, info.ToNormalized(values[(int)id]), spread);
        var points = new float[count];
        for (var i = 0; i < count; i++)
            points[i] = info.FromNormalized(normalized[i]);

        return AddLane(id, points, speed);
    }

    /// <summary>A lane that holds each value for an equal share of <paramref name="seconds"/>: a melody on a pitch fader, a stepped pattern on anything.</summary>
    public PresetBuilder Steps(ParamId id, float seconds, params float[] steps)
    {
        var info = LaneInfo(id);
        var count = Math.Max(steps.Length, (int)(seconds * AutomationLane.PointsPerSecond));
        var points = new float[count];
        for (var i = 0; i < count; i++)
            points[i] = info.Clamp(steps[i * steps.Length / count]);

        return AddLane(id, points, 1f);
    }

    // ---- Chord sequencer ----

    public PresetBuilder Sequencer(float stepSeconds, string type, string voicing, int octave, string root, params string[] degrees)
    {
        Set(ParamId.ChordSeqOn, true);
        Set(ParamId.ChordSeqSteps, degrees.Length);
        Set(ParamId.ChordSeqStepTime, stepSeconds);
        Choose(ParamId.ChordSeqType, type);
        Choose(ParamId.ChordSeqVoicing, voicing);
        Set(ParamId.ChordSeqOctave, octave);
        Choose(ParamId.ChordSeqRoot, root);
        for (var i = 0; i < degrees.Length; i++)
            Choose(ParameterTable.ChordSeqStep(i), degrees[i]);

        return this;
    }

    public PresetState Build(string name, string description)
    {
        var state = new PresetState { Name = name, Description = description, Seed = seed };
        foreach (var info in ParameterTable.All)
            state.Parameters[info.Id.ToString()] = values[info.Index];

        state.Lanes.AddRange(lanes);
        state.Samples.AddRange(samples.OrderBy(sample => sample.Key));

        return state;
    }

    private static ParamInfo LaneInfo(ParamId id)
    {
        var info = ParameterTable.Get(id);
        if (!info.IsLaneCapable)
            throw new ArgumentException($"{info.Name} cannot carry a lane.");

        return info;
    }

    private PresetBuilder AddLane(ParamId id, float[] points, float speed)
    {
        lanes.RemoveAll(lane => lane.Parameter == id.ToString());
        lanes.Add(new LaneState { Parameter = id.ToString(), Mode = nameof(LaneMode.Automated), SpeedMultiplier = speed, Points = points });

        return this;
    }
}
