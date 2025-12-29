
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; 
using System.Linq;

public class TubeController : MonoBehaviour
{
    [Header("Core Data")]
    public List<Color> waterStack = new List<Color>();
    public int capacity = 4; 

    [Header("Visual References")]
    public SpriteRenderer[] layerSprites; 
    public SpriteRenderer surfaceTop;     
    public SpriteRenderer surfaceBot;     
    public SpriteRenderer capSprite;      
    public Transform mouthPos;            
        public LineRenderer lineRenderer; 

    [Header("Settings")]
    public float totalHeight = 1.0f;     
    public float topLevelY = 0.0f;       
    public float stepHeight = 1.7f;       
    public float surfaceOffset = 0.0f;   

    [HideInInspector] public Vector3 originalPosition;
    private bool isClosed = false; 

    private void Awake()
    {
        originalPosition = transform.position;
        
        if (capSprite) capSprite.gameObject.SetActive(false);

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
            
            lineRenderer.widthMultiplier = 1f;
            lineRenderer.startWidth = 0.4f; 
            lineRenderer.endWidth = 0.15f;  
            
            lineRenderer.sortingOrder = 20; 
        }
    }

    private void OnValidate() 
    { 
        if (Application.isPlaying) UpdateVisuals(); 
    }

    public void CloseTube()
    {
        if (isClosed || capSprite == null) return;
        isClosed = true;
        StartCoroutine(AnimateCapRoutine());
    }

    private IEnumerator AnimateCapRoutine()
    {
        capSprite.gameObject.SetActive(true);
        
        Vector3 finalPos = Vector3.zero; 
        Vector3 startPos = new Vector3(0, 10f, 0); 
        
        capSprite.transform.localPosition = startPos;

        float elapsed = 0f;
        float duration = 0.6f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float t_eased = DOVirtual.EasedValue(0, 1, t, Ease.OutBounce); 
            
            capSprite.transform.localPosition = Vector3.Lerp(startPos, finalPos, t_eased);
            yield return null;
        }
        capSprite.transform.localPosition = finalPos;
    }

    public void UpdateVisuals()
    {
        if (layerSprites != null)
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
                    if (layerSprites[i] != null) layerSprites[i].gameObject.SetActive(false);
                }
            }
        }

        if (waterStack.Count > 0)
        {
            int missingLayers = 4 - waterStack.Count;
            float calculatedY = topLevelY - (missingLayers * stepHeight);
            float finalY = calculatedY + surfaceOffset;

            if (surfaceTop)
            {
                surfaceTop.gameObject.SetActive(true);
                surfaceTop.color = Color.white;
                surfaceTop.transform.localPosition = new Vector3(0, finalY, 0);
                surfaceTop.sortingOrder = 11;
            }

            if (surfaceBot)
            {
                surfaceBot.gameObject.SetActive(true);
                surfaceBot.color = waterStack[waterStack.Count - 1];
                surfaceBot.transform.localPosition = new Vector3(0, finalY, 0);
                surfaceBot.sortingOrder = 10;
            }
        }
        else
        {
            if (surfaceTop) surfaceTop.gameObject.SetActive(false);
            if (surfaceBot) surfaceBot.gameObject.SetActive(false);
        }
    }

    public int GetTopColorCount()
    {
        if (waterStack.Count == 0) return 0;
        Color topColor = waterStack[waterStack.Count - 1];
        int count = 0;
        for (int i = waterStack.Count - 1; i >= 0; i--)
        {
            if (waterStack[i] == topColor) count++;
            else break;
        }
        return count;
    }

    public int GetFreeSpace() => capacity - waterStack.Count;
    public bool IsFull() => waterStack.Count >= capacity;
    public bool IsEmpty() => waterStack.Count == 0;
    public Color GetTopColor() => waterStack.Count > 0 ? waterStack[waterStack.Count - 1] : Color.clear;
    
    public bool IsCompleted()
    {
        if (waterStack.Count < capacity) return false;
        Color first = waterStack[0];
        return waterStack.All(c => c == first);
    }

    public bool CanReceive(Color incomingColor)
    {
        if (isClosed) return false; 
        if (IsFull()) return false;
        return IsEmpty() || GetTopColor() == incomingColor;
    }
}