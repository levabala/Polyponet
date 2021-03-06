﻿using Polyponet.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Polyponet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Network network;
        NetworkVisualizer netVisualizer;

        string Position = "";
        string TotalMoving = "";

        public MainWindow()
        {
            InitializeComponent();
            MyWindow.Loaded += MyWindow_Loaded;
            MyWindow.KeyDown += MyWindow_KeyDown;
            MyWindow.MouseMove += MyWindow_MouseMove;
        }

        private void MyWindow_MouseMove(object sender, MouseEventArgs e)
        {            
            Point p = netVisualizer.transformPoint(e.GetPosition(netVisualizer));
            Position = String.Format("X: {0}  Y: {1}", Math.Round(p.X, 1), Math.Round(p.Y, 1));
            updateTitle();
        }

        private void MyWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Application.Current.Shutdown();
                    break;
                case Key.Space:
                    initScene();
                    break;
            }
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            network = new Network();
            netVisualizer = new NetworkVisualizer(network);
            netVisualizer.registerScaling(MyWindow);

            SliderNodeRadius.Value = netVisualizer.NODE_RADIUS;
            SliderElasticity.Value = netVisualizer.ELASTICITY;
            SliderMinDistance.Value = netVisualizer.MIN_DISTANCE;
            SliderVectorScale.Value = netVisualizer.VECTOR_SCALE;
            SliderInteractionWeight.Value = netVisualizer.INTERACTION_WEIGHT;
            SliderMouseMinDistance.Value = netVisualizer.MOUSE_MIN_DISTANCE;

            SliderNodeRadius.ValueChanged += (o, ev) =>
            {
                netVisualizer.NODE_RADIUS = ev.NewValue;
            };
            SliderElasticity.ValueChanged += (o, ev) =>
            {
                netVisualizer.ELASTICITY = ev.NewValue;
            };
            SliderMinDistance.ValueChanged += (o, ev) =>
            {
                netVisualizer.MIN_DISTANCE= ev.NewValue;
            };
            SliderVectorScale.ValueChanged += (o, ev) =>
            {
                netVisualizer.VECTOR_SCALE = ev.NewValue;
            };
            SliderInteractionWeight.ValueChanged += (o, ev) =>
            {
                netVisualizer.INTERACTION_WEIGHT= ev.NewValue;
            };
            SliderMouseMinDistance.ValueChanged += (o, ev) =>
            {
                netVisualizer.MOUSE_MIN_DISTANCE = ev.NewValue;
            };
            SliderMouseForce.ValueChanged += (o, ev) =>
            {
                netVisualizer.MOUSE_FORCE_COEFFICIENT = ev.NewValue;
            };

            MainGrid.Children.Add(netVisualizer);

            initScene();
        }

        private void initScene()
        {            
            network = new Network(3, 1);
            network.timerFast.Tick += DispatcherTimer_Tick;

            netVisualizer.Network = network;
            netVisualizer.resetConnectionsList();            
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            TotalMoving = Math.Round(netVisualizer.totalMoving(), 4).ToString();
            updateTitle();
        }

        private void updateTitle()
        {
            Title = Position + " " + TotalMoving;
        }
    }
}
