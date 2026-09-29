namespace Nodify;

internal partial class ConnectionsMultiSelector
{
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        OnSelectedItemsSourceChanged(null!, SelectedItems!);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (SelectedItems is System.Collections.Specialized.INotifyCollectionChanged selected)
            selected.CollectionChanged -= OnSelectedItemsChanged;
        base.OnDetachedFromVisualTree(e);
    }

    static ConnectionsMultiSelector()
    {
        SelectedItemsProperty.Changed.AddClassHandler<ConnectionsMultiSelector>(OnSelectedItemsSourceChanged);
        CanSelectMultipleItemsProperty.Changed.AddClassHandler<ConnectionsMultiSelector>(OnCanSelectMultipleItemsChanged);
    }

    protected override Type StyleKeyOverride => typeof(ItemsControl);
}
