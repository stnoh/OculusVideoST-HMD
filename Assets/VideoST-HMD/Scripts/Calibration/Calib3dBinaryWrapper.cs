/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public enum FLIP_CODE { NONE, UPDOWN, LEFTRIGHT, BOTH }

public interface ICalib3dWrapper
{
    void Start(int block_x, int block_y);
    void End();

    // checker tool
    void SetImageSize(int id, int width, int height);
    void ConvertBinaryImage(int id, IntPtr ptr, FLIP_CODE flipCode, int threshold);
    Texture2D GetTexture(int id);
    bool FindChecker(int id, float[] pts_2D);

    // calib3d tool
    int PushPoints(int id, float[] pts_2D, float[] pts_3D);
    void ResetPoints(int id);
    double Calibrate(int id, int flag);
    bool GetCameraPose(int id, float[] pts_2D, float[] pts_3D, float[] rot3x3, float[] tr3x1);

    void SetCameraMatrix_temp(int id, float[] cam_mat);

    // simpleXML tool
    Matrix4x4 LoadXML(string file);
    void SaveXML(string file, Transform transform);
}


public class Calib3dNativeWrapper : ICalib3dWrapper
{
    #region PRIVATE_MEMBERS

    private Texture2D[] mTexture = new Texture2D[2];

    private Color32[][] pixelStore = new Color32[2][];
    private GCHandle[] pixelHandle = new GCHandle[2];
    private IntPtr[] pixelPtr = new IntPtr[2];

    #endregion // PRIVATE_MEMBERS



    #region PUBLIC_METHODS

    ////////////////////////////////////////////////////////////
    // initialize all binary modules
    ////////////////////////////////////////////////////////////
    public void Start(int block_x, int block_y)
    {
        initCheckerTool(block_x, block_y);
        initCalib3DTool(block_x, block_y);
    }

    public void End()
    {
        endCheckerTool();
        endCalib3DTool();

        // release pinned objects
        for (int id = 0; id < 2; id++)
        {
            pixelHandle[id].Free();
        }
    }


    ////////////////////////////////////////////////////////////
    // checker tool
    ////////////////////////////////////////////////////////////
    public void SetImageSize(int id, int width, int height)
    {
        setImageSize(id, width, height);

        // set texture for chessboard image
        mTexture[id] = new Texture2D(width, height);

        // pinned object without GC for pixel store
        pixelStore[id] = mTexture[id].GetPixels32();
        pixelHandle[id] = GCHandle.Alloc(pixelStore[id], GCHandleType.Pinned);
        pixelPtr[id] = pixelHandle[id].AddrOfPinnedObject();
    }

    public void ConvertBinaryImage(int id, IntPtr ptr, FLIP_CODE flipCode, int threshold)
    {
        copyRawImage(id, ptr);
        switch(flipCode){
        case FLIP_CODE.UPDOWN: flipRawImage(id, 0); break;
        case FLIP_CODE.LEFTRIGHT: flipRawImage(id, 1); break;
        case FLIP_CODE.BOTH: flipRawImage(id, -1); break;
        default: break;
        }
        convertRawToBinary(id, threshold);

        // get chessboard image
        copyProcessedImage(id, pixelPtr[id]);
        mTexture[id].SetPixels32(pixelStore[id]);
        mTexture[id].Apply();
    }

    public Texture2D GetTexture(int id) 
    {
        return mTexture[id];
    }

    public bool FindChecker(int id, float[] pts_2D)
    {
        bool found = findChessCorners(id, pts_2D);

        // get chessboard image
        copyProcessedImage(id, pixelPtr[id]);
        mTexture[id].SetPixels32(pixelStore[id]);
        mTexture[id].Apply();

        return found;
    }


    ////////////////////////////////////////////////////////////
    // calib3D tool
    ////////////////////////////////////////////////////////////
    public int PushPoints(int id, float[] pts_2D, float[] pts_3D)
    {
        return pushImageObjectPairPoints(id, pts_2D, pts_3D);
    }

    public void ResetPoints(int id)
    {
        clearImageObjectPairPoints(id);
    }

    public double Calibrate(int id, int flag)
    {
        int width  = mTexture[id].width;
        int height = mTexture[id].height;
        return calibrateCamera(id, width, height, flag);
    }

    public bool GetCameraPose(int id, float[] pts_2D, float[] pts_3D,
        float[] rot3x3, float[] tr3x1)
    {
        return solvePnP(id, pts_2D, pts_3D, rot3x3, tr3x1);
    }


