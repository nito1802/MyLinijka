using MyLinijka.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MyLinijka
{
    enum AlignEnum
    {
        None = 0, Left, Up, Right, Down
    }

    enum CreateShape
    {
        None = 0, Line, Rectangle
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsMouseDown = false;
        private bool IsShiftLine = false;
        private AlignEnum alignE = AlignEnum.None;
        private CreateShape createShape = CreateShape.Line;

        DrawOptionsModel toolbarOptions = new DrawOptionsModel();
        TextBox tbFocusable;
        private readonly DispatcherTimer settingsTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
        private bool settingsReady;
        private bool settingsErrorShown;


        private Line activeLine;
        private Rectangle activeRectangle;
        private Border activeColorBorder;
        private List<Shape> listOfShapes = new List<Shape>();
        private Stack<StateAction> stackUndoOfShapes = new Stack<StateAction>();
        private Stack<StateAction> stackRedoOfShapes = new Stack<StateAction>();

        public MainWindow()
        {
            InitializeComponent();

            Width = System.Windows.SystemParameters.WorkArea.Width;
            Height = System.Windows.SystemParameters.WorkArea.Height;
            Left = Top = 0;
            Cursor = Cursors.Cross;
            ToolbarGrid.DataContext = toolbarOptions;

            ChangeColorGrid.Visibility = Visibility.Collapsed;
            settingsTimer.Tick += (_, _) => SaveSettings();
            toolbarOptions.PropertyChanged += (_, _) => QueueSettingsSave();
            toolbarOptions.StatsDataContext.PropertyChanged += (_, _) => QueueSettingsSave();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = AppSettings.Load();
                settings.Apply(toolbarOptions);
                Canvas.SetLeft(ToolbarGrid, Math.Clamp(settings.PanelLeft, 0,
                    Math.Max(0, MainCanvas.ActualWidth - ToolbarGrid.ActualWidth)));
                Canvas.SetTop(ToolbarGrid, Math.Clamp(settings.PanelTop, 0,
                    Math.Max(0, MainCanvas.ActualHeight - ToolbarGrid.ActualHeight)));
                SelectShape(settings.RectangleSelected ? CreateShape.Rectangle : CreateShape.Line);
                if (settings.ClickThrough) btnSwitch_Click(btnSwitch, new RoutedEventArgs());
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
            {
                MessageBox.Show(this, $"Nie udało się odczytać ustawień:\n{AppSettings.FilePath}\n\n{error.Message}",
                    "Ustawienia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            settingsReady = true;
            if (!File.Exists(AppSettings.FilePath)) QueueSettingsSave();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || ToolbarGrid.IsMouseOver || ChangeColorGrid.IsMouseOver)
                return;

            if (tbFocusable != null)
                tbFocusable.MoveFocus(new TraversalRequest(FocusNavigationDirection.Last));
            //Keyboard.ClearFocus();
            //Focus();
            ChangeColorGrid.Visibility = Visibility.Collapsed;
            //e.Handled = true;
            //return;
            //if(e.LeftButton == MouseButtonState.Pressed)
            IsMouseDown = true;

            if (IsMouseDown && createShape == CreateShape.Line)
            {
                var mousePosLine = e.GetPosition(this);
                mousePosLine.X = Math.Round(mousePosLine.X);
                mousePosLine.Y = Math.Round(mousePosLine.Y);

                toolbarOptions.StatsDataContext.StartPoint = mousePosLine;
                if (null == activeLine)
                {
                    activeLine = new Line();
                    activeLine.Fill = toolbarOptions.LineFill;

                    activeLine.StrokeThickness = toolbarOptions.LineThickness;
                    activeLine.Stroke = toolbarOptions.LineStroke;

                    var fillColor = ((SolidColorBrush)(activeLine.Fill)).Color;
                    fillColor.A = 200;
                    activeLine.Fill = new SolidColorBrush(fillColor);

                    var fillStroke = ((SolidColorBrush)(activeLine.Stroke)).Color;
                    fillStroke.A = 200;
                    activeLine.Stroke = new SolidColorBrush(fillStroke);

                    MainCanvas.Children.Add(activeLine);
                    listOfShapes.Add(activeLine);
                    stackUndoOfShapes.Push(new StateAction(activeLine, StateAction.TypeOfAction.Normal));
                }

                activeLine.X1 = toolbarOptions.StatsDataContext.StartPoint.X;
                activeLine.Y1 = toolbarOptions.StatsDataContext.StartPoint.Y;

                this.CaptureMouse();
            }
            else if (IsMouseDown && createShape == CreateShape.Rectangle)
            {
                var mousePosRect = e.GetPosition(this);
                mousePosRect.X = Math.Round(mousePosRect.X);
                mousePosRect.Y = Math.Round(mousePosRect.Y);

                toolbarOptions.StatsDataContext.StartPoint = mousePosRect;

                if (null == activeRectangle)
                {
                    activeRectangle = new Rectangle();
                    activeRectangle.Fill = toolbarOptions.RectFill;
                    activeRectangle.Stroke = toolbarOptions.RectStroke;
                    activeRectangle.StrokeThickness = toolbarOptions.RectThickness;

                    var fillColor = ((SolidColorBrush)(activeRectangle.Fill)).Color;
                    fillColor.A = 160;
                    activeRectangle.Fill = new SolidColorBrush(fillColor);

                    var fillStroke = ((SolidColorBrush)(activeRectangle.Stroke)).Color;
                    fillStroke.A = 160;
                    activeRectangle.Stroke = new SolidColorBrush(fillStroke);

                    MainCanvas.Children.Add(activeRectangle);
                    listOfShapes.Add(activeRectangle);
                    stackUndoOfShapes.Push(new StateAction(activeRectangle, StateAction.TypeOfAction.Normal));
                }
                //activeRectangle.X1 = lineStatsPanelData.StartPoint.X;
                //activeLine.Y1 = lineStatsPanelData.StartPoint.Y;

                Canvas.SetLeft(activeRectangle, toolbarOptions.StatsDataContext.StartPoint.X);
                Canvas.SetTop(activeRectangle, toolbarOptions.StatsDataContext.StartPoint.Y);

                this.CaptureMouse();

            }


            base.OnMouseDown(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            //Debug.WriteLine("DOWNNNNNNNNNN");
            if (createShape == CreateShape.Line && !IsShiftLine && (e.Key == Key.LeftShift || e.Key == Key.RightShift))
            {
                IsShiftLine = true;
                var lineInfoPanel = toolbarOptions.StatsDataContext;

                if (null != activeLine)
                {
                    double tempAngle = GetAngle();

                    if (tempAngle > -2.5 && tempAngle < -0.64) { alignE = AlignEnum.Up; lineInfoPanel.Angle = -90; Debug.WriteLine("UP"); }
                    else if (tempAngle > -0.64 && tempAngle < 0.64) { alignE = AlignEnum.Right; lineInfoPanel.Angle = 0; Debug.WriteLine("right"); }
                    else if (tempAngle > 0.64 && tempAngle < 2.5) { alignE = AlignEnum.Down; lineInfoPanel.Angle = 90; Debug.WriteLine("down"); }
                    else { alignE = AlignEnum.Left; lineInfoPanel.Angle = 180; Debug.WriteLine("left"); }

                    switch (alignE)
                    {
                        case AlignEnum.Up:
                            activeLine.X2 = lineInfoPanel.StartPoint.X;
                            activeLine.Y2 = lineInfoPanel.EndPoint.Y;
                            lineInfoPanel.EndPoint = new Point(Math.Round(lineInfoPanel.StartPoint.X), Math.Round(lineInfoPanel.EndPoint.Y));
                            break;

                        case AlignEnum.Right:
                            activeLine.X2 = lineInfoPanel.EndPoint.X;
                            activeLine.Y2 = lineInfoPanel.StartPoint.Y;
                            lineInfoPanel.EndPoint = new Point(Math.Round(lineInfoPanel.EndPoint.X), Math.Round(lineInfoPanel.StartPoint.Y));
                            break;

                        case AlignEnum.Down:
                            activeLine.X2 = lineInfoPanel.StartPoint.X;
                            activeLine.Y2 = lineInfoPanel.EndPoint.Y;
                            lineInfoPanel.EndPoint = new Point(Math.Round(lineInfoPanel.StartPoint.X), Math.Round(lineInfoPanel.EndPoint.Y));
                            break;

                        case AlignEnum.Left:
                            activeLine.X2 = lineInfoPanel.EndPoint.X;
                            activeLine.Y2 = lineInfoPanel.StartPoint.Y;
                            lineInfoPanel.EndPoint = new Point(Math.Round(lineInfoPanel.EndPoint.X), Math.Round(lineInfoPanel.StartPoint.Y));
                            break;
                    }
                }
            }

            if (e.Key == Key.S)
            {
                SelectShape(createShape == CreateShape.Line ? CreateShape.Rectangle : CreateShape.Line);
            }
            else if (e.Key == Key.A)
            {
                btnSwitch.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            IsShiftLine = false;
            Cursor = Cursors.Cross;

            base.OnPreviewKeyUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseDown)
            {
                if (createShape == CreateShape.Line)
                {
                    var lineInfoPanel = toolbarOptions.StatsDataContext;

                    var mousePosLine = e.GetPosition(this);
                    mousePosLine.X = Math.Round(mousePosLine.X);
                    mousePosLine.Y = Math.Round(mousePosLine.Y);

                    lineInfoPanel.EndPoint = mousePosLine;
                    double tempAngle = GetAngle();

                    //statsPanelData.Angle = Vector.AngleBetween(new Vector(statsPanelData.StartPoint.X, statsPanelData.StartPoint.Y),
                    //               new Vector(statsPanelData.EndPoint.X, statsPanelData.EndPoint.Y));

                    if (IsShiftLine)
                    {
                        if (tempAngle > -2.5 && tempAngle < -0.64) { alignE = AlignEnum.Up; lineInfoPanel.Angle = -90; }
                        else if (tempAngle > -0.64 && tempAngle < 0.64) { alignE = AlignEnum.Right; lineInfoPanel.Angle = 0; }
                        else if (tempAngle > 0.64 && tempAngle < 2.5) { alignE = AlignEnum.Down; lineInfoPanel.Angle = 90; }
                        else { alignE = AlignEnum.Left; lineInfoPanel.Angle = 180; }
                    }
                    else
                    {
                        alignE = AlignEnum.None;
                        lineInfoPanel.Angle = RadianToDegree(tempAngle);
                    }


                    switch (alignE)
                    {
                        case AlignEnum.None:
                            activeLine.X2 = lineInfoPanel.EndPoint.X;
                            activeLine.Y2 = lineInfoPanel.EndPoint.Y;
                            break;

                        case AlignEnum.Up:
                            activeLine.X2 = lineInfoPanel.StartPoint.X;
                            activeLine.Y2 = lineInfoPanel.EndPoint.Y;
                            lineInfoPanel.EndPoint = new Point(Math.Round(lineInfoPanel.StartPoint.X), Math.Round(lineInfoPanel.EndPoint.Y));
                            break;

                        case AlignEnum.Right:
                            activeLine.X2 = lineInfoPanel.EndPoint.X;
                            activeLine.Y2 = lineInfoPanel.StartPoint.Y;
                            lineInfoPanel.EndPoint = new Point(Math.Round(lineInfoPanel.EndPoint.X), Math.Round(lineInfoPanel.StartPoint.Y));
                            break;

                        case AlignEnum.Down:
                            activeLine.X2 = lineInfoPanel.StartPoint.X;
                            activeLine.Y2 = lineInfoPanel.EndPoint.Y;
                            lineInfoPanel.EndPoint = new Point(Math.Round(lineInfoPanel.StartPoint.X), Math.Round(lineInfoPanel.EndPoint.Y));
                            break;

                        case AlignEnum.Left:
                            activeLine.X2 = lineInfoPanel.EndPoint.X;
                            activeLine.Y2 = lineInfoPanel.StartPoint.Y;
                            lineInfoPanel.EndPoint = new Point(Math.Round(lineInfoPanel.EndPoint.X), Math.Round(lineInfoPanel.StartPoint.Y));
                            break;
                    }

                    lineInfoPanel.LengthLine = Distance(lineInfoPanel.StartPoint, lineInfoPanel.EndPoint);
                }
                else if (createShape == CreateShape.Rectangle)
                {
                    var rectInfoPanel = toolbarOptions.StatsDataContext;

                    var mousePosRect = e.GetPosition(this);
                    mousePosRect.X = Math.Round(mousePosRect.X);
                    mousePosRect.Y = Math.Round(mousePosRect.Y);

                    rectInfoPanel.EndPoint = mousePosRect;

                    if (rectInfoPanel.EndPoint.X >= rectInfoPanel.StartPoint.X)
                    {
                        Canvas.SetLeft(activeRectangle, rectInfoPanel.StartPoint.X);
                        activeRectangle.Width = rectInfoPanel.EndPoint.X - rectInfoPanel.StartPoint.X;
                    }
                    else
                    {
                        Canvas.SetLeft(activeRectangle, rectInfoPanel.StartPoint.X - (rectInfoPanel.StartPoint.X - rectInfoPanel.EndPoint.X));
                        activeRectangle.Width = rectInfoPanel.StartPoint.X - rectInfoPanel.EndPoint.X;
                    }

                    if (rectInfoPanel.EndPoint.Y >= rectInfoPanel.StartPoint.Y)
                    {
                        Canvas.SetTop(activeRectangle, rectInfoPanel.StartPoint.Y);
                        activeRectangle.Height = rectInfoPanel.EndPoint.Y - rectInfoPanel.StartPoint.Y;
                    }
                    else
                    {
                        Canvas.SetTop(activeRectangle, rectInfoPanel.StartPoint.Y - (rectInfoPanel.StartPoint.Y - rectInfoPanel.EndPoint.Y));
                        activeRectangle.Height = rectInfoPanel.StartPoint.Y - rectInfoPanel.EndPoint.Y;
                    }

                    rectInfoPanel.Width = activeRectangle.ActualWidth;
                    rectInfoPanel.Height = activeRectangle.ActualHeight;
                }
            }

            //base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            IsMouseDown = false;
            this.ReleaseMouseCapture();
            activeLine = null;
            activeRectangle = null;

            base.OnMouseUp(e);
        }

        private void ToolbarDragHandle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double left = Canvas.GetLeft(ToolbarGrid);
            double top = Canvas.GetTop(ToolbarGrid);
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            Canvas.SetLeft(ToolbarGrid, Math.Clamp(left + e.HorizontalChange,
                0, Math.Max(0, MainCanvas.ActualWidth - ToolbarGrid.ActualWidth)));
            Canvas.SetTop(ToolbarGrid, Math.Clamp(top + e.VerticalChange,
                0, Math.Max(0, MainCanvas.ActualHeight - ToolbarGrid.ActualHeight)));
            ChangeColorGrid.Visibility = Visibility.Collapsed;
            QueueSettingsSave();
            e.Handled = true;
        }
        public double GetAngle()
        {
            return Math.Atan2(toolbarOptions.StatsDataContext.EndPoint.Y - toolbarOptions.StatsDataContext.StartPoint.Y, toolbarOptions.StatsDataContext.EndPoint.X - toolbarOptions.StatsDataContext.StartPoint.X);
        }

        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        public static double Distance(Point a, Point b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "Czy na pewno usunąć wszystkie figury?", "Usuń wszystko",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
                return;

            stackUndoOfShapes.Push(new StateAction(listOfShapes.ToList(), StateAction.TypeOfAction.DeleteAll));

            listOfShapes.ForEach(x =>
            {
                MainCanvas.Children.Remove(x);
            });
            listOfShapes.Clear();

        }

        private void Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (activeColorBorder != null)
                activeColorBorder.Style = (Style)FindResource("HoverBorder");

            ChangeColorGrid.Visibility = Visibility.Visible;

            Border br = sender as Border;
            br.Style = (Style)FindResource("BorderClicked");
            //br.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#B2FFFFFF"));
            activeColorBorder = br;

            Point brPos = br.PointToScreen(new Point(0, 0));

            Canvas.SetLeft(ChangeColorGrid, brPos.X);
            Canvas.SetTop(ChangeColorGrid, brPos.Y + br.ActualHeight);
            e.Handled = true;
        }

        private void ColorPickup_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border br = sender as Border;

            GetVisualChild<Rectangle>(activeColorBorder).Fill = GetVisualChild<Rectangle>(br).Fill;
            activeColorBorder.Style = (Style)FindResource("HoverBorder");
            activeColorBorder = null;
            ChangeColorGrid.Visibility = Visibility.Collapsed;
            e.Handled = true;
            //Point brPos = br.PointToScreen(new Point(0, 0));
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>
                    (v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (settingsTimer.IsEnabled) SaveSettings();
        }

        private void LineBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectShape(CreateShape.Line);
            e.Handled = true;
        }

        private void RectBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectShape(CreateShape.Rectangle);
            e.Handled = true;
        }

        private void SelectShape(CreateShape shape)
        {
            createShape = shape;
            bool rectangle = shape == CreateShape.Rectangle;
            LineBorder.Style = (Style)FindResource(rectangle ? "ShapeHoverBorder" : "ShapeBorderClicked");
            RectBorder.Style = (Style)FindResource(rectangle ? "ShapeBorderClicked" : "ShapeHoverBorder");
            tBoxLengthStats.Visibility = tBoxAngleStats.Visibility = rectangle ? Visibility.Collapsed : Visibility.Visible;
            tBoxWidthStats.Visibility = tBoxHeightStats.Visibility = rectangle ? Visibility.Visible : Visibility.Collapsed;
            tbLengthStats.Text = rectangle ? "Width" : "Length";
            tbAngleStats.Text = rectangle ? "Height" : "Angle";
            QueueSettingsSave();
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            tbFocusable = sender as TextBox;
        }

        private void btnSwitchTransparent_Click(object sender, RoutedEventArgs e)
        {
            if (Background == Brushes.Transparent)
                Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#01FFFFFF"));
            else
                Background = Brushes.Transparent;
        }

        private void btnSwitch_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            if (Background == Brushes.Transparent)
            {
                Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#01FFFFFF"));
                btn.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF0E910E"));
            }
            else
            {
                Background = Brushes.Transparent;
                btn.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFB6B6B6"));
            }
            QueueSettingsSave();
        }

        private void QueueSettingsSave()
        {
            if (!settingsReady) return;
            settingsTimer.Stop();
            settingsTimer.Start();
        }

        private void SaveSettings()
        {
            settingsTimer.Stop();
            try
            {
                var settings = AppSettings.Capture(toolbarOptions);
                settings.PanelLeft = Canvas.GetLeft(ToolbarGrid);
                settings.PanelTop = Canvas.GetTop(ToolbarGrid);
                settings.RectangleSelected = createShape == CreateShape.Rectangle;
                settings.ClickThrough = Background == Brushes.Transparent;
                settings.Save();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
            {
                if (settingsErrorShown) return;
                settingsErrorShown = true;
                MessageBox.Show(this, $"Nie udało się zapisać ustawień obok aplikacji:\n{AppSettings.FilePath}\n\n{error.Message}",
                    "Ustawienia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }

    public class StateAction : ICloneable
    {
        public Shape shape { get; set; }
        public List<Shape> listOfShape { get; set; }
        public TypeOfAction eTypeAction { get; set; }

        public StateAction(Shape shape, TypeOfAction eTypeAction)
        {
            this.shape = shape;
            this.eTypeAction = eTypeAction;
        }

        public StateAction(List<Shape> listOfShape, TypeOfAction eTypeAction)
        {
            this.listOfShape = new List<Shape>(listOfShape);
            this.eTypeAction = eTypeAction;
        }

        public enum TypeOfAction
        { Normal = 0, DeleteAll }

        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
