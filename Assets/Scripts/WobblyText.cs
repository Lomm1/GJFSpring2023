using UnityEngine;
using TMPro;

public class WobblyText : MonoBehaviour
{
    public TMP_Text text;
    public float wobbleAmount;
    public float timeScaler;
    public float sinScaler;

    private void Update()
    {
        text.ForceMeshUpdate();
        var textInfo = text.textInfo;

        for (var i = 0; i < textInfo.characterCount; ++i)
        {
            var charInfo = textInfo.characterInfo[i];

            if (charInfo.isVisible == false)
                continue;

            var vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

            for (var k = 0; k < 4; ++k)
            {
                var orig = vertices[charInfo.vertexIndex + k];
                vertices[charInfo.vertexIndex + k] = orig + new Vector3(0, Mathf.Sin(Time.time * timeScaler + orig.x * wobbleAmount) * sinScaler, 0);
            }
        }

        for (var i = 0; i < textInfo.meshInfo.Length; ++i)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            text.UpdateGeometry(meshInfo.mesh, i);
        }
    }
}
