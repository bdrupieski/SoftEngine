using System.Numerics;

namespace SoftEngine
{
    public class Mesh
    {
        public string Name { get; private set; }
        public Vector3[] Vertices { get; private set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public Mesh(string name, int verticesCount)
        {
            Name = name;
            Vertices = new Vector3[verticesCount];
        }
    }
}