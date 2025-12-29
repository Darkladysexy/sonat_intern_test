using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelGenerator : MonoBehaviour
{
    public List<Color> colorPalette; 

    public int GetColorsForLevel(int currentLevel)
    {
        if (currentLevel <= 5) return 2;
        if (currentLevel <= 20) return 3;
        if (currentLevel <= 50) return 4;
        return Mathf.Min(5, colorPalette.Count); 
    }

    public void GenerateLevel(List<TubeController> allTubes, int numColors)
    {
        numColors = Mathf.Min(numColors, colorPalette.Count);
        
        // Chỉ thao tác trên các chai sẽ chứa nước (trừ 2 chai cuối)
        int tubesWithWater = allTubes.Count - 2;
        if (tubesWithWater < numColors) numColors = tubesWithWater;

        // 1. TẠO DANH SÁCH MÀU (4 đốt cho mỗi màu)
        List<Color> allUnits = new List<Color>();
        for (int i = 0; i < tubesWithWater; i++)
        {
            Color colorToAdd = colorPalette[i % numColors];
            for(int k = 0; k < 4; k++) allUnits.Add(colorToAdd);
        }

        // 2. TRỘN MÀU NGẪU NHIÊN
        Shuffle(allUnits);

        // 3. ĐỔ VÀO CHAI
        int unitIndex = 0;
        for (int i = 0; i < tubesWithWater; i++)
        {
            allTubes[i].waterStack.Clear();
            for (int j = 0; j < 4; j++)
            {
                if(unitIndex < allUnits.Count)
                    allTubes[i].waterStack.Add(allUnits[unitIndex++]);
            }
            allTubes[i].UpdateVisuals();
        }

        // Làm sạch 2 chai cuối
        allTubes[allTubes.Count - 2].waterStack.Clear();
        allTubes[allTubes.Count - 2].UpdateVisuals();
        allTubes[allTubes.Count - 1].waterStack.Clear();
        allTubes[allTubes.Count - 1].UpdateVisuals();

        // 4. BƯỚC QUAN TRỌNG: KIỂM TRA VÀ PHÁ VỠ CÁC CHAI HOÀN THÀNH
        ForceBreakCompletedTubes(allTubes, tubesWithWater);
    }

    // --- THUẬT TOÁN MỚI: CƯỠNG CHẾ PHÁ VỠ ---
    private void ForceBreakCompletedTubes(List<TubeController> tubes, int count)
    {
        int safetyLoop = 0; 
        bool hasIssue = true;

        // Lặp lại việc kiểm tra cho đến khi KHÔNG CÒN chai nào bị hoàn thành
        while (hasIssue && safetyLoop < 100)
        {
            hasIssue = false;
            safetyLoop++;

            for (int i = 0; i < count; i++)
            {
                // Nếu tìm thấy một chai đã hoàn thành (4 màu giống nhau)
                if (tubes[i].IsCompleted() && !tubes[i].IsEmpty())
                {
                    hasIssue = true; // Đánh dấu là vẫn còn lỗi để lặp lại kiểm tra
                    
                    // Tìm một chai khác để tráo đổi
                    // Điều kiện: Không phải chai hiện tại (i) VÀ có màu nắp KHÁC màu chai hiện tại
                    int targetIndex = FindSwapTarget(tubes, count, i);
                    
                    if (targetIndex != -1)
                    {
                        // Thực hiện tráo đổi lớp màu trên cùng
                        SwapTopColor(tubes[i], tubes[targetIndex]);
                    }
                }
            }
        }
    }

    // Tìm một chai thích hợp để tráo (có màu nắp khác)
    private int FindSwapTarget(List<TubeController> tubes, int count, int ignoreIndex)
    {
        Color badColor = tubes[ignoreIndex].GetTopColor();

        // Duyệt qua các chai khác để tìm ứng viên
        // Ta random điểm bắt đầu duyệt để game không bị lặp lại quy luật
        int startIndex = Random.Range(0, count);

        for (int k = 0; k < count; k++)
        {
            int index = (startIndex + k) % count;

            if (index == ignoreIndex) continue; // Bỏ qua chính nó

            // Chỉ tráo với chai có nước và màu nắp KHÁC màu chai đang bị lỗi
            if (!tubes[index].IsEmpty() && tubes[index].GetTopColor() != badColor)
            {
                return index;
            }
        }
        return -1; // Không tìm thấy (trường hợp cực hiếm)
    }

    private void SwapTopColor(TubeController tubeA, TubeController tubeB)
    {
        // Lấy màu đỉnh
        int indexA = tubeA.waterStack.Count - 1;
        int indexB = tubeB.waterStack.Count - 1;

        Color temp = tubeA.waterStack[indexA];
        tubeA.waterStack[indexA] = tubeB.waterStack[indexB];
        tubeB.waterStack[indexB] = temp;

        // Cập nhật lại hình ảnh ngay lập tức
        tubeA.UpdateVisuals();
        tubeB.UpdateVisuals();
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int r = Random.Range(i, list.Count);
            list[i] = list[r];
            list[r] = temp;
        }
    }
}