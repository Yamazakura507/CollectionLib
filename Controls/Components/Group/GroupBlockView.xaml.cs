namespace CollectionLib.Controls.Components.Group;

public partial class GroupBlockView : ContentView
{
    public GroupBlockView() => InitializeComponent();

    public Border HeaderView => Header;
    public VerticalStackLayout ContentHost => ChildrenHost;
    public Image ChevronImage => Chevron;
    public Label GroupTitle => TitleLabel;
}