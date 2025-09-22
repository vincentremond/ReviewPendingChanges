namespace ReviewPendingChangesLegacy.Records;

public enum DecisionType
{
    ReviewChanges,
    Undefined,
    None,
    ReviewNewFile,
}

public enum DecisionTypeGroup
{
    Error,
    Ignore,
    Operate,
}