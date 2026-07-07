using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using static ExcelUtil;

/// <summary>
/// NPC创建（GUI版，纯代码UI）
/// 与预制版 UITestNpcCreate 功能一致，但全部控件用 IMGUI(OnGUI) 代码创建、Spine 模型用代码在场景中生成，
/// 不依赖任何运行时UI预制，方便直接在脚本里增删改控件。由 LauncherTest.StartNpcCreateGUI 挂到空物体上启动。
/// </summary>
public class TestNpcCreateGUI : MonoBehaviour
{
    #region 预览摆放可调参数（预览不对位时直接改这里）
    private const float ModelDistance = 6f;    //模型距相机的正前方距离
    private const float ModelSideOffset = 1.2f;//两个模型左右分开的距离
    private const float ModelHeightOffset = -1f;//模型相对相机视线的高度偏移
    #endregion

    #region 数据字段
    private CreatureBean creatureData;             //当前生物数据
    private bool isShowEquip = true;               //是否展示装备
    private List<long> listCreatureSkinData = new List<long>();   //自定义皮肤ID列表
    private List<long> listCreatureEquipItemIds = new List<long>();//自定义装备ID列表
    private Color colorHair = Color.white;         //头发颜色

    private SkeletonAnimation normalModel;         //标准参考模型(创建后不变)
    private SkeletonAnimation targetModel;         //目标编辑模型(随配置刷新)
    #endregion

    #region GUI输入缓存
    private string npcIdInput = "2000000001";      //NPC ID 输入框
    private string inputHP, inputDR, inputMP, inputATK, inputASPD, inputMSPD, inputSearchRange;//属性输入框
    private Vector2 scrollMain;                     //主面板滚动
    private Vector2 scrollSelect;                   //选择面板滚动
    #endregion

    #region 选择面板状态
    private enum SelectMode { None, Skin, Equip }
    private SelectMode selectMode = SelectMode.None;//当前选择模式
    private int selectShowType;                     //当前选择的皮肤类型/装备类型
    private List<SelectItem> listSelectItem = new List<SelectItem>();//候选项

    /// <summary>选择面板候选项(id + 显示名)</summary>
    private struct SelectItem
    {
        public long id;
        public string label;
        public SelectItem(long id, string label) { this.id = id; this.label = label; }
    }
    #endregion

    #region GUI样式
    private bool guiStyleInited;
    private GUIStyle titleStyle, labelStyle, sectionStyle;
    #endregion

    #region 生命周期
    /// <summary>
    /// 初始化：生成标准参考模型，等待加载目标NPC
    /// </summary>
    private void Start()
    {
        //标准参考模型(与预制版一致：生物2001基础皮肤)
        CreatureBean creatureNormalTest = new CreatureBean(2001);
        creatureNormalTest.AddSkinForBase();
        normalModel = CreateModel("NpcCreateGUI_NormalModel", -ModelSideOffset);
        CreatureHandler.Instance.SetCreatureData(normalModel, creatureNormalTest);
        SpineHandler.Instance.PlayAnim(normalModel, SpineAnimationStateEnum.Idle, creatureNormalTest, true);

        //目标模型空壳(加载NPC后填充)
        targetModel = CreateModel("NpcCreateGUI_TargetModel", ModelSideOffset);
    }

    /// <summary>
    /// 销毁时清理生成的模型
    /// </summary>
    private void OnDestroy()
    {
        if (normalModel != null) Destroy(normalModel.gameObject);
        if (targetModel != null) Destroy(targetModel.gameObject);
    }
    #endregion

    #region 模型生成与刷新
    /// <summary>
    /// 在相机正前方生成一个空的Spine模型物体
    /// </summary>
    private SkeletonAnimation CreateModel(string objName, float sideOffset)
    {
        var cam = CameraHandler.Instance.manager.mainCamera;
        GameObject obj = new GameObject(objName);
        //按相机朝向摆放，让扁平Spine正面朝向相机
        if (cam != null)
        {
            Vector3 pos = cam.transform.position
                + cam.transform.forward * ModelDistance
                + cam.transform.right * sideOffset
                + cam.transform.up * ModelHeightOffset;
            obj.transform.position = pos;
            obj.transform.rotation = cam.transform.rotation;
        }
        return obj.AddComponent<SkeletonAnimation>();
    }

