using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using TMPro;
public static class PolishInspectionWorkspace
{
 const string Folder="Assets/TaskBubble/Integration/Materials";
 static Scene Scene(){if(Application.isPlaying)throw new InvalidOperationException("Leave Play Mode first.");var s=SceneManager.GetActiveScene();if(s.path!="Assets/TaskBubble/Integration/MainScene.unity"||s.isDirty)throw new InvalidOperationException("Open and save MainScene before polishing.");return s;}
 static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
 static Material Mat(string name,string hex,float metal=0,float smooth=.25f){string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",Hex(hex));m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
 static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;g.layer=2;return g;}
 static void Save(Scene s){EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();}
 public static object Workbench(){var s=Scene();var env=s.GetRootGameObjects().Single(g=>g.name=="Environment").transform;var table=env.Find("Inspection Table");if(table.Find("Bench Details")!=null)throw new InvalidOperationException("Workbench already polished.");
  var dark=Mat("WorkbenchGraphite","#222D35",.45f,.28f);var steel=Mat("BrushedSteel","#8D9AA3",.72f,.42f);var rubber=Mat("InspectionMat","#183D40",0,.15f);var yellow=Mat("SafetyYellow","#E9B93E",.05f,.25f);var ink=Mat("MarkingInk","#B8C7C6",0,.1f);
  table.Find("Top").GetComponent<Renderer>().sharedMaterial=dark;
  foreach(Transform leg in table)if(leg.name=="Leg")leg.GetComponent<Renderer>().sharedMaterial=dark;
  var details=new GameObject("Bench Details").transform;details.SetParent(table,false);
  Box(details,"Front apron",new Vector3(0,.71f,.78f),new Vector3(1.68f,.13f,.045f),dark);Box(details,"Rear apron",new Vector3(0,.71f,1.72f),new Vector3(1.68f,.13f,.045f),dark);
  Box(details,"Lower storage shelf",new Vector3(0,.23f,1.25f),new Vector3(1.5f,.035f,.72f),dark);
  foreach(float x in new[]{-.77f,.77f})foreach(float z in new[]{.85f,1.65f})Box(details,"Adjustable foot",new Vector3(x,.035f,z),new Vector3(.14f,.07f,.14f),dark);
  foreach(float z in new[]{.758f,1.742f})Box(details,"Steel edge",new Vector3(0,.819f,z),new Vector3(1.8f,.027f,.012f),steel);
  foreach(float x in new[]{-.892f,.892f})Box(details,"Corner guard",new Vector3(x,.809f,1.25f),new Vector3(.016f,.08f,1.015f),steel);
  Box(details,"Inspection mat",new Vector3(0,.851f,1.25f),new Vector3(.88f,.003f,.7f),rubber);
  foreach(float x in new[]{-.39f,.39f})Box(details,"Mat border",new Vector3(x,.853f,1.25f),new Vector3(.004f,.001f,.61f),ink);
  foreach(float z in new[]{.945f,1.555f})Box(details,"Mat border",new Vector3(0,.853f,z),new Vector3(.78f,.001f,.004f),ink);
  for(int i=0;i<19;i++)Box(details,"Ruler tick",new Vector3(-.7f+i*.075f,.8508f,.86f),new Vector3(.002f,.001f,i%5==0?.035f:.017f),ink);
  foreach(float x in new[]{-.78f,.78f}){
   Box(details,"Safety corner",new Vector3(x,.852f,.83f),new Vector3(.14f,.004f,.025f),yellow);
   foreach(float z in new[]{.779f,1.721f}){
    var bolt=GameObject.CreatePrimitive(PrimitiveType.Cylinder);bolt.name="Fastener";bolt.transform.SetParent(details,false);bolt.transform.localPosition=new Vector3(x,.72f,z+(z<1?-.025f:.025f));bolt.transform.localRotation=Quaternion.Euler(90,0,0);bolt.transform.localScale=new Vector3(.022f,.004f,.022f);UnityEngine.Object.DestroyImmediate(bolt.GetComponent<Collider>());bolt.GetComponent<Renderer>().sharedMaterial=steel;bolt.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;bolt.layer=2;
   }
  }
  var cad=s.GetRootGameObjects().Single(g=>g.name=="GripperAssembly");cad.transform.position=new Vector3(cad.transform.position.x,.854f,cad.transform.position.z);
  RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Hex("#B4C5D2");RenderSettings.ambientEquatorColor=Hex("#7D8D9A");RenderSettings.ambientGroundColor=Hex("#3A3D42");RenderSettings.ambientIntensity=1;
  var key=s.GetRootGameObjects().Single(g=>g.name=="Key Light").GetComponent<Light>();key.color=Hex("#FFF1DC");key.intensity=1.15f;key.transform.rotation=Quaternion.Euler(48,-30,0);key.shadows=LightShadows.Hard;
  var fill=s.GetRootGameObjects().Single(g=>g.name=="Fill Light").GetComponent<Light>();fill.color=Hex("#C4D8E8");fill.intensity=.28f;fill.shadows=LightShadows.None;
  Save(s);return new{workbench=true,newLights=0,decorativeColliders=details.GetComponentsInChildren<Collider>().Length};
 }
}
