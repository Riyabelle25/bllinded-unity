using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TensorFlowLite;
//D:\JapGuy\Unity-3d-pose-baseline-master\Assets\pose_unity\Pose.txt
public class PoseNetSample : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string fileName = "posenet_mobilenet_v1_100_257x257_multi_kpt_stripped.tflite";
  
   //[SerializeField] RawImage cameraView = null;
   // [SerializeField, Range(0f, 1f)] float threshold = 0.5f;
     WebCamTexture webcamTexture;

    PoseNet poseNet;
    Vector3[] corners = new Vector3[4];
    public PoseNet.Result[] results;

  //  static Vector3 a; static Vector3 b; static Vector3 c;static Vector3 d; static Vector3 e; static Vector3 f;
	


	float scale_ratio = 0.001f;  // pos.txt
  
    float heal_position = 0.05f; // 
    float head_angle = 15f; // 15
    public String pos_filename = "Pose.txt";  // pos.txt
    public Boolean debug_cube;   
    public int start_frame;     
    public String end_frame;  
    float play_time; 
    Transform[] bone_t; // Transform
    Transform[] cube_t; // Cube_Transform
    Vector3 init_position; 
    Quaternion[] init_rot; 
    Quaternion[] init_inv; //Inverse
    List<Vector3[]> pos; // pos.txt
	int[] bones = new int[10] { 1, 2, 4, 5, 7, 8, 11, 12, 14, 15 }; 
    int[] child_bones = new int[10] { 2, 3, 5, 6, 8, 9, 12, 13, 15, 16 }; // bones
    int bone_num = 17;
    Animator anim;
    int s_frame;
    int e_frame;

//Variable Definitions end

   
// List<Vector3[]> ReadPosData(string filename)
//      {
        
//         StreamReader sr = new StreamReader(filename);
//         while (!sr.EndOfStream) 
//         {
//             lines.Add(sr.ReadLine());
//         }
//         sr.Close();

//     try{
        
//         foreach (string line in lines) {
//             string line2 = line.Replace(",", "");
//             string[] str = line2.Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries); 

//             Vector3[] vs = new Vector3[bone_num];
//             for (int i = 0; i < str.Length; i += 4)
//             {
    
//                 int a = int.Parse(str[i]);
//                 vs[a] = new Vector3(1000*(-float.Parse(str[i + 1],CultureInfo.InvariantCulture)), 1000*(-float.Parse(str[i + 3],CultureInfo.InvariantCulture)), 1000*(-float.Parse(str[i + 2],CultureInfo.InvariantCulture)));
//                     Debug.Log(a);
//                     Debug.Log(vs[a]);
//             }
//             vs[10] = new Vector3(0,0,0);
//             Debug.Log(vs[10]);
//             data.Add(vs);
//         }

//     }
//         catch (Exception e) {
//             Debug.Log(e);
//             return null;
//         }
//         return data;
//     }

    void GetInitInfo()
    {
        bone_t = new Transform[bone_num];
        init_rot = new Quaternion[bone_num];
        init_inv = new Quaternion[bone_num];

        bone_t[0] = anim.GetBoneTransform(HumanBodyBones.Hips);
        bone_t[1] = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        bone_t[2] = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        bone_t[3] = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        bone_t[4] = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        bone_t[5] = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        bone_t[6] = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        bone_t[7] = anim.GetBoneTransform(HumanBodyBones.Spine);
        bone_t[8] = anim.GetBoneTransform(HumanBodyBones.Neck);
        bone_t[9] = anim.GetBoneTransform(HumanBodyBones.Head);
        bone_t[11] = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        bone_t[12] = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        bone_t[13] = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        bone_t[14] = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        bone_t[15] = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        bone_t[16] = anim.GetBoneTransform(HumanBodyBones.RightHand);

       
        Vector3 init_forward = TriangleNormal(bone_t[7].position, bone_t[4].position, bone_t[1].position);
        init_inv[0] = Quaternion.Inverse(Quaternion.LookRotation(init_forward));

        init_position = bone_t[0].position;
        init_rot[0] = bone_t[0].rotation;

        try{
            for (int i = 0; i < bones.Length; i++) {
            int b = bones[i];
            int cb = child_bones[i];
        
           init_rot[b] = bone_t[b].rotation;
            
            init_inv[b] = Quaternion.Inverse(Quaternion.LookRotation(bone_t[b].position - bone_t[cb].position, init_forward));
            
        }

        }
        catch(Exception e){
            Debug.Log(e);
        }
        
    }

    
    Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }
	
	static Vector3 ThoraxCalc(Vector3 a1, Vector3 b1)
	{		
	Vector3 t = (a1+b1)/2;
	return t;
	}
	
	static Vector3 SpineCalc(Vector3 a2, Vector3 b2, Vector3 c2)
	{ 
    Vector3 s2 = (b2+c2)/2;
	Vector3 s=(a2+s2)/2;
	return s;
	}

    static Vector3 HipCalc(Vector3 a3, Vector3 b3 , Vector3 c3){
        Vector3 h3=(b3+c3)/2;
       Vector3 h= (a3+h3)/2;
       return h;
    }
   
    void UpdateCube(int frame)
    {
        if (cube_t == null) {
            
            cube_t = new Transform[bone_num];

            for (int i = 0; i < bone_num; i++) {
                Transform t = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                t.transform.parent = this.transform;
                t.localPosition = pos[frame][i] * scale_ratio;
                t.name = i.ToString();
                t.localScale = new Vector3(50f, 50f, 50f);
                cube_t[i] = t;

                Destroy(t.GetComponent<BoxCollider>());
            }
        }
        else {
            
            Vector3 offset = new Vector3(1.2f, 0, 0);

            for (int i = 0; i < bone_num; i++) {
                cube_t[i].localPosition = pos[frame][i] * scale_ratio + new Vector3(0, heal_position, 0) + offset;
            }
        }
    }

