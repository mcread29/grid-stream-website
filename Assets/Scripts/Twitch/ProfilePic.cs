using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ProfilePic : MonoBehaviour
{
    private RawImage m_pic;

    private void Awake()
    {
        m_pic = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        if (m_pic.texture == null)
        {
            StartCoroutine(SetProfileImage());
        }
    }

    private IEnumerator SetProfileImage()
    {
        while (TwitchAPIHelper.ProfileTexture == null)
        {
            yield return null;
        }

        m_pic.texture = TwitchAPIHelper.ProfileTexture;
    }
}
