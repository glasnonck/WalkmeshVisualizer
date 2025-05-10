using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using WalkmeshVisualizerWpf.Models;

namespace WalkmeshVisualizerWpf.UserControls
{
    /// <summary>
    /// Interaction logic for MouseHoverDisplayControl.xaml
    /// </summary>
    public partial class MouseHoverDisplayControl : UserControl
    {
        public static readonly DependencyProperty RimDataUnderMouseProperty =
            DependencyProperty.Register(nameof(RimDataUnderMouse), typeof(List<RimDataInfo>), typeof(MouseHoverDisplayControl),
                new PropertyMetadata(new List<RimDataInfo>()
                {
                    new RimDataInfo()
                    {
                        RimDataType = RimDataType.Door,
                        MeshColor = Brushes.Blue,
                        ResRef = "TestDoor",
                        Tag = "DoorTag",
                        ScriptOnEnter = "DoorScript",
                        LocalizedName = "Door Name",
                    },
                    new RimDataInfo()
                    {
                        RimDataType = RimDataType.Trigger,
                        MeshColor = Brushes.Green,
                        ResRef = "TestTrigger",
                        Tag = "TriggerTag",
                        ScriptOnEnter = "TriggerScript",
                        LocalizedName = "Trigger Name",
                    },
                    new RimDataInfo()
                    {
                        RimDataType = RimDataType.Encounter,
                        MeshColor = Brushes.Red,
                        ResRef = "TestEncounter",
                        Tag = "EncounterTag",
                        ScriptOnEnter = "EncounterScript",
                        LocalizedName = "Encounter Name",
                    },
                }));

        public static readonly DependencyProperty ShowRimDataUnderMouseProperty =
            DependencyProperty.Register(nameof(ShowRimDataUnderMouse), typeof(bool), typeof(MouseHoverDisplayControl), new PropertyMetadata(true));

        public List<RimDataInfo> RimDataUnderMouse
        {
            get => (List<RimDataInfo>)GetValue(RimDataUnderMouseProperty);
            set => SetValue(RimDataUnderMouseProperty, value);
        }

        public bool ShowRimDataUnderMouse
        {
            get => (bool)GetValue(ShowRimDataUnderMouseProperty);
            set => SetValue(ShowRimDataUnderMouseProperty, value);
        }

        public MouseHoverDisplayControl()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
