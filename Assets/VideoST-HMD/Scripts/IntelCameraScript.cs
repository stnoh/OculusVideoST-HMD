/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class IntelCameraScript : MonoBehaviour
{
    #region PUBLIC_VARIABLES

    public bool enable_mask = true;
    public Material mask_material = null;

    public bool enable_HSKL = false;
    public float hand_w = 0.08f;
    public float hand_h = 0.19f;
    public Material skin_material = null;
    public Material fail_material = null;

    #endregion // PUBLIC_VARIABLES



    #region NATIVE_FUNTIONS

    private const string CAMERA_SERVER_DLL = "IntelCamera_MMF_server";

    [DllImport(CAMERA_SERVER_DLL)]
    private static extern int  initIntelCameraServer(int device_num);
    [DllImport(CAMERA_SERVER_DLL)]
    private static extern void startIntelCameraUpdateThread();
    [DllImport(CAMERA_SERVER_DLL)]
    private static extern void stopIntelCameraUpdateThread();
    [DllImport(CAMERA_SERVER_DLL)]
    private static extern void endIntelCameraServer();

    #endregion // NATIVE_FUNTIONS



    #region PUBLIC_MEMBERS

    public RawDepthMeshImpl mRawDepthMeshImpl = null;
    public HSKLTrackerImpl  mHSKLTrackerImpl = null;

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    void Start ()
    {
        // should start earlier than instances
        initIntelCameraServer(0);
        startIntelCameraUpdateThread();

        // instantiate classes
        if (enable_mask) mRawDepthMeshImpl = new RawDepthMeshImpl(this.gameObject, mask_material);
        if (enable_HSKL)
        {
            // initialize HSKL/CAPE binary module
            if(BinaryWrapper.Instance.InitHSKL(hand_w, hand_h))
                mHSKLTrackerImpl = new HSKLTrackerImpl(this.gameObject, skin_material, fail_material);
        }
    }
	
    void Update () 
    {
        if (mRawDepthMeshImpl != null) mRawDepthMeshImpl.Update();
        if (mHSKLTrackerImpl != null) mHSKLTrackerImpl.Update();
    }

    void OnDestroy()
    {
        // destroy instances
        if (mRawDepthMeshImpl != null) mRawDepthMeshImpl.OnDestroy();
        if (mHSKLTrackerImpl != null)
        {
            mHSKLTrackerImpl.OnDestroy();

            // finalize HSKL/CAPE binary module
            BinaryWrapper.Instance.EndHSKL();
        }

        // should end later than instances
        stopIntelCameraUpdateThread();
        endIntelCameraServer();
    }

    #endregion // MONO_BEHAVIOUR
}
