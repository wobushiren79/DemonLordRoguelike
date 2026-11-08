from pathlib import Path
import sys, json
from PIL import Image, ImageDraw

ROOT=Path(sys.argv[1])
ASSETS=Path(__file__).resolve().parents[3]/'资源'/'生物'/'骷髅'/'装备'
NEAREST=Image.Resampling.NEAREST
HATS={'Equipment_Helmet','Equipment_Hood','Equipment_Crown','Equipment_Headwrap','Equipment_Headwrap_Tail','Equipment_Cloth_Cap','Equipment_Tricorne','Equipment_Kabuto'}
SPECS={
 1:{'Equipment_Helmet':(61,47,18,11)},
 2:{'Equipment_Helmet':(61,47,18,13)},
 3:{'Equipment_Crown':(62,48,17,7)},
 4:{'Equipment_Helmet':(61,47,18,12)},
 5:{'Equipment_Hood':(58,46,22,26)},
 8:{'Equipment_Headwrap':(60,47,19,12),'Equipment_Headwrap_Tail':(54,56,10,17)},
 9:{'Equipment_Hood':(58,47,22,24)},
 12:{'Equipment_Cloth_Cap':(60,48,19,11)},
 13:{'Equipment_Tricorne':(58,47,25,11)},
 15:{'Equipment_Kabuto':(60,47,20,10)},
 16:{'Equipment_Crown':(63,48,16,7)},
}

def load(i):
    d=ROOT/f'{i:02}'
    m=json.loads((d/'manifest.json').read_text(encoding='utf-8'))
    return d,m

def composite(d,m,replacements=None,body_only=False):
    canvas=Image.new('RGBA',(128,128))
    for c in m['cels']:
        if not c['visible'] or (body_only and c['layer'].startswith('Equipment_')): continue
        c=(replacements or {}).get(c['layer'],c)
        canvas.alpha_composite(Image.open(d/c['png']).convert('RGBA'),(c['x'],c['y']))
    return canvas

def on_gray(im):
    bg=Image.new('RGBA',im.size,'#65676e'); bg.alpha_composite(im); return bg.convert('RGB')

