using System.Collections.Generic;
using UnityEngine;
public static class Utilities
{
    public static Vector3Int OffsetToCube(Vector2Int offset)
    {
        return OffsetToCubeOddR(offset);
    }

    public static Vector3Int OffsetToCubeOddR(Vector2Int offset)
    {
        int row = offset.y;
        int col = offset.x;
        int q = col - (row - (row & 1)) / 2;
        int r = row;
        return new Vector3Int(q, r, -q - r);
    }

    public static Vector3Int OffsetToCubeEvenR(Vector2Int offset)
    {
        int row = offset.y;
        int col = offset.x;
        int q = col - (row + (row & 1)) / 2;
        int r = row;
        return new Vector3Int(q, r, -q - r);
    }

    public static Vector3 GetPositionForHexFromCoordinate(int column, int row, int chunkSize, float radius = 1f, bool isFlatTopped = false)
    {
        float width, height, xPosition, yPosition, horizontalDistance, verticalDistance, offset;
        bool shouldOffset;
        float size = radius;

        if (!isFlatTopped)
        {

            shouldOffset = row / (chunkSize == 0 ? 1 : chunkSize) % 2 == 0;

            width = Mathf.Sqrt(3) * size;
            height = 2f * size;

            horizontalDistance = width;
            verticalDistance = height * (3f / 4f);

            offset = shouldOffset ? width / 2 : 0;
            xPosition = (column * horizontalDistance) + offset;
            yPosition = row * verticalDistance;
        }
        else
        {
            shouldOffset = (column % 2) == 0;
            width = 2f * size;
            height = Mathf.Sqrt(3f) * size;

            horizontalDistance = width * (3f / 4f);
            verticalDistance = height;

            offset = shouldOffset ? height / 2 : 0;
            xPosition = column * horizontalDistance;
            yPosition = (row * verticalDistance) - offset;
        }

        return new Vector3(xPosition, 0, -yPosition);
    }



}
