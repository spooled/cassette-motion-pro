"""Generate the Cassette Motion Pro splash, report logo and mark images.

Uses the sprocket mark from generate_app_icon.py. Needs Playwright with Chromium and
the Inter font installed. The app icon (.ico) comes from generate_app_icon.py.
"""
import os, shutil, sys
from playwright.sync_api import sync_playwright

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from generate_app_icon import mark_svg, LIME, INK  # noqa: E402

ROOT = os.path.dirname(HERE)
RESOURCES = os.path.join(ROOT, "src", "Kinovea", "Resources")
ASSETS = os.path.join(ROOT, "assets")

FONT = "Inter, 'Segoe UI', sans-serif"
MONO = "'DejaVu Sans Mono', Consolas, monospace"

def mark(size):
    return mark_svg(size, "full")

def splash_html():
    return f"""<html><body style="margin:0;width:1120px;height:706px;background:#0b100e;position:relative;overflow:hidden;font-family:{FONT}">
<div style="position:absolute;inset:0;background-image:linear-gradient(#111a16 1px,transparent 1px),linear-gradient(90deg,#111a16 1px,transparent 1px);background-size:56px 56px"></div>
<svg style="position:absolute;right:-170px;top:-170px" width="620" height="620" viewBox="0 0 620 620"><circle cx="310" cy="310" r="250" fill="none" stroke="#17221d" stroke-width="64"/><circle cx="310" cy="310" r="190" fill="none" stroke="{LIME}" stroke-width="10"/><circle cx="310" cy="310" r="120" fill="none" stroke="#1d2b25" stroke-width="3"/></svg>
<div style="position:absolute;left:92px;top:92px">{mark(150)}</div>
<div style="position:absolute;left:92px;top:310px;color:#e4ece7;font-size:92px;font-weight:600;letter-spacing:-3px;line-height:1.02">Cassette</div>
<div style="position:absolute;left:96px;top:424px;color:{LIME};font-family:{MONO};font-size:34px;font-weight:700;letter-spacing:12px">MOTION PRO</div>
<div style="position:absolute;left:96px;top:500px;color:#b6c4bc;font-size:26px">Precision bike-fit video analysis</div>
<div style="position:absolute;left:96px;bottom:70px;color:#8fa198;font-family:{MONO};font-size:17px;letter-spacing:3px">CASSETTE FIT STUDIO</div>
<div style="position:absolute;right:96px;bottom:70px;color:#6f8178;font-family:{MONO};font-size:15px;letter-spacing:1.5px">BUILT ON THE KINOVEA ENGINE</div>
</body></html>"""

def logo_html():
    return f"""<html><body style="margin:0;width:1142px;height:210px;background:#fff;position:relative;font-family:{FONT}">
<div style="position:absolute;left:18px;top:17px">{mark(176)}</div>
<div style="position:absolute;left:236px;top:36px;color:#131f1b;font-size:84px;font-weight:600;letter-spacing:-2.5px;line-height:1">Cassette</div>
<div style="position:absolute;left:238px;top:132px;display:flex;align-items:center;gap:22px"><span style="color:#5f8f13;font-family:{MONO};font-size:30px;font-weight:700;letter-spacing:9px">MOTION PRO</span><span style="color:#5c6962;font-size:24px">Professional bike fitting</span></div>
</body></html>"""

def mark_html():
    return f"""<html><body style="margin:0;width:1024px;height:1024px;background:transparent">{mark(1024)}</body></html>"""

def shot(page, html, w, h, path, scale=1, transparent=False):
    page.set_viewport_size({"width": w, "height": h})
    page.set_content(html)
    page.wait_for_timeout(250)
    page.screenshot(path=path, clip={"x": 0, "y": 0, "width": w, "height": h}, omit_background=transparent)

def main():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(device_scale_factor=1)
        shot(page, splash_html(), 1120, 706, os.path.join(HERE, "cassette-motion-splash.png"))
        shot(page, logo_html(), 1142, 210, os.path.join(HERE, "cassette-motion-logo.png"))
        shot(page, mark_html(), 1024, 1024, os.path.join(HERE, "cassette-motion-mark.png"), transparent=True)
        browser.close()
    shutil.copy(os.path.join(HERE, "cassette-motion-splash.png"), os.path.join(RESOURCES, "cassette-motion-splash.png"))
    shutil.copy(os.path.join(HERE, "cassette-motion-logo.png"), os.path.join(RESOURCES, "cassette-motion-logo.png"))
    from PIL import Image
    Image.open(os.path.join(HERE, "cassette-motion-mark.png")).resize((512, 512), Image.LANCZOS).save(os.path.join(ASSETS, "cassette-motion-mark.png"))
    print("brand images written")

if __name__ == "__main__":
    main()
