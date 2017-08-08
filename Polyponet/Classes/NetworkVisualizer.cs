using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Polyponet.Classes
{
    class NetworkVisualizer : FrameworkElement
    {
        public double NODE_RADIUS, ELASTICITY;
        public int RADIUSES_BETWEEN;

        private Network network;         
        private Dictionary<byte[], Node> visual

        public Network Network
        {
            get
            {
                return network;
            }
            set
            {
                network = value;

            }
        }

        public NetworkVisualizer()
        {

        }

        protected override void OnRender(DrawingContext context)
        {
            foreach (Node n in network.nodes)
        }        

        private double getPullRate(double distance, double interaction, double radius, double elasticity)
        {
            double f1 = -1 / Math.Pow(distance + interaction - radius, 7);
            double f2 = 1 / Math.Pow(distance + interaction - radius, 13);
            double pullRate = (1 / elasticity) * (f1 + f2);

            return pullRate;
        }

        private class NodeVisual
        {
            public double x, y, radius;
            public Pen pen;

            public NodeVisual(double x, double y, double radius)
            {
                this.x = x;
                this.y = y;
                this.radius = radius;
            }

            public void render(DrawingContext context)
            {
                context.DrawEllipse(Brushes.Transparent, pen, new Point(x, y), 5, 5);
            }
        }
    }
}
