﻿using System.Numerics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftEngine
{
    public sealed partial class MainPage : Page
    {
        private Device _device;
        private readonly Camera _camera = new Camera();
        private readonly Mesh _mesh = new Mesh("cube", 8, 12);

        public MainPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Choose the back buffer resolution here
            WriteableBitmap bmp = new WriteableBitmap(500, 500);

            _device = new Device(bmp);

            // Our Image XAML control
            frontBuffer.Source = bmp;

            _mesh.Vertices[0] = new Vector3(-1, 1, 1);
            _mesh.Vertices[1] = new Vector3(1, 1, 1);
            _mesh.Vertices[2] = new Vector3(-1, -1, 1);
            _mesh.Vertices[3] = new Vector3(1, -1, 1);
            _mesh.Vertices[4] = new Vector3(-1, 1, -1);
            _mesh.Vertices[5] = new Vector3(1, 1, -1);
            _mesh.Vertices[6] = new Vector3(1, -1, -1);
            _mesh.Vertices[7] = new Vector3(-1, -1, -1);
            
            _mesh.Faces[0] = new Face { A = 0, B = 1, C = 2 };
            _mesh.Faces[1] = new Face { A = 1, B = 2, C = 3 };
            _mesh.Faces[2] = new Face { A = 1, B = 3, C = 6 };
            _mesh.Faces[3] = new Face { A = 1, B = 5, C = 6 };
            _mesh.Faces[4] = new Face { A = 0, B = 1, C = 4 };
            _mesh.Faces[5] = new Face { A = 1, B = 4, C = 5 };
            
            _mesh.Faces[6] = new Face { A = 2, B = 3, C = 7 };
            _mesh.Faces[7] = new Face { A = 3, B = 6, C = 7 };
            _mesh.Faces[8] = new Face { A = 0, B = 2, C = 7 };
            _mesh.Faces[9] = new Face { A = 0, B = 4, C = 7 };
            _mesh.Faces[10] = new Face { A = 4, B = 5, C = 6 };
            _mesh.Faces[11] = new Face { A = 4, B = 6, C = 7 };

            _camera.Position = new Vector3(0, 0, 10.0f);
            _camera.Target = Vector3.Zero;

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            _device.Clear(0, 0, 0, 255);

            // rotating slightly the cube during each frame rendered
            _mesh.Rotation = new Vector3(_mesh.Rotation.X + 0.01f, _mesh.Rotation.Y + 0.01f, _mesh.Rotation.Z);

            // Doing the various matrix operations
            _device.Render(_camera, _mesh);
            // Flushing the back buffer into the front buffer
            _device.Present();
        }
    }
}
