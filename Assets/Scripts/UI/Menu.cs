using UnityEngine;

public class Menu : MonoBehaviour
{
    [SerializeField] private CreateScreen m_createScreen;
    [SerializeField] private OverlayActive m_overlayActive;
    [SerializeField] private LoadOverlay m_loadOverlay;

    private void Awake()
    {
        ShowCreate();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void ShowCreate()
    {
        m_createScreen.gameObject.SetActive(true);
        m_overlayActive.gameObject.SetActive(false);
        m_loadOverlay.gameObject.SetActive(false);
    }

    public void ShowOverlayActive()
    {
        m_createScreen.gameObject.SetActive(false);
        m_overlayActive.gameObject.SetActive(true);
        m_loadOverlay.gameObject.SetActive(false);
    }

    public void ShowLoadOverlay()
    {
        m_overlayActive.gameObject.SetActive(false);
        m_createScreen.gameObject.SetActive(false);
        m_loadOverlay.gameObject.SetActive(true);
    }
}
