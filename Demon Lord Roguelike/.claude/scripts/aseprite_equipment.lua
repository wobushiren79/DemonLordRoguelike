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

local function walk(layers, prefix, result)
  result = result or {}
  for _, layer in ipairs(layers) do
    local path = prefix == "" and layer.name or prefix .. "/" .. layer.name
    result[#result + 1] = { layer = layer, path = path }
    if layer.isGroup then walk(layer.layers, path, result) end
  end
  return result
end

local function layerAt(sprite, path)
  local found = nil
  for _, entry in ipairs(walk(sprite.layers, "")) do
    if entry.path == path then
      assert(not found, "Ambiguous layer path: " .. path)
      found = entry.layer
    end
  end
  return found
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
  for _, entry in ipairs(walk(sprite.layers, "")) do
    local layer = entry.layer
    local cel = not layer.isGroup and layer:cel(frame) or nil
    if cel then
      fileNumber = fileNumber + 1
      local filename = string.format("cel_%03d_part.png", fileNumber)
      local path = app.fs.joinPath(root, filename)
      assert(not app.fs.isFile(path), "Export exists: " .. path)
      cel.image:saveAs(path)
      manifest.cels[#manifest.cels + 1] = {
        layer = entry.path, png = filename,
        x = cel.position.x, y = cel.position.y,
        width = cel.image.width, height = cel.image.height,
        opacity = cel.opacity, z_index = cel.zIndex,
        visible = layer.isVisible, blend_mode = tostring(layer.blendMode)
      }
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
  local loaded, seen = {}, {}
  for i, item in ipairs(data.cels or {}) do
    assert(item.layer and item.png and not seen[item.layer], "Missing or duplicate layer")
    seen[item.layer] = true
    assert(not item.png:match("^[/\\]") and not item.png:match("^%a:") and not item.png:find("..", 1, true), "PNG must be relative without parent traversal")
    local existing = layerAt(sprite, item.layer)
    assert(existing or (item.create == true and not item.layer:find("/", 1, true)), "Missing layer; new layers must be explicit top-level entries")
    if existing then assert(not existing.isGroup, "Cannot assign cel to a group") end
    assert(type(item.x) == "number" and item.x % 1 == 0 and type(item.y) == "number" and item.y % 1 == 0, "Integer x/y required")
    -- Image loads one PNG without app.open's automatic numbered image-sequence import.
    loaded[i] = Image{ fromFile = app.fs.joinPath(pngRoot, item.png) }
    assert(loaded[i] and loaded[i].colorMode == ColorMode.RGB, "Expected RGBA PNG: " .. item.png)
  end
  assert(#loaded > 0, "No replacement cels")
  app.activeSprite = sprite
  app.transaction("Apply independent equipment cels", function()
    if mode == "append" then
      local frame = sprite:newEmptyFrame(data.frame)
      frame.duration = data.duration or 0.8
      -- Copy only the body/face layers explicitly selected by the artist.
      for _, item in ipairs(data.copy_cels or {}) do
        local layer = assert(layerAt(sprite, item.layer))
        local source = assert(layer:cel(item.from_frame))
        local cel = sprite:newCel(layer, data.frame, Image(source.image), source.position)
        cel.opacity, cel.zIndex = source.opacity, source.zIndex
      end
    end
    for i, item in ipairs(data.cels) do
      local layer = layerAt(sprite, item.layer)
      if not layer then
        layer = sprite:newLayer()
        layer.name = item.layer
        if item.stack_index then layer.stackIndex = item.stack_index end
      end
      local cel = layer:cel(data.frame)
      if cel then
        -- Clone the image so linked cels in earlier frames cannot be changed.
        cel.image = Image(loaded[i])
        cel.position = Point(item.x, item.y)
      else
        cel = sprite:newCel(layer, data.frame, Image(loaded[i]), Point(item.x, item.y))
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
  for _, item in ipairs(data.cels) do
    assert(layerAt(check, item.layer):cel(data.frame), "Missing saved cel")
  end
  check:close()
  print("EQUIPMENT_APPLY_OK mode=" .. mode .. " frame=" .. data.frame .. " imported=" .. #loaded)
end

local function verifyUnchanged()
  local source = assert(app.open(required("src")))
  local output = assert(app.open(required("out")))
  local through = tonumber(required("through"))
  assert(source.width == output.width and source.height == output.height and source.colorMode == output.colorMode)
  assert(through and through <= #source.frames and through <= #output.frames)
  local matched = {}
  local sourceOrder = {}
  for _, entry in ipairs(walk(source.layers, "")) do
    sourceOrder[#sourceOrder + 1] = entry.path
    local left, right = entry.layer, assert(layerAt(output, entry.path), "Missing layer: " .. entry.path)
    matched[entry.path] = true
    assert(left.isVisible == right.isVisible and left.opacity == right.opacity and left.blendMode == right.blendMode)
    if not left.isGroup then
      for f = 1, through do
        local a, b = left:cel(f), right:cel(f)
        assert((a == nil) == (b == nil), "Cel presence changed: " .. entry.path)
        if a then
          assert(a.position == b.position and a.opacity == b.opacity and a.zIndex == b.zIndex, "Cel properties changed: " .. entry.path)
          assert(a.image.width == b.image.width and a.image.height == b.image.height, "Cel bounds changed")
          for pixel in a.image:pixels() do
            assert(pixel() == b.image:getPixel(pixel.x, pixel.y), "Pixel changed: " .. entry.path .. " frame=" .. f)
          end
        end
      end
    end
  end
  local outputOrder = {}
  for _, entry in ipairs(walk(output.layers, "")) do
    if matched[entry.path] then outputOrder[#outputOrder + 1] = entry.path end
    if not matched[entry.path] and not entry.layer.isGroup then
      for f = 1, through do assert(not entry.layer:cel(f), "New layer modifies a protected frame") end
    end
  end
  assert(#sourceOrder == #outputOrder, "Original layer count changed")
  for i, path in ipairs(sourceOrder) do assert(outputOrder[i] == path, "Original layer order changed") end
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
