using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using SharpDX;

namespace SoftEngine
{
    public class Device
    {
        private readonly byte[] _backBuffer;
        private readonly float[] _depthBuffer;
        private readonly WriteableBitmap _bmp;
        private readonly int _renderWidth;
        private readonly int _renderHeight;

        public Device(WriteableBitmap bmp)
        {
            _bmp = bmp;
            _renderWidth = bmp.PixelWidth;
            _renderHeight = bmp.PixelHeight;

            // width * height * (R,G,B,A)
            _backBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
            _depthBuffer = new float[bmp.PixelWidth * bmp.PixelHeight];
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (int i = 0; i < _backBuffer.Length; i += 4)
            {
                _backBuffer[i] = b;
                _backBuffer[i + 1] = g;
                _backBuffer[i + 2] = r;
                _backBuffer[i + 3] = a;
            }

            // Clearing Depth Buffer
            for (var index = 0; index < _depthBuffer.Length; index++)
            {
                _depthBuffer[index] = float.MaxValue;
            }
        }

        public void Present()
        {
            using (var stream = _bmp.PixelBuffer.AsStream())
            {
                stream.Write(_backBuffer, 0, _backBuffer.Length);
            }
            _bmp.Invalidate();
        }

        // Called to put a pixel on screen at a specific X,Y coordinates
        private void PutPixel(int x, int y, float z, Color4 color)
        {
            // As we have a 1-D Array for our back buffer
            // we need to know the equivalent cell in 1-D based
            // on the 2D coordinates on screen
            var index = x + y * _renderWidth;
            var index4 = index * 4;

            if (_depthBuffer[index] < z)
            {
                return; // Discard
            }

            _depthBuffer[index] = z;

            _backBuffer[index4] = (byte) (color.Blue * 255);
            _backBuffer[index4 + 1] = (byte) (color.Green * 255);
            _backBuffer[index4 + 2] = (byte) (color.Red * 255);
            _backBuffer[index4 + 3] = (byte) (color.Alpha * 255);
        }

        // Project takes some 3D coordinates and transform them
        // in 2D coordinates using the transformation matrix
        // It also transform the same coordinates and the norma to the vertex 
        // in the 3D world
        private Vertex Project(Vertex vertex, Matrix transMat, Matrix world)
        {
            // transforming the coordinates into 2D space
            var point2D = Vector3.TransformCoordinate(vertex.Coordinates, transMat);
            // transforming the coordinates & the normal to the vertex in the 3D world
            var point3DWorld = Vector3.TransformCoordinate(vertex.Coordinates, world);
            var normal3DWorld = Vector3.TransformCoordinate(vertex.Normal, world);

            // The transformed coordinates will be based on coordinate system
            // starting on the center of the screen. But drawing on screen normally starts
            // from top left. We then need to transform them again to have x:0, y:0 on top left.
            var x = point2D.X * _renderWidth + _renderWidth / 2.0f;
            var y = -point2D.Y * _renderHeight + _renderHeight / 2.0f;

            return new Vertex
            {
                Coordinates = new Vector3(x, y, point2D.Z),
                Normal = normal3DWorld,
                WorldCoordinates = point3DWorld
            };
        }

