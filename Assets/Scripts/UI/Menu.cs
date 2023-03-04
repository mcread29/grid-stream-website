using UnityEngine;

public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject m_createScreen;
    [SerializeField] private GameObject m_overlayActive;

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
        m_createScreen.SetActive(true);
        m_overlayActive.SetActive(false);
    }

    public void ShowOverlayActive()
    {
        m_createScreen.SetActive(false);
        m_overlayActive.SetActive(true);
    }
}
