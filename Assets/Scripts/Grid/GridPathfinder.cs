using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder
{
    Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private class Node
    {
        public Vector2Int pos;
        public Node parent;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;

        public Node(Vector2Int position)
        {
            pos = position;
        }
    }

    public List<Vector2Int> FindPath (GridManager gridManager, Vector2Int start, Vector2Int end)
    {
        var mapGrid = gridManager.GetMapGrid();

        if (!mapGrid.ContainsKey(start) || !mapGrid.ContainsKey(end)) return null;

        List<Vector2Int> validStarts = FindAdjacentRoad(start, gridManager);
        List<Vector2Int> validEnds = FindAdjacentRoad(end, gridManager);

        if (validStarts.Count == 0 ||  validEnds.Count == 0) return null;

        //A* algorithm

        List<Node> openSet = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        foreach (Vector2Int roadStart in validStarts)
        {
            Node startNode = new Node(roadStart)
            {
                GCost = 0,
                HCost = GetManhattanDistance(roadStart, validEnds)
            };
            openSet.Add(startNode);
        }


        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.pos);

            if (validEnds.Contains(currentNode.pos))
            {
                return RetracePath(currentNode);
            }

            foreach(Vector2Int dir in directions)
            {
                Vector2Int neighbourPos = currentNode.pos + dir;

                if (closedSet.Contains(neighbourPos)) continue;

                if (!mapGrid.TryGetValue(neighbourPos, out GridManager.GridTile neighbourTile) || !neighbourTile.isRoad)
                {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.GCost + 1;
                Node neighbourNode = openSet.Find(n => n.pos == neighbourPos);

                if (neighbourNode == null)
                {
                    neighbourNode = new Node(neighbourPos)
                    {
                        GCost = newMovementCostToNeighbour,
                        HCost = GetManhattanDistance(neighbourPos, validEnds),
                        parent = currentNode
                    };
                    openSet.Add(neighbourNode);
                } else if (newMovementCostToNeighbour < neighbourNode.GCost)
                {
                    neighbourNode.GCost = newMovementCostToNeighbour;
                    neighbourNode.parent = currentNode;
                }
            }
        }

        return null;
    }

    private List<Vector2Int> FindAdjacentRoad(Vector2Int building, GridManager gridManager)
    {
        List<Vector2Int> roads = new List<Vector2Int>();

        foreach (Vector2Int dir in directions)
        {
            Vector2Int checkpos = building + dir;
            if (gridManager.GetMapGrid().TryGetValue(checkpos, out GridManager.GridTile tile) && tile.isRoad)
            {
                roads.Add(checkpos);
            }
        }

        return roads;
    }

    private static List<Vector2Int> RetracePath (Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(currentNode.pos);
            currentNode = currentNode.parent;
        }
        
        path.Reverse();
        return path;
    }

    private static int GetManhattanDistance(Vector2Int a, List<Vector2Int> targets)
    {
        int minDistance = int.MaxValue;
        foreach ( Vector2Int target in targets)
        {
            int dist = Mathf.Abs(a.x - target.x) + Mathf.Abs(a.y - target.y);
            if (dist < minDistance)
            {
                minDistance = dist;
            }
        }
        return minDistance;
    }

}
