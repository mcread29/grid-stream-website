
using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public struct GridImageSave
{
    public string URL;
    public Vector2Int startPos;
    public Vector2Int endPos;
    public bool isGif;
}

[Serializable]
public struct OverlaySaveData
{
    public int rows;
    public int cols;
    public string username;
    public string password;
    public GridImageSave[] images;
}

public class OverlaySave
{
    public static int rows
    {
        get;
        private set;
    }

    public static int cols
    {
        get;
        private set;
    }

    public static List<GridImageSave> images
    {
        get;
        private set;
    }
}
