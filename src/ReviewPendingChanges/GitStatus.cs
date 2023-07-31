using System;
using System.Runtime.Serialization;

namespace ReviewPendingChanges;

[Flags]
public enum GitStatus
{
    Undefined = 0,
    [EnumMember(Value = " ")] Unmodified = 1 << 0,
    [EnumMember(Value = "M")] Modified = 1 << 1,
    [EnumMember(Value = "A")] Added = 1 << 2,
    [EnumMember(Value = "R")] Renamed = 1 << 3,
    [EnumMember(Value = "C")] Copied = 1 << 4,
    [EnumMember(Value = "D")] Deleted = 1 << 5,
    [EnumMember(Value = "U")] UpdatedButUnmerged = 1 << 6,
    [EnumMember(Value = "?")] Untracked = 1 << 7,
    [EnumMember(Value = "!")] Ignored = 1 << 8,
}