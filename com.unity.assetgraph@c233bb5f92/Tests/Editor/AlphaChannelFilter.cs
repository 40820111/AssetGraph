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
            outputName = string.Format("Aͨ��:{0}", hasAChannel ? "����" : "������");
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
            keyword = EditorGUI.ToggleLeft(rect, "����Alphaͨ��", hasAChannel);
			if (keyword != hasAChannel) {
                hasAChannel = keyword;
                onValueChanged();
			}
		}
	}
}