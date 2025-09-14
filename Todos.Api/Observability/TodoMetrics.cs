using System.Diagnostics.Metrics;

namespace Todos.Api.Observability;

public static class TodoMetrics
{
    public const string MeterName = "Todos.Api";
    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> TodosCreated = Meter.CreateCounter<long>("todos_created_total", description: "Number of todos created");
    public static readonly Counter<long> TodosUpdated = Meter.CreateCounter<long>("todos_updated_total", description: "Number of todos updated");
    public static readonly Counter<long> TodosDeleted = Meter.CreateCounter<long>("todos_deleted_total", description: "Number of todos deleted");
}
