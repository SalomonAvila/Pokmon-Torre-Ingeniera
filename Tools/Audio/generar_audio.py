#!/usr/bin/env python3
"""
Genera la música y los efectos provisionales de Torre Ingeniería (Etapa 9) con síntesis chiptune
propia: ondas cuadradas, triangulares y ruido, como un chip de 8 bits. Todo es original y
determinista (semillas fijas): volver a ejecutarlo produce los mismos archivos.

Salida (WAV mono, 16 bits, 22 050 Hz), con los ids que usa GameAudio:
  Assets/Resources/Audio/Musica/<tema>.wav   (7 temas)
  Assets/Resources/Audio/Efectos/<efecto>.wav (30 efectos)

Uso, desde la carpeta del proyecto Unity:  python3 Tools/Audio/generar_audio.py [--musica]
Por defecto solo genera los efectos: la música definitiva viene de preparar_musica.py. Con --musica
vuelve a escribir también los temas provisionales (y pisaría la música buena si tiene el mismo nombre).
Requiere numpy. Para sustituir un sonido basta con dejar otro archivo con el mismo nombre.
"""
import os
import random
import wave

import numpy as np

RATE = 22050
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
MUSIC_DIR = os.path.join(ROOT, "Assets", "Resources", "Audio", "Musica")
SFX_DIR = os.path.join(ROOT, "Assets", "Resources", "Audio", "Efectos")


# ---------------------------------------------------------------- osciladores

def t_axis(seconds):
    return np.arange(int(RATE * seconds)) / RATE


def square(freq, seconds, duty=0.5):
    t = t_axis(seconds)
    if np.isscalar(freq):
        phase = (t * freq) % 1.0
    else:
        phase = np.cumsum(freq / RATE) % 1.0
    return np.where(phase < duty, 1.0, -1.0)


def triangle(freq, seconds):
    t = t_axis(seconds)
    phase = (t * freq) % 1.0 if np.isscalar(freq) else np.cumsum(freq / RATE) % 1.0
    return 4.0 * np.abs(phase - 0.5) - 1.0


