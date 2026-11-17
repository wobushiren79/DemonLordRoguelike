-- Official Aseprite API only: export, append/apply equipment, and compare saved sprites.
local function required(name)
  local value = app.params[name]
  assert(value and value ~= "", "Missing parameter: " .. name)
  return value
end

local function normalized(path)
  return path:gsub("\\", "/"):lower()
end

local function readJson(path)
  local file = assert(io.open(path, "rb"))
  local value = json.decode(file:read("*a"))
  file:close()
  return value
end

local function writeJson(path, value)
  local file = assert(io.open(path, "wb"))
  file:write(json.encode(value))
  file:close()
end

local function indexLayers(sprite)
  local entries, byPath = {}, {}
  local function walk(layers, prefix, parentIndices)
    for i, layer in ipairs(layers) do
      local path = prefix == "" and layer.name or prefix .. "/" .. layer.name
      local indices = { table.unpack(parentIndices) }
      indices[#indices + 1] = i
      local matches = byPath[path] or {}
      local entry = { layer = layer, path = path, indices = indices, occurrence = #matches + 1 }
      matches[#matches + 1] = entry
      byPath[path] = matches
      entries[#entries + 1] = entry
      if layer.isGroup then walk(layer.layers, path, indices) end
    end
  end
  walk(sprite.layers, "", {})
  return byPath, entries
end

local function layerEntry(byPath, item)
  local path, indices = item.layer, item.layer_indices
  assert(type(path) == "string" and path ~= "", "Missing layer path")
  local matches = byPath[path] or {}
  if indices == nil then
    assert(#matches < 2, "Ambiguous layer path; supply layer_indices: " .. path)
    return matches[1]
  end
  -- Aseprite's JSON decoder exposes arrays as userdata.
  assert((type(indices) == "userdata" or type(indices) == "table") and #indices > 0, "Expected layer_indices array: " .. path)
  for _, entry in ipairs(matches) do
    local same = #indices == #entry.indices
    for i, index in ipairs(entry.indices) do
      if index ~= indices[i] then same = false end
    end
    if same then return entry end
  end
  error("layer_indices do not match the layer path: " .. path)
end

local function folder(path)
  assert(app.fs.isDirectory(path), "Directory must already exist: " .. path)
end

local function outputFile(path, src)
  assert(normalized(path) ~= normalized(src), "Source and output must differ")
  assert(not app.fs.isFile(path) and not app.fs.isDirectory(path), "Output already exists: " .. path)
  folder(app.fs.filePath(path))
end

local function exportFrame()
  local src, root = required("src"), required("dir")
  local sprite = assert(app.open(src))
  local frame = tonumber(required("frame"))
  assert(frame and frame >= 1 and frame <= #sprite.frames and frame % 1 == 0)
  folder(root)
  local manifest = { width = sprite.width, height = sprite.height, frame = frame, cels = {} }
  local fileNumber = 0
  local byPath, entries = indexLayers(sprite)
  for _, entry in ipairs(entries) do
    local layer = entry.layer
    local cel = not layer.isGroup and layer:cel(frame) or nil
    if cel then
      fileNumber = fileNumber + 1
      local filename = string.format("cel_%03d_part.png", fileNumber)
      local path = app.fs.joinPath(root, filename)
      assert(not app.fs.isFile(path), "Export exists: " .. path)
      cel.image:saveAs(path)
      local item = {
        layer = entry.path, png = filename,
        x = cel.position.x, y = cel.position.y,
        width = cel.image.width, height = cel.image.height,
        opacity = cel.opacity, z_index = cel.zIndex,
        visible = layer.isVisible, blend_mode = tostring(layer.blendMode)
      }
      if #byPath[entry.path] > 1 then item.layer_indices = entry.indices end
      manifest.cels[#manifest.cels + 1] = item
    end
  end
  local path = app.fs.joinPath(root, "manifest.json")
  assert(not app.fs.isFile(path), "Manifest already exists")
  writeJson(path, manifest)
  sprite:close()
  print("EQUIPMENT_EXPORT_OK cels=" .. #manifest.cels)
end

local function applyManifest()
  local src, out, manifestPath = required("src"), required("out"), required("manifest")
  outputFile(out, src)
  local data = readJson(manifestPath)
  local sprite = assert(app.open(src))
  assert(sprite.colorMode == ColorMode.RGB, "Apply requires RGB/RGBA")
  assert(sprite.width == data.width and sprite.height == data.height, "Canvas mismatch")
  local originalCount = #sprite.frames
  local mode = data.mode or "replace"
  assert(mode == "append" or mode == "replace", "mode must be append or replace")
  assert(data.frame and data.frame % 1 == 0)
  if mode == "append" then
    assert(data.frame == originalCount + 1, "Append exactly one frame at the end")
  else
    assert(data.frame >= 1 and data.frame <= originalCount, "Invalid replacement frame")
  end
  local pngRoot = app.fs.filePath(manifestPath)
  local byPath = indexLayers(sprite)
  local loaded, seen = {}, {}
  for i, item in ipairs(data.cels or {}) do
    local existing = layerEntry(byPath, item)
    local target = existing or item.layer
    assert(item.png and not seen[target], "Missing PNG or duplicate layer: " .. item.layer)
    seen[target] = true
    assert(not item.png:match("^[/\\]") and not item.png:match("^%a:") and not item.png:find("..", 1, true), "PNG must be relative without parent traversal")
    assert(existing or (item.create == true and not item.layer:find("/", 1, true)), "Missing layer; new layers must be explicit top-level entries")
    if existing then assert(not existing.layer.isGroup, "Cannot assign cel to a group") end
    assert(type(item.x) == "number" and item.x % 1 == 0 and type(item.y) == "number" and item.y % 1 == 0, "Integer x/y required")
    -- Image loads one PNG without app.open's automatic numbered image-sequence import.
    local image = Image{ fromFile = app.fs.joinPath(pngRoot, item.png) }
    assert(image and image.colorMode == ColorMode.RGB, "Expected RGBA PNG: " .. item.png)
    loaded[i] = { image = image, entry = existing }
  end
  assert(#loaded > 0, "No replacement cels")
  app.activeSprite = sprite
  app.transaction("Apply independent equipment cels", function()
    if mode == "append" then
      local frame = sprite:newEmptyFrame(data.frame)
      frame.duration = data.duration or 0.8
      -- Copy only the body/face layers explicitly selected by the artist.
      for _, item in ipairs(data.copy_cels or {}) do
        local entry = assert(layerEntry(byPath, item), "Missing copy layer: " .. item.layer)
        local layer = entry.layer
        local source = assert(layer:cel(item.from_frame))
        local cel = sprite:newCel(layer, data.frame, Image(source.image), source.position)
        cel.opacity, cel.zIndex = source.opacity, source.zIndex
      end
    end
    for i, item in ipairs(data.cels) do
      local target = loaded[i]
      local layer = target.entry and target.entry.layer
      if not layer then
        layer = sprite:newLayer()
        layer.name = item.layer
        if item.stack_index then layer.stackIndex = item.stack_index end
      end
      local cel = layer:cel(data.frame)
      if cel then
        -- Clone the image so linked cels in earlier frames cannot be changed.
        cel.image = Image(target.image)
        cel.position = Point(item.x, item.y)
      else
        cel = sprite:newCel(layer, data.frame, Image(target.image), Point(item.x, item.y))
      end
      if item.opacity ~= nil then cel.opacity = item.opacity end
      if item.z_index ~= nil then cel.zIndex = item.z_index end
    end
    if data.tag then
      local tag = sprite:newTag(data.frame, data.frame)
      tag.name = data.tag
    end
  end)
  app.activeFrame = sprite.frames[data.frame]
  sprite:saveCopyAs(out)
  sprite:close()
  local check = assert(app.open(out), "Saved sprite cannot reopen")
  assert(#check.frames == originalCount + (mode == "append" and 1 or 0))
  local savedByPath = indexLayers(check)
  for i, item in ipairs(data.cels) do
    -- A new layer can shift the indices of existing layers, but cannot change their order within a path.
    local existing = loaded[i].entry
    local matches = assert(savedByPath[item.layer], "Missing saved layer: " .. item.layer)
    local entry = assert(matches[existing and existing.occurrence or 1], "Missing saved layer: " .. item.layer)
    assert(entry.layer:cel(data.frame), "Missing saved cel: " .. item.layer)
  end
  check:close()
  print("EQUIPMENT_APPLY_OK mode=" .. mode .. " frame=" .. data.frame .. " imported=" .. #loaded)
end

local function verifyCels(left, right, through, path)
  for f = 1, through do
    local a, b = left and left:cel(f), right:cel(f)
    assert((not a) == (not b), "Cel presence changed: " .. path .. " frame=" .. f)
    if a then
      assert(a.position == b.position and a.opacity == b.opacity and a.zIndex == b.zIndex, "Cel properties changed: " .. path .. " frame=" .. f)
      assert(a.image.width == b.image.width and a.image.height == b.image.height, "Cel bounds changed: " .. path .. " frame=" .. f)
      for pixel in a.image:pixels() do
        assert(pixel() == b.image:getPixel(pixel.x, pixel.y), "Pixel changed: " .. path .. " frame=" .. f)
      end
    end
  end
end

local function verifyLayers(before, after, through, prefix)
  local originalNames = {}
  for _, layer in ipairs(before) do originalNames[layer.name] = true end
  local nextOriginal = 1
  for _, right in ipairs(after) do
    local path = prefix .. right.name
    local left
    if originalNames[right.name] then
      left = before[nextOriginal]
      assert(left and left.name == right.name and left.isGroup == right.isGroup, "Original layer order or type changed: " .. path)
      assert(left.isVisible == right.isVisible and left.opacity == right.opacity and left.blendMode == right.blendMode, "Layer properties changed: " .. path)
      nextOriginal = nextOriginal + 1
    end
    if right.isGroup then
      verifyLayers(left and left.layers or {}, right.layers, through, path .. "/")
    else
      verifyCels(left, right, through, path)
    end
  end
  assert(nextOriginal == #before + 1, "Original layer missing: " .. prefix)
end

local function verifyUnchanged()
  local source = assert(app.open(required("src")))
  local output = assert(app.open(required("out")))
  local through = tonumber(required("through"))
  assert(source.width == output.width and source.height == output.height and source.colorMode == output.colorMode)
  assert(through and through <= #source.frames and through <= #output.frames)
  verifyLayers(source.layers, output.layers, through, "")
  for f = 1, through do assert(source.frames[f].duration == output.frames[f].duration, "Frame timing changed") end
  source:close()
  output:close()
  print("EQUIPMENT_VERIFY_OK unchanged_frames=" .. through)
end

local op = required("op")
if op == "export" then exportFrame()
elseif op == "apply" then applyManifest()
elseif op == "verify" then verifyUnchanged()
else error("Expected op=export, apply, or verify") end
