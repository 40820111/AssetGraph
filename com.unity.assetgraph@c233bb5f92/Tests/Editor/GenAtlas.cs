using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;
using UnityEditor.U2D;
using UnityEngine.U2D;
using static AtlasReferenceData;
using System.Linq;

[CustomNode("Custom/GenerateAtlas", 1000)]
public class GenAtlas : Node {

	[SerializeField] private string atlasPath;

    public override string ActiveStyle {
		get {
			return "node 8 on";
		}
	}

	public override string InactiveStyle {
		get {
			return "node 8";
		}
	}

	public override string Category {
		get {
			return "Custom";
		}
	}

	public override void Initialize(Model.NodeData data) {
        if (atlasPath == null)
            atlasPath = string.Empty;

        data.AddDefaultInputPoint();
		data.AddDefaultOutputPoint();
	}

	public override Node Clone(Model.NodeData newData) {
		var newNode = new GenAtlas();
		newNode.atlasPath = atlasPath;
        newData.AddDefaultInputPoint();
		newData.AddDefaultOutputPoint();
		return newNode;
	}

	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

		EditorGUILayout.HelpBox("My Custom Node: Implement your own Inspector.", MessageType.Info);
		editor.UpdateNodeName(node);

		GUILayout.Space(10f);

		using (new EditorGUILayout.VerticalScope(GUI.skin.box)) 
        {
            var path = atlasPath;
            EditorGUILayout.LabelField("图集生成路径:");

            string newLoadPath = null;

            newLoadPath = editor.DrawFolderSelector(Model.Settings.Path.ASSETS_PATH, "Select Asset Folder",
                path,
                FileUtility.PathCombine(Model.Settings.Path.ASSETS_PATH, path),
                (string folderSelected) =>
				{
					return NormalizePath(folderSelected);
				}
            );

            var currentGenPath = Path.Combine(Model.Settings.Path.ASSETS_PATH, newLoadPath);
            if (newLoadPath != path)
            {
                atlasPath = newLoadPath;
                Debug.LogError("FolderChange!");
            }

            bool dirExists = Directory.Exists(currentGenPath);

            GUILayout.Space(10f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(newLoadPath) || !dirExists))
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Highlight in Project Window", GUILayout.Width(180f)))
                    {
                        if (currentGenPath[currentGenPath.Length - 1] == '/')
                        {
                            currentGenPath = currentGenPath.Substring(0, currentGenPath.Length - 1);
                        }
                        var obj = AssetDatabase.LoadMainAssetAtPath(currentGenPath);
                        EditorGUIUtility.PingObject(obj);
                    }
                }
            }

            if (!dirExists)
            {
                var parentDirPath = Path.GetDirectoryName(currentGenPath);
                bool parentDirExists = Directory.Exists(parentDirPath);
                if (parentDirExists)
                {
                    EditorGUILayout.LabelField("Available Directories:");
                    string[] dirs = Directory.GetDirectories(parentDirPath);
                    foreach (string s in dirs)
                    {
                        EditorGUILayout.LabelField(s);
                    }
                }
            }
        }
	}

	string NormalizePath(string path) 
	{
        if (!string.IsNullOrEmpty(path))
        {
            var dataPath = Application.dataPath;
            if (dataPath == path)
            {
                path = string.Empty;
            }
            else
            {
                int index = path.IndexOf(Model.Settings.Path.ASSETS_PATH);
                path = path.Substring(index+ Model.Settings.Path.ASSETS_PATH.Length);
            }
        }
        return path;
    }
	/**
	 * Prepare is called whenever graph needs update. 
	 */ 
	public override void Prepare (BuildTarget target, 
		Model.NodeData node, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output Output) 
	{
        if (incoming != null)
        {
            foreach (var ag in incoming)
            {
                foreach (var item in ag.assetGroups)
                {
					GenAtlasByFolder(item.Value);
                }
            }
        }
	}

	void GenAtlasByFolder(List<AssetReference> assetReferencesList) 
	{
		Dictionary<string, List<Sprite>> dic = new Dictionary<string, List<Sprite>>();

		for (int i = 0; i < assetReferencesList.Count; i++)
		{
			string folderName  = Directory.GetParent(assetReferencesList[i].path).Name;
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetReferencesList[i].path);

            if (!dic.ContainsKey(folderName))
			{
				dic.Add(folderName, new List<Sprite>());
			}
            dic[folderName].Add(sp);
        }

        string dataPath = "Assets/AssetGraph/AtlasReference.asset";
        AtlasReferenceData referenceData = AssetDatabase.LoadAssetAtPath<AtlasReferenceData>(dataPath);
        if (!referenceData)
        {
            referenceData = ScriptableObject.CreateInstance<AtlasReferenceData>();
            referenceData.atlasList = new List<AtlasData>();

            foreach (var item in dic)
            {
                AtlasData atlasData = new AtlasData() { };
                atlasData.FolderName = item.Key;
                atlasData.spritesList = new List<Sprite>();
                atlasData.spritesList.AddRange(item.Value);

                atlasData.nameList = new List<string>();
                for (int i = 0; i < item.Value.Count; i++)
                {
                    atlasData.nameList.Add(item.Value[i].name);
                }
                referenceData.atlasList.Add(atlasData);
            }
            AssetDatabase.CreateAsset(referenceData, dataPath);
            AssetDatabase.SaveAssets();
            //不存在引用配置，直接全部重新生成
            foreach (var item in dic)
            {
                GenerateAtlas(item.Key, item.Value);
            }
        }
        else
        {
            //检测是否删除文件夹
            string[] deleteFolderArray;
            string oldFolder, newFolder,addFolder;
            if (DeleteFolder(referenceData,out deleteFolderArray))
            {
                for (int i = 0; i < deleteFolderArray.Length; i++)
                {
                    string atlasPath = GetAtlasPath(deleteFolderArray[i]);
                    AssetDatabase.DeleteAsset(atlasPath);
                    AssetDatabase.Refresh();
                    Debug.LogFormat("{0}整个文件夹删除", deleteFolderArray[i]);
                }
            }
            //检测重命名文件夹
            else if (FolderRename(referenceData,dic,out newFolder,out oldFolder))
            {
                Debug.LogFormat("{0}重命名为{1}", oldFolder, newFolder);
                string atlasPath = GetAtlasPath(oldFolder);
                AssetDatabase.DeleteAsset(atlasPath);
                GenerateAtlas(newFolder, dic[newFolder]);
            }
            //新增文件夹
            else if (AddFolder(referenceData, dic,out addFolder))
            {
                Debug.LogFormat("新增文件夹_新建图集:{0}", addFolder);
                GenerateAtlas(addFolder, dic[addFolder]);
            }
            else
            {
                foreach (var item in dic)
                {
                    if (FolderContentChanged(item.Key, item.Value, referenceData))
                    {
                        GenerateAtlas(item.Key, item.Value);
                    }
                }
            }

            //更新引用数据
            referenceData.atlasList.Clear();
            foreach (var item in dic)
            {
                AtlasData atlasData = new AtlasData() { };
                atlasData.FolderName = item.Key;
                atlasData.spritesList = new List<Sprite>();
                atlasData.spritesList.AddRange(item.Value);
                atlasData.nameList = new List<string>();
                for (int i = 0; i < item.Value.Count; i++)
                {
                    atlasData.nameList.Add(item.Value[i].name);
                }
                referenceData.atlasList.Add(atlasData);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    bool DeleteFolder(AtlasReferenceData referenceData,out string[] deleteFolderName) 
    {
        List<string> deleteFolderList = new List<string>();
        for (int i = 0; i < referenceData.atlasList.Count; i++)
        {
            bool deleteFolder = true;
            AtlasData atlasData = referenceData.atlasList[i];
            for (int j = 0; j < atlasData.spritesList.Count; j++)
            {
                if (atlasData.spritesList[j] != null)
                {
                    deleteFolder = false;
                    continue;
                }
            }
            if (deleteFolder)
            {
                deleteFolderList.Add(atlasData.FolderName);
            }
        }

        deleteFolderName = deleteFolderList.ToArray();
        return deleteFolderList.Count > 0;
    }

    bool FolderRename(AtlasReferenceData referenceData, Dictionary<string, List<Sprite>> dic,out string newFolderName,out string oldFolderName) 
    {
        newFolderName = oldFolderName = string.Empty;
        //检测重命名文件夹
        if (referenceData.atlasList.Count == dic.Count)
        {
            for (int i = 0; i < referenceData.atlasList.Count; i++)
            {
                string folderName = referenceData.atlasList[i].FolderName;
                if (!dic.ContainsKey(folderName))
                {
                    oldFolderName = folderName;
                    break;
                }
            }

            for (int i = 0; i < dic.Keys.Count; i++)
            {
                string folderNameIncomming = dic.Keys.ToArray()[i];
                AtlasData atlasData = referenceData.atlasList.Find(data => data.FolderName == folderNameIncomming);
                if (atlasData == null)
                {
                    newFolderName = folderNameIncomming;
                    break;
                }
            }

            return (newFolderName != string.Empty && oldFolderName != string.Empty);
            //Debug.LogFormat("{0}重命名为{1}", oldNameFolder, newNameFolder);
            //string atlasPath = GetAtlasPath(oldNameFolder);
            //AssetDatabase.DeleteAsset(atlasPath);
            //GenerateAtlas(newNameFolder, dic[newNameFolder]);
        }
        else
        {
            return false;
        }
    }

    bool AddFolder(AtlasReferenceData referenceData, Dictionary<string, List<Sprite>> dic,out string addFolder) 
    {
        addFolder = string.Empty;
        for (int i = 0; i < dic.Keys.Count; i++)
        {
            string folderNameIncomming = dic.Keys.ToArray()[i];
            AtlasData atlasData = referenceData.atlasList.Find(data => data.FolderName == folderNameIncomming);
            if (atlasData == null)
            {
                addFolder = folderNameIncomming;
                return true;
            }
        }
        return false;
    }

    bool FolderContentChanged(string folderName,List<Sprite> incomingSprites,AtlasReferenceData atlasReferenceData)
    {
        AtlasData atlasData = atlasReferenceData.atlasList.Find(atlasData => atlasData.FolderName == folderName);

        if (atlasData.spritesList.Count != incomingSprites.Count)
        {
            Debug.LogFormat("{0}:数量发生变化,重新生成图集", folderName);
            return true;
        }

        for (int i = 0; i < incomingSprites.Count; i++)
        {
            Sprite sprite = incomingSprites[i];
            //检测是否修改命名
            string matchedName = atlasData.nameList.Find(s => s == sprite.name);
            if (matchedName == null) 
            {
                Debug.LogFormat("{0}:命名修改,重新生成图集", sprite.name);
                return true;
            }
        }

        return false;
    }

	void GenerateAtlas(string folderName,List<Sprite> spritesList) 
	{
        if (spritesList.Count == 0) 
            return;

        Debug.LogFormat("生成图集:{0}",folderName);
        SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
        {
            blockOffset = 1,
            enableRotation = false,
            enableTightPacking = false,
            padding = 4,
            enableAlphaDilation = true
        };
        SpriteAtlasTextureSettings textureSetting = new SpriteAtlasTextureSettings()
        {
            anisoLevel = 0,
            filterMode = FilterMode.Bilinear,
            generateMipMaps = false,
            readable = false,
            sRGB = true
        };

        string genPath = GetAtlasPath(folderName);
        SpriteAtlas spriteAtlas = new SpriteAtlas();

        SpriteAtlasAsset spriteAtlasAsset = new SpriteAtlasAsset();
        spriteAtlasAsset.Add(spritesList.ToArray());
        spriteAtlasAsset.SetIsVariant(false);
        spriteAtlasAsset.SetMasterAtlas(spriteAtlas);
        SpriteAtlasAsset.Save(spriteAtlasAsset, genPath);
        AssetDatabase.Refresh();

        SpriteAtlasImporter spriteAtlasImporter = AssetImporter.GetAtPath(genPath) as SpriteAtlasImporter;
        spriteAtlasImporter.packingSettings = packSetting;
        spriteAtlasImporter.textureSettings = textureSetting;
        spriteAtlasImporter.includeInBuild = false;
        spriteAtlasImporter.SaveAndReimport();
    }

    string GetAtlasPath(string folderName) 
    {
        string currentGenPath = Path.Combine(Model.Settings.Path.ASSETS_PATH, atlasPath);
        string genPath = Path.Combine(currentGenPath, string.Format("{0}.spriteatlasv2", folderName));
        return genPath;
    }
    /**
	 * Build is called when Unity builds assets with AssetBundle Graph. 
	 */
    public override void Build (BuildTarget target, 
		Model.NodeData nodeData, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output outputFunc,
		Action<Model.NodeData, string, float> progressFunc)
	{
		// Do nothing
	}
}
