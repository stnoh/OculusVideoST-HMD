/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class RawPointCloudImpl
{
    #region NATIVE_FUNTIONS

    private const string CAMERA_CLIENT_DLL = "IntelCamera_MMF_client";

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void initIntelCameraClient(int device_num);

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void getIntelCameraInfraImage(out int w, out int h);
    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void copyIntelCameraPointCloud(IntPtr arrayPtr, int w, int h, int offset_i, int offset_j);
    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void copyIntelCameraPointUVmap(IntPtr arrayPtr, int w, int h, int offset_i, int offset_j);

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void getIntelCameraColorImage(out int w, out int h);
    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void copyIntelCameraColorImage(IntPtr arrayPtr);

    [DllImport(CAMERA_CLIENT_DLL)]
    private static extern void endIntelCameraClient();

    #endregion // NATIVE_FUNTIONS



    #region CONSTANTS

    private const float OFFSET_Z = 0.20f;
    private const float MAX_DISTANCE = 0.85f; // [m]

    #endregion // CONSTANTS



    #region MEMBERS

    public GameObject mPointCloudVisualizer;
    public ParticleSystem pointCloud;
    public ParticleSystem.Particle[] points;

    readonly GameObject _root; // [FIX ME LATER]

    private bool init = false;

    private int DEPTH_WIDTH;
    private int DEPTH_HEIGHT;

    private float[] floatArray;
    private GCHandle floatArrayHandle;
    private IntPtr floatArrayPtr;

    private float[] float_uvArray;
    private GCHandle float_uvArrayHandle;
    private IntPtr float_uvArrayPtr;

    private int COLOR_WIDTH;
    private int COLOR_HEIGHT;

    private Texture2D mTextureColor;
    private Color32[] pixelsColor;
    private GCHandle pixelsHandleColor;
    private IntPtr pixelsPointerColor = System.IntPtr.Zero;

    #endregion // MEMBERS



    #region CONSTRUCTION

    public RawPointCloudImpl(GameObject depthCamera)
	{
        _root = depthCamera; // [FIX ME LATER]

        initIntelCameraClient(0);

        // hieararchy in depth camera
        mPointCloudVisualizer = new GameObject("RawPointCloud");
        mPointCloudVisualizer.transform.parent = depthCamera.transform;
        mPointCloudVisualizer.transform.localPosition = new Vector3(0, 0, OFFSET_Z);
        mPointCloudVisualizer.transform.localRotation = Quaternion.identity;
        mPointCloudVisualizer.transform.localScale = Vector3.one;

        // initialize texture area
        InitColorTexture();

        // initialize raw depth mesh
        InitPointCloudVisualizer();

        ThreadStart();
    }

    #endregion // CONSTRUCTION



    #region PUBLIC_METHODS

    public void Update()
    {
        if (init)
        {
            ThreadUpdate();

            // update texture for renderer
            mTextureColor.SetPixels32(pixelsColor);
            mTextureColor.Apply();

            // get point cloud information from binary module
            UpdatePointCloudVisualizer(true);
        }
    }

    public void OnDestroy()
    {
        ThreadEnd();

        // free
        floatArrayHandle.Free();
        float_uvArrayHandle.Free();
        pixelsHandleColor.Free();

        init = false;

        // finalize binary module
        endIntelCameraClient();
    }

    #endregion // PUBLIC_METHODS



    #region PRIVATE_METHODS_FOR_POINTCLOUD_VISUALIZER

    private void InitPointCloudVisualizer()
    {
        // get the depth image size
        getIntelCameraInfraImage(out DEPTH_WIDTH, out DEPTH_HEIGHT);

        // make array & get pointer for pos3D
        floatArray = new float[DEPTH_WIDTH * DEPTH_HEIGHT * 3];
        floatArrayHandle = GCHandle.Alloc(floatArray, GCHandleType.Pinned);
        floatArrayPtr = floatArrayHandle.AddrOfPinnedObject();

        // make array & get pointer for UVs
        float_uvArray = new float[DEPTH_WIDTH * DEPTH_HEIGHT * 2];
        float_uvArrayHandle = GCHandle.Alloc(float_uvArray, GCHandleType.Pinned);
        float_uvArrayPtr = float_uvArrayHandle.AddrOfPinnedObject();

        // add & set Mesh for visualization
        if (mPointCloudVisualizer.GetComponent<ParticleSystem>() == null)
        {
            mPointCloudVisualizer.AddComponent<ParticleSystem>();
        }

        // set the ParticleSystem in UnityEngine
        pointCloud = mPointCloudVisualizer.GetComponent<ParticleSystem>();
        pointCloud.enableEmission = false;
        pointCloud.maxParticles = DEPTH_WIDTH * DEPTH_HEIGHT;
        pointCloud.loop = false;
        pointCloud.playOnAwake = true;
        pointCloud.simulationSpace = ParticleSystemSimulationSpace.World; // [FIX ME LATER] --- Local, if bug is fixed

        // renderer setting
        Renderer renderer = pointCloud.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // allocate particle memory
        points = new ParticleSystem.Particle[DEPTH_WIDTH * DEPTH_HEIGHT];

        // set rendering properties
        init = true;
    }


    ////////////////////////////////////////////////////////////////////////////////
    // FIX ME LATER : THEORETICALLY UNNECESSARY
    // but, Unity 5's ParticleSystem has a small issue, so I calculate it directly.
    ////////////////////////////////////////////////////////////////////////////////
    private void UpdatePointCloudVisualizer(bool aligned)
    {
        Matrix4x4 convert = _root.transform.localToWorldMatrix;

        ////////////////////////////////////////
        // get the positions of point
        ////////////////////////////////////////
        int count = 0;
        for (int j = 0; j < DEPTH_HEIGHT; j++)
        for (int i = 0; i < DEPTH_WIDTH; i++)
        {
            ParticleSystem.Particle particle = new ParticleSystem.Particle();

            float x = (floatArray[3 * (i + j * DEPTH_WIDTH) + 0]);
            float y = -(floatArray[3 * (i + j * DEPTH_WIDTH) + 1]);
            float z = (floatArray[3 * (i + j * DEPTH_WIDTH) + 2]);

            if (z > MAX_DISTANCE) continue;

            const float scale_up = 0.01f;
            particle.size = 0.0025f + scale_up * z;
            particle.position = convert.MultiplyPoint(new Vector3(x, y, z));

            // get the color from color image texture
            float u = 1.0f - float_uvArray[2 * (i + j * DEPTH_WIDTH) + 0] / COLOR_WIDTH;
            float v = float_uvArray[2 * (i + j * DEPTH_WIDTH) + 1] / COLOR_HEIGHT;
            particle.color = mTextureColor.GetPixelBilinear(u,v);

            points[count] = particle;
            count++;
        }

        // assign point cloud to Unity.ParticleSystem
        pointCloud.SetParticles(points, count);
    }

    #endregion // PRIVATE_METHODS_FOR_POINTCLOUD_VISUALIZER



    #region PRIVATE_METHODS_FOR_TEXTURE

    void InitColorTexture()
    {
        // color image
        getIntelCameraColorImage(out COLOR_WIDTH, out COLOR_HEIGHT);

        // data handling
        mTextureColor = new Texture2D(COLOR_WIDTH, COLOR_HEIGHT, TextureFormat.RGB24, false);
        pixelsColor = mTextureColor.GetPixels32();
        pixelsHandleColor = GCHandle.Alloc(pixelsColor, GCHandleType.Pinned);
        pixelsPointerColor = pixelsHandleColor.AddrOfPinnedObject();
    }

    #endregion // PRIVATE_METHODS_FOR_TEXTURE



    #region MULTI_THREAD

    private Thread CloudThread;
    private Mutex CloudThreadMutex;

    void ThreadStart()
    {
        // Create thread
        CloudThreadMutex = new Mutex(true);
        CloudThread = new Thread(CloudThreadFunc);
        CloudThread.Start();
    }

    void ThreadUpdate()
    {
        CloudThreadMutex.ReleaseMutex();
        CloudThreadMutex.WaitOne();
    }

    void ThreadEnd()
    {
        CloudThread.Abort();
    }

    void CloudThreadFunc()
    {
        try
        {
            _CloudThreadFunc();
        }
        catch (System.Exception e)
        {
            if (!(e is ThreadAbortException))
            {
                Debug.LogError("Unexpected Death: " + e.ToString());
            }
        }
    }

    void _CloudThreadFunc()
    {
        while (true)
        {
            Thread.Sleep(1);
            copyIntelCameraPointCloud(floatArrayPtr, DEPTH_WIDTH, DEPTH_HEIGHT, 0, 0);
            copyIntelCameraPointUVmap(float_uvArrayPtr, DEPTH_WIDTH, DEPTH_HEIGHT, 0, 0);
            copyIntelCameraColorImage(pixelsPointerColor);

            CloudThreadMutex.WaitOne();
            {
                // nothing to do 
            }
            CloudThreadMutex.ReleaseMutex();
        }
    }

    #endregion // MULTI_THREAD
}
