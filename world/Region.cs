using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class Region : Node3D
{
    [Export]
    public int resolution = 256;

    [Export]
    public int region_size = 256;

    [Export]
    public Texture2D height_map;

    [Export]
    public float max_height;

    private Image height_image;

    private MeshInstance3D mesh_instance;

    private struct MeshData {
        public List<Vector3> position;
        public List<Vector2> uvs;
        public List<int> indices;
    };

    private async Task<MeshData> BuildMeshTask(int cur_resolution, CancellationToken cancellation) {
        var data = new MeshData();
        data.position = new List<Vector3>();
        data.uvs = new List<Vector2>();
        data.indices = new List<int>();

        Vector2 img_size = (Vector2)height_image.GetSize();

        for (int x = 0; x < cur_resolution; x++) {
            for (int z = 0; z < cur_resolution; z++) {
                Vector2 uv = new Vector2(x, z) / img_size;

                Vector2I pixel = (Vector2I)(uv * img_size);

                var height = height_image.GetPixelv(pixel).R * max_height;

                var pos = new Vector3(uv.X * (float)region_size, uv.Y * (float)region_size, height);

                data.position.Add(pos);
                data.uvs.Add(uv);
            }

            await Task.Yield();
            cancellation.ThrowIfCancellationRequested();
        }

        for (int x = 0; x < (cur_resolution - 1); x++) {
            for (int z = 0; z < (cur_resolution - 1); z++) {
                int cur = z * cur_resolution + x;
                int right = cur + 1;
                int down = cur + cur_resolution;
                int down_right = down + 1;

                data.indices.Add(cur);
                data.indices.Add(down);
                data.indices.Add(down_right);

                data.indices.Add(cur);
                data.indices.Add(down_right);
                data.indices.Add(right);
            }

            await Task.Yield();
            cancellation.ThrowIfCancellationRequested();
        }

        return data;
    }

    private Task<MeshData> mesh_result = null;
    private CancellationTokenSource mesh_cancel = null;

    private void RebuildMesh() {
        if (mesh_cancel != null) {
            mesh_cancel.Cancel();
        }

        mesh_cancel = new CancellationTokenSource();
        mesh_result = Task.Run(() => BuildMeshTask(resolution, mesh_cancel.Token));
    }

    public override void _Ready()
    {
        base._Ready();

        height_image = height_map.GetImage();

        mesh_instance = new MeshInstance3D();

        AddChild(mesh_instance);

        RebuildMesh();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (mesh_result != null && mesh_result.IsCompleted) {
            var data = mesh_result.Result;
            
            var st = new SurfaceTool();

            st.Begin(Mesh.PrimitiveType.Triangles);
            // TODO This can be optimized by changed to TriangleStrips

            for (int i = 0; i < data.position.Count; i++) {
                //st.SetUV(data.uvs[i]);
                st.AddVertex(data.position[i]);
            }

            for (int i = 0; i < data.indices.Count; i++) {
                st.AddIndex(data.indices[i]);
            }

            //st.GenerateNormals();
            //st.GenerateTangents();

            var mesh = st.Commit();

            mesh_instance.Mesh = mesh;

            mesh_cancel = null;
            mesh_result = null;
        }
    }


}
