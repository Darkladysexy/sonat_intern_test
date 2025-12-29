using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TubeController : MonoBehaviour
{
    public List<Color> waterStack = new List<Color>();
    public SpriteRenderer[] layerSprites; 
    
    [Header("Surfaces")]
    public SpriteRenderer surfaceTop; 
    public SpriteRenderer surfaceBot; 

    [Header("Settings")]
    public Transform mouthPos; 
    public float totalHeight = 1.0f;
    
    public float surfaceOffset = 0f; 

    private float unitHeight => totalHeight / 4f;
    [HideInInspector] public Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.position;
    }

    public void UpdateVisuals()
    {
        float bottomY = -totalHeight / 2f; 

        for (int i = 0; i < layerSprites.Length; i++)
        {
            if (i < waterStack.Count)
            {
                layerSprites[i].gameObject.SetActive(true);
                layerSprites[i].color = waterStack[i];

                float scaleFactor = (i + 1) / 4f; 
                float currentHeight = totalHeight * scaleFactor;

                Vector2 newSize = layerSprites[i].size;
                newSize.y = currentHeight;
                layerSprites[i].size = newSize;

                float yPos = bottomY + (currentHeight / 2f);
                layerSprites[i].transform.localPosition = new Vector3(0, yPos, 0);

                layerSprites[i].sortingOrder = 10 - i;
            }
            else
            {
                layerSprites[i].gameObject.SetActive(false);
            }
        }

        if (waterStack.Count > 0)
        {
            int topIdx = waterStack.Count - 1;
            surfaceTop.gameObject.SetActive(true);
            surfaceBot.gameObject.SetActive(true); 
            
            Color topColor = waterStack[topIdx];
            surfaceTop.color = Color.white;
            surfaceBot.color = topColor;
            SpriteRenderer topSprite = layerSprites[topIdx];

            float topEdgeY = topSprite.transform.localPosition.y + (topSprite.size.y / 2f);

            float finalY = topEdgeY + surfaceOffset;

            surfaceTop.transform.localPosition = new Vector3(0, finalY, 0);
            surfaceBot.transform.localPosition = new Vector3(0, finalY, 0); 

            surfaceTop.sortingOrder = 16; 
            surfaceBot.sortingOrder = 15;
        }
        else
        {
            surfaceTop.gameObject.SetActive(false);
            surfaceBot.gameObject.SetActive(false);
        }
    }

    public bool IsFull() => waterStack.Count >= 4;
    public bool IsEmpty() => waterStack.Count == 0;
    public Color GetTopColor() => waterStack.Count > 0 ? waterStack[waterStack.Count - 1] : Color.clear;

    public bool CanReceive(Color incomingColor)
    {
        if (IsFull()) return false;
        return IsEmpty() || GetTopColor() == incomingColor;
    }
}