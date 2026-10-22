using Noumenon.Engine.Scales;

namespace Noumenon.Engine.Modulation;

/// <summary>
/// One of MF's two pitch faders, P1 or P2, as the engine hears it: the lane-or-static fader value
/// in semitones or, while the fader's MIDI button is on, the last MIDI note played (relative to
/// middle C, optionally quantized to the scale), whichever moved last — a note takes over from the
/// fader, and moving the fader (its set value: a drag, host automation, a learned controller, a
/// preset) takes over from the note until the next one, so the slider is never dead. Switching the
/// MIDI button on keeps the fader until a note arrives, and pitch bend times the bend range is
/// added to either while the button is on. Then the glide knob's portamento. The result drives
/// the oscillators through the routing rows, the sampler and the Resochord through their Mod
/// menus, and the chord-select input.
/// </summary>
internal sealed class PitchFader
{
    private float value;
    private float target;
    private float set = float.NaN;
    private bool followingNote;

    public float Value => value;

    public float Target => target;

    public bool FollowingNote => followingNote;

    public void Snap(float semitones)
    {
        value = semitones;
        target = semitones;
    }

    /// <param name="setSemitones">The fader's set value (the bank's); a change is the fader being moved.</param>
    /// <param name="faderSemitones">What the fader plays: the set value through its lane, the mod-wheel box and the chord sequencer.</param>
    /// <param name="noteArrived">A note-on arrived in this tick's MIDI drain.</param>
    public void Tick(double seconds, float setSemitones, float faderSemitones, bool midiFollow, bool noteArrived, MidiInput midi, float bendRange, float glideSeconds, Scale scale, int root)
    {
        if (setSemitones != set)
        {
            set = setSemitones;
            followingNote = false;
        }

        followingNote = midiFollow && (followingNote || (noteArrived && midi.LastNote >= 0));
        target = faderSemitones;
        if (followingNote)
        {
            var note = midi.LastNote - (float)Dsp.Shared.Tuning.MiddleC;
            if (!scale.IsIdentity)
                note = scale.Quantize(note, root);

            target = note;
        }

        if (midiFollow)
            target += midi.Bend * bendRange;

        if (glideSeconds <= 0.0005f)
        {
            value = target;
            return;
        }

        var coeff = 1f - MathF.Exp((float)(-seconds / (glideSeconds / 3.0)));
        value += (target - value) * coeff;
        if (MathF.Abs(target - value) <= 1e-4f)
            value = target;
    }
}
