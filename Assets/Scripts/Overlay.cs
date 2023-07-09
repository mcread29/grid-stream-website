using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Overlay : MonoBehaviour
{
    private GridLayoutGroup m_gridLayout;

    [SerializeField] private Transform m_grid;
    [SerializeField] private Transform m_imagesParent;

    [SerializeField] private GridSpace m_gridSpacePrefab;

    private float spaceWidth;
    private float spaceHeight;

    private int m_rows;
    private int m_cols;

    private List<GridImage> m_images;
    private GridSpace[,] m_spaces;

    private void Awake()
    {
        m_gridLayout = m_grid.GetComponent<GridLayoutGroup>();
    }

    public void Create(int rows, int columns, IntPtr handle, GridImageSave[] images = null)
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

        m_images = new List<GridImage>();
        m_spaces = new GridSpace[rows, columns];

        m_rows = rows;
        m_cols = columns;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                GridSpace grid = Instantiate(m_gridSpacePrefab, m_grid);
                m_spaces[i, j] = grid;
                grid.SetLabel(i, j);
            }
        }

        if (images != null)
        {
            foreach (var image in images)
            {
                if(image.isGif) AddGif(image.startPos.x, image.startPos.y, image.URL, image.endPos.x, image.endPos.y);
                else AddImage(image.startPos.x, image.startPos.y, image.URL, image.endPos.x, image.endPos.y);
            }
        }
    }

    public void ClearImages()
    {
        foreach (Transform child in m_imagesParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void AddImage(int startRow, int startCol, string url, int endRow = -1, int endCol = -1)
    {
        if (endRow == -1) endRow = startRow;
        if (endCol == -1) endCol = startCol;

        GameObject o = new GameObject(url);
        o.transform.SetParent(m_imagesParent);

        GridImage gridImage = o.AddComponent<GridImage>();
        gridImage.startPos = new Vector2Int(startRow, startCol);
        gridImage.endPos = new Vector2Int(endRow, endCol);
        gridImage.URL = url;
        gridImage.isGif = false;
        m_images.Add(gridImage);

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
            Destroy(i.gameObject);
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            i.texture = texture;
        }
    }

    public void AddGif(int startRow, int startCol, string url, int endRow = -1, int endCol = -1)
    {
        if (endRow == -1) endRow = startRow;
        if (endCol == -1) endCol = startCol;

        GameObject o = new GameObject(url);
        o.transform.SetParent(m_imagesParent);
        
        GridImage gridImage = o.AddComponent<GridImage>();
        gridImage.startPos = new Vector2Int(startRow, startCol);
        gridImage.endPos = new Vector2Int(endRow, endCol);
        gridImage.URL = url;
        gridImage.isGif = true;
        m_images.Add(gridImage);

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
        i.enabled = false;
        UniGifImage g = o.AddComponent<UniGifImage>();
        Canvas c = o.AddComponent<Canvas>();
        StartCoroutine(g.SetGifFromUrlCoroutine(url));
    }

    IEnumerator DownloadGif(string url, UniGifImage g)
    {
        yield return null;
    }

    public OverlaySaveData GetSaveData()
    {
        OverlaySaveData data = new OverlaySaveData();

        data.rows = m_rows;
        data.cols = m_cols;
        
        List<GridImageSave> images = new List<GridImageSave>();
        foreach (var image in m_images)
        {
            images.Add(image.ToSave());
        }

        data.images = images.ToArray();
        
        return data;
    }
}
