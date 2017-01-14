using System.Numerics;

namespace SoftEngine
{
    public class Mesh
    {
        public string Name { get; private set; }
        public Vector3[] Vertices { get; private set; }
        public Face[] Faces { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Name = name;
            Faces = new Face[facesCount];
            Vertices = new Vector3[verticesCount];
        }
    }

    public struct Face
    {
        public int A;
        public int B;
        public int C;
    }
}