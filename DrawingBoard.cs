using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace DigitRecognition
{

    public delegate void NewNumberEvent(object sender, System.Collections.ArrayList lst);
    public partial class DrawingBoard : UserControl
    {
         public event NewNumberEvent NewNumber;

        public enum DrawingStatus
        {
            drNone,
            drDrawing,
            drWaiting,
            drFinished,
        }

        public DrawingStatus fDrawingStatus = DrawingStatus.drNone;
        List<int[]> lst = new List<int[]>();
        List<Point> Dots = new List<Point>();
        List<Point> FoundNumber = new List<Point>();


        public DrawingBoard()
        {
            InitializeComponent();

            pictureBox1.Image = new Bitmap(480, 640);

            using (Graphics graphics = Graphics.FromImage(pictureBox1.Image))
            using (System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                graphics.FillRectangle(myBrush, new Rectangle(0, 0, 480, 640));

            clear();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (fDrawingStatus == DrawingStatus.drFinished)
                clear();

            fDrawingStatus = DrawingStatus.drDrawing;
            timer1.Enabled = false;
        }

        public void ClearDots()
        {
            Bitmap bmp = (Bitmap)pictureBox1.Image;
            foreach (Point aDot in Dots)
            {
                DrawDot(bmp, aDot, Color.White);            
            }

            foreach (Point aDot in FoundNumber)
            {
                DrawDot(bmp, aDot, Color.White);
            }
        }

        private void clear()
        {
            Bitmap bmp = (Bitmap)pictureBox1.Image;
            ClearDots();

            Dots.Clear();
            pictureBox1.Image = bmp;
            pictureBox1.Refresh();
        }

        public void DrawDot(Bitmap bmp, Point pnt,Color cl)
        {
            for (int x = -10; x < 10; x++)
                for (int y = -10; y < 10; y++)
                {
                    if (Math.Sqrt(x * x + y * y) < 10)
                        if (x + pnt.X >= 0 && x + pnt.X < bmp.Width &&
                         y + pnt.Y >= 0 && y + pnt.Y < bmp.Height)
                            bmp.SetPixel(x + pnt.X, y + pnt.Y, cl);
                }
        }



        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Bitmap bmp = (Bitmap)pictureBox1.Image;

            if (fDrawingStatus == DrawingStatus.drDrawing)
                if (bmp != null)
                {
                    timer1.Enabled = false;
                    Dots.Add(new Point(e.X, e.Y));
                    DrawDot(bmp, new Point(e.X,e.Y), Color.FromArgb(255, 0, 0, 0));                 
                }

            pictureBox1.Refresh();
        }


        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            fDrawingStatus = DrawingStatus.drWaiting;
            timer1.Interval = 1000;
            timer1.Enabled = true;
            timer1.Start();
        }

        public void DrawFoundNumber(List<Point> aFoundNumber)
        {
            if (aFoundNumber == null)
                return;

            FoundNumber = aFoundNumber;
            Bitmap bmp = (Bitmap)pictureBox1.Image;
            for (int i = 0; i < aFoundNumber.Count; i++)
            {
                Point pnt = aFoundNumber[i];
                DrawDot(bmp, pnt, Color.FromArgb(255, 0, 255, 0));
            }
            pictureBox1.Refresh();
        }


        public void GetSpecsDots(List<Point> Dots, ref double scale, ref Point center)
        {
            Bitmap bmp = (Bitmap)pictureBox1.Image;
            double left = 9999;
            double right = 0;
            double top = 9999;
            double bottom = 0;
            double xtot = 0;
            double ytot = 0;

            if (Dots.Count == 0)
                return;
            foreach (Point aDot in Dots)
            {
                if (aDot.X > right)
                    right = aDot.X;
                if (aDot.X < left)
                    left = aDot.X;
                if (aDot.Y > bottom)
                    bottom = aDot.Y;
                if (aDot.Y < top)
                    top = aDot.Y;

                xtot = xtot + aDot.X;
                ytot = ytot + aDot.Y;
            }

            xtot = xtot / Dots.Count;
            ytot = ytot / Dots.Count;

            int W = System.Convert.ToInt16(right - left);
            int H = System.Convert.ToInt16(bottom - top);

            int pick = 0;
            for (int i = 0; i < 1000; i++)
            {
                double nW = W * (0.5 + i / 100.0);
                double nH = H * (0.5 + i / 100.0);
                if ((nW > 300) || (nH > 400))
                {
                    pick = i;
                    break;
                }
            }

            scale = (0.5 + pick / 100.0);
            center = new Point(System.Convert.ToInt16(xtot), System.Convert.ToInt16(ytot));

            ClearDots();

            for (int i = 0; i < Dots.Count; i++)
            {
                int x =System.Convert.ToInt32( (Dots[i].X - xtot) * scale);
                int y = System.Convert.ToInt32((Dots[i].Y - ytot) * scale);
                Dots[i] = new Point(x + bmp.Width / 2, y + bmp.Height / 2);
            }            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            fDrawingStatus = DrawingStatus.drFinished;

            if (Dots.Count == 0)
                return;

            double Scale = 1.0;
            Point Center = new Point(0, 0);
            GetSpecsDots(Dots, ref Scale, ref Center);

            Bitmap bmp = (Bitmap)pictureBox1.Image;
            double pick = 0;
            Point prev = new Point(0, 0);

            System.Collections.ArrayList Dots50 = new System.Collections.ArrayList();
            Point pnt = new Point(0, 0);

            for (int p = 0; p < 400; p++)
            {
                int tot = 0;
                prev = new Point(0, 0);
                for (int i = 0; i < Dots.Count; i++)
                {
                    pnt = Dots[i];
                    double d = Math.Sqrt((pnt.X - prev.X) * (pnt.X - prev.X) +
                        (pnt.Y - prev.Y) * (pnt.Y - prev.Y));
                    if (d > p / 4)
                    {
                        tot++;
                        prev = pnt;
                    }
                }
                if (tot < 51)
                {
                    pick = p;
                    break;
                }
            }

            prev = new Point(0, 0);
            for (int i = 0; i < Dots.Count; i++)
            {
                pnt = Dots[i];
                double d = Math.Sqrt((pnt.X - prev.X) * (pnt.X - prev.X) +
                    (pnt.Y - prev.Y) * (pnt.Y - prev.Y));

                if (d > pick / 4)
                {
                    Dots50.Add(pnt);                  
                    if (bmp != null)
                        DrawDot(bmp, pnt, Color.FromArgb(255, 255, 0, 0));
                    prev = pnt;
                }
            }

            Dots50.Add(Dots[Dots.Count - 1]);
            pnt = Dots[Dots.Count - 1];
            if (bmp != null)
                DrawDot(bmp, pnt, Color.FromArgb(255, 255, 0, 0));

            pictureBox1.Refresh();
            if (this.NewNumber != null)
                NewNumber(this, Dots50);
            //  Bitmap bmp = (Bitmap)pictureBox1.Image;
        }
    }
    
}
