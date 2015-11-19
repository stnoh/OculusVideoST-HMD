/*==============================================================================
Copyright (c) 2013-2015, Seung-Tak Noh (seungtak.noh [at] gmail.com)
All rights reserved.
==============================================================================*/
using UnityEngine;
using System.Collections;

public static class ConvertCoord {

    ////////////////////////////////////////////////////////////////////////////////
    // camera representations
    ////////////////////////////////////////////////////////////////////////////////
    public static Matrix4x4 CameraMatrix2ProjMatrix(float[] cam_mat, int dimx, int dimy, float z_near, float z_far)
    {
        float fx = cam_mat[0];
        float fy = cam_mat[1];
        float cx = cam_mat[2];
        float cy = cam_mat[3];

        float left = -z_near / fx * cx;
        float right = z_near / fx * (dimx - cx);
        float bottom = -z_near / fy * (dimy - cy);
        float top = z_near / fy * cy;

        // SET FRUSTUM
        Matrix4x4 mat = Matrix4x4.zero;

        mat[0, 0] = 2.0f * z_near / (right - left);
        mat[0, 1] = 0.0f;
        mat[0, 2] = (right + left) / (right - left);
        mat[0, 3] = 0.0f;

        mat[1, 0] = 0.0f;
        mat[1, 1] = 2.0f * z_near / (top - bottom);
        mat[1, 2] = (top + bottom) / (top - bottom);
        mat[1, 3] = 0.0f;

        mat[2, 0] = 0.0f;
        mat[2, 1] = 0.0f;
        mat[2, 2] = -(z_far + z_near) / (z_far - z_near);
        mat[2, 3] = -2.0f * (z_far * z_near) / (z_far - z_near);

        mat[3, 0] = 0.0f;
        mat[3, 1] = 0.0f;
        mat[3, 2] = -1.0f;
        mat[3, 3] = 0.0f;

        return mat;
    }

    public static float[] ProjMatrix2CameraMatrix(Matrix4x4 proj_mat, int dimx, int dimy)
    {
        float[] cam_mat = new float[4];

        float z_near = proj_mat[2,3] / (proj_mat[2,2] - 1.0f);
        float z_far = proj_mat[2,3] / (proj_mat[2,2] + 1.0f); // no need for recover camera matrix

        float left = z_near * (proj_mat[0,2] - 1.0f) / proj_mat[0,0];
        float right = z_near * (proj_mat[0,2] + 1.0f) / proj_mat[0,0];
        float top = z_near * (proj_mat[1,2] + 1.0f) / proj_mat[1,1];
        float bottom = z_near * (proj_mat[1,2] - 1.0f) / proj_mat[1,1];

        cam_mat[0] = z_near * dimx / (right - left); // fx
        cam_mat[1] = z_near * dimy / (top - bottom); // fy
        cam_mat[2] = - left * dimx / (right - left); // cx
        cam_mat[3] =    top * dimy / (top - bottom); // cy

        return cam_mat;
    }


    ////////////////////////////////////////////////////////////////////////////////
    // transformations
    ////////////////////////////////////////////////////////////////////////////////
    public static void RT2CameraTransform(Transform cam_transform, Transform obj_transform, float[] rot3x3, float[] tr3x1, Vector4 offset)
    {
        // RT --> matrix4x4 (in OpenCV)
        Matrix4x4 mat = RT2Matrix(rot3x3, tr3x1);

        // matrix4x4 (in OpenCV) --> matrix4x4 (in Unity)
        Quaternion rot = QuaternionFromMatrix(mat);
        Vector3 pos = mat.GetColumn(3);
        Matrix4x4 modelUnityMat = Matrix4x4.TRS(
            new Vector3(pos.x, -pos.y, pos.z),
            new Quaternion(rot.x, -rot.y, rot.z, -rot.w)*Quaternion.AngleAxis(90.0f, Vector3.left), // for "Plane"
            Vector3.one);

        // 
        Matrix4x4 objConversionMat = obj_transform.localToWorldMatrix;
        Matrix4x4 objUnityMat = Matrix4x4.TRS(
            objConversionMat.GetColumn(3),
            QuaternionFromMatrix(objConversionMat),
            Vector3.one);

        // final solution : camera pose
        modelUnityMat = modelUnityMat.inverse; // camera, not object
        modelUnityMat = objUnityMat * modelUnityMat;
        cam_transform.localPosition = modelUnityMat.GetColumn(3) + offset;
        cam_transform.localRotation = QuaternionFromMatrix(modelUnityMat);
    }

