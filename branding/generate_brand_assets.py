"""Generate Cassette Motion Pro application branding assets."""

from pathlib import Path
import math

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
BRANDING = ROOT / "branding"
ASSETS = ROOT / "assets"
RESOURCES = ROOT / "src" / "Kinovea" / "Resources"
APP = ROOT / "src" / "Kinovea"

INK = "#F4F7F5"
MUTED = "#98A29D"
PANEL = "#17201D"
BACKGROUND = "#0D1311"
ACCENT = "#B8F34A"
ACCENT_DARK = "#79A828"


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    names = (
        "/System/Library/Fonts/SFNS.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    )
    for name in names:
        path = Path(name)
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def gear_points(cx: float, cy: float, outer: float, inner: float, teeth: int = 12):
    points = []
    for i in range(teeth * 4):
        radius = outer if i % 4 in (0, 1) else inner
        angle = -math.pi / 2 + i * math.pi / (teeth * 2)
        points.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
    return points


def draw_mark(draw: ImageDraw.ImageDraw, box, scale: float = 1.0):
    x0, y0, x1, y1 = box
    cx = (x0 + x1) / 2
    cy = (y0 + y1) / 2
    radius = min(x1 - x0, y1 - y0) * 0.43
    draw.polygon(gear_points(cx, cy, radius, radius * 0.83), fill=ACCENT)
    draw.ellipse((cx - radius * 0.63, cy - radius * 0.63, cx + radius * 0.63, cy + radius * 0.63), fill=BACKGROUND)
    draw.ellipse((cx - radius * 0.27, cy - radius * 0.27, cx + radius * 0.27, cy + radius * 0.27), outline=ACCENT, width=max(2, int(10 * scale)))

    # Forward motion chevrons double as stylized cassette teeth.
    stroke = max(3, int(15 * scale))
    draw.line(
        [(cx - radius * 0.42, cy + radius * 0.10), (cx - radius * 0.08, cy - radius * 0.20), (cx + radius * 0.17, cy + radius * 0.02)],
        fill=INK,
        width=stroke,
        joint="curve",
    )
    draw.line(
        [(cx + radius * 0.02, cy + radius * 0.20), (cx + radius * 0.30, cy - radius * 0.06), (cx + radius * 0.50, cy + radius * 0.12)],
        fill=ACCENT,
        width=stroke,
        joint="curve",
    )


def make_icon():
    image = Image.new("RGBA", (1024, 1024), BACKGROUND)
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((36, 36, 988, 988), radius=210, fill=BACKGROUND, outline=PANEL, width=14)
    draw_mark(draw, (134, 134, 890, 890), scale=2.0)
    return image


def make_horizontal():
    image = Image.new("RGB", (1142, 210), "white")
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((12, 12, 198, 198), radius=38, fill=BACKGROUND)
    draw_mark(draw, (32, 32, 178, 178), scale=0.48)
    draw.text((232, 39), "CASSETTE MOTION", font=font(55, True), fill=BACKGROUND)
    draw.text((234, 112), "PRO", font=font(43, True), fill=ACCENT_DARK)
    draw.text((328, 124), "PROFESSIONAL BIKE FITTING", font=font(20, True), fill="#59645F")
    return image


def make_splash():
    image = Image.new("RGB", (1120, 706), BACKGROUND)
    draw = ImageDraw.Draw(image)

    # Quiet technical grid nods to measurement and motion analysis.
    for x in range(0, 1121, 56):
        draw.line((x, 0, x, 706), fill="#121A17", width=2)
    for y in range(0, 707, 56):
        draw.line((0, y, 1120, y), fill="#121A17", width=2)

    draw.ellipse((712, -190, 1240, 338), outline="#23302B", width=46)
    draw.arc((778, -124, 1174, 272), 18, 208, fill=ACCENT, width=15)
    draw.rounded_rectangle((92, 86, 282, 276), radius=42, fill=PANEL)
    draw_mark(draw, (108, 102, 266, 260), scale=0.52)

    draw.text((92, 330), "CASSETTE", font=font(82, True), fill=INK)
    draw.text((92, 414), "MOTION PRO", font=font(82, True), fill=INK)
    draw.rounded_rectangle((96, 534, 450, 590), radius=28, fill=ACCENT)
    draw.text((126, 546), "PRO BIKE FITTING", font=font(25, True), fill=BACKGROUND)
    draw.text((92, 632), "CASSETTE FIT STUDIO", font=font(20, True), fill=MUTED)
    return image


def main():
    for directory in (BRANDING, ASSETS, RESOURCES):
        directory.mkdir(parents=True, exist_ok=True)

    icon = make_icon()
    horizontal = make_horizontal()
    splash = make_splash()

    icon.save(BRANDING / "cassette-motion-mark.png")
    horizontal.save(BRANDING / "cassette-motion-logo.png")
    splash.save(BRANDING / "cassette-motion-splash.png")

    icon.resize((512, 512), Image.Resampling.LANCZOS).save(ASSETS / "cassette-motion-mark.png")
    horizontal.resize((571, 105), Image.Resampling.LANCZOS).save(RESOURCES / "cassette-motion-logo.png")
    splash.resize((560, 353), Image.Resampling.LANCZOS).save(RESOURCES / "cassette-motion-splash.png")

    icon_path = APP / "cassette-motion-pro.ico"
    icon.save(icon_path, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    icon.save(RESOURCES / "cassette-motion-pro.ico", format="ICO", sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])


if __name__ == "__main__":
    main()
