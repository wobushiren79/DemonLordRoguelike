using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class TestTemp : MonoBehaviour
{
    public VisualEffect visualEffect;
    public async Task OnGUI()
    {
        if (GUILayout.Button("Test"))
        {
            for (int i = 0; i < 5; i++)
            {
                await new WaitForSeconds(i*0.01f);
                visualEffect.SetVector3("StartPosition", new Vector3(i * 1 + 1, 0, 0));
                visualEffect.SetVector3("EndPosition", new Vector3(0, 0, 0));
                visualEffect.Play();
            }

        }
    }
}
