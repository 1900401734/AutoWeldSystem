
namespace AutoWeldSystem.UI.ViewModels;

public sealed class ProductHistoryRawColumnCandidate
{
    private const string PreviewTouchNoColumn = "TouchNo";
    private const string PreviewTouchResultColumn = "TouchResult";
    private const string PreviewMessageColumn = "Message";
    private const string PreviewActualRole = "Actual";
    private const string PreviewResultRole = "Result";
    private const string PreviewUpperRole = "Upper";
    private const string PreviewLowerRole = "Lower";

    public ProductHistoryRawColumnCandidate(string itemKey, string itemName, int sort)
    {
        ItemKey = itemKey;
        ItemName = itemName;
        Sort = sort;
    }

    public string ItemKey { get; }

    public string ItemName { get; }

    public int Sort { get; }

    public bool EnableActual { get; private set; }

    public bool EnableUpper { get; private set; }

    public bool EnableLower { get; private set; }

    public bool EnableResult { get; private set; }

    public void EnableRole(string role)
    {
        switch (role)
        {
            case PreviewUpperRole:
                EnableUpper = true;
                break;
            case PreviewLowerRole:
                EnableLower = true;
                break;
            case PreviewResultRole:
                EnableResult = true;
                break;
            default:
                EnableActual = true;
                break;
        }
    }
}