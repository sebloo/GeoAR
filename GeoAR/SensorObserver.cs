using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.UI.Xaml.Controls;

namespace GeoAR
{
    public class SensorObserver
    {
        protected Accelerometer _accelerometer;
        protected Gyrometer _gyrometer;
        protected SimpleOrientationSensor _simpleOrientationSensor;
        protected OrientationSensor _orientationSensor;
        protected Compass _compassSensor;

        protected ViewModel _viewModel;
        protected Page _mainPage;

        private bool isVertical = false;

        private static SensorObserver _SensorObserver = null;


        public static void InitSensorObserver(ViewModel viewModel, Page mainPage)
        {

            if (_SensorObserver == null)
            {
                _SensorObserver = new SensorObserver();
                _SensorObserver._viewModel = viewModel;
                _SensorObserver._mainPage = mainPage;
                _SensorObserver.initSensorObserver();
            }
        }



        private void initSensorObserver()
        {
            _simpleOrientationSensor = SimpleOrientationSensor.GetDefault();
            if (_simpleOrientationSensor != null)
            {
                System.Diagnostics.Debug.WriteLine("simple orientation sensor working");
                _simpleOrientationSensor.OrientationChanged += OrientationChanged;
            }
            else
            {
                _orientationSensor = OrientationSensor.GetDefault();
                if (_orientationSensor != null)
                {
                    System.Diagnostics.Debug.WriteLine("Orientation sensor working");
                    _orientationSensor.ReadingChanged += OrientationSensor_ReadingChanged;
                }
                else
                {
                    _accelerometer = Accelerometer.GetDefault();
                    if (_accelerometer != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Accelerometion sensor working");
                        _accelerometer.ReportInterval = 330; // _accelerometer.MinimumReportInterval;
                        _accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
                    }
                }
            }

            _compassSensor = Compass.GetDefault();
            if (_compassSensor != null)
            {
                System.Diagnostics.Debug.WriteLine("compass sensor working");
                _compassSensor.ReportInterval = 50;
                _compassSensor.ReadingChanged += CompassReadingChanged;
            }

            _mainPage.KeyDown += _mainPage_KeyDown;
        }

        int headingEmu = 0;
        int pitchEmu = 0;

        private void _mainPage_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.A:
                    headingEmu -= 30;
                    emulateCompass(headingEmu % 360);
                    break;
                case Windows.System.VirtualKey.D:
                    headingEmu += 30;
                    emulateCompass(headingEmu % 360);
                    break;
                case Windows.System.VirtualKey.W:
                    pitchEmu -= 10;
                    emulatePitch((pitchEmu % 100) / 100);
                    break;
                case Windows.System.VirtualKey.S:
                    pitchEmu += 10;
                    emulatePitch((pitchEmu % 100) / 100);
                    break;

            }
        }

        private async void emulatePitch(double value)
        {
            handleQuaternion(value, "manuell");
        }

        private async void emulateCompass(double value)
        {
            var reading = value;

            double displayOffset = _viewModel.CompassOffset;

            await _mainPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Calculate the compass heading offset based on
                // the current display orientation.
                var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();

                switch (displayInfo.CurrentOrientation)
                {
                    case Windows.Graphics.Display.DisplayOrientations.Landscape:
                        displayOffset += 0;
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.Portrait:
                        displayOffset += 270;
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.LandscapeFlipped:
                        displayOffset += 180;
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.PortraitFlipped:
                        displayOffset += 90;
                        break;
                }

                var displayCompensatedHeading = (value + displayOffset) % 360;

                _viewModel.UpdateCompass(displayCompensatedHeading);
            });
        }           


        async private void Accelerometer_ReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
        {
            await _mainPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                AccelerometerReading reading = args.Reading;
            //Debug.WriteLine("Accelerometer: " + String.Format("{0,8:0.00000}", reading.AccelerationX));
            handleQuaternion((-1 * reading.AccelerationY), "-Y");
            });
        }


        #region Tilting

        async private void OrientationSensor_ReadingChanged(OrientationSensor sender, OrientationSensorReadingChangedEventArgs args)
        {
            await _mainPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                OrientationSensorReading reading = args.Reading;

            // Quaternion values
            SensorQuaternion quaternion = reading.Quaternion;   // get a reference to the object to avoid re-creating it for each access

            var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
                switch (displayInfo.CurrentOrientation)
                {
                    case Windows.Graphics.Display.DisplayOrientations.Landscape:
                        handleQuaternion(quaternion.X, "X");
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.Portrait:
                        handleQuaternion(quaternion.Y, "Y");
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.LandscapeFlipped:
                        handleQuaternion(quaternion.X, "X");
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.PortraitFlipped:
                        handleQuaternion(quaternion.Y, "Y");
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.None:
                        handleQuaternion(quaternion.Z, "Z");
                        break;
                }
            });
        }

        private void handleQuaternion(double value, string axis)
        {
            if (value > 0.0 && value < 0.5)
            {
                if (isVertical)
                {
                    _viewModel.IsTilted = false;
                }

                isVertical = false;
                System.Diagnostics.Debug.WriteLine("MAP Mode\t" + axis + ": " + String.Format("{0,8:0.00000}", value));
            }
            else
            {
                if (value > 0.5 && value < 1.0)
                {
                    if (!isVertical)
                    {
                        _viewModel.IsTilted = true;
                    }
                    isVertical = true;
                    System.Diagnostics.Debug.WriteLine("AR Mode\t" + axis + ": " + String.Format("{0,8:0.00000}", value));
                }
                else
                {

                    System.Diagnostics.Debug.WriteLine("MAP Mode\t" + axis + ": " + String.Format("{0,8:0.00000}", value));
                }
            }
        }

        #endregion


        #region Compass

        async internal void CompassReadingChanged(Compass sender, CompassReadingChangedEventArgs e)
        {
            var reading = sender.GetCurrentReading();

            double displayOffset = _viewModel.CompassOffset;

            await _mainPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
            // Calculate the compass heading offset based on
            // the current display orientation.
            var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();

                switch (displayInfo.CurrentOrientation)
                {
                    case Windows.Graphics.Display.DisplayOrientations.Landscape:
                        displayOffset += 0;
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.Portrait:
                        displayOffset += 270;
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.LandscapeFlipped:
                        displayOffset += 180;
                        break;
                    case Windows.Graphics.Display.DisplayOrientations.PortraitFlipped:
                        displayOffset += 90;
                        break;
                }

                var displayCompensatedHeading = (reading.HeadingMagneticNorth + displayOffset) % 360;

                _viewModel.UpdateCompass(displayCompensatedHeading);
            });


        }


        #endregion

        async internal void OrientationChanged(object sender, SimpleOrientationSensorOrientationChangedEventArgs e)
        {
            await _mainPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                UpdateOrientation(e.Orientation);
            });
        }


        private void UpdateOrientation(SimpleOrientation orientation)
        {
            if (orientation == SimpleOrientation.Faceup)
            {
                _viewModel.IsTilted = false;
            }
            else
            {
                _viewModel.IsTilted = true;
            }
        }
    }
}
