local function read(path)
  local f=assert(io.open(path,'rb'));local d=json.decode(f:read('*a'));f:close();return d
end
local root=assert(app.params.root)
local src=assert(app.params.src)
local output=assert(app.params.output)
local selected=app.params.selected
local all=read(app.fs.joinPath(root,'all.json'))
local function sameCel(a,b,label)
  assert((a==nil)==(b==nil),'Cel presence '..label)
  if not a then return end
  assert(a.position==b.position and a.opacity==b.opacity and a.zIndex==b.zIndex,'Cel properties '..label)
  assert(a.image.width==b.image.width and a.image.height==b.image.height,'Cel size '..label)
  for p in a.image:pixels() do assert(p()==b.image:getPixel(p.x,p.y),'Pixel '..label) end
end
local function unique(sprite,name)
  local found=nil
  for _,l in ipairs(sprite.layers) do if l.name==name then assert(not found,'Ambiguous '..name);found=l end end
  return assert(found,'Missing '..name)
end
local anchors={Hat='Hat_1',Chest='Body_1',Belt='Body_1',Skirt='Pelvis_1',ArmUp_L='ArmUp_L',ArmDown_L='ArmDown_L',Palm_L='Palm_L',ArmUp_R='ArmUp_R',ArmDown_R='ArmDown_R',Palm_R='Palm_R',Thigh_L='Thigh_L_1',Calf_L='Calf_L',Foot_L='Foot_L',Thigh_R='Thigh_R_1',Calf_R='Calf_R',Foot_R='Foot_R'}
local completed={}
for _,data in ipairs(all) do
  if not selected or selected==data.id then
    local filename='人类_'..data.id..'_'..data.name..'.aseprite'
    local dest=app.fs.joinPath(output,filename)
    assert(not app.fs.isFile(dest),'Output exists '..dest)
    local sprite=assert(app.open(src));assert(#sprite.frames==22)
    local originals={};for _,l in ipairs(sprite.layers) do originals[#originals+1]=l end
    sprite:newEmptyFrame(23).duration=sprite.frames[1].duration
    for _,name in ipairs(data.body_names) do
      local l=unique(sprite,name);local c=assert(l:cel(1));local n=sprite:newCel(l,23,Image(c.image),c.position);n.opacity=c.opacity;n.zIndex=c.zIndex
    end
    for _,item in ipairs(data.cels) do
      local img=Image{fromFile=app.fs.joinPath(root,'parts',data.id,item.png)}
      local layer=sprite:newLayer();layer.name='Equip_'..data.id..'_'..item.part
      if item.part=='Cape' then layer.stackIndex=1
      elseif anchors[item.part] then layer.stackIndex=unique(sprite,anchors[item.part]).stackIndex+1 end
      sprite:newCel(layer,23,img,Point(item.x,item.y))
    end
    local tag=sprite:newTag(23,23);tag.name=data.id..'_'..data.name
    app.activeFrame=sprite.frames[23]
    sprite:saveCopyAs(dest);sprite:close()
    local saved=assert(app.open(dest));local baseline=assert(app.open(src))
    assert(#saved.frames==23 and #baseline.frames==22)
    local old={}
    for _,l in ipairs(saved.layers) do
      if l.name:sub(1,6)~='Equip_' then old[#old+1]=l
      else for f=1,22 do assert(not l:cel(f),'Equipment in protected frame') end end
    end
    assert(#old==#baseline.layers)
    for i,l in ipairs(baseline.layers) do
      local r=old[i]
      assert(l.name==r.name and l.isVisible==r.isVisible and l.opacity==r.opacity and l.blendMode==r.blendMode,'Layer properties/order')
      for f=1,22 do sameCel(l:cel(f),r:cel(f),l.name..':'..f) end
    end
    for f=1,22 do assert(saved.frames[f].duration==baseline.frames[f].duration,'Duration') end
    for _,name in ipairs(data.body_names) do sameCel(unique(baseline,name):cel(1),unique(saved,name):cel(23),'Body '..name) end
    baseline:close();app.activeSprite=saved
    local render=Image(saved.spec);render:drawSprite(saved,23)
    render:saveAs(app.fs.joinPath(root,'parts',data.id,'native.png'))
    saved:close()
    completed[#completed+1]={id=data.id,file=filename,body_cels=#data.body_names,equipment_cels=#data.cels,old_frames=22,frame=23,verified=true}
    print('HUMAN_OK '..data.id..' parts='..#data.cels..' old_frames=22 body=17')
  end
end
local f=assert(io.open(app.fs.joinPath(root,'verification'..(selected or '')..'.json'),'wb'));f:write(json.encode(completed));f:close()
