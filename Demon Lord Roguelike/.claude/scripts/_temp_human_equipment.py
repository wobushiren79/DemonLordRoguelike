import os, json, math, hashlib
from pathlib import Path
from collections import deque
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT=Path(os.environ['TEMP'])/'human-equipment-20261107'
OUT=Path(__file__).resolve().parents[3]/'资源'/'生物'/'人类'/'装备'
BODY_NAMES=['ArmUp_R','ArmDown_R','Palm_R','Foot_R','Calf_R','Thigh_R_1','Foot_L','Calf_L','Thigh_L_1','Pelvis_1','Body_1','Head_1','Mouth_1','Eye_1','ArmUp_L','ArmDown_L','Palm_L']

def skin(c):
    r,g,b=c[:3]
    return r>82 and g>30 and b>25 and r-g>18 and -16<g-b<62 and b/r>.32 and g/r>.35

def component(mask,w,h):
    seen=set(); comps=[]
    for y in range(h):
        for x in range(w):
            p=y*w+x
            if not mask[p] or p in seen: continue
            seen.add(p); q=[p]; pixels=[]
            while q:
                z=q.pop(); pixels.append(z); xx=z%w; yy=z//w
                for nx,ny in ((xx-1,yy),(xx+1,yy),(xx,yy-1),(xx,yy+1)):
                    nz=ny*w+nx
                    if 0<=nx<w and 0<=ny<h and mask[nz] and nz not in seen: seen.add(nz);q.append(nz)
            comps.append(pixels)
    return comps

