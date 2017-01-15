﻿using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;
using SharpDX;

namespace SoftEngine
{
    public class Texture
    {
        private byte[] _internalBuffer;
        private readonly int _width;
        private readonly int _height;

        // Working with a fix sized texture (512x512, 1024x1024, etc.).
        public Texture(string filename, int width, int height)
        {
            _width = width;
            _height = height;
            Load(filename);
        }

        async void Load(string filename)
        {
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(filename);

            using (var stream = await file.OpenReadAsync())
            {
                var bmp = new WriteableBitmap(_width, _height);
                bmp.SetSource(stream);

                _internalBuffer = bmp.PixelBuffer.ToArray();
            }
        }

        // Takes the U & V coordinates exported by Blender
        // and return the corresponding pixel color in the texture
        public Color4 Map(float tu, float tv)
        {
            // Image is not loaded yet
            if (_internalBuffer == null)
            {
                return Color4.White;
            }
            // using a % operator to cycle/repeat the texture if needed
            int u = Math.Abs((int)(tu * _width) % _width);
            int v = Math.Abs((int)(tv * _height) % _height);

            int pos = (u + v * _width) * 4;
            byte b = _internalBuffer[pos];
            byte g = _internalBuffer[pos + 1];
            byte r = _internalBuffer[pos + 2];
            byte a = _internalBuffer[pos + 3];

            return new Color4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }
    }
}