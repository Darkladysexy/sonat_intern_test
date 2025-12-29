using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public List<Color> colorPalette; 

    public void GenerateLevel(List<TubeController> allTubes, int numColors)
    {
        List<Color> allUnits = new List<Color>();
        
        for (int i = 0; i < numColors; i++)
        {
            for (int j = 0; j < 4; j++) 
            {
                allUnits.Add(colorPalette[i]);
            }
        }
        for (int i = 0; i < allUnits.Count; i++)
        {
            Color temp = allUnits[i];
            int randomIndex = Random.Range(i, allUnits.Count);
            allUnits[i] = allUnits[randomIndex];
            allUnits[randomIndex] = temp;
        }
        int unitIndex = 0;
        for (int i = 0; i < allTubes.Count - 2; i++)
        {
            allTubes[i].waterStack.Clear(); 
            for (int j = 0; j < 4; j++)
            {
                allTubes[i].waterStack.Add(allUnits[unitIndex++]); 
            }
            allTubes[i].UpdateVisuals();
        }

        allTubes[allTubes.Count - 2].waterStack.Clear();
        allTubes[allTubes.Count - 1].waterStack.Clear();
        allTubes[allTubes.Count - 2].UpdateVisuals();
        allTubes[allTubes.Count - 1].UpdateVisuals();
    }
}