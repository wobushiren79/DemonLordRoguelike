using UnityEngine;
using UnityEngine.UI;

public class TestTemp : MonoBehaviour
{
    public int size=32;
    public float scale1 =0.15f;
    public float scale2 = 0.05f;
    public RawImage rawImage;
    public void OnGUI()
    {
        if(GUILayout.Button("Test"))
        {
            CreateToolsForPlanetTextureBean createData=new CreateToolsForPlanetTextureBean(Random.Range(0,int.MaxValue));
            createData.size = size;
            createData.scale1_HeightMapScale = scale1;
            createData.scale2_HeightMapScale=scale2;
            Texture2D target = CreateTools.CreatePlanetTexture(createData);
            rawImage.texture = target;
        }

    }
}
