using Spine;
using Spine.Unity;

public partial class SpineHandler
{
    public TrackEntry PlayAnim(
        SkeletonAnimation skeletonAnimation, SpineAnimationStateEnum animationCreatureState, CreatureBean creatureData, bool isLoop,
        float mixDuration = -1, float animStartTime = 0, float animSpeed = 1)
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
        var animData = PlayAnim(skeletonAnimation, animationCreatureState, isLoop, animNameAppoint: animNameAppoint, animStartTime: animStartTime, animSpeed: animSpeed);
        if (animData != null && mixDuration != -1)
        {
            animData.MixDuration = mixDuration;
        }
        return animData;
    }

    public TrackEntry PlayAnim(
        SkeletonGraphic skeletonGraphic, SpineAnimationStateEnum animationCreatureState, CreatureBean creatureData, bool isLoop,
        float mixDuration = -1, float animStartTime = 0, float animSpeed = 1)
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
        var animData = PlayAnim(skeletonGraphic, animationCreatureState, isLoop, animNameAppoint: animNameAppoint, animStartTime: animStartTime, animSpeed: animSpeed);
        if (animData != null && mixDuration != -1)
        {
            animData.MixDuration = mixDuration;
        }
        return animData;
    }

    protected string GetAnimNameAppoint(SpineAnimationStateEnum spineAnimationState, CreatureBean creatureData)
    {
        //幻化整骨替换后原生物 anim_* 配置名不适用于新骨架:不指定动画名,交框架按目标骨架实际动画列表解析(缺失仅日志不播,避免 SetAnimation 抛异常)
        if (creatureData.GetTransformSpineRes() != null)
        {
            //映射动画例外:幻化药配置了 show 骨架替代动画(other_data 的 idle_anim/walk_anim/attack_anim/dead_anim 键,生成期检测骨架无标准候选时写入)时优先按名直播
            switch (spineAnimationState)
            {
                case SpineAnimationStateEnum.Idle:
                    string transformIdleAnim = creatureData.GetTransformIdleAnim();
                    if (!transformIdleAnim.IsNull())
                        return transformIdleAnim;
                    break;
                case SpineAnimationStateEnum.Walk:
                    string transformWalkAnim = creatureData.GetTransformWalkAnim();
                    if (!transformWalkAnim.IsNull())
                        return transformWalkAnim;
                    break;
                case SpineAnimationStateEnum.Attack:
                    string transformAttackAnim = creatureData.GetTransformAttackAnim();
                    if (!transformAttackAnim.IsNull())
                        return transformAttackAnim;
                    break;
                case SpineAnimationStateEnum.Dead:
                    string transformDeadAnim = creatureData.GetTransformDeadAnim();
                    if (!transformDeadAnim.IsNull())
                        return transformDeadAnim;
                    break;
            }
            return null;
        }
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

    /// <summary>
    /// 设置动画到第一帧（静态姿势，不播放）
    /// </summary>
    public void SetAnimFirstFrame(SkeletonAnimation skeletonAnimation, SpineAnimationStateEnum spineAnimationState, CreatureBean creatureData)
    {
        if (skeletonAnimation == null || creatureData == null)
            return;
        var trackEntry = PlayAnim(skeletonAnimation, spineAnimationState, creatureData, false, animSpeed: 0);
        if (trackEntry != null)
        {
            trackEntry.TrackTime = 0;
            skeletonAnimation.Update(0);
        }
    }

    /// <summary>
    /// 设置动画到第一帧（静态姿势，不播放，SkeletonGraphic 版）
    /// </summary>
    public void SetAnimFirstFrame(SkeletonGraphic skeletonGraphic, SpineAnimationStateEnum spineAnimationState, CreatureBean creatureData)
    {
        //内部动画组件为空=骨架数据未设置成功(资源缺失等),保持原姿势不报错
        if (skeletonGraphic == null || creatureData == null || skeletonGraphic.Animation == null)
            return;
        var trackEntry = PlayAnim(skeletonGraphic, spineAnimationState, creatureData, false, animSpeed: 0);
        if (trackEntry != null)
        {
            trackEntry.TrackTime = 0;
            ((SkeletonAnimation)skeletonGraphic.Animation).Update(0);
        }
    }
}