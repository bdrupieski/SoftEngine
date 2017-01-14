﻿using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;
using SharpDX.Mathematics.Interop;

namespace SoftEngine
{
    public class Device
    {
        private readonly byte[] _backBuffer;
        private readonly WriteableBitmap _bmp;

        public Device(WriteableBitmap bmp)
        {
            _bmp = bmp;
            // width * height * (R,G,B,A)
            _backBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
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
        private void PutPixel(int x, int y, RawColor4 color)
        {
            // As we have a 1-D Array for our back buffer
            // we need to know the equivalent cell in 1-D based
            // on the 2D coordinates on screen
            var index = (x + y * _bmp.PixelWidth) * 4;

            _backBuffer[index] = (byte) (color.B * 255);
            _backBuffer[index + 1] = (byte) (color.G * 255);
            _backBuffer[index + 2] = (byte) (color.R * 255);
            _backBuffer[index + 3] = (byte) (color.A * 255);
        }

        // Project takes some 3D coordinates and transform them
        // in 2D coordinates using the transformation matrix
        private Vector2 Project(Vector3 coord, Matrix4x4 transMat)
        {
            // transforming the coordinates
            var point = Vector3.Transform(coord, transMat);
            // The transformed coordinates will be based on coordinate system
            // starting on the center of the screen. But drawing on screen normally starts
            // from top left. We then need to transform them again to have x:0, y:0 on top left.
            var x = point.X * _bmp.PixelWidth + _bmp.PixelWidth / 2.0f;
            var y = -point.Y * _bmp.PixelHeight + _bmp.PixelHeight / 2.0f;
            return new Vector2(x, y);
        }

        private void DrawPoint(Vector2 point)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < _bmp.PixelWidth && point.Y < _bmp.PixelHeight)
            {
                // Drawing a yellow point
                PutPixel((int) point.X, (int) point.Y, new RawColor4(1.0f, 1.0f, 0.0f, 1.0f));
            }
        }

        // The main method of the engine that re-compute each vertex projection
        // during each frame
        public void Render(Camera camera, params Mesh[] meshes)
        {
            // To understand this part, please read the prerequisites resources
            var viewMatrix = Matrix4x4.CreateLookAt(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix4x4.CreatePerspective(0.78f,
                (float) _bmp.PixelWidth / _bmp.PixelHeight,
                0.01f, 1.0f);

            foreach (Mesh mesh in meshes)
            {
                // Beware to apply rotation before translation 
                var worldMatrix = Matrix4x4.CreateFromYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix4x4.CreateTranslation(mesh.Position);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                foreach (var vertex in mesh.Vertices)
                {
                    // First, we project the 3D coordinates into the 2D space
                    var point = Project(vertex, transformMatrix);
                    // Then we can draw on screen
                    DrawPoint(point);
                }
            }
        }
    }
}