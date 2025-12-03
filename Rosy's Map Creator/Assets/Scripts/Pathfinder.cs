using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Pathfinder : MonoBehaviour
{

    public static List<HexTileData> FindPath(HexTileData originTile, HexTileData destinationTile)
    {
        Dictionary<HexTileData, Node> unprocessedNodes = new Dictionary<HexTileData, Node>();
        Dictionary<HexTileData, Node> processedNodes = new Dictionary<HexTileData, Node>();

        Node startNode = new Node(originTile, originTile, destinationTile, 0);
        unprocessedNodes.Add(originTile, startNode);

        bool foundPath = EvaluateNextNode(unprocessedNodes, processedNodes, originTile, destinationTile, out List<HexTileData> path);

        while (!foundPath)
        {
            foundPath = EvaluateNextNode(unprocessedNodes, processedNodes, originTile, destinationTile, out path);
        }

        return path;
    }

    private static bool EvaluateNextNode(Dictionary<HexTileData, Node> unprocessedNodes, Dictionary<HexTileData, Node> processedNodes, HexTileData originTile, HexTileData destinationTile, out List<HexTileData> path)
    {
        Node currentNode = GetCheapestNode(unprocessedNodes.Values.ToArray());

        if (currentNode == null)
        {
            path = new List<HexTileData>();
            return true;
        }

        unprocessedNodes.Remove(currentNode.targetTile);
        processedNodes.Add(currentNode.targetTile, currentNode);

        path = new List<HexTileData>();

        if (currentNode.targetTile == destinationTile)
        {
            // Walk back from destination to origin
            while (currentNode != null)
            {
                path.Add(currentNode.targetTile);

                if (currentNode.targetTile == originTile)
                    break;

                currentNode = currentNode.parent;
            }

            // Now path is [dest, ..., origin] → reverse it
            path.Reverse(); // path[0] = origin, last = destination
            return true;
        }


        List<Node> neighborNodes = new List<Node>();
        foreach (HexTileData neighbor in currentNode.targetTile.neighbors)
        {
            Node node = new Node(neighbor, originTile, destinationTile, currentNode.GetCost());
            if (!neighbor.isTraversable || processedNodes.ContainsKey(neighbor))
            {
                //node.baseCost = 9999999;
                continue;
            }
            neighborNodes.Add(node);
        }

        foreach (Node neighbor in neighborNodes)
        {
            if (neighbor.GetCost() < currentNode.GetCost() || !unprocessedNodes.ContainsKey(neighbor.targetTile))
            {
                neighbor.SetParent(currentNode);
                if (!unprocessedNodes.ContainsKey(neighbor.targetTile))
                {
                    unprocessedNodes.Add(neighbor.targetTile, neighbor);
                }
            }
        }
        return false;
    }

    private static Node GetCheapestNode(Node[] unprocessedNodes)
    {
        if (unprocessedNodes.Length == 0)
        {
            return null;
        }

        Node cheapestNode = unprocessedNodes[0];

        foreach (Node node in unprocessedNodes)
        {
            if (node.GetCost() < cheapestNode.GetCost())
            {
                cheapestNode = node;
            }
            else if (node.GetCost() == cheapestNode.GetCost() && node.costToDestination < cheapestNode.costToDestination)
            {

                cheapestNode = node;
            }
        }

        return cheapestNode;
    }


}