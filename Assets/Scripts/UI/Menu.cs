using UnityEngine;

public class Menu : MonoBehaviour
{
    [SerializeField] private CreateScreen m_createScreen;
    [SerializeField] private OverlayActive m_overlayActive;

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
    }

    public void ShowOverlayActive()
    {
        m_createScreen.gameObject.SetActive(false);
        m_overlayActive.gameObject.SetActive(true);
    }
}