    public static void RT2ObjectTransform(Transform cam_transform, Transform obj_transform, float[] rot3x3, float[] tr3x1)
    {
        // RT --> matrix4x4 (in OpenCV)
        Matrix4x4 mat = RT2Matrix(rot3x3, tr3x1);

        // matrix4x4 (in OpenCV) --> matrix4x4 (in Unity)
        Quaternion rot = QuaternionFromMatrix(mat);
        Vector3 pos = mat.GetColumn(3);
        Matrix4x4 modelUnityMat = Matrix4x4.TRS(
            new Vector3(pos.x, -pos.y, pos.z),
            new Quaternion(rot.x, -rot.y, rot.z, -rot.w) * Quaternion.AngleAxis(90.0f, Vector3.left), // for "Plane"
            Vector3.one);

        // 
        Matrix4x4 camConversionMat = cam_transform.localToWorldMatrix;
        Matrix4x4 objUnityMat = Matrix4x4.TRS(
            camConversionMat.GetColumn(3),
            QuaternionFromMatrix(camConversionMat),
            Vector3.one);

        // final solution : object pose
//        modelUnityMat = modelUnityMat.inverse; // object, so does not need
        modelUnityMat = objUnityMat * modelUnityMat;
        obj_transform.localPosition = modelUnityMat.GetColumn(3);
        obj_transform.localRotation = QuaternionFromMatrix(modelUnityMat);
    }

    public static void Matrix2Transform(Transform transform, Matrix4x4 mat)
    {
        transform.position = mat.GetColumn(3);
        transform.rotation = ConvertCoord.QuaternionFromMatrix(mat);
        transform.localScale = Vector3.one;
    }


    ////////////////////////////////////////////////////////////////////////////////
    // OpenCV [rot3x3 | tr3x1] --> matrix4x4 (in OpenCV)
    ////////////////////////////////////////////////////////////////////////////////
    private static Matrix4x4 RT2Matrix(float[] rot3x3, float[] tr3x1)
    {
        Matrix4x4 mat = Matrix4x4.zero;
        mat[0, 0] = rot3x3[0];
        mat[0, 1] = rot3x3[1];
        mat[0, 2] = rot3x3[2];

        mat[1, 0] = rot3x3[3];
        mat[1, 1] = rot3x3[4];
        mat[1, 2] = rot3x3[5];

        mat[2, 0] = rot3x3[6];
        mat[2, 1] = rot3x3[7];
        mat[2, 2] = rot3x3[8];

        mat[0, 3] = tr3x1[0];
        mat[1, 3] = tr3x1[1];
        mat[2, 3] = tr3x1[2];

        mat[3, 0] = mat[3, 1] = mat[3, 2] = 0.0f;
        mat[3, 3] = 1.0f;

        return mat;
    }

    private static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Adapted from: http://forum.unity3d.com/threads/is-it-possible-to-get-a-quaternion-from-a-matrix4x4.142325/
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));

        // [DEPRECATED]
        /*
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm

        Quaternion q = new Quaternion();
        q.w = (float)Mathf.Sqrt(Mathf.Max(0.0f, 1.0f + m.m00 + m.m11 + m.m22)) * 0.5f;
        q.x = (float)Mathf.Sqrt(Mathf.Max(0.0f, 1.0f + m.m00 - m.m11 - m.m22)) * 0.5f;
        q.y = (float)Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - m.m00 + m.m11 - m.m22)) * 0.5f;
        q.z = (float)Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - m.m00 - m.m11 + m.m22)) * 0.5f;

        q.x *= Mathf.Sign(m.m21 - m.m12);
        q.y *= Mathf.Sign(m.m02 - m.m20);
        q.z *= Mathf.Sign(m.m10 - m.m01);

        return q;
        //*/
    }
}
