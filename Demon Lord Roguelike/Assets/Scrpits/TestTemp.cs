using System.Threading.Tasks;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class TestTemp : MonoBehaviour
{
    public ParticleSystem visualEffect;
    public async Task OnGUI()
    {
        if (GUILayout.Button("Test"))
        {
            for (int i = 0; i < 5; i++)
            {
                await new WaitForSeconds(i*0.01f);
                var ps = visualEffect.GetComponentsInChildren<ParticleSystem>() ;
                Vector3 randomPos=new Vector3(Random.Range(-1f,1f),Random.Range(-1f,1f),Random.Range(-1f,1f));
                foreach(var item in ps)
                {
                    var shopMo = item.shape;
                    shopMo.position = randomPos;
                }
                 var shopMo1 = visualEffect.shape;
                    shopMo1.position = randomPos;
                visualEffect.Play();
            }

        }
    }
}