    /// <summary>
    /// 刷新目标模型与所有展示数据(等价预制版 RefreshCreature)
    /// </summary>
    private void RefreshCreature()
    {
        if (creatureData == null) return;
        //从UI更新属性
        UpdateCreatureAttributeFromUI();
        //自定义皮肤/装备/发色
        creatureData.InitSkin(listCreatureSkinData);
        creatureData.InitEquip(listCreatureEquipItemIds);
        creatureData.ChangeSkinColor(CreatureSkinTypeEnum.Hair, colorHair);
        //设置spine并播放待机
        CreatureHandler.Instance.SetCreatureData(targetModel, creatureData, isNeedEquip: isShowEquip);
        SpineHandler.Instance.PlayAnim(targetModel, SpineAnimationStateEnum.Idle, creatureData, true);
    }
    #endregion

    #region 加载/属性
    /// <summary>
    /// 用默认程序打开 excel_npc_info 配置表(仅编辑器有效)
    /// </summary>
    private void OpenNpcInfoExcel()
    {
        string fullPath = Application.dataPath + "/Data/Excel/excel_npc_info[NPC信息].xlsx";
        if (!System.IO.File.Exists(fullPath))
        {
            LogUtil.LogError($"找不到NPC配置表: {fullPath}");
            return;
        }
        Application.OpenURL("file:///" + fullPath.Replace("\\", "/"));
    }

    /// <summary>
    /// 按输入框ID加载NPC并初始化
    /// </summary>
    private void OnClickLoadNpc()
    {
        if (!int.TryParse(npcIdInput, out int npcId))
        {
            LogUtil.LogError($"NPC ID 解析失败: {npcIdInput}");
            return;
        }
        NpcInfoBean npcInfoData = NpcInfoCfg.GetItemData(npcId);
        if (npcInfoData == null)
        {
            LogUtil.LogError($"找不到NPC配置: {npcId}");
            return;
        }
        creatureData = new CreatureBean(npcInfoData);
        var creatureNpcData = creatureData.GetCreatureNpcData();
        listCreatureSkinData = creatureNpcData.npcInfo.skin_data.SplitForListLong('&');
        listCreatureEquipItemIds = creatureNpcData.npcInfo.equip_item_ids.SplitForListLong('&');
        colorHair = Color.white;
        InitCreatureAttributeUI();
        RefreshCreature();
    }

