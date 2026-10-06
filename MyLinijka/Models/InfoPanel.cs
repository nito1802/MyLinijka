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