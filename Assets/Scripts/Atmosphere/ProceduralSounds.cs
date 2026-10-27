using System;
using System.Collections.Generic;
using UnityEngine;

// 외부 사운드 파일 없이 효과음을 코드로 합성한다. 한 번 만든 클립은 캐시해서 재사용한다.
// 나중에 실제 사운드 에셋으로 바꿀 때는 해당 메서드가 에셋의 AudioClip을 돌려주게 고치면 된다.
public static class ProceduralSounds
{
    public const int SampleRate = 44100;
    const double TwoPi = Math.PI * 2.0;

    // 도(C4) 레 미 파 솔 라 시, 높은 도(C5)
    static readonly float[] Scale = { 261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 493.88f, 523.25f };

    static readonly Dictionary<string, AudioClip> cache = new();

    // 만드는 데 오래 걸리는 클립을 미리 만들어 둔다 (인트로 암전 중에 호출)
    public static void Warmup()
    {
        Ambience();
        Song();
        for (int i = 0; i < 7; i++) PianoNote(i);
    }

    public static AudioClip Get(SoundKind kind) => kind switch
    {
        SoundKind.DoorSlam => DoorSlam(),
        SoundKind.ChairDrag => ChairDrag(),
        SoundKind.Static => Static(),
        SoundKind.StaticBreath => StaticBreath(),
        SoundKind.GhostNote => GhostNote(),
        SoundKind.Stinger => Stinger(),
        SoundKind.LightPop => LightPop(),
        SoundKind.Chime => Chime(),
        SoundKind.Knock => Knock(),
        SoundKind.WoodCreak => WoodCreak(),
        SoundKind.Drip => Drip(),
        _ => null
    };

    // ───────── 음악 ─────────

    public static AudioClip PianoNote(int index) => Cached($"Piano_{index}", () =>
    {
        float[] d = Buffer(2.4f);
        AddTone(d, 0, Scale[index], 1f, 2.4f);
        return d;
    }, 0.8f);

    public static AudioClip PianoDissonant() => Cached("PianoDissonant", () =>
    {
        float[] d = Buffer(2.2f);
        foreach (float f in new[] { 130.81f, 246.94f, 261.63f, 369.99f }) AddTone(d, 0, f, 0.6f, 2.2f);
        return d;
    }, 0.8f);

    public static AudioClip GhostNote() => Cached("GhostNote", () =>
    {
        float[] d = Buffer(4.5f);
        AddTone(d, 0, 130.81f, 1f, 4.5f, 0.45f);
        AddTone(d, 0, 138.59f, 0.35f, 4.5f, 0.45f);
        return d;
    }, 0.85f);

    // 서진이의 노래: 피아노 퍼즐의 6음(미 솔 라 솔 미 레)을 늘린 곡을 낡은 테이프처럼 만든다
    public static AudioClip Song() => Cached("Song", () =>
    {
        int[] melody = { 2, 4, 5, 4, 2, 1, 2, 4, 5, 7, 6, 5, 2, 4, 5, 4, 2, 1, 1, 2, 0 };
        float[] beats = { 1, 1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 2, 1, 1, 4 };
        float[] bass = { 130.81f, 98.00f, 110.00f, 87.31f, 130.81f, 98.00f, 98.00f, 130.81f }; // 4박마다
        const float beat = 0.45f;

        float total = 0f;
        foreach (float b in beats) total += b;
        float[] clean = Buffer(total * beat + 3f);

        float t = 0f;
        for (int i = 0; i < melody.Length; i++)
        {
            AddTone(clean, (int)(t * SampleRate), Scale[melody[i]], 1f, beats[i] * beat + 1.2f);
            t += beats[i] * beat;
        }
        for (int i = 0; i < bass.Length; i++)
            AddTone(clean, (int)(i * 4 * beat * SampleRate), bass[i], 0.45f, 4 * beat + 1f, 0.7f);

        // 와우·플러터(음높이 흔들림), 고음 손실, 테이프 잡음
        float[] output = new float[clean.Length];
        var rng = new System.Random(17);
        var tone = Biquad.LowPass(3200f, 0.7f);
        var hiss = Biquad.LowPass(6000f, 0.5f);
        double pos = 0;
        for (int i = 0; i < output.Length; i++)
        {
            int p0 = (int)pos;
            if (p0 + 1 >= clean.Length) break;
            float sample = Mathf.Lerp(clean[p0], clean[p0 + 1], (float)(pos - p0));
            output[i] = tone.Process(sample) + hiss.Process(Noise(rng)) * 0.02f;
            double time = i / (double)SampleRate;
            pos += 1.0 + 0.0025 * Math.Sin(TwoPi * 0.55 * time) + 0.0008 * Math.Sin(TwoPi * 6.5 * time);
        }
        return output;
    }, 0.75f);

