using MyLinijka.Models;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Xml.Serialization;

namespace MyLinijka
{
    [Serializable]
    public class DrawOptionsModel : INotifyPropertyChanged
    {
        public InfoPanel StatsDataContext { get; set; } = new InfoPanel();

        private Brush lineFill;
        private Brush lineStroke;
        private double lineThickness;

        private Brush rectFill;
        private Brush rectStroke;
        private double rectThickness;

        public DrawOptionsModel()
        {
            this.LineFill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF98BFF7"));
            this.LineStroke = Brushes.Black;
            this.LineThickness = 1;

            this.RectFill = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF618DCD"));
            this.RectStroke = Brushes.Black;
            this.RectThickness = 1;
        }

        public Brush LineFill
        {
            get
            {
                return lineFill;
            }

            set
            {
                lineFill = value;
                OnPropertyChanged("LineFill");
            }
        }

        public Brush LineStroke
        {
            get
            {
                return lineStroke;
            }

            set
            {
                lineStroke = value;
                OnPropertyChanged("LineStroke");
            }
        }

        public double LineThickness
        {
            get
            {
                return lineThickness;
            }

            set
            {
                lineThickness = value;
                OnPropertyChanged("LineThickness");
            }
        }

        public Brush RectFill
        {
            get
            {
                return rectFill;
            }

            set
            {
                rectFill = value;
                OnPropertyChanged("RectFill");
            }
        }

        public Brush RectStroke
        {
            get
            {
                return rectStroke;
            }

            set
            {
                rectStroke = value;
                OnPropertyChanged("RectStroke");
            }
        }

        public double RectThickness
        {
            get
            {
                return rectThickness;
            }

            set
            {
                rectThickness = value;
                OnPropertyChanged("RectThickness");
            }
        }

        public static void Serialize(DrawOptionsModel arg)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DrawOptionsModel));
            using (TextWriter writer = new StreamWriter("ConfigFile.xml"))
            {
                serializer.Serialize(writer, arg);
            }
        }

        public void Serialize()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DrawOptionsModel));
            using (TextWriter writer = new StreamWriter("ConfigFile.xml"))
            {
                serializer.Serialize(writer, this);
            }
        }

        static public DrawOptionsModel Deserialize()
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(DrawOptionsModel));
            DrawOptionsModel obj;
            using (TextReader reader = new StreamReader("ConfigFile.xml"))
            {
                obj = (DrawOptionsModel)deserializer.Deserialize(reader);
                //AddressDetails XmlData = (AddressDetails)obj;
            }

            return obj;
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