using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFightPrefabEntity 
{
    public string id;
    public string pathAsstes;

    public int valueInt;//��ֵ
    public GameObject gameObject;//Ŀ��Ԥ��
    public SpriteRenderer spriteRenderer;//Ŀ����Ⱦ
    public Collider collider;//Ŀ����ײ

    public GameFightPrefabStateEnum state =  GameFightPrefabStateEnum.None;//״̬
    public float lifeTime = -1;//��������

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
    /// �����Լ�
    /// </summary>
    public virtual void Destroy()
    {
        SetState(GameFightPrefabStateEnum.None);
        FightHandler.Instance.RemoveFightPrefab(this);
    }

    /// <summary>
    /// ����״̬
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
