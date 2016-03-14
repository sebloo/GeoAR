using Microsoft.Maps.SpatialToolbox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace GeoAR
{
    public class ViewModel : INotifyPropertyChanged
    {
        private Geolocator geolocator;

        public Windows.UI.Core.CoreDispatcher Dispatcher { get; set; }


        public ObservableCollection<GeoItem> VisibleItems { get; set; }


        private double _Range = 10.0;
        public double Range
        {
            get { return _Range; }
            set
            {
                if (_Range != value)
                {
                    _Range = value;
                    RaisePropertyChanged("Range");
                }
            }
        }

        private bool? _IsTilted = null;
        internal int countTiltSwitched = 0;
        public bool IsTilted
        {
            get { return countTiltSwitched > 1 && _IsTilted.HasValue ? _IsTilted.Value : false; }
            set
            {
                if (_IsTilted != value)
                {
                    _IsTilted = value;
                    RaisePropertyChanged("IsTilted");
                    countTiltSwitched++;
                }
            }
        }


        private Geopoint _currentPosition;
        public Geopoint CurrentPosition
        {
            get
            {
                if (_currentPosition == null)
                {
                    GetCurrentLocation();   // ruft async request auf
                }
                return _currentPosition;
            }
            set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    RaisePropertyChanged("CurrentPosition");
                }
            }
        }

      


        private double _currentHeading = 0;
        public double CurrentHeading
        {
            //value between 0 and 360
            get { return _currentHeading; }
            set
            {
                if (_currentHeading != value)
                {
                    _currentHeading = value;
                    RaisePropertyChanged("CurrentHeading");
                }
            }
        }

        private bool _IsUpdateHeadingByCompass;
        /// <summary>
        /// Wenn true, wird das Heading vom Compasssensor geupdated
        /// </summary>
        public bool IsUpdateHeadingByCompass
        {
            get { return _IsUpdateHeadingByCompass; }
            set
            {
                if (_IsUpdateHeadingByCompass != value)
                {
                    _IsUpdateHeadingByCompass = value;
                    RaisePropertyChanged("IsUpdateHeadingByCompass");

                    if (!_IsUpdateHeadingByCompass)
                    {
                        this.CurrentHeading = 0;
                    }
                }
            }
        }

        private int _CompassOffset;
        public int CompassOffset
        {
            get { return _CompassOffset; }
            set
            {
                if (_CompassOffset != value)
                {
                    _CompassOffset = value % 360;
                    RaisePropertyChanged("CompassOffset");
                }
            }
        }

        

        private ObservableCollection<GeoItem> _Items;
        public ObservableCollection<GeoItem> Items
        {
            get { return _Items; }
            set
            {
                if (_Items != value)
                {
                    _Items = value;


                    RaisePropertyChanged("SymbolItems");
                }
            }
        }


        private GeoItem _SelectedItem;
        public GeoItem SelectedItem
        {
            get { return _SelectedItem; }
            set
            {
                if (_SelectedItem != value)
                {
                    _SelectedItem = value;
                    RaisePropertyChanged("SelectedSymbol");
                }

                if (value != null)
                {
                    ShowItemDetails = true;
                    //this.Range = double.MaxValue;
                }
                else
                {
                    ShowItemDetails = false;
                    //this.Range = 1;
                }
            }
        }

        private bool _ShowItemDetails = false;
        public bool ShowItemDetails
        {
            get { return _ShowItemDetails; }
            set
            {
                //if (_ShowSymbolDetails != value)
                //{
                _ShowItemDetails = value;
                RaisePropertyChanged("ShowSymbolDetails");
                //}
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;



        protected ViewModel()
        {
            geolocator = new Geolocator();
        }


        public static ViewModel GetViewModel(object dataContext, Windows.UI.Core.CoreDispatcher dispatcher)
        {
            ViewModel viewModel = (dataContext as ViewModel);
            if (viewModel == null)
            {
                Frame rootFrame = Windows.UI.Xaml.Window.Current.Content as Frame;
                viewModel = rootFrame.DataContext as ViewModel;

                if (viewModel == null)
                {
                    viewModel = new ViewModel();
                    rootFrame.DataContext = viewModel;

                    viewModel.LoadItems();
                }
            }

            viewModel.Dispatcher = dispatcher;
            viewModel.IsUpdateHeadingByCompass = true;

            return viewModel;
        }


        internal async Task LoadItems()
        {
            try
            {

                Items = new ObservableCollection<GeoItem>();
                Items.Add(new GeoItem { Lat = 52.51627, Lon = 13.33777, Name = "Brandenburger Tor" });
                Items.Add(new GeoItem { Lat = 52.54327, Lon = 13.359458, Name = "Stammlokal" });
                Items.Add(new GeoItem { Lat = 52.514057, Lon = 13.350111, Name = "Gold Else" });
                Items.Add(new GeoItem { Lat = 52.53, Lon = 13.37, Name = "Berghain" });

            }
            catch (Exception)
            {
                string errSym = "errMsgNetworkSymbols";
                string errNet = "errMsgNoNetworkHint";
                var errorMessage = new MessageDialog(errSym + " " + errNet);
                await errorMessage.ShowAsync();
            }
        }



        /// <summary>
        /// Gets and sets the current geo position (_currentPosition)
        /// </summary>
        /// <returns></returns>
        public async Task GetCurrentLocation()
        {
            try
            {
                Geoposition pos = await geolocator.GetGeopositionAsync();
                CurrentPosition = pos.Coordinate.Point;
            }
            catch (Exception)
            {
                ;
            }

            //CurrentPosition = new Geopoint(new BasicGeoposition { Latitude = 52.5064, Longitude = 13.3289 });
            //CurrentPosition = new Geopoint(new BasicGeoposition { Latitude = 52.50648, Longitude = 13.32546 }); // FIT
            //CurrentPosition = new Geopoint(new BasicGeoposition { Latitude = 52.51956, Longitude = 13.29604 }); // Charlottenburg
        }



        public ObservableCollection<GeoItem> CalculateItemsInView()
        {
            if (CurrentPosition != null)
            {
                if (Items != null && Items.Count > 0)
                {
                    VisibleItems = new ObservableCollection<GeoItem>();
                    foreach (var item in Items)
                    {
                        var posAsGeopoint = new Windows.Devices.Geolocation.Geopoint(item.Location.Position);
                        var itemHeading = SpatialTools.CalculateHeading(CurrentPosition, posAsGeopoint);

                        double angle = CurrentHeading - itemHeading;
                        if (angle > 180)
                            angle = CurrentHeading - (itemHeading + 360);
                        else if (angle < -180)
                            angle = CurrentHeading + 360 - itemHeading;

                        if (Math.Abs(angle) <= 22.5)
                        {
                            var distance = SpatialTools.HaversineDistance(CurrentPosition, posAsGeopoint, SpatialTools.DistanceUnits.KM);
                            if (distance <= this.Range)
                            {
                                item.Distance = distance;
                                item.Angle = angle;
                                VisibleItems.Add(item);
                            }
                        }
                    }
                    return VisibleItems;
                }
            }
            return null;
        }

        internal void UpdateSymbols()
        {
            RaisePropertyChanged("SymbolItems");
        }




        internal async Task UpdateCompass(double headingMagneticNorth)
        {
            if (IsUpdateHeadingByCompass == true)
            {
                double diff = Math.Abs(CurrentHeading - headingMagneticNorth);

                if (diff >= 8)
                {
                    if (Dispatcher != null)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                        //Debug.WriteLine("Unterschied: " + String.Format("{0:0.##}", diff));
                        CurrentHeading = headingMagneticNorth;
                        });
                    }
                }
            }
        }

        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
