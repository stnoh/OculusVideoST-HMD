/*==============================================================================
This is Intel HSKL (Hand Skeletal Tracking Library) C# adaptor for Unity Engine.
Original "Intel Hand Skeletal Tracking Library" will be availiable here:
https://software.intel.com/en-us/articles/the-intel-skeletal-hand-tracking-library-experimental-release

This C# script is written by Seung-Tak Noh (seungtak.noh [at] gmail.com), 2013-2015
==============================================================================*/
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class HSKLTrackerImpl
{
    #region CONSTANTS

    private const float ERROR_CRITERION = 1.0f; // just following HSKL sample

    #endregion // CONSTANTS



    #region PUBLIC_MEMBERS

    public GameObject mHSKLModel = null;
    public GameObject[] bones;
    public Mesh[] meshes;
    public float[] bone_poses = new float[34 * 7]; // prepare for two hands, but not used yet... [FIX ME]

    #endregion // PUBLIC_MEMBERS



    #region MEMBERS

    private readonly GameObject mDepthCamera = null;
    Material material_skin = null;
    Material material_fail = null;

    #endregion // MEMBERS



    #region CONSTRUCTION

    public HSKLTrackerImpl(GameObject depthCamera, Material skin, Material fail)
	{
        // assign materials for rendering
        material_skin = skin;
        material_fail = fail;

        // hieararchy in depth camera
        mDepthCamera = depthCamera;
        GameObject handModel = new GameObject("HandModel");
        handModel.transform.parent = depthCamera.transform;
        handModel.transform.localPosition = Vector3.zero;
        handModel.transform.localRotation = Quaternion.identity;
        handModel.transform.localScale = Vector3.one;

        // initialize HSKL tracker
        InitHandModel(handModel);
    }

    #endregion // CONSTRUCTION



    #region PUBLIC_METHODS

    public void Update()
    {
        // HSKL hand tracking
        float hskl_error = BinaryWrapper.Instance.GetHSKLHandPoses(bone_poses);
        ManipulateHandModel(hskl_error);
    }

    public void OnDestroy()
    {
        // finalize
    }

    #endregion // PUBLIC_METHODS



    #region PRIVATE_METHODS_FOR_HSKL_HAND

    private void InitHandModel(GameObject handModel)
    {
        mHSKLModel = new GameObject("HSKL_hand");
        mHSKLModel.transform.parent = handModel.transform;
        mHSKLModel.transform.localPosition = Vector3.zero;
        mHSKLModel.transform.localRotation = Quaternion.identity;
        mHSKLModel.transform.localScale = Vector3.one;

        // create array of GameObject
        bones = new GameObject[17];
        meshes = new Mesh[17];

        // 17 bones
        for (int id = 0; id < 17; id++)
        {
            // copy vertices for mesh
            int[] hand_tris = new int[76 * 3];
            float[] hand_verts = new float[40 * 3];
            BinaryWrapper.Instance.GetHSKLBoneMesh(id, hand_tris, hand_verts);

            // create empty GameObject for each bone	
            string bone_name = "bone" + id;
            bones[id] = new GameObject(bone_name);

            // set transformation for this bone
            bones[id].transform.parent = mHSKLModel.transform;
            bones[id].transform.localPosition = Vector3.zero;
            bones[id].transform.localRotation = Quaternion.identity;
            bones[id].transform.localScale = Vector3.one;

            // create mesh for this bone
            meshes[id] = new Mesh();
            meshes[id].name = "bonemesh" + id;

            // vertices for mesh
            meshes[id].vertices = new Vector3[40];
            Vector3[] vertices = meshes[id].vertices;
            for (int j = 0; j < 40; j++)
            {
                float x =  hand_verts[3 * j + 0];
                float y = -hand_verts[3 * j + 1]; // CAUTION: HSKL(x,y,z) -> Unity(x,-y,z)
                float z =  hand_verts[3 * j + 2];

                vertices[j] = new Vector3(x, y, z);
            }
            meshes[id].vertices = vertices;

            // triangles for mesh :: CAUTION - Unity mesh ordering
            meshes[id].triangles = new int[76 * 3];
            int[] triangles = meshes[id].triangles;
            for (int j = 0; j < 76; j++)
            {
                triangles[3 * j + 0] = hand_tris[3 * j + 0];
                triangles[3 * j + 1] = hand_tris[3 * j + 2]; // for normal direction
                triangles[3 * j + 2] = hand_tris[3 * j + 1]; // for normal direction
            }
            meshes[id].triangles = triangles;

            // uvs & normals for mesh
            meshes[id].uv = new Vector2[meshes[id].vertices.Length];
            meshes[id].normals = new Vector3[meshes[id].vertices.Length];

            // recalculate normals & bounds
            meshes[id].Optimize();
            meshes[id].RecalculateNormals();
            meshes[id].RecalculateBounds();

            // create MeshFilter for GameObject
            bones[id].AddComponent<MeshFilter>();
            MeshFilter meshFilter = bones[id].GetComponent<MeshFilter>();
            meshFilter.mesh = meshes[id];

            // create MeshRenderer for GameObject
            bones[id].AddComponent<MeshRenderer>();
            bones[id].GetComponent<Renderer>().material = material_skin;

            // enable collider on "palm"
            if (id==1)
            {
                // create Collider for collision detection
                bones[id].AddComponent<MeshCollider>();
                MeshCollider meshCollider = bones[id].GetComponent<MeshCollider>();
                meshCollider.sharedMesh = meshes[id];
                meshCollider.convex = true;
                meshCollider.enabled = true;
                meshCollider.isTrigger = true; // prohibit frequent callback among bones by "trigger"

                // create RigidBody for collision detection
                bones[id].AddComponent<Rigidbody>();
                Rigidbody rigidbody = bones[id].GetComponent<Rigidbody>();
                rigidbody.useGravity = false; // unmovable by gravity
                rigidbody.isKinematic = true; // unmovable by collision
            }
        }

        bones[0].SetActive(false); // hide "wrist" bone
    }

    private void ManipulateHandModel(float error)
    {
        // set material by tracking error
        Material material_hand = (error < ERROR_CRITERION) ? material_skin : material_fail;

        // change transform at each bone
        for (int id = 0; id < 17; id++)
        {
            float rot_x = bone_poses[7 * id + 0];
            float rot_y = bone_poses[7 * id + 1];
            float rot_z = bone_poses[7 * id + 2];
            float rot_w = bone_poses[7 * id + 3];

            float tr_x = bone_poses[7 * id + 4];
            float tr_y = bone_poses[7 * id + 5];
            float tr_z = bone_poses[7 * id + 6];

            // HSKL (x,y,z) -> Unity (x,-y,z)
            Vector3 trans = new Vector3(tr_x, -tr_y, tr_z);
            Quaternion rot = new Quaternion(rot_x, -rot_y, rot_z, -rot_w);

            bones[id].transform.localPosition = trans;
            bones[id].transform.localRotation = rot;
            bones[id].GetComponent<Renderer>().material = material_hand;
        }
    }

    #endregion // PRIVATE_METHODS_FOR_HSKL_HAND
}
