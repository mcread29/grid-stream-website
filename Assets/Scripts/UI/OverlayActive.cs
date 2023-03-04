using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OverlayActive : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_usernameText;

    private void OnEnable()
    {
        m_usernameText.SetText("Showing overlay for " + Manager.Username);
    }

    public void CopyPassword()
    {
        Manager.Password.CopyToClipboard();
    }
}
