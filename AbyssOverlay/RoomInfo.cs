using System.Collections.Generic;

namespace AbyssOverlay;

public sealed class RoomInfo
{
    public string Name { get; set; } = string.Empty;
    public List<string> Targets { get; } = new();
    public List<PriorityItem> Priority { get; } = new();
    public List<string> Notes { get; } = new();
}

public sealed class PriorityItem
{
    public string Num { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
