using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.AssetGraph;
using Model = UnityEngine.AssetGraph.DataModel.Version2;

[CustomFilter("AlphaFilter")]
public class AlphaChannelFilter : IFilter {

	[SerializeField]
	private bool hasAChannel;

	private string outputName;
	public string Label { 
		get {
            outputName = string.Format("A通道:{0}", hasAChannel ? "存在" : "不存在");
            return outputName;
		}
	}

	public AlphaChannelFilter() {
		hasAChannel = false;
    }

	public bool FilterAsset(AssetReference a)
	{
		if (a.assetType == typeof(Texture2D))
		{
            var importer = AssetImporter.GetAtPath(a.importFrom) as TextureImporter;
            return importer.DoesSourceTextureHaveAlpha() == hasAChannel;
		}
		else
		{
			return false;
		}
    }

	public void OnInspectorGUI (Rect rect, Action onValueChanged) {

		bool keyword = hasAChannel;
		using (new EditorGUILayout.HorizontalScope()) {
            keyword = EditorGUI.ToggleLeft(rect, "存在Alpha通道", hasAChannel);
			if (keyword != hasAChannel) {
                hasAChannel = keyword;
                onValueChanged();
			}
		}
	}
}