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
 public static object FinishRoom(){var s=Scene();var env=s.GetRootGameObjects().Single(g=>g.name=="Environment").transform;var room=env.Find("Workshop Details");if(room.Find("Ceiling")==null)Box(room,"Ceiling",new Vector3(0,3.03f,1),new Vector3(6,.06f,5),AssetDatabase.LoadAssetAtPath<Material>(Folder+"/WorkshopWall.mat"));
  const string meshFolder="Assets/TaskBubble/Integration/DecorMeshes";if(!AssetDatabase.IsValidFolder(meshFolder))AssetDatabase.CreateFolder("Assets/TaskBubble/Integration","DecorMeshes");int before=0,after=0;
  foreach(var root in new[]{env.Find("Inspection Table/Bench Details"),room}){
   var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<TMP_Text>()==null).ToArray();before+=filters.Length;
   foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial)){
    string path=meshFolder+"/"+root.name.Replace(" ","")+group.Key.name+".asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(path)!=null)throw new InvalidOperationException("Decor already combined.");
    var mesh=new Mesh{name=root.name+" "+group.Key.name};mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);AssetDatabase.CreateAsset(mesh,path);
    var go=new GameObject(group.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.layer=2;go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=group.Key;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;after++;
   }
   foreach(var f in filters)UnityEngine.Object.DestroyImmediate(f.gameObject);
  }
  Save(s);return new{detailRenderersBefore=before,detailRenderersAfter=after};
 }
 static TMP_Text Label(Transform parent,string name,string value,Vector3 position,Vector2 size,float fontSize){var g=new GameObject(name,typeof(TextMeshPro));g.transform.SetParent(parent,false);g.transform.localPosition=position;var t=g.GetComponent<TextMeshPro>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=fontSize;t.color=Hex("#D9E3E5");t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=size;t.textWrappingMode=TextWrappingModes.NoWrap;t.raycastTarget=false;g.layer=2;return t;}
 public static object Room(){var s=Scene();var env=s.GetRootGameObjects().Single(g=>g.name=="Environment").transform;if(env.Find("Workshop Details")!=null)throw new InvalidOperationException("Room already polished.");
  var wall=Mat("WorkshopWall","#9AA8AE",0,.18f);var concrete=Mat("ConcreteFloor","#656D71",0,.1f);var dark=Mat("WorkbenchGraphite","#222D35",.45f,.28f);var yellow=Mat("SafetyYellow","#E9B93E",.05f,.25f);var steel=Mat("BrushedSteel","#8D9AA3",.72f,.42f);var cabinet=Mat("CabinetBlue","#3B5967",.3f,.25f);var seam=Mat("FloorSeams","#414A4F",0,.1f);var lamp=Mat("FixtureDiffuser","#DCE8EC",0,.1f);lamp.EnableKeyword("_EMISSION");lamp.SetColor("_EmissionColor",Hex("#DCE8EC")*1.25f);
  env.Find("Floor").GetComponent<Renderer>().sharedMaterial=concrete;env.Find("Back Wall").GetComponent<Renderer>().sharedMaterial=wall;
  var root=new GameObject("Workshop Details").transform;root.SetParent(env,false);
  foreach(float x in new[]{-3f,3f})Box(root,"Side wall",new Vector3(x,1.5f,1),new Vector3(.1f,3,5),wall);
  Box(root,"Wall skirting",new Vector3(0,.09f,3.43f),new Vector3(6,.18f,.05f),dark);Box(root,"Lower wall cladding",new Vector3(0,.5f,3.43f),new Vector3(6,.6f,.035f),cabinet);
  foreach(float x in new[]{-2.95f,-1.5f,0,1.5f,2.95f})Box(root,"Wall panel seam",new Vector3(x,1.72f,3.435f),new Vector3(.025f,2.35f,.03f),steel);
  Box(root,"Service conduit",new Vector3(0,2.65f,3.4f),new Vector3(5.9f,.035f,.045f),dark);
  for(int i=-2;i<=3;i++){
   Box(root,"Floor expansion joint",new Vector3(i,.001f,1),new Vector3(.007f,.001f,6),seam);
   Box(root,"Floor expansion joint",new Vector3(0,.001f,i),new Vector3(6,.001f,.007f),seam);
  }
  foreach(float x in new[]{-1.25f,1.25f})Box(root,"Inspection zone marking",new Vector3(x,.003f,1.25f),new Vector3(.035f,.002f,1.65f),yellow);
  foreach(float z in new[]{.425f,2.075f})Box(root,"Inspection zone marking",new Vector3(0,.003f,z),new Vector3(2.535f,.002f,.035f),yellow);
  Box(root,"Station sign",new Vector3(0,2.03f,3.38f),new Vector3(1.85f,.48f,.035f),dark);
  Label(root,"Station title","ASSEMBLY / INSPECTION",new Vector3(0,2.12f,3.35f),new Vector2(1.7f,.16f),1.1f);
  var station=Label(root,"Station number","STATION 01",new Vector3(0,1.95f,3.35f),new Vector2(1.7f,.14f),.9f);station.color=Hex("#E9B93E");
  Box(root,"Tool cabinet",new Vector3(2.2f,.91f,2.93f),new Vector3(.85f,1.82f,.48f),dark);
  foreach(float x in new[]{1.995f,2.405f}){
   Box(root,"Cabinet door",new Vector3(x,.94f,2.68f),new Vector3(.39f,1.65f,.025f),cabinet);
   Box(root,"Cabinet handle",new Vector3(x+(x<2.2f?.135f:-.135f),.95f,2.655f),new Vector3(.022f,.18f,.025f),steel);
  }
  Label(root,"Cabinet label","TOOLING",new Vector3(2.2f,1.57f,2.65f),new Vector2(.7f,.14f),.8f);
  Box(root,"Parts bin",new Vector3(-2.2f,.28f,2.92f),new Vector3(.85f,.5f,.6f),cabinet);Box(root,"Bin lid",new Vector3(-2.2f,.54f,2.92f),new Vector3(.9f,.04f,.65f),dark);
  Label(root,"Bin label","COMPONENTS",new Vector3(-2.2f,.31f,2.61f),new Vector2(.72f,.14f),.7f);
  foreach(float x in new[]{-1.55f,0,1.55f}){
   Box(root,"Ceiling fixture",new Vector3(x,2.88f,1.3f),new Vector3(.2f,.07f,1.4f),dark);
   Box(root,"Light diffuser",new Vector3(x,2.838f,1.3f),new Vector3(.15f,.014f,1.3f),lamp);
  }
  Save(s);return new{room=true,decorativeColliders=root.GetComponentsInChildren<Collider>().Length,lights=s.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Light>(true).Length)};
 }
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
