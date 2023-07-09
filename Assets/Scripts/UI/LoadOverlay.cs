using System;
using System.Collections;
using System.Collections.Generic;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadOverlay : MonoBehaviour
{
    [SerializeField] private Dropdown m_window;
    [SerializeField] private TextMeshProUGUI m_rows;
    [SerializeField] private TextMeshProUGUI m_cols;
    [SerializeField] private TextMeshProUGUI m_errorText;

    private OverlaySaveData m_save;
    
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
    public void OverlayLoad()
    {
        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        // yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, "", "overlay.json", "Save", "Save");

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, "", null, "Load", "Load");
        if (FileBrowser.Success)
        {
            try
            {
                string fileContents = FileBrowserHelpers.ReadTextFromFile(FileBrowser.Result[0]);
                Debug.Log(fileContents);
                m_save = JsonUtility.FromJson<OverlaySaveData>(fileContents);
                
                m_rows.SetText("Num Rows: " + m_save.rows);
                m_cols.SetText("Num Columns: " + m_save.cols);
            }
            catch (Exception e)
            {
                m_errorText.SetText(e.Message);
            }
        }
    }

    public void Create()
    {
        if (m_save.username != null)
        {IntPtr selected = new IntPtr();
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
            
            Manager.Instance.TryCreateOverlay(m_save.rows, m_save.cols, selected, m_save.username, m_save.password, m_save.images);
        }
        else
        {
            m_errorText.SetText("No overlay loaded");
        }
    }
}
