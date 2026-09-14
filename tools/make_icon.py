"""Draws the 256x256 Thunderstore icon.

Thunderstore rejects anything that is not exactly 256x256 PNG, and there is no art
asset to crop from, so the icon is drawn: a shield for the safe part, a full stamina
bar across it for the stamina part.

    python tools/make_icon.py
"""
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(HERE), "package", "icon.png")

SIZE = 256
SCALE = 4  # drawn large and downsampled, so the curves are not stair-stepped

BACKGROUND = (38, 34, 30, 255)
SHIELD = (86, 102, 78, 255)
SHIELD_EDGE = (26, 32, 24, 255)
BAR_BED = (28, 25, 22, 255)
STAMINA = (247, 205, 61, 255)


def shield(draw, cx, top, w, h):
    """A heater shield: square shoulders, sides curving in to a point at the bottom."""
    shoulder = h * 0.34
    points = [(cx - w / 2, top), (cx + w / 2, top)]
    steps = 60
    for side in (1, -1):
        edge = []
        for i in range(steps + 1):
            t = i / steps
            edge.append((cx + side * (w / 2) * (1 - t ** 2.1), top + shoulder + t * (h - shoulder)))
        points += edge if side == 1 else list(reversed(edge))
    draw.polygon(points, fill=SHIELD, outline=SHIELD_EDGE, width=5 * SCALE)


image = Image.new("RGBA", (SIZE * SCALE, SIZE * SCALE), BACKGROUND)
draw = ImageDraw.Draw(image)

centre = SIZE * SCALE / 2
shield_w, shield_h = SIZE * SCALE * 0.68, SIZE * SCALE * 0.80
shield_top = SIZE * SCALE * 0.10
shield(draw, centre, shield_top, shield_w, shield_h)

# The bar sits inside the shield, narrower than its shoulders, so the silhouette
# survives at thumbnail size instead of being sliced in two.
bar_w, bar_h = shield_w * 0.66, SIZE * SCALE * 0.15
bar_y = shield_top + shield_h * 0.37
bar = [centre - bar_w / 2, bar_y - bar_h / 2, centre + bar_w / 2, bar_y + bar_h / 2]
draw.rounded_rectangle(bar, radius=bar_h / 2, fill=BAR_BED)

inset = 5 * SCALE
draw.rounded_rectangle(
    [bar[0] + inset, bar[1] + inset, bar[2] - inset, bar[3] - inset],
    radius=(bar_h - 2 * inset) / 2,
    fill=STAMINA,
)

image.resize((SIZE, SIZE), Image.LANCZOS).save(OUT)
print("wrote", OUT)
