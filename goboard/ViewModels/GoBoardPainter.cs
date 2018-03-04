using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Quobject.SocketIoClientDotNet.Client;

namespace GoBoard.ViewModels
{
    public class GoBoardPainter : FrameworkElement
    {
        enum END
        {
            TIE,
            BLACK,
            RED
        }
        public static readonly RoutedEvent MovePlayedEvent = EventManager.RegisterRoutedEvent("MovePlayed", RoutingStrategy.Bubble, typeof(MovePlayedEventHandler), typeof(GoBoardPainter));
        public static readonly DependencyProperty BoardSizeProperty = DependencyProperty.Register("BoardSize", typeof(int), typeof(GoBoardPainter), new FrameworkPropertyMetadata(19, new PropertyChangedCallback(OnBoardSizeChanged)), new ValidateValueCallback(BoardSizeValidateCallback));
        public static readonly DependencyProperty MouseHoverTypeProperty = DependencyProperty.Register("MouseHoverType", typeof(GoBoardHoverType), typeof(GoBoardPainter), new FrameworkPropertyMetadata(GoBoardHoverType.None, new PropertyChangedCallback(OnMouseHoverTypeChanged)));

        public delegate void MovePlayedEventHandler(object sender, RoutedMovePlayedEventArgs args);

        public event MovePlayedEventHandler MovePlayed
        {
            add { AddHandler(MovePlayedEvent, value); }
            remove { RemoveHandler(MovePlayedEvent, value); }
        }

        private List<Visual> m_Visuals = new List<Visual>();
        private Dictionary<GoBoardPoint, Stone> m_StoneList = new Dictionary<GoBoardPoint, Stone>();
        private ObservableCollection<GoBoardAnnotation> m_AnnotationsList = new ObservableCollection<GoBoardAnnotation>();
        private Stone m_ToPlay = Stone.Black;

        private DrawingVisual m_BoardVisual, m_StonesVisual, m_StarPointVisual, m_CoordinatesVisual, m_AnnotationVisual, m_MouseHoverVisual;
        private Brush m_BlackStoneBrush, m_RedStoneBrush, m_BoardBrush, m_StoneShadowBrush, m_BlackStoneShadowBrush, m_RedStoneShadowBrush;
        private Pen m_BlackStoneAnnotationPen, m_RedStoneAnnotationPen, m_BlackPen;
        private Typeface m_BoardTypeface;

        private Rect m_GoBoardRect;
        private Rect m_GoBoardHitBox;
        private GoBoardPoint m_MousePosition;
        private string[] m_Coordinates = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V" };
        private int m_Border = 20;
        private int m_BoardSize;
        private double m_BoardWidthFactor = 14;
        private double m_BoardHeightFactor = 15;
        private END _end;
        public GoBoardPainter()
        {
            Resources.Source = new Uri("pack://application:,,,/GoBoard;component/GoBoardPainterResources.xaml");

            m_BlackStoneBrush = (Brush)TryFindResource("blackStoneBrush");
            m_RedStoneBrush = (Brush)TryFindResource("RedStoneBrush");
            m_BoardBrush = (Brush)TryFindResource("boardBrush");
            m_StoneShadowBrush = (Brush)TryFindResource("stoneShadowBrush");
            m_RedStoneAnnotationPen = (Pen)TryFindResource("RedStoneAnnotationPen");
            m_BlackStoneAnnotationPen = (Pen)TryFindResource("blackStoneAnnotationPen");
            m_BlackPen = (Pen)TryFindResource("blackPen");
            m_BlackStoneShadowBrush = (Brush)TryFindResource("blackStoneShadowBrush");
            m_RedStoneShadowBrush = (Brush)TryFindResource("RedStoneShadowBrush");

            m_BoardTypeface = new Typeface("Arial");

            InitializeBoard(this.BoardSize - 1);
        }

        #region Draw Methods

