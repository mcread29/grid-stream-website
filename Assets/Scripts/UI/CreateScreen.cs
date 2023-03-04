using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateScreen : MonoBehaviour
{
    [SerializeField] private Slider m_columns;
    [SerializeField] private Slider m_rows;
    [SerializeField] private Dropdown m_window;

    private void Awake()
    {
        SetOptions();
    }

    public void SetOptions()
    {
        m_window.ClearOptions();
        List<string> options = new List<string>();
        IDictionary<System.IntPtr, WindowInfo> windows = OpenWindowGetter.GetOpenWindows();
        foreach (KeyValuePair<IntPtr, WindowInfo> window in windows)
        {
            IntPtr handle = window.Key;
            WindowInfo info = window.Value;
            options.Add(info.name);
        }
        m_window.AddOptions(options);
        m_window.SetValueWithoutNotify(0);
    }

    public void Create()
    {
        if (m_columns.value < 1)
        {
            // error
            return;
        }
        if (m_rows.value < 1)
        {
            // error
            return;
        }

        IntPtr selected = new IntPtr();
        foreach (KeyValuePair<IntPtr, WindowInfo> window in OpenWindowGetter.Windows)
        {
            IntPtr handle = window.Key;
            WindowInfo info = window.Value;
            if (info.name == m_window.options[m_window.value].text)
            {
                selected = handle;
                break;
            }
        }

        if (selected == IntPtr.Zero)
        {
            // error
            return;
        }

        Manager.Instance.CreateOverlay((int)m_rows.value, (int)m_columns.value, selected);
    }
}
