using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class BrackegJob_Gen : MonoBehaviour
{
    
    
    //reference mesh Filter
    public MeshFilter Mesh;
    public  TestMeshData Meshdata;

    private Vector3[] vertices;

    [Serializable]
    public struct TestMeshData
    {
        public int setxsize, setzsize; // xsize =  amount of quads on x axis, zsize =  amount of quads on z axis
        public int xsize => setxsize;
        public int zsize => setzsize;
        public float XSpacing;
        public float ZSpacing;


        public int TriangleCount =>(xsize *zsize) * 6 ;
        public int VertexCount => (xsize +1) * (zsize +1) ;
    }

    // Update is called once per frame
    [Button]
    void Startupdate()
    {

        GenerateNewMesh Meshjob = new GenerateNewMesh(Meshdata);
        Meshjob.Schedule((Meshdata.xsize * Meshdata.zsize), 1000).Complete();

        vertices = Meshjob.GetVerts();
        Mesh.mesh = Meshjob.GetMesh();
        Meshjob.Dispose();


    }

    private void OnDrawGizmos()
    {
        if(vertices == null) return;
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(transform.position + vertices[i],0.15f);
        }
    }


    public struct GenerateNewMesh : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]private NativeArray<Vector3> Verticies; // stores vertices;
        [NativeDisableParallelForRestriction]private NativeArray<int> Triangles;
        private readonly TestMeshData meshdata;

        public GenerateNewMesh(TestMeshData data)
        {
            Verticies = new NativeArray<Vector3>(data.VertexCount,Allocator.TempJob);
            Triangles = new NativeArray<int>(data.TriangleCount,Allocator.TempJob);
            meshdata = data;
        }

        public void Execute(int index)
        {
            int y = index % meshdata.xsize;
            int x = index / meshdata.xsize;
            
            
            //have to calculate all 4 indices;
            //calculate bottom left to top right
            
            //bottom left
            int Bl = index + Mathf.FloorToInt(index / meshdata.xsize); //bottom left
            //BottomRight
            int Br = Bl + 1;
            //TopLeft
            int Tl = Br+ meshdata.xsize;
            //TopRight
            int Tr = Tl + 1;
            
            
            //xspacing = horizontal spacing between each point , increase this if using lod to remove points
            //zspacing = Vertical spacing between each point , increase this if using lod to remove points
            
            //Set vertices;

            Verticies[Bl] = new Vector3(x * meshdata.XSpacing, 0, (y) * meshdata.ZSpacing);
            Verticies[Br] = new Vector3((x) * meshdata.XSpacing, 0, (y+1) * meshdata.ZSpacing);
            Verticies[Tl] = new Vector3((x+1) * meshdata.XSpacing, 0, (y) * meshdata.ZSpacing);
            Verticies[Tr] = new Vector3((x+1) * meshdata.XSpacing, 0, (y+1) * meshdata.ZSpacing);


            //triangle index = index *6

            //triangle position =  0, 1, 2, 3 , 1 , 2 // 
            // c - - - - d
            // |         |
            // |         |
            // |         |
            // a - - - - b
            // a is bottom left and the rest of the points are calculated using the index of a
            // we are only looping through each square to calculate the triangle and other bs

            
            //starts from index 0 to resolution index
            Triangles[index * 6 + 0] = Bl;
            Triangles[index * 6 + 1] = Br;
            Triangles[index * 6 + 2] = Tl;
            Triangles[index * 6 + 3] = Br;
            Triangles[index * 6 + 4] = Tr;
            Triangles[index * 6 + 5] = Tl;

        }

        public Vector3[] GetVerts()
        {
            return Verticies.ToArray();
        }

        public void Dispose()
        {
            Verticies.Dispose();
            Triangles.Dispose();
        }

        public Mesh GetMesh()
        {
            Mesh m = new Mesh();

            m.SetVertices(Verticies);
            m.triangles = Triangles.ToArray();
            m.RecalculateBounds();
            m.RecalculateNormals();

            return m;
        }
    }
}