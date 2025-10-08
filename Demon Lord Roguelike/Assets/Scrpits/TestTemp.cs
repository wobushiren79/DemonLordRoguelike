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
            var itemData = BuffInfoCfg.GetItemData(1000100001);
        }
    }
}
