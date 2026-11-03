"""Render the sprocket mark to cassette-motion-pro.ico (run from branding/, then copy the ico into src/Kinovea and src/Kinovea/Resources)."""
import math, struct, io, sys
from playwright.sync_api import sync_playwright
from PIL import Image

LIME="#bff44a"; INK="#0b100e"; LINE="#2a3832"

def cog(n, cx, cy, ro, ri):
    pts=[]
    w=2*math.pi/n
    for i in range(n):
        a0=w*i-math.pi/2
        for f,r in [(0,ri),(0.12,ri),(0.22,ro),(0.5,ro),(0.60,ri)]:
            a=a0+f*w
            pts.append("%.2f,%.2f"%(cx+r*math.cos(a),cy+r*math.sin(a)))
    return " ".join(pts)

def mark_svg(size, detail):
    # detail: 'full' (>=48), 'mid' (24-32), 'tiny' (16)
    if detail=='full':
        body=f'''<rect x="1" y="1" width="94" height="94" rx="22" fill="{INK}" stroke="{LINE}" stroke-width="2"/>
<g transform="translate(58 48) scale(0.78) translate(-52 -48)"><polygon points="{cog(14,52,48,33,27)}" fill="none" stroke="{LIME}" stroke-width="6" stroke-linejoin="round"/></g>
<circle cx="58" cy="48" r="12" fill="none" stroke="{LIME}" stroke-width="5"/>
<circle cx="58" cy="48" r="3" fill="{LIME}"/>
<line x1="9" y1="38" x2="21" y2="38" stroke="{LIME}" stroke-width="5" stroke-linecap="round"/>
<line x1="9" y1="50" x2="17" y2="50" stroke="{LIME}" stroke-width="5" stroke-linecap="round" opacity="0.6"/>
<line x1="9" y1="62" x2="21" y2="62" stroke="{LIME}" stroke-width="5" stroke-linecap="round" opacity="0.35"/>'''
    elif detail=='mid':
        body=f'''<rect x="1" y="1" width="94" height="94" rx="20" fill="{INK}"/>
<g transform="translate(60 48) scale(0.95) translate(-52 -48)"><polygon points="{cog(10,52,48,33,25)}" fill="none" stroke="{LIME}" stroke-width="8" stroke-linejoin="round"/></g>
<circle cx="60" cy="48" r="11" fill="{LIME}"/>
<line x1="8" y1="38" x2="20" y2="38" stroke="{LIME}" stroke-width="7" stroke-linecap="round"/>
<line x1="8" y1="58" x2="20" y2="58" stroke="{LIME}" stroke-width="7" stroke-linecap="round" opacity="0.55"/>'''
    else:
        body=f'''<rect x="0" y="0" width="96" height="96" rx="20" fill="{INK}"/>
<polygon points="{cog(8,48,48,40,31)}" fill="none" stroke="{LIME}" stroke-width="11" stroke-linejoin="round"/>
<circle cx="48" cy="48" r="12" fill="{LIME}"/>'''
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 96 96">{body}</svg>'

def render(page, svg, size):
    page.set_viewport_size({"width":size,"height":size})
    page.set_content(f'<html><body style="margin:0;background:transparent">{svg}</body></html>')
    png=page.screenshot(omit_background=True, clip={"x":0,"y":0,"width":size,"height":size})
    return png

def write_ico(path, frames):
    # frames: list of (size, png_bytes)
    out=io.BytesIO()
    out.write(struct.pack('<HHH',0,1,len(frames)))
    offset=6+16*len(frames)
    entries=b''; data=b''
    for size,png in frames:
        w=0 if size>=256 else size
        entries+=struct.pack('<BBBBHHII',w,w,0,0,1,32,len(png),offset+len(data))
        data+=png
    out.write(entries); out.write(data)
    open(path,'wb').write(out.getvalue())

if __name__=="__main__":
    with sync_playwright() as p:
        b=p.chromium.launch(); page=b.new_page()
        plan=[(16,'tiny'),(24,'mid'),(32,'mid'),(48,'full'),(64,'full'),(128,'full'),(256,'full')]
        frames=[]
        for size,detail in plan:
            png=render(page, mark_svg(size,detail), size)
            open(f"icon-{size}.png","wb").write(png)
            frames.append((size,png))
        write_ico("cassette-motion-pro.ico", frames)
        open("mark-sprocket.svg","w").write(mark_svg(512,'full'))
        b.close()
    print("ok")
