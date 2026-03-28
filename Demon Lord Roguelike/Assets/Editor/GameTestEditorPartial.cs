using System.Collections.Generic;
using UnityEditor;

public partial class GameTestEditor
{
    // 基础测试参数
    public int testDataCardNum = 20;
    public int fightSceneId = 1;
    public string fightCardId = "2002";
    public int fightSceneRoadNum = 1;
    public int fightSceneRoadLength = 10;
    public int fightSceneAttackNum = 2;
    public float fightSceneAttackDelay = 1;

    public int creatureId = 1;
    public int creatureModelId = 0;

    public int attackModeAttackTestId = 0;
    public int attackModeDefenseTestId = 0;

    public string buffSelfAttackTestId = "";
    public string buffSelfDefenseTestId = "";
    public string buffTestId = "";
    public string abyssalBlessingIds = "";
    public List<long> enemyIds = new List<long>() { 1010010001 };
    public long doomCouncilBillId = 1000000001;

    // 奖励选择测试参数
    public RarityEnum rewardSelectRarity = RarityEnum.N;
    public int rewardSelectAddAttribute = 5;
    public int rewardSelectCrystalNum = 100;
    public int rewardSelectCreateEquipNum = 1;
    public int rewardSelectCreateItemNum = 3;
    public int rewardSelectNumMax = 1;
    public float rewardSelectEquipDemonLordRate = 0.1f;

    // 折叠状态
    private bool showBaseTest = true;
    private bool showFightSceneTest = true;
    private bool showCardTest = true;
    private bool showBaseSceneTest = true;
    private bool showRewardSelectTest = true;
    private bool showDoomCouncilTest = true;
    private bool showNpcCreateTest = true;
    private bool showResearchTest = true;

    // 战斗场景测试折叠
    private bool showFightBasicSettings = true;
    private bool showFightEnemySettings = true;
    private bool showFightBuffSettings = true;

    private const string PREFS_KEY_PREFIX = "GameTestEditor_";
    private const string ENEMY_IDS_KEY = PREFS_KEY_PREFIX + "enemyIds";
    private const string ENEMY_IDS_COUNT_KEY = PREFS_KEY_PREFIX + "enemyIdsCount";

    private void OnEnable()
    {
        LoadAllPreferences();
    }

    private void OnDisable()
    {
        SaveAllPreferences();
    }

