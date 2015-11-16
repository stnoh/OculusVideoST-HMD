/*==============================================================================
This is binary module (DLL made by C++) wrapper for Unity C#.
"Intel Hand Skeletal Tracking Library" will be availiable here:
http://software.intel.com/en-us/articles/the-intel-skeletal-hand-tracking-library-experimental-release

This wrapper is written by Seung-Tak Noh (seungtak.noh [at] gmail.com), 2013-2015
==============================================================================*/
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public interface IBinaryWrapper
{
    // for Intel HSKL
    bool  InitHSKL(float hand_w, float hand_h);
    void  EndHSKL();
    void  SetHSKLHandSize(float hand_w, float hand_h);

    float GetHSKLHandPoses(float[] bone_poses);
    void  GetHSKLBoneMesh(int id, int[] tris, float[] verts);
}



public class BinaryNativeWrapper : IBinaryWrapper
{
    #region PUBLIC_METHODS

    public bool InitHSKL(float hand_w, float hand_h)
    {
        try
        {
            initHSKLTracking(0, 443.4065f * 0.5f, 442.1876f * 0.5f);
            setHSKLMeasurements(hand_w, hand_h);
            startHSKLTracking_thread();
            return true;
        }
        catch(Exception e)
        {
            Debug.LogError(e);

            String msg = "You need to get Intel Hand Skeletal Library from the official website,\n"
                +"build DLL using RGBD-PCSDK, copy hskl.dll and HSKL_MMF.dll into Plugins,\n"
                +"and run the program again.";

            Debug.LogWarning(msg);
            return false;
        }
    }

    public void EndHSKL()
    {
        stopHSKLTracking_thread();
        endHSKLTracking();
    }

    public void SetHSKLHandSize(float hand_w, float hand_h)
    {
        setHSKLMeasurements(hand_w, hand_h);
    }

    public float GetHSKLHandPoses(float[] bone_poses)
    {
        getHSKLBonePoses(bone_poses);
        return getHSKLTrackingError();
    }

    public void GetHSKLBoneMesh(int id, int[] tris, float[] verts)
    {
        getHSKLBoneMesh(id, tris, verts);
    }

    #endregion // PUBLIC_METHODS



    #region NATIVE_FUNTIONS

    private const string HSKLwithCAPE_DLL = "HSKL_MMF";

    [DllImport(HSKLwithCAPE_DLL)]
    private static extern void initHSKLTracking(int cam_id, float fx, float fy);
    [DllImport(HSKLwithCAPE_DLL)]
    private static extern void endHSKLTracking();

    [DllImport(HSKLwithCAPE_DLL)]
    private static extern float runHSKLTracking();
    [DllImport(HSKLwithCAPE_DLL)]
    private static extern void startHSKLTracking_thread();
    [DllImport(HSKLwithCAPE_DLL)]
    private static extern void stopHSKLTracking_thread();

    [DllImport(HSKLwithCAPE_DLL)]
    private static extern void setHSKLModelType(int type);
    [DllImport(HSKLwithCAPE_DLL)]
    private static extern void setHSKLMeasurements(float hand_width, float hand_height);

    [DllImport(HSKLwithCAPE_DLL)]
    private static extern float getHSKLTrackingError();
    [DllImport(HSKLwithCAPE_DLL)]
    private static extern void getHSKLBoneMesh(int id, int[] bone_tris, float[] bone_verts);
    [DllImport(HSKLwithCAPE_DLL)]
    private static extern void getHSKLBonePoses(float[] bone_poses);

    #endregion // NATIVE_FUNTIONS
}



public static class BinaryWrapper
{
    private static IBinaryWrapper sWrapper = null;

    public static IBinaryWrapper Instance
    {
        get
        {
            if (sWrapper == null)
            {
                Create();
            }

            return sWrapper;
        }
    }

    public static void Create()
    {
        sWrapper = new BinaryNativeWrapper();
    }

    public static void SetImplementation(IBinaryWrapper implementation)
    {
        sWrapper = implementation;
    }
}