def noise(seconds, seed=0):
    rng = np.random.default_rng(seed)
    # Ruido "de chip": muestras que se repiten unas cuantas veces (suena más granulado).
    n = int(RATE * seconds)
    raw = rng.uniform(-1, 1, n // 4 + 1)
    return np.repeat(raw, 4)[:n]


def envelope(n, attack=0.005, decay=0.0, sustain=1.0, release=0.05):
    env = np.ones(n) * sustain
    a = min(n, int(attack * RATE))
    r = min(n - a, int(release * RATE))
    if a > 0:
        env[:a] = np.linspace(0, 1, a)
    d = min(n - a - r, int(decay * RATE))
    if d > 0:
        env[a:a + d] = np.linspace(1, sustain, d)
    if r > 0:
        env[n - r:] = np.linspace(env[n - r - 1] if n - r - 1 >= 0 else sustain, 0, r)
    return env


def sweep(f0, f1, seconds):
    return np.geomspace(max(f0, 1), max(f1, 1), int(RATE * seconds))


def note_freq(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


def tone(midi, seconds, wave_kind="square", duty=0.5, vol=1.0, release=0.04):
    f = note_freq(midi)
    w = square(f, seconds, duty) if wave_kind == "square" else triangle(f, seconds)
    return w * envelope(len(w), release=release) * vol


def concat(*parts):
    return np.concatenate([p for p in parts if len(p)])


def silence(seconds):
    return np.zeros(int(RATE * seconds))


def save(path, data, peak=0.8):
    data = np.asarray(data, dtype=np.float64)
    m = np.max(np.abs(data)) if len(data) else 0
    if m > 0:
        data = data / m * peak
    pcm = (data * 32767).astype(np.int16)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(pcm.tobytes())


# ---------------------------------------------------------------- efectos

def sfx():
    s = {}
    s["cursor"] = tone(84, 0.035, duty=0.25, release=0.01)
    s["aceptar"] = concat(tone(79, 0.04, duty=0.25), tone(86, 0.06, duty=0.25))
    s["cancelar"] = concat(tone(81, 0.04, duty=0.25), tone(74, 0.07, duty=0.25))
    s["abrir_menu"] = concat(*[tone(m, 0.03, duty=0.25) for m in (72, 76, 79)])
    s["cerrar_menu"] = concat(*[tone(m, 0.03, duty=0.25) for m in (79, 76, 72)])
    s["error"] = square(110, 0.18, 0.5) * envelope(int(RATE * 0.18), release=0.05)
    s["choque"] = (triangle(sweep(140, 60, 0.08), 0.08) + 0.4 * noise(0.08, 1)) * envelope(int(RATE * 0.08), release=0.05)
    s["paso"] = noise(0.03, 2) * envelope(int(RATE * 0.03), release=0.025) * 0.6
    s["puerta"] = noise(0.35, 3) * np.linspace(0.1, 1, int(RATE * 0.35)) * envelope(int(RATE * 0.35), release=0.15) * 0.6
    s["ascensor"] = concat(tone(88, 0.25, "tri", release=0.2), tone(84, 0.45, "tri", release=0.4))
    s["objeto"] = concat(*[tone(m, 0.08, duty=0.25) for m in (72, 76, 79)], tone(84, 0.25, duty=0.25, release=0.15))
    s["alerta"] = concat(tone(88, 0.05, duty=0.125), silence(0.02), tone(88, 0.12, duty=0.125, release=0.08))
    enc = square(sweep(200, 1600, 0.5), 0.5, 0.5) * envelope(int(RATE * 0.5), release=0.1)
    s["encuentro"] = concat(enc, enc * 0.7)
    s["curar"] = concat(*[tone(m, 0.09, duty=0.25) for m in (72, 76, 79, 84, 88)], tone(91, 0.3, duty=0.25, release=0.25))
    s["guardar"] = concat(tone(79, 0.1), tone(84, 0.1), tone(88, 0.3, release=0.25))
    s["compra"] = concat(tone(93, 0.05, duty=0.25), tone(98, 0.25, duty=0.25, release=0.2))
    s["sello"] = concat(*[tone(m, 0.12) for m in (72, 72, 72)], tone(79, 0.5, release=0.3))
    def hit(seconds, low, high, seed, vol=1.0):
        n = int(RATE * seconds)
        body = 0.6 * noise(seconds, seed) + 0.6 * square(sweep(high, low, seconds), seconds, 0.5)
        return body * envelope(n, release=seconds * 0.8) * vol
    s["golpe"] = hit(0.18, 60, 300, 4)
    s["golpe_eficaz"] = concat(hit(0.12, 80, 400, 5), hit(0.25, 50, 300, 6))
    s["golpe_poco"] = hit(0.12, 100, 250, 7, 0.5)
    s["pokebola_lanzar"] = square(sweep(1200, 300, 0.3), 0.3, 0.25) * envelope(int(RATE * 0.3), release=0.1)
    s["pokebola_sacudida"] = concat(tone(60, 0.05, "tri"), silence(0.05), tone(57, 0.08, "tri"))
    s["pokebola_salir"] = concat(noise(0.08, 8) * envelope(int(RATE * 0.08), release=0.07), tone(84, 0.1, duty=0.25))
    s["captura"] = concat(*[tone(m, 0.1) for m in (79, 83, 86)], tone(91, 0.5, release=0.3))
    s["huida"] = square(sweep(900, 200, 0.35), 0.35, 0.25) * envelope(int(RATE * 0.35), release=0.15)
    s["desmayo"] = concat(*[tone(m, 0.16, "tri") for m in (72, 67, 64, 60)], tone(55, 0.5, "tri", release=0.4))
    s["subir_nivel"] = concat(*[tone(m, 0.08) for m in (76, 79, 84, 79, 84)], tone(88, 0.35, release=0.25))
    s["exp"] = square(sweep(400, 1400, 0.6), 0.6, 0.125) * envelope(int(RATE * 0.6), release=0.1) * 0.6
    s["baja_stat"] = square(sweep(900, 250, 0.4), 0.4, 0.5) * envelope(int(RATE * 0.4), release=0.1)
    s["sube_stat"] = square(sweep(250, 900, 0.4), 0.4, 0.5) * envelope(int(RATE * 0.4), release=0.1)
    return s


# ---------------------------------------------------------------- música

MAJOR = [0, 2, 4, 5, 7, 9, 11]
MINOR = [0, 2, 3, 5, 7, 8, 10]


def chord_tones(root, scale, degree):
    """Tríada sobre el grado (0 = I) de la escala, en semitonos desde la tónica."""
    return [root + scale[(degree + k) % 7] + 12 * ((degree + k) // 7) for k in (0, 2, 4)]


def render_theme(name, root, scale, progression, bpm, bars_per_chord=1, repeats=2, seed=1,
                 lead_duty=0.5, drums=True, arp=False, loop=True):
    rng = random.Random(seed)
    beat = 60.0 / bpm
    step = beat / 2  # corcheas
    steps_per_bar = 8
    total_steps = len(progression) * bars_per_chord * steps_per_bar
    n_total = int(RATE * step * total_steps)
    lead = np.zeros(n_total)
    bass = np.zeros(n_total)
    perc = np.zeros(n_total)

    # Un motivo rítmico por tema (1 = nota nueva, 0 = mantener), repetido con variaciones.
    motif = [1, 0, 1, 1, 0, 1, 1, 0] if not arp else [1] * 8
    prev = root + 12
    for i in range(total_steps):
        bar = i // steps_per_bar
        degree = progression[(bar // bars_per_chord) % len(progression)]
        tones = chord_tones(root, scale, degree)
        start = int(RATE * step * i)

        # Bajo: tónica del acorde en negras (triangular, una octava abajo).
        if i % 2 == 0:
            b = tones[0] - 12 if (i // 2) % 2 == 0 else tones[0] - 5
            seg = tone(b, step * 2 * 0.9, "tri", vol=0.9, release=0.03)
            bass[start:start + len(seg)] += seg[:n_total - start]

        # Melodía: notas del acorde o de paso, sin saltos grandes.
        if motif[i % 8] or (rng.random() < 0.15):
            if arp:
                target = tones[i % 3] + 12
            else:
                candidates = [t + 12 for t in tones] + [root + 12 + scale[rng.randrange(7)]]
                target = min(candidates, key=lambda c: abs(c - prev) + rng.random() * 4)
                if rng.random() < 0.2:
                    target += 12 if target < root + 12 else 0
            prev = target
            length = step * (2 if (i + 1 < total_steps and motif[(i + 1) % 8] == 0 and not arp) else 1) * 0.92
            seg = tone(target, length, duty=lead_duty, vol=0.55, release=0.03)
            lead[start:start + len(seg)] += seg[:n_total - start]

        # Batería: bombo en 1 y 3, caja en 2 y 4, charles en corcheas.
        if drums:
            pos = i % 8
            if pos in (0, 4):
                k = triangle(sweep(120, 40, 0.1), 0.1) * envelope(int(RATE * 0.1), release=0.08)
                perc[start:start + len(k)] += k[:n_total - start] * 0.8
            if pos in (2, 6):
                sn = noise(0.09, i) * envelope(int(RATE * 0.09), release=0.08)
                perc[start:start + len(sn)] += sn[:n_total - start] * 0.45
            hh = noise(0.02, i + 999) * envelope(int(RATE * 0.02), release=0.018)
            perc[start:start + len(hh)] += hh[:n_total - start] * 0.18

    section = lead * 0.5 + bass * 0.35 + perc * 0.4
    if loop:
        song = np.tile(section, repeats)
    else:
        song = concat(section, silence(0.3))
        song *= envelope(len(song), release=0.6)
    return song


THEMES = {
    # nombre: (tónica MIDI, escala, progresión de grados, bpm, compases por acorde, kwargs)
    "titulo": dict(root=60, scale=MAJOR, progression=[0, 4, 5, 3, 0, 4, 3, 4], bpm=100, seed=11),
    "atrio": dict(root=67, scale=MAJOR, progression=[0, 3, 4, 0, 5, 3, 1, 4], bpm=120, seed=12, lead_duty=0.25),
    "ascensores": dict(root=65, scale=MAJOR, progression=[0, 5, 1, 4], bpm=96, bars_per_chord=2, seed=13,
                       lead_duty=0.125),
    "escalera": dict(root=57, scale=MINOR, progression=[0, 5, 2, 6], bpm=132, seed=14, arp=True, lead_duty=0.25),
    "piso3": dict(root=62, scale=MINOR, progression=[0, 3, 5, 4, 0, 3, 6, 4], bpm=110, seed=15),
    "combate": dict(root=64, scale=MINOR, progression=[0, 0, 5, 6, 0, 0, 3, 4], bpm=150, seed=16, lead_duty=0.25),
    "victoria": dict(root=60, scale=MAJOR, progression=[0, 3, 4, 0], bpm=120, seed=17, loop=False),
}


def main():
    for name, data in sfx().items():
        save(os.path.join(SFX_DIR, f"{name}.wav"), data, peak=0.7)
    import sys
    if "--musica" in sys.argv:
        for name, kwargs in THEMES.items():
            save(os.path.join(MUSIC_DIR, f"{name}.wav"), render_theme(name, **kwargs), peak=0.6)
        print(f"{len(THEMES)} temas provisionales en {os.path.normpath(MUSIC_DIR)}.")
    print(f"Listo: {len(sfx())} efectos en {os.path.normpath(SFX_DIR)}.")


if __name__ == "__main__":
    main()
