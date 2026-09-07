using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

// RefHB 3.15 Sichten (views).
public class ViewTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "View attribute with property derived by assignment",
            "VIEW V PROJECTION OF base~Class; = ATTRIBUTE NewAttr (FINAL) := base->Attr; END V;",
            Description: """
                ViewAttributes 'Attribute-Name Properties := Expression' production: derived attribute carrying a
                property (FINAL).
                """,
            RefHB: "3.15-6",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 80, 0, 81) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                    { "NewAttr", new AttributeDef
                    {
                        Name = "NewAttr",
                        NameLocations = { new RangePosition(0, 45, 0, 52) },
                        TypeDef = UndefinedType.Instance,
                        Properties = { Property.Final },
                        Values = {
                            new PathExpression
                            {
                                Path = {
                                    new IdentifierPathElement
                                    {
                                        Value = "base",
                                    },
                                    new IdentifierPathElement
                                    {
                                        Value = "Attr",
                                    },
                                },
                            },
                        },
                    } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                },
            }));

        yield return Rule(new(
            "Abstract view property",
            "VIEW V (ABSTRACT) PROJECTION OF base~Class; = ATTRIBUTE END V;",
            Description: "ViewDef property ABSTRACT (RefHB 3.15-3 / 3.15-7 Properties).",
            RefHB: "3.15-7",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 60, 0, 61) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 32, 0, 36) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 37, 0, 42) } } },
                },
                Properties = { Property.Abstract },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 32, 0, 36) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 37, 0, 42) } },
                },
            }));

        yield return Rule(new(
            "Transient view property",
            "VIEW V (TRANSIENT) PROJECTION OF base~Class; = ATTRIBUTE END V;",
            Description: "ViewDef property TRANSIENT (RefHB 3.15-2 / 3.15-7 Properties).",
            RefHB: "3.15-7",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 61, 0, 62) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 33, 0, 37) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 38, 0, 43) } } },
                },
                Properties = { Property.Transient },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 33, 0, 37) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 38, 0, 43) } },
                },
            }));

        yield return Rule(new(
            "View extends another view",
            "VIEW V EXTENDS BaseView = ATTRIBUTE END V;",
            Description: "ViewDef EXTENDS another view, no formation (RefHB 3.15-4).",
            RefHB: "3.15-7",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 40, 0, 41) },
                Extends = new Reference<ViewDef> { Path = { "BaseView" }, SourceRange = new RangePosition(0, 15, 0, 23) },
            }));

        yield return Rule(new(
            "Abstract view extends another view",
            "VIEW V (ABSTRACT) EXTENDS BaseView = ATTRIBUTE END V;",
            Description: "ViewDef EXTENDS combined with a property (RefHB 3.15-4).",
            RefHB: "3.15-7",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 51, 0, 52) },
                Properties = { Property.Abstract },
                Extends = new Reference<ViewDef> { Path = { "BaseView" }, SourceRange = new RangePosition(0, 26, 0, 34) },
            }));

        yield return Rule(new(
            "View extends a qualified view ref",
            "VIEW V EXTENDS ModelX.TopicX.BaseView = ATTRIBUTE END V;",
            Description: "ViewRef qualified with Model '.' Topic '.' View-Name used in an EXTENDS (RefHB 3.15-9).",
            RefHB: "3.15-8",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 54, 0, 55) },
                Extends = new Reference<ViewDef> { Path = { "ModelX", "TopicX", "BaseView" }, SourceRange = new RangePosition(0, 15, 0, 37) },
            }));

        yield return Rule(new(
            "Aggregation view attribute from AGGREGATES",
            "VIEW V AGGREGATION OF base~Class ALL; = ATTRIBUTE NewAttr := AGGREGATES; END V;",
            Description: "Implicit AGGREGATES attribute assigned to a view attribute inside an aggregation view (RefHB 3.15-16).",
            RefHB: "3.15-14",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 77, 0, 78) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 22, 0, 26) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 27, 0, 32) } } },
                    { "NewAttr", new AttributeDef
                    {
                        Name = "NewAttr",
                        NameLocations = { new RangePosition(0, 50, 0, 57) },
                        TypeDef = UndefinedType.Instance,
                        Values = {
                            new PathExpression
                            {
                                Path = {
                                    new KeyWordPathElement
                                    {
                                        Value = KeyWordPathElement.KeyWord.Aggregates,
                                    },
                                },
                            },
                        },
                    } },
                },
                Formation = new AggregationView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 22, 0, 26) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 27, 0, 32) } },
                    All = true,
                },
            }));

        yield return Rule(new(
            "Projection view",
            "VIEW V PROJECTION OF base~Class; = ATTRIBUTE END V;",
            Description: """
                The rule-level case references an undefined class (only meaningful with a surrounding viewable): its AST
                is verified by ReadViewDef, but the comparison diverges (ili2c rejects the undefined class) and is left
                red as a known compiler-gap signal. The self-contained full-file case exercises the comparison with both
                compilers accepting.
                """,
            RefHB: "3.15-29",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 49, 0, 50) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                },
            }));

        yield return FullFile(new(
            "Projection view in a topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        Attr : TEXT;
                    END Class;

                    VIEW AView
                      PROJECTION OF base~Class;
                      =
                      ATTRIBUTE
                        ALL OF base;
                    END AView;
                END Topic;
            END Model.
            """,
            RefHB: "3.15-29",
            Expected: TestTools.Build(() =>
            {
                var classDef = new ClassDef
                {
                    Name = "Class",
                    Content =
                    {
                        {
                            "Attr",
                            new AttributeDef
                            {
                                Name = "Attr",
                                TypeDef = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                            }
                        },
                    },
                };

                var baseView = new BaseView
                {
                    Name = "base",
                    IsRenamed = true,
                    Viewable = new Reference<IInterlisDefinition> { Target = classDef, Path = { "Class" } },
                };

                return new InterlisEnvironment
                {
                    Version = 2.4,
                    Content =
                    {
                        { InternalModel.Interlis.Name, InternalModel.Interlis },
                        {
                            "Model",
                            new ModelDef
                            {
                                Name = "Model",
                                URI = "http://example.com",
                                Version = "1.0.0",
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                                },
                                Content =
                                {
                                    {
                                        "Topic",
                                        new TopicDef
                                        {
                                            Name = "Topic",
                                            Content =
                                            {
                                                { "Class", classDef },
                                                {
                                                    "AView",
                                                    new ViewDef
                                                    {
                                                        Name = "AView",
                                                        // The formation base is lifted into the view's namespace as an addressable base view
                                                        // (shares the formation reference; resolved to the base class).
                                                        Content =
                                                        {
                                                            { "base", baseView },
                                                        },
                                                        Formation = new ProjectionView
                                                        {
                                                            Source = baseView,
                                                        },
                                                        AllOfBases = { new Reference<BaseView> { Target = baseView, Path = { "base" }, SourceRange = new RangePosition(11, 19, 11, 23) } },
                                                    }
                                                },
                                            },
                                        }
                                    },
                                }
                            }
                        },
                    }
                };
            })));

        yield return Rule(new(
            "Projection view without base alias",
            "VIEW V PROJECTION OF Class; = ATTRIBUTE END V;",
            Description: "Projection with a plain ViewableRef (no Base '~' alias) (RefHB 3.15-38 RenamedViewableRef).",
            RefHB: "3.15-36",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 44, 0, 45) },
                Content = {
                    { "Class", new BaseView { Name = "Class", Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 21, 0, 26) } } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "Class", Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 21, 0, 26) } },
                },
            }));

        yield return Rule(new(
            "Join view",
            "VIEW V JOIN OF base1~ClassA, base2~ClassB; = ATTRIBUTE END V;",
            Description: "JOIN OF several viewables (RefHB 3.15-13 / 3.15-32).",
            RefHB: "3.15-30",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 59, 0, 60) },
                Content = {
                    { "base1", new BaseView { Name = "base1", NameLocations = { new RangePosition(0, 15, 0, 20) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassA" }, SourceRange = new RangePosition(0, 21, 0, 27) } } },
                    { "base2", new BaseView { Name = "base2", NameLocations = { new RangePosition(0, 29, 0, 34) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassB" }, SourceRange = new RangePosition(0, 35, 0, 41) } } },
                },
                Formation = new JoinView
                {
                    Sources = {
                        new JoinViewSource
                        {
                            Viewable = new BaseView { Name = "base1", NameLocations = { new RangePosition(0, 15, 0, 20) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassA" }, SourceRange = new RangePosition(0, 21, 0, 27) } },
                        },
                        new JoinViewSource
                        {
                            Viewable = new BaseView { Name = "base2", NameLocations = { new RangePosition(0, 29, 0, 34) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassB" }, SourceRange = new RangePosition(0, 35, 0, 41) } },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Join view with OR NULL",
            "VIEW V JOIN OF base1~ClassA, base2~ClassB (OR NULL); = ATTRIBUTE END V;",
            Description: "JOIN with an outer-join source marked (OR NULL) (RefHB 3.15-13 / 3.15-32).",
            RefHB: "3.15-30",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 69, 0, 70) },
                Content = {
                    { "base1", new BaseView { Name = "base1", NameLocations = { new RangePosition(0, 15, 0, 20) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassA" }, SourceRange = new RangePosition(0, 21, 0, 27) } } },
                    { "base2", new BaseView { Name = "base2", NameLocations = { new RangePosition(0, 29, 0, 34) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassB" }, SourceRange = new RangePosition(0, 35, 0, 41) } } },
                },
                Formation = new JoinView
                {
                    Sources = {
                        new JoinViewSource
                        {
                            Viewable = new BaseView { Name = "base1", NameLocations = { new RangePosition(0, 15, 0, 20) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassA" }, SourceRange = new RangePosition(0, 21, 0, 27) } },
                        },
                        new JoinViewSource
                        {
                            Viewable = new BaseView { Name = "base2", NameLocations = { new RangePosition(0, 29, 0, 34) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassB" }, SourceRange = new RangePosition(0, 35, 0, 41) } },
                            OrNull = true,
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Join view rejects OR NULL on the base source",
            "VIEW V JOIN OF base1~ClassA (OR NULL), base2~ClassB; = ATTRIBUTE END V;",
            Description: """
                RefHB 3.15-32: '(OR NULL)' may only mark an appended join source, never the base source; the grammar
                rejects it on the base source at parse time (ili2c rejects it likewise).
                """,
            RefHB: "3.15-30",
            ExpectedLog: ["Compile error at line 1:28 mismatched input '(' expecting ','."],
            AssertOutput: false));

        yield return Rule(new(
            "Union view",
            "VIEW V UNION OF base1~ClassA, base2~ClassB; = ATTRIBUTE END V;",
            Description: "UNION OF several viewables (RefHB 3.15-14 / 3.15-33).",
            RefHB: "3.15-31",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 60, 0, 61) },
                Content = {
                    { "base1", new BaseView { Name = "base1", NameLocations = { new RangePosition(0, 16, 0, 21) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassA" }, SourceRange = new RangePosition(0, 22, 0, 28) } } },
                    { "base2", new BaseView { Name = "base2", NameLocations = { new RangePosition(0, 30, 0, 35) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassB" }, SourceRange = new RangePosition(0, 36, 0, 42) } } },
                },
                Formation = new UnionView
                {
                    Sources = {
                        new BaseView { Name = "base1", NameLocations = { new RangePosition(0, 16, 0, 21) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassA" }, SourceRange = new RangePosition(0, 22, 0, 28) } },
                        new BaseView { Name = "base2", NameLocations = { new RangePosition(0, 30, 0, 35) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassB" }, SourceRange = new RangePosition(0, 36, 0, 42) } },
                    },
                },
            }));

        yield return Rule(new(
            "Aggregation view ALL",
            "VIEW V AGGREGATION OF base~Class ALL; = ATTRIBUTE END V;",
            Description: "AGGREGATION OF a viewable, grouping ALL objects (RefHB 3.15-15 / 3.15-34).",
            RefHB: "3.15-32",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 54, 0, 55) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 22, 0, 26) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 27, 0, 32) } } },
                },
                Formation = new AggregationView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 22, 0, 26) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 27, 0, 32) } },
                    All = true,
                },
            }));

        yield return Rule(new(
            "Aggregation view EQUAL",
            "VIEW V AGGREGATION OF base~Class EQUAL (Attr); = ATTRIBUTE END V;",
            Description: "AGGREGATION OF a viewable, grouping by EQUAL ( UniqueEl ) (RefHB 3.15-15 / 3.15-34).",
            RefHB: "3.15-32",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 63, 0, 64) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 22, 0, 26) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 27, 0, 32) } } },
                },
                Formation = new AggregationView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 22, 0, 26) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 27, 0, 32) } },
                    UniqueBy = {
                        new PathExpression
                        {
                            Path = {
                                new IdentifierPathElement
                                {
                                    Value = "Attr",
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Inspection view",
            "VIEW V INSPECTION OF base~Class -> Attr; = ATTRIBUTE END V;",
            Description: "INSPECTION OF a viewable following a structure attribute (RefHB 3.15-17 / 3.15-35).",
            RefHB: "3.15-33",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 57, 0, 58) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                },
                Formation = new InspectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                    Path = { new Reference<AttributeDef> { Path = { "Attr" }, SourceRange = new RangePosition(0, 35, 0, 39) } },
                },
            }));

        yield return Rule(new(
            "Area inspection view",
            "VIEW V AREA INSPECTION OF base~Class -> Geometry; = ATTRIBUTE END V;",
            Description: "AREA INSPECTION OF an area-partitioning viewable (RefHB 3.15-19 / 3.15-35).",
            RefHB: "3.15-33",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 66, 0, 67) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 26, 0, 30) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 31, 0, 36) } } },
                },
                Formation = new InspectionView
                {
                    IsArea = true,
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 26, 0, 30) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 31, 0, 36) } },
                    Path = { new Reference<AttributeDef> { Path = { "Geometry" }, SourceRange = new RangePosition(0, 40, 0, 48) } },
                },
            }));

        yield return Rule(new(
            "Inspection view chained path",
            "VIEW V INSPECTION OF base~Class -> Attr1 -> Attr2; = ATTRIBUTE END V;",
            Description: "INSPECTION following a chained attribute path (-> Attr1 -> Attr2) (RefHB 3.15-35).",
            RefHB: "3.15-33",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 67, 0, 68) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                },
                Formation = new InspectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                    Path = {
                        new Reference<AttributeDef> { Path = { "Attr1" }, SourceRange = new RangePosition(0, 35, 0, 40) },
                        new Reference<AttributeDef> { Path = { "Attr2" }, SourceRange = new RangePosition(0, 44, 0, 49) },
                    },
                },
            }));

        yield return Rule(new(
            "Projection with qualified viewable ref",
            "VIEW V PROJECTION OF base~ModelX.TopicX.Class; = ATTRIBUTE END V;",
            Description: "ViewableRef qualified with Model '.' Topic '.' Name (RefHB 3.15-39).",
            RefHB: "3.15-37",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 63, 0, 64) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ModelX", "TopicX", "Class" }, SourceRange = new RangePosition(0, 26, 0, 45) } } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ModelX", "TopicX", "Class" }, SourceRange = new RangePosition(0, 26, 0, 45) } },
                },
            }));

        yield return Rule(new(
            "Base extension definition",
            "VIEW V PROJECTION OF base~Class; BASE base EXTENDED BY base2~ClassB = ATTRIBUTE END V;",
            Description: "BASE base EXTENDED BY RenamedViewableRef (RefHB 3.15-42).",
            RefHB: "3.15-40",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 84, 0, 85) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                },
                BaseExtensions = {
                    new BaseExtension
                    {
                        Base = "base",
                        ExtendedBy = {
                            new BaseView { Name = "base2", NameLocations = { new RangePosition(0, 55, 0, 60) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "ClassB" }, SourceRange = new RangePosition(0, 61, 0, 67) } },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "View selection WHERE",
            "VIEW V PROJECTION OF base~Class; WHERE DEFINED (base->Attr); = ATTRIBUTE END V;",
            Description: "WHERE selection restricting the view's objects (RefHB 3.15-45).",
            RefHB: "3.15-43",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 77, 0, 78) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                },
                Selections = {
                    new DefinedExpression
                    {
                        Operand = new PathExpression
                        {
                            Path = {
                                new IdentifierPathElement
                                {
                                    Value = "base",
                                },
                                new IdentifierPathElement
                                {
                                    Value = "Attr",
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "View mandatory constraint",
            "VIEW V PROJECTION OF base~Class; = ATTRIBUTE MANDATORY CONSTRAINT DEFINED (base->Attr); END V;",
            Description: "Views carry consistency constraints just like classes/structures (RefHB 3.15-46).",
            RefHB: "3.15-44",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 92, 0, 93) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                },
                Constraints = {
                    new MandatoryConstraint
                    {
                        NameIndex = 1,
                        Condition = new DefinedExpression
                        {
                            Operand = new PathExpression
                            {
                                Path = {
                                    new IdentifierPathElement
                                    {
                                        Value = "base",
                                    },
                                    new IdentifierPathElement
                                    {
                                        Value = "Attr",
                                    },
                                },
                            },
                        },
                        SourceRange = new RangePosition(0, 45, 0, 87),
                    },
                },
            }));

        yield return Rule(new(
            "View attribute ALL OF",
            "VIEW V PROJECTION OF base~Class; = ATTRIBUTE ALL OF base; END V;",
            Description: "ALL OF Base view attribute, taking over all base attributes (RefHB 3.15-48).",
            RefHB: "3.15-46",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 62, 0, 63) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                },
                AllOfBases = { new Reference<BaseView> { Path = { "base" }, SourceRange = new RangePosition(0, 52, 0, 56) } },
            }));

        yield return Rule(new(
            "View attribute defined as AttributeDef",
            "VIEW V PROJECTION OF base~Class; = ATTRIBUTE NewAttr : TEXT; END V;",
            Description: "View attribute defined as a full AttributeDef (RefHB 3.15-48 AttributeDef alternative).",
            RefHB: "3.15-46",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 65, 0, 66) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                    { "NewAttr", new AttributeDef
                    {
                        Name = "NewAttr",
                        NameLocations = { new RangePosition(0, 45, 0, 52) },
                        TypeDef = new TextType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = 1 },
                            SourceRange = new RangePosition(0, 55, 0, 59),
                        },
                    } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                },
            }));

        yield return Rule(new(
            "View attribute derived by assignment",
            "VIEW V PROJECTION OF base~Class; = ATTRIBUTE NewAttr := base->Attr; END V;",
            Description: "View attribute derived by assignment Attr := Factor (RefHB 3.15-10 / 3.15-48 / 3.15-49).",
            RefHB: "3.15-47",
            Expected: new ViewDef
            {
                Name = "V",
                NameLocations = { new RangePosition(0, 5, 0, 6), new RangePosition(0, 72, 0, 73) },
                Content = {
                    { "base", new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } } },
                    { "NewAttr", new AttributeDef
                    {
                        Name = "NewAttr",
                        NameLocations = { new RangePosition(0, 45, 0, 52) },
                        TypeDef = UndefinedType.Instance,
                        Values = {
                            new PathExpression
                            {
                                Path = {
                                    new IdentifierPathElement
                                    {
                                        Value = "base",
                                    },
                                    new IdentifierPathElement
                                    {
                                        Value = "Attr",
                                    },
                                },
                            },
                        },
                    } },
                },
                Formation = new ProjectionView
                {
                    Source = new BaseView { Name = "base", NameLocations = { new RangePosition(0, 21, 0, 25) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { "Class" }, SourceRange = new RangePosition(0, 26, 0, 31) } },
                },
            }));

        yield return FullFile(new(
            "Join view in a topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        AttrA : TEXT;
                    END ClassA;
                    CLASS ClassB =
                        AttrB : TEXT;
                    END ClassB;

                    VIEW JoinView
                        JOIN OF a~ClassA, b~ClassB;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END JoinView;
                END Topic;
            END Model.
            """,
            Description: "Self-contained JOIN view (both compilers accept) (RefHB 3.15-32).",
            RefHB: "3.15-30",
            AssertOutput: false));

        yield return FullFile(new(
            "Projected attribute is visible in view constraints",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS Flurname =
                        Name : TEXT*20;
                        Geometrie : MANDATORY SURFACE WITH (STRAIGHTS, ARCS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    END Flurname;

                    VIEW Gueltig
                        PROJECTION OF Flurname;
                        =
                        ALL OF Flurname;
                        SET CONSTRAINT CH070501: INTERLIS.areAreas(ALL, UNDEFINED, >> Geometrie);
                    END Gueltig;
                END Topic;
            END Model.
            """,
            Description: """
                The attributes taken over with ALL OF belong to the view's namespace (RefHB 3.5.4-11), so an
                attribute-path constant in a view constraint resolves against the projected base's attributes. This is
                the DMAV pattern (e.g. DMAV_Nomenklatur_V1_0.Flurname_Gueltig: areAreas over >> Geometrie).
                """,
            RefHB: "3.15",
            AssertOutput: false));

        yield return FullFile(new(
            "Attribute taken over with ALL OF is visible through a base view path",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS BaseCover (ABSTRACT) =
                        Geometry : TEXT*20;
                    END BaseCover;
                    CLASS Cover EXTENDS BaseCover =
                    END Cover;

                    VIEW First
                        PROJECTION OF Cover;
                        =
                        ATTRIBUTE
                            ALL OF Cover;
                    END First;

                    VIEW Second
                        PROJECTION OF B ~ First;
                        =
                        ATTRIBUTE
                            Geometry := B -> Geometry;
                    END Second;
                END Topic;
            END Model.
            """,
            Description: """
                A path that descends INTO a view resolves the members the view takes over with ALL OF (RefHB 3.5.4-11),
                including attributes the ALL OF base itself only inherits. This is the RoadsExgm2ien pattern
                (Surface_Boundary2 derives 'Geometry := Base -> Geometry' from Surface_Boundary, which declares ALL OF
                LandCover).
                """,
            RefHB: "3.15",
            AssertOutput: false));

        yield return FullFile(new(
            "Area inspection keywords in an area inspection view are accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS ClassA =
                        Name : TEXT*20;
                        Geom : AREA WITH (STRAIGHTS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    END ClassA;

                    VIEW Boundaries
                        AREA INSPECTION OF a~ClassA -> Geom;
                        =
                        LeftName := THISAREA -> Name;
                        RightName := THATAREA -> Name;
                    END Boundaries;
                END Topic;
            END Model.
            """,
            Description: """
                RefHB 3.13-35/-43: THISAREA/THATAREA are only usable within the inspection of an area partition and
                AGGREGATES only within an aggregation view — enforced at the head of a path (mid-path keywords ride on
                an established context object and are tolerated, as ili2c does).
                """,
            RefHB: "3.13-35",
            AssertOutput: false));

        yield return FullFile(new(
            "Area inspection keyword outside an area inspection is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Dummy = END Dummy;
                    VIEW ExpressionView PROJECTION OF Dummy; =
                        Attr := THISAREA;
                    END ExpressionView;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["'THISAREA' can only be used within the inspection of an area partition (in 'Model.Topic.ExpressionView')"],
            RefHB: "3.13-35",
            AssertOutput: false));

        yield return FullFile(new(
            "AGGREGATES outside an aggregation view is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Dummy = END Dummy;
                    VIEW ExpressionView PROJECTION OF Dummy; =
                        Attr := AGGREGATES;
                    END ExpressionView;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["'AGGREGATES' can only be used within an aggregation view (in 'Model.Topic.ExpressionView')"],
            RefHB: "3.13-43",
            AssertOutput: false));

        yield return FullFile(new(
            "Inspection view in a topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Part =
                    Name : TEXT;
                END Part;
                TOPIC Topic =
                    CLASS ClassA =
                        Parts : BAG OF Part;
                    END ClassA;

                    VIEW PartsView
                        INSPECTION OF a~ClassA -> Parts;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END PartsView;
                END Topic;
            END Model.
            """,
            Description: "Self-contained INSPECTION view over a sub-structure attribute (RefHB 3.15-35).",
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Chained inspection path through nested substructures",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Inner =
                    Val : TEXT;
                END Inner;
                STRUCTURE Outer =
                    Inners : BAG OF Inner;
                END Outer;
                TOPIC Topic =
                    CLASS ClassA =
                        Parts : BAG OF Outer;
                    END ClassA;

                    VIEW InnersView
                        INSPECTION OF a~ClassA -> Parts -> Inners;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END InnersView;
                END Topic;
            END Model.
            """,
            Description: """
                An inspection path descends structure by structure: the first step is a substructure attribute of the
                source viewable, each further step one of the previous step's structure (RefHB 3.15-15).
                """,
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Unknown attribute in an inspection path is reported",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Part =
                    Name : TEXT;
                END Part;
                TOPIC Topic =
                    CLASS ClassA =
                        Parts : BAG OF Part;
                    END ClassA;

                    VIEW PartsView
                        INSPECTION OF a~ClassA -> Nope;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END PartsView;
                END Topic;
            END Model.
            """,
            Description: "Each step of an inspection path must name a member of the previous step's structure (the first of the source viewable).",
            ExpectedLog: ["Could not resolve 'Nope' in 'Model.Topic.ClassA'"],
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Unknown attribute in a chained inspection path is reported",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Outer =
                    Val : TEXT;
                END Outer;
                TOPIC Topic =
                    CLASS ClassA =
                        Parts : BAG OF Outer;
                    END ClassA;

                    VIEW PartsView
                        INSPECTION OF a~ClassA -> Parts -> Nope;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END PartsView;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Could not resolve 'Nope' in 'Model.Outer'"],
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Inspection path can not continue after a scalar attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Part =
                    Name : TEXT;
                END Part;
                TOPIC Topic =
                    CLASS ClassA =
                        Label : TEXT*10;
                        Parts : BAG OF Part;
                    END ClassA;

                    VIEW PartsView
                        INSPECTION OF a~ClassA -> Label -> Parts;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END PartsView;
                END Topic;
            END Model.
            """,
            Description: """
                Only the last step of an inspection path may be a line attribute; every earlier step must be a
                substructure attribute the walk can descend into (RefHB 3.15-15; ili2c: "Path should stop at ...").
                """,
            ExpectedLog: ["the inspection path can not continue after 'Model.Topic.ClassA -> Label' because it is not a substructure attribute"],
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Inspection of a scalar attribute is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                        Label : TEXT*10;
                    END ClassA;

                    VIEW PartsView
                        INSPECTION OF a~ClassA -> Label;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END PartsView;
                END Topic;
            END Model.
            """,
            Description: """
                The inspected attribute must be decomposable: a substructure or a line attribute (RefHB 3.15-15;
                ili2c: "can not decompose ...").
                """,
            ExpectedLog: ["'Model.Topic.ClassA -> Label' can not be inspected because its type is not a substructure or a single polyline, surface or area"],
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Inspection of a polyline attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS ClassA =
                        Geom : POLYLINE WITH (STRAIGHTS) VERTEX CoordD;
                    END ClassA;

                    VIEW LinesView
                        INSPECTION OF a~ClassA -> Geom;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END LinesView;
                END Topic;
            END Model.
            """,
            Description: "A line attribute is inspectable: the inspection yields its curve segments (RefHB 3.15-15).",
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Inspection of a surface attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS ClassA =
                        Geom : SURFACE WITH (STRAIGHTS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    END ClassA;

                    VIEW BoundariesView
                        INSPECTION OF a~ClassA -> Geom;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END BoundariesView;
                END Topic;
            END Model.
            """,
            Description: "A plain INSPECTION of a surface attribute yields the boundaries of the surfaces (RefHB 3.15-16; the Anhang E Surface_Boundary pattern).",
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Chained inspection over a surface inspection view",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS ClassA =
                        Geometry : SURFACE WITH (STRAIGHTS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    END ClassA;

                    VIEW Boundaries
                        INSPECTION OF ClassA -> Geometry;
                        =
                        ATTRIBUTE
                            ALL OF ClassA;
                    END Boundaries;

                    VIEW Edges
                        INSPECTION OF Base ~ Boundaries -> Lines;
                        =
                        ATTRIBUTE
                            Geometry := Base -> Geometry;
                    END Edges;
                END Topic;
            END Model.
            """,
            Description: """
                The Anhang E Surface_Boundary2 pattern: an inspection whose source is itself an inspection view steps
                into the ELEMENTS that view yields — a surface inspection yields the predefined SurfaceBoundary
                structure (RefHB 3.15-16), so '-> Lines' names its LIST OF SurfaceEdge attribute.
                """,
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "AREA INSPECTION of a surface attribute is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS ClassA =
                        Geom : SURFACE WITH (STRAIGHTS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    END ClassA;

                    VIEW BoundariesView
                        AREA INSPECTION OF a~ClassA -> Geom;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END BoundariesView;
                END Topic;
            END Model.
            """,
            Description: """
                An AREA INSPECTION decomposes an area partition, so the inspected attribute must be an AREA
                (RefHB 3.15-17; ili2c: "Area decompositions can only be performed on attributes whose type is an
                area").
                """,
            ExpectedLog: ["'Model.Topic.ClassA -> Geom' can not be inspected by an AREA INSPECTION because its type is not an area partition (AREA)"],
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Inspection of a MULTISURFACE attribute is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS ClassA =
                        Geom : MULTISURFACE WITH (STRAIGHTS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    END ClassA;

                    VIEW BoundariesView
                        INSPECTION OF a~ClassA -> Geom;
                        =
                        ATTRIBUTE
                            ALL OF a;
                    END BoundariesView;
                END Topic;
            END Model.
            """,
            Description: """
                A MULTI... geometry is a single value, not a decomposable collection of structure elements
                (RefHB 3.8.13.3-7), so it can not be inspected — ili2c agrees ("can not decompose ...").
                """,
            ExpectedLog: ["'Model.Topic.ClassA -> Geom' can not be inspected because its type is not a substructure or a single polyline, surface or area"],
            RefHB: "3.15-33",
            AssertOutput: false));

        yield return FullFile(new(
            "Derived association from a join view (worked example)",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CHSurface = (a, b, c);
                FUNCTION Intersect (Surface1: CHSurface; Surface2: CHSurface): BOOLEAN;
                TOPIC Topic =
                    CLASS A =
                        a1: CHSurface;
                    END A;
                    CLASS B =
                        b1: CHSurface;
                    END B;
                    VIEW ABIntersection
                        JOIN OF A, B;
                        WHERE Intersect (A->a1, B->b1);
                        =
                    END ABIntersection;
                    ASSOCIATION IntersectedAB
                        DERIVED FROM ABIntersection =
                        ARole -- A := ABIntersection -> A;
                        BRole -- B := ABIntersection -> B;
                    END IntersectedAB;
                END Topic;
            END Model.
            """,
            Description: """
                Worked example (RefHB 3.15-51..3.15-61): a JOIN view restricted by WHERE used as the basis of a
                DERIVED FROM association. CHSurface simplified to an enumeration domain so the example parses without
                relying on SURFACE geometry detail not central to the view construct.
                """,
            RefHB: "3.15-49",
            AssertOutput: false));

        // --- Base-name ("Basissicht") resolution and object-path checking (RefHB 3.15 / 3.5.4) ---

        yield return FullFile(new(
            "View base name and paths resolve",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        attr : TEXT;
                    END Class;

                    VIEW V
                        PROJECTION OF b~Class;
                        WHERE DEFINED(b -> attr);
                        =
                        ATTRIBUTE
                            ALL OF b;
                    END V;
                END Topic;
            END Model.
            """,
            Description: """
                The base gets a local name (explicit 'b ~' or, when omitted, the viewable's own name). The name is then
                usable to take over attributes (ALL OF) and as the head of object paths (WHERE / derived attributes).
                Both compilers accept.
                """,
            RefHB: "3.15-38",
            AssertOutput: false));

        yield return FullFile(new(
            "View 'ALL OF' references an unknown base",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        attr : TEXT;
                    END Class;

                    VIEW V
                        PROJECTION OF Class;
                        =
                        ATTRIBUTE
                            ALL OF NoSuchBase;
                    END V;
                END Topic;
            END Model.
            """,
            Description: """
                RefHB 3.15: 'ALL OF Base' takes over the attributes of a base, so the name must denote a base of the
                view. (Verified: ili2c rejects with "There is no alias ... for any base of VIEW".)
                """,
            ExpectedLog: ["Could not resolve 'reference 'NoSuchBase' from Model.Topic.V'"],
            RefHB: "3.15-38",
            AssertOutput: false));

        yield return FullFile(new(
            "View object-path head must be a base",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        attr : TEXT;
                    END Class;

                    VIEW V
                        PROJECTION OF Class;
                        =
                        ATTRIBUTE
                            x := other -> attr;
                    END V;
                END Topic;
            END Model.
            """,
            Description: """
                RefHB 3.13/3.15: a view has no implicit "this object" (it may be formed from several bases), so an
                object-path head must denote a base of the view. Here 'other' is neither a base nor a keyword.
                (Verified: ili2c rejects with "Name other is not applicable to VIEW".)
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.V': the path must start with a base of the view, but 'other' is not a base."],
            RefHB: "3.15-38",
            AssertOutput: false));

        yield return FullFile(new(
            "View declares two bases with the same name",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        attr : TEXT;
                    END Class;

                    VIEW V
                        JOIN OF Class, Class;
                        =
                        ATTRIBUTE
                            ALL OF Class;
                    END V;
                END Topic;
            END Model.
            """,
            Description: """
                Base names must be unique within the view. A JOIN over the same class twice without an explicit alias
                yields the same implicit base name 'Class' for both. (Verified: ili2c rejects with "The name ... can not
                be an alias for ... at the same time".)
                """,
            ExpectedLog: ["Compile error at line 8:13 An element with name Class already exists in the scope V."],
            RefHB: "3.5.4",
            AssertOutput: false));

        yield return FullFile(new(
            "View base viewable in another topic requires a dependency",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC TopicA =
                    CLASS Class =
                        attr : TEXT;
                    END Class;
                END TopicA;
                TOPIC TopicB =
                    VIEW V
                        PROJECTION OF Model.TopicA.Class;
                        =
                        ATTRIBUTE
                            ALL OF Class;
                    END V;
                END TopicB;
            END Model.
            """,
            Description: """
                RefHB 3.15: a base viewable in another topic requires a topic dependency (like a cross-topic role or
                reference). Here TopicB's view is formed over TopicA.Class without a DEPENDS ON.
                (Verified: ili2c rejects with "This reference to a viewable requires a topic dependency".)
                """,
            ExpectedLog: ["Type check error in 'Model.TopicB.V': the base viewable 'Class' is in topic 'TopicA' and requires a topic dependency."],
            RefHB: "3.15-38",
            AssertOutput: false));

        yield return FullFile(new(
            "View derived attribute uses an expression, not a factor",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        attr : 0 .. 10;
                    END Class;

                    VIEW V
                        PROJECTION OF Class;
                        =
                        ATTRIBUTE
                            ALL OF Class;
                            newAttr := attr + 1;
                    END V;
                END Topic;
            END Model.
            """,
            Description: """
                RefHB 3.15: a derived view attribute is defined by a single Factor, not a full expression, so the
                arithmetic '+' is a syntax error. Parser recovery leaves 'newAttr := attr', whose head 'attr' is not a
                base either — surfacing the base-name rule as a second, independent diagnostic.
                (Verified: ili2c also rejects at the '+'.)
                """,
            ExpectedLog: [
                "Compile error at line 13:32 mismatched input '+' expecting ';'.",
                "Type check error in 'Model.Topic.V': the path must start with a base of the view, but 'attr' is not a base.",
            ],
            RefHB: "3.15-38",
            AssertOutput: false));

        yield return FullFile(new(
            "View with a plain non-derived attribute",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Class =
                        attr : TEXT;
                    END Class;

                    VIEW V
                        PROJECTION OF Class;
                        =
                        ATTRIBUTE
                            extra : TEXT;
                    END V;
                END Topic;
            END Model.
            """,
            Description: """
                A plain (non-derived) attribute in a view: the RefHB 3.15 grammar admits a bare AttributeDef among the
                ViewAttributes, and both compilers accept it. Kept as a boundary case: our base-name/path checks stay
                silent here (there is no path and no base collision).
                """,
            RefHB: "3.15-38",
            AssertOutput: false));

        yield return FullFile(new(
            "View extending itself",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    VIEW V EXTENDS V =
                        ATTRIBUTE
                    END V;
                END Topic;
            END Model.
            """,
            Description: """
                A circular EXTENDS chain makes the view transitively its own base (ili2c rejects the model too: its
                single-pass name resolution can not even resolve the self-reference).
                """,
            RefHB: "3.15-4",
            ExpectedLog: ["Type check error in 'Model.Topic.V': the view transitively EXTENDS itself."],
            AssertOutput: false));

    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                {fragment}
            END Topic;
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(ViewTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadViewDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitViewDef(p.viewDef()));
    }
}
