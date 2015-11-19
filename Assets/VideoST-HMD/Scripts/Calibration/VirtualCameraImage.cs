/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class VirtualCameraImage : MonoBehaviour 
{
    public Camera render_cam;
    public RenderTexture render_tex;

    Texture2D tex2D;

	void Start ()
    {
        tex2D = new Texture2D(render_tex.width, render_tex.height, TextureFormat.ARGB32, false);
    }

    void OnDestroy()
    {

    }

    public Matrix4x4 GetProjMatrix()
    {
        return render_cam.projectionMatrix;
    }

    ////////////////////////////////////////////////////////////
    // THIS CODE HAS A SPEED ISSUE INEVITABLY.
    ////////////////////////////////////////////////////////////
    public void RunAndCopyImage(int threshold)
    {
        // If you want to convert RenderTexture to Texture2D, you need to convert data like this...
        RenderTexture activeRenderTexture = RenderTexture.active; // temporary preserve rendering context
        RenderTexture.active = render_tex;
        render_cam.Render();
        tex2D.ReadPixels(new Rect(0, 0, render_tex.width, render_tex.height), 0, 0);
        tex2D.Apply();
        RenderTexture.active = activeRenderTexture; // return back rendering context

        Color32[] colors = tex2D.GetPixels32();
        GCHandle cameraTexturePixelsHandle_ = GCHandle.Alloc(colors, GCHandleType.Pinned);
        IntPtr ptr = cameraTexturePixelsHandle_.AddrOfPinnedObject();
        Calib3dBinaryWrapper.Instance.ConvertBinaryImage(1, ptr, FLIP_CODE.UPDOWN, threshold);
        cameraTexturePixelsHandle_.Free();
    }
}
