using UnityEngine;

public static class CreateGrid
{
    public static void CreateGridTiles(GameObject floorTile, Transform gameGrid, int width, int height)
    {
        GameObject[,] floorTiles = new GameObject[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x, 0, y); 
                GameObject cell = Object.Instantiate(floorTile, position, Quaternion.identity, gameGrid);
                cell.name = $"Cell({x},{y})";
                floorTiles[x, y] = cell;
            }
        }
    } 
}
