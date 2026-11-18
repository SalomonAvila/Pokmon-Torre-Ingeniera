#!/usr/bin/env python3
"""
Convierte las pistas de música (por ejemplo, las de ElevenLabs) en bucles listos para GameAudio.

Para cada tema:
  1. Recorta el silencio y el desvanecimiento del final (la parte que baja de volumen).
  2. Busca, en los últimos segundos útiles, el punto que más se parece al inicio del bucle (envolvente
     de ataques) y lo ajusta a nivel de muestra con correlación cruzada.
  3. Mezcla en el inicio medio segundo de lo que sigue tras ese punto (empalme con fundido cruzado),
     así al repetir no se oye el corte.
  4. Si el tema tiene introducción (inicio_bucle > 0), la guarda aparte como <tema>_intro: GameAudio la
     toca una vez y luego encadena el bucle.
  5. Exporta OGG Vorbis (Unity lo importa directamente) a Assets/Resources/Audio/Musica/.

Uso (desde la carpeta del proyecto Unity):
  python3 Tools/Audio/preparar_musica.py <carpeta_origen>
La carpeta debe tener 1.wav … 7.wav en el orden de TEMAS (o <tema>.wav). Requiere numpy y ffmpeg.
"""
import os
import subprocess
import sys
import wave

import numpy as np

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Audio", "Musica")

# (tema, inicio del bucle en segundos: 0 = todo se repite; > 0 = lo anterior es una intro)
TEMAS = [
    ("titulo", 19.0),
    ("atrio", 0.0),
    ("ascensores", 0.0),
    ("escalera", 0.0),
    ("piso3", 0.0),
    ("combate", 0.0),
    ("victoria", 0.0),
]

CROSSFADE = 0.5   # s de empalme
SEARCH = 14.0     # s del final útil donde se busca el punto de corte
WINDOW = 4.0      # s de envolvente que se comparan
HOP = 0.01        # resolución de la envolvente


def load(path):
    with wave.open(path) as w:
        rate, ch, width = w.getframerate(), w.getnchannels(), w.getsampwidth()
        raw = w.readframes(w.getnframes())
    dtype = {2: np.int16, 4: np.int32}[width]
    data = np.frombuffer(raw, dtype=dtype).reshape(-1, ch).astype(np.float64) / float(np.iinfo(dtype).max)
    return data, rate


def envelope(mono, rate):
    hop = int(rate * HOP)
    frames = len(mono) // hop
    rms = np.sqrt(np.mean(mono[: frames * hop].reshape(frames, hop) ** 2, axis=1))
    return rms


def useful_end(rms):
    """Último fotograma antes del desvanecimiento final (cuando cae por debajo del 45 % de la mediana)."""
    level = np.median(rms)
    smooth = np.convolve(rms, np.ones(50) / 50, mode="same")
    above = np.where(smooth > 0.45 * level)[0]
    return above[-1] if len(above) else len(rms) - 1


def onsets(rms):
    on = np.maximum(0, np.diff(rms, prepend=rms[0]))
    return on - on.mean()


def best_loop_end(on, start, end_limit):
    """Fotograma e (en [end_limit - SEARCH, end_limit]) cuyo tramo siguiente se parece más al inicio."""
    w = int(WINDOW / HOP)
    ref = on[start: start + w]
    ref = (ref - ref.mean()) / (ref.std() + 1e-9)
    best, best_score = end_limit, -np.inf
    lo = max(start + w, end_limit - int(SEARCH / HOP))
    for e in range(lo, end_limit - w):
        seg = on[e: e + w]
        seg = (seg - seg.mean()) / (seg.std() + 1e-9)
        score = float(np.dot(ref, seg)) / w
        if score > best_score:
            best, best_score = e, score
    return best, best_score


def refine(mono, rate, start, end):
    """Ajusta el corte a la muestra comparando la forma de onda (±25 ms)."""
    span = int(rate * 0.025)
    w = int(rate * 0.05)
    ref = mono[start: start + w]
    best, best_score = end, -np.inf
    for e in range(end - span, end + span):
        seg = mono[e: e + w]
        if len(seg) < w:
            break
        score = float(np.dot(ref, seg))
        if score > best_score:
            best, best_score = e, score
    return best


def make_loop(data, rate, start_s):
    mono = data.mean(axis=1)
    rms = envelope(mono, rate)
    on = onsets(rms)
    start_f = int(start_s / HOP)
    x = int(CROSSFADE / HOP)
    end_limit = useful_end(rms) - x
    end_f, score = best_loop_end(on, start_f, end_limit)

    start = int(start_s * rate)
    end = refine(mono, rate, start, int(end_f * HOP * rate))
    fade = int(CROSSFADE * rate)

    loop = data[start:end].copy()
    tail = data[end: end + fade]
    ramp = np.linspace(0, 1, len(tail))[:, None]
    loop[: len(tail)] = loop[: len(tail)] * ramp + tail * (1 - ramp)
    intro = data[:start] if start > 0 else None
    return loop, intro, (end - start) / rate, score


def write_ogg(data, rate, path):
    tmp = path + ".tmp.wav"
    pcm = (np.clip(data, -1, 1) * 32767).astype(np.int16)
    with wave.open(tmp, "wb") as w:
        w.setnchannels(data.shape[1])
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm.tobytes())
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", tmp, "-ac", "2", "-c:a", "libvorbis",
                    "-q:a", "5", path], check=True)
    os.remove(tmp)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)
    src = sys.argv[1]
    os.makedirs(OUT_DIR, exist_ok=True)
    for index, (name, start_s) in enumerate(TEMAS, start=1):
        path = next((p for p in (os.path.join(src, f"{name}.wav"), os.path.join(src, f"{index}.wav"))
                     if os.path.exists(p)), None)
        if path is None:
            print(f"{name}: no hay archivo, se deja el actual.")
            continue
        data, rate = load(path)
        loop, intro, seconds, score = make_loop(data, rate, start_s)
        write_ogg(loop, rate, os.path.join(OUT_DIR, f"{name}.ogg"))
        if intro is not None:
            write_ogg(intro, rate, os.path.join(OUT_DIR, f"{name}_intro.ogg"))
        print(f"{name}: {os.path.basename(path)} → bucle de {seconds:.1f} s"
              f"{f' + intro de {start_s:.1f} s' if intro is not None else ''} (parecido {score:.2f})")


if __name__ == "__main__":
    main()
