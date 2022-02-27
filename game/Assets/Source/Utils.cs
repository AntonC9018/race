using System.IO;
using System.Linq;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEngine;
using static Unity.VectorGraphics.SVGParser;
using static Unity.VectorGraphics.VectorUtils;

namespace Race
{
    public static class Utils
    {
        // TODO: move to an editor asmdef.
        #if UNITY_EDITOR
            // TODO: 
            // This function does not work at all right now.
            // I'm getting an (undocumented) internal error in the unity console.
            // I'll need way more time to get down to the root of the problem.
            // For now, just convert that png manually, I guess.
            [MenuItem("Assets/Convert SVG to sprite for normal image (doesn't work)", isValidateFunction: false, priority: 1000)]
            public static void ConvertSVGToSpriteUsableForNormalImage()
            {
                string inputPath = Path.Join(Application.dataPath, "Garage/UI_Elements/arrow.svg");
                using var textReader = new StreamReader(inputPath);
                SceneInfo importedSVG = SVGParser.ImportSVG(textReader);

                var tesselationOptions = new TessellationOptions
                {
                    // In pixels?
                    StepDistance = 10,

                    // Do not generate additional verts.
                    MaxCordDeviation = float.MaxValue,

                    //                        
                    MaxTanAngleDeviation = 0.1f,

                    // In pixels?
                    SamplingStepSize = 20,
                };
                
                var geometry = VectorUtils.TessellateScene(
                    importedSVG.Scene,
                    tesselationOptions,
                    importedSVG.NodeOpacity);

                {
                    // var sprite = VectorUtils.BuildSprite(
                    //     geometry,
                    //     svgPixelsPerUnit: 100.0f, 
                    //     Alignment.SVGOrigin, 
                    //     // Ignored unless the alignment is "Custom"
                    //     Vector2.zero,
                    //     gradientResolution: 128);
                    // sprite.name = "arrow";

                    // var svgImage = Object.FindObjectOfType<SVGImage>();
                    // if (svgImage is null)
                    // {
                    //     Debug.Log("Oops");
                    //     return;
                    // }

                    // svgImage.sprite = sprite;

                    // It doesn't find any textures and so it returns null, which makes sense.
                    // {
                    //     var atlas = VectorUtils.GenerateAtlas(geometry, 128);
                    //     if (atlas is null)
                    //     {
                    //         Debug.Log("Oops");
                    //         return;
                    //     }
                    // }

                    // bool sceneContainsTexturesOrGradients = false;
                    // string materialName = sceneContainsTexturesOrGradients
                    //     ? "Unlit/VectorGradient"
                    //     : "Unlit/Vector";
                    // string materialName = "Unlit/Color";
                    // var material = new Material(Shader.Find(materialName));

                    // var texture2d = VectorUtils.RenderSpriteToTexture2D(
                    //     sprite,
                    //     width: 128, height: 64,
                    //     material);

                    // var pngBytes = texture2d.EncodeToPNG();
                    // string outputPath = Path.Join(Application.dataPath, "Garage/UI_Elements/arrow.png");
                    // File.WriteAllBytes(outputPath, pngBytes);
                }

                {
                    var mesh = new Mesh();
                    VectorUtils.FillMesh(mesh, geometry, 100.0f);

                    var subject = GameObject.Find("test_subject");
                    var meshFilter = subject.GetComponent<MeshFilter>();
                    Debug.Assert(meshFilter is not null);
                    meshFilter.mesh = mesh;
                }
            }
        #endif
    }
}