/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class HandleRawDataImpl
{
    #region NATIVE_FUNTIONS

    private const string CAMERA_CLIENT_DLL = "IntelCamera_MMF_client";

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void initIntelCameraClient(int device_num);
    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void endIntelCameraClient();

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void copyIntelCameraPointCloud(IntPtr arrayPtr, int w, int h, int offset_i, int offset_j);

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void getIntelCameraColorImage(out int w, out int h);
    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void copyIntelCameraColorImage(IntPtr arrayPtr);

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void getIntelCameraInfraImage(out int w, out int h);
    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void copyIntelCameraInfraImage(IntPtr arrayPtr);

    #endregion // NATIVE_FUNTIONS



    #region CONSTANTS

    private const float DIST_THRESHOLD = 1.0f; // [m]
    private const float OFFSET_Z = 0.200f; // visibility checking by Unity
    private const int WIDTH = 290;
    private const int HEIGHT = 220;
    private const int OFFSET_I = (320-WIDTH) / 2;
    private const int OFFSET_J = (240-HEIGHT)/ 2;

    #endregion // CONSTANTS



    #region MEMBERS

    public Mesh depthMesh;

    private bool init = false;

    GameObject mMeshVisualizer;

    private float[] floatArray;
    private GCHandle floatArrayHandle;
    private IntPtr floatArrayPtr;

    private Texture2D mTextureColor;
    private Color32[] pixelsColor; 
    private GCHandle pixelsHandleColor;
    private IntPtr pixelsPointerColor = System.IntPtr.Zero;

    private Texture2D mTextureInfra;
    private Color32[] pixelsInfra;
    private GCHandle pixelsHandleInfra;
    private IntPtr pixelsPointerInfra = System.IntPtr.Zero;

    #endregion // MEMBERS



    #region CONSTRUCTION

    public HandleRawDataImpl(GameObject depthCamera, Material meshMaterial, GameObject colorPlane, GameObject depthPlane)
	{
        initIntelCameraClient(0);

        // hieararchy in depth camera
        mMeshVisualizer = new GameObject("RawDepthMesh");
        mMeshVisualizer.transform.parent = depthCamera.transform;
        mMeshVisualizer.transform.localPosition = new Vector3(0, 0, OFFSET_Z);
        mMeshVisualizer.transform.localRotation = Quaternion.identity;
        mMeshVisualizer.transform.localScale = Vector3.one;

        // initialize 3D mesh visualizer
        InitMeshVisualizer(mMeshVisualizer, meshMaterial);

        // initialize texture for camera image
        InitColorTexture(colorPlane);
        InitDepthTexture(depthPlane);

        // start the image update thread
        RawDataThreadStart();
    }

    #endregion // CONSTRUCTION



    #region PUBLIC_METHODS

    public void Update()
    {
        // synchronize thread
        ThreadUpdate();

        // [DEPRECATED] update in the thread
        /*
        copyIntelCameraColorImage(pixelsPointerColor);
        copyIntelCameraConfiImage(pixelsPointerInfra);
        copyIntelCameraPointCloud(floatArrayPtr, WIDTH, HEIGHT, OFFSET_I, OFFSET_J);
        //*/

        if (init)
        {
            // get point cloud information from binary module
            UpdateMeshVisualizer(true);
        }

        // update texture for renderer
        mTextureColor.SetPixels32(pixelsColor);
        mTextureColor.Apply();
        mTextureInfra.SetPixels32(pixelsInfra);
        mTextureInfra.Apply();
    }

    public void OnDestroy()
    {
        // end the image update thread
        ThreadEnd();

        // free
        pixelsHandleColor.Free();
        pixelsHandleInfra.Free();
        floatArrayHandle.Free();

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

        // initial mesh vertices...
        depthMesh.vertices = new Vector3[WIDTH * HEIGHT];
        Vector3[] vertices = depthMesh.vertices;
        for (int j = 0; j < HEIGHT; j++)
        for (int i = 0; i < WIDTH; i++)
        {
            float x = (i - WIDTH / 2);
            float y = -(j - HEIGHT / 2);
            float z = 0.0f;

            vertices[i + j * WIDTH] = new Vector3(x, y, z);
        }
        depthMesh.vertices = vertices;

        // initial mesh triangles...
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

        // add & set Mesh for visualization
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
        for (int i = 0; i < WIDTH ; i++)
        {
            float x =  (floatArray[3 * (i + j * WIDTH) + 0]);
            float y = -(floatArray[3 * (i + j * WIDTH) + 1]);
            float z =  (floatArray[3 * (i + j * WIDTH) + 2]) - OFFSET_Z;

            if (z + OFFSET_Z > DIST_THRESHOLD) x = y = z = Mathf.Infinity;

            vertices[i + j * WIDTH] = new Vector3(x, y, z);
        }
        depthMesh.vertices = vertices;

        // recalculate normals & bounds
//        depthMesh.RecalculateNormals(); // [DEPRECATED]
    }

    #endregion // PRIVATE_METHODS_FOR_MESH_VISUALIZER



    #region PRIVATE_METHODS_FOR_TEXTURE

    void InitColorTexture(GameObject colorPlane)
    {
        // color image
        int color_w, color_h;
        getIntelCameraColorImage(out color_w, out color_h);

        // data handling
        mTextureColor = new Texture2D(color_w, color_h, TextureFormat.RGB24, false);
        pixelsColor = mTextureColor.GetPixels32();
        pixelsHandleColor = GCHandle.Alloc(pixelsColor, GCHandleType.Pinned);
        pixelsPointerColor = pixelsHandleColor.AddrOfPinnedObject();

        // game object properties
        colorPlane.GetComponent<Renderer>().material.mainTexture = mTextureColor;
    }

    void InitDepthTexture(GameObject infraPlane)
    {
        // confidence (= infrared) image
        int confi_w, confi_h;
        getIntelCameraInfraImage(out confi_w, out confi_h);

        // data handling
        mTextureInfra = new Texture2D(confi_w, confi_h, TextureFormat.RGB24, false);
        pixelsInfra = mTextureInfra.GetPixels32();
        pixelsHandleInfra = GCHandle.Alloc(pixelsInfra, GCHandleType.Pinned);
        pixelsPointerInfra = pixelsHandleInfra.AddrOfPinnedObject();

        // game object properties
        infraPlane.GetComponent<Renderer>().material.mainTexture = mTextureInfra;
    }

    #endregion // PRIVATE_METHODS_FOR_TEXTURE


    
    #region MULTI_THREAD_MESH

    private Thread clientThread;
    private Mutex clientThreadMutex;

    void RawDataThreadStart()
    {
        // Create thread
        clientThreadMutex = new Mutex(true);
        clientThread = new Thread(RawDataThreadFunc);
        clientThread.Start();
    }

    void ThreadUpdate()
    {
        clientThreadMutex.ReleaseMutex();
        clientThreadMutex.WaitOne();
    }

    void ThreadEnd()
    {
        clientThread.Abort();
    }

    void RawDataThreadFunc()
    {
        try
        {
            _RawDataThreadFunc();
        }
        catch (System.Exception e)
        {
            if (!(e is ThreadAbortException))
            {
                Debug.LogError("Unexpected Death: " + e.ToString());
            }
        }
    }

    void _RawDataThreadFunc()
    {
        while (true)
        {
            Thread.Sleep(1);
            copyIntelCameraColorImage(pixelsPointerColor);
            copyIntelCameraInfraImage(pixelsPointerInfra);
            copyIntelCameraPointCloud(floatArrayPtr, WIDTH, HEIGHT, OFFSET_I, OFFSET_J);

            clientThreadMutex.WaitOne();
            {
                // nothing to do 
            }
            clientThreadMutex.ReleaseMutex();
        }
    }

    #endregion // MULTI_THREAD



    #region PUBLIC_METHODS

    public System.IntPtr getColorImagePtr() { return pixelsPointerColor; }

    #endregion // PUBLIC_METHODS
}
