using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GridImage : MonoBehaviour
{
    public string URL;
    public Vector2Int startPos;
    public Vector2Int endPos;
    public bool isGif;

    public GridImageSave ToSave()
    {
        return new GridImageSave
        {
            startPos = startPos,
            endPos = endPos,
            URL = URL,
            isGif = isGif
        };
    }
}
