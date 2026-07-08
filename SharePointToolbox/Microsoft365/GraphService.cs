using Microsoft.Graph;

namespace SharePointToolbox.Graph;

/// <summary>
/// Gestisce tutte le operazioni verso Microsoft Graph.
/// </summary>
public class GraphService
{
    // ===========================
    // Private fields
    // ===========================

    private readonly GraphServiceClient _graphClient;

    // ===========================
    // Constructor
    // ===========================

    public GraphService(GraphServiceClient graphClient)
    {
        _graphClient = graphClient;
    }
}