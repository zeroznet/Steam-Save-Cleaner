#!/usr/bin/env python3
# scripted/written by Robert Bopko (github.com/zeroznet) with Boba Bott (Claude Opus 5.5)
"""Render the ssc cracktro as an animated SVG for the README.

ssc has no terminal intro, so logo, palettes, release info and scroller text
live here. Rerun after changing them: python3 assets/render-cracktro.py
"""
import argparse
import math
import random
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "assets" / "cracktro.svg"

W, H = 74, 24
CW, CH = 12, 24
LX = 2
# Frame time of the nadrbomz intro this banner copies, so both animate alike.
FRAME = 0.025
NSTARS = 46
FONT = "ui-monospace,SFMono-Regular,Menlo,Consolas,'DejaVu Sans Mono','Liberation Mono',monospace"


def die(message):
    sys.exit(f"render-cracktro: {message}")


def xterm(n):
    if 16 <= n <= 231:
        n -= 16
        levels = (0, 95, 135, 175, 215, 255)
        r, g, b = levels[n // 36], levels[n // 6 % 6], levels[n % 6]
    elif 232 <= n <= 255:
        r = g = b = 8 + 10 * (n - 232)
    else:
        die(f"Unsupported xterm colour: {n}")
    return f"#{r:02x}{g:02x}{b:02x}"


def sec(frames):
    return f"{frames * FRAME:.3f}s"


# ANSI Shadow letters: # is a solid block, 7 F J L = I are double-line box corners and edges.
LETTER_S = ["#######7", "##F====J", "#######7", "L====##I", "#######I", "L======J"]
LETTER_C = [" ######7", "##F====J", "##I     ", "##I     ", "L######7", " L=====J"]
LOGO = [f"{s}  {s2}  {c}".center(70) for s, s2, c in zip(LETTER_S, LETTER_S, LETTER_C)]

DATA = {
    "logo": LOGO,
    "chrome": [93, 129, 165, 201, 207, 213, 219, 225, 231, 195, 159, 123, 87, 51, 45, 39, 33, 27, 57],
    "copper": [53, 54, 55, 56, 57, 93, 129, 165, 201, 165, 129, 93, 57, 56, 55, 54],
    "rainbow": [196, 202, 208, 214, 220, 226, 190, 154, 118, 82, 46, 47, 48, 49, 50, 51, 45, 39, 33, 27,
                21, 57, 93, 129, 165, 201, 200, 199, 198, 197],
    "wave": [2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 2, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0,
             0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2],
    "credits": "-=[ z e r o z n e t   p r e s e n t s ]=-",
    "info": [
        ("RELEASE ....... ", "ssc, steam save cleaner"),
        ("TARGET ........ ", "your steam cloud"),
        ("OPERATOR ...... ", "you"),
        ("RELEASE DATE .. ", "rolling @ main"),
        ("SUPPLIED BY ... ", "zeroznet"),
        ("CRACKED BY .... ", "Boba Bott"),
        ("PROTECTION .... ", "steam guard, scanned with a QR"),
    ],
    "scroll": (
        "*** SSC ***     ZEROZNET PRESENTS STEAM SAVE CLEANER ...     EVERY CLOUD SAVE YOU EVER MADE, "
        "GONE IN ONE RUN ...     NO STEAM CLIENT, NO GAMES INSTALLED, NO PASSWORD ...     "
        "CRACKED, TRAINED AND PACKED BY BOBA BOTT ...     GREETZ TO GABEN (STILL COUNTING TO THREE) * "
        "SEGA FOR THE GABECUBE * VALVE FOR NEVER SHIPPING A DELETE BUTTON * THE STEAMKIT CREW * "
        "STEAM DECK OWNERS WITH 40 GB OF SHADER CACHE * RAZOR 1911 * FAIRLIGHT * FUTURE CREW ...     "
        "NO SAVES WERE HARMED (OK, MACHINARIUM. ONE FILE. 557 BYTES) ...     "
    ),
}


def char(x, y, c, colour, bold=False, extra=""):
    weight = ' font-weight="700"' if bold else ""
    c = {"&": "&amp;", "<": "&lt;", ">": "&gt;"}.get(c, c)
    return (f'<text x="{x * CW + CW / 2:g}" y="{y * CH + CH / 2:g}" fill="{colour}"'
            f'{weight}{extra}>{c}</text>')


def shade(x, y, density, colour):
    return (f'<rect x="{x * CW}" y="{y * CH}" width="{CW}" height="{CH}" fill="{colour}"'
            f' fill-opacity="{density}"/>')


def box_glyph(glyph, x, y):
    """Double-line box drawing glyph as a path, cell (x, y) in pixels."""
    cx, cy, d = x + CW / 2, y + CH / 2, 3
    x1, y1 = x + CW, y + CH
    if glyph == "=":
        return f"M{x} {cy - d}H{x1}M{x} {cy + d}H{x1}"
    if glyph == "I":
        return f"M{cx - d} {y}V{y1}M{cx + d} {y}V{y1}"
    if glyph == "7":
        return f"M{x} {cy - d}H{cx + d}V{y1}M{x} {cy + d}H{cx - d}V{y1}"
    if glyph == "F":
        return f"M{x1} {cy - d}H{cx - d}V{y1}M{x1} {cy + d}H{cx + d}V{y1}"
    if glyph == "J":
        return f"M{x} {cy + d}H{cx + d}V{y}M{x} {cy - d}H{cx - d}V{y}"
    if glyph == "L":
        return f"M{x1} {cy + d}H{cx - d}V{y}M{x1} {cy - d}H{cx + d}V{y}"
    die(f"Unknown logo glyph: {glyph}")


def defs_copper(copper):
    out = []
    period = len(copper) * CW
    for name, sign in (("copper-top", 1), ("copper-bottom", -1)):
        steps = ";".join(f"{sign * i * CW} 0" for i in range(len(copper)))
        rects = "".join(f'<rect x="{i * CW}" width="{CW}" height="{CH}" fill="{xterm(c)}"/>'
                        for i, c in enumerate(copper))
        out.append(
            f'<pattern id="{name}" patternUnits="userSpaceOnUse" width="{period}" height="{CH}">'
            f'{rects}<animateTransform attributeName="patternTransform" type="translate"'
            f' calcMode="discrete" values="{steps}" dur="{sec(len(copper))}" repeatCount="indefinite"/>'
            f'</pattern>')
    return out


def defs_chrome(chrome):
    # Chrome index is 0.5 per column plus 1 per row and slides 0.6 per frame,
    # so the bands run diagonally and one period spans `span` px on both axes.
    span = len(chrome) / (0.5 / CW + 1 / CH)
    stops = "".join(f'<stop offset="{i / len(chrome):.4f}" stop-color="{xterm(c)}"/>'
                    for i, c in enumerate(chrome))
    stops += f'<stop offset="1" stop-color="{xterm(chrome[0])}"/>'
    frames = span / (0.6 / (0.5 / CW + 1 / CH))
    return [
        f'<linearGradient id="chrome" gradientUnits="userSpaceOnUse" spreadMethod="repeat"'
        f' x1="{LX * CW}" y1="{CH}" x2="{LX * CW + span:g}" y2="{CH + span:g}">{stops}'
        f'<animateTransform attributeName="gradientTransform" type="translate"'
        f' from="0 0" to="{span:g} {span:g}" dur="{sec(frames)}" repeatCount="indefinite"/>'
        f'</linearGradient>'
    ]


def layer_stars(rng):
    glyphs = {1: (".", 237), 2: ("·", 244), 3: ("*", 252)}
    speeds = {1: 0.25, 2: 0.5, 3: 1}
    layers = {1: [], 2: [], 3: []}
    for _ in range(NSTARS):
        x, y, layer = rng.random() * W, 8 + int(rng.random() * (H - 8)), 1 + int(rng.random() * 3)
        layers[layer].append((x, y))
    out = []
    for layer, stars in layers.items():
        glyph, colour = glyphs[layer]
        body = "".join(char(x + shift, y, glyph, xterm(colour))
                       for x, y in stars for shift in (0, W))
        out.append(
            f'<g>{body}<animateTransform attributeName="transform" type="translate"'
            f' from="0 0" to="{-W * CW} 0" dur="{sec(W / speeds[layer])}" repeatCount="indefinite"/></g>')
    return out


def layer_bars():
    mid = (LX + 35) * CW
    reveal = (f'<animate attributeName="x" from="{mid}" to="{LX * CW}" dur="{sec(14)}" fill="freeze"/>'
              f'<animate attributeName="width" from="0" to="{70 * CW}" dur="{sec(14)}" fill="freeze"/>')
    return [
        f'<clipPath id="bars-clip"><rect x="{mid}" y="0" width="0" height="{8 * CH}">{reveal}</rect></clipPath>',
        f'<g clip-path="url(#bars-clip)">'
        f'<rect x="{LX * CW}" y="{CH // 2}" width="{70 * CW}" height="{CH // 2}" fill="url(#copper-top)"/>'
        f'<rect x="{LX * CW}" y="{7 * CH}" width="{70 * CW}" height="{CH // 2}" fill="url(#copper-bottom)"/>'
        f'</g>',
    ]


# Each logo cell hides until its reveal frame, flickers as noise for the ten
# frames before it, flashes white for two, then joins the chrome gradient.
def layer_logo(logo, rng):
    reveal = {}
    for y in range(6):
        for x in range(70):
            reveal[y, x] = 6 + int(x * 0.45) + int(rng.random() * 14)
    finals, noise, flash, shine = {}, [], {}, []
    shadow = xterm(60)
    for (y, x), r in reveal.items():
        glyph = logo[y][x]
        if glyph == " ":
            continue
        px, py = (LX + x) * CW, (1 + y) * CH
        if glyph == "#":
            cell = f'<rect x="{px}" y="{py}" width="{CW}" height="{CH}"/>'
            finals.setdefault(r, []).append(f'<rect x="{px}" y="{py}" width="{CW}" height="{CH}" fill="url(#chrome)"/>')
            flash.setdefault(r, []).append(f'<rect x="{px}" y="{py}" width="{CW}" height="{CH}" fill="#fff"/>')
            shine.append(cell)
        else:
            path = box_glyph(glyph, px, py)
            finals.setdefault(r, []).append(f'<path d="{path}" stroke="{shadow}"/>')
            flash.setdefault(r, []).append(f'<path d="{path}" stroke="#fff"/>')
        grey = xterm(236 + int(rng.random() * 8))
        noise.append(
            f'<rect class="nz" x="{px}" y="{py}" width="{CW}" height="{CH}" fill="{grey}"'
            f' style="animation-delay:{sec(r - 10)},-{rng.random() * 0.12:.3f}s"/>')
    out = ['<g fill="none" stroke-width="1.5">']
    for r, cells in sorted(finals.items()):
        out.append(f'<g class="on" style="animation-delay:{sec(r + 2)}">{"".join(cells)}</g>')
    out.append("</g>")
    out.extend(noise)
    out.append('<g fill="none" stroke-width="1.5">')
    for r, cells in sorted(flash.items()):
        out.append(f'<g class="win2" style="animation-delay:{sec(r)}">{"".join(cells)}</g>')
    out.append("</g>")
    # The shine is a white copy of the solid cells seen through a diagonal band
    # sweeping 3 columns per frame, the terminal's x + 2y window.
    lx, ly, height = LX * CW, CH, 6 * CH
    band = f"{lx} {ly} {lx + 36} {ly} {lx + 36 - height} {ly + height} {lx - height} {ly + height}"
    out.append(
        f'<clipPath id="shine-band"><polygon points="{band}">'
        f'<animateTransform attributeName="transform" type="translate" from="{-40 * CW} 0" to="{200 * CW} 0"'
        f' begin="{sec(80)}" dur="{sec(80)}" repeatCount="indefinite"/></polygon></clipPath>')
    out.append(f'<g fill="#fff" clip-path="url(#shine-band)">{"".join(shine)}</g>')
    return out


def layer_credits(credits):
    x0 = LX + (70 - len(credits)) // 2
    out = [backing(x0, 8, len(credits), "on", 41)]
    for i, c in enumerate(credits, start=1):
        if c == " ":
            continue
        letter = c.islower()
        colour = xterm(51) if letter else xterm(240)
        out.append(char(x0 + i - 1, 8, c, colour, bold=letter,
                        extra=f' class="on" style="animation-delay:{sec(40 + math.ceil(i / 2))}"'))
    return out


def layer_info(info):
    out = []
    pink, white = xterm(213), "#fff"
    head = [(5, "░"), (6, "▒"), (7, "▓"), (8, "█")]
    tail = [(23, "█"), (24, "▓"), (25, "▒"), (26, "░")]
    density = {"░": 0.25, "▒": 0.5, "▓": 0.75, "█": 1}
    out.append(backing(5, 10, 22, "on", 55))
    for colour, cls, delay in ((pink, "on", 59), (white, "win4", 55)):
        cells = [shade(x, 10, density[g], colour) for x, g in head + tail]
        cells += [char(9 + i, 10, c, colour, bold=True) for i, c in enumerate(" RELEASE INFO ") if c != " "]
        out.append(f'<g class="{cls}" style="animation-delay:{sec(delay)}">{"".join(cells)}</g>')
    for row, (label, value) in enumerate(info):
        start = 62 + row * 5
        y = 11 + row
        out.append(backing(5, y, 16 + len(value), "on", start))
        labels = "".join(char(5 + i, y, c, xterm(141)) for i, c in enumerate(label) if c != " ")
        out.append(f'<g class="on" style="animation-delay:{sec(start)}">{labels}</g>')
        for j, c in enumerate(value):
            if c != " ":
                out.append(char(21 + j, y, c, white, bold=True,
                                extra=f' class="on" style="animation-delay:{sec(start + j // 3 + 1)}"'))
        for t in range(math.ceil(len(value) / 3)):
            out.append(f'<rect class="win1" x="{(21 + 3 * t) * CW}" y="{y * CH}" width="{CW}" height="{CH}"'
                       f' fill="{xterm(51)}" style="animation-delay:{sec(start + t)}"/>')
    return out


# The terminal scroller moves the text one column per frame, then lifts every
# screen column by its own wave row and paints it its own rainbow colour, both
# drifting with time. Each column here is a clipped copy of one text strip, so
# letters stay upright and split across columns exactly like the cells do.
# Column x runs the column 0 keyframes ahead by x * 3/4 frames (wave) and
# x * 2/3 frames (rainbow), the terminal's x + t*4/3 and x + t*1.5 indexes.
def layer_scroller(scroll, wave, rainbow):
    frames = W + len(scroll)
    steps = ";".join(f"{-i * CW} 0" for i in range(frames + 1))
    wave_keys = "".join(f"{100 * k / len(wave):.3f}%{{transform:translateY({h * CH}px)}}" for k, h in enumerate(wave))
    wave_keys += f"100%{{transform:translateY({wave[0] * CH}px)}}"
    rainbow_keys = "".join(f"{100 * k / len(rainbow):.3f}%{{fill:{xterm(c)}}}" for k, c in enumerate(rainbow))
    rainbow_keys += f"100%{{fill:{xterm(rainbow[0])}}}"
    wave_anim = f"wave {sec(len(wave) * 3 / 4)} step-end infinite"
    rainbow_anim = f"rainbow {sec(len(rainbow) / 1.5)} step-end infinite"
    text = scroll.replace("&", "&amp;").replace("<", "&lt;")
    out = [
        f"<style>.col{{animation:{wave_anim},{rainbow_anim}}}"
        f".edge{{animation:{wave_anim};fill:{xterm(240)}}}"
        f"@keyframes wave{{{wave_keys}}}@keyframes rainbow{{{rainbow_keys}}}</style>",
        f'<defs><text id="strip" class="scroll" x="{W * CW}" y="{(H - 5) * CH + CH / 2:g}"'
        f' textLength="{len(scroll) * CW}" lengthAdjust="spacing" xml:space="preserve">{text}'
        f'<animateTransform attributeName="transform" type="translate" calcMode="discrete" values="{steps}"'
        f' begin="{sec(30)}" dur="{sec(frames + 1)}" repeatCount="indefinite"/></text>',
    ]
    out += [f'<clipPath id="col{x}"><rect x="{x * CW}" y="{(H - 6) * CH}" width="{CW}" height="{6 * CH}"/></clipPath>'
            for x in range(W)]
    out.append("</defs>")
    for x in range(W):
        if x < 3 or x >= W - 3:
            cls, delays = "edge", sec(-x * 3 / 4)
        else:
            cls, delays = "col", f"{sec(-x * 3 / 4)},{sec(-x / 1.5)}"
        out.append(f'<use href="#strip" clip-path="url(#col{x})" class="{cls}" style="animation-delay:{delays}"/>')
    return out


def backing(x, y, cells, cls, delay):
    """Black cells behind a text run, so stars pass under it as in the terminal."""
    return (f'<rect class="{cls}" x="{x * CW}" y="{y * CH}" width="{cells * CW}" height="{CH}" fill="#000"'
            f' style="animation-delay:{sec(delay)}"/>')


STYLE = f"""
text{{font-family:{FONT};font-size:19px;text-anchor:middle;dominant-baseline:central}}
.on{{opacity:0;animation:on 1ms linear forwards}}
.win1,.win2,.win4,.nz{{visibility:hidden}}
.win1{{animation:win {sec(1)} linear}}
.win2{{animation:win {sec(2)} linear}}
.win4{{animation:win {sec(4)} linear}}
.nz{{animation:win {sec(10)} linear,flicker .12s steps(1) infinite}}
.scroll{{font-size:20px;font-weight:700;text-anchor:start}}
@keyframes on{{to{{opacity:1}}}}
@keyframes win{{from,to{{visibility:visible}}}}
@keyframes flicker{{0%{{fill-opacity:.3}}33%{{fill-opacity:.8}}66%{{fill-opacity:.55}}}}
"""


def render():
    data = DATA
    rng = random.Random(1911)
    width, height = W * CW, H * CH
    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" width="{width}" height="{height}"'
        f' role="img" aria-label="ssc cracktro: zeroznet presents ssc, steam save cleaner">',
        "<title>ssc cracktro</title>",
        f"<style>{STYLE}</style>",
        "<defs>", *defs_copper(data["copper"]), *defs_chrome(data["chrome"]), "</defs>",
        f'<rect width="{width}" height="{height}" rx="10" fill="#000"/>',
        *layer_stars(rng),
        *layer_bars(),
        *layer_logo(data["logo"], rng),
        *layer_credits(data["credits"]),
        *layer_info(data["info"]),
        *layer_scroller(data["scroll"], data["wave"], data["rainbow"]),
        "</svg>",
    ]
    return "\n".join(parts) + "\n"


def main():
    argparse.ArgumentParser(description=__doc__.splitlines()[0]).parse_args()
    OUT.write_text(render(), encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({OUT.stat().st_size // 1024} KiB)")


if __name__ == "__main__":
    main()
