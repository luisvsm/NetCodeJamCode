using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Block : MonoBehaviour
{
    public List<Sprite> blockImages;
    public Image image;

    public void SetBlockType(int type)
    {
        image.sprite = blockImages[type];
    }
}
