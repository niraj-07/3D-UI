using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaskBubble.VR;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;
public static class AddTrackedHandMeshes
{
 public static object Build()
 {
  if(Application.isPlaying)throw new InvalidOperationException("Leave Play Mode first.");
  const string rigPath="Assets/TaskBubble/VR/PcVrRig.prefab";
  const string folder="Assets/TaskBubble/VR/Hands";
  if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/TaskBubble/VR","Hands");
  var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.xr.hands");
  foreach(string side in new[]{"Left","Right"}) {
   string path=folder+"/"+side+"Hand.fbx";
   if(!File.Exists(path))File.Copy(Path.Combine(package.resolvedPath,"Samples~/HandVisualizer/Models/"+side+"Hand.fbx"),path);
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
  }
  string notice=folder+"/NOTICE.txt";
  if(!File.Exists(notice))File.WriteAllText(notice,"LeftHand.fbx and RightHand.fbx are from Unity XR Hands 1.10.0, Hand Visualizer sample. Copyright Unity Technologies. See the accompanying Unity license.\n");
  string license=Path.Combine(package.resolvedPath,"LICENSE.md");
  if(File.Exists(license)&&!File.Exists(folder+"/LICENSE.md"))File.Copy(license,folder+"/LICENSE.md");
  AssetDatabase.Refresh();
  string matPath=folder+"/Hand.mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
  if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.65f,.85f,.92f));material.SetFloat("_Smoothness",.2f);AssetDatabase.CreateAsset(material,matPath);}
  var rig=PrefabUtility.LoadPrefabContents(rigPath);
  try {
   var tracked=rig.GetComponentInChildren<VrPartSelection>(true).transform;
   if(tracked.Find("Left Hand Visual")!=null)throw new InvalidOperationException("Hands already added.");
   foreach(string side in new[]{"Left","Right"}) {
    var holder=new GameObject(side+" Hand Visual");holder.SetActive(false);holder.transform.SetParent(tracked,false);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/"+side+"Hand.fbx"));model.transform.SetParent(holder.transform,false);
    var wrist=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name.EndsWith("Wrist"));
    // The imported model root is the wrist's tracking-space parent.
    model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
    var events=holder.AddComponent<XRHandTrackingEvents>();events.handedness=side=="Left"?Handedness.Left:Handedness.Right;events.updateType=XRHandTrackingEvents.UpdateTypes.Dynamic|XRHandTrackingEvents.UpdateTypes.BeforeRender;
    var skeleton=holder.AddComponent<XRHandSkeletonDriver>();skeleton.handTrackingEvents=events;skeleton.rootTransform=wrist;
    skeleton.jointTransformReferences=new List<JointToTransformReference>();var missing=new List<string>();skeleton.FindJointsFromRoot(missing);
    if(missing.Count>0)throw new InvalidOperationException("Missing hand joints: "+string.Join(",",missing));
    var renderer=model.GetComponentInChildren<SkinnedMeshRenderer>(true);renderer.sharedMaterial=material;renderer.enabled=false;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.updateWhenOffscreen=true;
    var controller=holder.AddComponent<XRHandMeshController>();controller.handTrackingEvents=events;controller.handMeshRenderer=renderer;
    foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.layer=2;
    holder.SetActive(true);
   }
   PrefabUtility.SaveAsPrefabAsset(rig,rigPath);
   AssetDatabase.SaveAssets();
   return new {hands=2,jointsPerHand=26,colliders=0};
  } finally {PrefabUtility.UnloadPrefabContents(rig);}
 }
}
