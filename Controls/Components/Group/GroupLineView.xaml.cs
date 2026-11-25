namespace CollectionLib.Controls.Components.Group;

public partial class GroupLineView : ContentView
{
    public GroupLineView() => InitializeComponent();

    public Grid RootGrid => LayoutRoot;
    public GraphicsView TreeConnector => Connector;
    public Border HeaderView => Header;
    public Image ChevronImage => Chevron;
    public Label GroupTitle => TitleLabel;
    public Label ItemCount => CountLabel;
    public VerticalStackLayout ContentHost => ChildrenHost;
}