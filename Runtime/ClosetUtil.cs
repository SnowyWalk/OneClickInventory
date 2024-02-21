using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using VRC;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor;

namespace dog.miruku.ndcloset.runtime
{
    [AddComponentMenu("Non-Destructive Closet/Closet Util")]
    public class ClosetUtil
    {
        private static readonly string _generatedPathGuid = "6385f8da0e893d142aaaef7ed709f4bd";
        private static readonly string _generatedPathRoot = AssetDatabase.GUIDToAssetPath(_generatedPathGuid);

        private static string GetRelativePath(Transform o, Transform ancestor, String path)
        {
            if (o == null) throw new Exception("Invalid ancestor");
            if (o == ancestor) return path;

            return GetRelativePath(o.parent, ancestor, $"{o.name}/{path}");
        }

        private static string GetRelativePath(Transform o, Transform ancestor)
        {
            return GetRelativePath(o.parent, ancestor, o.name);
        }

        private static void AcquireDirectory(string path)
        {

            var directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static string GetAssetPath(string key)
        {
            var assetPath = $"{_generatedPathRoot}/{key}";
            AcquireDirectory(assetPath);
            return assetPath;
        }

        public static string GetPersistentAssetPath(string key)
        {
            var assetPath = $"Assets/Closet/{key}";
            AcquireDirectory(assetPath);
            return assetPath;
        }

        public static void ClearGeneratedAssets()
        {
            if (Directory.Exists(_generatedPathRoot))
            {
                Directory.Delete(_generatedPathRoot, true);
            }
            Directory.CreateDirectory(_generatedPathRoot);
        }

        public static AnimationClip GenerateAnimationClip(string key, VRCAvatarDescriptor avatar, IEnumerable<GameObject> enabledObjects, IEnumerable<GameObject> disabledObjects)
        {
            var clip = new AnimationClip();
            var enabledKeys = new Keyframe[1] { new(0.0f, 1f) };
            var disabledKeys = new Keyframe[1] { new(0.0f, 0f) };
            foreach (var o in enabledObjects)
            {
                clip.SetCurve(GetRelativePath(o.transform, avatar.transform), typeof(GameObject), "m_IsActive", new AnimationCurve(enabledKeys));
            }
            foreach (var o in disabledObjects)
            {
                clip.SetCurve(GetRelativePath(o.transform, avatar.transform), typeof(GameObject), "m_IsActive", new AnimationCurve(disabledKeys));
            }

            var path = GetAssetPath($"Animations/{key}.anim");
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
            Color[] rpixels = result.GetPixels(0);
            float incX = 1.0f / targetWidth;
            float incY = 1.0f / targetHeight;
            for (int px = 0; px < rpixels.Length; px++)
            {
                rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth), incY * ((float)Mathf.Floor(px / targetWidth)));
            }
            result.SetPixels(rpixels, 0);
            result.Apply();
            return result;
        }

        public static Texture2D GenerateIcon(ClosetItem item)
        {
            var cloned = new GameObject();
            foreach (var o in item.GameObjects)
            {
                var clone = GameObject.Instantiate(o);
                clone.transform.SetParent(cloned.transform);
                clone.SetActive(true);
            }

            // Setup camera
            cloned.transform.position = Vector3.zero;
            var cameraObject = new GameObject();
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Nothing;
            camera.nearClipPlane = 0.00001f;

            // Calculate bound
            var boundList = cloned.GetComponentsInChildren<Renderer>().Select<Renderer, Bounds?>(e =>
            {
                if (e.TryGetComponent(out SkinnedMeshRenderer renderer))
                {
                    if (renderer.sharedMesh == null) return null;
                    return new Bounds(renderer.bounds.center, renderer.sharedMesh.bounds.size);
                }
                return e.bounds;
            }).OfType<Bounds>().ToArray();

            var bounds = boundList.Length > 0 ? boundList[0] : new Bounds();
            foreach (var b in boundList.Skip(1))
            {
                bounds.Encapsulate(b);
            }

            // Calculate positions
            cameraObject.transform.eulerAngles = new Vector3(0, -180, 0);
            var maxExtent = bounds.extents.magnitude;
            var minDistance = (maxExtent) / Mathf.Sin(Mathf.Deg2Rad * camera.fieldOfView / 2.0f);
            var center = bounds.center;

            cloned.transform.position = new Vector3(5000, 5000, 5000);
            camera.transform.position = center + new Vector3(5000, 5000, 5000) + Vector3.forward * minDistance;

            var captureWidth = 2048;
            var captureHeight = 2048;

            // Capture
            var rt = new RenderTexture(captureWidth, captureHeight, 0);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = camera.targetTexture;
            var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.ARGB32, false);
            image.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            image.alphaIsTransparency = true;
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            GameObject.DestroyImmediate(rt);
            GameObject.DestroyImmediate(camera.gameObject);
            GameObject.DestroyImmediate(cloned.gameObject);

            // Clip alpha
            int minX = captureWidth, maxX = 0, minY = captureHeight, maxY = 0;
            for (int x = 0; x < captureWidth; x++)
            {
                for (int y = 0; y < captureHeight; y++)
                {
                    var pixel = image.GetPixel(x, y);
                    if (pixel.a != 0)
                    {
                        if (minX > x) minX = x;
                        if (maxX < x) maxX = x;
                        if (minY > y) minY = y;
                        if (maxY < y) maxY = y;
                    }
                }
            }

            int centerX = (minX + maxX) / 2, centerY = (minY + maxY) / 2;
            var size = Mathf.Max(maxX - minX, maxY - minY);
            if (size < 0)
            {
                size = 1;
            }
            var pixels = image.GetPixels(centerX - size / 2, centerY - size / 2, size, size);
            var clippedIcon = new Texture2D(size, size, TextureFormat.ARGB32, false);
            clippedIcon.SetPixels(pixels);
            clippedIcon.Apply();


            // Resize and save
            var resizedIcon = ResizeTexture(clippedIcon, 256, 256);
            var bytes = resizedIcon.EncodeToPNG();


            var path = GetPersistentAssetPath("Icons/" + GUID.Generate() + ".png");
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static T GetOrAddComponent<T>(GameObject o) where T : Component
        {
            return o.GetComponent<T>() ?? o.AddComponent<T>();
        }
    }
}