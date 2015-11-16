using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Ovr;
using System.Security;

/// <summary>
/// VDPlaymodeBehavior is an editor behavior that positions the Game window onto the Rift monitor when entering playmode (Ctrl-P).
/// This allows a user using Virtual Desktop to code and playtest while staying in VR the entire time.
/// 
/// Copyright 2015 - Guy Godin
/// http://www.vrdesktop.net/
/// </summary>
[InitializeOnLoad]
public class VDPlaymodeBehavior : MonoBehaviour
{
    #region Static Fields
    private static readonly MethodInfo _getMainGameViewMethod = Type.GetType("UnityEditor.GameView,UnityEditor").GetMethod("GetMainGameView", BindingFlags.NonPublic | BindingFlags.Static);
    private static bool _isPlaying;
    #endregion

    #region Static Constructor
    static VDPlaymodeBehavior()
    {
        EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;
        EditorApplication.update += OnUpdate;
    }
    #endregion

    #region Static Properties
    public static bool IsPlaying
    {
        get { return _isPlaying; }
        set
        {
            if (value != _isPlaying)
            {
                _isPlaying = value;
                if (value)
                {
                    ShowGameWindow();
                }
                else
                {
                    CloseGameWindow();
                }
            }
        }
    }
    #endregion

    #region Static Methods
    private static EditorWindow GetMainGameView()
    {
        // Execute the private GetMainGameView method
        return (EditorWindow)_getMainGameViewMethod.Invoke(null, null);
    }

    private static void ShowGameWindow()
    {
        // Make sure an hmd is connected
        var hmd = OVRManager.capiHmd;
        if (hmd == null || !OVRManager.display.isPresent)
            return;

        // Open the game window
        EditorApplication.ExecuteMenuItem("Window/Game");
        EditorWindow gameView = GetMainGameView();
        if (gameView == null)
            return;

        // Check if the hmd is in extended mode
        HmdDesc desc = hmd.GetDesc();
        if ((desc.HmdCaps & (uint)HmdCaps.ExtendDesktop) == 0)
        {
            // Get window handle of game view
            IntPtr hWndUnity = GetActiveWindow();
            IntPtr hWndGameView = ChildWindowFromPointEx(hWndUnity, new POINT((int)gameView.position.x, (int)gameView.position.y), WindowFromPointFlags.CWP_SKIPINVISIBLE | WindowFromPointFlags.CWP_SKIPTRANSPARENT);

            // Attach to window
            // Note: this isn't working for some reason
            OVRManager.SetEditorPlay(false);
            LoadDisplayShim();
            hmd.AttachToWindow(new Recti(), new Recti(), hWndGameView);
            return;
        }

        // Get the hmd monitor position and size
        Vector2i hmdPosition = desc.WindowsPos;
        Sizei hmdResolution = desc.Resolution;        

        // Adjust the game view position
        Rect newPos = new Rect(hmdPosition.x, hmdPosition.y + 17, hmdResolution.w, hmdResolution.h - 22);

        gameView.position = newPos;
        gameView.minSize = newPos.size;
        gameView.maxSize = newPos.size;
        gameView.position = newPos;

        // Toggle focused window so that Virtual Desktop gets the foreground change event
        EditorUtility.FocusProjectWindow();
        gameView.title = "Game (Stereo)";
        gameView.Focus();
    }

    private static void CloseGameWindow()
    {
        EditorWindow gameView = GetMainGameView();
        if (gameView != null)
        {
            gameView.Close();
        }
    }
    #endregion

    #region Event Handlers
    private static void OnPlaymodeStateChanged()
    {
        // Keep internal flag as the event is raised before and after the state changes
        IsPlaying = EditorApplication.isPlaying;
    }

    private static void OnUpdate()
    {
        if (EditorApplication.isPlaying && Input.GetKey(KeyCode.Escape))
        {
            // Exit Playmode when user hits Escape
            EditorApplication.isPlaying = false;
        }
    }
    #endregion

    #region External Methods
    [DllImport("user32.dll")]
    [SuppressUnmanagedCodeSecurity]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    [SuppressUnmanagedCodeSecurity]
    private static extern IntPtr ChildWindowFromPointEx(IntPtr hWndParent, POINT Point, WindowFromPointFlags flags);

    [DllImport("OculusPlugin", CallingConvention = CallingConvention.Cdecl)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void LoadDisplayShim();
    #endregion

    #region Structures
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
    #endregion

    #region Enums
    [Flags]
    private enum WindowFromPointFlags
    {
        CWP_ALL = 0x0000,
        CWP_SKIPINVISIBLE = 0x0001,
        CWP_SKIPDISABLED = 0x0002,
        CWP_SKIPTRANSPARENT = 0x0004
    }
    #endregion
}
