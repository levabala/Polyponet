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
        public DispatcherTimer dispatcherTimer;

        public delegate void NodeAddHandler(object sender, NodeAddEventArgs e);
        public event NodeAddHandler OnNodeAdd;

        public Network()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();

            OnNodeAdd += (o,e) =>
            {

            };
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
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
