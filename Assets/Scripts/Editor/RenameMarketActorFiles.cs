#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// MarketActor 파일명을 id에 맞게 변경하는 에디터 스크립트
/// </summary>
public class RenameMarketActorFiles : EditorWindow
{
    [MenuItem("Tools/Market Actor/Rename Files to Match IDs")]
    public static void RenameFiles()
    {
        string folderPath = "Assets/Datas/MarketActor";
        string[] assetGuids = AssetDatabase.FindAssets("t:MarketActorData", new[] { folderPath });

        int renamedCount = 0;
        foreach (string guid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            MarketActorData data = AssetDatabase.LoadAssetAtPath<MarketActorData>(assetPath);
            
            if (data == null || string.IsNullOrEmpty(data.id))
            {
                continue;
            }

            // id를 파일명으로 변환 (snake_case -> PascalCase)
            string newFileName = ConvertIdToFileName(data.id);
            string directory = Path.GetDirectoryName(assetPath);
            string currentFileName = Path.GetFileNameWithoutExtension(assetPath);
            string extension = Path.GetExtension(assetPath);

            if (currentFileName != newFileName)
            {
                string newPath = Path.Combine(directory, newFileName + extension);
                
                // 메타 파일도 함께 이동
                string metaPath = assetPath + ".meta";
                string newMetaPath = newPath + ".meta";

                if (File.Exists(assetPath))
                {
                    AssetDatabase.MoveAsset(assetPath, newPath);
                    renamedCount++;
                    Debug.Log($"Renamed: {currentFileName} -> {newFileName}");
                }
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Rename Complete", 
            $"Renamed {renamedCount} MarketActor files to match their IDs.", "OK");
    }

    private static string ConvertIdToFileName(string id)
    {
        // snake_case를 PascalCase로 변환
        string[] parts = id.Split('_');
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }
            
            // 첫 글자를 대문자로
            sb.Append(char.ToUpper(part[0]));
            if (part.Length > 1)
            {
                sb.Append(part.Substring(1));
            }
        }
        
        return sb.ToString();
    }
}
#endif

