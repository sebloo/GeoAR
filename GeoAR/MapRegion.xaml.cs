using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace GeoAR
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MapRegion : UserControl
    {
        private static MapIcon centerIcon;
        private bool isReady = false;
        private ViewModel viewModel;

        // save style beim Umschalten auf AR Modus und setzte style wieder zurück beim umschalten in den Karten Modus

        private MapStyle tmpMapStyle = MapStyle.Road;

        public MapControl Map { get { return this.MainMapControl; } }


        public MapRegion()
        {
            this.InitializeComponent();
            initMap();
            iterateItems();
            this.Map.CenterChanged += Map_CenterChanged;
        }

        private void Map_CenterChanged(MapControl sender, object args)
        {
            viewModel.RaisePropertyChanged("CurrentPosition");
    }

        private void initMap()
        {
            viewModel = (DataContext as ViewModel);
            if (viewModel == null)
            {
                viewModel = ViewModel.GetViewModel(this.DataContext, this.Dispatcher);
            }

            if (viewModel.Dispatcher != null)
            {
                viewModel.Dispatcher.StopProcessEvents();
            }

          
            centerMap();
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
          //  iterateItems();
        }
        private bool positionFixed = false;



        private void centerMap()
        {
            if (viewModel.CurrentPosition != null && !positionFixed)
            {
                MainMapControl.Center = viewModel.CurrentPosition;
                MainMapControl.ZoomLevel = 12;
                MainMapControl.LandmarksVisible = true;

                positionFixed = true;
            }
        }

        private void iterateItems()
        {
            var items = viewModel.Items;
            
            int index = 0;
            if (items != null)
            {
                foreach (var item in items)
                {
                    MapIcon MapIcon1 = new MapIcon();
                    MapIcon1.Location = item.Location;
                    MapIcon1.NormalizedAnchorPoint = new Point(0.5, 1.0);
                    MapIcon1.Title = item.Name;
                    MapIcon1.CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible;
                    MapIcon1.MapTabIndex = index++;
                    MainMapControl.MapElements.Add(MapIcon1);
                }
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "CurrentHeading" && captureManager != null && captureManager.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
            if (e.PropertyName == "CurrentHeading")
            {
                UpdateViewPort();
            }
            if (e.PropertyName == "CurrentPosition")
            {
                centerMap();
            }
        }

        private void UpdateViewPort()
        {

        }
    }
}
