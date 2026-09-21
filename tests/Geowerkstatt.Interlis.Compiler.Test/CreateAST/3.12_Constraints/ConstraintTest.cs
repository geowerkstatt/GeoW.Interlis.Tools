using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class ConstraintTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Set constraint worked example with WHERE and areAreas",
            "SET CONSTRAINT WHERE Art == #a: areAreas(ALL, UNDEFINED, >> Geometrie);",
            RefHB: "3.12-20",
            Expected: new SetConstraint
            {
                Where = new ComparisonExpression
                {
                    Operator = ComparisonExpression.ComparisonOperator.Equal,
                    FirstOperand = new PathExpression
                    {
                        Path = {
                            new IdentifierPathElement
                            {
                                Value = "Art",
                            },
                        },
                    },
                    SecondOperand = new EnumerationConstant
                    {
                        Path = { "a" },
                    },
                },
                Condition = new FunctionCall
                {
                    FunctionDef = new Reference<FunctionDef> { Path = { "areAreas" }, SourceRange = new RangePosition(0, 32, 0, 40) },
                    Arguments = {
                        new AllExpression
                        {
                        },
                        new UndefinedConstant
                        {
                        },
                        new AttributePathConstant
                        {
                            Attribute = new Reference<AttributeDef> { Path = { "Geometrie" }, SourceRange = new RangePosition(0, 60, 0, 69) },
                        },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 71),
            }));

        yield return Rule(new(
            "Set constraint worked example with reference path argument",
            "SET CONSTRAINT areAreas(ALL, >> Flaechen, >> F->Geometrie);",
            RefHB: "3.12-25",
            Expected: new SetConstraint
            {
                Condition = new FunctionCall
                {
                    FunctionDef = new Reference<FunctionDef> { Path = { "areAreas" }, SourceRange = new RangePosition(0, 15, 0, 23) },
                    Arguments = {
                        new AllExpression
                        {
                        },
                        new AttributePathConstant
                        {
                            Attribute = new Reference<AttributeDef> { Path = { "Flaechen" }, SourceRange = new RangePosition(0, 32, 0, 40) },
                        },
                        new AttributePathConstant
                        {
                            Attribute = new Reference<AttributeDef> { Path = { "F", "Geometrie" }, SourceRange = new RangePosition(0, 45, 0, 57) },
                        },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 59),
            }));

        yield return Rule(new(
            "Mandatory constraint",
            "MANDATORY CONSTRAINT Check: 5 > 0;",
            RefHB: "3.12-32",
            Expected: new MandatoryConstraint
            {
                Name = "Check",
                Condition = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 5 }, SecondOperand = new NumericConstant { Value = 0 } },
                SourceRange = new RangePosition(0, 0, 0, 34),
            }));

        yield return Rule(new(
            "Unnamed mandatory constraint",
            "MANDATORY CONSTRAINT 5 > 0;",
            RefHB: "3.12-32",
            Expected: new MandatoryConstraint
            {
                Condition = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 5 }, SecondOperand = new NumericConstant { Value = 0 } },
                SourceRange = new RangePosition(0, 0, 0, 27),
            }));

        yield return FullFile(new(
            "Constraints inside a class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS C =
                        Value : 0 .. 100;
                        MANDATORY CONSTRAINT Value > 0;
                        UNIQUE Value;
                    END C;
                END Topic;
            END Model.
            """,
            RefHB: "3.12-32",
            AssertOutput: false));

        yield return Rule(new(
            "Plausibility constraint",
            "CONSTRAINT >= 80% 5 > 0;",
            RefHB: "3.12-33",
            Expected: new PlausibilityConstraint
            {
                Direction = PlausibilityConstraint.Comparison.AtLeast,
                Percentage = 80,
                Condition = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 5 }, SecondOperand = new NumericConstant { Value = 0 } },
                SourceRange = new RangePosition(0, 0, 0, 24),
            }));

        yield return Rule(new(
            "Plausibility constraint at most named",
            "CONSTRAINT Check: <= 80% 5 > 0;",
            RefHB: "3.12-33",
            Expected: new PlausibilityConstraint
            {
                Direction = PlausibilityConstraint.Comparison.AtMost,
                Percentage = 80,
                Condition = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 5 }, SecondOperand = new NumericConstant { Value = 0 } },
                Name = "Check",
                SourceRange = new RangePosition(0, 0, 0, 31),
            }));

        yield return Rule(new(
            "Existence constraint",
            "EXISTENCE CONSTRAINT a REQUIRED IN Other: b;",
            RefHB: "3.12-34",
            Expected: new ExistenceConstraint
            {
                AttributePath = new PathExpression { Path = { new IdentifierPathElement { Value = "a" } } },
                RequiredIn =
                {
                    new ExistenceRequirement
                    {
                        Viewable = new Reference<IInterlisDefinition> { Path = { "Other" }, SourceRange = new RangePosition(0, 35, 0, 40) },
                        AttributePath = new PathExpression { Path = { new IdentifierPathElement { Value = "b" } } },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 44),
            }));

        yield return Rule(new(
            "Existence constraint with OR alternatives",
            "EXISTENCE CONSTRAINT a REQUIRED IN Other: b OR Third: c;",
            RefHB: "3.12-34",
            Expected: new ExistenceConstraint
            {
                AttributePath = new PathExpression { Path = { new IdentifierPathElement { Value = "a" } } },
                RequiredIn =
                {
                    new ExistenceRequirement
                    {
                        Viewable = new Reference<IInterlisDefinition> { Path = { "Other" }, SourceRange = new RangePosition(0, 35, 0, 40) },
                        AttributePath = new PathExpression { Path = { new IdentifierPathElement { Value = "b" } } },
                    },
                    new ExistenceRequirement
                    {
                        Viewable = new Reference<IInterlisDefinition> { Path = { "Third" }, SourceRange = new RangePosition(0, 47, 0, 52) },
                        AttributePath = new PathExpression { Path = { new IdentifierPathElement { Value = "c" } } },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 56),
            }));

        yield return Rule(new(
            "Global uniqueness constraint",
            "UNIQUE a, b;",
            RefHB: "3.12-35",
            Expected: new UniquenessConstraint
            {
                GlobalUnique =
                {
                    new PathExpression { Path = { new IdentifierPathElement { Value = "a" } } },
                    new PathExpression { Path = { new IdentifierPathElement { Value = "b" } } },
                },
                SourceRange = new RangePosition(0, 0, 0, 12),
            }));

        yield return Rule(new(
            "Named global uniqueness constraint",
            "UNIQUE Check: a, b;",
            RefHB: "3.12-35",
            Expected: new UniquenessConstraint
            {
                GlobalUnique =
                {
                    new PathExpression { Path = { new IdentifierPathElement { Value = "a" } } },
                    new PathExpression { Path = { new IdentifierPathElement { Value = "b" } } },
                },
                Name = "Check",
                SourceRange = new RangePosition(0, 0, 0, 19),
            }));

        yield return Rule(new(
            "Basket uniqueness constraint",
            "UNIQUE (BASKET) a, b;",
            RefHB: "3.12-35",
            Expected: new UniquenessConstraint
            {
                IsBasket = true,
                GlobalUnique =
                {
                    new PathExpression { Path = { new IdentifierPathElement { Value = "a" } } },
                    new PathExpression { Path = { new IdentifierPathElement { Value = "b" } } },
                },
                SourceRange = new RangePosition(0, 0, 0, 21),
            }));

        yield return FullFile(new(
            "Uniqueness constraint worked example",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS A =
                        K: (a, b, c);
                        ID: TEXT*10;
                        UNIQUE K, ID;
                    END A;
                END Topic;
            END Model.
            """,
            RefHB: "3.12-37",
            AssertOutput: false));

        yield return Rule(new(
            "Uniqueness over object attribute path",
            "UNIQUE a -> b;",
            RefHB: "3.12-37",
            Expected: new UniquenessConstraint
            {
                GlobalUnique =
                {
                    new PathExpression
                    {
                        Path =
                        {
                            new IdentifierPathElement { Value = "a" },
                            new IdentifierPathElement { Value = "b" },
                        },
                    },
                },
                SourceRange = new RangePosition(0, 0, 0, 14),
            }));

        yield return FullFile(new(
            "Local uniqueness over defined structure members is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Sub =
                    a : TEXT*10;
                    b : TEXT*10;
                END Sub;
                TOPIC Topic =
                    CLASS ClassName =
                        sub : BAG {0..*} OF Sub;
                        UNIQUE (LOCAL) sub: a, b;
                    END ClassName;
                END Topic;
            END Model.
            """,
            Description: """
                The (LOCAL) structure path and attribute names resolve like path members: the path leads through
                substructure attributes of the class, the attributes are members of the reached structure. Unknown names
                are reported (ili2c rejects them too); the bare rule cases 'Local uniqueness constraint' and 'Local
                uniqueness over nested structure path' stay unresolved fragments whose wrapped comparisons agree on the
                rejection.
                """,
            RefHB: "3.12-38",
            AssertOutput: false));

        yield return FullFile(new(
            "Attribute path constant in a CONSTRAINTS OF block resolves against the target",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    CoordD = COORD 0.000 .. 9.000, 0.000 .. 9.000;
                TOPIC Topic =
                    CLASS ClassName =
                        Geom : SURFACE WITH (STRAIGHTS) VERTEX CoordD WITHOUT OVERLAPS > 0.005;
                    END ClassName;
                    CONSTRAINTS OF ClassName =
                        SET CONSTRAINT INTERLIS.areAreas(ALL, UNDEFINED, >> Geom);
                    END;
                END Topic;
            END Model.
            """,
            Description: """
                The constraints of a CONSTRAINTS OF block belong to the referenced viewable, so an attribute-path
                constant ('>> attr') in such a block resolves against the target class's members — real-world case:
                Nutzungsplanung_NWOW_V2 attaches areAreas(ALL, UNDEFINED, >> Geometrie) to a class via CONSTRAINTS OF.
                """,
            RefHB: "3.12-41",
            AssertOutput: false));

        yield return FullFile(new(
            "Uniqueness path through a collection attribute is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Sub =
                    a : TEXT*10;
                END Sub;
                TOPIC Topic =
                    CLASS ClassName =
                        sub : BAG {0..*} OF Sub;
                        UNIQUE sub -> a;
                    END ClassName;
                END Topic;
            END Model.
            """,
            Description: """
                RefHB 3.12: every element of a UNIQUE attribute path must contribute a single value — the path may not
                lead through a collection attribute or a role reaching several links (ili2c agrees: "unexpected
                cardinality of attribute ..."; real-world case: AdministrativeUnits_V2.CountryNamesTranslation declares
                UNIQUE Entries->Code over a LIST). The (LOCAL) form exists for exactly that — its structure path is
                exempt (see 'Local uniqueness over defined structure members is accepted').
                """,
            ExpectedLog: ["The attribute 'Model.Topic.ClassName -> sub' at 9:19-9:27 can not be used in a UNIQUE constraint because its maximum cardinality is above 1"],
            RefHB: "3.12-15",
            AssertOutput: false));

        yield return FullFile(new(
            "Uniqueness path through an unbounded role is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Owner =
                        Name : TEXT*10;
                    END Owner;
                    CLASS Item =
                    END Item;
                    ASSOCIATION Ownership =
                        ItemRole -- {0..*} Item;
                        OwnerRole -- {0..*} Owner;
                    END Ownership;
                    CONSTRAINTS OF Item =
                        UNIQUE OwnerRole -> Name;
                    END;
                END Topic;
            END Model.
            """,
            Description: """
                The role cases declare the UNIQUE in a CONSTRAINTS OF block after the association: ili2c binds a
                class's association accesses when the ASSOCIATION is parsed, so a class-inline UNIQUE over a role of a
                later association fails in ili2c with "not applicable" before its cardinality rule is even evaluated.
                """,
            ExpectedLog: ["The role 'Model.Topic.Ownership -> OwnerRole' at 14:19-14:36 can not be used in a UNIQUE constraint because its maximum cardinality is above 1"],
            RefHB: "3.12-15",
            AssertOutput: false));

        yield return FullFile(new(
            "Uniqueness path through a single-target role is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Owner =
                        Name : TEXT*10;
                    END Owner;
                    CLASS Item =
                    END Item;
                    ASSOCIATION Ownership =
                        ItemRole -- {0..*} Item;
                        OwnerRole -- {0..1} Owner;
                    END Ownership;
                    CONSTRAINTS OF Item =
                        UNIQUE OwnerRole -> Name;
                    END;
                END Topic;
            END Model.
            """,
            RefHB: "3.12-15",
            AssertOutput: false));

        yield return FullFile(new(
            "Uniqueness over the association's own roles is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS Owner = END Owner;
                    CLASS Item = END Item;
                    ASSOCIATION Ownership =
                        ItemRole -- {0..*} Item;
                        OwnerRole -- {0..*} Owner;
                        UNIQUE ItemRole, OwnerRole;
                    END Ownership;
                END Topic;
            END Model.
            """,
            Description: """
                The roles OF the constrained association itself are exempt: a link instance holds exactly one target
                per role, so UNIQUE over the own roles is the idiomatic identifying constraint.
                """,
            RefHB: "3.12-15",
            AssertOutput: false));

        yield return FullFile(new(
            "Local uniqueness over an unknown structure attribute is reported",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassName =
                        UNIQUE (LOCAL) sub: a, b;
                    END ClassName;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Could not resolve 'sub' in 'Model.Topic.ClassName' at 5:27-5:30"],
            RefHB: "3.12-38",
            AssertOutput: false));

        yield return Rule(new(
            "Local uniqueness constraint",
            "UNIQUE (LOCAL) sub: a, b;",
            RefHB: "3.12-38",
            Expected: new UniquenessConstraint
            {
                Local = new LocalUniqueness
                {
                    StructurePath = { new Reference<AttributeDef> { Path = { "sub" }, SourceRange = new RangePosition(0, 15, 0, 18) } },
                    AttributeNames = { new Reference<AttributeDef> { Path = { "a" }, SourceRange = new RangePosition(0, 20, 0, 21) }, new Reference<AttributeDef> { Path = { "b" }, SourceRange = new RangePosition(0, 23, 0, 24) } },
                },
                SourceRange = new RangePosition(0, 0, 0, 25),
            }));

        yield return Rule(new(
            "Local uniqueness over nested structure path",
            "UNIQUE (LOCAL) sub -> deeper: a, b;",
            RefHB: "3.12-38",
            Expected: new UniquenessConstraint
            {
                Local = new LocalUniqueness
                {
                    StructurePath = { new Reference<AttributeDef> { Path = { "sub" }, SourceRange = new RangePosition(0, 15, 0, 18) }, new Reference<AttributeDef> { Path = { "deeper" }, SourceRange = new RangePosition(0, 22, 0, 28) } },
                    AttributeNames = { new Reference<AttributeDef> { Path = { "a" }, SourceRange = new RangePosition(0, 30, 0, 31) }, new Reference<AttributeDef> { Path = { "b" }, SourceRange = new RangePosition(0, 33, 0, 34) } },
                },
                SourceRange = new RangePosition(0, 0, 0, 35),
            }));

        yield return Rule(new(
            "Set constraint",
            "SET CONSTRAINT WHERE 5 > 0: 1 > 0;",
            RefHB: "3.12-39",
            Expected: new SetConstraint
            {
                Where = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 5 }, SecondOperand = new NumericConstant { Value = 0 } },
                Condition = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 0 } },
                SourceRange = new RangePosition(0, 0, 0, 34),
            }));

        yield return Rule(new(
            "Named set constraint",
            "SET CONSTRAINT Check: 1 > 0;",
            RefHB: "3.12-39",
            Expected: new SetConstraint
            {
                Condition = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 0 } },
                Name = "Check",
                SourceRange = new RangePosition(0, 0, 0, 28),
            }));

        yield return Rule(new(
            "Basket set constraint",
            "SET CONSTRAINT (BASKET) 1 > 0;",
            RefHB: "3.12-39",
            Expected: new SetConstraint
            {
                IsBasket = true,
                Condition = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 0 } },
                SourceRange = new RangePosition(0, 0, 0, 30),
            }));

        yield return FullFile(new(
            "Multiple external constraints blocks for the same class, default constraint names",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS C =
                        Value : 0 .. 100;
                        MANDATORY CONSTRAINT Value <> 50;
                    END C;
                    CONSTRAINTS OF C =
                        MANDATORY CONSTRAINT Value > 0;
                    END;
                    CONSTRAINTS OF C =
                        MANDATORY CONSTRAINT Value < 100;
                    END;
                END Topic;
            END Model.
            """,
            RefHB: "3.12-40",
            Expected: TestTools.Build(() =>
            {
                // Hoisted so the resolved object paths (inline and in both CONSTRAINTS OF blocks) can point at it.
                var value = new AttributeDef
                {
                    Name = "Value",
                    TypeDef = new DecimalType
                    {
                        Min = 0,
                        Max = 100,
                        Precision = 0,
                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                    },
                };
                var classC = new ClassDef
                {
                    Name = "C",
                    Content = { { "Value", value } },
                    Constraints =
                    {
                        new MandatoryConstraint
                        {
                            NameIndex = 1,
                            Condition = new ComparisonExpression
                            {
                                Operator = ComparisonExpression.ComparisonOperator.NotEqual,
                                FirstOperand = new PathExpression { Path = { new IdentifierPathElement { Value = "Value" } }, Target = value },
                                SecondOperand = new NumericConstant { Value = 50 },
                            },
                        },
                    },
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
                                                { "C", classC },
                                                {
                                                    "CONSTRAINTS OF C #1",
                                                    new ConstraintsBlockDef
                                                    {
                                                        Name = "CONSTRAINTS OF C #1",
                                                        Target = new Reference<IInterlisDefinition> { Target = classC, Path = { "C" } },
                                                        Constraints =
                                                        {
                                                            new MandatoryConstraint
                                                            {
                                                                NameIndex = 2,
                                                                Condition = new ComparisonExpression
                                                                {
                                                                    Operator = ComparisonExpression.ComparisonOperator.Greater,
                                                                    FirstOperand = new PathExpression { Path = { new IdentifierPathElement { Value = "Value" } }, Target = value },
                                                                    SecondOperand = new NumericConstant { Value = 0 },
                                                                },
                                                            },
                                                        },
                                                    }
                                                },
                                                {
                                                    "CONSTRAINTS OF C #2",
                                                    new ConstraintsBlockDef
                                                    {
                                                        Name = "CONSTRAINTS OF C #2",
                                                        Target = new Reference<IInterlisDefinition> { Target = classC, Path = { "C" } },
                                                        Constraints =
                                                        {
                                                            new MandatoryConstraint
                                                            {
                                                                NameIndex = 3,
                                                                Condition = new ComparisonExpression
                                                                {
                                                                    Operator = ComparisonExpression.ComparisonOperator.Less,
                                                                    FirstOperand = new PathExpression { Path = { new IdentifierPathElement { Value = "Value" } }, Target = value },
                                                                    SecondOperand = new NumericConstant { Value = 100 },
                                                                },
                                                            },
                                                        },
                                                    }
                                                },
                                            },
                                        }
                                    },
                                }
                            }
                        },
                    },
                };
            })));

        yield return FullFile(new(
            "External constraints block",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS C =
                        Value : 0 .. 100;
                    END C;
                    CONSTRAINTS OF C =
                        MANDATORY CONSTRAINT Value > 0;
                    END;
                END Topic;
            END Model.
            """,
            RefHB: "3.12-42",
            AssertOutput: false));

        yield return FullFile(new(
            "Mandatory constraint with a non-boolean condition is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS C =
                        Value : 0 .. 100;
                        MANDATORY CONSTRAINT 1 + 2;
                    END C;
                END Topic;
            END Model.
            """,
            Description: """
                RefHB 3.12: a constraint condition must be a boolean expression. A numeric (non-boolean) body is rejected
                (GEOW previously accepted it, unlike ili2c).
                """,
            ExpectedLog: ["Type check error in 'Model.Topic.C.Constraint1' at 6:12-6:39: the constraint condition must be a boolean expression."],
            RefHB: "3.12-32",
            AssertOutput: false));

        yield return FullFile(new(
            "Mandatory constraint with a boolean condition is accepted",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS C =
                        Value : 0 .. 100;
                        MANDATORY CONSTRAINT Value > 0 AND DEFINED(Value);
                    END C;
                END Topic;
            END Model.
            """,
            Description: "A boolean condition (comparison / logical / DEFINED) is accepted — guards against false positives.",
            RefHB: "3.12-32",
            AssertOutput: false));

        yield return FullFile(new(
            "Plausibility constraint with a non-boolean condition is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS C =
                        Value : 0 .. 100;
                        CONSTRAINT >= 50% 1 + 2;
                    END C;
                END Topic;
            END Model.
            """,
            Description: "The plausibility constraint condition must be boolean too.",
            ExpectedLog: ["Type check error in 'Model.Topic.C.Constraint1' at 6:12-6:36: the constraint condition must be a boolean expression."],
            RefHB: "3.12-38",
            AssertOutput: false));

        yield return FullFile(new(
            "Set constraint with a non-boolean condition is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS C =
                        Value : 0 .. 100;
                        SET CONSTRAINT 1 + 2;
                    END C;
                END Topic;
            END Model.
            """,
            Description: "The set constraint condition must be boolean too.",
            ExpectedLog: ["Type check error in 'Model.Topic.C.Constraint1' at 6:12-6:33: the constraint condition must be a boolean expression."],
            RefHB: "3.12-39",
            AssertOutput: false));

        yield return FullFile(new(
            "Boolean domain alias as constraint condition",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    flag = BOOLEAN;
                    strictFlag EXTENDS flag = MANDATORY;
                TOPIC Topic =
                    CLASS C =
                        f : flag;
                        g : strictFlag;
                        MANDATORY CONSTRAINT f;
                        MANDATORY CONSTRAINT g;
                    END C;
                END Topic;
            END Model.
            """,
            Description: """
                A bare attribute path is a valid boolean condition also when its type is boolean THROUGH a named domain
                (including a chain of domain aliases): the alias is transparent for the condition's value type.
                """,
            RefHB: "3.12-2",
            Ili2cDivergenceReason: "ili2c's boolean-condition check does not see through a domain extension that only adds MANDATORY (verified: a plain alias of BOOLEAN and a direct MANDATORY BOOLEAN are both accepted, the MANDATORY extension of the alias is rejected with 'logical expression required'); such an extension only restricts the cardinality of a boolean domain (RefHB 3.8-6), its values stay boolean, so we accept.",
            AssertOutput: false));

        yield return FullFile(new(
            "Circular domain extension is reported, its condition left unchecked",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    a EXTENDS b = MANDATORY;
                    b EXTENDS a = MANDATORY;
                TOPIC Topic =
                    CLASS C =
                        attr : a;
                        MANDATORY CONSTRAINT attr;
                    END C;
                END Topic;
            END Model.
            """,
            Description: """
                Circular domain EXTENDS: each domain in the cycle is reported; the constraint condition itself stays
                unchecked (the alias can not be seen through, so its value type is unknown — and the walk must not loop).
                """,
            ExpectedLog:
            [
                "Type check error in 'Model.a' at 4:8-4:32: the domain transitively EXTENDS itself.",
                "Type check error in 'Model.b' at 5:8-5:32: the domain transitively EXTENDS itself.",
            ],
            RefHB: "3.8.1",
            AssertOutput: false));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                CLASS ClassName =
                    {fragment}
                END ClassName;
            END Topic;
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(ConstraintTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadConstraintDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitConstraintDef(p.constraintDef()));
    }
}
