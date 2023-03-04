using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GridSpace : MonoBehaviour
{
    [SerializeField] private RectTransform m_grid;
    // [SerializeField] private Image m_image;
    [SerializeField] private TextMeshProUGUI m_label;

    public void ToggleGrid()
    {
        m_grid.gameObject.SetActive(m_grid.gameObject.activeSelf);
    }

    public void SetLabel(int row, int column)
    {
        m_label.SetText("");
    }

    // public void SetImage(string url)
    // {
    //     // download image from url in coroutine
    //     // set m_image source to downloaded image
    // }
}
