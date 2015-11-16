/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class RawDepthMeshImpl
{
    #region NATIVE_FUNTIONS

    private const string CAMERA_CLIENT_DLL = "IntelCamera_MMF_client";

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void initIntelCameraClient(int device_num);
    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void copyIntelCameraPointCloud(IntPtr arrayPtr, int w, int h, int offset_i, int offset_j);
    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void endIntelCameraClient();

    #endregion // NATIVE_FUNTIONS



    #region CONSTANTS

    private const float OFFSET_Z = 0.200f; // visibility checking by Unity
    private const int WIDTH = 290;
    private const int HEIGHT = 220;
    private const int OFFSET_I = (320 - WIDTH) / 2;
    private const int OFFSET_J = (240 - HEIGHT) / 2;

    #endregion // CONSTANTS



    #region MEMBERS

    public Mesh depthMesh;

    private bool init = false;

    private float[] floatArray;
    private GCHandle floatArrayHandle;
    private IntPtr floatArrayPtr;

    #endregion // MEMBERS



    #region CONSTRUCTION

    public RawDepthMeshImpl(GameObject depthCamera, Material meshMaterial)
	{
        // initialize binary module
        initIntelCameraClient(0);

        // hieararchy in depth camera
        GameObject mMeshVisualizer = new GameObject("RawDepthMesh");
        mMeshVisualizer.transform.parent = depthCamera.transform;
        mMeshVisualizer.transform.localPosition = new Vector3(0,0,OFFSET_Z);
        mMeshVisualizer.transform.localRotation = Quaternion.identity;
        mMeshVisualizer.transform.localScale = Vector3.one;

        // initialize raw depth mesh
        InitMeshVisualizer(mMeshVisualizer, meshMaterial);

        ThreadStart();
    }

    #endregion // CONSTRUCTION



    #region PUBLIC_METHODS

    public void Update()
    {
        ThreadUpdate();

        if (init)
        {
            // get point cloud information from binary module
//            copyIntelCameraPointCloud(floatArrayPtr, WIDTH, HEIGHT, OFFSET_I, OFFSET_J); // update in the thread
            UpdateMeshVisualizer(true);
        }
    }

    public void OnDestroy()
    {
        ThreadEnd();

        //free
        floatArrayHandle.Free();

        init = false;

        // finalize binary module
        endIntelCameraClient();
    }

    #endregion // PUBLIC_METHODS



    #region PRIVATE_METHODS_FOR_MESH_VISUALIZER

    private void InitMeshVisualizer(GameObject meshVisualizer, Material meshMaterial)
    {
        // make array & get pointer
        floatArray = new float[WIDTH * HEIGHT * 3];
        floatArrayHandle = GCHandle.Alloc(floatArray, GCHandleType.Pinned);
        floatArrayPtr = floatArrayHandle.AddrOfPinnedObject();

        // add & set Mesh for visualization
        MeshFilter meshFilter = meshVisualizer.AddComponent<MeshFilter>();
        depthMesh = new Mesh();
        depthMesh.name = "rawdepth";

        // initial mesh vertices
        depthMesh.vertices = new Vector3[WIDTH * HEIGHT]; // CAUTION! Unity Mesh CANNOT handle more than 65000 vertices!
        Vector3[] vertices = depthMesh.vertices;
        for (int j = 0; j < HEIGHT; j++)
        for (int i = 0; i < WIDTH; i++)
        {
            float x =  (i - WIDTH / 2);
            float y = -(j - HEIGHT / 2);
            float z =  0.0f;

            vertices[i + j * WIDTH] = new Vector3(x, y, z);
        }
        depthMesh.vertices = vertices;

        // initial mesh triangles
        depthMesh.triangles = new int[(WIDTH - 1) * (HEIGHT - 1) * 6];
        int[] triangles = depthMesh.triangles;
        for (int j = 0; j < HEIGHT - 1; j++)
        for (int i = 0; i < WIDTH - 1; i++)
        {
            int idx = i + j * WIDTH;

            triangles[6 * (i + j * (WIDTH - 1)) + 0] = idx;
            triangles[6 * (i + j * (WIDTH - 1)) + 1] = idx + 1;
            triangles[6 * (i + j * (WIDTH - 1)) + 2] = idx + WIDTH;

            triangles[6 * (i + j * (WIDTH - 1)) + 3] = idx + 1;
            triangles[6 * (i + j * (WIDTH - 1)) + 4] = idx + 1 + WIDTH;
            triangles[6 * (i + j * (WIDTH - 1)) + 5] = idx + WIDTH;
        }
        depthMesh.triangles = triangles;

        // uvs & normals for mesh
        depthMesh.uv = new Vector2[depthMesh.vertices.Length];
        depthMesh.normals = new Vector3[depthMesh.vertices.Length];

        // recalculate normals & bounds [DEPRECATED by speed issue]
        /*
        depthMesh.Optimize();
        depthMesh.RecalculateNormals();
        depthMesh.RecalculateBounds();
        //*/

        // create MeshFilter for GameObject
        meshFilter.mesh = depthMesh;

        // set rendering properties by material
        MeshRenderer meshRenderer = meshVisualizer.AddComponent<MeshRenderer>();
        meshRenderer.material = meshMaterial;

        init = true;
    }

    private void UpdateMeshVisualizer(bool aligned)
    {
        // get mesh vertices
        depthMesh.vertices = new Vector3[WIDTH * HEIGHT];
        Vector3[] vertices = depthMesh.vertices;
        for (int j = 0; j < HEIGHT; j++)
        for (int i = 0; i < WIDTH; i++)
        {
            float x =  (floatArray[3 * (i + j * WIDTH) + 0]);
            float y = -(floatArray[3 * (i + j * WIDTH) + 1]);
            float z =  (floatArray[3 * (i + j * WIDTH) + 2]) - OFFSET_Z;

            if (z + OFFSET_Z > 0.5) x = y = z = Mathf.Infinity;

            vertices[i + j * WIDTH] = new Vector3(x, y, z);
        }
        depthMesh.vertices = vertices;

        // recalculate normals & bounds
//        depthMesh.RecalculateNormals(); // [DEPRECATED by speed issue]
    }

    #endregion // PRIVATE_METHODS_FOR_MESH_VISUALIZER



    #region MULTI_THREAD

    private Thread meshThread;
    private Mutex meshThreadMutex;

    void ThreadStart()
    {
        // Create thread
        meshThreadMutex = new Mutex(true);
        meshThread = new Thread(MeshThreadFunc);
        meshThread.Start();
    }

    void ThreadUpdate()
    {
        meshThreadMutex.ReleaseMutex();
        meshThreadMutex.WaitOne();
    }

    void ThreadEnd()
    {
        meshThread.Abort();
    }

    void MeshThreadFunc()
    {
        try
        {
            _MeshThreadFunc();
        }
        catch (System.Exception e)
        {
            if (!(e is ThreadAbortException))
            {
                Debug.LogError("Unexpected Death: " + e.ToString());
            }
        }
    }

    void _MeshThreadFunc()
    {
        while (true)
        {
            Thread.Sleep(1);
            copyIntelCameraPointCloud(floatArrayPtr, WIDTH, HEIGHT, OFFSET_I, OFFSET_J);

            meshThreadMutex.WaitOne();
            {
                // nothing to do 
            }
            meshThreadMutex.ReleaseMutex();
        }
    }

    #endregion // MULTI_THREAD
}