def inspect():
    sheet=Image.new('RGB',(1200,1120),'#33353b'); draw=ImageDraw.Draw(sheet)
    for i in range(1,17):
        d,m=load(i)
        im=composite(d,m)
        im.save(d/'before.png')
        x=((i-1)%4)*300;y=((i-1)//4)*280
        sheet.paste(on_gray(im.crop((42,38,88,79))).resize((276,246),NEAREST),(x+12,y+24))
        draw.text((x+14,y+6),f'{i:02} BEFORE',fill='white')
        for c in m['cels']:
            if c['layer'] in HATS: print(i,c['layer'],c['x'],c['y'],c['width'],c['height'])
    sheet.save(ROOT/'before-heads.png')
    d,m=load(1)
    on_gray(composite(d,m,body_only=True).crop((55,46,82,73))).resize((540,540),NEAREST).save(ROOT/'head-base.png')

def prepare():
    sheet=Image.new('RGB',(1200,1120),'#33353b'); draw=ImageDraw.Draw(sheet)
    for i in range(1,17):
        d,m=load(i); repl={}
        hc=next(c for c in m['cels'] if c['layer']=='Head_1')
        head=Image.open(d/hc['png']).convert('RGBA')
        for c in m['cels']:
            spec=SPECS.get(i,{}).get(c['layer'])
            if not spec: continue
            x,y,w,h=spec
            old=Image.open(d/c['png']).convert('RGBA')
            new=old.resize((w,h),NEAREST)
            if c['layer']=='Equipment_Hood':
                for hy in range(head.height):
                    for hx in range(head.width):
                        px,py=hx+hc['x'],hy+hc['y']
                        if px>=66 and 56<=py<=67 and head.getpixel((hx,hy))[3] and 0<=px-x<w and 0<=py-y<h:
                            new.putpixel((px-x,py-y),(0,0,0,0))
            png='fixed_'+c['layer']+'.png'; new.save(d/png)
            repl[c['layer']]={'layer':c['layer'],'png':png,'x':x,'y':y}
            assert {p for p in new.get_flattened_data() if p[3]} <= set(old.get_flattened_data()), 'Unexpected new color'
        if repl:
            manifest={'width':128,'height':128,'mode':'replace','frame':21,'cels':list(repl.values())}
            (d/'replacement.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
        im=composite(d,m,repl); im.save(d/'planned.png')
        eyes=[]
        covered=[]
        for hy in range(head.height):
            for hx in range(head.width):
                color=head.getpixel((hx,hy)); xy=(hx+hc['x'],hy+hc['y'])
                if color[:3]==(255,0,0): eyes.append((xy,im.getpixel(xy)))
                if color[3] and xy[0]>=67 and xy[1]>=58 and color!=im.getpixel(xy): covered.append(xy)
        print(i,'eyes',eyes,'face_overlaps',covered)
        x=((i-1)%4)*300;y=((i-1)//4)*280
        sheet.paste(on_gray(im.crop((42,38,88,79))).resize((276,246),NEAREST),(x+12,y+24))
        draw.text((x+14,y+6),f'{i:02} AFTER' if repl else f'{i:02} UNCHANGED',fill='white')
    sheet.save(ROOT/'after-heads.png')

def verify_and_preview():
    overview=Image.new('RGB',(1536,1536),'#65676e')
    head_sheet=Image.new('RGB',(1200,1120),'#33353b'); draw=ImageDraw.Draw(head_sheet)
    count=0
    for i in range(1,17):
        d,m=load(i)
        native=Image.open(d/'native.png').convert('RGBA')
        planned=Image.open(d/'planned.png').convert('RGBA')
        assert native.size==(128,128) and native.tobytes()==planned.tobytes(), f'Native render differs: {i}'
        if i in SPECS:
            am=json.loads((d/'after'/'manifest.json').read_text(encoding='utf-8'))
            assert m['width']==am['width'] and m['height']==am['height'] and am['frame']==21
            assert [c['layer'] for c in m['cels']]==[c['layer'] for c in am['cels']]
            for c,a in zip(m['cels'],am['cels']):
                for key in ['layer','opacity','z_index','visible','blend_mode']:
                    assert c[key]==a[key], f'Changed metadata: {i} {key}'
                if c['layer'] not in SPECS[i]:
                    assert all(c[k]==a[k] for k in ['x','y','width','height'])
                    assert Image.open(d/c['png']).convert('RGBA').tobytes()==Image.open(d/'after'/a['png']).convert('RGBA').tobytes(), f'Non-headgear cel changed: {i} {c["layer"]}'
                else:
                    assert (a['x'],a['y'],a['width'],a['height'])==SPECS[i][c['layer']]
                    count+=1
            on_gray(native).resize((768,768),NEAREST).save(d/'preview.png')
        overview.paste(on_gray(native).resize((384,384),NEAREST),(((i-1)%4)*384,((i-1)//4)*384))
        x=((i-1)%4)*300;y=((i-1)//4)*280
        head_sheet.paste(on_gray(native.crop((42,38,88,79))).resize((276,246),NEAREST),(x+12,y+24))
        draw.text((x+14,y+6),f'{i:02}',fill='white')
    overview.save(ROOT/'overview.png')
    head_sheet.save(ROOT/'native-heads.png')
    print(f'VERIFIED 16 native renders, {len(SPECS)} corrected suits, {count} corrected cels, all other frame-21 cels unchanged')

if __name__=='__main__':
    if sys.argv[2]=='inspect': inspect()
    elif sys.argv[2]=='prepare': prepare()
    elif sys.argv[2]=='verify': verify_and_preview()
