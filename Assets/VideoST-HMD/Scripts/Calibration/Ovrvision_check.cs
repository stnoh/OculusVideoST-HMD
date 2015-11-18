/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// This class provides main interface to the Ovrvision
/// </summary>
public class Ovrvision_check : MonoBehaviour
{
    //Ovrvision Dll import
    //ovrvision_csharp.cpp
    ////////////// Main Ovrvision System //////////////
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovOpen(int locationID, float marker_meter, int hmdType);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovClose();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovPreStoreCamData();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovGetCamImage(System.IntPtr img, int eye, int qt);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovGetCamImageBGR(System.IntPtr img, int eye, int qt);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovGetCamImageForUnity(System.IntPtr pImagePtr_Left, System.IntPtr pImagePtr_Right, int qt);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovGetCamImageWithAR(System.IntPtr img, int eye, int qt);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovGetCamImageBGRWithAR(System.IntPtr img, int eye, int qt);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovGetCamImageForUnityWithAR(System.IntPtr pImagePtr_Left, System.IntPtr pImagePtr_Right, int qt);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetPixelSize();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetBufferSize();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetImageWidth();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetImageHeight();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetImageRate();

    //Set camera properties
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovSetExposure(int value);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovSetWhiteBalance(int value);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovSetContrast(int value);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovSetSaturation(int value);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovSetBrightness(int value);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovSetSharpness(int value);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovSetGamma(int value);
    //Get camera properties
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetExposure();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetWhiteBalance();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetContrast();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetSaturation();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetBrightness();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetSharpness();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovGetGamma();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern float ovGetOculusRightGap(int at);
    ////////////// Ovrvision AR System //////////////
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovARRender();
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovARGetData(System.IntPtr mdata, int datasize);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void ovARSetMarkerSize(float value);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern float ovARGetMarkerSize();

