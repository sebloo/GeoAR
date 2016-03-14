using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace GeoAR
{
    public sealed partial class MyUserControl1 : UserControl
    {
        public MyUserControl1()
        {
            this.InitializeComponent();
            initARMode();
        }

        private static Windows.Media.Capture.MediaCapture captureManager;
        private static bool cameraStarted = false;

        private bool isLoaded = false;

        private ViewModel viewModel;
        

        private void initARMode()
        {
            viewModel = (DataContext as ViewModel);
            if (viewModel == null)
            {
                viewModel = ViewModel.GetViewModel(this.DataContext, this.Dispatcher);
            }

            initAndStartCamera();
            this.Unloaded += ARRegion_Unloaded;
            this.LayoutUpdated += ARRegion_LayoutUpdated;
            this.SizeChanged += ARRegion_SizeChanged;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        async private void initAndStartCamera()
        {
            if (captureManager != null)
            {
                captureManager.Dispose();
                captureManager = null;
            }

            var cameraAvailable = await initCamera();
            if (cameraAvailable)
            {
                await startCamera();
                cameraStarted = true;
            }
        }




        private void ARRegion_LayoutUpdated(object sender, object e)
        {
            if (!isLoaded && Visibility == Visibility.Visible)
            {
                this.isLoaded = true;
                this.UpdateARView();
            }
            if (Visibility == Visibility.Collapsed)
            {
                this.isLoaded = false;
            }
        }

        private void ARRegion_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdateARView();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "CurrentHeading" && captureManager != null && captureManager.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
            if (e.PropertyName == "CurrentHeading")
            {
                UpdateARView();
            }
            if (e.PropertyName == "IsTilted")
            {
                if (viewModel.IsTilted)
                {
                    UpdateARView();
                }
            }
        }


        internal void UpdateARView()
        {
            if (Visibility != Visibility.Visible)
            {
                // only proceed if control has height values etc.              
                return;
            }


            ObservableCollection<GeoItem> visibleItems = viewModel.CalculateItemsInView();

            List<Box> boxes = new List<Box>();
            ItemCanvas.Children.Clear();

            if (visibleItems != null && visibleItems.Count > 0)
            {
                var sortedSymbols = new ObservableCollection<GeoItem>(visibleItems.OrderBy(i => i.Distance));

                foreach (var symbol in sortedSymbols)
                {
                    double left = 0;
                    if (symbol.Angle > 0)
                    {
                        left = ItemCanvas.ActualWidth / 2 * ((22.5 - symbol.Angle) / 22.5);
                    }
                    else
                    {
                        left = ItemCanvas.ActualWidth / 2 * (1 + -symbol.Angle / 22.5);
                    }
                    double top = (ItemCanvas.ActualHeight - 60) * 0.75;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(symbol.Name + " : ");
                    sb.Append(String.Format("{0:n}", symbol.Distance * 1000) + " m");

                    var metaInfoBlock = new TextBlock()
                    {
                        Text = sb.ToString(),
                        FontSize = 14,
                        TextAlignment = TextAlignment.Left,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 5, 5, 5)
                    };

                    StackPanel symbolProxy = new StackPanel { Orientation = Orientation.Horizontal, Background = new SolidColorBrush(Colors.White), Width = 128, Height = 50, Opacity = 0.5 };

                    symbolProxy.Children.Add(metaInfoBlock);

                    var proxyBoundingBox = new Box { Left = left, Top = top, Height = 50, Width = 128 };
                    if (!isCollisionDetected(boxes, ref proxyBoundingBox))
                    {
                        boxes.Add(proxyBoundingBox);
                    }

                    Canvas.SetLeft(symbolProxy, left);
                    Canvas.SetTop(symbolProxy, proxyBoundingBox.Top);

                    ItemCanvas.Children.Add(symbolProxy);
                }
            }
        }

        private static bool isCollisionDetected(List<Box> boxes, ref Box newBox)
        {
            foreach (var box in boxes)
            {
                if (box.Intersects(newBox))
                {
                    newBox.Top = newBox.Top - newBox.Height - 1;
                    while (isCollisionDetected(boxes, ref newBox))
                    {
                        return true;
                    }
                }

            }
            return false;
        }


        private void ARRegion_Unloaded(object sender, RoutedEventArgs e)
        {
            this.isLoaded = false;
        }


        async private Task<bool> initCamera()
        {
            if (captureManager == null)
            {
                captureManager = null;
                captureManager = new Windows.Media.Capture.MediaCapture();

                var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                string deviceId = "";

                if (devices.Count == 1)
                {
                    deviceId = devices[0].Id;
                }
                else
                {
                    for (int i = 0; i < devices.Count; i++)
                    {
                        if (devices[0].EnclosureLocation != null && devices[i].EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back)
                        {
                            deviceId = devices[i].Id;
                        }
                    }
                }

                if (string.IsNullOrEmpty(deviceId))
                {
                    System.Diagnostics.Debug.WriteLine("no camera found");
                    return false;
                }
                else
                {
                    var settings = new Windows.Media.Capture.MediaCaptureInitializationSettings();
                    settings.VideoDeviceId = deviceId;
                    await captureManager.InitializeAsync(settings);
                    return true;
                }
            }
            return true;
        }

        async private Task startCamera()
        {
            if (captureManager != null && captureManager.CameraStreamState == Windows.Media.Devices.CameraStreamState.NotStreaming)
            {
                PreviewScreen.Source = captureManager;
                await captureManager.StartPreviewAsync();
            }
        }

        async public static Task stopCamera()
        {
            if (captureManager != null && captureManager.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
            {
                await captureManager.StopPreviewAsync();
            }
        }
    }
}
