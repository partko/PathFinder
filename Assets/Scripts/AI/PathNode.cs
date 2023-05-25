using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode //: MonoBehaviour
{
    public enum NodeState
    {
        Walkable,
        Active1,
        Active2,
        Obstrained,
    }

    public bool walkable => State != NodeState.Obstrained;
    
    public NodeState State; //  Свободна для перемещения
    public Vector3 worldPosition; //  Позиция в глобальных координатах
    private GameObject objPrefab; //  Шаблон объекта
    public GameObject body; //  Объект для отрисовки

    private PathNode parentNode = null; //  откуда пришли

    /// <summary>
    /// Родительская вершина - предшествующая текущей в пути от начальной к целевой
    /// </summary>
    public PathNode ParentNode
    {
        get => parentNode;
        set => SetParent(value);
    }

    private float distance = float.PositiveInfinity; //  расстояние от начальной вершины

    /// <summary>
    /// Расстояние от начальной вершины до текущей (+infinity если ещё не развёртывали)
    /// </summary>
    public float Distance
    {
        get => distance;
        set => distance = value;
    }

    /// <summary>
    /// Устанавливаем родителя и обновляем расстояние от него до текущей вершины. Неоптимально - дважды расстояние считается
    /// </summary>
    /// <param name="parent"></param>
    private void SetParent(PathNode parent)
    {
        //  Указываем родителя
        parentNode = parent;
        //  Вычисляем расстояние
        if (parent != null)
            distance = parent.Distance + Vector3.Distance(body.transform.position, parent.body.transform.position);
        else
            distance = float.PositiveInfinity;
    }

    /// <summary>
    /// Конструктор вершины
    /// </summary>
    /// <param name="_objPrefab">объект, который визуализируется в вершине</param>
    /// <param name="state">проходима ли вершина</param>
    /// <param name="position">мировые координаты</param>
    public PathNode(GameObject _objPrefab, NodeState state, Vector3 position)
    {
        objPrefab = _objPrefab;
        State = state;
        worldPosition = position;
        body = GameObject.Instantiate(objPrefab, worldPosition, Quaternion.identity);
    }

    /// <summary>
    /// Расстояние между вершинами - разброс по высоте учитывается дополнительно
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static float Dist(PathNode a, PathNode b)
    {
        var positiona = a.body.transform.position;
        var positionb = b.body.transform.position;
        return Vector3.Distance(positiona, positionb) +
               40 * Mathf.Abs(positiona.y - positionb.y);
    }

    /// <summary>
    /// Снять подсветку с вершины - перекрасить в синий
    /// </summary>
    public void Fade()
    {
        if (State == NodeState.Obstrained)
            return;
        SetState(NodeState.Walkable);
    }

    public void Illuminate(NodeState state = NodeState.Active1)
    {
        if (State == NodeState.Obstrained)
            return;
        SetState(state);
    }

    public void SetState(NodeState state)
    {
        State = state;
        switch (state)
        {
            case NodeState.Walkable:
                body.GetComponent<Renderer>().material.color = Color.blue;
                break;

            case NodeState.Obstrained:
                body.GetComponent<Renderer>().material.color = Color.magenta;
                break;

            case NodeState.Active1:
                body.GetComponent<Renderer>().material.color = Color.white;
                break;

            case NodeState.Active2:
                body.GetComponent<Renderer>().material.color = Color.green;
                break;
        }
    }
}