using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class DomainTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Text domain",
            """
            text = TEXT * 12;
            """,
            RefHB: "3.8-1",
            Expected: new DomainDef
            {
                Name = "text",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound }, SourceRange = new RangePosition(0, 7, 0, 16), }
            }));

        yield return Rule(new(
            "Domain with constraint",
            """
            text = TEXT * 12
                CONSTRAINTS
                    someMath: (1 + 1) == 2; /* (==) has higher precedence than (+) ¯\_(ツ)_/¯ */
                    !!noWhitespaceAtStartAndEnd: INTERLIS.len(INTERLIS.trim(THIS)) == INTERLIS.len(THIS),
                    !!minLength: INTERLIS.len(THIS) > 6;
            """,
            RefHB: "3.8-1",
            Expected: new DomainDef
            {
                Name = "text",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TextType
                {
                    Length = 12,
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 7, 0, 16),
                    Constraints =
                    {
                        {
                            "someMath",
                            new DomainConstraint
                            {
                                Name = "someMath",
                                SourceRange = new RangePosition(2, 8, 2, 30),
                                Condition = new ComparisonExpression
                                {
                                    Operator = ComparisonExpression.ComparisonOperator.Equal,
                                    FirstOperand = new ArithmeticExpression
                                    {
                                        Operator = ArithmeticExpression.ArithmeticOperator.Addition,
                                        FirstOperand = new NumericConstant { Value = 1 },
                                        SecondOperand = new NumericConstant { Value = 1 }
                                    },
                                    SecondOperand = new NumericConstant { Value = 2 },
                                }
                            }
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Generic coordinate domain",
            "Koord (GENERIC) = COORD NUMERIC;",
            RefHB: "3.8-3",
            Expected: new DomainDef
            {
                Name = "Koord",
                NameLocations = { new RangePosition(0, 0, 0, 5) },
                TypeDef = new CoordType
                {
                    Axis =
                    {
                        new NumericType
                        {
                            SourceRange = new RangePosition(0, 24, 0, 31),
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 18, 0, 31),
                },
                Properties = { Property.Generic },
            }));

        yield return Rule(new(
            "Final numeric domain",
            "Wert (FINAL) = 0 .. 100;",
            RefHB: "3.8-4",
            Expected: new DomainDef
            {
                Name = "Wert",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new DecimalType
                {
                    Min = 0,
                    Max = 100,
                    Precision = 0,
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 15, 0, 23),
                },
                Properties = { Property.Final },
            }));

        yield return Rule(new(
            "Abstract numeric domain",
            "Wert (ABSTRACT) = NUMERIC;",
            RefHB: "3.8-5",
            Expected: new DomainDef
            {
                Name = "Wert",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 18, 0, 25),
                },
                Properties = { Property.Abstract },
            }));

        yield return Rule(new(
            "Domain extends base domain",
            "GenWert EXTENDS Wert = 10.0 .. 100.0;",
            RefHB: "3.8-5",
            Expected: new DomainDef
            {
                Name = "GenWert",
                NameLocations = { new RangePosition(0, 0, 0, 7) },
                TypeDef = new DecimalType
                {
                    Min = 10,
                    Max = 100,
                    Precision = -1,
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    Extends = new Reference<DomainDef> { Path = { new("Wert") } },
                    SourceRange = new RangePosition(0, 23, 0, 36),
                },
            }));

        yield return Rule(new(
            "Mandatory domain type",
            "X = MANDATORY 0 .. 100;",
            RefHB: "3.8-10",
            Expected: new DomainDef
            {
                Name = "X",
                NameLocations = { new RangePosition(0, 0, 0, 1) },
                TypeDef = new DecimalType
                {
                    Min = 0,
                    Max = 100,
                    Precision = 0,
                    Cardinality = new Cardinality { Min = 1, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 14, 0, 22),
                },
            }));

        yield return Rule(new(
            "Domain ref qualified extends",
            "X EXTENDS OtherModel.OtherDomain = 0 .. 100;",
            RefHB: "3.8-12",
            Expected: new DomainDef
            {
                Name = "X",
                NameLocations = { new RangePosition(0, 0, 0, 1) },
                TypeDef = new DecimalType
                {
                    Min = 0,
                    Max = 100,
                    Precision = 0,
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    Extends = new Reference<DomainDef> { Path = { new("OtherModel"), new("OtherDomain") } },
                    SourceRange = new RangePosition(0, 35, 0, 43),
                },
            }));

        yield return Rule(new(
            "OID any domain",
            "NOOID = OID ANY;",
            RefHB: "3.8.9-6",
            Expected: new DomainDef
            {
                Name = "NOOID",
                NameLocations = { new RangePosition(0, 0, 0, 5) },
                TypeDef = new OidType
                {
                    Value = new OidType.AnyOid(),
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    SourceRange = new RangePosition(0, 8, 0, 15),
                },
            }));

        yield return Rule(new(
            "Abstract OID any extends",
            "ANYOID (ABSTRACT) EXTENDS NOOID = OID ANY;",
            RefHB: "3.8.9-8",
            Expected: new DomainDef
            {
                Name = "ANYOID",
                NameLocations = { new RangePosition(0, 0, 0, 6) },
                TypeDef = new OidType
                {
                    Value = new OidType.AnyOid(),
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    Extends = new Reference<DomainDef> { Path = { new("NOOID") } },
                    SourceRange = new RangePosition(0, 34, 0, 41),
                },
                Properties = { Property.Abstract },
            }));

        yield return Rule(new(
            "OID numeric range domain",
            "I32OID EXTENDS ANYOID = OID 0 .. 2147483647;",
            RefHB: "3.8.9-9",
            Expected: new DomainDef
            {
                Name = "I32OID",
                NameLocations = { new RangePosition(0, 0, 0, 6) },
                TypeDef = new OidType
                {
                    Value = new OidType.ValueRange
                    {
                        Type = new DecimalType
                        {
                            Min = 0,
                            Max = 2147483647,
                            Precision = 0,
                            SourceRange = new RangePosition(0, 28, 0, 43),
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    Extends = new Reference<DomainDef> { Path = { new("ANYOID") } },
                    SourceRange = new RangePosition(0, 24, 0, 43),
                },
            }));

        yield return Rule(new(
            "OID text domain",
            "STANDARDOID EXTENDS ANYOID = OID TEXT*16;",
            RefHB: "3.8.9-10",
            Expected: new DomainDef
            {
                Name = "STANDARDOID",
                NameLocations = { new RangePosition(0, 0, 0, 11) },
                TypeDef = new OidType
                {
                    Value = new OidType.ValueRange
                    {
                        Type = new TextType
                        {
                            Length = 16,
                            SourceRange = new RangePosition(0, 33, 0, 40),
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    Extends = new Reference<DomainDef> { Path = { new("ANYOID") } },
                    SourceRange = new RangePosition(0, 29, 0, 40),
                },
            }));

        yield return Rule(new(
            "OID UUID text domain",
            "UUIDOID EXTENDS ANYOID = OID TEXT*36;",
            RefHB: "3.8.9-12",
            Expected: new DomainDef
            {
                Name = "UUIDOID",
                NameLocations = { new RangePosition(0, 0, 0, 7) },
                TypeDef = new OidType
                {
                    Value = new OidType.ValueRange
                    {
                        Type = new TextType
                        {
                            Length = 36,
                            SourceRange = new RangePosition(0, 29, 0, 36),
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                    Extends = new Reference<DomainDef> { Path = { new("ANYOID") } },
                    SourceRange = new RangePosition(0, 25, 0, 36),
                },
            }));

        yield return FullFile(new(
            "Domain extension of a different kind is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    num = 0 .. 10;
                    txt EXTENDS num = TEXT*5;
            END Model.
            """,
            Description: "Domain extension legality: an extension must restrict its base.",
            ExpectedLog: ["Type check error in 'Model.txt' at 5:8-5:33: the domain must be of the same kind as its base 'num'."],
            RefHB: "3.8.1",
            AssertOutput: false));

        yield return FullFile(new(
            "Domain extension widening the numeric range is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    narrow = 0 .. 10;
                    wide EXTENDS narrow = 0 .. 20;
            END Model.
            """,
            Description: "Domain extension legality (RefHB 3.8.1): an extension must restrict its base.",
            ExpectedLog: ["Type check error in 'Model.wide' at 5:8-5:38: the value range must not be wider than the inherited range."],
            RefHB: "3.8.5",
            AssertOutput: false));

        yield return FullFile(new(
            "Domain extension reducing precision is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    precise = 0.000 .. 1000000.000;
                    normal EXTENDS precise = 0 .. 1000000;
                    lenient EXTENDS normal = 0.0000E7 .. 0.1000E7;
            END Model.
            """,
            Description: "Domain extension legality (RefHB 3.8.1): an extension must restrict its base.",
            ExpectedLog:
            [
                "Type check error in 'Model.normal' at 5:8-5:46: the precision must match the inherited precision.",
                "Type check error in 'Model.lenient' at 6:8-6:54: the domain must be of the same kind as its base 'normal'.",
            ],
            RefHB: "3.8.5",
            Ili2cDivergenceReason: "ili2c does not enforce the precision rule and accepts the coarser precision (and the notation change to mantissa form); RefHB 3.8.5-4 states the precision (Stellenzahl) may not be changed in an extension, so we reject.",
            AssertOutput: false));

        yield return FullFile(new(
            "Domain extension widening the text is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    short = TEXT*5;
                    long EXTENDS short = TEXT*10;
                    multi EXTENDS short = MTEXT*3;
            END Model.
            """,
            Description: "Domain extension legality (RefHB 3.8.1): an extension must restrict its base.",
            ExpectedLog:
            [
                "Type check error in 'Model.long' at 5:8-5:37: the text length must not exceed the inherited length.",
                "Type check error in 'Model.multi' at 6:8-6:38: an MTEXT can not extend a TEXT.",
            ],
            RefHB: "3.8.2",
            AssertOutput: false));

        yield return FullFile(new(
            "Incomplete domain without ABSTRACT declaration is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    size = NUMERIC;
                    length (ABSTRACT) = NUMERIC;
                    width EXTENDS length = MANDATORY;
            END Model.
            """,
            Description: """
                An incomplete value-range definition must be declared ABSTRACT — a bound-less NUMERIC counts as abstract
                (RefHB 3.8.5-1), and a pure alias of an abstract domain concretizes nothing. Only coordinate ranges have
                the GENERIC alternative (see the accepted "Generic coordinate domain" case).
                """,
            ExpectedLog:
            [
                "Type check error in 'Model.size' at 4:8-4:23: must be declared ABSTRACT because its type is not fully defined.",
                "Type check error in 'Model.width' at 6:8-6:41: must be declared ABSTRACT because its type is not fully defined.",
            ],
            RefHB: "3.8-3",
            AssertOutput: false));

        yield return FullFile(new(
            "Open numeric domain concretized in either notation is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    size (ABSTRACT) = NUMERIC;
                    decimalSize EXTENDS size = 1.0 .. 99.9;
                    floatSize EXTENDS size = 0.10E3 .. 0.99E5;
            END Model.
            """,
            Description: "A bound-less NUMERIC declares neither bounds nor notation, so it may be concretized in either notation.",
            RefHB: "3.8.5-6",
            AssertOutput: false));

        yield return FullFile(new(
            "Bound-less extension of a concrete numeric domain is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    floatSize = 0.10E3 .. 0.99E5;
                    openFloat (ABSTRACT) EXTENDS floatSize = NUMERIC;
            END Model.
            """,
            Description: """
                A NUMERIC without bounds counts as abstract; over a base with a concrete range it abstracts the
                definition again instead of restricting it (RefHB 3.8-4). In the RefHB 3.8.5-13 example the only
                bound-less domain extending a concrete range is the line marked invalid; ili2c rejects the form outright
                ("Abstract numeric types can not extend concrete numeric types").
                """,
            ExpectedLog: ["Type check error in 'Model.openFloat' at 5:8-5:57: an abstract NUMERIC can not extend a concrete numeric range."],
            RefHB: "3.8.5-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Domain extension changing the notation is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    fixedRange = 1.0 .. 9.9;
                    floatRange EXTENDS fixedRange = 0.10E1 .. 0.99E1;
            END Model.
            """,
            Description: """
                The notation is part of the type: a mantissa (float) range has no uniform grid, so extending a decimal
                range in float notation replaces the base's grid even when the digit count and the bounds are identical.
                """,
            ExpectedLog: ["Type check error in 'Model.floatRange' at 5:8-5:57: the domain must be of the same kind as its base 'fixedRange'."],
            RefHB: "3.8.5-3",
            Ili2cDivergenceReason: "ili2c compares only the bounds of a numeric extension and accepts the notation change; the notation is part of the type for us (a mantissa range has no uniform grid, RefHB 3.8.5-3), so we reject.",
            AssertOutput: false));

        yield return FullFile(new(
            "Domain extending itself is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    a EXTENDS a = MANDATORY;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.a' at 4:8-4:32: the domain transitively EXTENDS itself."],
            RefHB: "3.8.1",
            AssertOutput: false));

        yield return FullFile(new(
            "Legal domain extensions are accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    flag = BOOLEAN;
                    strictFlag EXTENDS flag = MANDATORY;
                    color = (red, green, blue);
                    warm EXTENDS color = (red (dark, light));
                    anyOid = OID ANY;
                    textOid EXTENDS anyOid = OID TEXT*6;
                    narrow = 0 .. 10;
                    narrower EXTENDS narrow = 2 .. 8;
                    short = TEXT*5;
                    shorter EXTENDS short = TEXT*3;
                    population = 0.100E7 .. 0.100E10;
                    cityPopulation EXTENDS population = 0.500E7 .. 0.100E9;
            END Model.
            """,
            Description: """
                Legal restrictions stay silent: a pure alias, an enumeration refinement, an OID domain extending OID ANY
                (RefHB 3.8.13), and genuine range/length narrowing.
                """,
            RefHB: "3.8.1",
            AssertOutput: false));

        yield return FullFile(new(
            "Domain extension refining precision is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    countryPopulation = 0.1E7 .. 0.1E10;
                    cityPopulation EXTENDS countryPopulation = 0.501E6 .. 0.100E7;
                    number = 5 .. 10;
                    precisionNumber EXTENDS number = 4.501 .. 10.499;
            END Model.
            """,
            Description: """
                The precision may not be changed in either direction — refining is as invalid as reducing
                (the RefHB 3.8.5-5 example marks the refined 'Genau' as "falsch, da Stellenzahl grösser").
                """,
            ExpectedLog:
            [
                "Type check error in 'Model.cityPopulation' at 5:8-5:70: the precision must match the inherited precision.",
                "Type check error in 'Model.precisionNumber' at 7:8-7:57: the precision must match the inherited precision.",
            ],
            RefHB: "3.8.5",
            AssertOutput: false));

        yield return FullFile(new(
            "Extension of a bound-less middle domain is checked against the inherited range",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    size = 0 .. 100;
                    anySize (ABSTRACT) EXTENDS size = NUMERIC;
                    narrowSize EXTENDS anySize = 10 .. 90;
                    wideSize EXTENDS anySize = 0 .. 200;
                    fineSize EXTENDS anySize = 0.00 .. 50.00;
                    floatSize EXTENDS anySize = 0.10E1 .. 0.90E2;
            END Model.
            """,
            Description: """
                The bound-less middle domain is itself illegal ('Bound-less extension of a concrete numeric domain is
                rejected'), but it still INHERITS its base's bounds, precision and notation into its effective type, so
                its extensions are checked against that inherited state — not against the bare NUMERIC it declares.
                (ili2c reports only the middle domain and never sees the widening further down the chain; the file
                verdicts agree nonetheless.)
                """,
            ExpectedLog:
            [
                "Type check error in 'Model.anySize' at 5:8-5:50: an abstract NUMERIC can not extend a concrete numeric range.",
                "Type check error in 'Model.wideSize' at 7:8-7:44: the value range must not be wider than the inherited range.",
                "Type check error in 'Model.fineSize' at 8:8-8:49: the precision must match the inherited precision.",
                "Type check error in 'Model.floatSize' at 9:8-9:53: the domain must be of the same kind as its base 'anySize'.",
            ],
            RefHB: "3.8-4/3.8.5-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Domain extension unit rules are enforced",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    plain = 0 .. 10;
                    withUnit EXTENDS plain = 0 .. 5 [INTERLIS.m];
                    dist (ABSTRACT) = NUMERIC [INTERLIS.LENGTH];
                    seconds EXTENDS dist = 0.0 .. 9.9 [INTERLIS.s];
                    vague EXTENDS dist = 0.0 .. 9.9;
                    meters EXTENDS dist = 0.00 .. 100.00 [INTERLIS.m];
                    half EXTENDS meters = 0.00 .. 50.00;
                    switched EXTENDS meters = 0.00 .. 50.00 [INTERLIS.s];
            END Model.
            """,
            Description: """
                RefHB 3.8.5-8/-9/-10, checked against the effective inherited unit: a unit-less concrete base admits no
                unit, an inherited abstract unit must be refined by an extension of it (and must be concretized once the
                range is), and an inherited concrete unit can not be overridden. 'meters' and 'half' show the legal
                forms: refining the abstract unit, and inheriting the concrete unit silently.
                """,
            ExpectedLog:
            [
                "Type check error in 'Model.withUnit' at 5:8-5:53: the inherited definition has no unit, so the extension can not introduce one.",
                "Type check error in 'Model.seconds' at 7:8-7:55: the unit 's' must be an extension of the inherited abstract unit 'LENGTH'.",
                "Type check error in 'Model.vague' at 8:8-8:40: the inherited abstract unit 'LENGTH' must be concretized along with the value range.",
                "Type check error in 'Model.switched' at 11:8-11:61: the inherited concrete unit 'm' can not be overridden.",
            ],
            RefHB: "3.8.5-8",
            AssertOutput: false));

        yield return FullFile(new(
            "Implicit ANYUNIT unit extension is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    any (ABSTRACT) = NUMERIC [INTERLIS.ANYUNIT];
                    anyMeters EXTENDS any = 0.0 .. 9.9 [INTERLIS.m];
            END Model.
            """,
            Description: """
                Every unit inherits ANYUNIT implicitly, "ohne dass dies definiert werden muss". (ili2c agrees for domain
                extensions; its stricter explicit-EXTENDS demand only appears in refsystem axis checks, see the
                ContextTest comparison cases.)
                """,
            RefHB: "3.9.1-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Abstract unit on a concrete numeric range is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    bad = 0.0 .. 9.9 [INTERLIS.LENGTH];
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.bad' at 4:8-4:43: the abstract unit 'LENGTH' is only allowed while the value range is undefined."],
            RefHB: "3.8.5-6",
            AssertOutput: false));

        yield return FullFile(new(
            "Unlimited text extension of a restricted text is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    short = TEXT*5;
                    unlimited EXTENDS short = TEXT;
            END Model.
            """,
            Description: """
                A bare TEXT/MTEXT declares an UNLIMITED length — unlike a bound-less NUMERIC it is not an omitted
                definition part — so it can not extend a length-restricted base (a lengthening is explicitly called
                incompatible with the base there). ili2c rejects this too.
                """,
            ExpectedLog: ["Type check error in 'Model.unlimited' at 5:8-5:39: the text length must not exceed the inherited length."],
            RefHB: "3.8.1-3",
            AssertOutput: false));

        yield return FullFile(new(
            "OID definitions extended outside the predefined ladder",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Oid8 = OID TEXT*8;
                    Oid6 EXTENDS Oid8 = OID TEXT*6;
                    StillAny EXTENDS INTERLIS.ANYOID = OID ANY;
            END Model.
            """,
            Description: """
                RefHB 3.8.9-13: an OID definition can not be extended, except that NOOID may be extended by ANYOID
                and an OID ANY definition by a concrete one (not OID ANY). A concrete OID definition is final —
                narrowing an identity value range would invalidate identities existing objects already carry.
                """,
            RefHB: "3.8.9-13",
            ExpectedLog:
            [
                "Type check error in 'Model.Oid6' at 5:8-5:39: a concrete OID definition can not be extended.",
                "Type check error in 'Model.StillAny' at 6:8-6:51: an OID ANY definition can only be extended by a concrete OID definition (not OID ANY).",
            ],
            Ili2cDivergenceReason: """
                ili2c accepts any OID domain extension (concrete over concrete, OID ANY over ANYOID); RefHB
                3.8.9-13 only allows the NOOID -> ANYOID -> concrete ladder, so we reject.
                """,
            AssertOutput: false));


        yield return FullFile(new(
            "OID definition over a bound-less numeric range",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Open = OID NUMERIC;
                    Declared (ABSTRACT) = OID NUMERIC;
            END Model.
            """,
            Description: """
                An OID value range is as complete as its inner type: OID NUMERIC leaves the range undefined like
                any bound-less NUMERIC (RefHB 3.8.5-1), so the domain must be declared ABSTRACT (RefHB 3.8-3).
                """,
            RefHB: "3.8.9-3",
            ExpectedLog: ["Type check error in 'Model.Open' at 4:8-4:27: must be declared ABSTRACT because its type is not fully defined."],
            Ili2cDivergenceReason: """
                ili2c accepts a concrete domain defined as OID NUMERIC; a bound-less NUMERIC counts as abstract
                (RefHB 3.8.5-1) and an incomplete definition must be declared ABSTRACT (RefHB 3.8-3), so we
                reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Domain constraints over the value",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Percent = 0 .. 100 CONSTRAINTS aboveHalf: THIS > 50;
                    ShortText = TEXT*20 CONSTRAINTS notTiny: INTERLIS.len(THIS) > 2;
                    Color = (red, green) CONSTRAINTS noRed: THIS != #red;
                    Flag = BOOLEAN CONSTRAINTS set: THIS;
            END Model.
            """,
            Description: """
                A domain constraint's condition is an expression over the domain value, referred to as THIS
                (RefHB 3.8-8/-10): comparisons, function calls and enumeration constants are all legal, and a bare
                THIS passes the boolean check unjudged (its result type is not statically derived).
                """,
            RefHB: "3.8-8",
            AssertOutput: false));

        yield return FullFile(new(
            "Unknown name in a domain constraint is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    ShortText = TEXT*20 CONSTRAINTS c: Unknown > 1;
            END Model.
            """,
            Description: """
                There is no context object in a domain constraint, so no path but the bare THIS can resolve
                (RefHB 3.8-10; ili2c's grammar rejects any other path outright).
                """,
            RefHB: "3.8-8",
            ExpectedLog: ["'Unknown' at 4:43-4:50 can not be used in domain constraint 'c' of 'Model.ShortText' because the condition can only refer to the domain value itself (THIS)"],
            AssertOutput: false));

        yield return FullFile(new(
            "Descending from THIS in a domain constraint is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    ShortText = TEXT*20 CONSTRAINTS c: THIS -> Foo > 1;
            END Model.
            """,
            Description: "A domain value has no members to step into; only the bare THIS is resolvable (RefHB 3.8-10).",
            RefHB: "3.8-8",
            ExpectedLog: ["'Foo' at 4:43-4:54 can not be used in domain constraint 'c' of 'Model.ShortText' because the condition can only refer to the domain value itself (THIS)"],
            AssertOutput: false));

        yield return FullFile(new(
            "Non-boolean domain constraint condition is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    ShortText = TEXT*20 CONSTRAINTS c: 5;
            END Model.
            """,
            Description: "RefHB 3.8-10: the condition is a Logical-Expression, so its result must be boolean.",
            RefHB: "3.8-8",
            ExpectedLog: ["Type check error in 'Model.ShortText' at 4:43-4:44: the condition of domain constraint 'c' must be a boolean expression."],
            Ili2cDivergenceReason: """
                ili2c does not judge a domain constraint's result type at all (a bare numeric constant is
                accepted); RefHB 3.8-10 demands a Logical-Expression, so we reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Duplicate domain constraint names are rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    ShortText = TEXT*20 CONSTRAINTS c: INTERLIS.len(THIS) > 2, c: INTERLIS.len(THIS) < 8;
            END Model.
            """,
            Description: """
                RefHB 3.8-8: every constraint has a unique name within its domain definition — enforced by
                construction (the constraints are keyed by name), so the duplicate is reported and dropped at
                build time.
                """,
            RefHB: "3.8-8",
            ExpectedLog: ["Compile error at 4:67-4:68 Duplicate domain constraint c."],
            Ili2cDivergenceReason: """
                ili2c accepts duplicate domain constraint names; RefHB 3.8-8 demands per-domain uniqueness
                ("Jede Einschränkung hat innerhalb der Wertebereichsdefinition einen eindeutigen Namen"), so we
                reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Constraint-only domain extension",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Base = TEXT*20 CONSTRAINTS notTiny: INTERLIS.len(THIS) > 2;
                    Sub EXTENDS Base = MANDATORY CONSTRAINTS notHuge: INTERLIS.len(THIS) < 8;
            END Model.
            """,
            Description: """
                An extension may add restrictions without repeating the type (RefHB 3.8-10: MANDATORY with the
                Type omitted): 'Sub' inherits Base's TEXT*20 and both restrictions apply — every restriction of
                the chain does (RefHB 3.8-8 "gelten alle").
                """,
            RefHB: "3.8-8",
            AssertOutput: false));

        yield return FullFile(new(
            "Inherited domain constraint name reused in an extension is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Base = TEXT*20 CONSTRAINTS c: INTERLIS.len(THIS) > 2;
                    Middle EXTENDS Base = MANDATORY CONSTRAINTS d: INTERLIS.len(THIS) < 15;
                    Sub EXTENDS Middle = TEXT*10 CONSTRAINTS c: INTERLIS.len(THIS) < 8;
            END Model.
            """,
            Description: """
                Constraints have no override semantics — every restriction of the chain applies (RefHB 3.8-8
                "gelten alle") — so a name reused from anywhere in the inherited chain (here from Base, two
                levels up across the constraint-only 'Middle') would leave two same-named restrictions in the
                effective domain, unaddressable for diagnostics and tooling.
                """,
            RefHB: "3.8-8",
            ExpectedLog: ["Type check error in 'Model.Sub' at 6:8-6:75: the domain constraint name 'c' is already used by an inherited constraint."],
            Ili2cDivergenceReason: """
                ili2c accepts a domain constraint whose name repeats an inherited one; both restrictions apply
                (RefHB 3.8-8 "gelten alle", no override semantics), leaving two same-named restrictions in the
                effective domain, so we reject the reuse.
                """,
            AssertOutput: false));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            DOMAIN
                {fragment}
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(DomainTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadDomainTypeDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitDomainTypeDef(p.domainTypeDef()));
    }

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }
}
