using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class FunctionTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Function with argument and explanation",
            "FUNCTION Area (Region : 0 .. 100) : 0 .. 9999 // area //;",
            RefHB: "3.14-1",
            Expected: new FunctionDef
            {
                Name = "Area",
                NameLocations = { new RangePosition(0, 9, 0, 13) },
                ReturnType = new DecimalType { Min = 0, Max = 9999, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 36, 0, 45) },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Region",
                        Type = new DecimalType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(0, 24, 0, 32) },
                    },
                },
                Explanation = " area ",
            }));

        yield return Rule(new(
            "Function without arguments",
            "FUNCTION f (): TEXT;",
            Description: "Empty parameter list ('(' [ ... ] ')'): the argument list is optional.",
            RefHB: "3.14-10",
            Expected: new FunctionDef
            {
                Name = "f",
                NameLocations = { new RangePosition(0, 9, 0, 10) },
                ReturnType = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 15, 0, 19),
                },
            }));

        yield return Rule(new(
            "Function argument OBJECT OF ANYCLASS",
            "FUNCTION f (o: OBJECT OF ANYCLASS): BOOLEAN;",
            Description: "ArgumentType: 'OBJECT' 'OF' RestrictedClassOrAssRef (singular).",
            RefHB: "3.14-11",
            Expected: new FunctionDef
            {
                Name = "f",
                NameLocations = { new RangePosition(0, 9, 0, 10) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 36, 0, 43),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "o",
                        Type = new ObjectType
                        {
                            Cardinality = new Cardinality { Min = 1, Max = 1 },
                            Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }],
                            SourceRange = new RangePosition(0, 15, 0, 33),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Function argument OBJECTS OF named class",
            "FUNCTION f (Objects: OBJECTS OF MyClass): BOOLEAN;",
            Description: """
                ArgumentType: 'OBJECTS' 'OF' RestrictedClassOrAssRef (named class). The target reference resolves
                against the function's enclosing scope, so a dangling name is reported (see 'Function argument over an
                unknown class is reported').
                """,
            RefHB: "3.14-11",
            Expected: new FunctionDef
            {
                Name = "f",
                NameLocations = { new RangePosition(0, 9, 0, 10) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 42, 0, 49),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Objects",
                        Type = new ObjectType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            Targets = [new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { "MyClass" }, SourceRange = new RangePosition(0, 32, 0, 39) } }],
                            SourceRange = new RangePosition(0, 21, 0, 39),
                        },
                    },
                },
            }));

        yield return FullFile(new(
            "Function argument over a defined class is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                CLASS MyClass (ABSTRACT) =
                END MyClass;
                FUNCTION f (Objects: OBJECTS OF MyClass): BOOLEAN;
            END Model.
            """,
            RefHB: "3.14-11",
            AssertOutput: false));

        yield return FullFile(new(
            "Function argument over an unknown class is reported",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                FUNCTION f (Objects: OBJECTS OF MyClass): BOOLEAN;
            END Model.
            """,
            ExpectedLog: ["Could not resolve 'reference 'MyClass' from Model'"],
            RefHB: "3.14-11",
            AssertOutput: false));

        yield return FullFile(new(
            "Function argument OBJECTS OF ANYSTRUCTURE is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                FUNCTION f (Objects: OBJECTS OF ANYSTRUCTURE): BOOLEAN;
            END Model.
            """,
            Description: """
                OBJECT/OBJECTS OF takes a RestrictedClassOrAssRef — ANYSTRUCTURE is not among its alternatives (ili2c's
                grammar rejects it outright; the merged restricted-reference rule accepts it for uniform parsing, so the
                type checker enforces the context).
                """,
            ExpectedLog: ["Type check error in 'Model.f': the object argument 'Objects' can not take ANYSTRUCTURE; a class, an association or ANYCLASS is required."],
            RefHB: "3.14-12",
            AssertOutput: false));

        yield return Rule(new(
            "Standard function myClass",
            "FUNCTION myClass (Object: ANYSTRUCTURE): STRUCTURE;",
            Description: "Standard function myClass (Object: ANYSTRUCTURE): STRUCTURE; — ANYSTRUCTURE argument, STRUCTURE result.",
            RefHB: "3.14-13",
            Expected: new FunctionDef
            {
                Name = "myClass",
                NameLocations = { new RangePosition(0, 9, 0, 16) },
                ReturnType = new ClassType
                {
                    IsStructure = true,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 41, 0, 50),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Object",
                        Type = new UnresolvedNamedType
                        {
                            Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 26, 0, 38),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function isSubClass",
            "FUNCTION isSubClass (potSubClass: STRUCTURE; potSuperClass: STRUCTURE): BOOLEAN;",
            Description: "Standard function isSubClass — two STRUCTURE arguments, BOOLEAN result.",
            RefHB: "3.14-15",
            Expected: new FunctionDef
            {
                Name = "isSubClass",
                NameLocations = { new RangePosition(0, 9, 0, 19) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 72, 0, 79),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "potSubClass",
                        Type = new ClassType
                        {
                            IsStructure = true,
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 34, 0, 43),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "potSuperClass",
                        Type = new ClassType
                        {
                            IsStructure = true,
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 60, 0, 69),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function isOfClass",
            "FUNCTION isOfClass (Object: ANYSTRUCTURE; Class: STRUCTURE): BOOLEAN;",
            Description: "Standard function isOfClass — ANYSTRUCTURE and STRUCTURE arguments, BOOLEAN result.",
            RefHB: "3.14-17",
            Expected: new FunctionDef
            {
                Name = "isOfClass",
                NameLocations = { new RangePosition(0, 9, 0, 18) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 61, 0, 68),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Object",
                        Type = new UnresolvedNamedType
                        {
                            Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 28, 0, 40),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "Class",
                        Type = new ClassType
                        {
                            IsStructure = true,
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 49, 0, 58),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function elementCount",
            "FUNCTION elementCount (bag: BAG OF ANYSTRUCTURE): NUMERIC;",
            Description: "Standard function elementCount — BAG OF ANYSTRUCTURE argument.",
            RefHB: "3.14-19",
            Expected: new FunctionDef
            {
                Name = "elementCount",
                NameLocations = { new RangePosition(0, 9, 0, 21) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 50, 0, 57),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "bag",
                        Type = new UnresolvedNamedType
                        {
                            Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            SourceRange = new RangePosition(0, 35, 0, 47),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function objectCount",
            "FUNCTION objectCount (Objects: OBJECTS OF ANYCLASS): NUMERIC;",
            Description: "Standard function objectCount — OBJECTS OF ANYCLASS argument.",
            RefHB: "3.14-21",
            Expected: new FunctionDef
            {
                Name = "objectCount",
                NameLocations = { new RangePosition(0, 9, 0, 20) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 53, 0, 60),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Objects",
                        Type = new ObjectType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }],
                            SourceRange = new RangePosition(0, 31, 0, 50),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function len",
            "FUNCTION len (TextVal: TEXT): NUMERIC;",
            Description: "Standard function len — TEXT argument, NUMERIC result, no explanation (Explanation is optional).",
            RefHB: "3.14-23",
            Expected: new FunctionDef
            {
                Name = "len",
                NameLocations = { new RangePosition(0, 9, 0, 12) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 30, 0, 37),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "TextVal",
                        Type = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 23, 0, 27),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function lenM",
            "FUNCTION lenM (TextVal: MTEXT): NUMERIC;",
            Description: "Standard function lenM — MTEXT argument.",
            RefHB: "3.14-23",
            Expected: new FunctionDef
            {
                Name = "lenM",
                NameLocations = { new RangePosition(0, 9, 0, 13) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 32, 0, 39),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "TextVal",
                        Type = new TextType
                        {
                            IsMText = true,
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 24, 0, 29),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function trim",
            "FUNCTION trim (TextVal: TEXT): TEXT;",
            Description: "Standard function trim — TEXT argument, TEXT result.",
            RefHB: "3.14-25",
            Expected: new FunctionDef
            {
                Name = "trim",
                NameLocations = { new RangePosition(0, 9, 0, 13) },
                ReturnType = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 31, 0, 35),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "TextVal",
                        Type = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 24, 0, 28),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function isEnumSubVal",
            "FUNCTION isEnumSubVal (SubVal: ENUMTREEVAL; NodeVal: ENUMTREEVAL): BOOLEAN;",
            Description: "Standard function isEnumSubVal — two ENUMTREEVAL arguments (EnumerationValuesType accepting tree node values too).",
            RefHB: "3.14-27",
            Expected: new FunctionDef
            {
                Name = "isEnumSubVal",
                NameLocations = { new RangePosition(0, 9, 0, 21) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 67, 0, 74),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "SubVal",
                        Type = new EnumerationValuesType
                        {
                            LeafsOnly = false,
                            SourceRange = new RangePosition(0, 31, 0, 42),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "NodeVal",
                        Type = new EnumerationValuesType
                        {
                            LeafsOnly = false,
                            SourceRange = new RangePosition(0, 53, 0, 64),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function inEnumRange",
            "FUNCTION inEnumRange (Enum: ENUMVAL; MinVal: ENUMTREEVAL; MaxVal: ENUMTREEVAL): BOOLEAN;",
            Description: "Standard function inEnumRange — an ENUMVAL argument (leaf values only) and two ENUMTREEVAL arguments (tree node values too), distinguished on EnumerationValuesType.LeafsOnly.",
            RefHB: "3.14-29",
            Expected: new FunctionDef
            {
                Name = "inEnumRange",
                NameLocations = { new RangePosition(0, 9, 0, 20) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 80, 0, 87),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Enum",
                        Type = new EnumerationValuesType
                        {
                            LeafsOnly = true,
                            SourceRange = new RangePosition(0, 28, 0, 35),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "MinVal",
                        Type = new EnumerationValuesType
                        {
                            LeafsOnly = false,
                            SourceRange = new RangePosition(0, 45, 0, 56),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "MaxVal",
                        Type = new EnumerationValuesType
                        {
                            LeafsOnly = false,
                            SourceRange = new RangePosition(0, 66, 0, 77),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function convertUnit",
            "FUNCTION convertUnit (from: NUMERIC): NUMERIC;",
            Description: "Standard function convertUnit — NUMERIC argument and NUMERIC result (unit conversion).",
            RefHB: "3.14-31",
            Expected: new FunctionDef
            {
                Name = "convertUnit",
                NameLocations = { new RangePosition(0, 9, 0, 20) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 38, 0, 45),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "from",
                        Type = new NumericType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 28, 0, 35),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function length (POLYLINE)",
            "FUNCTION length (geom: POLYLINE): NUMERIC;",
            Description: "Standard function length — POLYLINE argument.",
            RefHB: "3.14-33",
            Expected: new FunctionDef
            {
                Name = "length",
                NameLocations = { new RangePosition(0, 9, 0, 15) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 34, 0, 41),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "geom",
                        Type = new PolyLineType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 23, 0, 31),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function multilength (MULTIPOLYLINE)",
            "FUNCTION multilength (geom: MULTIPOLYLINE): NUMERIC;",
            Description: "Standard function multilength — MULTIPOLYLINE argument.",
            RefHB: "3.14-34",
            Expected: new FunctionDef
            {
                Name = "multilength",
                NameLocations = { new RangePosition(0, 9, 0, 20) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 44, 0, 51),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "geom",
                        Type = new PolyLineType
                        {
                            IsMultiGeometry = true,
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 28, 0, 41),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function surface (SURFACE)",
            "FUNCTION surface (geom: SURFACE): NUMERIC;",
            Description: "Standard function surface — SURFACE argument.",
            RefHB: "3.14-36",
            Expected: new FunctionDef
            {
                Name = "surface",
                NameLocations = { new RangePosition(0, 9, 0, 16) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 34, 0, 41),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "geom",
                        Type = new SurfaceType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 24, 0, 31),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function multisurface (MULTISURFACE)",
            "FUNCTION multisurface (geom: MULTISURFACE): NUMERIC;",
            Description: "Standard function multisurface — MULTISURFACE argument.",
            RefHB: "3.14-37",
            Expected: new FunctionDef
            {
                Name = "multisurface",
                NameLocations = { new RangePosition(0, 9, 0, 21) },
                ReturnType = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 44, 0, 51),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "geom",
                        Type = new SurfaceType
                        {
                            IsMultiGeometry = true,
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 29, 0, 41),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function areAreas (ATTRIBUTE OF)",
            "FUNCTION areAreas (Objects: OBJECTS OF ANYCLASS; SurfaceBag: ATTRIBUTE OF @ Objects RESTRICTION (BAG OF ANYSTRUCTURE); SurfaceAttr: ATTRIBUTE OF @ SurfaceBag RESTRICTION (SURFACE)): BOOLEAN;",
            Description: """
                Standard function areAreas — ATTRIBUTE OF @ ... RESTRICTION (...) arguments (RefHB worked example,
                3.8.11-4: the '@' argument reference is part of the OF clause).
                """,
            RefHB: "3.14-39",
            Expected: new FunctionDef
            {
                Name = "areAreas",
                NameLocations = { new RangePosition(0, 9, 0, 17) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 182, 0, 189),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Objects",
                        Type = new ObjectType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }],
                            SourceRange = new RangePosition(0, 28, 0, 47),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "SurfaceBag",
                        Type = new AttributePathType
                        {
                            ArgumentName = "Objects",
                            Restrictions =
                            {
                                new UnresolvedNamedType
                                {
                                    Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    SourceRange = new RangePosition(0, 104, 0, 116),
                                },
                            },
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 61, 0, 117),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "SurfaceAttr",
                        Type = new AttributePathType
                        {
                            ArgumentName = "SurfaceBag",
                            Restrictions =
                            {
                                new SurfaceType
                                {
                                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                                    SourceRange = new RangePosition(0, 171, 0, 178),
                                },
                            },
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 132, 0, 179),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function areAreas2 (TEXT paths)",
            "FUNCTION areAreas2 (Objects: OBJECTS OF ANYCLASS; SurfaceBag: TEXT; SurfaceAttr: TEXT): BOOLEAN;",
            Description: "Standard function areAreas2 — TEXT-based attribute-path variant.",
            RefHB: "3.14-41",
            Expected: new FunctionDef
            {
                Name = "areAreas2",
                NameLocations = { new RangePosition(0, 9, 0, 18) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 88, 0, 95),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Objects",
                        Type = new ObjectType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }],
                            SourceRange = new RangePosition(0, 29, 0, 48),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "SurfaceBag",
                        Type = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 62, 0, 66),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "SurfaceAttr",
                        Type = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 81, 0, 85),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function areAreasWithoutGaps (ATTRIBUTE OF)",
            "FUNCTION areAreasWithoutGaps (Objects: OBJECTS OF ANYCLASS; SurfaceBag: ATTRIBUTE OF @ Objects RESTRICTION (BAG OF ANYSTRUCTURE); SurfaceAttr: ATTRIBUTE OF @ SurfaceBag RESTRICTION (SURFACE)): BOOLEAN;",
            Description: """
                Standard function areAreasWithoutGaps — ATTRIBUTE OF @ ... RESTRICTION worked example (RefHB,
                3.8.11-4: the '@' argument reference is part of the OF clause).
                """,
            RefHB: "3.14-45",
            Expected: new FunctionDef
            {
                Name = "areAreasWithoutGaps",
                NameLocations = { new RangePosition(0, 9, 0, 28) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 193, 0, 200),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Objects",
                        Type = new ObjectType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }],
                            SourceRange = new RangePosition(0, 39, 0, 58),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "SurfaceBag",
                        Type = new AttributePathType
                        {
                            ArgumentName = "Objects",
                            Restrictions =
                            {
                                new UnresolvedNamedType
                                {
                                    Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    SourceRange = new RangePosition(0, 115, 0, 127),
                                },
                            },
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 72, 0, 128),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "SurfaceAttr",
                        Type = new AttributePathType
                        {
                            ArgumentName = "SurfaceBag",
                            Restrictions =
                            {
                                new SurfaceType
                                {
                                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                                    SourceRange = new RangePosition(0, 182, 0, 189),
                                },
                            },
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 143, 0, 190),
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Standard function areAreasWithoutGaps2 (TEXT paths)",
            "FUNCTION areAreasWithoutGaps2 (Objects: OBJECTS OF ANYCLASS; SurfaceBag: TEXT; SurfaceAttr: TEXT): BOOLEAN;",
            Description: "Standard function areAreasWithoutGaps2 — TEXT-based attribute-path variant (RefHB worked example).",
            RefHB: "3.14-46",
            Expected: new FunctionDef
            {
                Name = "areAreasWithoutGaps2",
                NameLocations = { new RangePosition(0, 9, 0, 29) },
                ReturnType = new BooleanType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 99, 0, 106),
                },
                Arguments =
                {
                    new FunctionArgument
                    {
                        Name = "Objects",
                        Type = new ObjectType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }],
                            SourceRange = new RangePosition(0, 40, 0, 59),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "SurfaceBag",
                        Type = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 73, 0, 77),
                        },
                    },
                    new FunctionArgument
                    {
                        Name = "SurfaceAttr",
                        Type = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 92, 0, 96),
                        },
                    },
                },
            }));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            {fragment}
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(FunctionTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadFunctionDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitFunctionDef(p.functionDef()));
    }

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }
}
