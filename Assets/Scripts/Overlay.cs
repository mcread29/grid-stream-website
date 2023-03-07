using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Overlay : MonoBehaviour
{
    private GridLayoutGroup m_gridLayout;

    [SerializeField] private Transform m_grid;
    [SerializeField] private Transform m_images;

    [SerializeField] private GridSpace m_gridSpacePrefab;

    private float spaceWidth;
    private float spaceHeight;

    private void Awake()
    {
        m_gridLayout = m_grid.GetComponent<GridLayoutGroup>();
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

        spaceWidth = width / columns;
        spaceHeight = height / rows;

        RectTransform r = GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(width, height);
        r.anchoredPosition = new Vector3(x, -y, 0);

        m_gridLayout.constraintCount = columns;
        m_gridLayout.cellSize = new Vector2(spaceWidth, spaceHeight);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                GridSpace grid = Instantiate(m_gridSpacePrefab, m_grid);
                grid.SetLabel(i, j);
            }
        }
    }

    public void ClearImages()
    {
        foreach (Transform child in m_images)
        {
            Destroy(child.gameObject);
        }
    }

    public void AddImage(int startRow, int startCol, string url, int endRow = -1, int endCol = -1)
    {
        if (endRow == -1) endRow = startRow;
        if (endCol == -1) endCol = startCol;

        GameObject o = new GameObject(url);
        o.transform.SetParent(m_images);

        RectTransform t = o.AddComponent<RectTransform>();
        t.anchorMin = new Vector2(0, 1);
        t.anchorMax = new Vector2(0, 1);
        t.sizeDelta = new Vector2((endCol - startCol + 1) * spaceWidth, (endRow - startRow + 1) * spaceHeight);
        t.anchoredPosition = new Vector2(
            startCol * spaceWidth + spaceWidth / 2 + ((endCol - startCol) * spaceWidth) / 2,
            -startRow * spaceHeight - spaceHeight / 2 - ((endRow - startRow) * spaceHeight) / 2
            );
        t.localScale = Vector3.one;
        // t.pivot = new Vector2(0, 1);

        RawImage i = o.AddComponent<RawImage>();
        StartCoroutine(DownloadImage(url, i));
    }

    IEnumerator DownloadImage(string MediaUrl, RawImage i)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(request.error);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            i.texture = texture;
        }
    }
}