    private void LoadAllPreferences()
    {
        // 基础测试
        testDataCardNum = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "testDataCardNum", 20);
        fightSceneId = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "fightSceneId", 1);
        fightCardId = EditorPrefs.GetString(PREFS_KEY_PREFIX + "fightCardId", "900002");
        fightSceneRoadNum = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "fightSceneRoadNum", 1);
        fightSceneRoadLength = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "fightSceneRoadLength", 10);
        fightSceneAttackNum = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "fightSceneAttackNum", 2);
        fightSceneAttackDelay = EditorPrefs.GetFloat(PREFS_KEY_PREFIX + "fightSceneAttackDelay", 1);

        // 生物相关
        creatureId = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "creatureId", 1);
        creatureModelId = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "creatureModelId", 0);

        // 攻击模式
        attackModeAttackTestId = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "attackModeAttackTestId", 0);
        attackModeDefenseTestId = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "attackModeDefenseTestId", 0);

        // BUFF 相关
        buffSelfAttackTestId = EditorPrefs.GetString(PREFS_KEY_PREFIX + "buffSelfAttackTestId", "");
        buffSelfDefenseTestId = EditorPrefs.GetString(PREFS_KEY_PREFIX + "buffSelfDefenseTestId", "");
        buffTestId = EditorPrefs.GetString(PREFS_KEY_PREFIX + "buffTestId", "");

        // 深渊馈赠
        abyssalBlessingIds = EditorPrefs.GetString(PREFS_KEY_PREFIX + "abyssalBlessingIds", "");

        // 终焉议会
        doomCouncilBillId = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "doomCouncilBillId", 1000000001);

        // 奖励选择测试参数
        rewardSelectRarity = (RarityEnum)EditorPrefs.GetInt(PREFS_KEY_PREFIX + "rewardSelectRarity", 1);
        rewardSelectAddAttribute = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "rewardSelectAddAttribute", 5);
        rewardSelectCrystalNum = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "rewardSelectCrystalNum", 100);
        rewardSelectCreateEquipNum = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "rewardSelectCreateEquipNum", 1);
        rewardSelectCreateItemNum = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "rewardSelectCreateItemNum", 3);
        rewardSelectNumMax = EditorPrefs.GetInt(PREFS_KEY_PREFIX + "rewardSelectNumMax", 1);
        rewardSelectEquipDemonLordRate = EditorPrefs.GetFloat(PREFS_KEY_PREFIX + "rewardSelectEquipDemonLordRate", 0.1f);

        // 敌人 IDs
        LoadEnemyIds();
    }

    private void SaveAllPreferences()
    {
        // 基础测试
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "testDataCardNum", testDataCardNum);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "fightSceneId", fightSceneId);
        EditorPrefs.SetString(PREFS_KEY_PREFIX + "fightCardId", fightCardId);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "fightSceneRoadNum", fightSceneRoadNum);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "fightSceneRoadLength", fightSceneRoadLength);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "fightSceneAttackNum", fightSceneAttackNum);
        EditorPrefs.SetFloat(PREFS_KEY_PREFIX + "fightSceneAttackDelay", fightSceneAttackDelay);

        // 生物相关
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "creatureId", creatureId);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "creatureModelId", creatureModelId);

        // 攻击模式
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "attackModeAttackTestId", attackModeAttackTestId);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "attackModeDefenseTestId", attackModeDefenseTestId);

        // BUFF 相关
        EditorPrefs.SetString(PREFS_KEY_PREFIX + "buffSelfAttackTestId", buffSelfAttackTestId);
        EditorPrefs.SetString(PREFS_KEY_PREFIX + "buffSelfDefenseTestId", buffSelfDefenseTestId);
        EditorPrefs.SetString(PREFS_KEY_PREFIX + "buffTestId", buffTestId);

        // 深渊馈赠
        EditorPrefs.SetString(PREFS_KEY_PREFIX + "abyssalBlessingIds", abyssalBlessingIds);

        // 终焉议会
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "doomCouncilBillId", (int)doomCouncilBillId);

        // 奖励选择测试参数
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "rewardSelectRarity", (int)rewardSelectRarity);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "rewardSelectAddAttribute", rewardSelectAddAttribute);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "rewardSelectCrystalNum", rewardSelectCrystalNum);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "rewardSelectCreateEquipNum", rewardSelectCreateEquipNum);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "rewardSelectCreateItemNum", rewardSelectCreateItemNum);
        EditorPrefs.SetInt(PREFS_KEY_PREFIX + "rewardSelectNumMax", rewardSelectNumMax);
        EditorPrefs.SetFloat(PREFS_KEY_PREFIX + "rewardSelectEquipDemonLordRate", rewardSelectEquipDemonLordRate);

        // 敌人 IDs
        SaveEnemyIds();
    }

    private void SaveEnemyIds()
    {
        EditorPrefs.SetInt(ENEMY_IDS_COUNT_KEY, enemyIds.Count);
        for (int i = 0; i < enemyIds.Count; i++)
        {
            EditorPrefs.SetInt(ENEMY_IDS_KEY + "_" + i, (int)enemyIds[i]);
        }
    }

    private void LoadEnemyIds()
    {
        int count = EditorPrefs.GetInt(ENEMY_IDS_COUNT_KEY, 1);
        enemyIds.Clear();
        for (int i = 0; i < count; i++)
        {
            long id = EditorPrefs.GetInt(ENEMY_IDS_KEY + "_" + i, 1010010001);
            enemyIds.Add(id);
        }
    }
}
