using Spine;
using Spine.Unity;

public partial class SpineHandler
{
    public TrackEntry PlayAnim(
        SkeletonAnimation skeletonAnimation, SpineAnimationStateEnum animationCreatureState, CreatureBean creatureData, bool isLoop,
        float mixDuration = -1, float animStartTime = 0)
    {
        if (skeletonAnimation == null)
        {
            LogUtil.LogError("播放动画失败 缺少skeletonAnimation资源");
            return null;
        }
        if (creatureData == null)
        {
            LogUtil.LogError("播放动画失败 缺少creatureData资源");
            return null;
        }
        var animNameAppoint = GetAnimNameAppoint(animationCreatureState, creatureData);
        var animData = PlayAnim(skeletonAnimation, animationCreatureState, isLoop, animNameAppoint: animNameAppoint, animStartTime: animStartTime);
        if (animData != null && mixDuration != -1)
        {
            animData.MixDuration = mixDuration;
        }
        return animData;
    }

    public TrackEntry PlayAnim(
        SkeletonGraphic skeletonGraphic, SpineAnimationStateEnum animationCreatureState, CreatureBean creatureData, bool isLoop,
        float mixDuration = -1, float animStartTime = 0)
    {
        if (skeletonGraphic == null)
        {
            LogUtil.LogError("播放动画失败 缺少skeletonAnimation资源");
            return null;
        }
        if (creatureData == null)
        {
            LogUtil.LogError("播放动画失败 缺少creatureData资源");
            return null;
        }
        var animNameAppoint = GetAnimNameAppoint(animationCreatureState, creatureData);
        var animData = PlayAnim(skeletonGraphic, animationCreatureState, isLoop, animNameAppoint: animNameAppoint, animStartTime: animStartTime);
        if (animData != null && mixDuration != -1)
        {
            animData.MixDuration = mixDuration;
        }
        return animData;
    }

    protected string GetAnimNameAppoint(SpineAnimationStateEnum spineAnimationState, CreatureBean creatureData)
    {
        string animNameAppoint = null;
        switch (spineAnimationState)
        {
            case SpineAnimationStateEnum.Idle:
                if (!creatureData.creatureInfo.anim_idle.IsNull())
                    animNameAppoint = creatureData.creatureInfo.anim_idle;
                break;
            case SpineAnimationStateEnum.Attack:
                if (!creatureData.creatureInfo.anim_attack.IsNull())
                    animNameAppoint = creatureData.creatureInfo.anim_attack;
                break;
            case SpineAnimationStateEnum.Walk:
                if (!creatureData.creatureInfo.anim_walk.IsNull())
                    animNameAppoint = creatureData.creatureInfo.anim_walk;
                break;
            case SpineAnimationStateEnum.Dead:
                if (!creatureData.creatureInfo.anim_dead.IsNull())
                    animNameAppoint = creatureData.creatureInfo.anim_dead;
                break;
        }
        return animNameAppoint;
    }
}