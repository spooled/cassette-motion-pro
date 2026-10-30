"""Render the Cassette Motion Pro player and tool icons.

Each icon replaces the upstream Kinovea PNG of the same name (same pixel size) when
the build overlays branding/player-icons onto Kinovea.ScreenManager/Resources.
Needs Playwright with Chromium: python branding/generate_player_icons.py
"""
import os
from playwright.sync_api import sync_playwright

INK = "#1c2823"
LIME = "#bff44a"
LIME_INK = "#101a08"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "player-icons")

def stroke(body, w=1.9, color=INK):
    return f'<g fill="none" stroke="{color}" stroke-width="{w}" stroke-linecap="round" stroke-linejoin="round">{body}</g>'

# name: (width, height, viewBox, body)
ICONS = {
    # transport, 24x18
    "flatprev3": (24, 18, "0 0 24 18", stroke('<path d="M7 4v10"/><path d="M17 4 9.5 9 17 14Z"/>')),
    "flatnext3": (24, 18, "0 0 24 18", stroke('<path d="M17 4v10"/><path d="M7 4l7.5 5L7 14Z"/>')),
    "flatstart3": (24, 18, "0 0 24 18", stroke('<path d="M5 4v10"/><path d="M12.5 4 8 9l4.5 5Z"/><path d="M19 4l-4.5 5 4.5 5Z"/>')),
    "flatend3": (24, 18, "0 0 24 18", stroke('<path d="M19 4v10"/><path d="M11.5 4 16 9l-4.5 5Z"/><path d="M5 4l4.5 5L5 14Z"/>')),
    "flatloop3": (24, 18, "0 0 24 18", stroke('<path d="M17.5 6.2A5.6 5.6 0 0 0 7 7"/><path d="M6.5 11.8A5.6 5.6 0 0 0 17 11"/><path d="M17.8 3.2v3.4h-3.4"/><path d="M6.2 14.8v-3.4h3.4"/>')),
    "flatswap3d": (24, 18, "0 0 24 18", stroke('<path d="M4 6.5h15"/><path d="M15.5 3l3.5 3.5-3.5 3.5"/><path d="M20 12H5"/><path d="M8.5 8.5 5 12l3.5 3.5"/>')),
    "flatsync3c": (24, 18, "0 0 24 18", stroke('<circle cx="9" cy="9" r="4.5"/><circle cx="15" cy="9" r="4.5"/>')),
    # play / pause, 40x30: lime tile
    "flatplay40x30": (40, 30, "0 0 40 30",
        f'<rect x="1" y="1" width="38" height="28" rx="7" fill="{LIME}"/><path d="M16 8.5 27 15 16 21.5Z" fill="{LIME_INK}" stroke="{LIME_INK}" stroke-width="1.6" stroke-linejoin="round"/>'),
    "flatplay40x301": (40, 30, "0 0 40 30",
        f'<rect x="1" y="1" width="38" height="28" rx="7" fill="{LIME}"/><path d="M16 8.5 27 15 16 21.5Z" fill="{LIME_INK}" stroke="{LIME_INK}" stroke-width="1.6" stroke-linejoin="round"/>'),
    "flatpause3b": (40, 30, "0 0 40 30",
        f'<rect x="1" y="1" width="38" height="28" rx="7" fill="{LIME}"/><rect x="14" y="8.5" width="4.2" height="13" rx="1.2" fill="{LIME_INK}"/><rect x="21.8" y="8.5" width="4.2" height="13" rx="1.2" fill="{LIME_INK}"/>'),
    # drawing tools, 22x22
    "line6": (22, 22, "0 0 24 24", stroke('<path d="M5 19 19 5"/><circle cx="5" cy="19" r="2.4"/><circle cx="19" cy="5" r="2.4"/>', 2)),
    "angle5": (22, 22, "0 0 24 24", stroke('<path d="M3.5 19H20.5"/><path d="M3.5 19 16 5"/><path d="M10.5 19A7.5 7.5 0 0 0 8.2 13.6"/>', 2)),
    "cross5": (22, 22, "0 0 24 24", stroke('<path d="M12 3v6M12 15v6M3 12h6M15 12h6"/><circle cx="12" cy="12" r="1.6"/>', 2)),
    "magnifier2": (22, 22, "0 0 24 24", stroke('<circle cx="10.5" cy="10.5" r="6.5"/><path d="M15.5 15.5 21 21"/>', 2)),
    "chrono5": (22, 22, "0 0 24 24", stroke('<circle cx="12" cy="13.5" r="7.5"/><path d="M9.5 3h5"/><path d="M12 13.5V9"/><path d="M18 7l1.6-1.6"/>', 2)),
    # small tools, 16x16
    "handtool4": (16, 16, "0 0 24 24", stroke('<path d="M12 3v18M3 12h18"/><path d="M9 6l3-3 3 3M9 18l3 3 3-3M6 9l-3 3 3 3M18 9l3 3-3 3"/>', 2.3)),
    "rectangle": (16, 16, "0 0 24 24", stroke('<rect x="4" y="6" width="16" height="12" rx="1.5"/>', 2.4)),
    "circle-16": (16, 16, "0 0 24 24", stroke('<circle cx="12" cy="12" r="8"/>', 2.4)),
    "polyline2": (16, 16, "0 0 24 24", stroke('<path d="M4 18 9 8l6 8 5-11"/>', 2.4)),
    "xline": (16, 16, "0 0 24 24", stroke('<path d="M3 12h18"/><path d="M3 8v8M21 8v8"/>', 2.4)),
    "yline": (16, 16, "0 0 24 24", stroke('<path d="M12 3v18"/><path d="M8 3h8M8 21h8"/>', 2.4)),
    "tracking": (16, 16, "0 0 24 24", stroke('<circle cx="12" cy="12" r="7.5"/><circle cx="12" cy="12" r="2"/><path d="M12 2v4M12 18v4M2 12h4M18 12h4"/>', 2.2)),
}

def main():
    os.makedirs(OUT, exist_ok=True)
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(device_scale_factor=1)
        for name, (w, h, box, body) in ICONS.items():
            svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="{box}">{body}</svg>'
            page.set_viewport_size({"width": w, "height": h})
            page.set_content(f'<html><body style="margin:0;background:transparent">{svg}</body></html>')
            page.screenshot(path=os.path.join(OUT, name + ".png"), omit_background=True, clip={"x": 0, "y": 0, "width": w, "height": h})
        browser.close()
    print("wrote", len(ICONS), "icons to", OUT)

if __name__ == "__main__":
    main()
