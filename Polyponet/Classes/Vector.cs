using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using System.Windows;

namespace Polyponet.Classes
{
    public class Vector
    {
        public double length, alpha;
        public Point startPoint, endPoint;

        public Vector()
        {
            length = 0.001;
            alpha = 0;
            startPoint = new Point(0, 0);
            endPoint = new Point(1, 1);
        }

        public Vector(Vector v)
        {
            length = v.length;
            alpha = v.alpha;
            startPoint = v.startPoint;
            endPoint = v.endPoint;
        }

        public Vector(double length, double alpha, Point startPoint)
        {
            this.length = length;
            this.alpha = alpha;
            this.startPoint = startPoint;
            endPoint = getEndPoint();
        }

        public Vector(Point startPoint, Point endPoint)
        {
            this.startPoint = startPoint;
            this.endPoint = endPoint;
            length = getLength();
            alpha = getAlpha();
        }

        public Point getEndPoint()
        {
            Point p = new Point(length * Math.Cos(alpha) + startPoint.X, length * Math.Sin(alpha) + startPoint.Y);
            return p;
        }

        public void changeDirection(double alpha)
        {
            this.alpha = alpha;
            getEndPoint();
        }

        public double getAlpha()
        {
            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;
            if (dx == 0) return 0;
            double angle = Math.Atan(dy / dx);
            if ((dx < 0 && dy < 0) || (dx < 0 && dy >= 0)) angle = angle - Math.PI;            

            return angle;
        }

        public static double getAlpha(Point startPoint, Point endPoint)
        {
            if (startPoint.X == endPoint.X && startPoint.Y == endPoint.Y)
                return 0;
            double dx = Math.Abs(endPoint.X - startPoint.X);
            double dy = Math.Abs(endPoint.Y - startPoint.Y);
            double angle = Math.Atan(dx / dy);
            if (dx < 0) angle = angle - 3 * Math.PI;            
            return angle;
        }

        public double getLength()
        {
            double lengthX = Math.Abs(endPoint.X - startPoint.X);
            double lengthY = Math.Abs(endPoint.Y - startPoint.Y);
            return (double)Math.Sqrt(lengthX * lengthX + lengthY * lengthY);
        }

        public static double getLength(Point endPoint, Point startPoint)
        {
            double lengthX = Math.Abs(endPoint.X - startPoint.X);
            double lengthY = Math.Abs(endPoint.Y - startPoint.Y);
            return (double)Math.Sqrt(lengthX * lengthX + lengthY * lengthY);
        }

        public Vector clone()
        {
            return new Vector(this);
        }

        public static Vector sum(Vector a, Vector b)
        {
            Vector sumV = a.clone();
            Vector tv1 = b.clone();
            b.startPoint = sumV.endPoint;
            b.endPoint = b.getEndPoint();
            sumV = new Vector(sumV.startPoint, b.endPoint);            
            return sumV;
        }
    }
}