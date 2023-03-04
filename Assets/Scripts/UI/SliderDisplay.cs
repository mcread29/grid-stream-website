using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderDisplay : MonoBehaviour
{
    [SerializeField] private Slider m_slider;
    [SerializeField] private TextMeshProUGUI m_value;

    void Update()
    {
        m_value.SetText(((int)m_slider.value).ToString());
    }
}
