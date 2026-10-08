using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗测试-场景编辑模式(GameTestEditor 的 partial)：
/// 开始测试后在运行中编辑战斗场景(Hierarchy 拖入新预制/移动旋转缩放原有物体)，
/// 「保存场景编辑」把新增预制与变换修改写回场景预制资产，「还原场景」恢复到开始测试时(或上次保存后)的样子。
/// </summary>
public partial class GameTestEditor
{
    #region 场景编辑-状态
    /// <summary>场景编辑备份快照(战斗场景加载完成后的整场景克隆, HideAndDontSave, 仅内存)</summary>
    private GameObject sceneEditBackup;
    /// <summary>是否等待战斗场景加载完成后拍快照(点击开始测试时置 true)</summary>
    private bool sceneEditPendingSnapshot;
    /// <summary>点击开始测试时已在场的旧战斗场景(等待其被新场景替换后才拍快照, 防止拍到未卸载的旧场景)</summary>
    private GameObject sceneEditPrevSceneObj;
    /// <summary>当前快照对应的场景预制资产路径</summary>
    private string sceneEditPrefabPath;
    /// <summary>上次改动检测结果(仅用于面板显示; null=未检测)</summary>
    private SceneEditDiffResult sceneEditLastDiff;
    /// <summary>是否已注册编辑器轮询与 Play 状态回调</summary>
    private bool sceneEditCallbackRegistered;
    /// <summary>道路物体名(运行时由战斗逻辑动态加载进场景, 见 WorldManager.PrefabNameFightSceneRoad; 编辑/保存/还原均排除)</summary>
    private const string SCENE_EDIT_ROAD_NAME = "FightSceneRoad";
    /// <summary>备份根节点名后缀(用于泄漏扫描)</summary>
    private const string SCENE_EDIT_BACKUP_SUFFIX = "_SceneEditBackup";
    /// <summary>变换变化判定精度(位置/缩放)</summary>
    private const float SCENE_EDIT_TRS_EPSILON = 1e-8f;
    #endregion

    #region 场景编辑-改动数据
    /// <summary>一条变换修改记录(原有物体的位置/旋转/缩放变化)</summary>
    private class SceneEditChangedItem
    {
        /// <summary>从场景根出发的定位路径(每段=名字#同名序号)</summary>
        public string path;
        /// <summary>运行时物体(保存时取其当前 TRS 写回预制)</summary>
        public Transform runtimeTF;
    }

    /// <summary>一条新增物体记录(用户拖入场景的预制/物体, 只收集最外层节点)</summary>
    private class SceneEditAddedItem
    {
        /// <summary>父节点定位路径(空=场景根)</summary>
        public string parentPath;
        /// <summary>物体名(写回预制时用此名)</summary>
        public string name;
        /// <summary>来源预制资产路径(预制实例时非空; 空=非预制物体走克隆轨道)</summary>
        public string sourcePrefabPath;
        /// <summary>运行时物体(取 TRS; 克隆轨道时整体克隆它)</summary>
        public Transform runtimeTF;
        /// <summary>是否预制实例(是=InstantiatePrefab 保链接; 否=整体克隆)</summary>
        public bool isPrefabInstance;
    }

    /// <summary>一次场景改动检测的结果</summary>
    private class SceneEditDiffResult
    {
        /// <summary>变换修改列表</summary>
        public List<SceneEditChangedItem> listChanged = new List<SceneEditChangedItem>();
        /// <summary>新增物体列表</summary>
        public List<SceneEditAddedItem> listAdded = new List<SceneEditAddedItem>();
        /// <summary>被删除的原有物体路径(仅提示, 保存不会从预制删除)</summary>
        public List<string> listDeletedOriginal = new List<string>();
        /// <summary>被跳过的物体及原因(仅提示)</summary>
        public List<string> listSkipped = new List<string>();
    }
    #endregion

    #region 场景编辑-会话 UI
    /// <summary>
    /// 绘制场景编辑会话区(快照状态/检测改动/保存/还原按钮)。仅战斗测试模式选「场景编辑」时显示。
    /// </summary>
    private void DrawSceneEditSession()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🎬 场景编辑会话", EditorStyles.boldLabel);

