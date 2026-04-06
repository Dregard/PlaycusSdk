using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;


namespace Playcus.Utils
{
    public static class ImageUtils
    {
        public static void SaveTextureToJPG(Texture2D texture, string filePath, int quality = 100)
        {
            Debug.Log($"ImageUtils.SaveTextureToJPG {filePath}");
            var bytes = texture.EncodeToJPG(quality);
            SaveFile(bytes, filePath);
        }

        public static void SaveTextureToPNG(Texture2D texture, string filePath)
        {
            Debug.Log($"ImageUtils.SaveTextureToPNG {filePath}");
            var bytes = texture.EncodeToPNG();
            SaveFile(bytes, filePath);
        }

        private static void SaveFile(byte[] bytes, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        }



        public static Color32[] Rescale(Texture2D tex, int newWidth, int newHeight)
        {
            var useBilinear = newWidth < .5f * tex.width;
            var texColors = tex.GetPixels();
            var newColors = new Color32[newWidth * newHeight];
            float ratioX;
            float ratioY;
            if (useBilinear)
            {
                ratioX = 1.0f / ((float) newWidth / (tex.width - 1));
                ratioY = 1.0f / ((float) newHeight / (tex.height - 1));
            }
            else
            {
                ratioX = ((float) tex.width) / newWidth;
                ratioY = ((float) tex.height) / newHeight;
            }

            var w = tex.width;
            var w2 = newWidth;

            if (useBilinear)
            {
                for (var y = 0; y < newHeight; y++)
                {
                    int yFloor = (int) Mathf.Floor(y * ratioY);
                    var y1 = yFloor * w;
                    var y2 = (yFloor + 1) * w;
                    var yw = y * w2;

                    for (var x = 0; x < w2; x++)
                    {
                        int xFloor = (int) Mathf.Floor(x * ratioX);
                        var xLerp = x * ratioX - xFloor;
                        newColors[yw + x] = ColorLerpUnclamped(
                            ColorLerpUnclamped(texColors[y1 + xFloor], texColors[y1 + xFloor + 1], xLerp),
                            ColorLerpUnclamped(texColors[y2 + xFloor], texColors[y2 + xFloor + 1], xLerp),
                            y * ratioY - yFloor);
                    }
                }
            }
            else
            {
                for (var y = 0; y < newHeight; y++)
                {
                    var thisY = (int) (ratioY * y) * w;
                    var yw = y * w2;
                    for (var x = 0; x < w2; x++)
                    {
                        newColors[yw + x] = texColors[(int) (thisY + ratioX * x)];
                    }
                }
            }

            return newColors;
        }

        private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
        {
            return new Color(c1.r + (c2.r - c1.r) * value,
                c1.g + (c2.g - c1.g) * value,
                c1.b + (c2.b - c1.b) * value,
                c1.a + (c2.a - c1.a) * value);
        }
    }
}