    /// <summary>
    /// 用NPC数据初始化属性输入框
    /// </summary>
    private void InitCreatureAttributeUI()
    {
        var npcInfo = creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null) return;
        inputHP = $"{npcInfo.HP}";
        inputDR = $"{npcInfo.DR}";
        inputMP = $"{npcInfo.MP}";
        inputATK = $"{npcInfo.ATK}";
        inputASPD = $"{npcInfo.ASPD}";
        inputMSPD = $"{npcInfo.MSPD}";
        inputSearchRange = $"{npcInfo.attack_search_range}";
    }

    /// <summary>
    /// 从属性输入框写回NPC数据
    /// </summary>
    private void UpdateCreatureAttributeFromUI()
    {
        var npcInfo = creatureData?.creatureNpcData?.npcInfo;
        if (npcInfo == null) return;
        if (float.TryParse(inputHP, out float hp)) npcInfo.HP = hp;
        if (float.TryParse(inputDR, out float dr)) npcInfo.DR = dr;
        if (float.TryParse(inputMP, out float mp)) npcInfo.MP = mp;
        if (float.TryParse(inputATK, out float atk)) npcInfo.ATK = atk;
        if (float.TryParse(inputASPD, out float aspd)) npcInfo.ASPD = aspd;
        if (float.TryParse(inputMSPD, out float mspd)) npcInfo.MSPD = mspd;
        if (float.TryParse(inputSearchRange, out float range)) npcInfo.attack_search_range = range;
    }
    #endregion

    #region 保存
    /// <summary>
    /// 弹窗确认后把当前配置写回 excel_npc_info(仅编辑器)
    /// </summary>
    private void OnClickSave()
    {
        if (creatureData == null)
        {
            LogUtil.LogError("没有生物数据");
            return;
        }
        DialogBean dialogData = new DialogBean();
        dialogData.content = $"是否要保存生物数据 npcId:{creatureData.creatureNpcData.npcId}";
        dialogData.actionSubmit = (view, data) =>
        {
            string creatureSkinData = "";
            string creatureEquipItemIds = "";
            foreach (var item in listCreatureSkinData) creatureSkinData += $"{item}&";
            foreach (var item in listCreatureEquipItemIds) creatureEquipItemIds += $"{item}&";
#if UNITY_EDITOR
            UpdateCreatureAttributeFromUI();
            var creatureNpcData = creatureData.GetCreatureNpcData();
            long npciD = creatureNpcData.npcId;
            List<ExcelChangeData> listData = new List<ExcelChangeData>()
            {
                new ExcelChangeData(npciD,"skin_data",creatureSkinData),
                new ExcelChangeData(npciD,"equip_item_ids",creatureEquipItemIds),
                new ExcelChangeData(npciD,"HP",$"{creatureNpcData.npcInfo.HP}"),
                new ExcelChangeData(npciD,"DR",$"{creatureNpcData.npcInfo.DR}"),
                new ExcelChangeData(npciD,"MP",$"{creatureNpcData.npcInfo.MP}"),
                new ExcelChangeData(npciD,"ATK",$"{creatureNpcData.npcInfo.ATK}"),
                new ExcelChangeData(npciD,"ASPD",$"{creatureNpcData.npcInfo.ASPD}"),
                new ExcelChangeData(npciD,"MSPD",$"{creatureNpcData.npcInfo.MSPD}"),
                new ExcelChangeData(npciD,"attack_search_range",$"{creatureNpcData.npcInfo.attack_search_range}"),
            };
            ExcelUtil.SetExcelData("Assets/Data/Excel/excel_npc_info[NPC信息].xlsx", "NpcInfo", listData);
#endif
        };
        dialogData.actionCancel = (view, data) => { };
        UIHandler.Instance.ShowDialogNormal(dialogData);
    }

    /// <summary>
    /// 关闭GUI并销毁自身
    /// </summary>
    private void CloseSelf()
    {
        Destroy(gameObject);
    }
    #endregion

    #region GUI绘制
    /// <summary>
    /// IMGUI入口，绘制纯代码创建的NPC创建面板
    /// </summary>
    private void OnGUI()
    {
        InitGUIStyle();

        GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20), GUI.skin.box);
        scrollMain = GUILayout.BeginScrollView(scrollMain);

        GUILayout.Label("NPC 创建（GUI版）", titleStyle);
        GUILayout.Space(4);

        //顶部：打开NPC配置表
        if (GUILayout.Button("📂 打开 NPC 表", GUILayout.Height(26)))
            OpenNpcInfoExcel();
        GUILayout.Space(4);

        //加载行
        GUILayout.BeginHorizontal();
        GUILayout.Label("NPC ID", labelStyle, GUILayout.Width(60));
        npcIdInput = GUILayout.TextField(npcIdInput, GUILayout.Height(26));
        if (GUILayout.Button("开始创建", GUILayout.Width(90), GUILayout.Height(26)))
            OnClickLoadNpc();
        GUILayout.EndHorizontal();

        if (creatureData != null)
        {
            GUILayout.Space(6);
            if (GUILayout.Button(isShowEquip ? "装备展示：显示中(点击隐藏)" : "装备展示：隐藏中(点击显示)"))
            {
                isShowEquip = !isShowEquip;
                RefreshCreature();
            }

            DrawAttributeSection();
            DrawHairColorSection();
            DrawBodySection();
            DrawEquipSection();
            DrawCardDataSection();

            GUILayout.Space(6);
            if (GUILayout.Button("保存到 Excel", GUILayout.Height(30)))
                OnClickSave();
        }

        GUILayout.Space(6);
        if (GUILayout.Button("关闭", GUILayout.Height(26)))
            CloseSelf();

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        //右侧选择面板
        if (selectMode != SelectMode.None)
            DrawSelectPanel();
    }

    /// <summary>
    /// 属性编辑区
    /// </summary>
    private void DrawAttributeSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 属性 ──", sectionStyle);
        inputHP = DrawAttrField("HP", inputHP);
        inputDR = DrawAttrField("DR", inputDR);
        inputMP = DrawAttrField("MP", inputMP);
        inputATK = DrawAttrField("ATK", inputATK);
        inputASPD = DrawAttrField("ASPD", inputASPD);
        inputMSPD = DrawAttrField("MSPD", inputMSPD);
        inputSearchRange = DrawAttrField("搜索范围", inputSearchRange);
        if (GUILayout.Button("应用属性"))
            RefreshCreature();
    }

    /// <summary>
    /// 单行属性输入
    /// </summary>
    private string DrawAttrField(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(80));
        value = GUILayout.TextField(value ?? "", GUILayout.Height(24));
        GUILayout.EndHorizontal();
        return value;
    }

    /// <summary>
    /// 发色调节区(IMGUI无颜色选择器，用RGB滑条)
    /// </summary>
    private void DrawHairColorSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 发色 ──", sectionStyle);
        colorHair.r = DrawColorSlider("R", colorHair.r);
        colorHair.g = DrawColorSlider("G", colorHair.g);
        colorHair.b = DrawColorSlider("B", colorHair.b);
        if (GUILayout.Button("应用发色"))
            RefreshCreature();
    }

    /// <summary>
    /// 单条颜色滑条
    /// </summary>
    private float DrawColorSlider(string label, float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(30));
        value = GUILayout.HorizontalSlider(value, 0f, 1f);
        GUILayout.Label($"{value:F2}", labelStyle, GUILayout.Width(40));
        GUILayout.EndHorizontal();
        return value;
    }

    /// <summary>
    /// 身体皮肤部件区
    /// </summary>
    private void DrawBodySection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 身体皮肤 ──", sectionStyle);
        var dicAllSkins = CreatureModelInfoCfg.GetData(creatureData.creatureInfo.model_id);
        foreach (var kv in dicAllSkins)
        {
            CreatureSkinTypeEnum skinType = kv.Key;
            long currentId = GetCurrentSkinId(skinType);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{skinType.GetEnumName()}: {currentId}", labelStyle);
            if (GUILayout.Button("选择", GUILayout.Width(70), GUILayout.Height(22)))
                OpenSkinSelect(skinType);
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 装备槽区
    /// </summary>
    private void DrawEquipSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 装备 ──", sectionStyle);
        List<ItemTypeEnum> listEquipType = creatureData.creatureInfo.GetEquipItemsType();
        foreach (var equipType in listEquipType)
        {
            long currentId = GetCurrentEquipId(equipType);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{equipType.GetEnumName()}: {currentId}", labelStyle);
            if (GUILayout.Button("选择", GUILayout.Width(70), GUILayout.Height(22)))
                OpenEquipSelect(equipType);
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 卡片数据区(纯文本列出，不还原卡面美术)
    /// </summary>
    private void DrawCardDataSection()
    {
        GUILayout.Space(6);
        GUILayout.Label("── 卡片数据 ──", sectionStyle);
        GUILayout.Label($"名字: {creatureData.creatureName}", labelStyle);
        GUILayout.Label($"等级: {creatureData.level}   稀有度: {creatureData.rarity}", labelStyle);
        GUILayout.Label($"HP:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.HP)}  " +
                        $"MP:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.MP)}  " +
                        $"DR:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.DR)}", labelStyle);
        GUILayout.Label($"ATK:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.ATK)}  " +
                        $"ASPD:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.ASPD)}  " +
                        $"MSPD:{creatureData.GetAttributeInt(CreatureAttributeTypeEnum.MSPD)}", labelStyle);
        GUILayout.Label($"皮肤: {string.Join(",", listCreatureSkinData)}", labelStyle);
        GUILayout.Label($"装备: {string.Join(",", listCreatureEquipItemIds)}", labelStyle);
    }

    /// <summary>
    /// 右侧候选选择面板
    /// </summary>
    private void DrawSelectPanel()
    {
        GUILayout.BeginArea(new Rect(420, 10, 320, Screen.height - 20), GUI.skin.box);
        GUILayout.Label(selectMode == SelectMode.Skin ? "选择皮肤" : "选择装备", titleStyle);
        if (GUILayout.Button("关闭选择"))
            CloseSelect();
        scrollSelect = GUILayout.BeginScrollView(scrollSelect);
        //当前该部位/该槽位已选中的id，用于列表内高亮
        long currentId = selectMode == SelectMode.Skin
            ? GetCurrentSkinId((CreatureSkinTypeEnum)selectShowType)
            : GetCurrentEquipId((ItemTypeEnum)selectShowType);
        foreach (var item in listSelectItem)
        {
            //选中项前加勾并变色，方便随时对照切换
            bool isCurrent = item.id == currentId;
            Color oldColor = GUI.color;
            if (isCurrent) GUI.color = Color.green;
            string label = isCurrent ? $"✔ {item.label}" : item.label;
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                if (selectMode == SelectMode.Skin) OnSelectSkin(item.id);
                else OnSelectEquip(item.id);
                GUI.color = oldColor;
                break;
            }
            GUI.color = oldColor;
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
    #endregion

    #region 选择逻辑
    /// <summary>
    /// 取某皮肤部位当前选中的皮肤id
    /// </summary>
    private long GetCurrentSkinId(CreatureSkinTypeEnum skinType)
    {
        foreach (var id in listCreatureSkinData)
        {
            var info = CreatureModelInfoCfg.GetItemData(id);
            if (info != null && info.GetPartType() == skinType)
                return id;
        }
        return 0;
    }

    /// <summary>
    /// 取某装备类型当前选中的装备id
    /// </summary>
    private long GetCurrentEquipId(ItemTypeEnum itemType)
    {
        foreach (var id in listCreatureEquipItemIds)
        {
            var info = ItemsInfoCfg.GetItemData(id);
            if (info != null && info.GetItemType() == itemType)
                return id;
        }
        return 0;
    }

    /// <summary>
    /// 打开皮肤候选列表
    /// </summary>
    private void OpenSkinSelect(CreatureSkinTypeEnum skinType)
    {
        selectMode = SelectMode.Skin;
        selectShowType = (int)skinType;
        listSelectItem.Clear();
        listSelectItem.Add(new SelectItem(0, "取消(不放置)"));
        var dicAllSkins = CreatureModelInfoCfg.GetData(creatureData.creatureInfo.model_id);
        if (dicAllSkins.TryGetValue(skinType, out var listSkinData))
        {
            foreach (var info in listSkinData)
                listSelectItem.Add(new SelectItem(info.id, $"{info.id}  {info.res_name}"));
        }
        scrollSelect = Vector2.zero;
    }

    /// <summary>
    /// 打开装备候选列表
    /// </summary>
    private void OpenEquipSelect(ItemTypeEnum itemType)
    {
        selectMode = SelectMode.Equip;
        selectShowType = (int)itemType;
        listSelectItem.Clear();
        listSelectItem.Add(new SelectItem(0, "取消(不放置)"));
        var creatureModelData = CreatureModelCfg.GetItemData(creatureData.creatureInfo.model_id);
        var listItemInfo = ItemsInfoCfg.GetDataByCreatureModelId(creatureModelData.id);
        foreach (var itemInfo in listItemInfo)
        {
            if (itemInfo.GetItemType() == itemType)
                listSelectItem.Add(new SelectItem(itemInfo.id, $"{itemInfo.id}  {itemInfo.icon_res}"));
        }
        scrollSelect = Vector2.zero;
    }

    /// <summary>
    /// 选中皮肤：替换同部位皮肤后刷新(不关闭选择面板，方便随时切换)
    /// </summary>
    private void OnSelectSkin(long showId)
    {
        var skinType = (CreatureSkinTypeEnum)selectShowType;
        //移除同部位旧皮肤
        for (int i = 0; i < listCreatureSkinData.Count; i++)
        {
            var info = CreatureModelInfoCfg.GetItemData(listCreatureSkinData[i]);
            if (info != null && info.GetPartType() == skinType)
            {
                listCreatureSkinData.RemoveAt(i);
                i--;
            }
        }
        if (showId != 0)
            listCreatureSkinData.Add(showId);
        RefreshCreature();
    }

    /// <summary>
    /// 选中装备：替换同类型装备后刷新(不关闭选择面板，方便随时切换)
    /// </summary>
    private void OnSelectEquip(long showId)
    {
        var itemType = (ItemTypeEnum)selectShowType;
        //移除同类型旧装备
        for (int i = 0; i < listCreatureEquipItemIds.Count; i++)
        {
            var info = ItemsInfoCfg.GetItemData(listCreatureEquipItemIds[i]);
            if (info != null && info.GetItemType() == itemType)
            {
                listCreatureEquipItemIds.RemoveAt(i);
                i--;
            }
        }
        if (showId != 0)
            listCreatureEquipItemIds.Add(showId);
        RefreshCreature();
    }

    /// <summary>
    /// 关闭选择面板
    /// </summary>
    private void CloseSelect()
    {
        selectMode = SelectMode.None;
        listSelectItem.Clear();
    }
    #endregion

    #region GUI样式
    /// <summary>
    /// 懒初始化GUI样式，只初始化一次
    /// </summary>
    private void InitGUIStyle()
    {
        if (guiStyleInited) return;
        guiStyleInited = true;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
        sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
    }
    #endregion
}
