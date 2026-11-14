using UnityEditor;
using UnityEngine;

public sealed class SaloonSurfaceImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Art/Surfaces/"))return;
        var texture=(TextureImporter)assetImporter;
        // The shader reads GL normals directly, so keep RGB rather than Unity's packed normals.
        texture.textureType=TextureImporterType.Default;
        texture.sRGBTexture=assetPath.EndsWith("_Color.jpg");
        texture.maxTextureSize=1024;texture.mipmapEnabled=true;
        texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Trilinear;
        texture.anisoLevel=4;texture.textureCompression=TextureImporterCompression.Compressed;
    }
}
