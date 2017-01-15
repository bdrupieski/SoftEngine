using SharpDX;

namespace SoftEngine
{
    public class Mesh
    {
        public string Name { get; private set; }
        public Vertex[] Vertices { get; private set; }
        public Face[] Faces { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Name = name;
            Faces = new Face[facesCount];
            Vertices = new Vertex[verticesCount];
        }
    }
}