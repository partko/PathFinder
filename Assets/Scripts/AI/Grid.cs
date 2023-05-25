using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

public class Grid : MonoBehaviour
{
    //  Модель для отрисовки узла сетки
    public GameObject nodeModel;

    //  Ландшафт (Terrain) на котором строится путь
    [SerializeField] private Terrain landscape = null;

    //  Шаг сетки (по x и z) для построения точек
    [SerializeField] private int gridDelta = 20;

    //  Номер кадра, на котором будет выполнено обновление путей
    private int updateAtFrame = 0;

    //  Массив узлов - создаётся один раз, при первом вызове скрипта
    private PathNode[,] grid = null;

    private void CheckWalkableNodes()
    {
        foreach (var node in grid)
        {
            //  Пока что считаем все вершины проходимыми, без учёта препятствий
            // node.walkable = true;
            node.SetState(Physics.CheckSphere(node.body.transform.position, 1)
                ? PathNode.NodeState.Obstrained
                : PathNode.NodeState.Walkable);

            if (node.walkable)
                node.Fade();
            else
            {
                node.Illuminate();
            }
        }
    }


    // Метод вызывается однократно перед отрисовкой первого кадра
    void Start()
    {
        //  Создаём сетку узлов для навигации - адаптивную, под размер ландшафта
        var terrainSize = landscape.terrainData.bounds.size;
        var sizeX = (int) (terrainSize.x / gridDelta);
        var sizeZ = (int) (terrainSize.z / gridDelta);
        //  Создаём и заполняем сетку вершин, приподнимая на 25 единиц над ландшафтом
        grid = new PathNode[sizeX, sizeZ];
        for (int x = 0; x < sizeX; ++x)
        {
            for (int z = 0; z < sizeZ; ++z)
            {
                Vector3 position = new Vector3(x * gridDelta, 0, z * gridDelta);
                position.y = landscape.SampleHeight(position) + 25;
                grid[x, z] = new PathNode(nodeModel, PathNode.NodeState.Walkable, position);
                grid[x, z].ParentNode = null;
                grid[x, z].Fade();
            }
        }
    }

    /// <summary>
    /// Получение списка соседних узлов для вершины сетки
    /// </summary>
    /// <param name="current">индексы текущей вершины </param>
    /// <returns></returns>
    private List<(Vector2Int, float)> GetNeighbours(Vector2Int current)
    {
        var nodes = new List<(Vector2Int, float)>();
        for (var dx = -1; dx <= 1; ++dx)
        for (var dy = -1; dy <= 1; ++dy)
        {
            if (dx == 0 && dy == 0)
                continue;
            var x = current.x + dx;
            var y = current.y + dy;
            if (x >= 0 && y >= 0
                       && x < grid.GetLength(0)
                       && y < grid.GetLength(1)
               )
                nodes.Add((new Vector2Int(x, y), PathNode.Dist(grid[current.x, current.y], grid[x, y])));

            // const int jumpDist = 9;
            // var x10 = current.x + dx * jumpDist;
            // var y10 = current.y + dy * jumpDist;
            // if (x10 >= jumpDist && y10 >= jumpDist
            //                     && x10 < grid.GetLength(0) - jumpDist
            //                     && y10 < grid.GetLength(1) - jumpDist
            //    )
            //     nodes.Add((new Vector2Int(x10, y10),
            //         PathNode.Dist(grid[current.x, current.y], grid[x10, y10]) * (jumpDist + 1) / jumpDist));
        }

        return nodes;
    }