        private void DrawBoard()
        {
            m_BoardVisual = new DrawingVisual();

            using (DrawingContext dc = m_BoardVisual.RenderOpen())
            {
                dc.DrawRectangle(m_BoardBrush, new Pen(Brushes.Black, 0.2), new Rect(0, 0, m_BoardSize * m_BoardWidthFactor + m_Border * 2, m_BoardSize * m_BoardHeightFactor + m_Border * 2));
                dc.DrawRectangle(m_BoardBrush, new Pen(Brushes.Black, 0.2), new Rect(m_Border, m_Border, m_BoardSize * m_BoardWidthFactor, m_BoardSize * m_BoardHeightFactor));

                for (int x = 0; x < m_BoardSize; x++)
                {
                    for (int y = 0; y < m_BoardSize; y++)
                    {
                        dc.DrawRectangle(null, m_BlackPen, new Rect(getPosX(x), getPosY(y), m_BoardWidthFactor, m_BoardHeightFactor));
                    }
                }
            }
        }

        public void DrawStones()
        {
            m_BoardVisual.Children.Remove(m_StonesVisual);
            m_StonesVisual = new DrawingVisual();

            using (DrawingContext dc = m_StonesVisual.RenderOpen())
            {
                foreach (var item in m_StoneList)
                {
                    double posX = getPosX(item.Key.X);
                    double posY = getPosY(item.Key.Y);

                    dc.DrawEllipse(m_StoneShadowBrush, null, new Point(posX + 1, posY + 1), 6.7, 6.7);
                    dc.DrawEllipse(((item.Value == Stone.Red) ? m_RedStoneBrush : m_BlackStoneBrush), m_BlackPen, new Point(posX, posY), m_BoardWidthFactor / 2 - 0.5, m_BoardWidthFactor / 2 - 0.5);
                }
            }

            m_BoardVisual.Children.Add(m_StonesVisual);
        }

        private void DrawStarPoints()
        {
            List<Point> starPointList = new List<Point>();

            if (m_BoardSize == 18)
            {
                starPointList.Add(new Point(getPosX(3), getPosY(3)));
                starPointList.Add(new Point(getPosX(3), getPosY(9)));
                starPointList.Add(new Point(getPosX(3), getPosY(15)));
                starPointList.Add(new Point(getPosX(9), getPosY(3)));
                starPointList.Add(new Point(getPosX(9), getPosY(9)));
                starPointList.Add(new Point(getPosX(9), getPosY(15)));
                starPointList.Add(new Point(getPosX(15), getPosY(3)));
                starPointList.Add(new Point(getPosX(15), getPosY(9)));
                starPointList.Add(new Point(getPosX(15), getPosY(15)));
            }
            else if (m_BoardSize == 12)
            {
                starPointList.Add(new Point(getPosX(3), getPosY(3)));
                starPointList.Add(new Point(getPosX(3), getPosY(6)));
                starPointList.Add(new Point(getPosX(3), getPosY(9)));
                starPointList.Add(new Point(getPosX(6), getPosY(3)));
                starPointList.Add(new Point(getPosX(6), getPosY(6)));
                starPointList.Add(new Point(getPosX(6), getPosY(9)));
                starPointList.Add(new Point(getPosX(9), getPosY(3)));
                starPointList.Add(new Point(getPosX(9), getPosY(6)));
                starPointList.Add(new Point(getPosX(9), getPosY(9)));
            }
            else if (m_BoardSize == 8)
            {
                starPointList.Add(new Point(getPosX(2), getPosY(2)));
                starPointList.Add(new Point(getPosX(2), getPosY(6)));
                starPointList.Add(new Point(getPosX(4), getPosY(4)));
                starPointList.Add(new Point(getPosX(6), getPosY(2)));
                starPointList.Add(new Point(getPosX(6), getPosY(6)));
            }

            m_BoardVisual.Children.Remove(m_StarPointVisual);
            m_StarPointVisual = new DrawingVisual();

            using (DrawingContext dc = m_StarPointVisual.RenderOpen())
            {
                starPointList.ForEach(delegate(Point p)
                {
                    dc.DrawGeometry(Brushes.Black, m_BlackPen, new EllipseGeometry(p, 1.2, 1.2));
                });
            }

            m_BoardVisual.Children.Add(m_StarPointVisual);
        }

