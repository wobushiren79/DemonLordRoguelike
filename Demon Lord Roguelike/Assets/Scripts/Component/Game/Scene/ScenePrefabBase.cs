using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
public class ScenePrefabBase : BaseMonoBehaviour
{
    /// <summary>
    ///  初始化场景数据
    /// </summary>
    public virtual async Task InitSceneData()
    {
        
    }

    /// <summary>
    /// 刷新场景
    /// </summary>
    public virtual async Task RefreshScene()
    {

    }
    
    /// <summary>
    /// 删除场景
    /// </summary>
    /// <returns></returns>
    public virtual async Task DestoryScene()
    {
        if (gameObject != null)
        {
           DestroyImmediate(gameObject); 
        }
    }
}