//Function definitions End

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
       
        poseNet = new PoseNet(path);
	//	Vector3 a= new Vector3(0,0,0); Vector3 b= new Vector3(0,0,0); Vector3 c= new Vector3(0,0,0); Vector3 d= new Vector3(0,0,0); Vector3 e= new Vector3(0,0,0); Vector3 f= new Vector3(0,0,0);
		
 // Init camera
       string cameraName = WebCamUtil.FindName();
        webcamTexture = new WebCamTexture(cameraName, 640, 480, 30);
        webcamTexture.Play();

       // cameraView.texture = webcamTexture;
       // glDrawer.OnDraw += OnGLDraw;
//
   anim = GetComponent<Animator>();
        play_time = 0;

        

        GetInitInfo();

        if (pos != null) {
            // inspectorで指定した開始フレーム、終了フレーム番号をセット
            if (start_frame >= 0 && start_frame < pos.Count) {
                s_frame = start_frame;
            } else {
                s_frame = 0;
            }
            int ef;
            if (int.TryParse(end_frame, out ef)) {
                if (ef >= s_frame && ef < pos.Count) {
                    e_frame = ef;
                } else {
                    e_frame = pos.Count - 1;
                }
            } else {
                e_frame = pos.Count - 1;
            }
            Debug.Log("End Frame:" + e_frame.ToString());
        }
	}

    void OnDestroy()
    {
        webcamTexture?.Stop();
        poseNet?.Dispose();
        //File.Delete(@"D:\TFLite\tf-lite-unity-sample-master\tf-lite-unity-sample-master\Assets\PoseNet\Pose.txt");
   
    }

    void Update()
    {
         poseNet.Invoke(webcamTexture);
         results = poseNet.GetResults();
         pos = ReadPoseData(results);

	//  write results into pose.txt	

	    // RESULT = BuildResults(results);
 //       Task asyncTask = WriteFileAsync(longString);

		if (pos == null) {
            return;
        }
        play_time += Time.deltaTime;

        int frame = s_frame + (int)(play_time * 30.0f);  
       
        Debug.Log($"Frame: {frame}");
        if (frame > e_frame) {
            play_time = 0;  
            frame = s_frame;
            
        }

        if (debug_cube) {
            UpdateCube(frame); 
        }

        Vector3[] now_pos = pos[frame];

  
        Vector3 pos_forward = TriangleNormal(now_pos[7], now_pos[4], now_pos[1]);
        bone_t[0].position = now_pos[0] * scale_ratio + new Vector3(init_position.x, heal_position, init_position.z);
        bone_t[0].rotation = Quaternion.LookRotation(pos_forward) * init_inv[0] * init_rot[0];

       
        for (int i = 0; i < bones.Length; i++){
            int b = bones[i];
            int cb = child_bones[i];
            bone_t[b].rotation = Quaternion.LookRotation(now_pos[b] - now_pos[cb], pos_forward) * init_inv[b] * init_rot[b];
         
        }

        
        bone_t[8].rotation = Quaternion.AngleAxis(head_angle, bone_t[11].position - bone_t[14].position) * bone_t[8].rotation;
	 }  
	
	// static async Task WriteFileAsync(string content)
 //    {
 //            Console.WriteLine("Async Write File has started.");
 //            using(StreamWriter outputFile = new StreamWriter(@"D:\TFLite\tf-lite-unity-sample-master\tf-lite-unity-sample-master\Assets\PoseNet\Pose.txt", true))
 //            {
 //                await outputFile.WriteAsync(content);
 //            }
 //            Console.WriteLine("Async Write File has completed.");
 //        }

    List<Vector3[]> ReadPoseData(PoseNet.Result[] results){

        List<Vector3[]> data = new List<Vector3[]>();
        List<string> lines = new List<string>();
        Vector3[] vs = new Vector3[bone_num];

        vs[11] = new Vector3(0,0,0); vs[14] = new Vector3(0,0,0); vs[8] = new Vector3(0,0,0);
        vs[1] = new Vector3(0,0,0); vs[4] = new Vector3(0,0,0); vs[7] = new Vector3(0,0,0);

        int[] pose_net = new int[16] { 0, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        int[] open_pose =new int[16] { 9, 11, 14, 12, 15, 13, 16, 4, 1, 5, 2, 6, 3, 8, 7, 0 };

        for(int i=0; i<pose_net.Length; i++){
            int p = pose_net[i];
            int o = open_pose[i];

            Debug.Log("Before switch p " + p);
            switch(p)
            {   

                case 17: vs[o]= new Vector3(ThoraxCalc(vs[11], vs[14]).x, (ThoraxCalc(vs[11], vs[14]).y), 0); //Thorax
                break;
                case 18: vs[o]= new Vector3(SpineCalc(vs[8], vs[1], vs[4]).x, (SpineCalc(vs[8], vs[1], vs[4]).y), 0); //Spine
                break;
                case 19: vs[o]= new Vector3(HipCalc(vs[7], vs[1], vs[4]).x, (HipCalc(vs[7], vs[1], vs[4]).y), 0); //Hips
                break;
                default: vs[o]= new Vector3(-1000*results[p].x, -1000*results[p].y, 0);
                break;

            }
            Debug.Log("p" + vs[o]);
             }
        vs[10]= new Vector3(0 ,0 ,0);
        data.Add(vs);

        return data;
    }
              
 }
   //  static string BuildResults(PoseNet.Result[] results)
   //      {
			// StringBuilder myStringBuilder = new StringBuilder();
			
   //          for(int i = 0; i < 20; i++)
   //              { 
			//       switch(i){
   //                  // switch(i){

   //          //         case 0: myStringBuilder.Append($"9 {(results[i].x)} 0 {(results[i].y)}, ");    //Nose or Head
   //          //                   break;
   //          //         case 5: myStringBuilder.Append($"11 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   a = new Vector3((results[i].x),(results[i].y),0);                   //LShoulder
   //          //                   break;
   //          //         case 6: myStringBuilder.Append($"14 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   b = new Vector3((results[i].x),(results[i].y),0);                   //Rshoulder
   //          //                   c = new Vector3(ThoraxCalc(a,b).x,ThoraxCalc(a,b).y,ThoraxCalc(a,b).z);                                  //Thorax
   //          //                   break;
   //          //         case 7: myStringBuilder.Append($"12 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   break;
   //          //         case 8: myStringBuilder.Append($"15 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   break;
   //          //         case 9: myStringBuilder.Append($"13 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   break;
   //          //         case 10:myStringBuilder.Append($"16 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   break;                              
   //          //         case 11: myStringBuilder.Append($"4 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   e = new Vector3((results[i].x),(results[i].y),0);                    //Lhip
   //          //                   break;
   //          //         case 12: myStringBuilder.Append($"1 {(results[i].x)} 0 {(results[i].y)}, ");        
   //          //                   f = new Vector3((results[i].x),(results[i].y),0);                  //Rhip     
   //          //                   d = new Vector3(SpineCalc(c,e,f).x,SpineCalc(c,e,f).y,SpineCalc(c,e,f).z);
   //          //                   break;
   //          //         case 13: myStringBuilder.Append($"5 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   break;
   //          //         case 14: myStringBuilder.Append($"2 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   break;
   //          //         case 15: myStringBuilder.Append($"6 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   break;
   //          //         case 16: myStringBuilder.Append($"3 {(results[i].x)} 0 {(results[i].y)}, ");
   //          //                   break;
   //          //         case 17: myStringBuilder.Append($"8 {c.x} 0 {c.y}, ");  //THORAX
   //          //                   break;
   //          //         case 18: myStringBuilder.Append($"7 {d.x} 0 {d.y}, ");  //SPINE
   //          //                   break;
   //          //         case 19: myStringBuilder.Append($"0 {(HipCalc(d,e,f).x)} 0 {(HipCalc(d,e,f).y)}, ");  //Hips
   //          //                   break;
   //          //         default:  break;

   //          // }
        
			// 		}
			// }

   //          myStringBuilder.AppendLine();            

			// return myStringBuilder.ToString();
   //      }


