using System.ComponentModel;
using System.Windows;

namespace MyLinijka.Models
{
    public class InfoPanel : INotifyPropertyChanged
    {
        private Point startPoint;
        private Point endPoint;
        private double lengthLine;
        private double angle;
        private double width;
        private double height;

        public Point StartPoint
        {
            get
            {
                return startPoint;
            }

            set
            {
                if (!double.IsFinite(value.X) || !double.IsFinite(value.Y)) return;
                startPoint = value;
                OnPropertyChanged("StartPoint");
            }
        }

        public Point EndPoint
        {
            get
            {
                return endPoint;
            }

            set
            {
                if (!double.IsFinite(value.X) || !double.IsFinite(value.Y)) return;
                endPoint = value;
                OnPropertyChanged("EndPoint");
            }
        }

        public double LengthLine
        {
            get
            {
                return lengthLine;
            }

            set
            {
                if (!double.IsFinite(value) || value < 0) return;
                lengthLine = value;
                OnPropertyChanged("LengthLine");
            }
        }

        public double Angle
        {
            get
            {
                return angle;
            }

            set
            {
                if (!double.IsFinite(value)) return;
                angle = value;
                OnPropertyChanged("Angle");
            }
        }

        public double Width
        {
            get
            {
                return width;
            }

            set
            {
                if (!double.IsFinite(value) || value < 0) return;
                width = value;
                OnPropertyChanged("Width");
            }
        }

        public double Height
        {
            get
            {
                return height;
            }

            set
            {
                if (!double.IsFinite(value) || value < 0) return;
                height = value;
                OnPropertyChanged("Height");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged; //INotifyPropertyChanged

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}