    /// <summary>
    /// Вычисление "кратчайшего" между двумя вершинами сетки
    /// </summary>
    /// <param name="startNode">Координаты начального узла пути (индексы элемента в массиве grid)</param>
    /// <param name="finishNode">Координаты конечного узла пути (индексы элемента в массиве grid)</param>
    void CalculatePathAstar(Vector2Int startNode, Vector2Int finishNode)
    {
        foreach (var node in grid)
        {
            node.ParentNode = null;
            node.Distance = float.PositiveInfinity;
        }

        var start = grid[startNode.x, startNode.y];

        start.ParentNode = null;
        start.Distance = 0;

        var nodes = new PriorityQueue<Vector2Int>();
        nodes.Enqueue(PathNode.Dist(grid[startNode.x, startNode.y], grid[finishNode.x, finishNode.y]), startNode);

        var visited = new HashSet<Vector2Int>();

        while (nodes.Count > 0 && visited.Count != grid.Length)
        {
            var (heur0, current) = nodes.Dequeue();
            if (current == finishNode)
                break;
            if (visited.Contains(current))
                continue;

            var neighbours = GetNeighbours(current);
            foreach (var (neighbor, neighborDist) in neighbours)
            {
                if (!grid[neighbor.x, neighbor.y].walkable)
                    continue;

                var newDist = grid[current.x, current.y].Distance + neighborDist;
                //PathNode.Dist(grid[node.x, node.y], grid[current.x, current.y]);

                if (grid[neighbor.x, neighbor.y].Distance > newDist)
                {
                    grid[neighbor.x, neighbor.y].ParentNode = grid[current.x, current.y];
                    grid[neighbor.x, neighbor.y].Distance = newDist;
                    var heur = newDist + PathNode.Dist(grid[neighbor.x, neighbor.y], grid[finishNode.x, finishNode.y]);
                    nodes.Enqueue(heur, neighbor);
                }
            }

            visited.Add(current);
        }

        //  Восстанавливаем путь от целевой к стартовой
        var pathElem = grid[finishNode.x, finishNode.y];
        print($"Astar: {pathElem.Distance}");
        while (pathElem != null)
        {
            pathElem.Illuminate();
            pathElem = pathElem.ParentNode;
        }
    }


    /// <summary>
    /// Вычисление "кратчайшего" между двумя вершинами сетки
    /// </summary>
    /// <param name="startNode">Координаты начального узла пути (индексы элемента в массиве grid)</param>
    /// <param name="finishNode">Координаты конечного узла пути (индексы элемента в массиве grid)</param>
    void CalculatePathDijkstra(Vector2Int startNode, Vector2Int finishNode)
    {
        foreach (var node in grid)
        {
            node.ParentNode = null;
            node.Distance = float.PositiveInfinity;
        }

        var start = grid[startNode.x, startNode.y];

        start.ParentNode = null;
        start.Distance = 0;

        var nodes = new PriorityQueue<Vector2Int>();
        nodes.Enqueue(0, startNode);

        var visited = new HashSet<Vector2Int>();

        while (nodes.Count > 0 && visited.Count != grid.Length)
        {
            var (dist, current) = nodes.Dequeue();
            if (current == finishNode)
                break;
            if (visited.Contains(current))
                continue;

            var neighbours = GetNeighbours(current);
            foreach (var (node, neighborDist) in neighbours)
            {
                if (!grid[node.x, node.y].walkable)
                    continue;

                var newDist = dist + neighborDist; //PathNode.Dist(grid[node.x, node.y], grid[current.x, current.y]);

                if (grid[node.x, node.y].Distance > newDist)
                {
                    grid[node.x, node.y].ParentNode = grid[current.x, current.y];
                    grid[node.x, node.y].Distance = newDist;
                    nodes.Enqueue(newDist, node);
                }
            }

            visited.Add(current);
        }

        //  Восстанавливаем путь от целевой к стартовой
        var pathElem = grid[finishNode.x, finishNode.y];
        print($"Dijkstra: {pathElem.Distance}");
        while (pathElem != null)
        {
            pathElem.Illuminate(PathNode.NodeState.Active2);
            pathElem = pathElem.ParentNode;
        }
    }

    // Метод вызывается каждый кадр
    void Update()
    {
        //  Чтобы не вызывать этот метод каждый кадр, устанавливаем интервал вызова в 1000 кадров
        if (Time.frameCount < updateAtFrame) return;
        updateAtFrame = Time.frameCount + 100;

        CheckWalkableNodes();
        CalculatePathAstar(new Vector2Int(0, 0), new Vector2Int(grid.GetLength(0) - 1, grid.GetLength(1) - 1));
        CalculatePathDijkstra(new Vector2Int(0, 0), new Vector2Int(grid.GetLength(0) - 1, grid.GetLength(1) - 1));
    }
}