    //Ovrvision config read write
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovSetParamXMLfromFile(byte[] filename);
    [DllImport("ovrvision", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern int ovSaveParamXMLtoFile(byte[] filename);

    //camera select define
    private const int OV_CAMEYE_LEFT = 0;
    private const int OV_CAMEYE_RIGHT = 1;
    private const int OV_SET_AUTOMODE = (-1);
    //renderer quality
    private const int OV_PSQT_NONE = 0;		//No Processing quality
    private const int OV_PSQT_LOW = 1;		//Low Processing quality
    private const int OV_PSQT_HIGH = 2;		//High Processing quality
    private const int OV_PSQT_REFSET = 3;		//Ref Processing quality
    //Ar Macro define
    private const int MARKERGET_MAXNUM10 = 100; //max marker is 10
    private const int MARKERGET_ARG10 = 10;
    private const int MARKERGET_RECONFIGURE_NUM = 10;
    //Oculus Rift HMDType define
    private const int OV_HMD_OCULUS_OTHER = 0;
    private const int OV_HMD_OCULUS_DK1 = 1;
    private const int OV_HMD_OCULUS_DK2 = 2;

    //Camera GameObject
    private GameObject go_cameraPlaneLeft;
    private GameObject go_cameraPlaneRight;
    //Camera texture
    private Texture2D go_CamTexLeft;
    private Texture2D go_CamTexRight;
    private Color32[] go_pixelsColorLeft;
    private Color32[] go_pixelsColorRight;
    private GCHandle go_pixelsHandleLeft;
    private GCHandle go_pixelsHandleRight;
    private System.IntPtr go_pixelsPointerLeft = System.IntPtr.Zero;
    private System.IntPtr go_pixelsPointerRight = System.IntPtr.Zero;

    //public setting var
    //Camera status
    public bool camStatus = false;
    public bool useOvrvisionAR = false;
    public float arSize = 0.15f;
    public int useProcessingQuality = OV_PSQT_HIGH;

    //Chroma-key system
    public int camViewShader = 0;
    public Vector2 chroma_hue = new Vector2(1.0f, 0.0f);         //x=max y=min (0.0f-1.0f)
    public Vector2 chroma_saturation = new Vector2(1.0f, 0.0f);  //x=max y=min (0.0f-1.0f)
    public Vector2 chroma_brightness = new Vector2(1.0f, 0.0f);  //x=max y=min (0.0f-1.0f)

    //property
    public OvrvisionProperty camProp = new OvrvisionProperty();

    // ------ Function ------

    // Use this for initialization
    void Awake()
    {
        int hmdType = OV_HMD_OCULUS_DK2;
        //Prop awake
        camProp.AwakePropSaveToXML();

        //Open camera
        if (ovOpen(0, arSize, hmdType) == 0)
        {
            camStatus = true;
        }
        else
        {
            camStatus = false;
            Debug.LogError("Ovrvision open error!!");
        }
    }

    // Use this for initialization
    void Start()
    {
        // Initialize camera plane object(Left)
        go_cameraPlaneLeft = this.transform.FindChild("CameraPlaneLeft").gameObject;
        // Initialize camera plane object(Right)
        go_cameraPlaneRight = this.transform.FindChild("CameraPlaneRight").gameObject;

        //Create cam texture
        go_CamTexLeft = new Texture2D(ovGetImageWidth(), ovGetImageHeight(), TextureFormat.RGB24, false);
        go_CamTexRight = new Texture2D(ovGetImageWidth(), ovGetImageHeight(), TextureFormat.RGB24, false);
        //Cam setting
        go_CamTexLeft.wrapMode = TextureWrapMode.Clamp;
        go_CamTexRight.wrapMode = TextureWrapMode.Clamp;

        //Set right eye gap
        const float scale = 0.001f; // 1/1000 [m]:[mm]
        Vector3 gap = scale * new Vector3(ovGetOculusRightGap(0), ovGetOculusRightGap(1), ovGetOculusRightGap(2));
        Debug.Log("OVRCameraGap = (" + gap.x + "," + gap.y + "," + gap.z + ")");

        ////////////////////////////////////////////////////////////////////////////////
        // set camera rig
        ////////////////////////////////////////////////////////////////////////////////
        if (GameObject.Find("LeftEyeAnchor"))
        {
            Camera cam_L = GameObject.Find("LeftEyeAnchor").GetComponent<Camera>();
            cam_L.transform.localPosition = -0.5f * gap;
        }
        if (GameObject.Find("RightEyeAnchor"))
        {
            Camera cam_R = GameObject.Find("RightEyeAnchor").GetComponent<Camera>();
            cam_R.transform.localPosition = 0.5f * gap;
        }


        ////////////////////////////////////////////////////////////////////////////////
        // set camera properties
        ////////////////////////////////////////////////////////////////////////////////
        if (camViewShader == 0)
        {   //Normal shader
            go_cameraPlaneLeft.GetComponent<Renderer>().material.shader = Shader.Find("Ovrvision/ovTexture");
            go_cameraPlaneRight.GetComponent<Renderer>().material.shader = Shader.Find("Ovrvision/ovTexture");
        }
        else if (camViewShader == 1)
        {   //Chroma-key shader
            go_cameraPlaneLeft.GetComponent<Renderer>().material.shader = Shader.Find("Ovrvision/ovChromaticMask");
            go_cameraPlaneRight.GetComponent<Renderer>().material.shader = Shader.Find("Ovrvision/ovChromaticMask");

            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_maxh", chroma_hue.x);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_minh", chroma_hue.y);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_maxs", chroma_saturation.x);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_mins", chroma_saturation.y);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_maxv", chroma_brightness.x);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_minv", chroma_brightness.y);

            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_maxh", chroma_hue.x);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_minh", chroma_hue.y);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_maxs", chroma_saturation.x);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_mins", chroma_saturation.y);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_maxv", chroma_brightness.x);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_minv", chroma_brightness.y);
        }

        if (!camStatus)
            return;

        //Camera open only

        //Get texture pointer
        go_pixelsColorLeft = go_CamTexLeft.GetPixels32();
        go_pixelsColorRight = go_CamTexRight.GetPixels32();
        go_pixelsHandleLeft = GCHandle.Alloc(go_pixelsColorLeft, GCHandleType.Pinned);
        go_pixelsHandleRight = GCHandle.Alloc(go_pixelsColorRight, GCHandleType.Pinned);
        go_pixelsPointerLeft = go_pixelsHandleLeft.AddrOfPinnedObject();
        go_pixelsPointerRight = go_pixelsHandleRight.AddrOfPinnedObject();

        go_cameraPlaneLeft.GetComponent<Renderer>().material.mainTexture = go_CamTexLeft;
        go_cameraPlaneRight.GetComponent<Renderer>().material.mainTexture = go_CamTexRight;

        // start the image update thread
        ThreadStart();
    }

    // Update is called once per frame
    void Update()
    {
        //camStatus
        if (!camStatus)
            return;

        // synchronize thread
        ThreadUpdate();

        //Apply
        go_CamTexLeft.SetPixels32(go_pixelsColorLeft);
        go_CamTexLeft.Apply();
        go_CamTexRight.SetPixels32(go_pixelsColorRight);
        go_CamTexRight.Apply();

        //Key Input
//		CameraViewKeySetting ();
    }

    //GUI view
    void OnGUI()
    {

        //Error
        if (!camStatus)
        {
            GUIStyle guiStyle = new GUIStyle();
            guiStyle.normal.textColor = Color.red;	//error color
            //ovrvision not found.
            GUI.Label(new Rect(20, 20, 300, 40), "[Error] Ovrvision not found.", guiStyle);
        }
    }

    //Ovrvision AR Render to OversitionTracker Objects.
    int OvrvisionARRender()
    {
        ovARRender();

        float[] markerGet = new float[MARKERGET_MAXNUM10];
        GCHandle marker = GCHandle.Alloc(markerGet, GCHandleType.Pinned);

        //Get marker data
        int ri = ovARGetData(marker.AddrOfPinnedObject(), MARKERGET_MAXNUM10);

        OvrvisionTracker[] otobjs = GameObject.FindObjectsOfType(typeof(OvrvisionTracker)) as OvrvisionTracker[];
        foreach (OvrvisionTracker otobj in otobjs)
        {
            otobj.UpdateTransformNone();
            for (int i = 0; i < ri; i++)
            {
                if (otobj.markerID == (int)markerGet[i * MARKERGET_ARG10])
                {
                    otobj.UpdateTransform(markerGet, i);
                    break;
                }
            }
        }

        marker.Free();

        return ri;
    }

    // Quit
    void OnDestroy()
    {
        // end the image update thread
        ThreadEnd();

        if (!camStatus)
            return;

        //Close camera
        if (ovClose() != 0)
            Debug.LogError("Ovrvision close error!!");

        //free
        go_pixelsHandleLeft.Free();
        go_pixelsHandleRight.Free();

        camStatus = false;
    }

    //Public methods.
    //UpdateOvrvisionSetting method
    public void UpdateOvrvisionSetting(OvrvisionProperty prop)
    {
        if (!camStatus)
            return;

        //set config
        ovSetExposure(prop.exposure);
        ovSetWhiteBalance(prop.whitebalance);
        ovSetContrast(prop.contrast);
        ovSetSaturation(prop.saturation);
        ovSetBrightness(prop.brightness);
        ovSetSharpness(prop.sharpness);
        ovSetGamma(prop.gamma);

        //change shader
        if (camViewShader == 0)
        {   //Normal shader
            go_cameraPlaneLeft.GetComponent<Renderer>().material.shader = Shader.Find("Ovrvision/ovTexture");
            go_cameraPlaneRight.GetComponent<Renderer>().material.shader = Shader.Find("Ovrvision/ovTexture");
        }
        else if (camViewShader == 1)
        {   //Chroma-key shader
            go_cameraPlaneLeft.GetComponent<Renderer>().material.shader = Shader.Find("Ovrvision/ovChromaticMask");
            go_cameraPlaneRight.GetComponent<Renderer>().material.shader = Shader.Find("Ovrvision/ovChromaticMask");

            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_maxh", chroma_hue.x);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_minh", chroma_hue.y);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_maxs", chroma_saturation.x);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_mins", chroma_saturation.y);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_maxv", chroma_brightness.x);
            go_cameraPlaneLeft.GetComponent<Renderer>().material.SetFloat("_Color_minv", chroma_brightness.y);

            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_maxh", chroma_hue.x);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_minh", chroma_hue.y);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_maxs", chroma_saturation.x);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_mins", chroma_saturation.y);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_maxv", chroma_brightness.x);
            go_cameraPlaneRight.GetComponent<Renderer>().material.SetFloat("_Color_minv", chroma_brightness.y);
        }
    }



    #region MULTI_THREAD

    private Thread ovrvisionTextureThread;
    private Mutex ovrvisionTextureThreadMutex;

    void ThreadStart()
    {
        // Create thread
        ovrvisionTextureThreadMutex = new Mutex(true);
        ovrvisionTextureThread = new Thread(OvrvisionTextureThreadFunc);
        ovrvisionTextureThread.Start();
    }

    void ThreadUpdate()
    {
        ovrvisionTextureThreadMutex.ReleaseMutex();
        ovrvisionTextureThreadMutex.WaitOne();

        if (go_pixelsPointerLeft == System.IntPtr.Zero ||
            go_pixelsPointerRight == System.IntPtr.Zero)
            return;

        if (useOvrvisionAR)
            OvrvisionARRender();
    }

    void ThreadEnd()
    {
        ovrvisionTextureThread.Abort();
    }

    void OvrvisionTextureThreadFunc()
    {
        try
        {
            _OvrvisionTextureThreadFunc();
        }
        catch (System.Exception e)
        {
            if (!(e is ThreadAbortException))
            {
                Debug.LogError("Unexpected Death: " + e.ToString());
            }
        }
    }

    void _OvrvisionTextureThreadFunc()
    {
        while (true)
        {
            Thread.Sleep(1);
            if (useOvrvisionAR)
                ovGetCamImageForUnityWithAR(go_pixelsPointerLeft, go_pixelsPointerRight, useProcessingQuality);
            else
                ovGetCamImageForUnity(go_pixelsPointerLeft, go_pixelsPointerRight, useProcessingQuality);

            ovrvisionTextureThreadMutex.WaitOne();
            {
                // nothing to do 
            }
            ovrvisionTextureThreadMutex.ReleaseMutex();
        }
    }

    #endregion // MULTI_THREAD



    #region PUBLIC_METHODS

    public System.IntPtr getLeftImagePtr() { return go_pixelsPointerLeft; }

    public float getFocalLength() { return -camProp.focalPoint; }

    #endregion // PUBLIC_METHODS
}
