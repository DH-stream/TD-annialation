using System.Collections.Generic;
using UnityEngine;

namespace TDAnnihilation
{
    public static class GreenwardWorldBuilder
    {
        static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
        static readonly Color grass=new Color(.20f,.38f,.16f), earth=new Color(.38f,.25f,.13f), stone=new Color(.35f,.36f,.32f), wood=new Color(.30f,.16f,.08f), plaster=new Color(.72f,.59f,.39f), roof=new Color(.38f,.12f,.08f), leaf=new Color(.14f,.32f,.12f), gold=new Color(1f,.46f,.08f), corruption=new Color(.34f,.06f,.25f), water=new Color(.08f,.38f,.48f);

        public static GameObject Build(IReadOnlyList<Vector3> route)
        {
            var root=new GameObject("Greenward Valley World");
            Terrain(root.transform); GreenwardGroundBuilder.Build(root.transform, route); River(root.transform); Village(root.transform); Castle(root.transform); Corruption(root.transform); Boundaries(root.transform); Life(root.transform);
            return root;
        }

        static void Terrain(Transform p)
        {
            const int nx=49,nz=33; var v=new Vector3[nx*nz]; var uv=new Vector2[v.Length]; var t=new int[(nx-1)*(nz-1)*6];
            for(int z=0;z<nz;z++) for(int x=0;x<nx;x++){float wx=-48f+x*2f,wz=-32f+z*2f;int i=z*nx+x;v[i]=new Vector3(wx,GreenwardWorldLayout.HeightAt(wx,wz),wz);uv[i]=new Vector2(x/(float)(nx-1),z/(float)(nz-1));}
            int q=0; for(int z=0;z<nz-1;z++) for(int x=0;x<nx-1;x++){int i=z*nx+x;t[q++]=i;t[q++]=i+nx;t[q++]=i+1;t[q++]=i+1;t[q++]=i+nx;t[q++]=i+nx+1;}
            var go=new GameObject("Sculpted Meadow Terrain");go.transform.SetParent(p);var mf=go.AddComponent<MeshFilter>();var mr=go.AddComponent<MeshRenderer>();var mesh=new Mesh{name="Greenward Terrain"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=t;mesh.RecalculateNormals();mf.sharedMesh=mesh;mr.sharedMaterial=Mat(grass);
        }

        static void Road(Transform p,IReadOnlyList<Vector3> r){for(int i=1;i<r.Count;i++){Vector3 a=r[i-1],b=r[i],d=b-a;var s=Prim("Worn Kingdom Road",PrimitiveType.Cube,(a+b)/2+Vector3.down*.16f,new Vector3(d.magnitude,.22f,3.2f),earth,p);s.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(d.z,d.x)*Mathf.Rad2Deg,0);}}
        static void River(Transform p)
        {
            BuildRiverRibbon(p);
            for(int i=-11;i<=11;i++)
            {
                float z=-27+i*2.45f;
                Prim("Riverbank Stone",PrimitiveType.Sphere,new Vector3(-17.1f,.15f,z),new Vector3(.65f,.3f,.5f),stone,p);
                if(i%2==0) Prim("Riverbank Stone",PrimitiveType.Sphere,new Vector3(-12.9f,.15f,z+.8f),new Vector3(.5f,.24f,.42f),stone,p);
            }
            for(int i=-3;i<=3;i++)
            {
                Prim("Bridge Plank",PrimitiveType.Cube,new Vector3(-15,GreenwardWorldLayout.HeightAt(-15,4)+.32f,4+i*.62f),new Vector3(5.4f,.22f,.52f),wood,p);
            }
            for(int side=-1;side<=1;side+=2) Prim("Bridge Beam",PrimitiveType.Cube,new Vector3(-15,GreenwardWorldLayout.HeightAt(-15,4)+.08f,4+side*2.1f),new Vector3(5.7f,.32f,.3f),wood,p);
        }
        static void Village(Transform p)
        {
            var v=new GameObject("Hearthvale Village");v.transform.SetParent(p);
            Model("House_1",new Vector3(-8,0,12),1.65f,165,v.transform);
            Model("House_2",new Vector3(2,0,12),1.55f,190,v.transform);
            Model("Inn",new Vector3(11,0,10),1.45f,205,v.transform);
            Model("House_3",new Vector3(-3,0,-12),1.75f,18,v.transform);
            Model("Stable",new Vector3(10,0,-13),1.5f,-15,v.transform);
            Model("MarketStand_1",new Vector3(1,0,-7.5f),2.25f,10,v.transform);
            Model("MarketStand_2",new Vector3(5,0,-8.5f),2.1f,-12,v.transform);
            Model("Blacksmith",new Vector3(-9,0,-8),1.55f,8,v.transform);
            Model("Mill",new Vector3(16,0,14),1.55f,185,v.transform);
            Model("Well",new Vector3(5,0,7),2.1f,0,v.transform);
            Model("Cart",new Vector3(-1,0,8.5f),1.55f,32,v.transform);
            Model("Bonfire_Lit",new Vector3(10,0,4),1.5f,0,v.transform);
            for(int i=0;i<4;i++)Model(i%2==0?"Barrel":"Crate",new Vector3(-5+i*1.1f,0,-8.8f),1.4f,i*23,v.transform);
            for(int i=0;i<14;i++){float x=-7+i*1.1f;Prim("Farm Crop",PrimitiveType.Capsule,new Vector3(x,.45f,18+(i%2)),new Vector3(.18f,.45f,.18f),new Color(.55f,.62f,.12f),v.transform);}
        }
        static void House(Transform p,float x,float z,float s)
        {
            float y=GreenwardWorldLayout.HeightAt(x,z);
            Prim("Stone Foundation",PrimitiveType.Cube,new Vector3(x,y+.25f*s,z),new Vector3(5.4f*s,.5f*s,4.4f*s),stone,p);
            Prim("Warm Plaster Walls",PrimitiveType.Cube,new Vector3(x,y+1.65f*s,z),new Vector3(5*s,2.8f*s,4*s),plaster,p);
            GableRoof(p,new Vector3(x,y+3.1f*s,z),6.1f*s,5.1f*s,1.65f*s);
            for(int i=-1;i<=1;i+=2) Prim("Timber Upright",PrimitiveType.Cube,new Vector3(x+i*2.1f*s,y+1.7f*s,z-2.05f*s),new Vector3(.24f*s,3f*s,.2f*s),wood,p);
            Prim("Timber Crossbeam",PrimitiveType.Cube,new Vector3(x,y+2.35f*s,z-2.08f*s),new Vector3(4.6f*s,.22f*s,.18f*s),wood,p);
            Prim("Oak Door",PrimitiveType.Cube,new Vector3(x,y+1.15f*s,z-2.08f*s),new Vector3(1.05f*s,2.1f*s,.16f*s),wood,p);
            for(int i=-1;i<=1;i+=2)
            {
                Prim("Golden Window",PrimitiveType.Cube,new Vector3(x+i*1.55f*s,y+1.75f*s,z-2.1f*s),new Vector3(.7f*s,.85f*s,.12f*s),new Color(.92f,.55f,.18f),p);
            }
            Prim("Stone Chimney",PrimitiveType.Cube,new Vector3(x+1.7f*s,y+4f*s,z+.7f*s),new Vector3(.65f*s,2.4f*s,.65f*s),stone,p);
        }
        static void Castle(Transform p)
        {
            var c=new GameObject("Sunspire Keep");c.transform.SetParent(p);
            float y=GreenwardWorldLayout.HeightAt(38,4);
            Prim("Castle Courtyard",PrimitiveType.Cube,new Vector3(39,y-.12f,4),new Vector3(15,.45f,17),stone,c.transform);
            Prim("North Curtain Wall",PrimitiveType.Cube,new Vector3(39,y+1.5f,12),new Vector3(17,3f,1.1f),stone,c.transform);
            Prim("South Curtain Wall",PrimitiveType.Cube,new Vector3(39,y+1.5f,-4),new Vector3(17,3f,1.1f),stone,c.transform);
            Prim("East Curtain Wall",PrimitiveType.Cube,new Vector3(46.8f,y+1.5f,4),new Vector3(1.1f,3f,17),stone,c.transform);
            for(int z=-4;z<=12;z+=16) for(int x=31;x<=47;x+=16)
            {
                Prim("Round Castle Tower",PrimitiveType.Cylinder,new Vector3(x,y+2.4f,z),new Vector3(1.65f,2.4f,1.65f),stone,c.transform);
                ConeRoof(c.transform,new Vector3(x,y+4.75f,z),2.25f,1.7f);
            }
            Prim("Gatehouse Left",PrimitiveType.Cube,new Vector3(31,y+2.2f,-.2f),new Vector3(1.5f,4.4f,1.8f),stone,c.transform);
            Prim("Gatehouse Right",PrimitiveType.Cube,new Vector3(31,y+2.2f,5.6f),new Vector3(1.5f,4.4f,1.8f),stone,c.transform);
            Prim("Gatehouse Arch",PrimitiveType.Cube,new Vector3(31,y+4f,2.7f),new Vector3(1.6f,1.0f,3.2f),stone,c.transform);
            for(int i=0;i<5;i++) for(int side=-1;side<=1;side+=2)
                Prim("Battlement",PrimitiveType.Cube,new Vector3(34+i*2.5f,y+4.8f,4+side*8f),new Vector3(1.15f,1,.9f),stone,c.transform);
            for(int i=0;i<3;i++)
            {
                var b=Prim("Royal Banner",PrimitiveType.Cube,new Vector3(32.2f,y+3.8f,-1f+i*4f),new Vector3(.10f,2.2f,1.1f),new Color(.12f,.28f,.65f),c.transform);
                b.AddComponent<GreenwardAmbientMotion>().sway=5;
            }
        }
        static void Corruption(Transform p){var c=new GameObject("Blightfall Lowlands");c.transform.SetParent(p);for(int i=0;i<9;i++){float a=i*Mathf.PI*2/9;var point=new Vector2(-41+Mathf.Cos(a)*5,-10+Mathf.Sin(a)*5);if(GreenwardWorldLayout.IsRoad(point,2.8f))continue;Prim("Void Crystal",PrimitiveType.Cylinder,new Vector3(point.x,1,point.y),new Vector3(.45f,1.8f,.45f),corruption,c.transform);}Prim("Demon Portal",PrimitiveType.Cylinder,new Vector3(-43,2.8f,-10),new Vector3(4,.5f,4),corruption,c.transform).transform.rotation=Quaternion.Euler(90,0,0);for(int i=0;i<7;i++)Prim("Ruined Monolith",PrimitiveType.Cube,new Vector3(-35+i%3*3,1.3f,-19+i/3*3),new Vector3(1.2f,2.6f,1.2f),stone,c.transform);}
        static void Boundaries(Transform p){var rng=new System.Random(731);for(int i=0;i<64;i++){float x=-46+i*92f/63f;Tree(p,x,(i%2==0?-29:29),rng);if(i%3==0)Tree(p,x,(i%2==0?26:-26),rng);}for(int i=0;i<22;i++){float z=-27+i*54f/21f;Tree(p,-46,z,rng);if(i%2==0)Tree(p,47,z,rng);}for(int i=0;i<18;i++){float x=-44+(float)rng.NextDouble()*88,z=-27+(float)rng.NextDouble()*54;if(Mathf.Abs(z)<15)continue;Prim("Mossy Boulder",PrimitiveType.Sphere,new Vector3(x,GreenwardWorldLayout.HeightAt(x,z)+.5f,z),Vector3.one*(.6f+(float)rng.NextDouble()),stone,p);}}
        static void Tree(Transform p,float x,float z,System.Random rng){if(GreenwardWorldLayout.IsRoad(new Vector2(x,z),4.2f))return;float y=GreenwardWorldLayout.HeightAt(x,z),s=.8f+(float)rng.NextDouble()*.7f;Prim("Ancient Tree",PrimitiveType.Cylinder,new Vector3(x,y+1.5f*s,z),new Vector3(.45f*s,1.5f*s,.45f*s),wood,p);Prim("Broadleaf Crown",PrimitiveType.Sphere,new Vector3(x,y+4.1f*s,z),new Vector3(2.1f*s,1.6f*s,1.8f*s),leaf,p);Prim("Leaf Cluster",PrimitiveType.Sphere,new Vector3(x-1.2f*s,y+3.7f*s,z+.4f*s),new Vector3(1.3f*s,1.15f*s,1.2f*s),new Color(.18f,.39f,.13f),p);Prim("Leaf Cluster",PrimitiveType.Sphere,new Vector3(x+1.1f*s,y+4.3f*s,z-.3f*s),new Vector3(1.25f*s,1.1f*s,1.25f*s),new Color(.12f,.29f,.10f),p);}
        static void Atmosphere(){RenderSettings.skybox=null;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.36f,.46f,.45f);RenderSettings.fogStartDistance=48f;RenderSettings.fogEndDistance=105f;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.42f,.52f,.56f);RenderSettings.ambientEquatorColor=new Color(.28f,.34f,.30f);RenderSettings.ambientGroundColor=new Color(.18f,.16f,.12f);}
        static void BuildRiverRibbon(Transform p)
        {
            const int count=25;
            var vertices=new Vector3[count*2];
            var uv=new Vector2[count*2];
            var triangles=new int[(count-1)*6];
            for(int i=0;i<count;i++)
            {
                float t=i/(float)(count-1);
                float z=Mathf.Lerp(-33f,29f,t);
                float center=-15f+Mathf.Sin(t*8.4f)*.65f+Mathf.Sin(t*19f)*.18f;
                float half=1.65f+Mathf.Sin(t*11f)*.28f;
                vertices[i*2]=new Vector3(center-half,-.28f,z);
                vertices[i*2+1]=new Vector3(center+half,-.28f,z);
                uv[i*2]=new Vector2(0,t*10f);uv[i*2+1]=new Vector2(1,t*10f);
                if(i==count-1)continue;
                int q=i*6,v=i*2;
                triangles[q]=v;triangles[q+1]=v+2;triangles[q+2]=v+1;
                triangles[q+3]=v+1;triangles[q+4]=v+2;triangles[q+5]=v+3;
            }
            var mesh=new Mesh{name="Greenward River Ribbon"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();
            var river=new GameObject("Meandering Greenward River");river.transform.SetParent(p);river.AddComponent<MeshFilter>().sharedMesh=mesh;river.AddComponent<MeshRenderer>().sharedMaterial=GreenwardMaterialLibrary.Water;
        }
        static void GableRoof(Transform p,Vector3 center,float width,float depth,float rise)
        {
            float hx=width*.5f,hz=depth*.5f;
            Vector3[] v={
                new Vector3(-hx,0,-hz),new Vector3(hx,0,-hz),new Vector3(-hx,0,hz),new Vector3(hx,0,hz),
                new Vector3(0,rise,-hz),new Vector3(0,rise,hz)};
            int[] t={0,4,1,2,3,5,0,2,5,0,5,4,1,4,5,1,5,3,0,1,3,0,3,2};
            var mesh=new Mesh{name="Chunky Gable Roof"};mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();
            var roofObject=new GameObject("Deep Gabled Shingle Roof");roofObject.transform.SetParent(p);roofObject.transform.position=center;roofObject.AddComponent<MeshFilter>().sharedMesh=mesh;roofObject.AddComponent<MeshRenderer>().sharedMaterial=GreenwardMaterialLibrary.Roof;
        }
        static void ConeRoof(Transform p,Vector3 center,float radius,float height)
        {
            const int sides=12;
            var vertices=new Vector3[sides+1];
            var triangles=new int[sides*3];
            vertices[0]=new Vector3(0,height,0);
            for(int i=0;i<sides;i++)
            {
                float angle=i*Mathf.PI*2f/sides;
                vertices[i+1]=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%sides+1;
            }
            var mesh=new Mesh{name="Faceted Tower Roof"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();
            var roofObject=new GameObject("Steep Copper Shingle Tower Roof");roofObject.transform.SetParent(p);roofObject.transform.position=center;roofObject.AddComponent<MeshFilter>().sharedMesh=mesh;roofObject.AddComponent<MeshRenderer>().sharedMaterial=GreenwardMaterialLibrary.Roof;
        }
        static void Life(Transform p){for(int i=0;i<7;i++){float x=-5+i*3,z=-8+(i%3)*4;while(GreenwardWorldLayout.IsRoad(new Vector2(x,z),3.2f))z-=1f;var npc=Prim(i<2?"Greenward Guard":"Village Inhabitant",PrimitiveType.Capsule,new Vector3(x,GreenwardWorldLayout.HeightAt(x,z)+1,z),new Vector3(.55f,1,.55f),i<2?new Color(.16f,.28f,.55f):new Color(.55f,.28f,.12f),p);npc.transform.rotation=Quaternion.Euler(0,i*47,0);}}
        static Material Mat(Color c){return GreenwardMaterialLibrary.ForColor(c);}
        static GameObject Model(string assetName,Vector3 position,float scale,float yaw,Transform parent)
        {
            GameObject prefab=Resources.Load<GameObject>("TDAnnihilation/Environment/MedievalVillage/"+assetName);
            if(prefab==null)return null;
            position.y=GreenwardWorldLayout.HeightAt(position.x,position.z);
            GameObject instance=Object.Instantiate(prefab,position,Quaternion.Euler(0,yaw,0),parent);
            instance.name="Authored "+assetName.Replace('_',' ');
            instance.transform.localScale=Vector3.one*scale;
            Renderer[] renderers=instance.GetComponentsInChildren<Renderer>();
            if(renderers.Length>0)
            {
                Bounds bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
                BoxCollider collider=instance.AddComponent<BoxCollider>();
                collider.center=instance.transform.InverseTransformPoint(bounds.center);
                Vector3 lossy=instance.transform.lossyScale;
                collider.size=new Vector3(bounds.size.x/lossy.x,bounds.size.y/lossy.y,bounds.size.z/lossy.z);
            }
            return instance;
        }
        static GameObject Prim(string n,PrimitiveType t,Vector3 pos,Vector3 scale,Color c,Transform p){var g=GameObject.CreatePrimitive(t);g.name=n;g.transform.SetParent(p);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=Mat(c);return g;}
    }
}
