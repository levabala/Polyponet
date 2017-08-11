using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Polyponet.Classes
{
    class NetworkVisualizer : FrameworkElement
    {
        public double NODE_RADIUS = 10;
        public double ELASTICITY = 70;
        public double MIN_DISTANCE = 0.3;
        public double MOUSE_MIN_DISTANCE = 1;
        public double VECTOR_SCALE = 2;
        public double ACCELERATION_MAX = 0.2;
        public double INTERACTION_WEIGHT = 2;

        private Point mouseNodePosition = new Point(-10, -10);
        private List<GeometryDrawing> drawings = new List<GeometryDrawing>();
        private Matrix m;
        private DispatcherTimer renderTimer;
        private Network network;
        public Dictionary<byte[], NodeVisual> visualNodes = new Dictionary<byte[], NodeVisual>(new ByteArrayComparer());
        class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] left, byte[] right)
            {
                if (left == null || right == null)
                    return left == right;
                if (left.Length != right.Length)
                    return false;
                for (int i = 0; i < left.Length; i++)
                    if (left[i] != right[i])
                        return false;
                return true;
            }
            public int GetHashCode(byte[] key)
            {
                if (key == null)
                    throw new ArgumentNullException("key");
                int sum = 0;
                foreach (byte cur in key)
                    sum += cur;
                return sum;
            }
        }

        public Network Network
        {
            get
            {
                return network;
            }
            set
            {
                network.OnNodeAdd -= Network_OnNodeAdd;
                changeNetwork(value);
            }
        }

        public NetworkVisualizer(Network network)
        {
            renderTimer = new DispatcherTimer();
            renderTimer.Tick += RenderTimer_Tick;
            renderTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            renderTimer.Start();

            changeNetwork(network);

            m.Translate(2, 2);
            m.Scale(50, 50);            
        }        

        private void changeNetwork(Network network)
        {
            visualNodes = new Dictionary<byte[], NodeVisual>(new ByteArrayComparer());            

            this.network = network;
            registerNodes(network.nodes);            

            network.OnNodeAdd += Network_OnNodeAdd;
        }

        public void registerScaling(UIElement element)
        {
            element.MouseWheel += NetworkVisualizer_MouseWheel;
            element.MouseMove += NetworkVisualizer_MouseMove;
        }

        private void NetworkVisualizer_MouseMove(object sender, MouseEventArgs e)
        {
            mouseNodePosition = transformPoint(e.GetPosition(this));
        }

        private void NetworkVisualizer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point p = e.GetPosition(this);
            if (e.Delta > 0)
                m.ScaleAt(1.1, 1.1, p.X, p.Y);
            else
                m.ScaleAt(1 / 1.1, 1 / 1.1, p.X, p.Y);

            InvalidateVisual();
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        private void Network_OnNodeAdd(object sender, NodeAddEventArgs e)
        {
            registerNode(e.node);
        }

        private void registerNodes(List<Node> nodes)
        {            
            foreach (Node n in nodes)
                registerNode(n);
        }

        double spawnDistance = 3;
        double columnsCount = 4;

        private void registerNode(Node n)
        {
            if (!visualNodes.ContainsKey(n.deviceId))
            {
                int rowNumber = (int)Math.Floor(visualNodes.Values.Count / columnsCount);
                double rowWidth = columnsCount * spawnDistance;
                double y = rowNumber * spawnDistance;
                double x = visualNodes.Values.Count * spawnDistance - rowNumber * rowWidth;
                NodeVisual nv = new NodeVisual(x, y);
                visualNodes.Add(n.deviceId, nv);                
            }
        }

        protected override void OnRender(DrawingContext context)
        {
            resetConnectionsList();

            calcForcePoint(mouseNodePosition);

            foreach (Node n in network.nodes)
            {
                NodeVisual nodeVisual = calcForces(n, network.nodes);
                if (nodeVisual.moveVector.length > ACCELERATION_MAX)
                    nodeVisual.moveVector.length = ACCELERATION_MAX;
                nodeVisual.applyForce();

                if (n.online)
                    nodeVisual.pen.Brush = Brushes.Green;
                else nodeVisual.pen.Brush = Brushes.DarkGray;

                nodeVisual.render(context, m, NODE_RADIUS);                
            }

            foreach (GeometryDrawing drawing in drawings)
                context.DrawDrawing(drawing);

            drawings.Clear();
        }

        Dictionary<byte[], List<byte[]>> connections;
        private NodeVisual calcForces(Node n, List<Node> nodes, double time = 1)
        {            
            NodeVisual targetNV = visualNodes[n.deviceId];
            foreach (Node nn in nodes)
            {
                if (nn.deviceId == n.deviceId) continue;

                NodeVisual nv = visualNodes[nn.deviceId];
                Vector v = new Vector(targetNV.position, nv.position);                

                double distance = v.length;
                double interaction = calcInteraction(n, nn);
                double minDistance = MIN_DISTANCE;

                v.length = getPullRate(distance, interaction, minDistance, ELASTICITY) * time;
                v.endPoint = v.getEndPoint();

                drawVector(v);
                //drawVector(targetNV.moveVector);
                targetNV.addForce(v);                

                if(n.trustedNodes.ContainsKey(nn.deviceId))
                    if (!connections[n.deviceId].Contains(nn.deviceId))
                    {
                        drawConnect(targetNV, nv);
                        connections[n.deviceId].Add(nn.deviceId);
                        connections[nn.deviceId].Add(n.deviceId);
                    }
            }
            return targetNV;
        }

        private void calcForcePoint(Point p, double coeff = 1, double time = 1)
        {
            foreach (NodeVisual nv in visualNodes.Values)
            {                                
                Vector v = new Vector(nv.position, p);

                double distance = v.length;
                double interaction = 0.5;
                double minDistance = MOUSE_MIN_DISTANCE;

                /*if (distance < MIN_DISTANCE / 2)
                    continue;*/

                v.length = getPullRate(distance, interaction, minDistance, ELASTICITY) * time;               
                v.endPoint = v.getEndPoint();

                drawVector(v);                
                nv.addForce(v);                
            }
        }

        public void resetConnectionsList()
        {
            connections = new Dictionary<byte[], List<byte[]>>(new ByteArrayComparer());
            foreach (Node n in network.nodes)
                connections.Add(n.deviceId, new List<byte[]>());
        }

        private double calcInteraction(Node n1, Node n2)
        {
            double interaction = Convert.ToDouble(n1.trustedNodes.ContainsKey(n2.deviceId)) * 0.5;
            return interaction * INTERACTION_WEIGHT;
        }

        private double getPullRate(double distance, double interaction, double minDistance, double elasticity)
        {
            double f1 = -1 / Math.Pow(distance + interaction - minDistance, 1);
            double f2 = 1 / Math.Pow(distance + interaction - minDistance, 3);
            double pullRate = -(1 / elasticity) * (f1 + f2);

            return pullRate;
        }        

        public Point transformPoint(Point p)
        {
            Matrix mm = m;
            mm.Invert();
            return mm.Transform(p);
        }

        public double totalMoving()
        {
            double output = 0;
            foreach (NodeVisual nv in visualNodes.Values)
                output += nv.moveVector.length;
            return output;
        }
        
        public void drawVector(Vector v, DrawingContext context, bool applyScale = true)
        {
            Vector sv = new Vector(v);
            if (applyScale)
                sv.length *= VECTOR_SCALE;
            Point scaledPoint = sv.getEndPoint();            
            context.DrawLine(new Pen(Brushes.DarkGreen, 1), m.Transform(sv.startPoint), m.Transform(scaledPoint));
        }

        public void drawVector(Vector v, bool applyScale = true)
        {
            Vector sv = new Vector(v);
            if (applyScale)
                sv.length *= VECTOR_SCALE;
            Point scaledPoint = sv.getEndPoint();
            LineGeometry lg = new LineGeometry(m.Transform(v.startPoint), m.Transform(scaledPoint));

            drawings.Add(new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Black, 1), lg));
        }

        public void drawConnect(NodeVisual n1, NodeVisual n2)
        {
            LineGeometry lg = new LineGeometry(m.Transform(n1.position), m.Transform(n2.position));
            Pen p = new Pen(Brushes.DarkRed, 1);
            p.DashStyle = DashStyles.Dash;
            drawings.Add(new GeometryDrawing(Brushes.Transparent, p, lg));
        }
    }

    public class NodeVisual
    {
        public Point position;        
        public Pen pen;
        public Vector moveVector;

        public NodeVisual(double x, double y)
        {
            pen = new Pen(Brushes.DarkGray, 3);
            position = new Point(x, y);            
            moveVector = new Vector(position, position);
        }        

        public void addForce(Vector forceV)
        {
            moveVector = Vector.sum(moveVector, forceV);
        }

        public void applyForce()
        {            
            position = moveVector.endPoint = moveVector.getEndPoint();
            moveVector.startPoint = position;
        }

        public void render(DrawingContext context, Matrix m, double radius)
        {
            context.DrawEllipse(Brushes.Transparent, pen, m.Transform(position), radius, radius);
        }
    }
}
