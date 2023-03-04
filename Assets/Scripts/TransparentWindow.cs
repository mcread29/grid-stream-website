﻿using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityRawInput;
using System.Diagnostics;

using System.Text;

public class TransparentWindow : MonoBehaviour
{

    [DllImport("user32.dll")]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern bool SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(System.String className, System.String windowName);

    private struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    const int GWL_EXSTYLE = -20;

    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    const uint LWA_COLORKEY = 0x00000001;

    private IntPtr hWnd;

    [SerializeField] private GameObject m_menu;
    // [SerializeField] private RectTransform m_gr
    // [SerializeField] private GameObject m_grid;

    private void OnEnable()
    {
        RawInput.OnKeyUp += LogKeyUp;
        RawInput.OnKeyDown += LogKeyDown;

        RawInput.Start();

        RawInput.WorkInBackground = true;
    }

    private void OnDisable()
    {
        RawInput.Stop();

        RawInput.OnKeyUp -= LogKeyUp;
        RawInput.OnKeyDown -= LogKeyDown;
    }

    private bool m_isControlDown = false;
    private bool m_isShiftDown = false;
    private bool m_isGDown = false;

    private void LogKeyUp(RawKey key)
    {
        if (key.ToString() == "LeftControl") m_isControlDown = false;
        if (key.ToString() == "LeftShift") m_isShiftDown = false;
        if (key.ToString() == "G") m_isGDown = false;
    }

    private void LogKeyDown(RawKey key)
    {
        if (key.ToString() == "LeftControl") m_isControlDown = true;
        if (key.ToString() == "LeftShift") m_isShiftDown = true;
        if (key.ToString() == "G") m_isGDown = true;

        if (m_isControlDown && m_isShiftDown && m_isGDown)
        {
            if (m_menu.gameObject.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }

    private void Start()
    {
        //         RectTransform c = GetComponentInParent<Canvas>().gameObject.GetComponent<RectTransform>();

        //         int w = Screen.currentResolution.width;
        //         int h = Screen.currentResolution.height;

        //         float scale = c.sizeDelta.y / h;

        //         foreach (KeyValuePair<IntPtr, WindowInfo> window in OpenWindowGetter.GetOpenWindows())
        //         {
        //             IntPtr handle = window.Key;
        //             WindowInfo info = window.Value;

        //             if (info.name == "Untitled - Notepad")
        //             {
        //                 float x = info.info.rcClient.Left * scale;
        //                 float y = info.info.rcClient.Top * scale;
        //                 float width = (info.info.rcClient.Right - info.info.rcClient.Left) * scale;
        //                 float height = (info.info.rcClient.Bottom - info.info.rcClient.Top) * scale;

        //                 RectTransform r = m_grid.GetComponent<RectTransform>();
        //                 r.sizeDelta = new Vector2(width, height);
        //                 r.anchoredPosition = new Vector3(x, -y, 0);

        //                 UnityEngine.Debug.Log($"{handle}: {info.name} - ({info.info.rcClient.Left}, {info.info.rcClient.Top}), ({info.info.rcClient.Right}, {info.info.rcClient.Bottom}), ({w}, {h})");
        //             }
        //         }

#if !UNITY_EDITOR
                hWnd = GetActiveWindow();

                MARGINS margins = new MARGINS { cxLeftWidth = -1 };
                DwmExtendFrameIntoClientArea(hWnd, ref margins);

                SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
                //SetLayeredWindowAttributes(hWnd, 0, 0, LWA_COLORKEY);

                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, 0);
#endif

        Application.runInBackground = true;

        SetClickthrough(false);
    }

    private void SetClickthrough(bool clickthrough)
    {
        if (clickthrough)
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }
        else
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);
        }
    }

    public void Show()
    {
        SetClickthrough(false);
        m_menu.gameObject.SetActive(true);
    }

    public void Hide()
    {
        SetClickthrough(true);
        m_menu.gameObject.SetActive(false);
    }
}