        // DrawPoint calls PutPixel but does the clipping operation before
        private void DrawPoint(Vector3 point, Color4 color)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < _bmp.PixelWidth && point.Y < _bmp.PixelHeight)
            {
                // Drawing a point
                PutPixel((int) point.X, (int) point.Y, point.Z, color);
            }
        }

        // The main method of the engine that re-compute each vertex projection
        // during each frame
        public void Render(Camera camera, params Mesh[] meshes)
        {
            // To understand this part, please read the prerequisites resources
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovRH(0.78f,
                (float) _bmp.PixelWidth / _bmp.PixelHeight,
                0.01f, 1.0f);

            foreach (Mesh mesh in meshes)
            {
                // Beware to apply rotation before translation 
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix.Translation(mesh.Position);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                var faceIndex = 0;
                foreach (var face in mesh.Faces)
                {
                    var vertexA = mesh.Vertices[face.A];
                    var vertexB = mesh.Vertices[face.B];
                    var vertexC = mesh.Vertices[face.C];

                    var pixelA = Project(vertexA, transformMatrix, worldMatrix);
                    var pixelB = Project(vertexB, transformMatrix, worldMatrix);
                    var pixelC = Project(vertexC, transformMatrix, worldMatrix);

                    var color = 0.25f + faceIndex % mesh.Faces.Length * 0.75f / mesh.Faces.Length;
                    DrawTriangle(pixelA, pixelB, pixelC, new Color4(color, color, color, 1));
                    faceIndex++;
                }
            }
        }

        // Loading the JSON file in an asynchronous manner
        public async Task<Mesh[]> LoadJsonFileAsync(string fileName)
        {
            var meshes = new List<Mesh>();
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(fileName);
            var data = await Windows.Storage.FileIO.ReadTextAsync(file);
            dynamic jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(data);

            for (var meshIndex = 0; meshIndex < jsonObject.meshes.Count; meshIndex++)
            {
                var verticesArray = jsonObject.meshes[meshIndex].vertices;
                // Faces
                var indicesArray = jsonObject.meshes[meshIndex].indices;

                var uvCount = jsonObject.meshes[meshIndex].uvCount.Value;
                var verticesStep = 1;

                // Depending of the number of texture's coordinates per vertex
                // we're jumping in the vertices array  by 6, 8 & 10 windows frame
                switch ((int) uvCount)
                {
                    case 0:
                        verticesStep = 6;
                        break;
                    case 1:
                        verticesStep = 8;
                        break;
                    case 2:
                        verticesStep = 10;
                        break;
                }

                // the number of interesting vertices information for us
                var verticesCount = verticesArray.Count / verticesStep;
                // number of faces is logically the size of the array divided by 3 (A, B, C)
                var facesCount = indicesArray.Count / 3;
                var mesh = new Mesh(jsonObject.meshes[meshIndex].name.Value, verticesCount, facesCount);

                // Filling the Vertices array of our mesh first
                for (var index = 0; index < verticesCount; index++)
                {
                    var x = (float) verticesArray[index * verticesStep].Value;
                    var y = (float) verticesArray[index * verticesStep + 1].Value;
                    var z = (float) verticesArray[index * verticesStep + 2].Value;
                    // Loading the vertex normal exported by Blender
                    var nx = (float) verticesArray[index * verticesStep + 3].Value;
                    var ny = (float) verticesArray[index * verticesStep + 4].Value;
                    var nz = (float) verticesArray[index * verticesStep + 5].Value;
                    mesh.Vertices[index] = new Vertex
                    {
                        Coordinates = new Vector3(x, y, z),
                        Normal = new Vector3(nx, ny, nz)
                    };
                }

                // Then filling the Faces array
                for (var index = 0; index < facesCount; index++)
                {
                    var a = (int) indicesArray[index * 3].Value;
                    var b = (int) indicesArray[index * 3 + 1].Value;
                    var c = (int) indicesArray[index * 3 + 2].Value;
                    mesh.Faces[index] = new Face {A = a, B = b, C = c};
                }

                // Getting the position you've set in Blender
                var position = jsonObject.meshes[meshIndex].position;
                mesh.Position = new Vector3((float) position[0].Value, (float) position[1].Value,
                    (float) position[2].Value);
                meshes.Add(mesh);
            }
            return meshes.ToArray();
        }

        // Clamping values to keep them between 0 and 1
        float Clamp(float value, float min = 0, float max = 1)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        // Interpolating the value between 2 vertices 
        // min is the starting point, max the ending point
        // and gradient the % between the 2 points
        float Interpolate(float min, float max, float gradient)
        {
            return min + (max - min) * Clamp(gradient);
        }


        // drawing line between 2 points from left to right
        // papb -> pcpd
        // pa, pb, pc, pd must then be sorted before
        void ProcessScanLine(ScanLineData data, Vertex va, Vertex vb, Vertex vc, Vertex vd, Color4 color)
        {
            Vector3 pa = va.Coordinates;
            Vector3 pb = vb.Coordinates;
            Vector3 pc = vc.Coordinates;
            Vector3 pd = vd.Coordinates;

            // Thanks to current Y, we can compute the gradient to compute others values like
            // the starting X (sx) and ending X (ex) to draw between
            // if pa.Y == pb.Y or pc.Y == pd.Y, gradient is forced to 1
            var gradient1 = Math.Abs(pa.Y - pb.Y) > 0.0001 ? (data.currentY - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = Math.Abs(pc.Y - pd.Y) > 0.0001 ? (data.currentY - pc.Y) / (pd.Y - pc.Y) : 1;

            int sx = (int) Interpolate(pa.X, pb.X, gradient1);
            int ex = (int) Interpolate(pc.X, pd.X, gradient2);

            // starting Z & ending Z
            float z1 = Interpolate(pa.Z, pb.Z, gradient1);
            float z2 = Interpolate(pc.Z, pd.Z, gradient2);

            // drawing a line from left (sx) to right (ex) 
            for (var x = sx; x < ex; x++)
            {
                float gradient = (x - sx) / (float) (ex - sx);

                var z = Interpolate(z1, z2, gradient);
                var ndotl = data.ndotla;
                // changing the color value using the cosine of the angle
                // between the light vector and the normal vector
                DrawPoint(new Vector3(x, data.currentY, z), color * ndotl);
            }
        }

        // Compute the cosine of the angle between the light vector and the normal vector
        // Returns a value between 0 and 1
        private float ComputeNDotL(Vector3 vertex, Vector3 normal, Vector3 lightPosition)
        {
            var lightDirection = lightPosition - vertex;

            normal.Normalize();
            lightDirection.Normalize();

            return Math.Max(0, Vector3.Dot(normal, lightDirection));
        }

        private void DrawTriangle(Vertex v1, Vertex v2, Vertex v3, Color4 color)
        {
            // Sorting the points in order to always have this order on screen p1, p2 & p3
            // with p1 always up (thus having the Y the lowest possible to be near the top screen)
            // then p2 between p1 & p3
            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            if (v2.Coordinates.Y > v3.Coordinates.Y)
            {
                var temp = v2;
                v2 = v3;
                v3 = temp;
            }

            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v2;
                v2 = v1;
                v1 = temp;
            }

            Vector3 p1 = v1.Coordinates;
            Vector3 p2 = v2.Coordinates;
            Vector3 p3 = v3.Coordinates;

            // normal face's vector is the average normal between each vertex's normal
            // computing also the center point of the face
            Vector3 vnFace = (v1.Normal + v2.Normal + v3.Normal) / 3;
            Vector3 centerPoint = (v1.WorldCoordinates + v2.WorldCoordinates + v3.WorldCoordinates) / 3;
            // Light position 
            Vector3 lightPos = new Vector3(0, 10, 10);
            // computing the cos of the angle between the light vector and the normal vector
            // it will return a value between 0 and 1 that will be used as the intensity of the color
            float ndotl = ComputeNDotL(centerPoint, vnFace, lightPos);

            var data = new ScanLineData {ndotla = ndotl};

            // computing lines' directions
            float dP1P2, dP1P3;

            // http://en.wikipedia.org/wiki/Slope
            // Computing slopes
            if (p2.Y - p1.Y > 0)
            {
                dP1P2 = (p2.X - p1.X) / (p2.Y - p1.Y);
            }
            else
            {
                dP1P2 = 0;
            }

            if (p3.Y - p1.Y > 0)
            {
                dP1P3 = (p3.X - p1.X) / (p3.Y - p1.Y);
            }
            else
            {
                dP1P3 = 0;
            }

            // First case where triangles are like that:
            // P1
            // -
            // -- 
            // - -
            // -  -
            // -   - P2
            // -  -
            // - -
            // -
            // P3
            if (dP1P2 > dP1P3)
            {
                for (var y = (int) p1.Y; y <= (int) p3.Y; y++)
                {
                    data.currentY = y;

                    if (y < p2.Y)
                    {
                        ProcessScanLine(data, v1, v3, v1, v2, color);
                    }
                    else
                    {
                        ProcessScanLine(data, v1, v3, v2, v3, color);
                    }
                }
            }
            // First case where triangles are like that:
            //       P1
            //        -
            //       -- 
            //      - -
            //     -  -
            // P2 -   - 
            //     -  -
            //      - -
            //        -
            //       P3
            else
            {
                for (var y = (int) p1.Y; y <= (int) p3.Y; y++)
                {
                    data.currentY = y;

                    if (y < p2.Y)
                    {
                        ProcessScanLine(data, v1, v2, v1, v3, color);
                    }
                    else
                    {
                        ProcessScanLine(data, v2, v3, v1, v3, color);
                    }
                }
            }
        }
    }
}