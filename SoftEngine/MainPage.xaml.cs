using SharpDX;
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
        private Mesh[] _meshes;

        public MainPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Choose the back buffer resolution here
            WriteableBitmap bmp = new WriteableBitmap(640, 480);

            // Our Image XAML control
            frontBuffer.Source = bmp;

            _device = new Device(bmp);
            _meshes = await _device.LoadJsonFileAsync("monkey.babylon");
            _camera.Position = new Vector3(0, 0, 10.0f);
            _camera.Target = Vector3.Zero;

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, object e)
        {
            _device.Clear(0, 0, 0, 255);

            foreach (var mesh in _meshes)
            {
                // rotating slightly the meshes during each frame rendered
                mesh.Rotation = new Vector3(mesh.Rotation.X, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
            }

            // Doing the various matrix operations
            _device.Render(_camera, _meshes);
            // Flushing the back buffer into the front buffer
            _device.Present();
        }
    }
}