def prep():
    records=[]
    for group in 'AB':
        sheet=Image.open(ROOT/f'reference_{group}.png').convert('RGBA')
        for i in range(16):
            col,row=i%4,i//4
            box=(round(col*sheet.width/4),round(row*sheet.height/4),round((col+1)*sheet.width/4),round((row+1)*sheet.height/4))
            im=sheet.crop(box); w,h=im.size; data=list(im.getdata())
            m=[skin(c) and 70<x%w<230 and x//w<140 for x,c in enumerate(data)]
            cs=component(m,w,h)
            cs=[c for c in cs if len(c)>60 and min(z//w for z in c)<110]
            cs.sort(key=len,reverse=True)
            c=cs[0]; bb=[min(z%w for z in c),min(z//w for z in c),max(z%w for z in c)+1,max(z//w for z in c)+1]
            records.append({'id':f'{group}{i+1:02d}','box':box,'face':bb,'n':len(c)})
            p=ROOT/'cells'/f'{group}{i+1:02d}.png';p.parent.mkdir(exist_ok=True);im.save(p)
    (ROOT/'records.json').write_text(json.dumps(records),encoding='utf-8')
    print(json.dumps(records))

NAMES_A=['紫影轻甲','星辉法师','森林游侠','圣光骑士','荒原狂战士','晨曦祭司','赤衣武士','赤刃刺客','橡林德鲁伊','蒸汽猎手','赤绸武僧','幽冥术士','暮紫魔女','赤金角斗士','冰刃战士','霜雪法师']
NAMES_B=['黑革轻装','蓝金法师','银辉骑士','绿兜游侠','红金法师','蓝袍重骑','暗紫刺客','赤沙斗士','黑紫魔女','荒野猎人','白金祭司','绯羽猎魔人','蓝甲冒险者','紫金术士','霜毛勇士','黑银守卫']
# Source coordinates are measured in each original grid cell; the master stays unchanged.
# head x, head top, neck, waist, hips, knee, sole, torso x
CFG_A=[
 (168,45,99,169,198,247,311,164),(144,62,114,170,199,247,311,143),(161,49,109,170,197,245,310,153),(143,47,108,166,200,249,311,144),
 (168,31,88,145,185,236,294,159),(154,42,99,145,183,235,294,151),(163,48,92,149,183,235,294,153),(146,34,89,142,177,232,295,145),
 (174,39,96,151,185,241,299,163),(143,39,80,146,185,243,299,141),(153,36,96,146,181,240,299,150),(141,31,83,151,185,242,299,138),
 (169,39,89,140,175,232,288,160),(164,45,92,145,182,234,288,155),(155,30,82,138,175,231,288,150),(139,29,84,141,179,233,288,143)
]
CFG_B=[
 (171,45,97,160,186,238,295,163),(167,60,107,165,192,243,295,155),(155,39,92,156,191,241,299,151),(161,47,95,156,186,237,298,150),
 (175,56,100,163,196,243,293,158),(164,31,90,151,190,236,293,156),(161,35,90,157,190,240,293,151),(155,31,88,149,182,234,293,149),
 (171,47,96,153,185,235,290,158),(165,29,86,149,181,232,290,156),(164,33,90,150,184,235,290,151),(161,43,87,151,184,236,290,150),
 (165,20,80,141,174,229,280,156),(157,22,78,139,174,228,280,151),(157,21,81,141,177,230,280,152),(159,24,82,140,177,229,280,151)
]
# Independent hand-held props, traced on original pixels.
WEAPONS={
'A02': [[(234,42),(266,42),(275,95),(238,127),(196,276),(185,271),(220,122),(214,100),(217,66)]],
'A03': [[(81,60),(103,59),(128,100),(113,123),(95,106)]],
'A04': [[(80,167),(104,160),(171,245),(174,309),(147,284),(97,217),(74,200)]],
'A05': [[(214,153),(278,140),(293,174),(289,222),(268,227),(242,194),(214,186)]],
'A06': [[(211,29),(249,29),(255,83),(231,108),(218,266),(204,267),(216,107),(199,81)]],
'A07': [[(8,259),(68,209),(95,183),(132,184),(126,207),(66,245),(24,269)]],
'A08': [[(70,174),(83,185),(55,237),(47,235)],[(201,172),(215,174),(238,227),(232,233)]],
'A09': [[(211,37),(253,35),(270,65),(257,99),(235,110),(226,279),(216,280),(221,105),(204,91),(203,58)]],
'A10': [[(195,146),(214,159),(248,230),(235,243),(216,223),(197,182)]],
'A12': [[(204,8),(255,0),(271,39),(265,84),(229,113),(216,266),(204,265),(215,111),(201,92),(199,55)]],
'A13': [[(87,154),(113,158),(125,190),(114,202),(91,199),(81,180)]],
'A14': [[(182,111),(217,114),(240,160),(230,204),(211,217),(183,203),(170,162)],[(96,164),(113,177),(49,239),(37,235)]],
'A15': [[(88,141),(101,151),(65,205),(52,207),(50,200)],[(195,150),(206,148),(215,203),(210,226),(202,213)]],
'A16': [[(233,9),(249,5),(266,35),(251,75),(233,98),(202,243),(190,242),(217,95),(216,67)]]
}
FEET_A=[(109,217),(93,196),(99,202),(87,193),(109,212),(96,191),(108,204),(94,190),(111,215),(92,191),(86,202),(87,184),(117,211),(105,202),(97,212),(89,191)]
FEET_B=[(114,212),(110,198),(103,197),(104,195),(119,216),(111,202),(102,199),(104,193),(115,212),(109,208),(111,199),(104,199),(112,211),(110,199),(103,204),(111,196)]
FACE_GEAR={'A02':(125,76,165,111),'A03':(143,54,179,102),'A06':(135,47,173,91),'A07':(149,78,178,96),'A09':(151,45,191,89),'A10':(123,53,160,74),'A13':(147,58,188,94),'A14':(148,49,181,88),'B02':(149,67,186,103),'B04':(147,60,180,93),'B05':(158,68,193,102),'B07':(148,48,180,83),'B09':(158,58,191,95),'B11':(146,44,182,82),'B12':(140,58,179,91)}
PROP_ANCHORS={'A02':[(216,170,'R')],'A04':[(95,190,'L')],'A05':[(218,173,'R')],'A06':[(215,153,'R')],'A07':[(111,192,'L')],'A08':[(80,177,'L'),(208,175,'R')],'A09':[(225,153,'R')],'A10':[(195,167,'R')],'A12':[(215,160,'R')],'A13':[(105,177,'L')],'A14':[(201,161,'R'),(101,176,'L')],'A15':[(92,154,'L'),(201,154,'R')],'A16':[(196,139,'R')]}
# Skin is removed only where the reference actually exposes the body.
# Fur, pale leather, gold trims and white cloth are deliberately excluded.
SKIN_REGIONS={
'A01':[(111,102,149,142),(186,117,211,157),(130,141,185,170),(117,194,159,244),(184,195,211,235)],
'A02':[], 'A03':[(102,113,134,145),(182,116,208,146),(119,191,162,244),(168,190,198,231)], 'A04':[],
'A05':[(128,117,187,150),(110,176,151,219),(174,184,206,218)],'A06':[],'A07':[],
'A08':[(105,88,136,118),(167,95,193,126),(126,119,173,145)],
'A09':[(126,108,174,153),(103,113,136,151),(190,118,213,153),(128,181,165,235),(183,185,210,237)],
'A10':[(90,115,119,143)],'A11':[(100,91,195,153)],'A12':[],
'A13':[(122,96,145,141),(181,100,211,142),(140,119,182,143),(130,174,158,207),(173,175,201,216)],
'A14':[(111,87,148,118),(104,118,129,152),(142,116,184,146),(124,184,151,218),(179,184,203,220)],
'A15':[(109,92,139,132),(176,93,205,136),(123,171,153,208),(176,170,207,211)],'A16':[],
'B01':[(109,93,209,158),(134,126,189,162),(111,180,160,241),(173,180,214,240)],
'B02':[(113,180,157,231),(164,182,197,233)],'B03':[(111,179,144,208),(174,177,207,213)],
'B04':[(117,179,150,219),(166,178,202,215)],
'B05':[(132,119,186,164),(113,193,155,231),(172,193,204,230)],'B06':[],
'B07':[(114,99,143,129),(178,103,205,132),(126,127,182,158),(107,189,146,225),(173,190,204,225)],
'B08':[(108,87,144,118),(170,91,198,120),(127,113,181,153),(118,184,145,215),(168,184,201,222)],
'B09':[(128,103,147,137),(178,111,203,145),(135,135,184,157),(120,186,161,224),(172,185,204,227)],
'B10':[(109,87,144,119),(176,89,205,119),(128,117,188,152),(119,178,157,225),(176,180,207,224)],
'B11':[(154,185,192,238)],'B12':[(139,88,173,116),(124,182,160,216),(174,183,203,220)],
'B13':[(110,78,142,113),(176,85,202,119),(129,107,186,145),(112,172,150,207),(174,172,207,210)],
'B14':[(140,84,182,111),(135,118,181,144),(122,173,159,220),(170,172,200,219)],
'B15':[(128,110,183,143),(112,175,153,216),(171,175,203,217)],'B16':[]
}

def polygon_mask(size, polys):
    m=Image.new('L',size);d=ImageDraw.Draw(m)
    for poly in polys:d.polygon(poly,fill=255)
    return m

def strip_background(im,sole):
    w,h=im.size; data=list(im.getdata()); candidates=[]
    for z,c in enumerate(data):
        r,g,b=c[:3];low=35 if z//w>=sole-14 else 77
        candidates.append(max(r,g,b)-min(r,g,b)<18 and low<sum(c[:3])/3<166)
    seen=set();q=[]
    for x in range(w):q.extend([x,(h-1)*w+x])
    for y in range(h):q.extend([y*w,y*w+w-1])
    while q:
        z=q.pop()
        if z in seen or not candidates[z]:continue
        seen.add(z);x,y=z%w,z//w
        for nx,ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
            if 0<=nx<w and 0<=ny<h:q.append(ny*w+nx)
    for z in seen:data[z]=(0,0,0,0)
    result=Image.new('RGBA',im.size);result.putdata(data);return result

def interp(v,keys,vals):
    i=0
    while i<len(keys)-2 and v>keys[i+1]:i+=1
    t=(v-keys[i])/(keys[i+1]-keys[i]);return vals[i]*(1-t)+vals[i+1]*t

def process(record,config):
    key=record['id'];group=key[0];num=int(key[1:]);name=(NAMES_A if group=='A' else NAMES_B)[num-1]
    cx,ht,neck,waist,hip,knee,sole,tx=config
    im=Image.open(ROOT/'cells'/f'{key}.png').convert('RGBA');cut=strip_background(im,sole)
    pix=cut.load();w,h=cut.size
    # Blank face opening; naked heads are entirely supplied by the master.
    headgear=key in ['A02','A03','A06','A07','A09','A10','A13','A14','B02','B04','B05','B07','B09','B11','B12']
    for y in range(h):
        for x in range(w):
            p=pix[x,y]
            if not p[3]:continue
            if not headgear and y<neck-6 and abs(x-cx)<35:pix[x,y]=(0,0,0,0);continue
            fb=FACE_GEAR.get(key,(cx-27,ht-5,cx+27,neck+2))
            inface=fb[0]<=x<=fb[2] and fb[1]<=y<=fb[3]
            regions=SKIN_REGIONS[key]
            inskin=any(x0<=x<=x1 and y0<=y<=y1 for x0,y0,x1,y1 in regions)
            if (inface or inskin) and skin(p):pix[x,y]=(0,0,0,0)
    # Remove tiny detached anti-alias specks while retaining the reference color values.
    cs=component([p[3]>0 for p in cut.getdata()],w,h)
    for c in cs:
        if len(c)<6:
            for z in c:pix[z%w,z//w]=(0,0,0,0)
    weapon_mask=polygon_mask(im.size,WEAPONS.get(key,[])).filter(ImageFilter.MaxFilter(9))
    # Garments are split into functional body regions, with weapons kept separate.
    labels={n:Image.new('RGBA',(256,128)) for n in ['Cape','Hat','Chest','Belt','Skirt','ArmUp_L','ArmDown_L','Palm_L','ArmUp_R','ArmDown_R','Palm_R','Thigh_L','Calf_L','Foot_L','Thigh_R','Calf_R','Foot_R','Weapon']}
    sykeys=[ht,neck,waist,hip,knee,sole]
    dykeys=[57,73,92,101,114,128]
    # Correct reference arm placement independently of the trunk.
    # Source hand holds may be higher than the neutral master; the prop follows its hand.
    left=[(tx-30,neck+12),(tx-43,waist-13),(tx-51,waist+16)]
    right=[(tx+34,neck+16),(tx+44,waist-6),(tx+52,waist+18)]
    if key=='A02':right=[(tx+35,neck+16),(tx+48,waist-15),(tx+69,waist)]
    if key=='A06':right=[(tx+37,neck+15),(tx+48,waist-2),(tx+61,waist+6)]
    if key in ['A09','A12','A16']:right=[(tx+34,neck+15),(tx+47,waist-8),(tx+60,waist+8)]
    if key=='A14':right=[(tx+35,neck+10),(tx+40,waist-6),(tx+45,waist+9)]
    footL,footR=(FEET_A if group=='A' else FEET_B)[num-1]
    def src_at(dx,dy):
        sy=interp(dy,dykeys,sykeys)
        center=interp(dy,[57,73,92,128],[cx,cx-3,tx,tx])
        scale=interp(dy,[57,73,92,128],[.31,.30,.29,.275])
        if dy>=101:
            sl=interp(dy,[101,114,128],[tx-23,footL+9,footL]);sr=interp(dy,[101,114,128],[tx+24,footR-9,footR])
            dl=interp(dy,[101,114,128],[121,119,119]);dr=interp(dy,[101,114,128],[132,135,137])
            hw=interp(dy,[101,114,128],[5.5,4,4])
            sx=interp(dx,[dl-hw,dl+hw,dr-3.1,dr+4.1],[sl-15,sl+15,sr-11,sr+15])
            return sx,sy
        return center+(dx-127)/scale,sy
    # Assign source pixels using the master-aligned partition. Uncovered body pixels remain original.
    for dy in range(35,128):
        for dx in range(65,190):
            sx,sy=src_at(dx+.5,dy+.5);ix,iy=round(sx),round(sy)
            if not(0<=ix<w and 0<=iy<h):continue
            c=pix[ix,iy]
            if not c[3]:continue
            if weapon_mask.getpixel((ix,iy)):part='Weapon'
            elif key in ['A03','B04'] and ix<tx and iy>neck+15 and c[1]>c[0]*.88 and c[1]>c[2]*1.08:part='Cape'
            elif dy<74:part='Hat'
            elif dy<93:
                if dx<119:part='ArmUp_L' if dy<83 else 'ArmDown_L'
                elif dx>=137:part='ArmUp_R' if dy<88 else 'ArmDown_R'
                else:part='Chest'
            elif dy<101:
                if dx<118:part='Palm_L' if dy>=94 else 'ArmDown_L'
                elif dx>=137:part='Palm_R' if dy>=96 else 'ArmDown_R'
                else:part='Belt' if dy<96 else 'Skirt'
            else:
                leftx=interp(dy,[101,114,128],[121,119,119]);rightx=interp(dy,[101,114,128],[132,135,137])
                if abs(dx-leftx)<5:part='Thigh_L' if dy<113 else ('Calf_L' if dy<124 else 'Foot_L')
                elif abs(dx-rightx)<5:part='Thigh_R' if dy<114 else ('Calf_R' if dy<124 else 'Foot_R')
                elif 122<=dx<=133 and dy<118:part='Skirt'
                else:part='Cape'
            labels[part].putpixel((dx,dy),c)
    # Refit arm garments by joint anchors, keeping the master's limb pixels intact below them.
    base_manifest=json.loads((ROOT/'base/manifest.json').read_text())
    originals={c['layer']:c for c in base_manifest['cels'] if c['layer'] in BODY_NAMES}
    for side,points,targets in [('L',left,[(118,77),(114,86),(113,97)]),('R',right,[(136,79),(139,90),(139,99)])]:
        for segment in ['ArmUp','ArmDown','Palm']:
            part=segment+'_'+side;labels[part]=Image.new('RGBA',(256,128))
            c=originals[part];bm=Image.new('L',(256,128));bm.paste(Image.open(ROOT/'base'/c['png']).getchannel('A'),(c['x'],c['y']))
            radius=7 if segment=='ArmUp' else 3
            mask=bm.filter(ImageFilter.MaxFilter(radius));bbox=mask.getbbox()
            for yy in range(bbox[1],bbox[3]):
                for xx in range(bbox[0],bbox[2]):
                    if not mask.getpixel((xx,yy)):continue
                    if segment=='ArmUp' and yy>targets[1][1]:continue
                    if segment=='ArmDown' and (yy<=targets[1][1] or yy>=targets[2][1]-2):continue
                    if segment=='Palm' and yy<targets[2][1]-2:continue
                    sy=interp(yy+.5,[p[1] for p in targets],[p[1] for p in points])
                    scx=interp(yy+.5,[p[1] for p in targets],[p[0] for p in points]);dcx=interp(yy+.5,[p[1] for p in targets],[p[0] for p in targets])
                    sx=scx+(xx+.5-dcx)/.30;ix,iy=round(sx),round(sy)
                    if not(0<=ix<w and 0<=iy<h) or weapon_mask.getpixel((ix,iy)):continue
                    color=pix[ix,iy]
                    if segment=='Palm' and skin(color):continue
                    labels[part].putpixel((xx,yy),color)
    # Props have their own transform around the grip, independent of leg and torso reshaping.
    prop_order=[]
    if key in PROP_ANCHORS:
        labels['Weapon']=Image.new('RGBA',(256,128))
        for poly,anchor in zip(WEAPONS[key],PROP_ANCHORS[key]):
            ax,ay,side=anchor;dx0,dy0=(113,97) if side=='L' else (139,99)
            mask=polygon_mask(im.size,[poly]).filter(ImageFilter.MaxFilter(9));part='Weapon_'+side;prop_order.append(part);layer=Image.new('RGBA',(256,128))
            for yy in range(25,128):
                scale_y=.34 if yy<dy0 else .28;sy=round(ay+(yy-dy0)/scale_y)
                if not 0<=sy<h:continue
                for xx in range(65,190):
                    sx=round(ax+(xx-dx0)/.30)
                    if 0<=sx<w and mask.getpixel((sx,sy)):
                        p=pix[sx,sy]
                        if abs(sx-ax)<9 and abs(sy-ay)<10 and skin(p):continue
                        layer.putpixel((xx,yy),p)
            labels[part]=layer
    # Extend only boot pixels onto uncovered master toes, never redraw or move the body.
    for side in ['L','R']:
        part='Foot_'+side;layer=labels[part];c=originals[part];bm=Image.open(ROOT/'base'/c['png'])
        existing=[(x,y,layer.getpixel((x,y))) for y in range(122,128) for x in range(110,146) if layer.getpixel((x,y))[3]]
        if existing:
            for y in range(bm.height):
                for x in range(bm.width):
                    xx,yy=x+c['x'],y+c['y']
                    if bm.getpixel((x,y))[3] and not layer.getpixel((xx,yy))[3]:
                        near=min(existing,key=lambda a:(a[0]-xx)**2+(a[1]-yy)**2)
                        layer.putpixel((xx,yy),near[2])
    union=Image.new('RGBA',(256,128))
    for layer in labels.values():union.alpha_composite(layer)
    tiny=component([p[3]>0 for p in union.getdata()],256,128)
    contact=Image.open(ROOT/'body.png').getchannel('A').filter(ImageFilter.MaxFilter(3))
    for comp in tiny:
        if len(comp)<8:
            if not any(contact.getpixel((z%256,z//256)) for z in comp):
                for z in comp:
                    for layer in labels.values():layer.putpixel((z%256,z//256),(0,0,0,0))
    body=Image.open(ROOT/'body.png').convert('RGBA');preview=body.copy()
    order=['Cape','Thigh_R','Calf_R','Foot_R','ArmUp_R','ArmDown_R','Palm_R','Thigh_L','Calf_L','Foot_L','Skirt','Chest','Belt','Hat','ArmUp_L','ArmDown_L','Palm_L','Weapon']+prop_order
    d=ROOT/'parts'/key;d.mkdir(parents=True,exist_ok=True)
    cels=[]
    for part in order:
        layer=labels[part];bbox=layer.getbbox()
        if not bbox:continue
        cropped=layer.crop(bbox);cropped.save(d/f'{part}.png')
        preview.alpha_composite(layer)
        cels.append({'part':part,'png':f'{part}.png','x':bbox[0],'y':bbox[1]})
    preview.save(d/'preview.png');cut.save(d/'cutout.png')
    manifest={'id':key,'name':name,'body_names':BODY_NAMES,'cels':cels,'frame':23,'width':256,'height':128}
    (d/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False),encoding='utf-8')
    return manifest

def build():
    records=json.loads((ROOT/'records.json').read_text());results=[]
    for i,r in enumerate(records):results.append(process(r,(CFG_A+CFG_B)[i]))
    (ROOT/'all.json').write_text(json.dumps(results,ensure_ascii=False),encoding='utf-8')
    overview=Image.new('RGB',(8*200,4*340),(77,79,84));draw=ImageDraw.Draw(overview)
    for i,r in enumerate(records):
        p=Image.open(ROOT/'parts'/r['id']/'preview.png');p=p.crop((85,35,170,128)).resize((255,279),Image.Resampling.NEAREST)
        col=i%4+(4 if i>=16 else 0);row=(i%16)//4
        p.thumbnail((195,300),Image.Resampling.NEAREST)
        overview.paste(p,(col*200+(200-p.width)//2,row*340+25),p)
        draw.text((col*200+8,row*340+8),r['id'],fill='white')
    overview.save(ROOT/'overview_draft.png')
    print('Built',len(results),'outfits')

def review():
    items=json.loads((ROOT/'all.json').read_text(encoding='utf-8'))
    fontpath=Path(os.environ['WINDIR'])/'Fonts'/'msyh.ttc'
    font=ImageFont.truetype(str(fontpath),18)
    boards={g:Image.new('RGB',(1200,1440),(72,74,80)) for g in 'AB'}
    for i,item in enumerate(items):
        group=item['id'][0];n=int(item['id'][1:])-1
        im=Image.open(ROOT/'parts'/item['id']/'native.png').convert('RGBA')
        bb=im.getbbox();crop=im.crop((max(bb[0]-3,0),max(bb[1]-3,0),min(bb[2]+3,256),128))
        crop=crop.resize((crop.width*3,crop.height*3),Image.Resampling.NEAREST)
        assert crop.width<=294 and crop.height<=320,(item['id'],crop.size)
        board=boards[group];x=n%4*300;y=n//4*360
        board.paste(crop,(x+(300-crop.width)//2,y+326-crop.height),crop)
        d=ImageDraw.Draw(board);d.text((x+12,y+335),item['id']+' '+item['name'],font=font,fill=(236,233,227))
    for g,b in boards.items():b.save(ROOT/f'Human_Equipment_{g}_Preview.png')
    combined=Image.new('RGB',(2400,1440));combined.paste(boards['A'],(0,0));combined.paste(boards['B'],(1200,0));combined.save(ROOT/'Human_Equipment_32_Preview.png')
    # Every isolated imported garment is visible in this contact sheet, away from the body.
    atlas=Image.new('RGB',(19*90,32*100),(90,92,98));draw=ImageDraw.Draw(atlas)
    for row,item in enumerate(items):
        for col,c in enumerate(item['cels']):
            im=Image.open(ROOT/'parts'/item['id']/c['png']).convert('RGBA')
            scale=min(4,80//max(im.width,1),72//max(im.height,1));scale=max(1,scale)
            im=im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST)
            x=col*90+(90-im.width)//2;y=row*100+19
            atlas.paste(im,(x,y),im);draw.text((col*90+2,row*100+2),item['id']+' '+c['part'],fill='white')
    atlas.save(ROOT/'isolated_parts_qa.png')
    print('Native and isolated-part review sheets ready')

def deliver():
    import shutil,zipfile
    items=json.loads((ROOT/'all.json').read_text(encoding='utf-8'));verified=json.loads((ROOT/'verification.json').read_text(encoding='utf-8'))
    assert len(verified)==32 and all(x['verified'] and x['body_cels']==17 and x['frame']==23 for x in verified)
    master=Path(__file__).resolve().parents[3]/'资源'/'生物'/'人类'/'人类.aseprite'
    assert hashlib.sha256(master.read_bytes()).hexdigest()=='10132019e243632b6c4fbae62712f7ef7773eb8c741449e4b6d7a3dc9ebee629'
    OUT.mkdir(exist_ok=True)
    for item in items:
        basename='人类_'+item['id']+'_'+item['name'];dest=OUT/(basename+'.aseprite')
        shutil.copy2(ROOT/'verified_v5'/(basename+'.aseprite'),dest)
        native=Image.open(ROOT/'parts'/item['id']/'native.png').convert('RGBA');assert native.size==(256,128)
        native.resize((1024,512),Image.Resampling.NEAREST).save(OUT/(basename+'.png'))
        reference=Image.open(ROOT/'cells'/(item['id']+'.png'));reference.save(OUT/(basename+'_参考.png'))
    public=Path(sys.argv[2]).resolve();public.mkdir(parents=True,exist_ok=True)
    for g in ['A','B','32']:
        name=f'Human_Equipment_{g}_Preview.png';shutil.copy2(ROOT/name,public/name)
    package=public/'Human_Equipment_32.zip'
    with zipfile.ZipFile(package,'w',zipfile.ZIP_DEFLATED) as z:
        for item in items:
            stem='人类_'+item['id']+'_'+item['name']
            for suffix in ['.aseprite','.png','_参考.png']:
                path=OUT/(stem+suffix);z.write(path,'装备/'+path.name)
        for g in ['A','B','32']:z.write(public/f'Human_Equipment_{g}_Preview.png',f'Human_Equipment_{g}_Preview.png')
    with zipfile.ZipFile(package) as z:assert z.testzip() is None and len(z.namelist())==99
    print(json.dumps({'outfits':32,'files':96,'frame':23,'source_unchanged':True,'zip_bytes':package.stat().st_size,'zip':str(package),'output':str(OUT)},ensure_ascii=False))

if __name__=='__main__':
    import sys
    if len(sys.argv)>1 and sys.argv[1]=='build':build()
    elif len(sys.argv)>1 and sys.argv[1]=='review':review()
    elif len(sys.argv)>1 and sys.argv[1]=='deliver':deliver()
    else:prep()
