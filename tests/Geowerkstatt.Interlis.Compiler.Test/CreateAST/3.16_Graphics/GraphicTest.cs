using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class GraphicTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Graphic based on a viewable",
            "GRAPHIC G BASED ON BaseClass = R : (p := 5); END G;",
            Description: "3.16-9: a graphic BASED ON a viewable.",
            RefHB: "3.16-1",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 49, 0, 50) },
                BasedOn = new Reference<IInterlisDefinition> { Path = { "BaseClass" }, SourceRange = new RangePosition(0, 19, 0, 28) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 31, 0, 44),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Abstract graphic with no drawing rules",
            "GRAPHIC G (ABSTRACT) = END G;",
            Description: "3.16-9: an abstract graphic with no drawing rules.",
            RefHB: "3.16-2",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 27, 0, 28) },
                Properties = { Property.Abstract },
            }));

        yield return FullFile(new(
            "Graphic extension refining a drawing rule",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC SignsTopic =
                    CLASS Textlabel EXTENDS INTERLIS.SIGN =
                        PARAMETER
                            Text: MANDATORY TEXT;
                            Groesse: 0.1 .. 10.0;
                    END Textlabel;
                END SignsTopic;
                TOPIC Topic =
                    CLASS Punkt =
                        PunktName: TEXT*12;
                    END Punkt;
                    GRAPHIC PunktGrafik BASED ON Punkt =
                        Beschriftung OF Model.SignsTopic.Textlabel : (
                            Text := PunktName
                        );
                    END PunktGrafik;
                    GRAPHIC PunktGrafikPlus EXTENDS PunktGrafik =
                        Beschriftung (EXTENDED) : (
                            Groesse := 2
                        );
                    END PunktGrafikPlus;
                END Topic;
            END Model.
            """,
            Description: """
                3.16-3: a graphic extends another graphic (BASED ON omitted, its base is inherited) and refines a
                drawing rule (EXTENDED). The refinement keeps the sign class of the rule it refines, so it omits 'OF
                ...'.
                """,
            RefHB: "3.16-2",
            AssertOutput: false));

        yield return Rule(new(
            "Drawing rule with conditional WHERE assignment",
            "GRAPHIC G = R : WHERE Art == 1 (p := 5); END G;",
            Description: "3.16-13: a conditional signature-parameter assignment guarded by WHERE.",
            RefHB: "3.16-3",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 45, 0, 46) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 40),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Where = new ComparisonExpression
                                {
                                    Operator = ComparisonExpression.ComparisonOperator.Equal,
                                    FirstOperand = new PathExpression
                                    {
                                        Path = { new IdentifierPathElement { Value = "Art" } },
                                    },
                                    SecondOperand = new NumericConstant { Value = 1 },
                                },
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return FullFile(new(
            "Abstract drawing rule without a sign class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Punkt =
                        PunktName: TEXT*12;
                    END Punkt;
                    GRAPHIC PunktGrafik BASED ON Punkt =
                        Beschriftung (ABSTRACT) : (
                            Text := PunktName
                        );
                    END PunktGrafik;
                END Topic;
            END Model.
            """,
            Description: """
                3.16-10: an ABSTRACT drawing rule may omit its sign class ('OF ...'), because the sign class must only
                be defined once a drawing rule is concrete. ili2c demands 'OF' on an abstract rule as well.
                """,
            RefHB: "3.16-4",
            Ili2cDivergenceReason: "RefHB 3.16-4 requires the sign class only once a drawing rule is concrete ('Sobald Zeichnungsregeln konkret sind'), so an ABSTRACT rule may omit 'OF ...'. ili2c rejects it regardless of the ABSTRACT property.",
            AssertOutput: false));

        yield return Rule(new(
            "Graphic with selection",
            "GRAPHIC G BASED ON BaseClass = WHERE Attr == 5; R : (p := 5); END G;",
            Description: "3.16-9: a graphic-level selection (WHERE) restricting the objects the graphic applies to.",
            RefHB: "3.16-8",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 66, 0, 67) },
                BasedOn = new Reference<IInterlisDefinition> { Path = { "BaseClass" }, SourceRange = new RangePosition(0, 19, 0, 28) },
                Selections =
                {
                    new ComparisonExpression
                    {
                        Operator = ComparisonExpression.ComparisonOperator.Equal,
                        FirstOperand = new PathExpression
                        {
                            Path = { new IdentifierPathElement { Value = "Attr" } },
                        },
                        SecondOperand = new NumericConstant { Value = 5 },
                    },
                },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 48, 0, 61),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Final graphic",
            "GRAPHIC G (FINAL) = R : (p := 5); END G;",
            Description: "3.16-9: a FINAL graphic.",
            RefHB: "3.16-8",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 38, 0, 39) },
                Properties = { Property.Final },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 20, 0, 33),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Graphic extends graphic by name",
            "GRAPHIC G EXTENDS BaseG = R : (p := 5); END G;",
            Description: "3.16-9 / 3.16-11: a graphic that EXTENDS another graphic referenced by its simple name.",
            RefHB: "3.16-8",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 44, 0, 45) },
                Extends = new Reference<GraphicDef> { Path = { "BaseG" }, SourceRange = new RangePosition(0, 18, 0, 23) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 26, 0, 39),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Graphic extends qualified graphic reference",
            "GRAPHIC G EXTENDS ModelN.TopicN.BaseG = R : (p := 5); END G;",
            Description: "3.16-11: a qualified GraphicRef (Model-Name '.' Topic-Name '.' Graphic-Name) after EXTENDS.",
            RefHB: "3.16-9",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 58, 0, 59) },
                Extends = new Reference<GraphicDef> { Path = { "ModelN", "TopicN", "BaseG" }, SourceRange = new RangePosition(0, 18, 0, 37) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 40, 0, 53),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Graphic with drawing rule",
            "GRAPHIC G = R : (p := 5); END G;",
            Description: """
                The comparison diverges: a graphic references a SIGN class and its parameters, which ili2c validates
                against its predefined SIGN / signature model (not modelled here). The AST is verified by the rule-level
                ReadGraphicDef test; the comparison is left red as a known compiler-gap signal.
                """,
            RefHB: "3.16-10",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 30, 0, 31) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 25),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Drawing rule OF sign class",
            "GRAPHIC G = R OF SignClass : (p := 5); END G;",
            Description: "3.16-12: a drawing rule selecting a sign class via OF Sign-ClassRef.",
            RefHB: "3.16-10",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 43, 0, 44) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 38),
                        Sign = new Reference<IInterlisDefinition> { Path = { "SignClass" }, SourceRange = new RangePosition(0, 17, 0, 26) },
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Extended drawing rule",
            "GRAPHIC G = R (EXTENDED) OF SignClass : (p := 5); END G;",
            Description: "3.16-12: an EXTENDED drawing rule.",
            RefHB: "3.16-10",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 54, 0, 55) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 49),
                        Properties = { Property.Extended },
                        Sign = new Reference<IInterlisDefinition> { Path = { "SignClass" }, SourceRange = new RangePosition(0, 28, 0, 37) },
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Abstract drawing rule",
            "GRAPHIC G = R (ABSTRACT) : (p := 5); END G;",
            Description: "3.16-12: an ABSTRACT drawing rule.",
            RefHB: "3.16-10",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 41, 0, 42) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 36),
                        Properties = { Property.Abstract },
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Graphic with multiple drawing rules",
            "GRAPHIC G = R1 : (p := 5); R2 : (q := 6); END G;",
            Description: "3.16-12: multiple drawing rules in one graphic.",
            RefHB: "3.16-10",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 46, 0, 47) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R1",
                        SourceRange = new RangePosition(0, 12, 0, 26),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                        },
                    },
                    new DrawingRule
                    {
                        Name = "R2",
                        SourceRange = new RangePosition(0, 27, 0, 41),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "q", Value = new NumericConstant { Value = 6 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Drawing rule with multiple conditional assignment blocks",
            "GRAPHIC G = R : (p := 5), (q := 6); END G;",
            Description: "3.16-12: a drawing rule with several comma-separated conditional assignment blocks.",
            RefHB: "3.16-10",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 40, 0, 41) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 35),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                },
                            },
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "q", Value = new NumericConstant { Value = 6 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Conditional block with multiple parameter assignments",
            "GRAPHIC G = R : (p := 5; q := 6); END G;",
            Description: "3.16-14: several semicolon-separated SignParamAssignments in one block.",
            RefHB: "3.16-11",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 38, 0, 39) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 33),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "p", Value = new NumericConstant { Value = 5 } },
                                    new SignParamAssignment { ParameterName = "q", Value = new NumericConstant { Value = 6 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Parameter assigned a meta-object reference",
            "GRAPHIC G = R : (Sign := {Punktsignatur}); END G;",
            Description: "3.16-15: a parameter assigned a meta-object reference ({ MetaObjectRef }).",
            RefHB: "3.16-12",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 47, 0, 48) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 42),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Sign",
                                        MetaObject = new Reference<IInterlisDefinition> { Path = { "Punktsignatur" }, SourceRange = new RangePosition(0, 26, 0, 39) },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Parameter assigned an attribute factor",
            "GRAPHIC G = R : (Pos := Lage); END G;",
            Description: "3.16-15: a parameter assigned a Factor referencing a base-class attribute.",
            RefHB: "3.16-12",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 35, 0, 36) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 30),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Pos",
                                        Value = new PathExpression
                                        {
                                            Path = { new IdentifierPathElement { Value = "Lage" } },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Parameter assigned ACCORDING with single enum node",
            "GRAPHIC G = R : (Sign := ACCORDING Art ({Quadratsignatur} WHEN IN #Stein)); END G;",
            Description: "3.16-15 / 3.16-17: an ACCORDING assignment with a meta-object for a single enumeration node.",
            RefHB: "3.16-12",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 80, 0, 81) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 75),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Sign",
                                        According = new PathExpression
                                        {
                                            Path = { new IdentifierPathElement { Value = "Art" } },
                                        },
                                        EnumAssignments =
                                        {
                                            new EnumAssignment
                                            {
                                                MetaObject = new Reference<IInterlisDefinition> { Path = { "Quadratsignatur" }, SourceRange = new RangePosition(0, 41, 0, 56) },
                                                RangeFrom = new EnumerationConstant { Path = { "Stein" } },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "ACCORDING with multiple constant enum assignments",
            "GRAPHIC G = R : (Sign := ACCORDING Art (5 WHEN IN #Stein, 6 WHEN IN #Bolzen)); END G;",
            Description: "3.16-16: an ACCORDING assignment with several constant-valued enum assignments.",
            RefHB: "3.16-13",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 83, 0, 84) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 78),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Sign",
                                        According = new PathExpression
                                        {
                                            Path = { new IdentifierPathElement { Value = "Art" } },
                                        },
                                        EnumAssignments =
                                        {
                                            new EnumAssignment
                                            {
                                                Value = new NumericConstant { Value = 5 },
                                                RangeFrom = new EnumerationConstant { Path = { "Stein" } },
                                            },
                                            new EnumAssignment
                                            {
                                                Value = new NumericConstant { Value = 6 },
                                                RangeFrom = new EnumerationConstant { Path = { "Bolzen" } },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "ACCORDING with enum range interval",
            "GRAPHIC G = R : (Sign := ACCORDING Art ({Kreuzsignatur} WHEN IN #Rohr .. #Kreuz)); END G;",
            Description: "3.16-17: an EnumRange interval (EnumerationConst '..' EnumerationConst).",
            RefHB: "3.16-14",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 87, 0, 88) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 82),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Sign",
                                        According = new PathExpression
                                        {
                                            Path = { new IdentifierPathElement { Value = "Art" } },
                                        },
                                        EnumAssignments =
                                        {
                                            new EnumAssignment
                                            {
                                                MetaObject = new Reference<IInterlisDefinition> { Path = { "Kreuzsignatur" }, SourceRange = new RangePosition(0, 41, 0, 54) },
                                                RangeFrom = new EnumerationConstant { Path = { "Rohr" } },
                                                RangeTo = new EnumerationConstant { Path = { "Kreuz" } },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Conditional assignment on qualified enum node",
            "GRAPHIC G = R : WHERE Art == #Stein.klein (ScaleFactor := 0.5); END G;",
            Description: "3.16-72 / 3.16-73: WHERE comparing an attribute against a qualified enumeration node, with a decimal value.",
            RefHB: "3.16-70",
            Expected: new GraphicDef
            {
                Name = "G",
                NameLocations = { new RangePosition(0, 8, 0, 9), new RangePosition(0, 68, 0, 69) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "R",
                        SourceRange = new RangePosition(0, 12, 0, 63),
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Where = new ComparisonExpression
                                {
                                    Operator = ComparisonExpression.ComparisonOperator.Equal,
                                    FirstOperand = new PathExpression
                                    {
                                        Path = { new IdentifierPathElement { Value = "Art" } },
                                    },
                                    SecondOperand = new EnumerationConstant { Path = { "Stein", "klein" } },
                                },
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "ScaleFactor", Value = new NumericConstant { Value = 0.5 } },
                                },
                            },
                        },
                    },
                },
            }));

        yield return FullFile(new(
            "Graphic end-name mismatch is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    GRAPHIC G =
                        R : (p := 5);
                    END WrongName;
                END Topic;
            END Model.
            """,
            Description: "3.16-10: a graphic whose END name does not match its start name is rejected.",
            ExpectedLog:
            [
                "Compile error at line 6:12 Start name 'G' and end name 'WrongName' do not match.",
                "Type check error in 'Model.Topic.G': must be BASED ON a class or view, or EXTEND a graphic to inherit its base.",
                "Type check error in 'Model.Topic.G': the drawing rule 'R' must specify the class of the graphic signatures it assigns ('OF ...').",
            ],
            RefHB: "3.16-8",
            AssertOutput: false));

        yield return FullFile(new(
            "Worked example graphic description compiles",
            """
            INTERLIS 2.4;

            SYMBOLOGY MODEL SimpleSignsSymbology AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    S_COORD2 (ABSTRACT) = COORD NUMERIC, NUMERIC;
                TOPIC SignsTopic =
                    CLASS Symbol EXTENDS INTERLIS.SIGN =
                        PARAMETER
                            Pos: MANDATORY S_COORD2;
                    END Symbol;
                END SignsTopic;
            END SimpleSignsSymbology.

            MODEL DatenModell AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    LKoord = COORD 0.000 .. 200.000 [INTERLIS.m], 0.000 .. 200.000 [INTERLIS.m], ROTATION 2 -> 1;
                TOPIC PunktThema =
                    CLASS Punkt =
                        Lage: LKoord;
                        PunktName: TEXT*12;
                    END Punkt;
                END PunktThema;
            END DatenModell.

            MODEL SimpleGrafik AT "http://example.com" VERSION "1.0.0" =
                IMPORTS DatenModell;
                IMPORTS SimpleSignsSymbology;
                SIGN BASKET SimpleSignsBasket ~ SimpleSignsSymbology.SignsTopic
                    OBJECTS OF Symbol: Punktsignatur;
                TOPIC PunktGrafikenThema =
                    DEPENDS ON DatenModell.PunktThema;
                    GRAPHIC SimplePunktGrafik BASED ON DatenModell.PunktThema.Punkt =
                        Symbol OF SimpleSignsSymbology.SignsTopic.Symbol: (
                            Sign := {Punktsignatur};
                            Pos := Lage
                        );
                    END SimplePunktGrafik;
                END PunktGrafikenThema;
            END SimpleGrafik.
            """,
            Description: """
                3.16-21 .. 3.16-49: the complete worked example of the reference handbook, as a graphic description that
                BOTH compilers accept: a symbology model whose sign class extends the predefined INTERLIS.SIGN and declares
                signature parameters (3.16-16 / 3.16-24), a data model, and a graphic model that imports both, declares the
                signature library the metaobjects live in (SIGN BASKET ... OBJECTS OF, 3.16-44) and draws every Punkt with
                one drawing rule naming its sign class ('OF ...') plus a metaobject reference and an attribute factor
                (3.16-47). Everything a concrete graphic needs is present, so it must compile without a diagnostic.
                """,
            RefHB: "3.16-44",
            AssertOutput: false));

        yield return FullFile(new(
            "Graphic based on a same-model class resolves",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC SignsTopic =
                    CLASS Textlabel EXTENDS INTERLIS.SIGN =
                        PARAMETER
                            Text: MANDATORY TEXT;
                    END Textlabel;
                END SignsTopic;
                TOPIC Topic =
                    CLASS Punkt =
                        PunktName: TEXT*12;
                    END Punkt;
                    GRAPHIC PunktGrafik BASED ON Punkt =
                        Beschriftung OF Model.SignsTopic.Textlabel : (
                            Text := PunktName
                        );
                    END PunktGrafik;
                END Topic;
            END Model.
            """,
            Description: """
                3.16-49: a complete graphic BASED ON a class in the same model, naming the sign class of its drawing rule
                ('OF ...', a class extending the predefined INTERLIS.SIGN) and assigning a base-class attribute (Factor).
                """,
            RefHB: "3.16-46",
            AssertOutput: false));

        yield return Rule(new(
            "Worked example SimplePunktGrafik drawing rule",
            "GRAPHIC SimplePunktGrafik BASED ON Punkt =\n  Symbol OF SimpleSignsSymbology.SignsTopic.Symbol: ( Sign := {Punktsignatur}; Pos := Lage );\nEND SimplePunktGrafik;",
            Description: """
                3.16-50 / 3.16-51: the worked-example SimplePunktGrafik drawing rule selecting a qualified sign class
                (OF Model.Topic.Class) and assigning a meta-object reference (Sign) plus a base-class attribute factor (Pos).
                """,
            RefHB: "3.16-47",
            Expected: new GraphicDef
            {
                Name = "SimplePunktGrafik",
                NameLocations = { new RangePosition(0, 8, 0, 25), new RangePosition(2, 4, 2, 21) },
                BasedOn = new Reference<IInterlisDefinition> { Path = { "Punkt" }, SourceRange = new RangePosition(0, 35, 0, 40) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "Symbol",
                        SourceRange = new RangePosition(1, 2, 1, 93),
                        Sign = new Reference<IInterlisDefinition> { Path = { "SimpleSignsSymbology", "SignsTopic", "Symbol" }, SourceRange = new RangePosition(1, 12, 1, 50) },
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Sign",
                                        MetaObject = new Reference<IInterlisDefinition> { Path = { "Punktsignatur" }, SourceRange = new RangePosition(1, 63, 1, 76) },
                                    },
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Pos",
                                        Value = new PathExpression
                                        {
                                            Path = { new IdentifierPathElement { Value = "Lage" } },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Worked example GrafikPlus PunktGrafikPlus graphic",
            "GRAPHIC PunktGrafikPlus EXTENDS SimplePunktGrafik =\n  Symbol (EXTENDED) OF ScalableSignsSymbology.ScalableSignsTopic.Symbol: ( Sign := ACCORDING Art ( {Quadratsignatur} WHEN IN #Stein, {Kreissignatur} WHEN IN #Bolzen, {Kreuzsignatur} WHEN IN #Rohr .. #Kreuz ) ), WHERE Art == #Stein.klein (\n    ScaleFactor := 0.5 );\n  Text OF SimpleSignsSymbology.SignsTopic.Textlabel: ( Sign := {Schrift1}; Pos := Lage; Text := PunktName );\nEND PunktGrafikPlus;",
            Description: """
                3.16-70 .. 3.16-76: the worked-example GrafikPlus graphic PunktGrafikPlus. It EXTENDS a graphic, has an
                EXTENDED drawing rule selecting a qualified sign class, an ACCORDING assignment over an enumeration
                (single nodes plus an ordered range), a comma-separated WHERE conditional block (ScaleFactor), and a
                second drawing rule (Text) with three parameter assignments.
                """,
            RefHB: "3.16-67",
            Expected: new GraphicDef
            {
                Name = "PunktGrafikPlus",
                NameLocations = { new RangePosition(0, 8, 0, 23), new RangePosition(4, 4, 4, 19) },
                Extends = new Reference<GraphicDef> { Path = { "SimplePunktGrafik" }, SourceRange = new RangePosition(0, 32, 0, 49) },
                DrawingRules =
                {
                    new DrawingRule
                    {
                        Name = "Symbol",
                        SourceRange = new RangePosition(1, 2, 2, 25),
                        Properties = { Property.Extended },
                        Sign = new Reference<IInterlisDefinition> { Path = { "ScalableSignsSymbology", "ScalableSignsTopic", "Symbol" }, SourceRange = new RangePosition(1, 23, 1, 71) },
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Sign",
                                        According = new PathExpression
                                        {
                                            Path = { new IdentifierPathElement { Value = "Art" } },
                                        },
                                        EnumAssignments =
                                        {
                                            new EnumAssignment
                                            {
                                                MetaObject = new Reference<IInterlisDefinition> { Path = { "Quadratsignatur" }, SourceRange = new RangePosition(1, 100, 1, 115) },
                                                RangeFrom = new EnumerationConstant { Path = { "Stein" } },
                                            },
                                            new EnumAssignment
                                            {
                                                MetaObject = new Reference<IInterlisDefinition> { Path = { "Kreissignatur" }, SourceRange = new RangePosition(1, 134, 1, 147) },
                                                RangeFrom = new EnumerationConstant { Path = { "Bolzen" } },
                                            },
                                            new EnumAssignment
                                            {
                                                MetaObject = new Reference<IInterlisDefinition> { Path = { "Kreuzsignatur" }, SourceRange = new RangePosition(1, 167, 1, 180) },
                                                RangeFrom = new EnumerationConstant { Path = { "Rohr" } },
                                                RangeTo = new EnumerationConstant { Path = { "Kreuz" } },
                                            },
                                        },
                                    },
                                },
                            },
                            new CondSignParamAssignment
                            {
                                Where = new ComparisonExpression
                                {
                                    Operator = ComparisonExpression.ComparisonOperator.Equal,
                                    FirstOperand = new PathExpression
                                    {
                                        Path = { new IdentifierPathElement { Value = "Art" } },
                                    },
                                    SecondOperand = new EnumerationConstant { Path = { "Stein", "klein" } },
                                },
                                Assignments =
                                {
                                    new SignParamAssignment { ParameterName = "ScaleFactor", Value = new NumericConstant { Value = 0.5 } },
                                },
                            },
                        },
                    },
                    new DrawingRule
                    {
                        Name = "Text",
                        SourceRange = new RangePosition(3, 2, 3, 108),
                        Sign = new Reference<IInterlisDefinition> { Path = { "SimpleSignsSymbology", "SignsTopic", "Textlabel" }, SourceRange = new RangePosition(3, 10, 3, 51) },
                        Assignments =
                        {
                            new CondSignParamAssignment
                            {
                                Assignments =
                                {
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Sign",
                                        MetaObject = new Reference<IInterlisDefinition> { Path = { "Schrift1" }, SourceRange = new RangePosition(3, 64, 3, 72) },
                                    },
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Pos",
                                        Value = new PathExpression
                                        {
                                            Path = { new IdentifierPathElement { Value = "Lage" } },
                                        },
                                    },
                                    new SignParamAssignment
                                    {
                                        ParameterName = "Text",
                                        Value = new PathExpression
                                        {
                                            Path = { new IdentifierPathElement { Value = "PunktName" } },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            }));
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
        => ComparisonRows(nameof(GraphicTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadGraphicDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitGraphicDef(p.graphicDef()));
    }
}
