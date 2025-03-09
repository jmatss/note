using Editor.ViewModel;

namespace Note.Tabs
{
    public interface ITabFocus
    {
        FileViewModel? FocusedFile { get; set; }
    }
}
