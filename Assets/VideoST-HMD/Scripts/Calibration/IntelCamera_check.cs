/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class IntelCamera_check : MonoBehaviour
{
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

    HandleRawDataImpl rawData = null;

    public GameObject cam_c;
    public GameObject cam_d;
    public GameObject plane_c;
    public GameObject plane_d;

    public Material mask_material = null;

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    void Start ()
    {
        // should start earlier than submodule instances
        initIntelCameraServer(0);
        startIntelCameraUpdateThread();

        // 
        rawData = new HandleRawDataImpl(cam_d, mask_material, plane_c, plane_d);
    }
	
    void Update () 
    {
        // update submodule instances
        if( rawData != null ) rawData.Update();
    }

    void OnDestroy()
    {
        // 
        rawData.OnDestroy();

        // should end later than submodule instances
        stopIntelCameraUpdateThread();
        endIntelCameraServer();
    }

    #endregion // MONO_BEHAVIOUR



    #region PUBLIC_METHODS

    public System.IntPtr getIntelColorImagePtr() { return rawData.getColorImagePtr(); }

    #endregion // PUBLIC_METHODS
}
