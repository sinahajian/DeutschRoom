using System.Collections.Generic;
using UnityEngine;

// Small procedural meshes, generated once. No collider, texture download or per-frame mesh rebuilding.
public static class AvatarAnatomy
{
    public static Mesh Head(bool hair)
    {
        const int sides = 64, rows = 40;
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
        for (int row = 0; row <= rows; row++) for (int side = 0; side <= sides; side++)
        {
            float angle = side * Mathf.PI * 2 / sides;
            float end = hair ? Mathf.Lerp(1.85f, 1.22f + .055f * Mathf.Sin(angle * 3), Mathf.Max(0, Mathf.Cos(angle))) : Mathf.PI;
            float latitude = row * end / rows;
            float y = Mathf.Cos(latitude) * .5f;
            float jaw = Mathf.Lerp(.72f, 1, Mathf.InverseLerp(-.46f, -.06f, y));
            float x = Mathf.Sin(latitude) * Mathf.Sin(angle) * .5f * jaw;
            float z = Mathf.Sin(latitude) * Mathf.Cos(angle) * (Mathf.Cos(angle) > 0 ? .43f : .48f);
            if (!hair && Mathf.Cos(angle) > 0)
            {
                float front = Mathf.Pow(Mathf.Max(0, Mathf.Cos(angle)), 6);
                // Continuous cheek/chin volumes and recessed eye sockets.
                float cheek = .022f * Mathf.Exp(-Mathf.Pow((y + .045f) / .13f, 2));
                float socket = .018f * Mathf.Exp(-Mathf.Pow((y - .085f) / .065f, 2));
                float chin = .035f * Mathf.Exp(-Mathf.Pow((y + .34f) / .09f, 2));
                float bridge = .055f * Mathf.Exp(-Mathf.Pow(x/.062f,2)-Mathf.Pow((y-.005f)/.18f,2));
                float tip = .078f * Mathf.Exp(-Mathf.Pow(x/.075f,2)-Mathf.Pow((y+.085f)/.055f,2));
                z += front * (cheek - socket + chin) + bridge + tip;
            }
            if (hair)
            {
                float sweep = Mathf.Sin(latitude) * Mathf.Max(0, Mathf.Cos(angle));
                x = x * 1.045f + .035f * sweep;
                z *= 1.045f;
                y += .025f + .045f * sweep * (.65f + .35f * Mathf.Sin(angle));
            }
            vertices.Add(new Vector3(x, y, z)); uv.Add(new Vector2((float)side / sides, (float)row / rows));
        }
        for (int row = 0; row < rows; row++) for (int side = 0; side < sides; side++)
        {
            int a = row * (sides + 1) + side, b = a + sides + 1;
            indices.Add(a); indices.Add(b); indices.Add(a+1);
            indices.Add(a+1); indices.Add(b); indices.Add(b+1);
        }
        var mesh = new Mesh { name = hair ? "Swept hair cap" : "Shaped jaw and cranium" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        var normals=mesh.normals;
        for(int row=0;row<=rows;row++)
        {
            int first=row*(sides+1),last=first+sides;
            Vector3 n=(normals[first]+normals[last]).normalized;
            normals[first]=normals[last]=n;
        }
        // Pole triangles are degenerate; an explicit normal avoids a pinched light seam.
        for(int i=0;i<=sides;i++)normals[i]=Vector3.up;
        if(!hair)for(int i=rows*(sides+1);i<normals.Length;i++)normals[i]=Vector3.down;
        mesh.normals=normals;mesh.RecalculateBounds();return mesh;
    }

    // Curved facial strips sit on the skin instead of floating sphere-shaped features.
    public static Mesh Feature(float width, float height, float arch, float depth)
    {
        const int steps=24;
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<=steps;i++)
        {
            float x=(float)i/steps*2-1;
            float taper=Mathf.Sqrt(Mathf.Max(.015f,1-x*x));
            for(int edge=0;edge<2;edge++)
                v.Add(new Vector3(x*width*.5f,arch*(1-x*x)+(edge==0?-1:1)*height*.5f*taper,depth*(1-x*x)));
            if(i==steps)continue;
            int a=i*2;t.Add(a);t.Add(a+2);t.Add(a+1);t.Add(a+1);t.Add(a+2);t.Add(a+3);
        }
        var mesh=new Mesh {name="Curved facial contour"};mesh.SetVertices(v);mesh.SetTriangles(t,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }

    // Tailored shirt silhouette: rounded shoulders, chest and waist without capsule ends.
    public static Mesh Shirt()
    {
        float[] heights = {-.5f,-.46f,-.12f,.22f,.37f,.47f,.5f};
        float[] widths = {.43f,.46f,.48f,.55f,.58f,.44f,.24f};
        float[] depths = {.42f,.45f,.48f,.50f,.46f,.34f,.25f};
        const int sides = 40;
        var v = new List<Vector3>(); var triangles = new List<int>();
        for (int r=0;r<heights.Length;r++) for (int i=0;i<=sides;i++)
        {
            float a=i*Mathf.PI*2/sides;
            v.Add(new Vector3(Mathf.Sin(a)*widths[r],heights[r],Mathf.Cos(a)*depths[r]));
            if(r==0 || i==sides)continue;
            int b=r*(sides+1)+i, a0=b-sides-1;
            triangles.Add(a0);triangles.Add(a0+1);triangles.Add(b);
            triangles.Add(a0+1);triangles.Add(b+1);triangles.Add(b);
        }
        var mesh=new Mesh {name="Tailored sweatshirt silhouette"};
        mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }

    public sealed class Hand
    {
        public readonly Transform root;
        public readonly Mesh mesh;
        private readonly Transform[,] fingers = new Transform[5,3];
        private readonly float mirror;
        private float lastGrip = float.NaN;
        public Hand(Transform root, Material skin, bool left)
        {
            this.root = root; mirror = left ? -1 : 1;
            var bones = new List<Transform> { root };
            var vertices = new List<Vector3>(); var normals = new List<Vector3>();
            var weights = new List<BoneWeight>(); var indices = new List<int>();
            AddOval(new Vector3(0,0,.043f),new Vector3(.037f,.014f,.046f),0,root);
            AddOval(new Vector3(-mirror*.024f,-.002f,.032f),new Vector3(.022f,.015f,.031f),0,root);
            float[] lengths = {.071f,.079f,.073f,.057f};
            float[] widths = {.0082f,.0088f,.008f,.0069f};
            for (int finger = 0; finger < 5; finger++)
            {
                float length = finger < 4 ? lengths[finger] : .062f;
                Transform parent = root;
                for (int joint = 0; joint < 3; joint++)
                {
                    var bone = new GameObject((finger == 4 ? "Thumb" : "Finger " + (finger+1)) + " joint " + (joint+1)).transform;
                    bone.SetParent(parent,false);
                    if (joint == 0)
                        bone.localPosition = finger == 4 ? new Vector3(-mirror*.034f,-.001f,.026f) :
                            new Vector3(mirror*(-.026f+finger*.0175f),0,finger==3?.072f:.082f);
                    else bone.localPosition = Vector3.forward * length * (joint==1?.43f:.33f);
                    bones.Add(bone); fingers[finger,joint] = bone;
                    float segment = length * (joint==0?.43f:joint==1?.33f:.24f);
                    float radius = (finger==4?.009f:widths[finger]) * (1-joint*.12f);
                    AddOval(Vector3.forward * segment*.5f,new Vector3(radius,radius*.85f,segment*.57f),bones.Count-1,bone);
                    parent = bone;
                }
            }
            mesh = new Mesh { name = left ? "Left articulated hand" : "Right articulated hand" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(indices,0); mesh.boneWeights = weights.ToArray();
            var bind = new Matrix4x4[bones.Count];
            for (int i=0;i<bind.Length;i++) bind[i]=bones[i].worldToLocalMatrix*root.localToWorldMatrix;
            mesh.bindposes=bind;mesh.RecalculateBounds();
            var renderer=root.gameObject.AddComponent<SkinnedMeshRenderer>(); renderer.sharedMesh=mesh;renderer.sharedMaterial=skin;
            renderer.bones=bones.ToArray();renderer.rootBone=root;
            renderer.localBounds=new Bounds(new Vector3(0,-.025f,.07f),new Vector3(.22f,.2f,.26f));
            Pose(0);

            void AddOval(Vector3 centre,Vector3 radii,int boneIndex,Transform bone)
            {
                const int rings=6,sides=10;
                int first=vertices.Count;
                Matrix4x4 matrix=root.worldToLocalMatrix*bone.localToWorldMatrix;
                for(int row=0;row<=rings;row++)for(int side=0;side<=sides;side++)
                {
                    float latitude=row*Mathf.PI/rings, angle=side*Mathf.PI*2/sides;
                    Vector3 sphere=new Vector3(Mathf.Sin(latitude)*Mathf.Cos(angle),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(angle));
                    vertices.Add(matrix.MultiplyPoint3x4(centre+Vector3.Scale(sphere,radii)));
                    Vector3 normal=new Vector3(sphere.x/radii.x,sphere.y/radii.y,sphere.z/radii.z).normalized;
                    normals.Add(matrix.MultiplyVector(normal).normalized);
                    weights.Add(new BoneWeight {boneIndex0=boneIndex,weight0=1});
                }
                for(int row=0;row<rings;row++)for(int side=0;side<sides;side++)
                {
                    int a=first+row*(sides+1)+side,b=a+sides+1;
                    indices.Add(a);indices.Add(a+1);indices.Add(b);indices.Add(a+1);indices.Add(b+1);indices.Add(b);
                }
            }
        }
        public void Pose(float grip)
        {
            grip=Mathf.Clamp01(grip);
            if(grip.Equals(lastGrip))return;
            lastGrip=grip;
            for(int finger=0;finger<5;finger++)for(int joint=0;joint<3;joint++)
            {
                float bend=joint==0?12+grip*48:joint==1?9+grip*62:5+grip*42;
                float spread=joint==0?mirror*((finger-1.5f)*4)*(1-grip):0;
                if(finger==4) {bend=joint==0?20+grip*32:10+grip*35;spread=joint==0?mirror*(-52+grip*36):0;}
                fingers[finger,joint].localRotation=Quaternion.Euler(bend,spread,finger==4&&joint==0?mirror*14:0);
            }
        }
    }

    public static void CombineStatic(Transform parent,List<Mesh> owned)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();
        var filters=parent.GetComponentsInChildren<MeshFilter>();
        foreach(var filter in filters)
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null || filter.sharedMesh==null)continue;
            Material material=renderer.sharedMaterial;
            if(!groups.TryGetValue(material,out var group))groups.Add(material,group=new List<CombineInstance>());
            group.Add(new CombineInstance {mesh=filter.sharedMesh,transform=parent.worldToLocalMatrix*filter.transform.localToWorldMatrix});
        }
        foreach(var group in groups)
        {
            var mesh=new Mesh {name="Combined avatar "+group.Key.name};mesh.CombineMeshes(group.Value.ToArray());
            mesh.UploadMeshData(true);owned.Add(mesh);
            var part=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(parent,false);
            part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var filter in filters) {filter.GetComponent<MeshRenderer>().enabled=false;Object.Destroy(filter.gameObject);}
    }
}

public static class ManipulationMotion
{
    public const float PickupSeconds=.28f, PlaceSeconds=.24f;
    public static float Ease(float time)
    { float t=System.Math.Max(0,System.Math.Min(1,time));return t*t*(3-2*t); }
    public static float Lift(float time,float height)
    {float t=System.Math.Max(0,System.Math.Min(1,time));return 16*t*t*(1-t)*(1-t)*height;}
}
