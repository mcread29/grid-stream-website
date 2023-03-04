using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

string[] map = {
    "a", "b", "c",
    "d", "e", "f",
    "g", "h", "i"
};

public class Overlay : MonoBehaviour
{
    private GridLayoutGroup m_layout;

    [SerializeField] private GridSpace m_gridSpacePrefab;

    private static Overlay _instance;
    public static Overlay Instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            m_layout = GetComponent<GridLayoutGroup>();
        }
    }

    public void Create(int rows, int columns, IntPtr handle)
    {
        WindowInfo info = OpenWindowGetter.GetWindow(handle);

        RectTransform c = GetComponentInParent<Canvas>().gameObject.GetComponent<RectTransform>();

        int w = Screen.currentResolution.width;
        int h = Screen.currentResolution.height;

        float scale = c.sizeDelta.y / h;

        float x = info.info.rcClient.Left * scale;
        float y = info.info.rcClient.Top * scale;
        float width = (info.info.rcClient.Right - info.info.rcClient.Left) * scale;
        float height = (info.info.rcClient.Bottom - info.info.rcClient.Top) * scale;

        RectTransform r = GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(width, height);
        r.anchoredPosition = new Vector3(x, -y, 0);

        // UnityEngine.Debug.Log($"{handle}: {info.name} - ({info.info.rcClient.Left}, {info.info.rcClient.Top}), ({info.info.rcClient.Right}, {info.info.rcClient.Bottom}), ({w}, {h})");

        m_layout.constraintCount = columns;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                GridSpace grid = Instantiate(m_gridSpacePrefab, transform);
            }
        }
    }
}
