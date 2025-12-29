using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;
    private TubeController selectedTube;
    public LayerMask tubeLayer;

    private void Awake() => Instance = this;

    void Start() {
        LevelGenerator generator = Object.FindFirstObjectByType<LevelGenerator>();
        
        // Tìm tất cả Tube trong scene
        List<TubeController> allTubesInScene = new List<TubeController>(Object.FindObjectsByType<TubeController>(FindObjectsSortMode.None));

        if (generator != null) {
            int requiredColors = allTubesInScene.Count - 2;

            if (requiredColors > generator.colorPalette.Count) {
                Debug.LogError($"Không đủ màu trong Palette! Cần {requiredColors}, nhưng chỉ có {generator.colorPalette.Count}");
                requiredColors = generator.colorPalette.Count; 
            }

            generator.GenerateLevel(allTubesInScene, requiredColors); 
        } else {
            Debug.LogError("Chưa gán LevelGenerator vào Scene!");
        }
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 10f, tubeLayer);
        
        if (hit.collider != null)
        {
            TubeController clickedTube = hit.collider.GetComponent<TubeController>();

            if (selectedTube == null)
            {
                if (!clickedTube.IsEmpty())
                {
                    SelectTube(clickedTube);
                }
            }
            else
            {
                if (clickedTube == selectedTube)
                {
                    DeselectTube();
                }
                else if (clickedTube.CanReceive(selectedTube.GetTopColor()))
                {
                    StartCoroutine(PourRoutine(selectedTube, clickedTube));
                    selectedTube = null;
                }
                else
                {
                    DeselectTube();
                    if (!clickedTube.IsEmpty()) SelectTube(clickedTube);
                }
            }
        }
    }

    void SelectTube(TubeController tube)
    {
        selectedTube = tube;
        selectedTube.transform.DOMoveY(selectedTube.originalPosition.y + 0.5f, 0.25f).SetEase(Ease.OutQuad);
    }

    void DeselectTube()
    {
        if (selectedTube == null) return;
        selectedTube.transform.DOMove(selectedTube.originalPosition, 0.25f).SetEase(Ease.InQuad);
        selectedTube = null;
    }

    IEnumerator PourRoutine(TubeController src, TubeController dst)
    {
        Vector3 targetPos = dst.mouthPos.position + new Vector3(src.transform.position.x > dst.transform.position.x ? 0.8f : -0.8f, 1.2f, 0);
        src.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutQuad);
        
        float rotateAngle = src.transform.position.x > dst.transform.position.x ? 80f : -80f;
        yield return src.transform.DORotate(new Vector3(0, 0, rotateAngle), 0.4f).WaitForCompletion();

        Color colorToMove = src.GetTopColor();
        dst.waterStack.Add(colorToMove);
        src.waterStack.RemoveAt(src.waterStack.Count - 1);

        src.UpdateVisuals();
        dst.UpdateVisuals();

        yield return new WaitForSeconds(0.2f); 

        src.transform.DORotate(Vector3.zero, 0.3f);
        src.transform.DOMove(src.originalPosition, 0.5f).SetEase(Ease.InQuad);
    }
}