using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                        OnEnter = "DoorScript",
                        LocalizedName = "Door Name",
                    },
                    new RimDataInfo()
                    {
                        RimDataType = RimDataType.Trigger,
                        MeshColor = Brushes.Green,
                        ResRef = "TestTrigger",
                        Tag = "TriggerTag",
                        OnEnter = "TriggerScript",
                        LocalizedName = "Trigger Name",
                    },
                    new RimDataInfo()
                    {
                        RimDataType = RimDataType.Encounter,
                        MeshColor = Brushes.Red,
                        ResRef = "TestEncounter",
                        Tag = "EncounterTag",
                        OnEnter = "EncounterScript",
                        LocalizedName = "Encounter Name",
                    },
                }));

        public static readonly DependencyProperty ShowRimDataUnderMouseProperty =
            DependencyProperty.Register(nameof(ShowRimDataUnderMouse), typeof(bool), typeof(MouseHoverDisplayControl), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowMeshColorProperty =
            DependencyProperty.Register(nameof(ShowMeshColor), typeof(bool), typeof(MouseHoverDisplayControl), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowTypeProperty =
            DependencyProperty.Register(nameof(ShowType), typeof(bool), typeof(MouseHoverDisplayControl), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowResRefProperty =
            DependencyProperty.Register(nameof(ShowResRef), typeof(bool), typeof(MouseHoverDisplayControl), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowTagProperty =
            DependencyProperty.Register(nameof(ShowTag), typeof(bool), typeof(MouseHoverDisplayControl), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowLocalizedNameProperty =
            DependencyProperty.Register(nameof(ShowLocalizedName), typeof(bool), typeof(MouseHoverDisplayControl), new PropertyMetadata(true));

        public static readonly DependencyProperty ShowOnEnterProperty =
            DependencyProperty.Register(nameof(ShowOnEnter), typeof(bool), typeof(MouseHoverDisplayControl), new PropertyMetadata(true));

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

        public bool ShowMeshColor
        {
            get { return (bool)GetValue(ShowMeshColorProperty); }
            set { SetValue(ShowMeshColorProperty, value); }
        }

        public bool ShowType
        {
            get { return (bool)GetValue(ShowTypeProperty); }
            set { SetValue(ShowTypeProperty, value); }
        }

        public bool ShowResRef
        {
            get { return (bool)GetValue(ShowResRefProperty); }
            set { SetValue(ShowResRefProperty, value); }
        }

        public bool ShowTag
        {
            get { return (bool)GetValue(ShowTagProperty); }
            set { SetValue(ShowTagProperty, value); }
        }

        public bool ShowLocalizedName
        {
            get { return (bool)GetValue(ShowLocalizedNameProperty); }
            set { SetValue(ShowLocalizedNameProperty, value); }
        }

        public bool ShowOnEnter
        {
            get { return (bool)GetValue(ShowOnEnterProperty); }
            set { SetValue(ShowOnEnterProperty, value); }
        }

        public MouseHoverDisplayControl()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