        private void DrawCoordinates()
        {
            m_BoardVisual.Children.Remove(m_CoordinatesVisual);
            m_CoordinatesVisual = new DrawingVisual();

            using (DrawingContext dc = m_CoordinatesVisual.RenderOpen())
            {
                for (int i = 0; i < m_BoardSize + 1; i++)
                {
                    double posX = 3;
                    double posY = getPosY(i) - 3;

                    dc.DrawText(new FormattedText((m_BoardSize - i).ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, m_BoardTypeface, 4, Brushes.Black), new Point(posX, posY));

                    posX = getPosX(i) - 1;
                    posY = getPosY(m_BoardSize) + m_Border / 2;

                    dc.DrawText(new FormattedText(m_Coordinates[i], CultureInfo.CurrentCulture, FlowDirection.LeftToRight, m_BoardTypeface, 4, Brushes.Black), new Point(posX, posY));
                }
            }

            m_BoardVisual.Children.Add(m_CoordinatesVisual);
        }

        private void DrawAnnotations()
        {
            m_BoardVisual.Children.Remove(m_AnnotationVisual);
            m_AnnotationVisual = new DrawingVisual();

            using (DrawingContext dc = m_AnnotationVisual.RenderOpen())
            {
                foreach (var anno in m_AnnotationsList)
                {
                    Stone stone = m_StoneList.ContainsKey(anno.Position) ? m_StoneList[anno.Position] : Stone.Empty;
                    Pen annoPen = (stone != Stone.Empty && stone == Stone.Black) ? m_BlackStoneAnnotationPen : m_RedStoneAnnotationPen;
                    Brush annoColor = (stone != Stone.Empty && stone == Stone.Black) ? Brushes.White : Brushes.Black;

                    switch (anno.Type)
                    {
                        case GoBoardAnnotationType.Circle:
                            dc.DrawEllipse(Brushes.Transparent, annoPen, new Point(getPosX(anno.Position.X), getPosY(anno.Position.Y)), m_BoardWidthFactor / 4, m_BoardWidthFactor / 4);
                            break;
                        case GoBoardAnnotationType.Rectangle:
                            dc.DrawRectangle(Brushes.Transparent, annoPen, new Rect(new Point(getPosX(anno.Position.X) - m_BoardWidthFactor / 4, getPosY(anno.Position.Y) - m_BoardHeightFactor / 4), new Size(m_BoardWidthFactor / 2, m_BoardHeightFactor / 2)));
                            break;
                        case GoBoardAnnotationType.Label:
                            FormattedText text = new FormattedText(anno.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, m_BoardTypeface, 8, annoColor);
                            dc.DrawRectangle(stone == Stone.Empty ? m_BoardBrush : Brushes.Transparent, null, new Rect(new Point(getPosX(anno.Position.X) - m_BoardWidthFactor / 4, getPosY(anno.Position.Y) - m_BoardHeightFactor / 4), new Size(m_BoardWidthFactor / 2, m_BoardHeightFactor / 2)));
                            dc.DrawText(text, new Point(getPosX(anno.Position.X) - text.Width / 2, getPosY(anno.Position.Y) - text.Height / 2));
                            break;
                        case GoBoardAnnotationType.Triangle:
                            string first = getPosX(anno.Position.X) + "," + (getPosY(anno.Position.Y) - 5).ToString().Replace(',', '.');
                            string second = (getPosX(anno.Position.X) - 4).ToString().Replace(',', '.') + "," + (getPosY(anno.Position.Y) + 3).ToString().Replace(',', '.');
                            string third = (getPosX(anno.Position.X) + 4).ToString().Replace(',', '.') + "," + (getPosY(anno.Position.Y) + 3).ToString().Replace(',', '.');
                            dc.DrawGeometry(Brushes.Transparent, annoPen, Geometry.Parse("M " + first + " L " + second + " L " + third + " Z"));
                            break;
                        default:
                            break;
                    }
                }
            }

            m_BoardVisual.Children.Add(m_AnnotationVisual);
        }

