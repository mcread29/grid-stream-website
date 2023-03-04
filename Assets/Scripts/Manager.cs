using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    [SerializeField] private Menu m_menu;
    [SerializeField] private Overlay m_overlay;

    private static Manager m_instance;
    public static Manager Instance { get { return m_instance; } }

    private void Awake()
    {
        if (m_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this;
    }

    public void CreateOverlay(int rows, int columns, IntPtr handle)
    {
        m_overlay.Create(rows, columns, handle);
        m_menu.ShowOverlayActive();
    }
}
