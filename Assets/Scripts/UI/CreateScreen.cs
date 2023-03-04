using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateScreen : MonoBehaviour
{
    [SerializeField] private Slider m_columns;
    [SerializeField] private Slider m_rows;
    [SerializeField] private Dropdown m_window;
    [SerializeField] private TMP_InputField m_username;
    [SerializeField] private TMP_InputField m_password;
    [SerializeField] private TextMeshProUGUI m_errorText;

    private void Awake()
    {
        SetOptions();
    }

    private void OnEnable()
    {
        m_errorText.SetText("");
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
            m_errorText.SetText("Columns must be greater than 0");
            return;
        }
        if (m_rows.value < 1)
        {
            m_errorText.SetText("Rows must be greater than 0");
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
            m_errorText.SetText("Invalid window");
            return;
        }

        if (m_username.text.Length < 1)
        {
            m_errorText.SetText("Username cannot be empty");
            return;
        }

        if (m_password.text.Length < 1)
        {
            m_errorText.SetText("Password cannot be empty");
            return;
        }

        Manager.Instance.TryCreateOverlay((int)m_rows.value, (int)m_columns.value, selected, m_username.text, m_password.text);
    }
}
