namespace MatteiGrid.Shared.Kernel;

public readonly record struct ClinicId(Guid Value)
{
    public static ClinicId New() => new(Guid.NewGuid());
    public static ClinicId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

public readonly record struct EntityId(Guid Value)
{
    public static EntityId New() => new(Guid.NewGuid());
    public static EntityId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
