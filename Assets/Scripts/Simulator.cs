using System.Collections.Generic;
using UnityEngine;

public class Simulator : MonoBehaviour
{
    public Color pointColor;
    public Color lookedPointColor;
    public Color stickColor;

    public float stickWidth;
    public float pointRadius;

    [Space(5)]
    public float gravity;
    public int stickStabilization;

    List<Point> points = new List<Point>();
    List<Stick> sticks = new List<Stick>();

    bool simulating;
    bool drawingStick;
    int stickStartIndex;

    void Update()
    {
        PointGenerate();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            simulating = true;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject obj in objs)
            {
                Destroy(obj);
            }

            points.Clear();
            sticks.Clear();
        }

        if (simulating)
        {
            Simulate();
        }
    }

    void PointGenerate()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int i = MouseOverPointIndex(mousePos);
        bool mouseOverPoint = i != -1;

        if (Input.GetMouseButtonDown(1) && mouseOverPoint)
        {
            Point point = points[i];

            MeshRenderer mesh = point.myObject.GetComponent<MeshRenderer>();

            mesh.material.color = point.locked ? pointColor : lookedPointColor;
            point.locked = !point.locked;
            point.prevPosition = point.position;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (mouseOverPoint)
            {
                drawingStick = true;
                stickStartIndex = i;
            }
            else
            {
                Transform point = DrawPoint(mousePos, pointColor, pointRadius);

                points.Add(new Point(point.position, point.position, point));
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (mouseOverPoint && drawingStick)
            {
                if (i != stickStartIndex)
                {
                    Transform stick = DrawStick(points[stickStartIndex].myObject.position, points[i].myObject.position, stickColor, stickWidth);
                    sticks.Add(new Stick(points[stickStartIndex], points[i], stick));
                }
            }
            drawingStick = false;
        }
    }

    void Simulate()
    {
        foreach (Point p in points)
        {
            if (!p.locked)
            {
                Vector2 updateBeforePosition = p.position;

                p.position += p.position - p.prevPosition;
                p.position += Vector2.down * gravity * Time.deltaTime * Time.deltaTime;

                p.prevPosition = updateBeforePosition;

                p.myObject.position = p.position;
            }
        }

        for (int i = 0; i < stickStabilization; i++)
        {
            foreach (Stick s in sticks)
            {
                Vector2 stickCentre = (s.pointA.position + s.pointB.position) / 2;
                Vector2 stickDir = (s.pointA.position - s.pointB.position).normalized;

                if (!s.pointA.locked)
                {
                    s.pointA.position = stickCentre + stickDir * s.length / 2;
                }

                if (!s.pointB.locked)
                {
                    s.pointB.position = stickCentre - stickDir * s.length / 2;
                }

                s.myObject.GetComponent<LineRenderer>().SetPosition(0, s.pointA.position);
                s.myObject.GetComponent<LineRenderer>().SetPosition(1, s.pointB.position);
            }
        }
    }

    int MouseOverPointIndex(Vector2 mousePos)
    {
        for (int i = 0; i < points.Count; i++)
        {
            float dst = (points[i].position - mousePos).magnitude;

            if (dst < pointRadius)
            {
                return i;
            }
        }
        return -1;
    }

    Transform DrawPoint(Vector2 position, Color color, float pointRadius)
    {
        Shader pointShader = Shader.Find("Unlit/Color");

        Transform point = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        point.parent = transform;
        point.position = new Vector3(position.x, position.y, -5);
        point.tag = "Player";
        point.localScale = new Vector3(pointRadius, pointRadius) * 2f;

        MeshRenderer pointSR = point.GetComponent<MeshRenderer>();
        pointSR.material.shader = pointShader;
        pointSR.material.color = color;

        return point;
    }

    Transform DrawStick(Vector2 to, Vector2 from, Color color, float width)
    {
        Shader stickShader = Shader.Find("Unlit/Color");

        Transform stick = new GameObject("Line").transform;
        stick.parent = transform;
        stick.tag = "Player";

        LineRenderer line = stick.gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;

        line.SetPosition(0, to);
        line.SetPosition(1, from);

        line.startWidth = width;
        line.endWidth = width;

        line.material.shader = stickShader;
        line.material.color = color;

        return stick;
    }
}

[System.Serializable]
public class Point
{
    public Vector2 position, prevPosition;
    public Transform myObject;
    public bool locked;

    public Point(Vector2 position, Vector2 prevPosition, Transform myObject)
    {
        this.position = position;
        this.prevPosition = prevPosition;
        this.myObject = myObject;
    }
}

[System.Serializable]
public class Stick
{
    public Point pointA, pointB;
    public Transform myObject;
    public float length;

    public Stick(Point pointA, Point pointB, Transform myObject)
    {
        this.pointA = pointA;
        this.pointB = pointB;
        this.myObject = myObject;

        length = Vector2.Distance(pointA.position, pointB.position);
    }
}