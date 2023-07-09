using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GridSpace : MonoBehaviour
{
    private GridImage image;
    
    string[] map = {
        "a", "b", "c",
        "d", "e", "f",
        "g", "h", "i",
        "j", "k", "l",
        "m", "n", "o",
        "p", "q", "r",
        "s", "t", "u",
        "v", "w", "x",
        "y", "z"
    };

    [SerializeField] private RectTransform m_grid;
    [SerializeField] private TextMeshProUGUI m_label;

    public void ToggleGrid()
    {
        m_grid.gameObject.SetActive(m_grid.gameObject.activeSelf);
    }

    public void SetLabel(int row, int column)
    {
        m_label.SetText(map[row].ToUpper() + "\n" + column);
    }
}
