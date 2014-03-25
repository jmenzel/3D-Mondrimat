using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class WireFrameLineRenderer : MonoBehaviour
{
    //****************************************************************************************
    //  Material options
    //****************************************************************************************
    public float lineWidth;
    public Color LineColor;
    //public bool ZWrite = true;
    //public bool AWrite = true;
    //public bool Blend = true;

    //****************************************************************************************
    // Line Data
    //****************************************************************************************
    private Vector3[] Lines;
    private List<Line> LinesArray = new List<Line>();
    private Material LineMaterial;


    //*****************************************************************************************
    // Helper class, Line is defined as two Points
    //*****************************************************************************************
    public class Line
    {
        protected bool Equals(Line other)
        {
            return PointA.Equals(other.PointA) && PointB.Equals(other.PointB);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Line) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (PointA.GetHashCode()*397) ^ PointB.GetHashCode();
            }
        }

        public readonly Vector3 PointA;
        public Vector3 PointB;

        public Line(Vector3 a, Vector3 b)
        {
            PointA = a;
            PointB = b;
        }

        //*****************************************************************************************
        // A == B if   Aa&&Ab == Ba&&Bb or Ab&&Ba == Aa && Bb
        //*****************************************************************************************
        public static bool operator ==(Line lA, Line lB)
        {
            if (lA.PointA == lB.PointA && lA.PointB == lB.PointB)
            {
                return true;
            }

            if (lA.PointA == lB.PointB && lA.PointB == lB.PointA)
            {
                return true;
            }


            return false;
        }

        //*****************************************************************************************
        // A != B if   !(Aa&&Ab == Ba&&Bb or Ab&&Ba == Aa && Bb)
        //*****************************************************************************************
        public static bool operator !=(Line lA, Line lB)
        {
            return !(lA == lB);
        }
    }

    //*****************************************************************************************
    // Parse the mesh this is attached to and save the line data
    //*****************************************************************************************
    public void Start()
    {
        LineMaterial = new Material("Shader \"Lines/Colored Blended\" { SubShader { Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Front Fog { Mode Off } } } }");
        LineMaterial.hideFlags = HideFlags.HideAndDontSave;
        LineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;

        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh mesh = filter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length / 3; i++)
        {
            int j = i * 3;
            Line lineA = new Line(vertices[triangles[j]], vertices[triangles[j + 1]]);
            Line lineB = new Line(vertices[triangles[j + 1]], vertices[triangles[j + 2]]);
            Line lineC = new Line(vertices[triangles[j + 2]], vertices[triangles[j]]);


            var distanceA = Vector3.Distance(lineA.PointA, lineA.PointB);
            var distanceB = Vector3.Distance(lineB.PointA, lineB.PointB);
            var distanceC = Vector3.Distance(lineC.PointA, lineC.PointB);

            var max = Math.Max(Math.Max(distanceA, distanceB), distanceC);

            if(max == distanceA)
            {
                AddLine(lineB);
                AddLine(lineC);
            }
            else if(max == distanceB)
            {
                AddLine(lineA);
                AddLine(lineC);
            }
            else
            {
                AddLine(lineA);
                AddLine(lineB);
            }
            /*
            if (Fidelity == 3)
            {
                AddLine(lineA);
                AddLine(lineB);
                AddLine(lineC);
            }
            else if (Fidelity == 2)
            {
                AddLine(lineA);
                AddLine(lineB);
            }
            else if (Fidelity == 1)
            {
                AddLine(lineA);
            }
             */
        }
    }

    //****************************************************************************************
    // Adds a line to the array if the equivalent line isn't stored already
    //****************************************************************************************
    public void AddLine(Line l)
    {
        bool found = false;
        foreach (Line line in LinesArray)
        {
            if (l == line)
            { found = true; break; }
        }

        if (!found)
        { LinesArray.Add(l); }
    }

    //****************************************************************************************
    // Deferred rendering of wireframe, this should let materials go first
    //****************************************************************************************
    public void OnRenderObject()
    {
        LineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Color(LineColor);

        if (lineWidth == 1F)
        {
            GL.Begin(GL.LINES);

            foreach (Line line in LinesArray)
            {
                GL.Vertex(line.PointA);
                GL.Vertex(line.PointB);
            }
        }
        else
        {
            GL.Begin(GL.QUADS);
            foreach (Line line in LinesArray)
            {
                DrawQuad(line.PointA, line.PointB);
            }
        }

        GL.End();
        GL.PopMatrix();
    }

    public void DrawQuad(Vector3 p1, Vector3 p2)
    {
        var thisWidth = 1.0F/Screen.width * lineWidth * .5F;
        var edge1 = (p2 + p1); // Camera.main.transform.positions - (p2 + p1) / 2.0F;	//vector from line center to camera
        var edge2 = p2-p1;	//vector from point to point
        var perpendicular = Vector3.Cross(edge1,edge2).normalized * thisWidth;
 
        GL.Vertex(p1 - perpendicular);
        GL.Vertex(p1 + perpendicular);
        GL.Vertex(p2 + perpendicular);
        GL.Vertex(p2 - perpendicular);
    }
}