        GameObject sceneObj = GetSceneEditFightScene(out _, false);
        bool hasBackup = sceneEditBackup != null;
        bool canOperate = Application.isPlaying && hasBackup && sceneObj != null;

        //状态行
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("未在运行中。", MessageType.None);
        }
        else if (sceneEditPendingSnapshot)
        {
            EditorGUILayout.HelpBox("等待战斗场景加载完成后拍摄快照...", MessageType.None);
        }
        else if (!hasBackup)
        {
            EditorGUILayout.HelpBox("尚未拍摄场景快照，点击上方「开始场景编辑测试」进入。", MessageType.None);
        }
        else
        {
            EditorGUILayout.LabelField("场景预制", sceneEditPrefabPath ?? "(未知)", EditorStyles.miniLabel);
            if (sceneObj == null)
            {
                EditorGUILayout.HelpBox("当前不在战斗场景中(可能已切走)。", MessageType.Warning);
            }
        }

        //上次检测结果
        if (sceneEditLastDiff != null)
        {
            EditorGUILayout.LabelField($"检测: 新增 {sceneEditLastDiff.listAdded.Count} / 修改 {sceneEditLastDiff.listChanged.Count} / 被删(不保存) {sceneEditLastDiff.listDeletedOriginal.Count}", EditorStyles.miniLabel);
        }

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(!canOperate))
        {
            if (GUILayout.Button("🔍 检测改动"))
            {
                sceneEditLastDiff = ComputeSceneEditDiff(sceneObj);
                LogSceneEditDiff(sceneEditLastDiff);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        using (new EditorGUI.DisabledScope(!canOperate))
        {
            if (GUILayout.Button("💾 保存场景编辑", GUILayout.Height(26)))
            {
                SaveSceneEdit(sceneObj);
            }
        }
        GUI.backgroundColor = new Color(0.9f, 0.6f, 0.3f);
        using (new EditorGUI.DisabledScope(!canOperate))
        {
            if (GUILayout.Button("↩️ 还原场景", GUILayout.Height(26)))
            {
                RestoreSceneEdit(sceneObj);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("在 Hierarchy 中编辑战斗场景：拖入新预制、移动/旋转/缩放原有物体后——\n" +
            "「保存场景编辑」= 把新增预制与原有物体的位置/旋转/缩放修改写回场景预制资产(不保存删除/激活状态/组件修改, 道路不纳入)；保存成功后重新拍摄快照。\n" +
            "「还原场景」= 新加的删除、动过的回位、删掉的补回，回到开始测试时(或上次保存后)的样子。\n" +
            "注意：结算后点「下一步」重开战斗会重载场景，未保存的编辑会丢失。", MessageType.Info);
        EditorGUILayout.EndVertical();
    }
    #endregion

    #region 场景编辑-快照
    /// <summary>
    /// 点击开始场景编辑测试时调用：清旧快照与泄漏备份，标记等待战斗场景加载完成后拍摄初始快照
    /// </summary>
    private void BeginSceneEditSession()
    {
        RegisterSceneEditCallback();
        DestroySceneEditBackup();
        SweepLeakedSceneEditBackups();
        //记录当前已在场的旧战斗场景，轮询时等它被新场景替换(重进战斗会先卸载旧场景，直接拍到旧场景会导致快照内容错误)
        sceneEditPrevSceneObj = Application.isPlaying && WorldHandler.Instance != null
            ? WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.Fight) : null;
        sceneEditPendingSnapshot = true;
        sceneEditLastDiff = null;
    }

    /// <summary>
    /// 注册编辑器轮询与 Play 状态回调(幂等)
    /// </summary>
    private void RegisterSceneEditCallback()
    {
        if (sceneEditCallbackRegistered) return;
        sceneEditCallbackRegistered = true;
        EditorApplication.update += OnSceneEditUpdate;
        EditorApplication.playModeStateChanged += OnSceneEditPlayModeChanged;
    }

    /// <summary>
    /// 编辑器轮询：等待战斗场景与防守核心就绪后拍摄初始快照(此时场景位置/道路/Details 均已处理完, 才是最终初始状态)
    /// </summary>
    private void OnSceneEditUpdate()
    {
        if (!sceneEditPendingSnapshot) return;
        if (!Application.isPlaying)
        {
            sceneEditPendingSnapshot = false;
            return;
        }
        GameObject sceneObj = GetSceneEditFightScene(out _, true);
        //旧场景尚未被替换时继续等待(重进战斗时先卸载旧场景再加载新场景)
        if (sceneObj == null || sceneObj == sceneEditPrevSceneObj) return;
        CaptureSceneEditSnapshot(sceneObj);
        Repaint();
    }

    /// <summary>
    /// Play 状态变化：退出 Play 时销毁备份快照(HideAndDontSave 物体需手动清理)
    /// </summary>
    private void OnSceneEditPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            DestroySceneEditBackup();
            sceneEditPendingSnapshot = false;
            sceneEditPrevSceneObj = null;
            sceneEditLastDiff = null;
        }
    }

    /// <summary>
    /// 获取当前战斗场景根。requireFightReady=true 时要求战斗逻辑与防守核心已就绪(供初始快照定时使用)
    /// </summary>
    /// <param name="fightSceneId">输出当前战斗的场景 ID(读不到为 0)</param>
    /// <param name="requireFightReady">是否要求防守核心已创建</param>
    private GameObject GetSceneEditFightScene(out long fightSceneId, bool requireFightReady)
    {
        fightSceneId = 0;
        if (!Application.isPlaying || WorldHandler.Instance == null) return null;
        GameObject sceneObj = WorldHandler.Instance.GetCurrentScene(GameSceneTypeEnum.Fight);
        if (sceneObj == null) return null;
        var fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        if (requireFightReady && (fightLogic?.fightData == null || fightLogic.fightData.fightDefenseCoreCreature == null)) return null;
        if (fightLogic?.fightData != null) fightSceneId = fightLogic.fightData.fightSceneId;
        return sceneObj;
    }

    /// <summary>
    /// 拍摄场景快照：克隆整个场景根为 HideAndDontSave 的内存备份，并记录场景预制资产路径
    /// </summary>
    private void CaptureSceneEditSnapshot(GameObject sceneObj)
    {
        DestroySceneEditBackup();
        sceneEditBackup = Object.Instantiate(sceneObj);
        sceneEditBackup.name = sceneObj.name + SCENE_EDIT_BACKUP_SUFFIX;
        sceneEditBackup.SetActive(false);
        sceneEditBackup.hideFlags = HideFlags.HideAndDontSave;
        //以实际进入战斗的场景为准取预制路径
        sceneEditPrefabPath = null;
        var fightLogic = GameHandler.Instance.manager.GetGameLogic<GameFightLogic>();
        long fightSceneId = fightLogic?.fightData != null ? fightLogic.fightData.fightSceneId : 0;
        var fightSceneInfo = FightSceneCfg.GetItemData(fightSceneId);
        if (fightSceneInfo != null)
        {
            sceneEditPrefabPath = $"{PathInfo.FightScenePrefabPath}/{fightSceneInfo.name_res}";
        }
        sceneEditPendingSnapshot = false;
        sceneEditPrevSceneObj = null;
        sceneEditLastDiff = null;
        LogUtil.Log($"[场景编辑] 已拍摄场景快照: {sceneObj.name} (预制: {sceneEditPrefabPath})");
    }

    /// <summary>
    /// 销毁备份快照
    /// </summary>
    private void DestroySceneEditBackup()
    {
        if (sceneEditBackup != null)
        {
            Object.DestroyImmediate(sceneEditBackup);
            sceneEditBackup = null;
        }
    }

    /// <summary>
    /// 清扫泄漏的备份快照(域重载后字段引用丢失但 HideAndDontSave 物体可能残留, 按名字后缀+hideFlags 识别)
    /// </summary>
    private void SweepLeakedSceneEditBackups()
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            var obj = allObjects[i];
            if (obj.name.EndsWith(SCENE_EDIT_BACKUP_SUFFIX) && obj.hideFlags == HideFlags.HideAndDontSave)
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

    /// <summary>
    /// 是否道路物体(运行时由战斗逻辑动态加载进场景根, 实例名可能是 FightSceneRoad 或 FightSceneRoad(Clone))
    /// </summary>
    private bool IsSceneEditRoad(string objName)
    {
        return objName == SCENE_EDIT_ROAD_NAME || objName == SCENE_EDIT_ROAD_NAME + "(Clone)";
    }
    #endregion

    #region 场景编辑-改动检测
    /// <summary>
    /// 比对备份快照与当前运行时场景, 得出新增物体/变换修改/被删原物体
    /// </summary>
    private SceneEditDiffResult ComputeSceneEditDiff(GameObject sceneObj)
    {
        var result = new SceneEditDiffResult();
        if (sceneObj == null || sceneEditBackup == null) return result;
        DiffSceneEditLevel(sceneEditBackup.transform, sceneObj.transform, "", result, true);
        return result;
    }

    /// <summary>
    /// 逐层比对：备份子物体按「同名第N个」匹配运行时子物体；未匹配的运行时子物体=新增(只收集最外层节点), 未匹配的备份子物体=被删(仅提示不保存)
    /// </summary>
    private void DiffSceneEditLevel(Transform backupParent, Transform runtimeParent, string parentPath, SceneEditDiffResult result, bool isRootLevel)
    {
        var setMatched = new HashSet<Transform>();
        for (int i = 0; i < backupParent.childCount; i++)
        {
            Transform backupChild = backupParent.GetChild(i);
            //道路由战斗逻辑动态管理, 不参与编辑与保存
            if (isRootLevel && IsSceneEditRoad(backupChild.name)) continue;
            int occurrence = GetNameOccurrence(backupParent, i);
            Transform runtimeChild = FindChildByNameOccurrence(runtimeParent, backupChild.name, occurrence, setMatched);
            string childPath = $"{parentPath}/{backupChild.name}#{occurrence}";
            if (runtimeChild == null)
            {
                result.listDeletedOriginal.Add(childPath);
                continue;
            }
            setMatched.Add(runtimeChild);
            //变换变化检测(位置/旋转/缩放)
            bool isChanged = (runtimeChild.localPosition - backupChild.localPosition).sqrMagnitude > SCENE_EDIT_TRS_EPSILON
                || Quaternion.Angle(runtimeChild.localRotation, backupChild.localRotation) > 0.001f
                || (runtimeChild.localScale - backupChild.localScale).sqrMagnitude > SCENE_EDIT_TRS_EPSILON;
            if (isChanged)
            {
                result.listChanged.Add(new SceneEditChangedItem { path = childPath, runtimeTF = runtimeChild });
            }
            DiffSceneEditLevel(backupChild, runtimeChild, childPath, result, false);
        }
        //未匹配的运行时子物体=用户新增(其子树随根节点一起处理, 不再递归)
        for (int i = 0; i < runtimeParent.childCount; i++)
        {
            Transform runtimeChild = runtimeParent.GetChild(i);
            if (setMatched.Contains(runtimeChild)) continue;
            if (isRootLevel && IsSceneEditRoad(runtimeChild.name)) continue;
            string sourcePrefabPath = PrefabUtility.IsAnyPrefabInstanceRoot(runtimeChild.gameObject)
                ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(runtimeChild.gameObject) : "";
            //拖入场景预制自身(套娃)跳过
            if (!sourcePrefabPath.IsNull() && sourcePrefabPath == sceneEditPrefabPath)
            {
                result.listSkipped.Add($"{parentPath}/{runtimeChild.name} (场景预制自身, 不可嵌套)");
                continue;
            }
            result.listAdded.Add(new SceneEditAddedItem
            {
                parentPath = parentPath,
                name = runtimeChild.name,
                sourcePrefabPath = sourcePrefabPath,
                runtimeTF = runtimeChild,
                isPrefabInstance = !sourcePrefabPath.IsNull(),
            });
        }
    }

    /// <summary>
    /// 统计父节点下第 index 个孩子是其同名孩子中的第几个(0 起)
    /// </summary>
    private int GetNameOccurrence(Transform parent, int index)
    {
        string targetName = parent.GetChild(index).name;
        int occurrence = 0;
        for (int i = 0; i < index; i++)
        {
            if (parent.GetChild(i).name == targetName) occurrence++;
        }
        return occurrence;
    }

    /// <summary>
    /// 在父节点下查找「同名第N个(0起)」且未被匹配的子节点
    /// </summary>
    private Transform FindChildByNameOccurrence(Transform parent, string childName, int occurrence, HashSet<Transform> setMatched)
    {
        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name != childName || setMatched.Contains(child)) continue;
            if (count == occurrence) return child;
            count++;
        }
        return null;
    }

    /// <summary>
    /// 按「名字#同名序号」路径段在预制内容中定位节点(空路径返回根)
    /// </summary>
    private Transform FindInContentsByPath(Transform contentsRoot, string path)
    {
        if (path.IsNull()) return contentsRoot;
        string[] segments = path.Split('/');
        Transform current = contentsRoot;
        var setEmpty = new HashSet<Transform>();
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i].IsNull()) continue;
            string segment = segments[i];
            int hashIndex = segment.LastIndexOf('#');
            string segName = hashIndex >= 0 ? segment.Substring(0, hashIndex) : segment;
            int occurrence = 0;
            if (hashIndex >= 0) int.TryParse(segment.Substring(hashIndex + 1), out occurrence);
            current = FindChildByNameOccurrence(current, segName, occurrence, setEmpty);
            if (current == null) return null;
        }
        return current;
    }

    /// <summary>
    /// 把检测结果明细输出到 Console(新增/修改/被删/跳过各列路径)
    /// </summary>
    private void LogSceneEditDiff(SceneEditDiffResult diff)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[场景编辑] 检测结果: 新增 {diff.listAdded.Count} / 修改 {diff.listChanged.Count} / 被删 {diff.listDeletedOriginal.Count} / 跳过 {diff.listSkipped.Count}");
        for (int i = 0; i < diff.listAdded.Count; i++)
        {
            var item = diff.listAdded[i];
            sb.AppendLine($"  + 新增 {(item.isPrefabInstance ? "预制" : "物体")}: {item.parentPath}/{item.name}{(item.isPrefabInstance ? $" ← {item.sourcePrefabPath}" : " (整体克隆)")}");
        }
        for (int i = 0; i < diff.listChanged.Count; i++)
        {
            sb.AppendLine($"  ~ 修改: {diff.listChanged[i].path}");
        }
        for (int i = 0; i < diff.listDeletedOriginal.Count; i++)
        {
            sb.AppendLine($"  - 被删(不保存): {diff.listDeletedOriginal[i]}");
        }
        for (int i = 0; i < diff.listSkipped.Count; i++)
        {
            sb.AppendLine($"  × 跳过: {diff.listSkipped[i]}");
        }
        LogUtil.Log(sb.ToString());
    }
    #endregion

    #region 场景编辑-保存
    /// <summary>
    /// 保存场景编辑：diff 后二次确认，把新增预制与原有物体的 TRS 修改写回场景预制资产；成功后重新拍摄快照(存档点语义)
    /// </summary>
    private void SaveSceneEdit(GameObject sceneObj)
    {
        if (sceneObj == null || sceneEditBackup == null)
        {
            EditorUtility.DisplayDialog("场景编辑", "当前没有可保存的场景编辑会话。", "确定");
            return;
        }
        if (sceneEditPrefabPath.IsNull() || !System.IO.File.Exists(sceneEditPrefabPath))
        {
            EditorUtility.DisplayDialog("场景编辑", $"未找到场景预制资产:\n{sceneEditPrefabPath}", "确定");
            return;
        }
        SceneEditDiffResult diff = ComputeSceneEditDiff(sceneObj);
        sceneEditLastDiff = diff;
        if (diff.listChanged.Count == 0 && diff.listAdded.Count == 0)
        {
            string tipDeleted = diff.listDeletedOriginal.Count > 0 ? $"\n(检测到 {diff.listDeletedOriginal.Count} 个原物体被删除, 但保存不会从预制中删除)" : "";
            EditorUtility.DisplayDialog("场景编辑", $"没有检测到可保存的改动。{tipDeleted}", "确定");
            return;
        }
        //确认框(含数量与被删/跳过提示)
        var sbConfirm = new StringBuilder();
        sbConfirm.AppendLine($"将以下改动写回场景预制:\n{sceneEditPrefabPath}\n");
        sbConfirm.AppendLine($"新增预制/物体: {diff.listAdded.Count} 个");
        sbConfirm.AppendLine($"变换修改(位置/旋转/缩放): {diff.listChanged.Count} 个");
        if (diff.listDeletedOriginal.Count > 0)
        {
            sbConfirm.AppendLine($"\n注意: {diff.listDeletedOriginal.Count} 个原物体在场景中被删除, 保存不会删除预制中的对应物体(还原可补回)。");
        }
        if (diff.listSkipped.Count > 0)
        {
            sbConfirm.AppendLine($"跳过: {diff.listSkipped.Count} 个(明细见 Console)。");
        }
        if (!EditorUtility.DisplayDialog("保存场景编辑", sbConfirm.ToString(), "保存", "取消")) return;

        int changedApplied = 0;
        int addedApplied = 0;
        GameObject contentsRoot = PrefabUtility.LoadPrefabContents(sceneEditPrefabPath);
        try
        {
            //① 变换修改: 按路径在预制内容中定位并写 TRS
            for (int i = 0; i < diff.listChanged.Count; i++)
            {
                var item = diff.listChanged[i];
                Transform target = FindInContentsByPath(contentsRoot.transform, item.path);
                if (target == null)
                {
                    LogUtil.LogWarning($"[场景编辑] 预制中未找到 {item.path}, 跳过该修改");
                    continue;
                }
                target.localPosition = item.runtimeTF.localPosition;
                target.localRotation = item.runtimeTF.localRotation;
                target.localScale = item.runtimeTF.localScale;
                changedApplied++;
            }
            //② 新增: 预制实例走 InstantiatePrefab 保链接; 非预制物体整体克隆(内部预制链接保留, 组件当前值一并拷贝)
            for (int i = 0; i < diff.listAdded.Count; i++)
            {
                var item = diff.listAdded[i];
                Transform parentTF = FindInContentsByPath(contentsRoot.transform, item.parentPath);
                if (parentTF == null)
                {
                    LogUtil.LogWarning($"[场景编辑] 预制中未找到父节点 {item.parentPath}, 跳过新增 {item.name}");
                    continue;
                }
                GameObject newObj;
                if (item.isPrefabInstance)
                {
                    GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(item.sourcePrefabPath);
                    if (source == null)
                    {
                        LogUtil.LogWarning($"[场景编辑] 加载来源预制失败 {item.sourcePrefabPath}, 跳过新增 {item.name}");
                        continue;
                    }
                    newObj = (GameObject)PrefabUtility.InstantiatePrefab(source, parentTF);
                }
                else
                {
                    newObj = Object.Instantiate(item.runtimeTF.gameObject, parentTF);
                }
                newObj.name = item.name;
                newObj.transform.localPosition = item.runtimeTF.localPosition;
                newObj.transform.localRotation = item.runtimeTF.localRotation;
                newObj.transform.localScale = item.runtimeTF.localScale;
                addedApplied++;
            }
            if (changedApplied > 0 || addedApplied > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(contentsRoot, sceneEditPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contentsRoot);
        }
        LogUtil.Log($"[场景编辑] 已保存到 {sceneEditPrefabPath}: 新增 {addedApplied}, 修改 {changedApplied}");
        //保存成功后重新拍摄快照(之后的「还原」回到本次保存的状态)
        CaptureSceneEditSnapshot(sceneObj);
        EditorUtility.DisplayDialog("保存场景编辑", $"已保存: 新增 {addedApplied} 个, 修改 {changedApplied} 个。\n预制已更新并重新拍摄快照。", "确定");
    }
    #endregion

    #region 场景编辑-还原
    /// <summary>
    /// 还原场景：按快照恢复——新加的删除、动过的回位、删掉的从备份克隆补回
    /// </summary>
    private void RestoreSceneEdit(GameObject sceneObj)
    {
        if (sceneObj == null || sceneEditBackup == null)
        {
            EditorUtility.DisplayDialog("场景编辑", "当前没有可还原的场景快照。", "确定");
            return;
        }
        if (!EditorUtility.DisplayDialog("还原场景",
            "将场景还原到开始测试时(或上次保存后)的样子:\n• 新加的物体被删除\n• 移动/旋转/缩放过的物体回到原位\n• 被删除的原有物体补回\n(道路不受影响)",
            "还原", "取消"))
        {
            return;
        }
        RestoreSceneEditLevel(sceneEditBackup.transform, sceneObj.transform, true);
        sceneEditLastDiff = null;
        LogUtil.Log("[场景编辑] 已还原场景");
    }

    /// <summary>
    /// 逐层还原：先匹配备份与运行时子物体, 再删除未匹配的新增物体, 最后恢复每个原物体的 TRS/激活/顺序(缺失的克隆补回)
    /// </summary>
    private void RestoreSceneEditLevel(Transform backupParent, Transform runtimeParent, bool isRootLevel)
    {
        var setMatched = new HashSet<Transform>();
        var listPairs = new List<KeyValuePair<Transform, Transform>>();
        //① 匹配: 备份子物体找运行时对应(道路跳过)
        for (int i = 0; i < backupParent.childCount; i++)
        {
            Transform backupChild = backupParent.GetChild(i);
            if (isRootLevel && IsSceneEditRoad(backupChild.name)) continue;
            int occurrence = GetNameOccurrence(backupParent, i);
            Transform runtimeChild = FindChildByNameOccurrence(runtimeParent, backupChild.name, occurrence, setMatched);
            if (runtimeChild != null) setMatched.Add(runtimeChild);
            listPairs.Add(new KeyValuePair<Transform, Transform>(backupChild, runtimeChild));
        }
        //② 删除未匹配的运行时子物体(用户新加的; 道路除外)
        for (int i = runtimeParent.childCount - 1; i >= 0; i--)
        {
            Transform runtimeChild = runtimeParent.GetChild(i);
            if (setMatched.Contains(runtimeChild)) continue;
            if (isRootLevel && IsSceneEditRoad(runtimeChild.name)) continue;
            Object.DestroyImmediate(runtimeChild.gameObject);
        }
        //③ 恢复每个备份子物体: 缺失则克隆补回, 否则恢复 TRS/激活并递归; 最后按备份顺序复位
        for (int i = 0; i < listPairs.Count; i++)
        {
            Transform backupChild = listPairs[i].Key;
            Transform runtimeChild = listPairs[i].Value;
            if (runtimeChild == null)
            {
                GameObject cloneObj = Object.Instantiate(backupChild.gameObject, runtimeParent);
                cloneObj.name = backupChild.name;
                cloneObj.SetActive(backupChild.gameObject.activeSelf);
                runtimeChild = cloneObj.transform;
            }
            else
            {
                runtimeChild.localPosition = backupChild.localPosition;
                runtimeChild.localRotation = backupChild.localRotation;
                runtimeChild.localScale = backupChild.localScale;
                runtimeChild.gameObject.SetActive(backupChild.gameObject.activeSelf);
                RestoreSceneEditLevel(backupChild, runtimeChild, false);
            }
            runtimeChild.SetSiblingIndex(backupChild.GetSiblingIndex());
        }
    }
    #endregion
}
