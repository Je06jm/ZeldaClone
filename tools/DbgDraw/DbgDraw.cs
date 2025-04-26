using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class DbgDraw : Node3D
{
    private ImmediateMesh im;
    private MeshInstance3D mesh;

    static private void PushVertex(ImmediateMesh im, Vector3 position, Color color) {
        im.SurfaceSetColor(color);
        im.SurfaceAddVertex(position);
    }

    static private void PushVertex(ImmediateMesh im, Vector3 position, Color color, Transform3D transform) {
        position = transform * position;
        PushVertex(im, position, color);
    }

    private abstract class DrawShape {
        abstract public void Draw(ImmediateMesh im);
    };

    private class DrawShapePoints : DrawShape {
        private List<Vector3> points;
        private Color color;

        public DrawShapePoints(List<Vector3> points, Color color) {
            this.points = points;
            this.color = color;
        }

        public override void Draw(ImmediateMesh im)
        {
            im.SurfaceBegin(Mesh.PrimitiveType.Points);

            foreach (var point in points) {
                PushVertex(im, point, color);
            }

            im.SurfaceEnd();
        }

    };

    private class DrawShapeLines : DrawShape {
        private List<Vector3> positions;
        private Color color;

        public DrawShapeLines(List<Vector3> positions, Color color) {
            this.positions = positions;
            this.color = color;
        }

        public override void Draw(ImmediateMesh im)
        {
            im.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

            foreach (var point in positions) {
                PushVertex(im, point, color);
            }

            im.SurfaceEnd();
        }
    };

    private class DrawShapeCube : DrawShape {
        private Transform3D transform;
        private Color color;

        public DrawShapeCube(Transform3D transform, Color color) {
            this.transform = transform;
            this.color = color;
        }

        public override void Draw(ImmediateMesh im)
        {
            Vector3 v000 = transform * new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 v001 = transform * new Vector3(-0.5f, -0.5f,  0.5f);
            Vector3 v010 = transform * new Vector3(-0.5f,  0.5f, -0.5f);
            Vector3 v011 = transform * new Vector3(-0.5f,  0.5f,  0.5f);
            Vector3 v100 = transform * new Vector3( 0.5f, -0.5f, -0.5f);
            Vector3 v101 = transform * new Vector3( 0.5f, -0.5f,  0.5f);
            Vector3 v110 = transform * new Vector3( 0.5f,  0.5f, -0.5f);
            Vector3 v111 = transform * new Vector3( 0.5f,  0.5f,  0.5f);

            im.SurfaceBegin(Mesh.PrimitiveType.Lines);

            PushVertex(im, v000, color);
            PushVertex(im, v010, color);

            PushVertex(im, v001, color);
            PushVertex(im, v011, color);

            PushVertex(im, v100, color);
            PushVertex(im, v110, color);

            PushVertex(im, v101, color);
            PushVertex(im, v111, color);


            PushVertex(im, v000, color);
            PushVertex(im, v001, color);

            PushVertex(im, v010, color);
            PushVertex(im, v011, color);

            PushVertex(im, v100, color);
            PushVertex(im, v101, color);

            PushVertex(im, v110, color);
            PushVertex(im, v111, color);


            PushVertex(im, v000, color);
            PushVertex(im, v100, color);

            PushVertex(im, v001, color);
            PushVertex(im, v101, color);

            PushVertex(im, v010, color);
            PushVertex(im, v110, color);

            PushVertex(im, v011, color);
            PushVertex(im, v111, color);

            im.SurfaceEnd();
        }
    };

    private class DrawShapeSphere : DrawShape {
        private Transform3D transform;
        private Color color;

        public DrawShapeSphere(Transform3D transform, Color color) {
            this.transform = transform;
            this.color = color;
        }

        public override void Draw(ImmediateMesh im)
        {
            const int segments = 32;

            im.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

            List<float> cos = new List<float>();
            List<float> sin = new List<float>();

            for (int i = 0; i <= segments; i++) {
                float p = (float)i / (float)segments;
                p *= 2.0f * (float)Math.PI;

                float c = (float)Math.Cos(p);
                float s = (float)Math.Sin(p);

                cos.Add(c);
                sin.Add(s);
            }

            for (int i = 0; i <= segments; i++) {
                Vector3 pos = new Vector3(cos[i], sin[i], 0.0f);

                PushVertex(im, pos, color, transform);
            }

            for (int i = 0; i <= segments; i++) {
                Vector3 pos = new Vector3(cos[i], 0.0f, sin[i]);

                PushVertex(im, pos, color, transform);
            }

            im.SurfaceEnd();
            im.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

            for (int i = 0; i <= segments; i++) {
                Vector3 pos = new Vector3(0.0f, cos[i], sin[i]);

                PushVertex(im, pos, color, transform);
            }

            im.SurfaceEnd();
        }
    };
    
    private class DrawShapeCylinder : DrawShape {
        private Transform3D transform;
        private float top_radius;
        private float bottom_radius;
        private float height;
        private Color color;

        public DrawShapeCylinder(Transform3D transform, float top_radius, float bottom_radius, float height, Color color) {
            this.transform = transform;
            this.top_radius = top_radius;
            this.bottom_radius = bottom_radius;
            this.height = height;
            this.color = color;
        }

        public override void Draw(ImmediateMesh im)
        {
            const int segments = 32;

            im.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

            float height_half = height / 2.0f;

            List<float> cos = new List<float>();
            List<float> sin = new List<float>();

            for (int i = 0; i <= segments; i++) {
                float p = (float)i / (float)segments;
                p *= 2.0f * (float)Math.PI;

                float c = (float)Math.Cos(p);
                float s = (float)Math.Sin(p);

                cos.Add(c);
                sin.Add(s);
            }

            for (int i = 0; i <= segments; i++) {
                Vector3 pos = new Vector3(cos[i], 0.0f, sin[i]) * top_radius;
                pos = pos with {Y = height_half};

                PushVertex(im, pos, color, transform);
            }

            for (int i = 0; i <= segments; i++) {
                Vector3 pos = new Vector3(cos[i], 0.0f, sin[i]) * bottom_radius;
                pos = pos with {Y = -height_half};

                PushVertex(im, pos, color, transform);
            }

            im.SurfaceEnd();

            im.SurfaceBegin(Mesh.PrimitiveType.Lines);

            PushVertex(im, new Vector3(-top_radius, height_half, 0.0f), color, transform);
            PushVertex(im, new Vector3(-bottom_radius, -height_half, 0.0f), color, transform);

            PushVertex(im, new Vector3(0.0f, height_half, top_radius), color, transform);
            PushVertex(im, new Vector3(0.0f, -height_half, bottom_radius), color, transform);

            PushVertex(im, new Vector3(0.0f, height_half, -top_radius), color, transform);
            PushVertex(im, new Vector3(0.0f, -height_half, -bottom_radius), color, transform);

            im.SurfaceEnd();
        }
    };

    private class DrawShapeCapsule : DrawShape {
        private Transform3D transform;
        private float top_radius;
        private float bottom_radius;
        private float height;
        private Color color;

        public DrawShapeCapsule(Transform3D transform, float top_radius, float bottom_radius, float height, Color color) {
            this.transform = transform;
            this.top_radius = top_radius;
            this.bottom_radius = bottom_radius;
            this.height = height;
            this.color = color;
        }

        public override void Draw(ImmediateMesh im)
        {
            var top_transform = Transform3D.Identity;
            var bottom_transform = Transform3D.Identity;

            top_transform = top_transform.Scaled(Vector3.One * top_radius);
            bottom_transform = bottom_transform.Scaled(Vector3.One * bottom_radius);

            top_transform.Origin = new Vector3(0.0f, (height / 2.0f) - top_radius, 0.0f);
            bottom_transform.Origin = new Vector3(0.0f, -((height / 2.0f) - bottom_radius), 0.0f);

            float top_height = top_transform.Origin.Y;
            float bottom_height = bottom_transform.Origin.Y;

            var center_transform = Transform3D.Identity;
            center_transform.Origin = (top_transform.Origin + bottom_transform.Origin) / 2.0f;

            top_transform = transform * top_transform;
            bottom_transform = transform * bottom_transform;
            center_transform = transform * center_transform;

            var top = new DrawShapeSphere(top_transform, color);
            var bottom = new DrawShapeSphere(bottom_transform, color);

            top.Draw(im);
            bottom.Draw(im);

            im.SurfaceBegin(Mesh.PrimitiveType.Lines);

            PushVertex(im, new Vector3(top_radius, top_height, 0.0f), color, center_transform);
            PushVertex(im, new Vector3(bottom_radius, bottom_height, 0.0f), color, center_transform);

            PushVertex(im, new Vector3(-top_radius, top_height, 0.0f), color, center_transform);
            PushVertex(im, new Vector3(-bottom_radius, bottom_height, 0.0f), color, center_transform);

            PushVertex(im, new Vector3(0.0f, top_height, top_radius), color, center_transform);
            PushVertex(im, new Vector3(0.0f, bottom_height, bottom_radius), color, center_transform);

            PushVertex(im, new Vector3(0.0f, top_height, -top_radius), color, center_transform);
            PushVertex(im, new Vector3(0.0f, bottom_height, -bottom_radius), color, center_transform);

            im.SurfaceEnd();
        }
    };

    static List<DrawShape> draw_shapes = new List<DrawShape>();

    static public void Cube(Transform3D transform, Vector3 size, Color color) {
        var scaled = Transform3D.Identity;
        scaled = scaled.Scaled(size);
        draw_shapes.Add(new DrawShapeCube(transform * scaled, color));
    }

    static public void Cube(Vector3 center, Quaternion rotation, Vector3 size, Color color) {
        var transform = Transform3D.Identity;
        transform.Origin = center;
        transform.Basis = new Basis(rotation) * transform.Basis;
        Cube(transform, size, color);
    }

    static public void Line(Vector3 begin, Vector3 end, Color color) {
        var points = new List<Vector3>();
        points.Add(begin);
        points.Add(end);

        draw_shapes.Add(new DrawShapeLines(points, color));
    }

    static public void Lines(List<Vector3> points, Color color) {
        draw_shapes.Add(new DrawShapeLines(points, color));
    }

    static public void Ray(Vector3 origin, Vector3 direction, Color color) {
        Vector3 end = origin + direction * 1000.0f;
        Line(origin, end, color);
    }

    static public void AxisMarker(Vector3 origin, float lead_length) {
        Line(origin + Vector3.Left * lead_length, origin + Vector3.Right * lead_length, Color.Color8(255, 0, 0));
        Line(origin + Vector3.Down * lead_length, origin + Vector3.Up * lead_length, Color.Color8(0, 255, 0));
        Line(origin + Vector3.Back * lead_length, origin + Vector3.Forward * lead_length, Color.Color8(0, 0, 255));
    }

    static public void AxisMarker(Transform3D transform, float lead_length) {
        Line(transform * Vector3.Left * lead_length, transform * Vector3.Right * lead_length, Color.Color8(255, 0, 0));
        Line(transform * Vector3.Down * lead_length, transform * Vector3.Up * lead_length, Color.Color8(0, 255, 0));
        Line(transform * Vector3.Back * lead_length, transform * Vector3.Forward * lead_length, Color.Color8(0, 0, 255));
    }

    static public void Point(Vector3 point, Color color) {
        var points = new List<Vector3>();
        points.Add(point);

        draw_shapes.Add(new DrawShapePoints(points, color));
    }

    static public void Points(List<Vector3> points, Color color) {
        draw_shapes.Add(new DrawShapePoints(points, color));
    }

    static public void Sphere(Transform3D transform, float radius, Color color) {
        var scaled = Transform3D.Identity;
        scaled = scaled.Scaled(Vector3.One * radius);
        draw_shapes.Add(new DrawShapeSphere(transform * scaled, color));
    }

    static public void Sphere(Vector3 center, Quaternion rotation, float radius, Color color) {
        var transform = Transform3D.Identity;
        transform.Origin = center;
        transform.Basis = new Basis(rotation) * transform.Basis;

        Sphere(transform, radius, color);
    }

    static public void Cylinder(Transform3D transform, float radius, float height, Color color) {
        draw_shapes.Add(new DrawShapeCylinder(transform, radius, radius, height, color));
    }

    static public void Cylinder(Vector3 center, Quaternion rotation, float radius, float height, Color color) {
        var transform = Transform3D.Identity;
        transform.Origin = center;
        transform.Basis = new Basis(rotation) * transform.Basis;

        Cylinder(transform, radius, height, color);
    }

    static public void Cylinder(Transform3D transform, float top_radius, float bottom_radius, float height, Color color) {
        draw_shapes.Add(new DrawShapeCylinder(transform, top_radius, bottom_radius, height, color));
    }

    static public void Cylinder(Vector3 center, Quaternion rotation, float top_radius, float bottom_radius, float height, Color color) {
        var transform = Transform3D.Identity;
        transform.Origin = center;
        transform.Basis = new Basis(rotation) * transform.Basis;

        Cylinder(transform, top_radius, bottom_radius, height, color);
    }

    static public void Capsule(Transform3D transform, float radius, float height, Color color) {
        draw_shapes.Add(new DrawShapeCapsule(transform, radius, radius, height, color));
    }

    static public void Capsule(Vector3 position, Quaternion rotation, float radius, float height, Color color) {
        var transform = Transform3D.Identity;
        transform.Origin = position;
        transform.Basis = new Basis(rotation) * transform.Basis;

        Capsule(transform, radius, height, color);
    }

    static public void Capsule(Transform3D transform, float top_radius, float bottom_radius, float height, Color color) {
        draw_shapes.Add(new DrawShapeCapsule(transform, top_radius, bottom_radius, height, color));
    }

    static public void Capsule(Vector3 position, Quaternion rotation, float top_radius, float bottom_radius, float height, Color color) {
        var transform = Transform3D.Identity;
        transform.Origin = position;
        transform.Basis = new Basis(rotation) * transform.Basis;

        Capsule(transform, top_radius, bottom_radius, height, color);
    }

    public override void _Ready()
    {
        base._Ready();

        im = new ImmediateMesh();
        mesh = new MeshInstance3D();

        mesh.Mesh = im;

        AddChild(mesh);

        mesh.MaterialOverride = (Material)ResourceLoader.Load("uid://bcj4rwdvhxfox");
    }


    public override void _Process(double delta)
    {
        base._Process(delta);

        im.ClearSurfaces();

        foreach (var shape in draw_shapes) {
            shape.Draw(im);
        }

        

        draw_shapes.Clear();
    }

}
