using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureManager : BaseManager
{
    //���е�ģ��
    public Dictionary<string, GameObject> dicCreatureModel = new Dictionary<string, GameObject>();
    //��������Ļ����
    public Dictionary<CreatureTypeEnum, Queue<GameObject>> dicPoolForCreature = new Dictionary<CreatureTypeEnum, Queue<GameObject>>();

    //����Ԥ��
    public GameObject objCreatureSelectPreview;
    public SkeletonAnimation skeletonAnimationSelectPreview;
    public CreatureBean creatureDataSelectPreview;

    /// <summary>
    /// ��������
    /// </summary>
    public void Clear()
    {
        if (objCreatureSelectPreview != null)
            DestroyImmediate(objCreatureSelectPreview);
        skeletonAnimationSelectPreview = null;
        creatureDataSelectPreview = null;

        foreach (var itemPool in dicPoolForCreature)
        {
            Queue<GameObject> pool = itemPool.Value;
            while (pool.Count > 0)
            {
                var itemObj = pool.Dequeue();
                DestroyImmediate(itemObj);
            }
        }
        dicPoolForCreature.Clear();
    }

    /// <summary>
    /// ��ȡ����Ԥ��
    /// </summary>
    /// <returns></returns>
    public GameObject GetCreaureSelectPreview(CreatureBean creatureData = null)
    {
        if (objCreatureSelectPreview == null)
        {
            string resPath = $"{PathInfo.CreaturesPrefabPath}/FightCreature_SelectPreview.prefab";
            var targetModel = GetModelForAddressablesSync(dicCreatureModel, resPath);
            objCreatureSelectPreview = Instantiate(gameObject, targetModel);

            Transform spineTF = objCreatureSelectPreview.transform.Find("Spine");
            skeletonAnimationSelectPreview = spineTF.GetComponent<SkeletonAnimation>();
        }
        var mainCamera = CameraHandler.Instance.manager.mainCamera;
        objCreatureSelectPreview.transform.eulerAngles = mainCamera.transform.eulerAngles;

        if (creatureData != null)
        {
            if (creatureDataSelectPreview == null || creatureData != creatureDataSelectPreview)
            {
                //���ù�������
                SpineHandler.Instance.SetSkeletonDataAsset(skeletonAnimationSelectPreview, creatureData.creatureModel.res_name);
                string[] skinArray = creatureData.GetSkinArray();
                //�޸�Ƥ��
                SpineHandler.Instance.ChangeSkeletonSkin(skeletonAnimationSelectPreview.skeleton, skinArray);
                creatureDataSelectPreview = creatureData;

                //�޸Ĳ�������ɫ
                skeletonAnimationSelectPreview.skeleton.A = 0.65f;

                Transform spineTF = objCreatureSelectPreview.transform.Find("Spine");
                spineTF.transform.localScale = Vector3.one * creatureData.creatureModel.size_spine;
            }
        }
        return objCreatureSelectPreview;
    }

    /// <summary>
    /// ����һ������obj
    /// </summary>
    /// <param name="creatureId"></param>
    /// <param name="actionForComplete"></param>
    public void LoadCreatureObj(int creatureId, Action<GameObject> actionForComplete)
    {
        var itemCreatureInfo = CreatureInfoCfg.GetItemData(creatureId);
        if (itemCreatureInfo == null)
        {
            LogUtil.LogError($"��������ʧ�ܣ�û���ҵ�IDΪ{creatureId}������");
            return;
        }
  
        CreatureTypeEnum creatureType = itemCreatureInfo.GetCreatureType();
        //���Ȼ�ȡ������������
        GameObject objItem = null;
        if (dicPoolForCreature.TryGetValue(creatureType, out Queue<GameObject> poolForCreature))
        {
            objItem = GetCreaureFromPool(poolForCreature);
        }

        //���û�� ����ش����µ�Ԥ��
        if (objItem == null)
        {
            string creatureModelName;
            switch (creatureType)
            {
                case CreatureTypeEnum.FightDef:
                    creatureModelName = "FightCreature_Def_1.prefab";
                    break;
                case CreatureTypeEnum.FightAtt:
                    creatureModelName = "FightCreature_Att_1.prefab";
                    break;
                case CreatureTypeEnum.FightDefCore:
                    creatureModelName = "FightCreature_DefCore_1.prefab";
                    break;
                default:
                    LogUtil.LogError($"��������ʧ�ܣ�û���ҵ�creature_typeΪ{itemCreatureInfo.creature_type}������");
                    return;
            }

            string resPath = $"{PathInfo.CreaturesPrefabPath}/{creatureModelName}";
            var targetModel = GetModelForAddressablesSync(dicCreatureModel, resPath);
            if (targetModel == null)
            {
                LogUtil.LogError($"��������ʧ�ܣ�û���ҵ���Դ·��Ϊ{resPath}������");
                return;
            }
            objItem = Instantiate(gameObject, targetModel);
        }

        objItem.gameObject.SetActive(true);
        actionForComplete?.Invoke(objItem);
    }

    /// <summary>
    /// �ӻ�����л�ȡ����
    /// </summary>
    /// <param name="pool"></param>
    public GameObject GetCreaureFromPool(Queue<GameObject> pool)
    {
        if (pool.Count <= 0)
        {
            return null;
        }
        return pool.Dequeue();
    }

    /// <summary>
    /// ���ն���
    /// </summary>
    public async void DestoryCreature(Queue<GameObject> pool, GameObject targetObj)
    {
        targetObj.transform.position = new Vector3(0,-100,0);
        //�ȴ�1֡��ֹ ��ǰ������������
        await new WaitNextFrame();
        targetObj.SetActive(false);
        pool.Enqueue(targetObj);
    }

}