    public static AudioClip Chime() => Cached("Chime", () =>
    {
        float[] d = Buffer(4.6f);
        float[] notes = { 659.25f, 523.25f, 587.33f, 392.00f };
        for (int i = 0; i < notes.Length; i++) AddBell(d, (int)(i * 0.65f * SampleRate), notes[i], 1f, 2.6f);
        return d;
    }, 0.7f);

    // ───────── 배경음 ─────────

    // 24초 반복: 바람(브라운 노이즈) + 낮게 웅웅거리는 저음. 끝과 처음을 겹쳐서 이음새가 없게 한다.
    public static AudioClip Ambience() => Cached("Ambience", () =>
    {
        const int loopSeconds = 24, fadeSeconds = 3;
        int n = loopSeconds * SampleRate, xf = fadeSeconds * SampleRate;
        float[] g = new float[n + xf];
        var rng = new System.Random(3);
        var windLow = Biquad.LowPass(380f, 0.6f);
        var windHigh = Biquad.BandPass(700f, 0.8f);
        float brown = 0f;
        for (int i = 0; i < g.Length; i++)
        {
            double t = i / (double)SampleRate;
            brown = Mathf.Clamp(brown * 0.998f + Noise(rng) * 0.04f, -1f, 1f);
            float gust = 0.55f + 0.3f * (float)Math.Sin(TwoPi * t / 12.0) + 0.15f * (float)Math.Sin(TwoPi * t / 8.0 + 1.3);
            float wind = (windLow.Process(brown) + windHigh.Process(Noise(rng)) * 0.03f * gust) * gust;
            float drone = 0.10f * (float)Math.Sin(TwoPi * 49.0 * t) + 0.08f * (float)Math.Sin(TwoPi * 49.5 * t)
                        + 0.04f * (float)Math.Sin(TwoPi * 73.5 * t) * (0.6f + 0.4f * (float)Math.Sin(TwoPi * t / 6.0));
            g[i] = wind * 0.9f + drone;
        }
        float[] d = new float[n];
        for (int i = 0; i < n; i++) d[i] = i < xf ? Mathf.Lerp(g[n + i], g[i], i / (float)xf) : g[i];
        return d;
    }, 0.5f, loop: true);

    public static AudioClip FluorescentBuzz() => Cached("FluorescentBuzz", () =>
    {
        float[] d = Buffer(1f);
        var rng = new System.Random(5);
        var hiss = Biquad.BandPass(3000f, 2f);
        for (int i = 0; i < d.Length; i++)
        {
            double t = i / (double)SampleRate;
            d[i] = 0.5f * (float)Math.Sin(TwoPi * 120 * t) + 0.25f * (float)Math.Sin(TwoPi * 240 * t)
                 + 0.15f * (float)Math.Sin(TwoPi * 360 * t + 0.5) + hiss.Process(Noise(rng)) * 0.15f;
        }
        return d;
    }, 0.6f, loop: true);

    public static AudioClip TapeHiss() => Cached("TapeHiss", () =>
    {
        float[] d = Buffer(2f);
        var rng = new System.Random(9);
        var high = Biquad.LowPass(7000f, 0.7f);
        var low = Biquad.LowPass(300f, 0.7f);
        for (int i = 0; i < d.Length; i++)
        {
            float n = Noise(rng);
            d[i] = (high.Process(n) - low.Process(n)) * 0.6f + 0.05f * (float)Math.Sin(TwoPi * 60 * i / SampleRate);
        }
        return d;
    }, 0.5f, loop: true);

