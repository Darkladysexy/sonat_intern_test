
using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro; 
public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance;
    
    [Header("UI References")]
    public GameObject winPanel;
    public GameObject losePanel; 
    public TextMeshProUGUI levelText; 

    [Header("Config")]
    public LayerMask tubeLayer;
    private bool isBusy = false;
    private int currentLevel = 1;

    private TubeController selectedTube;
    private List<TubeController> allTubes = new List<TubeController>();

    private void Awake() 
    {
        Instance = this;
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
    }

    void Start()
    {
        allTubes = new List<TubeController>(FindObjectsByType<TubeController>(FindObjectsSortMode.None));
        StartNewLevel();
    }

    void StartNewLevel()
    {
        if (levelText != null)
        {
            levelText.text = "LEVEL " + currentLevel;
        }

        LevelGenerator generator = FindFirstObjectByType<LevelGenerator>();
        if (generator != null)
        {
            int colors = generator.GetColorsForLevel(currentLevel);
            generator.GenerateLevel(allTubes, colors);
        }
        
        if(winPanel) winPanel.SetActive(false);
        if(losePanel) losePanel.SetActive(false);
        isBusy = false;
        Debug.Log("Level Started: " + currentLevel);
    }

    void Update()
    {
        if (isBusy) return;
        if (Input.GetMouseButtonDown(0)) HandleInput();
    }

    void HandleInput()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 10f, tubeLayer);
        
        if (hit.collider != null)
        {
            TubeController clickedTube = hit.collider.GetComponent<TubeController>();
            
            if (clickedTube.IsCompleted()) return; 

            if (selectedTube == null)
            {
                if (!clickedTube.IsEmpty()) SelectTube(clickedTube);
            }
            else
            {
                if (clickedTube == selectedTube) 
                {
                    DeselectTube(); 
                }
                else 
                {
                    if (clickedTube.CanReceive(selectedTube.GetTopColor()))
                    {
                        StartCoroutine(PourSequence(selectedTube, clickedTube));
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
    }

    void SelectTube(TubeController tube)
    {
        AudioManager.Instance.PlayPickUp();
        selectedTube = tube;
        selectedTube.transform.DOMoveY(selectedTube.originalPosition.y + 0.5f, 0.25f).SetEase(Ease.OutQuad);
    }

    void DeselectTube()
    {
        if (selectedTube == null) return;
        selectedTube.transform.DOMove(selectedTube.originalPosition, 0.25f).SetEase(Ease.InQuad);
        selectedTube = null;
    }

    IEnumerator PourSequence(TubeController src, TubeController dst)
    {
        isBusy = true;

        // 1. TÍNH TOÁN LƯỢNG NƯỚC SẼ RÓT (Multi-stack)
        Color colorToMove = src.GetTopColor();
        int srcCount = src.GetTopColorCount(); // Có bao nhiêu lớp cùng màu ở chai nguồn?
        int dstSpace = dst.GetFreeSpace();     // Chai đích còn bao nhiêu chỗ trống?
        
        // Rót max có thể (Min giữa lượng có và lượng chứa được)
        int amountToPour = Mathf.Min(srcCount, dstSpace);

        // 2. DI CHUYỂN & XOAY CHAI
        bool isRight = dst.transform.position.x > src.transform.position.x;
        float offsetDir = isRight ? -2.0f : 2.0f; // Đứng lệch trái hay phải
        Vector3 targetPos = dst.mouthPos.position + new Vector3(offsetDir, 0.8f, 0);

        yield return src.transform.DOMove(targetPos, 0.4f).SetEase(Ease.OutQuad).WaitForCompletion();
        
        Tween rotateTween = src.transform.DORotate(new Vector3(0, 0, isRight ? -50f : 50f), 0.3f);
        yield return rotateTween.WaitForCompletion();

        // 3. HIỆU ỨNG TIA NƯỚC (LINE RENDERER)
        GameEvents.OnPourStarted?.Invoke();
        if (src.lineRenderer != null)
        {
            src.lineRenderer.enabled = true;
            src.lineRenderer.startColor = colorToMove;
            src.lineRenderer.endColor = colorToMove;
            
            src.lineRenderer.SetPosition(0, src.mouthPos.position);
            Vector3 dropPoint = dst.mouthPos.position; 
            dropPoint.y -= 0.5f; 
            src.lineRenderer.SetPosition(1, dropPoint);
            
            src.lineRenderer.widthMultiplier = 1f; 
        }

        // Thời gian rót tuỳ thuộc vào lượng nước (rót nhiều thì lâu hơn tí)
        float pourDuration = 0.5f + (amountToPour * 0.1f);
        yield return new WaitForSeconds(pourDuration);

        // 4. CHUYỂN DỮ LIỆU
        // Chuyển từng đơn vị nước một
        for (int i = 0; i < amountToPour; i++)
        {
            src.waterStack.RemoveAt(src.waterStack.Count - 1);
            dst.waterStack.Add(colorToMove);
        }

        // Tắt effect tia nước
        if (src.lineRenderer != null) src.lineRenderer.enabled = false;

        // Cập nhật lại hình ảnh 2 chai
        src.UpdateVisuals();
        dst.UpdateVisuals();

        GameEvents.OnPourCompleted?.Invoke();

        // 5. TRẢ CHAI VỀ CHỖ CŨ
        src.transform.DORotate(Vector3.zero, 0.3f);
        yield return src.transform.DOMove(src.originalPosition, 0.4f).SetEase(Ease.InQuad).WaitForCompletion();

        // 6. KIỂM TRA HOÀN THÀNH CHAI ĐÍCH (Đóng nắp)
        if (dst.IsCompleted())
        {
            dst.CloseTube(); // Gọi animation nắp chai rơi xuống
            AudioManager.Instance.PlayWinSound(); 
        }

        // 7. KIỂM TRA THẮNG / THUA
        if (CheckWinCondition())
        {
            HandleWin();
        }
        else if (CheckLoseCondition())
        {
            HandleLose();
        }

        isBusy = false;
    }

    // --- HỆ THỐNG CHECK WIN/LOSE ---

    bool CheckWinCondition()
    {
        return allTubes.All(tube => tube.IsEmpty() || tube.IsCompleted());
    }

    bool CheckLoseCondition()
    {
        // Kiểm tra xem còn nước đi nào hợp lệ không?
        foreach (var src in allTubes)
        {
            if (src.IsEmpty()) continue; // Chai rỗng ko rót được
            if (src.IsCompleted()) continue; // Chai đã đóng nắp ko rót được nữa

            Color colorToMove = src.GetTopColor();

            foreach (var dst in allTubes)
            {
                if (src == dst) continue;
                // Nếu tìm thấy 1 cặp rót được -> Chưa thua
                if (dst.CanReceive(colorToMove)) return false; 
            }
        }
        // Duyệt hết mà không thấy đường đi nào -> Thua
        return true; 
    }

    void HandleWin()
    {
        Debug.Log("WIN!");
        isBusy = true; // Chặn input
        if(winPanel) winPanel.SetActive(true);
        GameEvents.OnLevelCompleted?.Invoke();
        
        currentLevel++;
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.Save();
        
    }

    void HandleLose()
    {
        Debug.Log("LOSE!");
        isBusy = true; 
        if(losePanel) losePanel.SetActive(true);
    }
}