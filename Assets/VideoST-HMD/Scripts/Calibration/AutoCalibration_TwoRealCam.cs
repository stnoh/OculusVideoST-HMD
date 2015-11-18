////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2015, Seung-Tak Noh (seungtak.noh@gmail.com)
////////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System;
using System.Runtime.InteropServices;


public class AutoCalibration_TwoRealCam : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject binaryImage_id0 = null;
    public GameObject binaryImage_id1 = null;

    // chessboard properties
    public int block_x = 9;
    public int block_y = 6;
    public float length_mm = 30.0f;

    // binary value threshold
    public int binary_threshold_id0 = 64;
    public int binary_threshold_id1 = 64;
    public string filename = "camera.xml";

    #endregion // PUBLIC_MEMBERS



    #region PRIVATE_MEMBERS

    // flags for control
    bool find_checker = false;

    // 
    GameObject mChessboard;
    public GameObject mOvrLeftObject3D;
    public GameObject mCreativeSenz3D;

    // 
    IntPtr image_ptr_id0;
    IntPtr image_ptr_id1;

    // for calibration
    float[] imgPts_id0;
    float[] imgPts_id1;
    float[] objPts3D_m;
    float[] objPts3D_mm;

    // for gettting camera pose
    float[] r3x3_id0 = new float[9];
    float[] t3x1_id0 = new float[3];
    float[] r3x3_id1 = new float[9];
    float[] t3x1_id1 = new float[3];

    #endregion // PRIVATE_MEMBERS



    #region MONO_BEHAVIOUR

    void Start()
    {
        // open calib3d DLL module based on chessboard input
        Calib3dBinaryWrapper.Instance.Start(block_x, block_y);

        // set image size in advance
        Calib3dBinaryWrapper.Instance.SetImageSize(0, 640, 480); // OvrVision undistorted image res : VGA (640x480)
        Calib3dBinaryWrapper.Instance.SetImageSize(1, 640, 480); // Creative Senz3D   raw image res : VGA (640x480)

        // make chessboard GameObject based on input values
        mChessboard = MakeChessboard(block_x, block_y, length_mm);
        mChessboard.transform.parent = this.gameObject.transform;

        // make 2D-3D points for calibration & pose estimation
        imgPts_id0 = new float[2 * (block_x - 1) * (block_y - 1)];
        imgPts_id1 = new float[2 * (block_x - 1) * (block_y - 1)];
        objPts3D_mm = CreateObjectPointsArray(1.0f);
        objPts3D_m = CreateObjectPointsArray(0.001f);

        // ImageQuad - Binary image with Chessboard
        if (binaryImage_id0 != null)
            SetImageQuad(binaryImage_id0, Calib3dBinaryWrapper.Instance.GetTexture(0));
        if (binaryImage_id1 != null)
            SetImageQuad(binaryImage_id1, Calib3dBinaryWrapper.Instance.GetTexture(1));
    }

    bool init = false;
    void Update()
    {
        ////////////////////////////////////////////////////////////
        // initialization for Calib3D module
        ////////////////////////////////////////////////////////////
        if (!init)
        {
            // id0: OvrVision - Left
            Ovrvision_check ovr_script = GameObject.Find("OvrvisionSDK").GetComponent<Ovrvision_check>();
            image_ptr_id0 = ovr_script.getLeftImagePtr();

            // set intrinsic parameters for camera
            float focal_length = ovr_script.getFocalLength();
            float[] cam_mat_id0 = {focal_length, focal_length, 320.0f, 240.0f};
            Calib3dBinaryWrapper.Instance.SetCameraMatrix_temp(0, cam_mat_id0);

            // id1: Senz3D - Color
            IntelCamera_check intelcam_script = GameObject.Find("Senz3D").GetComponent<IntelCamera_check>();
            image_ptr_id1 = intelcam_script.getIntelColorImagePtr();

            // set intrinsic parameters for camera
            float[] cam_mat_id1 = { 597.0f, 600.0f, 320.0f, 240.0f };
            Calib3dBinaryWrapper.Instance.SetCameraMatrix_temp(1, cam_mat_id1);

            init = true;
        }

        // control
        if (Input.GetKeyDown(KeyCode.Space))
        {
            find_checker = !find_checker;
        }

        if (find_checker)
        {
            mChessboard.SetActive(true);

            // pass image to DLL module
            Calib3dBinaryWrapper.Instance.ConvertBinaryImage(0, image_ptr_id0, FLIP_CODE.NONE, binary_threshold_id0);
            Calib3dBinaryWrapper.Instance.ConvertBinaryImage(1, image_ptr_id1, FLIP_CODE.LEFTRIGHT, binary_threshold_id1);

            // id0 = hold camera, move object
            if ( Calib3dBinaryWrapper.Instance.FindChecker(0, imgPts_id0) )
            {
                // convert OpenCV's [R|T] into GameObject position/rotation
                Calib3dBinaryWrapper.Instance.GetCameraPose(0, imgPts_id0, objPts3D_m, r3x3_id0, t3x1_id0);
                ConvertCoord.RT2ObjectTransform(mOvrLeftObject3D.transform, mChessboard.transform, r3x3_id0, t3x1_id0);
                mOvrLeftObject3D.SetActive(true);
            }
            else
            {
                mOvrLeftObject3D.SetActive(false);
            }

            // id1= hold object, move camera
            if ( Calib3dBinaryWrapper.Instance.FindChecker(1, imgPts_id1) )
            {
                // convert OpenCV's [R|T] into GameObject position/rotation
                Calib3dBinaryWrapper.Instance.GetCameraPose(1, imgPts_id1, objPts3D_m, r3x3_id1, t3x1_id1);
                ConvertCoord.RT2CameraTransform(mCreativeSenz3D.transform, mChessboard.transform, r3x3_id1, t3x1_id1, Vector4.zero);
                mCreativeSenz3D.SetActive(true);
            }
            else
            {
                mCreativeSenz3D.SetActive(false);
            }
        }
        else
        {
            mChessboard.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Calib3dBinaryWrapper.Instance.SaveXML(filename, mCreativeSenz3D.transform);
        }
    }

    void OnDestroy()
    {
        // close DLL module
        Calib3dBinaryWrapper.Instance.End();
    }

    #endregion // MONO_BEHAVIOUR



    #region PRIVATE_METHODS

    GameObject MakeChessboard(int block_x, int block_y, float length_mm)
    {
        GameObject chessboard = GameObject.CreatePrimitive(PrimitiveType.Plane);

        // metric-aware: default size of "Plane" type is (10,10) in Unity scale.
        const float mm2m = 0.001f;
        const float PlaneToMetric = 0.1f;
        float length_x = block_x * length_mm * mm2m * PlaneToMetric;
        float length_y = block_y * length_mm * mm2m * PlaneToMetric;

        // set transformation as default
        chessboard.transform.localPosition = Vector3.zero;
        chessboard.transform.localRotation = Quaternion.identity;
        chessboard.transform.localScale = new Vector3(length_x, 1.0f, length_y); // aligned on XZ plane

        // make grid-like texture
        Texture2D tex2D = new Texture2D(block_x * 16, block_y * 16);
        Color32[] colorArray = tex2D.GetPixels32();
        for (int j = 0; j < block_y * 16; j++)
        for (int i = 0; i < block_x * 16; i++)
        {
            Color32 c = Color.red;
            if (i % 16 == 0 || i % 16 == 15 ||
                j % 16 == 0 || j % 16 == 15)
                c.a = 255; // non-transparent
            else
                c.a = 0; // full-transparent

            colorArray[i + j * (block_x * 16)] = c;
        }
        tex2D.SetPixels32(colorArray);
        tex2D.Apply();

        // set half-transparent texture
        Renderer chessboardRenderer = chessboard.GetComponent<Renderer>();
        chessboardRenderer.material.mainTexture = tex2D;
        chessboardRenderer.material.shader = Shader.Find("Unlit/Transparent");

        // return chessboard object
        return chessboard;
    }

    float[] CreateObjectPointsArray(float scale)
    {
        // corner in the chessboard
        int corner_x = block_x - 1;
        int corner_y = block_y - 1;

        // allocate floating array for 2D-3D points
        int numOfPoints = corner_x * corner_y;
        float[] obj_pts = new float[numOfPoints * 3];

        // calculate 3D points in advance
        float boardW_half = (corner_x-1) * 0.5f * length_mm;
        float boardH_half = (corner_y-1) * 0.5f * length_mm;

	    for( int j = 0; j < corner_y; j++ )
	    for( int i = 0; i < corner_x; i++ ){
            Vector3 pts = new Vector3(i*length_mm-boardW_half,j*length_mm-boardH_half,0.0f);
            pts *= scale;

            obj_pts[3 * (i + j * corner_x) + 0] = pts.x;
            obj_pts[3 * (i + j * corner_x) + 1] = pts.y;
            obj_pts[3 * (i + j * corner_x) + 2] = pts.z;
	    }

        return obj_pts;
    }

    void SetImageQuad(GameObject quadObj, Texture2D tex2D)
    {
        // get raw image properties
        int w = tex2D.width;
        int h = tex2D.height;
        float aspect = (float)w / (float)h;

        // resize based on texture aspect
        float x = quadObj.transform.localScale.x;
        quadObj.transform.localScale = new Vector3(x,-x/aspect,1.0f);

        // set material for rendering
        Material mat = quadObj.GetComponent<Renderer>().material;
        mat.mainTexture = tex2D;
        mat.shader = Shader.Find("Unlit/Texture");
    }

    #endregion // PRIVATE_METHODS
}