    public void SetCameraMatrix_temp(int id, float[] cam_mat)
    {
        float[] dist_coeffs = new float[8]; // undistorted image
        setIntrinsicParams(id, cam_mat, dist_coeffs);
    }


    ////////////////////////////////////////////////////////////
    // simpleXML tool
    ////////////////////////////////////////////////////////////
    public Matrix4x4 LoadXML(string file)
    {
        float[] rot = new float[4];
        float[] tr  = new float[3];
        loadXML(file, rot, tr);

        Quaternion q = new Quaternion(rot[0], rot[1], rot[2], rot[3]);
        Vector3 pos = new Vector3(tr[0], tr[1], tr[2]);

        return Matrix4x4.TRS(pos, q, Vector3.one);
    }

    public void SaveXML(string file, Transform transform)
    {
        Quaternion q = transform.rotation;
        float[] rot = { q.x, q.y, q.z, q.w };

        Vector3 t = transform.position;
        float[] tr = { t.x, t.y, t.z };

        saveXML(file, rot, tr);
    }

    #endregion // PUBLIC_METHODS



    #region NATIVE_FUNTIONS_CHESSBOARD

    private const string CHECKER_DLL = "CheckerTool";

    // ctor & dtor
    [DllImport(CHECKER_DLL)]
    private static extern void initCheckerTool(int block_x, int block_y);
    [DllImport(CHECKER_DLL)]
    private static extern void endCheckerTool();

    // setter
    [DllImport(CHECKER_DLL)]
    private static extern void setImageSize(int id, int img_w, int img_h);

    // process loop
    [DllImport(CHECKER_DLL)]
    private static extern void copyRawImage(int id, IntPtr ptr);
    [DllImport(CHECKER_DLL)]
    private static extern void flipRawImage(int id, int flipCode);

    [DllImport(CHECKER_DLL)]
    private static extern void convertRawToBinary(int id, int threshold);
    [DllImport(CHECKER_DLL)]
    private static extern void copyProcessedImage(int id, IntPtr ptr);
    [DllImport(CHECKER_DLL)]
    private static extern bool findChessCorners(int id, float[] pts_2D);

    #endregion // NATIVE_FUNTIONS_CHESSBOARD



    #region NATIVE_FUNTIONS_CALIB3D

    private const string CALIB3D_DLL = "Calib3DTool";

    // ctor & dtor
    [DllImport(CALIB3D_DLL)]
    private static extern void initCalib3DTool(int block_x, int block_y);
    [DllImport(CALIB3D_DLL)]
    private static extern void endCalib3DTool();

    // process for unknown camera
    [DllImport(CALIB3D_DLL)]
    private static extern int pushImageObjectPairPoints(int deviceID, float[] imgPts_2D, float[] objPts_3D);
    [DllImport(CALIB3D_DLL)]
    private static extern void clearImageObjectPairPoints(int deviceID);
    [DllImport(CALIB3D_DLL)]
    private static extern double calibrateCamera(int deviceID, int img_w, int img_h, int flag);

    // setter / getter for intrinsic parameters
    [DllImport(CALIB3D_DLL)]
    private static extern void setIntrinsicParams(int deviceID, float[] cam_mat4, float[] dist_coeff8);
    [DllImport(CALIB3D_DLL)]
    private static extern void getIntrinsicParams(int deviceID, float[] cam_mat4, float[] dist_coeff8);

    // process loop for KNOWN camera
    [DllImport(CALIB3D_DLL)]
    private static extern bool solvePnP(int deviceID, float[] imgPts_2D, float[] objPts_3D, float[] rot3x3, float[] tr3x1);

    #endregion // NATIVE_FUNTIONS_CALIB3D



    #region NATIVE_FUNTIONS_XML

    private const string XML_TOOL_DLL = "SimpleXMLTool";

    [DllImport(XML_TOOL_DLL)]
    private static extern void loadXML(string filename, float[] rot, float[] tr);
    [DllImport(XML_TOOL_DLL)]
    private static extern void saveXML(string filename, float[] rot, float[] tr);

    #endregion // NATIVE_FUNTIONS_XML
}

public static class Calib3dBinaryWrapper
{
    private static ICalib3dWrapper sWrapper = null;

    public static ICalib3dWrapper Instance
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
        sWrapper = new Calib3dNativeWrapper();
    }

    public static void SetImplementation(ICalib3dWrapper implementation)
    {
        sWrapper = implementation;
    }
}
