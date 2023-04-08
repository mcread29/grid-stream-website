using System;
using System.Collections;
using System.Collections.Generic;
using Firesplash.GameDevAssets.SocketIOPlus;
using UnityEngine;

[Serializable]
struct StartAppData
{
    public string username;
    public string pass;
}

[Serializable]
struct AddImageData
{
    public int startRow;
    public int startCol;
    public string url;
    public int endRow;
    public int endCol;
    public bool isGif;
}

public class Manager : MonoBehaviour
{
    [SerializeField] private Menu m_menu;
    [SerializeField] private GameObject m_grid;
    [SerializeField] private GameObject m_images;
    [SerializeField] private Overlay m_overlay;

    private SocketIOClient io;

    private static Manager m_instance;
    public static Manager Instance { get { return m_instance; } }

    public static string Password { get { return m_instance.password; } }
    public static string Username { get { return m_instance.username; } }

    private int rows;
    private int cols;
    private IntPtr handle;

    private string username;
    private string password;

    private void Awake()
    {
        if (m_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this;
    }

    private void Start()
    {
        io = GetComponent<SocketIOClient>();
        io.D.On("connect", () =>
        {
            Debug.Log("Connected");
        });
        io.D.On("StartApp", this.createOverlay);
        io.D.On<AddImageData>("AddImage", this.addImage);

#if UNITY_EDITOR
        io.Connect("http://localhost:8000");
#else
        io.Connect("https://mysterious-garden-06949.herokuapp.com");
#endif
    }

    public void TryCreateOverlay(int rows, int cols, IntPtr handle, string username, string password)
    {
        this.rows = rows;
        this.cols = cols;
        this.handle = handle;
        this.username = username;
        this.password = password;

        io.D.Emit<StartAppData>("StartApp", new StartAppData()
        {
            username = username,
            pass = password
        });
    }

    private void createOverlay()
    {
        m_overlay.Create(rows, cols, handle);
        m_menu.ShowOverlayActive();
    }

    private void addImage(AddImageData data)
    {
        if (data.isGif)
        {
            m_overlay.AddGif(data.startRow, data.startCol, data.url, data.endRow, data.endCol);
        }
        else
        {
            m_overlay.AddImage(data.startRow, data.startCol, data.url, data.endRow, data.endCol);
        }
    }

    public void ClearImages()
    {
        m_overlay.ClearImages();
    }

    public void ShowUI()
    {
        m_menu.gameObject.SetActive(true);
    }

    public void ShowGrid()
    {
        m_grid.gameObject.SetActive(true);
    }

    public void ShowImages()
    {
        m_images.gameObject.SetActive(true);
    }

    public void HideUI()
    {
        m_menu.gameObject.SetActive(false);
    }

    public void HideGrid()
    {
        m_grid.gameObject.SetActive(false);
    }

    public void HideImages()
    {
        m_images.gameObject.SetActive(false);
    }
}
