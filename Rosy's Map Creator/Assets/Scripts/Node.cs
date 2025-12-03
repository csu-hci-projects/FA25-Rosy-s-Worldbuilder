

using UnityEngine;

public class Node
{
    public Node parent;
    public HexTileData targetTile;
    public HexTileData destinationTile;
    public HexTileData originTile;

    public int baseCost;
    public int costFromOrigin;
    public int costToDestination;
    public int pathCost;

    public Node(HexTileData current, HexTileData origin, HexTileData destination, int pathCost)
    {
        parent = null;
        this.targetTile = current;
        this.originTile = origin;
        this.destinationTile = destination;

        baseCost = 1;
        costFromOrigin = (int)Vector3.Distance(current.tileCoordinates, origin.tileCoordinates);
        costToDestination = (int)Vector3.Distance(current.tileCoordinates, destination.tileCoordinates);

        this.pathCost = pathCost;
    }


    public int GetCost()
    {
        return pathCost + baseCost + costFromOrigin + costToDestination;
    }

    public void SetParent(Node parent)
    {
        this.parent = parent;
    }
}