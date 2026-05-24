using System;
using UnityEditor;
using UnityEngine;

namespace CoInspector
{
    [Serializable]
    public class MissingScriptManager : ScriptableObject
    {
        private static MissingScriptManager instance;
        private static string STORAGE_PATH
        {
            get
            {
                string path = CoInspectorWindow._GetRootPath() + "/Core/Helpers/MissingScriptManager.asset";
                return path;
            }
        }

        [HideInInspector] public int instanceID = -1;
        [HideInInspector] public int localIdentifierInFile = -1;
        [HideInInspector] public int count = 0;
        [HideInInspector] public string path = "";
        [HideInInspector] public string assetName = "";
        [HideInInspector] public MonoScript script = null;

        private void CleanExtension()
        {
            if (assetName.Contains("."))
            {
                assetName = assetName.Substring(0, assetName.LastIndexOf('.'));
            }
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;

            instance = AssetDatabase.LoadAssetAtPath<MissingScriptManager>(STORAGE_PATH);

            if (instance == null)
            {
                instance = CreateInstance<MissingScriptManager>();
                AssetDatabase.CreateAsset(instance, STORAGE_PATH);
            }
        }

        public static MissingScriptManager WriteData(int instanceID = -1)
        {
            EnsureInstance();
            if (instanceID != -1)
            {
                instance.instanceID = instanceID;
            }
#if UNITY_6000_3_OR_NEWER
            instance.path = AssetDatabase.GetAssetPath(instance.GetEntityId());
#else
            instance.path = AssetDatabase.GetAssetPath(instance.instanceID);
#endif
            string guid;
            long _localIdentifierInFile;
#if UNITY_6000_3_OR_NEWER
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instance.GetEntityId(), out guid, out _localIdentifierInFile);
#else
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instance.instanceID, out guid, out _localIdentifierInFile);
#endif
            instance.localIdentifierInFile = (int)_localIdentifierInFile;
            instance.assetName = instance.path[(instance.path.LastIndexOf('/') + 1)..];
            instance.count = 1;
            instance.CleanExtension();
            //Debug.Log("Writing Missing Script Data: " + instance.assetName);
            return instance;
        }

        public static MissingScriptManager WriteData(string _path)
        {
            EnsureInstance();
            instance.instanceID = -1;
            instance.path = _path;
            instance.assetName = instance.path[(instance.path.LastIndexOf('/') + 1)..];
            instance.count = 1;
            instance.CleanExtension();
            return instance;
        }

        public static int CountMissingScripts()
        {
            int count = 0;
#if UNITY_6000_3_OR_NEWER
            foreach (var instanceID in Selection.entityIds)
#else
            foreach (var instanceID in Selection.instanceIDs)

#endif
            {
#if UNITY_6000_3_OR_NEWER
                var asset = EditorUtility.EntityIdToObject(instanceID);
#else
                var asset = EditorUtility.InstanceIDToObject(instanceID);

#endif
                if (!asset)
                {
                    if (count == 0)
                    {
                        EnsureInstance();
                        instance.instanceID = instanceID;
                    }
                    count++;
                }
            }
            return count;
        }

        public static MissingScriptManager WriteMultiData(int count)
        {
            EnsureInstance();
#if UNITY_6000_3_OR_NEWER
            instance.path = AssetDatabase.GetAssetPath(instance.GetEntityId());

#else
            instance.path = AssetDatabase.GetAssetPath(instance.instanceID);

#endif
            instance.assetName = count + " Missing Scripts";
            instance.count = count;
            return instance;
        }
        public static void SetInactive()
        {
            EnsureInstance();
            instance.count = 0;
        }
        public static int Count()
        {
            EnsureInstance();
            return instance.count;
        }
        public static void ClearData()
        {
            EnsureInstance();
            instance.instanceID = -1;
            instance.localIdentifierInFile = -1;
            instance.count = 0;
            instance.path = "";
            instance.assetName = "";
            instance.script = null;
        }
        public static bool IsActive()
        {
            EnsureInstance();
            return instance.count > 0;
        }

        public static bool IsMulti => instance != null && instance.count > 1;
    }
    [Serializable]
    internal class MissingComponent : MonoBehaviour
    {
        public int instanceID = -1;
        public int index = -1;
        public GameObject owner = null;

        internal MissingComponent(int instanceID, int index, GameObject owner)
        {
            this.instanceID = instanceID;
            this.index = index;
            this.owner = owner;
        }
    }
}