    // ───────── 문·물건 ─────────

    public static AudioClip Footstep(int variant) => Cached($"Footstep_{variant}", () =>
    {
        var rng = new System.Random(100 + variant);
        float[] d = Buffer(0.18f);
        var thump = Biquad.LowPass(350f + variant * 40f, 0.9f);
        var scuff = Biquad.BandPass(1800f + variant * 150f, 2f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            float n = Noise(rng);
            d[i] = thump.Process(n) * Mathf.Exp(-t * 35f) + scuff.Process(n) * Mathf.Exp(-t * 90f) * 0.25f;
        }
        return d;
    }, 0.7f);

    public static AudioClip DoorCreak() => Creak("DoorCreak", 1.3f, 55f, 650f, 1350f, 7);
    public static AudioClip WoodCreak() => Creak("WoodCreak", 0.9f, 35f, 420f, 900f, 11);

    public static AudioClip DoorSlam() => Cached("DoorSlam", () =>
    {
        var rng = new System.Random(21);
        float[] d = Buffer(1.4f);
        var crack = Biquad.LowPass(500f, 0.7f);
        double phase = 0;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            phase += TwoPi * (70f * Mathf.Exp(-t * 2f) + 38f) / SampleRate;
            d[i] = (float)Math.Sin(phase) * Mathf.Exp(-t * 4.5f) + crack.Process(Noise(rng)) * Mathf.Exp(-t * 18f) * 1.2f;
        }
        return d;
    }, 0.95f);

    public static AudioClip DoorRattle() => Hits("DoorRattle", 0.6f, new[] { 0f, 0.11f, 0.2f, 0.33f }, 1700f, 2300f, 400f, 31);
    public static AudioClip UnlockClick() => Hits("UnlockClick", 0.3f, new[] { 0f, 0.09f }, 3200f, 4100f, 900f, 33);

    public static AudioClip SlideDoor() => Rumble("SlideDoor", 0.9f, 250f, 20f, 41);
    public static AudioClip DrawerSlide() => Rumble("DrawerSlide", 0.35f, 420f, 28f, 43);

    public static AudioClip ChairDrag() => Cached("ChairDrag", () =>
    {
        var rng = new System.Random(51);
        float[] d = Buffer(1.8f);
        var low = Biquad.BandPass(520f, 5f);
        var high = Biquad.BandPass(1250f, 6f);
        double phase = 0;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            phase += (28f + 8f * Mathf.Sin(t * 5f) + Noise(rng) * 3f) / SampleRate;
            float pulse = 0f;
            if (phase >= 1.0) { phase -= 1.0; pulse = 0.8f; }
            float x = pulse + Noise(rng) * 0.15f;
            float env = Mathf.SmoothStep(0f, 1f, t / 0.15f) * Mathf.SmoothStep(0f, 1f, (1.8f - t) / 0.3f);
            d[i] = (low.Process(x) + 0.5f * high.Process(x)) * env;
        }
        return d;
    }, 0.9f);

    public static AudioClip Pickup() => Cached("Pickup", () =>
    {
        var rng = new System.Random(61);
        float[] d = Buffer(0.3f);
        var rustle = Biquad.BandPass(3500f, 1.2f);
        var tick = Biquad.BandPass(5000f, 3f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            float n = Noise(rng);
            d[i] = rustle.Process(n) * Mathf.Exp(-t * 20f) + tick.Process(n) * Mathf.Exp(-t * 200f);
        }
        return d;
    }, 0.6f);

    public static AudioClip PaperRustle() => Cached("PaperRustle", () =>
    {
        var rng = new System.Random(63);
        float[] d = Buffer(0.5f);
        var band = Biquad.BandPass(4000f, 0.8f);
        float[] starts = { 0f, 0.07f, 0.16f, 0.24f, 0.33f };
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate, env = 0f;
            foreach (float s in starts) if (t >= s) env += Mathf.Exp(-(t - s) * 30f);
            d[i] = band.Process(Noise(rng)) * env;
        }
        return d;
    }, 0.6f);

    public static AudioClip SwitchClick() => Hits("SwitchClick", 0.1f, new[] { 0f }, 3000f, 2200f, 1200f, 71);
    public static AudioClip ButtonPress() => Hits("ButtonPress", 0.15f, new[] { 0f }, 1500f, 900f, 600f, 73);

    public static AudioClip FuseClunk() => Cached("FuseClunk", () =>
    {
        var rng = new System.Random(81);
        float[] d = Buffer(1.2f);
        var buzz = Biquad.LowPass(2500f, 0.7f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            float thump = (float)Math.Sin(TwoPi * 70 * t) * Mathf.Exp(-t * 12f);
            float saw = 2f * (100f * t % 1f) - 1f;
            float crackle = t < 0.5f && rng.NextDouble() < 0.002 ? Noise(rng) * 2f : 0f;
            d[i] = thump + buzz.Process(saw * 0.35f + crackle) * Mathf.Exp(-t * 3f);
        }
        return d;
    }, 0.85f);

    // ───────── 연출 ─────────

    public static AudioClip Static() => Cached("Static", () => RenderStatic(2.5f, 91), 0.7f);

    public static AudioClip StaticBreath() => Cached("StaticBreath", () =>
    {
        float[] d = RenderStatic(4f, 93);
        for (int i = 0; i < d.Length; i++) d[i] *= Mathf.Exp(-i / (float)SampleRate * 0.9f);
        AddBreath(d, (int)(0.8f * SampleRate), 95);
        return d;
    }, 0.8f);

    public static AudioClip Stinger() => Cached("Stinger", () =>
    {
        var rng = new System.Random(101);
        float[] d = Buffer(2.4f);
        var rumble = Biquad.LowPass(200f, 0.7f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            float swell = Mathf.Pow(Mathf.Clamp01(t / 2f), 2f) * (t > 2f ? Mathf.Clamp01(1f - (t - 2f) / 0.4f) : 1f);
            float x = 0.4f * (float)Math.Sin(TwoPi * 55 * t) + 0.35f * (float)Math.Sin(TwoPi * 58.27 * t)
                    + 0.08f * (float)Math.Sin(TwoPi * 1244.5 * t) + 0.07f * (float)Math.Sin(TwoPi * 1318.5 * t)
                    + rumble.Process(Noise(rng)) * 0.5f;
            d[i] = x * swell;
        }
        return d;
    }, 0.8f);

    public static AudioClip LightPop() => Cached("LightPop", () =>
    {
        var rng = new System.Random(111);
        float[] d = Buffer(0.6f);
        var pop = Biquad.LowPass(2000f, 0.7f);
        float[] pings = { 0.05f, 0.11f, 0.19f, 0.27f };
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            float x = pop.Process(Noise(rng)) * Mathf.Exp(-t * 80f) + (float)Math.Sin(TwoPi * 90 * t) * Mathf.Exp(-t * 30f) * 0.6f;
            for (int k = 0; k < pings.Length; k++)
            {
                float dt = t - pings[k];
                if (dt >= 0f) x += (float)Math.Sin(TwoPi * (3200 + k * 700) * dt) * Mathf.Exp(-dt * 40f) * 0.12f;
            }
            d[i] = x;
        }
        return d;
    }, 0.8f);

    public static AudioClip Knock() => Cached("Knock", () =>
    {
        var rng = new System.Random(121);
        float[] d = Buffer(1.1f);
        var body = Biquad.LowPass(600f, 0.8f);
        float[] hits = { 0f, 0.32f, 0.6f };
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate, x = 0f;
            foreach (float h in hits)
            {
                float dt = t - h;
                if (dt >= 0f) x += (float)Math.Sin(TwoPi * 110 * dt) * Mathf.Exp(-dt * 25f) + Noise(rng) * Mathf.Exp(-dt * 60f) * 0.6f;
            }
            d[i] = body.Process(x);
        }
        return d;
    }, 0.8f);

    public static AudioClip Drip() => Cached("Drip", () =>
    {
        float[] d = Buffer(0.5f);
        AddDrip(d, 0, 1f);
        AddDrip(d, (int)(0.18f * SampleRate), 0.35f);
        return d;
    }, 0.6f);

    // 테이프 속 목소리: 알아들을 수 없는 웅얼거림. 대사는 자막으로 보여 준다.
    public static AudioClip Murmur(int seed, float seconds) => Cached($"Murmur_{seed}_{seconds:F1}", () =>
    {
        var rng = new System.Random(seed * 31 + 7);
        float[] d = Buffer(seconds);
        var muffle = Biquad.LowPass(2800f, 0.7f);
        Biquad formant1 = null, formant2 = null;
        double phase = 0;
        float pitch = 235f;
        int syllableStart = 0, syllableLength = 0, gapLength = 0, syllableCount = 0;
        for (int i = 0; i < d.Length; i++)
        {
            int pos = i - syllableStart;
            if (pos >= syllableLength + gapLength)
            {
                syllableStart = i;
                pos = 0;
                syllableCount++;
                syllableLength = (int)(SampleRate * (0.08 + rng.NextDouble() * 0.16));
                gapLength = (int)(SampleRate * (syllableCount % 7 == 0 ? 0.25 + rng.NextDouble() * 0.2 : 0.015 + rng.NextDouble() * 0.05));
                formant1 = Biquad.BandPass(450f + (float)rng.NextDouble() * 450f, 5f);
                formant2 = Biquad.BandPass(1000f + (float)rng.NextDouble() * 900f, 6f);
                pitch = Mathf.Clamp(pitch + (float)(rng.NextDouble() - 0.5) * 30f, 200f, 280f);
            }
            float env = pos < syllableLength ? Mathf.Sin(Mathf.PI * pos / syllableLength) : 0f;
            double t = i / (double)SampleRate;
            phase += pitch * (1.0 + 0.01 * Math.Sin(TwoPi * 5.5 * t)) / SampleRate;
            float saw = (float)(2.0 * (phase - Math.Floor(phase + 0.5)));
            float x = saw * 0.7f + Noise(rng) * 0.1f;
            d[i] = muffle.Process((formant1.Process(x) + 0.7f * formant2.Process(x)) * env);
        }
        return d;
    }, 0.6f);

    // ───────── 합성 도구 ─────────

    static AudioClip Cached(string key, Func<float[]> render, float peak, bool loop = false)
    {
        if (cache.TryGetValue(key, out var clip) && clip != null) return clip;
        float[] data = render();
        Normalize(data, peak, fadeOut: !loop);
        clip = AudioClip.Create(key, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        cache[key] = clip;
        return clip;
    }

    static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * SampleRate)];

    static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

    static void Normalize(float[] d, float peak, bool fadeOut)
    {
        float max = 0f;
        foreach (float s in d) max = Mathf.Max(max, Mathf.Abs(s));
        if (max < 1e-6f) return;
        float k = peak / max;
        for (int i = 0; i < d.Length; i++) d[i] *= k;
        if (!fadeOut) return;
        int fade = Mathf.Min(256, d.Length);
        for (int i = 0; i < fade; i++) d[d.Length - 1 - i] *= i / (float)fade;
    }

    // 피아노 비슷한 음: 배음을 쌓고 높은 배음일수록 빨리 사라지게 한다
    static void AddTone(float[] d, int start, float freq, float gain, float length, float decayScale = 1f)
    {
        int n = Mathf.Min(d.Length - start, Mathf.CeilToInt(length * SampleRate));
        if (n <= 0) return;
        const int attack = 200;
        int release = Mathf.Min(n, 2000);
        for (int h = 1; h <= 7; h++)
        {
            double f = freq * h * (1.0 + 0.0004 * h * h);
            if (f > 9000.0) break;
            float env = gain / Mathf.Pow(h, 1.5f);
            float decay = Mathf.Exp(-(0.8f + 0.9f * h) * decayScale / SampleRate);
            // 사인파를 점화식으로 계산 (매 샘플 Math.Sin을 부르지 않기 위해)
            double w = TwoPi * f / SampleRate, c = 2.0 * Math.Cos(w), y1 = Math.Sin(-w), y2 = Math.Sin(-2.0 * w);
            for (int i = 0; i < n; i++)
            {
                double y = c * y1 - y2;
                y2 = y1;
                y1 = y;
                float e = env;
                if (i < attack) e *= i / (float)attack;
                if (i > n - release) e *= (n - i) / (float)release;
                d[start + i] += (float)y * e;
                env *= decay;
            }
        }
        // 해머가 현을 치는 짧은 소리
        var rng = new System.Random((int)freq);
        var hammer = Biquad.LowPass(freq * 3f, 0.7f);
        int hn = Mathf.Min(n, 600);
        for (int i = 0; i < hn; i++) d[start + i] += hammer.Process(Noise(rng)) * gain * 0.25f * (1f - i / (float)hn);
    }

    static void AddBell(float[] d, int start, float freq, float gain, float length)
    {
        float[] ratios = { 1f, 2f, 2.76f, 5.4f };
        float[] amps = { 1f, 0.45f, 0.3f, 0.12f };
        float[] decays = { 1.1f, 1.8f, 2.6f, 4.5f };
        int n = Mathf.Min(d.Length - start, Mathf.CeilToInt(length * SampleRate));
        for (int k = 0; k < ratios.Length; k++)
        {
            double w = TwoPi * freq * ratios[k] / SampleRate, c = 2.0 * Math.Cos(w), y1 = Math.Sin(-w), y2 = Math.Sin(-2.0 * w);
            float env = gain * amps[k], decay = Mathf.Exp(-decays[k] / SampleRate);
            for (int i = 0; i < n; i++)
            {
                double y = c * y1 - y2;
                y2 = y1;
                y1 = y;
                d[start + i] += (float)y * env * Mathf.Min(1f, i / 100f);
                env *= decay;
            }
        }
    }

    static void AddDrip(float[] d, int start, float gain)
    {
        double phase = 0;
        for (int i = start; i < d.Length; i++)
        {
            float t = (i - start) / (float)SampleRate;
            phase += TwoPi * (900f + 1400f * Mathf.Min(t / 0.03f, 1f)) / SampleRate;
            d[i] += (float)Math.Sin(phase) * Mathf.Exp(-t * 28f) * gain;
        }
    }

    static void AddBreath(float[] d, int start, int seed)
    {
        var rng = new System.Random(seed);
        var inhale = Biquad.BandPass(1300f, 1.2f);
        var exhale = Biquad.BandPass(800f, 1f);
        int n = Mathf.Min(d.Length - start, (int)(3.2f * SampleRate));
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float a = t < 1.3f ? Mathf.Sin(Mathf.PI * t / 1.3f) : 0f;
            float b = t > 1.6f && t < 3.1f ? Mathf.Sin(Mathf.PI * (t - 1.6f) / 1.5f) : 0f;
            float noise = Noise(rng);
            d[start + i] += inhale.Process(noise) * a * 0.9f + exhale.Process(noise) * b;
        }
    }

    static float[] RenderStatic(float seconds, int seed)
    {
        var rng = new System.Random(seed);
        float[] d = Buffer(seconds);
        var band = Biquad.BandPass(2500f, 0.6f);
        float burst = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            if (rng.NextDouble() < 0.0015) burst = 1f;
            burst *= 0.995f;
            float t = i / (float)SampleRate;
            float level = 0.5f + 0.5f * Mathf.Sin(t * 3.7f) * Mathf.Sin(t * 1.3f);
            d[i] = band.Process(Noise(rng) * 0.35f * level + Noise(rng) * burst);
        }
        return d;
    }

    // 삐걱 소리: 짧은 마찰 펄스가 공명체를 울리는 방식
    static AudioClip Creak(string key, float seconds, float baseRate, float f1, float f2, int seed) => Cached(key, () =>
    {
        var rng = new System.Random(seed);
        float[] d = Buffer(seconds);
        var r1 = Biquad.BandPass(f1, 12f);
        var r2 = Biquad.BandPass(f2, 10f);
        var r3 = Biquad.BandPass(f2 * 1.8f, 8f);
        double phase = 0;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            phase += (baseRate + baseRate * 0.6f * Mathf.Sin(t * 3.1f) + baseRate * 0.35f * Mathf.Sin(t * 7.3f + 1f)) / SampleRate;
            float impulse = 0f;
            if (phase >= 1.0) { phase -= 1.0; impulse = 1f + Noise(rng) * 0.3f; }
            float x = impulse + Noise(rng) * 0.02f;
            float env = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds)), 0.7f);
            d[i] = (r1.Process(x) + 0.6f * r2.Process(x) + 0.3f * r3.Process(x)) * env;
        }
        return d;
    }, 0.8f);

    // 딸깍·덜컹 소리: 짧은 타격 여러 번
    static AudioClip Hits(string key, float seconds, float[] hitTimes, float bandFreq, float pingFreq, float bodyFreq, int seed) => Cached(key, () =>
    {
        var rng = new System.Random(seed);
        float[] d = Buffer(seconds);
        var band = Biquad.BandPass(bandFreq, 4f);
        var body = Biquad.LowPass(bodyFreq, 0.8f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate, x = 0f, ping = 0f;
            foreach (float h in hitTimes)
            {
                float dt = t - h;
                if (dt < 0f) continue;
                x += Mathf.Exp(-dt * 60f) * Noise(rng);
                ping += (float)Math.Sin(TwoPi * pingFreq * dt) * Mathf.Exp(-dt * 40f) * 0.3f;
            }
            d[i] = band.Process(x) * 0.8f + body.Process(x) + ping;
        }
        return d;
    }, 0.8f);

    // 드르륵 소리: 바퀴가 홈을 지나가는 듯한 진폭 변조 노이즈
    static AudioClip Rumble(string key, float seconds, float freq, float bumpRate, int seed) => Cached(key, () =>
    {
        var rng = new System.Random(seed);
        float[] d = Buffer(seconds);
        var band = Biquad.BandPass(freq, 1.5f);
        var low = Biquad.LowPass(freq * 0.5f, 0.7f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)SampleRate;
            float bumps = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Mathf.PI * bumpRate * t));
            float env = Mathf.SmoothStep(0f, 1f, t / 0.05f) * Mathf.SmoothStep(0f, 1f, (seconds - t) / 0.12f);
            float n = Noise(rng);
            d[i] = (band.Process(n) + low.Process(n) * 0.5f) * bumps * env;
        }
        return d;
    }, 0.7f);

    // RBJ 쿡북 2차 필터
    sealed class Biquad
    {
        float b0, b1, b2, a1, a2, x1, x2, y1, y2;

        public static Biquad LowPass(float freq, float q)
        {
            float w = 2f * Mathf.PI * freq / SampleRate, cos = Mathf.Cos(w), alpha = Mathf.Sin(w) / (2f * q), a0 = 1f + alpha;
            return new Biquad { b0 = (1f - cos) / 2f / a0, b1 = (1f - cos) / a0, b2 = (1f - cos) / 2f / a0, a1 = -2f * cos / a0, a2 = (1f - alpha) / a0 };
        }

        public static Biquad BandPass(float freq, float q)
        {
            float w = 2f * Mathf.PI * freq / SampleRate, cos = Mathf.Cos(w), alpha = Mathf.Sin(w) / (2f * q), a0 = 1f + alpha;
            return new Biquad { b0 = alpha / a0, b1 = 0f, b2 = -alpha / a0, a1 = -2f * cos / a0, a2 = (1f - alpha) / a0 };
        }

        public float Process(float x)
        {
            float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1;
            x1 = x;
            y2 = y1;
            y1 = y;
            return y;
        }
    }
}
