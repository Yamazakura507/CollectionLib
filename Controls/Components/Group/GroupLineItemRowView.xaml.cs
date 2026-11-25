namespace CollectionLib.Controls.Components.Group;

public partial class GroupLineItemRowView : ContentView
{
    public GroupLineItemRowView() => InitializeComponent();

    public GraphicsView TreeConnector => Connector;
    public ContentView ItemContent => ItemHost;
}