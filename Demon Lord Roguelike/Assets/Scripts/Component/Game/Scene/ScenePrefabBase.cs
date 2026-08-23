using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
public class ScenePrefabBase : BaseMonoBehaviour
{
    /// <summary>
    ///  初始化场景数据
    /// </summary>
    public virtual Task InitSceneData()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 刷新场景
    /// </summary>
    public virtual Task RefreshScene()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除场景
    /// </summary>
    /// <returns></returns>
    public virtual Task DestoryScene()
    {
        if (gameObject != null)
        {
           DestroyImmediate(gameObject);
        }
        return Task.CompletedTask;
    }
}