        private void DrawMouseHoverVisual()
        {
            m_BoardVisual.Children.Remove(m_MouseHoverVisual);
            m_MouseHoverVisual = new DrawingVisual();

            using (DrawingContext dc = m_MouseHoverVisual.RenderOpen())
            {
                switch (MouseHoverType)
                {
                    case GoBoardHoverType.Stone:
                        if (m_MousePosition.Equals(GoBoardPoint.Empty) || m_StoneList.ContainsKey(m_MousePosition)) break;
                        double posX = getPosX(m_MousePosition.X);
                        double posY = getPosY(m_MousePosition.Y);

                        dc.DrawEllipse(((m_ToPlay == Stone.Red) ? m_RedStoneShadowBrush : m_BlackStoneShadowBrush), null, new Point(posX, posY), m_BoardWidthFactor / 2 - 0.5, m_BoardWidthFactor / 2 - 0.5);
                        break;
                    case GoBoardHoverType.None:
                    default:
                        break;
                }
            }

            m_BoardVisual.Children.Add(m_MouseHoverVisual);
        }

        #endregion

        private void InitializeBoard(int boardSize)
        {
            m_Visuals.ForEach(delegate(Visual v) { RemoveVisualChild(v); });

            m_Visuals.Clear();
            m_StoneList.Clear();
            m_AnnotationsList.Clear();
            m_AnnotationsList.CollectionChanged += new NotifyCollectionChangedEventHandler(m_AnnotationsList_CollectionChanged);

            m_BoardSize = boardSize;

            m_GoBoardRect = new Rect(new Size(m_BoardSize * m_BoardWidthFactor, m_BoardSize * m_BoardHeightFactor));
            m_GoBoardHitBox = m_GoBoardRect;
            m_GoBoardHitBox.Inflate((m_BoardWidthFactor / 2), (m_BoardHeightFactor / 2));

            this.Width = m_GoBoardRect.Width + m_Border * 2;
            this.Height = m_GoBoardRect.Height + m_Border * 2;

            DrawBoard();
            DrawCoordinates();
            DrawStarPoints();
            DrawStones();
            DrawMouseHoverVisual();

            m_Visuals.Add(m_BoardVisual);

            m_Visuals.ForEach(delegate(Visual v) { AddVisualChild(v); });
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            Point pos = e.GetPosition(this);

            if (!m_GoBoardHitBox.Contains(new Point(pos.X - m_Border, pos.Y - m_Border))) return;

            int x = (int)Math.Round((pos.X - m_Border) / (m_GoBoardRect.Width / m_BoardSize));
            int y = (int)Math.Round((pos.Y - m_Border) / (m_GoBoardRect.Height / m_BoardSize));

            RaiseEvent(new RoutedMovePlayedEventArgs(MovePlayedEvent, this, new GoBoardPoint(x, y), m_ToPlay));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point pos = e.GetPosition(this);

            if (!m_GoBoardHitBox.Contains(new Point(pos.X - m_Border, pos.Y - m_Border)))
            {
                m_MousePosition = GoBoardPoint.Empty;
                DrawMouseHoverVisual();
                return;
            }

            int x = (int)Math.Round((pos.X - m_Border) / (m_GoBoardRect.Width / m_BoardSize));
            int y = (int)Math.Round((pos.Y - m_Border) / (m_GoBoardRect.Height / m_BoardSize));

            m_MousePosition = new GoBoardPoint(x, y);
            DrawMouseHoverVisual();
        }

        private void m_AnnotationsList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DrawAnnotations();
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return m_Visuals.Count;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= m_Visuals.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            return m_Visuals[index];
        }

