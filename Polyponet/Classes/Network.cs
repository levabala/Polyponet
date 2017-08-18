using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Polyponet.Classes
{
    public class Network
    {
        public List<Node> nodes = new List<Node>();                
        public DispatcherTimer timerFast;
        public DispatcherTimer timerUsual;
        public DispatcherTimer timerRare;

        public delegate void NodeAddHandler(object sender, NodeAddEventArgs e);
        public event NodeAddHandler OnNodeAdd;

        public Network()
        {            
            timerFast = new DispatcherTimer();
            timerFast.Tick += TimerFast_Tick;
            timerFast.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerFast.Start();

            timerUsual = new DispatcherTimer();
            timerUsual.Tick += TimerUsual_Tick;
            timerUsual.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            timerUsual.Start();

            timerRare = new DispatcherTimer();
            timerRare.Tick += TimerRare_Tick;
            timerRare.Interval = new TimeSpan(0, 0, 0, 0, 5000);
            timerRare.Start();

            OnNodeAdd += (o,e) =>
            {
                //just nothing to init event
            };
        }

        public Network(int nodesCount, double connectChance = 0.5) : this()
        {            
            Random rnd = new Random();
            for (int i = 0; i < nodesCount; i++)
            {
                Node n = new Node();
                addNode(n);
                for (int ii = 0; ii < i; ii++)
                    if (rnd.NextDouble() <= connectChance)
                        n.requestTrust(nodes[ii]);
            }
        }

        private void TimerRare_Tick(object sender, EventArgs e)
        {
            
        }

        private void TimerUsual_Tick(object sender, EventArgs e)
        {
            foreach (Node node in nodes)
                node.updateOnlineStatus();            
        }

        private void TimerFast_Tick(object sender, EventArgs e)
        {
            
        }

        public void addNodes(IEnumerable<Node> nodes)
        {
            foreach (Node n in nodes)
                addNode(n);
        }

        public void addNode(Node n)
        {
            nodes.Add(n);

            NodeAddEventArgs args = new NodeAddEventArgs(n);
            OnNodeAdd(this, args);
        }
    }

    public class NodeAddEventArgs : EventArgs
    {
        public Node node { get; private set; }

        public NodeAddEventArgs(Node node)
        {
            this.node = node;
        }
    }
}
