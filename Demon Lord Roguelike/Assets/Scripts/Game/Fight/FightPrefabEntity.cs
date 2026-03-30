using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightPrefabEntity 
{
    public string id;
    public string pathAsstes;

    public int valueInt;//价值
    public GameObject gameObject;//目标预制
    public SpriteRenderer spriteRenderer;//目标渲染
    public Collider collider;//目标碰撞

    public GameFightPrefabStateEnum state =  GameFightPrefabStateEnum.None;//状态
    public float lifeTime = -1;//生命周期

    public void Update()
    {
        if (state == GameFightPrefabStateEnum.DropCheck && lifeTime > 0)
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0)
            {
                Destroy();
            }
        }
    }

    /// <summary>
    /// 销毁自己
    /// </summary>
    public virtual void Destroy(bool isPermanently = false)
    {
        if (isPermanently)
        {
            if (gameObject != null)
            {
                GameObject.Destroy(gameObject);
            }
        }
        else
        {
            SetState(GameFightPrefabStateEnum.None);
            FightHandler.Instance.RemoveFightPrefab(this); 
        }
    }

    /// <summary>
    /// 设置状态
    /// </summary>
    /// <param name="targetState"></param>
    public virtual void SetState(GameFightPrefabStateEnum targetState)
    {
        switch (targetState)
        {
            case GameFightPrefabStateEnum.None:
                if (collider != null) collider.enabled = false;
                break;
            case GameFightPrefabStateEnum.DropCheck:
                if (collider != null) collider.enabled = true;
                break;
            case GameFightPrefabStateEnum.Droping:
                if (collider != null) collider.enabled = false;
                break;
            default:
                if (collider != null) collider.enabled = false;
                break;
        }
    }
}
