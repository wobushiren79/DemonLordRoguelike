-- 在时间轴选中素材对应的 Cel，设置或解除 Prepare-For-Spine_AI.lua 使用的排除标记。
local marker = "[spine:skip]"
local excludedColor = Color{ r = 255, g = 72, b = 72 }
local propertyKey = "dlr/spine-export"

-- 标记独占一行。保留其他行的原文，包括原有空行和换行格式。
local function readMark(data)
    local kept = {}
    local marked = false
    local start = 1
    local separator = ""
    while true do
        local lineEnd = data:find("[\r\n]", start)
        local line = data:sub(start, lineEnd and lineEnd - 1)
        if line == marker then
            marked = true
        else
            kept[#kept + 1] = (#kept > 0 and separator or "") .. line
        end
        if not lineEnd then break end
        separator = data:sub(lineEnd, lineEnd + 1) == "\r\n" and "\r\n" or data:sub(lineEnd, lineEnd)
        start = lineEnd + #separator
    end
    return marked, table.concat(kept)
end

local sprite = app.sprite
if not sprite then
    app.alert("请打开 Aseprite 母版，并在时间轴选中素材。")
    return
end

local range = app.range
local cels = {}
if range.type == RangeType.CELS then
    cels = range.cels
elseif range.type == RangeType.EMPTY then
    cels = { app.cel }
end

local markedCount = 0
for _, cel in ipairs(cels) do
    if readMark(cel.data) then markedCount = markedCount + 1 end
end

local dialog = Dialog("Spine 导出标记")
dialog:label{ text = "已排除的素材会显示红色时间轴格。操作后按 Ctrl+S 保存。" }
if #cels > 0 then
    dialog:label{ text = string.format("选中 %d 格，其中 %d 格已排除。", #cels, markedCount) }
    dialog:button{ id = "exclude", text = "排除导出" }
    dialog:button{ id = "restore", text = "恢复导出" }
else
    dialog:label{ text = "要排除或恢复素材，请在时间轴点选有内容的格子。" }
end
dialog:newrow()
dialog:button{ id = "sync", text = "刷新整份母版的标记颜色" }
dialog:button{ id = "cancel", text = "取消" }
dialog:show()
local choice = dialog.data
if not (choice.exclude or choice.restore or choice.sync) then return end

local changes = {}
for _, cel in ipairs(choice.sync and sprite.cels or cels) do
    local marked, notes = readMark(cel.data)
    local exclude = choice.exclude == true or (choice.sync == true and marked)
    local originalColor = cel.properties(propertyKey).originalColor
    local colorNeedsUpdate
    if exclude then
        colorNeedsUpdate = originalColor == nil or cel.color.rgbaPixel ~= excludedColor.rgbaPixel
    else
        colorNeedsUpdate = originalColor ~= nil
    end
    if marked ~= exclude or colorNeedsUpdate then
        changes[cel.image.id] = {
            cel = cel, notes = notes, marked = marked, exclude = exclude, originalColor = originalColor
        }
    end
end
if not next(changes) then return end

-- 链接格共用备注与颜色。选区操作须涵盖链接格；全母版刷新只按已有标记配色。
if not choice.sync then
    for _, other in ipairs(sprite.cels) do
        if changes[other.image.id] then
            local inSelection = (range.type == RangeType.CELS and range:contains(other))
                or (range.type == RangeType.EMPTY and other == app.cel)
            if not inSelection then
                app.alert(string.format("选区与图层「%s」第 %d 帧的单元格有链接。\n请一起选中所有链接格，或先对要单独处理的格子执行「取消链接 Cel」。",
                    other.layer.name, other.frameNumber))
                return
            end
        end
    end
end

app.transaction("设置 Spine 导出标记", function()
    for _, item in pairs(changes) do
        local cel = item.cel
        if item.marked ~= item.exclude then
            local data = item.notes
            if item.exclude then
                data = data == "" and marker or data .. "\r\n" .. marker
            end
            cel.data = data
        end
        if item.exclude then
            if item.originalColor == nil then
                cel.properties(propertyKey).originalColor = cel.color.rgbaPixel
            end
            cel.color = excludedColor
        elseif item.originalColor ~= nil then
            -- 若画师已手动改成其他色，保留此次手调；否则取回排除前的原色。
            if cel.color.rgbaPixel == excludedColor.rgbaPixel then cel.color = Color(item.originalColor) end
            cel.properties(propertyKey).originalColor = nil
        end
    end
end)
app.refresh()
