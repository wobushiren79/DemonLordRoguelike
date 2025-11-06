using System.Threading.Tasks;
using UnityEngine;

public class ScenePrefabForDoomCouncil : ScenePrefabBase
{
    public override async Task InitSceneData()
    {
        await base.InitSceneData();
        await InitCouncilor();
    }

    /// <summary>
    /// 初始化所有议员
    /// </summary>
    public async Task InitCouncilor()
    {
        //生成议会议员
        UserDataBean userData = GameDataHandler.Instance.manager.GetUserData();
        await CreatureHandler.Instance.CreateDoomCouncilCreature(userData.selfCreature, new Vector3(0, 0, 3));
    }
}