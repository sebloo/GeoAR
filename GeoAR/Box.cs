namespace GeoAR
{
    public struct Box
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }

        public bool Intersects(Box other)
        {
            if (this.Left + this.Width < other.Left || other.Left + other.Width < this.Left || this.Top + this.Height < other.Top || other.Top + other.Height < this.Top)
            {
                //Intersection = Empty
                return false;
            }
            else {
                //Intersection = Not Empty
                return true;
            }
        }
    }
}