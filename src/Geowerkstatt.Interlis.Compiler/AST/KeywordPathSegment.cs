using System.Diagnostics.CodeAnalysis;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// A keyword step of an object path (<c>THIS</c>, <c>THISAREA</c>, <c>THATAREA</c>, <c>PARENT</c>,
/// <c>AGGREGATES</c>; RefHB 3.13-33ff). It denotes the context object itself, or one the context implies, rather
/// than a name declared elsewhere: it never gets a <see cref="PathSegment.Target"/>, and there is nothing to
/// navigate to or rename. Its <see cref="PathSegment.Name"/> is the keyword, which is why it is constructed from
/// the keyword rather than initialized member by member.
/// </summary>
public sealed class KeywordPathSegment : PathSegment
{
    [SetsRequiredMembers]
    public KeywordPathSegment(PathKeyword keyword)
    {
        Keyword = keyword;
        Name = keyword.ToString().ToUpperInvariant();
    }

    public PathKeyword Keyword { get; }
}

/// <summary>The keywords a step of an object path can be (RefHB 3.13).</summary>
public enum PathKeyword
{
    This = Interlis24Parser.THIS,
    ThisArea = Interlis24Parser.THISAREA,
    ThatArea = Interlis24Parser.THATAREA,
    Parent = Interlis24Parser.PARENT,
    Aggregates = Interlis24Parser.AGGREGATES,
}
