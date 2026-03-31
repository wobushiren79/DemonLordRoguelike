using System.Collections.Generic;
using System.Threading.Tasks;
using Spine.Unity;
using UnityEngine;
public class TestTemp : MonoBehaviour
{
    public SkeletonAnimation skeletonAnimation;
    public async Task OnGUI()
    {
        if (GUILayout.Button("Test"))
        {
            var targetData = ModHandler.Instance.LoadAssetSync<SkeletonDataAsset>("Spine", "Assets/Spine/Common/Human/Human_SkeletonData.asset");
            skeletonAnimation.skeletonDataAsset = targetData;
            skeletonAnimation.Initialize(true);
            var skinData = new Dictionary<string, SpineSkinBean>();
            skinData.Add("Base/Base_1", new SpineSkinBean());
            SpineHandler.Instance.ChangeSkeletonSkin(skeletonAnimation.skeleton, skinData);
        }
    }
}