        private double getPosX(double value)
        {
            return m_BoardWidthFactor * value + m_Border;
        }

        private double getPosY(double value)
        {
            return m_BoardHeightFactor * value + m_Border;
        }

        public int getX(int value)
        {
            return (int)Math.Round((value - m_Border) / (m_GoBoardRect.Width / m_BoardSize));
        }

        public int getY(int value)
        {
            return (int)Math.Round((value - m_Border) / (m_GoBoardRect.Height / m_BoardSize));
        }

        private static void OnBoardSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            (sender as GoBoardPainter).InitializeBoard((sender as GoBoardPainter).BoardSize - 1);
        }

        private static void OnMouseHoverTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
        }

        private static bool BoardSizeValidateCallback(object target)
        {
            if ((int)target < 2 || (int)target > 19)
                return false;

            return true;
        }

        public void Redraw()
        {
            DrawStones();
            DrawAnnotations();
        }

        public int BoardSize
        {
            get { return (int)GetValue(BoardSizeProperty); }
            set { SetValue(BoardSizeProperty, value); }
        }

        public GoBoardHoverType MouseHoverType
        {
            get { return (GoBoardHoverType)GetValue(MouseHoverTypeProperty); }
            set { SetValue(MouseHoverTypeProperty, value); }
        }

        public Stone ToPlay
        {
            get { return m_ToPlay; }
            set { m_ToPlay = value; }
        }

        public Dictionary<GoBoardPoint, Stone> StoneList
        {
            get { return m_StoneList; }
            set
            {
                m_StoneList = value;
                DrawStones();
            }
        }

        public ObservableCollection<GoBoardAnnotation> AnnotationList
        {
            get { return m_AnnotationsList; }
            set
            {
                m_AnnotationsList = value;
                DrawAnnotations();
            }
        }

        #region Check Win
        public void messageEnd()
        {
            switch(_end)
            {
                case END.BLACK: MessageBox.Show("Quân đen thắng!!"); break;
                case END.RED: MessageBox.Show("Quân đỏ thắng!!"); break;
                case END.TIE: MessageBox.Show("Hòa!!"); break;
            }

        }

        public bool onWin()
        {

            if (m_StoneList.Count == (m_BoardSize + 1) * (m_BoardSize + 1))
            {
                _end = END.TIE;
                return true;
            }
            
            for (int i = 0; i < 12; i++)
            {
                if (checkVertical(i, Stone.Black) || checkHorizontal(i, Stone.Black) || checkCrossRight(i, Stone.Black) || checkCrossLeft(i, Stone.Black))
                {
                    _end = END.BLACK;
                    return true;
                }
                if (checkVertical(i, Stone.Red) || checkHorizontal(i, Stone.Red) || checkCrossRight(i, Stone.Red) || checkCrossLeft(i, Stone.Red))
                {
                    _end = END.RED;
                    return true;
                }
            }
            return false;
        }
        private bool checkVertical(int currCol, Stone currGo)
        {
            int wV = 0;
            int wY = -1;
            for (wY = 0; wY < 12; wY++)
            {         
                if (m_StoneList.ContainsKey(new GoBoardPoint(currCol, wY)))
                {
                    foreach (var item in m_StoneList)
                    {
                        
                        if (item.Key.X == currCol && item.Key.Y == wY)
                        {
                            if (item.Value == currGo)
                            {
                                wV++;
                                if (wV > 4 && (!m_StoneList.ContainsKey(new GoBoardPoint(currCol, wY + 1)) || wY == m_BoardSize - 1 || wY - 4 == 0))
                                    return true;
                            }
                            else wV = 0;
                        }
                    }
                } else wV = 0;
            }
            return false;
        }
        private bool checkHorizontal(int currRow, Stone currGo)
        {
            int wV = 0;
            int wX = -1;
            for (wX = 0; wX < 12; wX++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(wX, currRow)))
                {
                    foreach (var item in m_StoneList)
                    {

                        if (item.Key.X == wX && item.Key.Y == currRow)
                        {
                            if (item.Value == currGo)
                            {
                                wV++;
                                if (wV > 4 && (!m_StoneList.ContainsKey(new GoBoardPoint(wX + 1, currRow)) || wX == m_BoardSize - 1 || wX - 4 == 0))
                                    return true;
                            }
                            else wV = 0;
                        }
                    }
                }
                else wV = 0;
            }
            return false;
        }
        private bool checkCrossRight(int currColl, Stone currGo)
        {
            int cR = 0;
            int wX = currColl;
            int sP = 0;
            for (int wY = sP; wY < 12; wY++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(wX, wY)))
                {
                    foreach (var item in m_StoneList)
                    {

                        if (item.Key.X == wX && item.Key.Y == wY)
                        {
                            if (item.Value == currGo)
                            {
                                cR++;
                                if (cR > 4 && (!m_StoneList.ContainsKey(new GoBoardPoint(wX + 1, wY + 1)) || wX == m_BoardSize - 1 || wX - 4 == 0 || wY == m_BoardSize - 1 || wY - 4 == 0))
                                    return true;
                            }
                            else cR = 0;
                        }
                    }
                    wX++;
                }
                else
                {
                    cR = 0;
                    wX = currColl;
                    sP++;
                    if (m_StoneList.ContainsKey(new GoBoardPoint(wX, sP)))
                        wY = sP - 1;
                }
            } return false;
        }
        private bool checkCrossLeft(int currColl, Stone currGo)
        {
            int cR = 0;
            int wX = currColl;
            int sP = 0;
            for (int wY = 0; wY < 12; wY++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(wX, wY)))
                {
                    foreach (var item in m_StoneList)
                    {

                        if (item.Key.X == wX && item.Key.Y == wY)
                        {
                            if (item.Value == currGo)
                            {
                                cR++;
                                if (cR > 4 && (!m_StoneList.ContainsKey(new GoBoardPoint(wX + 1, wY + 1)) || wX == m_BoardSize - 1 || wX - 4 == 0 || wY == m_BoardSize - 1 || wY - 4 == 0))
                                    return true;
                            }
                            else cR = 0;
                        }
                    }
                    wX--;
                }
                else
                {
                    cR = 0;
                    wX = currColl;
                    sP++;
                    if (m_StoneList.ContainsKey(new GoBoardPoint(wX, sP)))
                        wY = sP - 1;
                }
            }
            return false;
        }
        #endregion

        #region PvC AI
        private long[] markAttack = new long[7] { 0, 9, 54, 162, 1458, 13112, 118008 };
        private long[] markDefend = new long[7] { 0, 3, 27, 99, 729, 6561, 59049 };
        public GoBoardPoint PlayerVsCOM()
        {
            GoBoardPoint gp = new GoBoardPoint(m_BoardSize / 2 - 1, m_BoardSize / 2 - 1);
            if (m_StoneList.Count != 0)
            { 
                gp = findPoint();
            }
            return gp;
        }

        private GoBoardPoint findPoint()
        {
            GoBoardPoint gp = new GoBoardPoint();
            long markMax = 0;
            for (int i = 0; i <= m_BoardSize; i++)
            {
                for (int j = 0; j <= m_BoardSize; j++)
                {
                    if (!m_StoneList.ContainsKey(new GoBoardPoint(i, j)))
                    {
                        long markAttack = markAttack_Vertical(i, j) + markAttack_Horizontal(i, j) + markAttack_CrossRight(i, j) + markAttack_CrossLeft(i, j);
                        long markĐefend = markDefend_Vertical(i, j) + markDefend_Horizontal(i, j) + markDefend_CrossRight(i, j) + markDefend_CrossLeft(i, j);
                        long Temp = markAttack > markĐefend ? markAttack : markĐefend;
                        if (markMax <= Temp)
                        {
                            markMax = Temp;
                            gp = new GoBoardPoint(i, j);
                        }
                    }
                }
            }
            return gp;
        }



        #region Attack
        private long markAttack_Vertical(int col, int row)
        {
            int t = 0;
            long mark = 0;
            int countTeammate = 0;
            int countOpponent = 0;
            for (int i = 1; i < 6 && row + i < m_BoardSize; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col, row + i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col && item.Key.Y == row + i)
                        {
                            if (item.Value == m_ToPlay)
                                countTeammate++;
                            else
                            {
                                countOpponent++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countOpponent)
                {
                    t = countOpponent;
                    break;
                }   
            }
            
            for (int i = 1; i < 6 && row - i >= 0; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col, row - i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col && item.Key.Y == row - i)
                        {
                            if (item.Value == m_ToPlay)
                                countTeammate++;
                            else
                            {
                                countOpponent++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countOpponent)
                    break;
            }
            if (countOpponent == 2)
                return 0;
            mark -= (markDefend[countOpponent + 1]*2);
            mark += markAttack[countTeammate];
            return mark;
        }
        private long markAttack_Horizontal(int col, int row)
        {
            int t = 0;
            long mark = 0;
            int countTeammate = 0;
            int countOpponent = 0;
            for (int i = 1; i < 6 && col + i < m_BoardSize; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col + i, row)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col + i && item.Key.Y == row)
                        {
                            if (item.Value == m_ToPlay)
                                countTeammate++;
                            else
                            {
                                countOpponent++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countOpponent)
                {
                    t = countOpponent;
                    break;
                }
            }

            for (int i = 1; i < 6 && col - i >= 0; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col - i, row)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col - i && item.Key.Y == row)
                        {
                            if (item.Value == m_ToPlay)
                                countTeammate++;
                            else
                            {
                                countOpponent++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countOpponent)
                    break;
            }
            if (countOpponent == 2)
                return 0;
            mark -= (markDefend[countOpponent + 1]*2);
            mark += markAttack[countTeammate];
            return mark;
        }
        private long markAttack_CrossRight(int col, int row)
        {
            int t = 0;
            long mark = 0;
            int countTeammate = 0;
            int countOpponent = 0;
            for (int i = 1; i < 6 && row - i >= 0 && col - i >= 0; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col - i, row - i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col - i && item.Key.Y == row - i)
                        {
                            if (item.Value == m_ToPlay)
                                countTeammate++;
                            else
                            {
                                countOpponent++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countOpponent)
                {
                    t = countOpponent;
                    break;
                }
            }

            for (int i = 1; i < 6 && row + i < m_BoardSize && col + i < m_BoardSize; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col + i, row + i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col + i && item.Key.Y == row + i)
                        {
                            if (item.Value == m_ToPlay)
                                countTeammate++;
                            else
                            {
                                countOpponent++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countOpponent)
                    break;
            }
            if (countOpponent == 2)
                return 0;
            mark -= (markDefend[countOpponent + 1]*2);
            mark += markAttack[countTeammate];
            return mark;
        }
        private long markAttack_CrossLeft(int col, int row)
        {
            int t = 0;
            long mark = 0;
            int countTeammate = 0;
            int countOpponent = 0;
            for (int i = 1; i < 6 && row - i >= 0 && col + i < m_BoardSize; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col + i, row - i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col + i && item.Key.Y == row - i)
                        {
                            if (item.Value == m_ToPlay)
                                countTeammate++;
                            else
                            {
                                countOpponent++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countOpponent)
                {
                    t = countOpponent;
                    break;
                }
            }

            for (int i = 1; i < 6 && row + i < m_BoardSize && col - i >= 0; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col - i, row + i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col - i && item.Key.Y == row + i)
                        {
                            if (item.Value == m_ToPlay)
                                countTeammate++;
                            else
                            {
                                countOpponent++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countOpponent)
                    break;
            }
            if (countOpponent == 2)
                return 0;
            mark -= (markDefend[countOpponent + 1]*2);
            mark += markAttack[countTeammate];
            return mark;
        }
        #endregion

        #region Defend
        private long markDefend_Vertical(int col, int row)
        {
            int t = 0;
            long mark = 0;
            int countTeammate = 0;
            int countOpponent = 0;
            for (int i = 1; i < 6 && row + i < m_BoardSize; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col, row + i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col && item.Key.Y == row + i)
                        {
                            if (item.Value != m_ToPlay)
                                countOpponent++;
                            else
                            {
                                countTeammate++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countTeammate)
                {
                    t = countTeammate;
                    break;
                }
            }

            for (int i = 1; i < 6 && row - i >= 0; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col, row - i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col && item.Key.Y == row - i)
                        {
                            if (item.Value != m_ToPlay)
                                countOpponent++;
                            else
                            {
                                countTeammate++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countTeammate)
                    break;
            }
            if (countTeammate == 2)
                return 0;
            mark += markDefend[countOpponent];
            return mark;
        }
        private long markDefend_Horizontal(int col, int row)
        {
            int t = 0;
            long mark = 0;
            int countTeammate = 0;
            int countOpponent = 0;
            for (int i = 1; i < 6 && col + i < m_BoardSize; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col + i, row)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col + i && item.Key.Y == row)
                        {
                            if (item.Value != m_ToPlay)
                                countOpponent++;
                            else
                            {
                                countTeammate++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countTeammate)
                {
                    t = countTeammate;
                    break;
                }
            }

            for (int i = 1; i < 6 && col - i >= 0; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col - i, row)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col - i && item.Key.Y == row)
                        {
                            if (item.Value != m_ToPlay)
                                countOpponent++; 
                            else
                            {
                                countTeammate++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countTeammate)
                    break;
            }
            if (countTeammate == 2)
                return 0;
            mark += markDefend[countOpponent];
            return mark;
        }
        private long markDefend_CrossRight(int col, int row)
        {
            int t = 0;
            long mark = 0;
            int countTeammate = 0;
            int countOpponent = 0;
            for (int i = 1; i < 6 && row - i >= 0 && col - i >= 0; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col - i, row - i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col - i && item.Key.Y == row - i)
                        {
                            if (item.Value != m_ToPlay)
                                countOpponent++; 
                            else
                            {
                                countTeammate++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countTeammate)
                {
                    t = countTeammate;
                    break;
                }
            }

            for (int i = 1; i < 6 && row + i < m_BoardSize && col + i < m_BoardSize; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col + i, row + i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col + i && item.Key.Y == row + i)
                        {
                            if (item.Value != m_ToPlay)
                                countOpponent++; 
                            else
                            {
                                countTeammate++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countTeammate)
                    break;
            }
            if (countTeammate == 2)
                return 0;
            mark += markDefend[countOpponent];
            return mark;
        }
        private long markDefend_CrossLeft(int col, int row)
        {
            int t = 0;
            long mark = 0;
            int countTeammate = 0;
            int countOpponent = 0;
            for (int i = 1; i < 6 && row - i >= 0 && col + i < m_BoardSize; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col + i, row - i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col + i && item.Key.Y == row - i)
                        {
                            if (item.Value != m_ToPlay)
                                countOpponent++; 
                            else
                            {
                                countTeammate++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countTeammate)
                {
                    t = countTeammate;
                    break;
                }
            }

            for (int i = 1; i < 6 && row + i < m_BoardSize && col - i >= 0; i++)
            {
                if (m_StoneList.ContainsKey(new GoBoardPoint(col - i, row + i)))
                {
                    foreach (var item in m_StoneList)
                    {
                        if (item.Key.X == col - i && item.Key.Y == row + i)
                        {
                            if (item.Value != m_ToPlay)
                                countOpponent++; 
                            else
                            {
                                countTeammate++;
                                break;
                            }
                            break;
                        }
                    }
                }
                else break;
                if (t != countTeammate)
                    break;
            }
            if (countTeammate == 2)
                return 0;
            mark += markDefend[countOpponent];
            return mark;
        }
        #endregion

        #endregion
        
    }
}