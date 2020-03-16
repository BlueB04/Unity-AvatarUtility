using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace ysk.Utility
{
    public sealed class AvatarUtility
    {
        public delegate bool GetterIsActivatableTransform(string transformPath);
        public static void CopyAvatarSkeletonAndBone(string from, string to)
        {
            CopyAvatarSkeletonAndBone(AssetImporter.GetAtPath(from) as ModelImporter, AssetImporter.GetAtPath(to) as ModelImporter);
        }

        public static void CopyAvatarSkeletonAndBone(ModelImporter from, ModelImporter to)
        {
            if (from == null) throw new ArgumentNullException("from");
            if (to == null) throw new ArgumentNullException("to");

            SerializedObject f = new SerializedObject(from), t = new SerializedObject(to);
            CopyAvatarSkeletonAndBone(f, t);
            t.ApplyModifiedProperties();
            t.Dispose();
            f.Dispose();
        }

        public static void CopyAvatarSkeletonAndBone(SerializedObject from, SerializedObject to)
        {
            if (from == null) throw new ArgumentNullException("from");
            if (to == null) throw new ArgumentNullException("to");

            ClearArrayProperty(to, "m_HumanDescription.m_Human");
            ClearArrayProperty(to, "m_HumanDescription.m_Skeleton");
            to.CopyFromSerializedProperty(from.FindProperty("m_HumanDescription"));
        }

        public static void SetAllClipAnimationAvatarMask(string target, GetterIsActivatableTransform logic, int at = -1)
        {
            SetAllClipAnimationAvatarMask(AssetImporter.GetAtPath(target) as ModelImporter, logic, at);
        }

        public static void SetAllClipAnimationAvatarMask(ModelImporter importer, GetterIsActivatableTransform logic, int at = -1)
        {
            if (importer == null) throw new ArgumentNullException("importer");
            var mask = new AvatarMask();
            mask.transformCount = importer.transformPaths.Length;
            for (int i = 0; i < mask.transformCount; i++)
            {
                var path = importer.transformPaths[i];
                mask.SetTransformPath(i, path);
                mask.SetTransformActive(i, logic(path));
                //mask.SetTransformActive(i, (!System.IO.Path.GetFileName(path).StartsWith("AttachBone_") || Array.IndexOf(importer.humanDescription.skeleton, path) >= 0));
            }

            var so = new SerializedObject(importer);
            var updateMethod = importer.GetType().GetMethod("UpdateTransformMask", BindingFlags.NonPublic | BindingFlags.Static);
            SetAllClipAnimationAvatarMask(so, mask, updateMethod, at);
            so.ApplyModifiedProperties();
            so.Dispose();
        }

        public static void SetAllClipAnimationAvatarMask(SerializedObject so, AvatarMask mask, MethodInfo updateMethod, int at = -1)
        {
            if (so == null) throw new ArgumentNullException("so");
            var clipsProperty = so.FindProperty("m_ClipAnimations");
            if (clipsProperty == null) return;
            if (at <= 0)
            {
                for (int p = 0; p < clipsProperty.arraySize; p++)
                {
                    var clipProp = clipsProperty.GetArrayElementAtIndex(p);
                    SetClipAnimationAvatarTransformMask(clipProp, mask, updateMethod);
                }
            }
            else if (at < clipsProperty.arraySize)
            {
                var clipProp = clipsProperty.GetArrayElementAtIndex(at);
                SetClipAnimationAvatarTransformMask(clipProp, mask, updateMethod);
            }
        }

        private static void SetClipAnimationAvatarTransformMask(SerializedProperty prop, AvatarMask mask, MethodInfo updateMethod)
        {
            var maskProp = prop.FindPropertyRelative("transformMask");
            updateMethod.Invoke(null, new object[] { mask, maskProp });
        }

        private static void ClearArrayProperty(SerializedObject target, string name)
        {
            var property = target.FindProperty(name);
            if (property != null && property.isArray)
            {
                property.ClearArray();
            }
        }
    }
}
