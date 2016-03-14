namespace GeoAR
{
    public class GeoItem
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Name { get; set; }

        public virtual Windows.Devices.Geolocation.Geopoint Location
        {
            get
            {
                return new Windows.Devices.Geolocation.Geopoint 
                    (new Windows.Devices.Geolocation.BasicGeoposition { Latitude = Lat, Longitude = Lon });
                
            }
            set
            {
                Lat = value.Position.Latitude;
                Lon = value.Position.Longitude;
            }
        }

        public virtual double Distance { get; internal set; }
        public virtual double Angle { get; internal set; }
    }
}