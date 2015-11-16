using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

/// <summary>
/// Ovrvision Custom Editor
/// </summary>
[CustomEditor( typeof(Ovrvision) )]
public class OvrvisionEditor : Editor {

	private const int OV_SET_AUTOMODE = (-1);

	public override void OnInspectorGUI() {
		Ovrvision obj = target as Ovrvision;
        OvrvisionProperty resultProp = obj.camProp;
       
		EditorGUILayout.LabelField( "Ovrvision Status" );
		if(obj.camStatus)
			EditorGUILayout.HelpBox( "Opened", MessageType.Info, true );
		else
			EditorGUILayout.HelpBox( "Closed", MessageType.Error, true );

		EditorGUILayout.Space();

        EditorGUILayout.LabelField("Ovrvision Configures");
		if (EditorGUILayout.Toggle ("Exposure Automatic", resultProp.exposure < 0) == false)
			resultProp.exposure = EditorGUILayout.IntSlider ("Exposure level", resultProp.exposure, 0, 5);
		else
			resultProp.exposure = OV_SET_AUTOMODE;	//Automode

		if (EditorGUILayout.Toggle ("Color temp Automatic", resultProp.whitebalance < 0) == false)
			resultProp.whitebalance = EditorGUILayout.IntSlider( "Color temperature", resultProp.whitebalance, 2800, 6500 );
		else
			resultProp.whitebalance = OV_SET_AUTOMODE;	//Automode

		resultProp.contrast = EditorGUILayout.IntSlider( "Contrast", resultProp.contrast, 0, 127 );
		resultProp.saturation = EditorGUILayout.IntSlider( "Saturation", resultProp.saturation, 0, 127 );
		resultProp.brightness = EditorGUILayout.IntSlider( "Brightness", resultProp.brightness, 0, 255 );
		resultProp.sharpness = EditorGUILayout.IntSlider( "Sharpness", resultProp.sharpness, 0, 15 );
		resultProp.gamma = EditorGUILayout.IntSlider( "Gamma", resultProp.gamma, 0, 10 );

		resultProp.focalPoint = EditorGUILayout.FloatField("FocalPoint", resultProp.focalPoint);

        EditorGUILayout.Space();
        if (GUILayout.Button("Default config", GUILayout.Width(200)))
            resultProp.DefaultConfig();

        EditorGUILayout.Space();

        //Ovrvision ex
        obj.useOvrvisionAR = EditorGUILayout.Toggle("Use the OvrvisionAR", obj.useOvrvisionAR);
		obj.arSize = EditorGUILayout.FloatField("AR Marker size(m).", obj.arSize);

        EditorGUILayout.Space();

        //Select plane shader
        string[] planeshader = {"Normal Shader", "Chroma-key Shader"};
        obj.camViewShader = EditorGUILayout.Popup("Camera view shader", obj.camViewShader, planeshader);
        if (obj.camViewShader == 1) //Chroma-Key shader
        {
            obj.chroma_hue.x = EditorGUILayout.Slider("Max Hue", obj.chroma_hue.x, 0.0f, 1.0f);
            obj.chroma_hue.y = EditorGUILayout.Slider("Min Hue", obj.chroma_hue.y, 0.0f, 1.0f);
            obj.chroma_saturation.x = EditorGUILayout.Slider("Max Saturation", obj.chroma_saturation.x, 0.0f, 1.0f);
            obj.chroma_saturation.y = EditorGUILayout.Slider("Min Saturation", obj.chroma_saturation.y, 0.0f, 1.0f);
            obj.chroma_brightness.x = EditorGUILayout.Slider("Max Brightness", obj.chroma_brightness.x, 0.0f, 1.0f);
            obj.chroma_brightness.y = EditorGUILayout.Slider("Min Brightness ", obj.chroma_brightness.y, 0.0f, 1.0f);
        }

		EditorUtility.SetDirty( target );	//editor set
		//changed param
		if (GUI.changed) {
			obj.UpdateOvrvisionSetting(resultProp);	//apply
			EditorUtility.SetDirty( target );	//editor set
		}
	}
}