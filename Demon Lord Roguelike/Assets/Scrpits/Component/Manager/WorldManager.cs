using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : BaseManager
{
    //ս������
    public Dictionary<string, GameObject> dicScene = new Dictionary<string, GameObject>();
    //��ǰ��պ�
    public Material currentSkyBoxMat;

    /// <summary>
    /// ��ȡս������
    /// </summary>
    public void GetFightScene(string dataPath, Action<GameObject> actionForComplete)
    {
        //����ս������
        GetModelForAddressables(dicScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }

    /// <summary>
    /// ��ȡ��ղ�����
    /// </summary>
    /// <param name="skyboxPath"></param>
    /// <param name="actionForComplete"></param>
    public void GetSkybox(string skyboxPath, Action<Material> actionForComplete)
    {
        //������պ���
        LoadAddressablesUtil.LoadAssetAsync<Material>(skyboxPath, data =>
        {
            currentSkyBoxMat = data.Result;
            actionForComplete?.Invoke(data.Result);
        });
    }

    /// <summary>
    /// ��ȡ���س���
    /// </summary>
    public void GetBaseScene(Action<GameObject> actionForComplete)
    {
        string dataPath = $"{PathInfo.CommonPrefabPath}/BaseScene.prefab";
        GetModelForAddressables(dicScene, dataPath, (target) =>
        {
            actionForComplete?.Invoke(target);
        });
    }

    /// <summary>
    /// �Ƴ���պ�
    /// </summary>
    public void RemoveSkybox()
    {
        //������պ���
        if (currentSkyBoxMat != null)
        {
            LoadAddressablesUtil.Release(currentSkyBoxMat);
            RenderSettings.skybox = null;
        }
    }
}
