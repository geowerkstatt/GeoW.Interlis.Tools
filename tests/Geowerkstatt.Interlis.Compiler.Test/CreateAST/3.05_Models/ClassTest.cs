using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class ClassTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Class with single property set",
            """
            CLASS A (ABSTRACT) =
            END A;
            """,
            RefHB: "3.2.5-6",
            Expected: new ClassDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 6, 0, 7), new RangePosition(1, 4, 1, 5) },
                Properties = { Property.Abstract },
            }));

        yield return Rule(new(
            "Class with multiple property set",
            """
            CLASS A (EXTENDED, FINAL) =
            END A;
            """,
            RefHB: "3.2.5-6",
            Expected: new ClassDef
            {
                Name = "A",
                NameLocations = { new RangePosition(0, 6, 0, 7), new RangePosition(1, 4, 1, 5) },
                Properties = { Property.Extended, Property.Final },
            }));

        // Cases that need multiple definitions, cross-topic extension or model level placement
        // cannot be parsed with the classDef rule, they are only used for the compiler comparison.
        yield return FullFile(new(
            "Multiple classes in a topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS ClassA =
                    END ClassA;
                    CLASS ClassB =
                    END ClassB;
                END Topic;
            END Model.
            """,
            RefHB: "3.5.3",
            AssertOutput: false));

        yield return Rule(new(
            "Class definition",
            """
            CLASS Test =
            END Test;
            """,
            RefHB: "3.5.3-1",
            Expected: new ClassDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 10),
                    new RangePosition(1, 4, 1, 8)
                },
            }));

        yield return FullFile(new(
            "Class extends another class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS BaseClass =
                    END BaseClass;
                    CLASS SubClass EXTENDS BaseClass =
                    END SubClass;
                END Topic;
            END Model.
            """,
            RefHB: "3.5.3-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Class extends final class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS BaseClass (FINAL) =
                    END BaseClass;
                    CLASS SubClass EXTENDS BaseClass =
                    END SubClass;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.SubClass' at 6:8-7:21: can not extend 'Model.Topic.BaseClass' because it is declared FINAL."],
            RefHB: "3.5.3-1",
            AssertOutput: false));

        yield return FullFile(new(
            "Class at model level",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                CLASS ClassName =
                END ClassName;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.ClassName' at 3:4-4:18: must be declared ABSTRACT because it is not part of a topic."],
            RefHB: "3.5.3-1",
            AssertOutput: false));

        yield return Rule(new(
            "Class with OID AS",
            """
            CLASS ClassName =
                OID AS INTERLIS.UUIDOID;
            END ClassName;
            """,
            RefHB: "3.5.3-2",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(2, 4, 2, 13)
                },
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "UUIDOID" }, SourceRange = new RangePosition(1, 11, 1, 27) },
            }));

        yield return Rule(new(
            "Class with NO OID",
            """
            CLASS ClassName =
                NO OID;
            END ClassName;
            """,
            RefHB: "3.5.3-2",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(2, 4, 2, 13)
                },
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "NOOID" } },
            }));

        yield return FullFile(new(
            "Reference to class with no oid",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    OID AS INTERLIS.UUIDOID;
                    CLASS ClassNoId =
                        NO OID;
                    END ClassNoId;
                    CLASS Other =
                        Attr : REFERENCE TO ClassNoId;
                    END Other;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.Other -> Attr' at 9:12-9:42: can not reference 'ClassNoId' because it has no stable object identification (NO OID)."],
            Ili2cDivergenceReason: """
                ili2c accepts references to classes declared with NO OID; RefHB 3.5.3-2 states such references can
                not be defined, so we reject.
                """,
            RefHB: "3.5.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Extend No OID with ANYOID",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic (ABSTRACT) =
                    CLASS BaseClass =
                        NO OID;
                    END BaseClass;
                    CLASS SubClass (ABSTRACT) EXTENDS BaseClass =
                        OID AS INTERLIS.ANYOID;
                    END SubClass;
                END Topic;
            END Model.
            """,
            ExpectedLog: [],
            RefHB: "3.5.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Extend ANYOID with concrete OID definition",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS BaseClass (ABSTRACT) =
                        OID AS INTERLIS.ANYOID;
                    END BaseClass;
                    CLASS SubClass EXTENDS BaseClass =
                        OID AS INTERLIS.STANDARDOID;
                    END SubClass;
                END Topic;
            END Model.
            """,
            ExpectedLog: [],
            RefHB: "3.5.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "Extend ANYOID with NO OID",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS BaseClass (ABSTRACT) =
                        OID AS INTERLIS.ANYOID;
                    END BaseClass;
                    CLASS SubClass EXTENDS BaseClass =
                        NO OID;
                    END SubClass;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.SubClass' at 7:8-9:21: the inherited OID definition 'ANYOID' can not be replaced by NO OID."],
            Ili2cDivergenceReason: """
                ili2c does not check OID redefinitions; RefHB 3.5.3-2 states an inherited ANYOID can not be
                replaced by NO OID, so we reject.
                """,
            RefHB: "3.5.3-2",
            AssertOutput: false));

        yield return Rule(new(
            "Class with consistency constraint",
            """
            CLASS Test =
                MANDATORY CONSTRAINT TRUE;
            END Test;
            """,
            RefHB: "3.5.3-3",
            Expected: new ClassDef
            {
                Name = "Test",
                NameLocations = { new RangePosition(0, 6, 0, 10), new RangePosition(2, 4, 2, 8) },
                Constraints =
                {
                    new MandatoryConstraint
                    {
                        NameIndex = 1,
                        Condition = new PathExpression
                        {
                            Path =
                            {
                                new IdentifierPathElement
                                {
                                    Value = "TRUE",
                                },
                            },
                        },
                        SourceRange = new RangePosition(1, 4, 1, 30),
                    },
                },
            }));

        yield return Rule(new(
            "Empty structure definition",
            """
            STRUCTURE StructureName =
            END StructureName;
            """,
            RefHB: "3.5.3-4",
            Expected: new ClassDef
            {
                Name = "StructureName",
                NameLocations =
                {
                    new RangePosition(0, 10, 0, 23),
                    new RangePosition(1, 4, 1, 17)
                },
                IsStructure = true,
            }));

        yield return Rule(new(
            "Structure with OID (invalid)",
            """
            STRUCTURE StructureName =
                OID AS INTERLIS.UUIDOID;
            END StructureName;
            """,
            ExpectedLog: ["Compile error at 2:4-2:7 Structure 'StructureName' cannot have an OID definition."],
            RefHB: "3.5.3-4",
            Expected: new ClassDef
            {
                Name = "StructureName",
                NameLocations =
                {
                    new RangePosition(0, 10, 0, 23),
                    new RangePosition(2, 4, 2, 17)
                },
                IsStructure = true,
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "UUIDOID" }, SourceRange = new RangePosition(1, 11, 1, 27) },
            },
            Ili2cDivergenceReason: "RefHB 3.5.3-12 (StructureDef) has no OID clause (only ClassDef 3.5.3-10 does), so we reject this; ili2c leniently accepts it"));

        yield return Rule(new(
            "Structure with NO OID (invalid)",
            """
            STRUCTURE StructureName =
                NO OID;
            END StructureName;
            """,
            ExpectedLog: ["Compile error at 2:7-2:10 Structure 'StructureName' cannot have an OID definition."],
            RefHB: "3.5.3-4",
            Expected: new ClassDef
            {
                Name = "StructureName",
                NameLocations =
                {
                    new RangePosition(0, 10, 0, 23),
                    new RangePosition(2, 4, 2, 17)
                },
                IsStructure = true,
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "NOOID" } },
            },
            Ili2cDivergenceReason: "RefHB 3.5.3-12 (StructureDef) has no OID clause (only ClassDef 3.5.3-10 does), so we reject this; ili2c leniently accepts it"));

        yield return FullFile(new(
            "Structure extends another structure",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    STRUCTURE BaseStructure =
                    END BaseStructure;
                    STRUCTURE SubStructure EXTENDS BaseStructure =
                    END SubStructure;
                END Topic;
            END Model.
            """,
            RefHB: "3.5.3-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Class extends a structure",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    STRUCTURE BaseStructure =
                    END BaseStructure;
                    CLASS SubClass EXTENDS BaseStructure =
                    END SubClass;
                END Topic;
            END Model.
            """,
            RefHB: "3.5.3-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Structure extends a class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS BaseClass =
                    END BaseClass;
                    STRUCTURE SubStructure EXTENDS BaseClass =
                    END SubStructure;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.Topic.SubStructure' at 6:8-7:25: a structure can not extend a class."],
            RefHB: "3.5.3-4",
            AssertOutput: false));

        yield return FullFile(new(
            "Structure at model level",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE StructureName =
                END StructureName;
            END Model.
            """,
            RefHB: "3.5.3-4",
            AssertOutput: false));

        yield return Rule(new(
            "Missing abstract property",
            """
            CLASS ClassName =
                Attr (ABSTRACT) : NUMERIC;
            END ClassName;
            """,
            RefHB: "3.5.3-6",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(2, 4, 2, 13)
                },
                Content =
                {
                    {
                        "Attr",
                        new AttributeDef
                        {
                            Name = "Attr",
                            NameLocations = { new RangePosition(1, 4, 1, 8) },
                            TypeDef = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(1, 22, 1, 29) },
                            Properties = { Property.Abstract },
                        }
                    },
                }
            }));

        yield return Rule(new(
            "Final class",
            """
            CLASS ClassName (FINAL) =
            END ClassName;
            """,
            RefHB: "3.5.3-6",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(1, 4, 1, 13)
                },
                Properties = { Property.Final },
            }));

        yield return Rule(new(
            "Class with conflicting properties ABSTRACT and FINAL",
            """
            CLASS ClassName (ABSTRACT, FINAL) =
            END ClassName;
            """,
            RefHB: "3.5.3-6",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(1, 4, 1, 13)
                },
                Properties = { Property.Abstract, Property.Final },
            }));

        yield return Rule(new(
            "Class with invalid property GENERIC",
            """
            CLASS ClassName (GENERIC) =
            END ClassName;
            """,
            ExpectedLog: ["Compile error at 1:17-1:24 Property 'GENERIC' is not one of the allowed properties ('ABSTRACT', 'EXTENDED', 'FINAL')."],
            RefHB: "3.5.3-6",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(1, 4, 1, 13)
                },
                Properties = { Property.Generic },
            }));

        yield return Rule(new(
            "Abstract structure",
            """
            STRUCTURE StructureName (ABSTRACT) =
            END StructureName;
            """,
            RefHB: "3.5.3-6",
            Expected: new ClassDef
            {
                Name = "StructureName",
                NameLocations =
                {
                    new RangePosition(0, 10, 0, 23),
                    new RangePosition(1, 4, 1, 17)
                },
                IsStructure = true,
                Properties = { Property.Abstract },
            }));

        yield return Rule(new(
            "Final structure",
            """
            STRUCTURE StructureName (FINAL) =
            END StructureName;
            """,
            RefHB: "3.5.3-6",
            Expected: new ClassDef
            {
                Name = "StructureName",
                NameLocations =
                {
                    new RangePosition(0, 10, 0, 23),
                    new RangePosition(1, 4, 1, 17)
                },
                IsStructure = true,
                Properties = { Property.Final },
            }));

        yield return FullFile(new(
            "Extended class in extended topic",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC BaseTopic (ABSTRACT) =
                    CLASS BaseClass (ABSTRACT) =
                        Attr : TEXT*20;
                    END BaseClass;
                END BaseTopic;
                TOPIC SubTopic EXTENDS BaseTopic =
                    CLASS BaseClass (EXTENDED) =
                        Attr (EXTENDED) : MANDATORY TEXT*10;
                    END BaseClass;
                END SubTopic;
            END Model.
            """,
            RefHB: "3.5.3-8",
            AssertOutput: false));

        yield return FullFile(new(
            "Extended and extends from the same baseclass",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC BaseTopic (ABSTRACT) =
                    CLASS BaseClass (ABSTRACT) =
                        Attr : TEXT*20;
                    END BaseClass;
                END BaseTopic;
                TOPIC SubTopic EXTENDS BaseTopic =
                    CLASS BaseClass (EXTENDED) =
                        Attr (EXTENDED) : MANDATORY TEXT*10;
                    END BaseClass;
                    CLASS SubClass EXTENDS Model.BaseTopic.BaseClass =
                        Attr (EXTENDED) : MANDATORY TEXT*5;
                    END SubClass;
                END SubTopic;
            END Model.
            """,
            RefHB: "3.5.3-8",
            AssertOutput: false,
            Ili2cDivergenceReason: "RefHB 3.5.3-8 extension rules: we accept this EXTENDED/EXTENDS combination, ili2c rejects it"));

        yield return FullFile(new(
            "Extended and extends in baseclass",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC BaseTopic (ABSTRACT) =
                    CLASS BaseClass (ABSTRACT) =
                        Attr : TEXT*20;
                    END BaseClass;
                    CLASS SubClass EXTENDS BaseClass =
                        Attr (EXTENDED) : MANDATORY TEXT*5;
                    END SubClass;
                END BaseTopic;
                TOPIC SubTopic EXTENDS BaseTopic =
                    CLASS BaseClass (EXTENDED) =
                        Attr (EXTENDED) : MANDATORY TEXT*10;
                    END BaseClass;
                END SubTopic;
            END Model.
            """,
            RefHB: "3.5.3-8",
            AssertOutput: false,
            Ili2cDivergenceReason: "RefHB 3.5.3-8 extension rules: we accept this EXTENDED/EXTENDS combination, ili2c rejects it"));

        yield return FullFile(new(
            "Extended and Extends on a class",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC BaseTopic (ABSTRACT) =
                    CLASS BaseClass (ABSTRACT) =
                    END BaseClass;
                END BaseTopic;
                TOPIC SubTopic EXTENDS BaseTopic =
                    CLASS BaseClass (EXTENDED) EXTENDS Model.BaseTopic.BaseClass =
                    END BaseClass;
                END SubTopic;
            END Model.
            """,
            ExpectedLog: ["Type check error in 'Model.SubTopic.BaseClass' at 8:8-9:22: can not use both EXTENDED and EXTENDS."],
            RefHB: "3.5.3-8",
            AssertOutput: false));

        yield return Rule(new(
            "Class definition complete",
            """
            !!@ key=value
            /** Doc-Comment */
            CLASS Test_A (ABSTRACT, EXTENDED) EXTENDS Test_B =
                OID AS INTERLIS.UUIDOID;
            END Test_A;
            """,
            RefHB: "3.5.3-10",
            Expected: new ClassDef
            {
                Name = "Test_A",
                NameLocations =
                {
                    new RangePosition(2, 6, 2, 12),
                    new RangePosition(4, 4, 4, 10)
                },
                DocComments = { "/** Doc-Comment */" },
                MetaAttributes = { { "key", "value" } },
                Extends = new Reference<ClassDef> { Path = { "Test_B" }, SourceRange = new RangePosition(2, 42, 2, 48) },
                OidType = new Reference<DomainDef> { Path = { "INTERLIS", "UUIDOID" }, SourceRange = new RangePosition(3, 11, 3, 27) },
                Properties = { Property.Abstract, Property.Extended },
            }));

        yield return Rule(new(
            "Class with mismatched end name",
            """
            CLASS ClassName =
            END WrongName;
            """,
            ExpectedLog: ["Compile error at 2:4-2:13 Start name 'ClassName' and end name 'WrongName' do not match."],
            RefHB: "3.5.3-10",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(1, 4, 1, 13)
                },
            }));

        yield return Rule(new(
            "Structure with attributes",
            """
            STRUCTURE Test =
                Attr : MANDATORY TEXT*12;
                Other : 0 .. 100;
            END Test;
            """,
            RefHB: "3.5.3-11",
            Expected: new ClassDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(0, 10, 0, 14),
                    new RangePosition(3, 4, 3, 8)
                },
                IsStructure = true,
                Content =
                {
                    {
                        "Attr",
                        new AttributeDef
                        {
                            Name = "Attr",
                            NameLocations = { new RangePosition(1, 4, 1, 8) },
                            TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 1, Max = 1 }, SourceRange = new RangePosition(1, 21, 1, 28) }
                        }
                    },
                    {
                        "Other",
                        new AttributeDef
                        {
                            Name = "Other",
                            NameLocations = { new RangePosition(2, 4, 2, 9) },
                            TypeDef = new DecimalType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 12, 2, 20) }
                        }
                    },
                }
            }));

        yield return Rule(new(
            "Structure with mismatched end name",
            """
            STRUCTURE StructureName =
            END WrongName;
            """,
            ExpectedLog: ["Compile error at 2:4-2:13 Start name 'StructureName' and end name 'WrongName' do not match."],
            RefHB: "3.5.3-11",
            Expected: new ClassDef
            {
                Name = "StructureName",
                NameLocations =
                {
                    new RangePosition(0, 10, 0, 23),
                    new RangePosition(1, 4, 1, 13)
                },
                IsStructure = true,
            }));

        yield return Rule(new(
            "Class with attributes",
            """
            CLASS Test =
                Attr : MANDATORY TEXT*12;
                Other : 0 .. 100;
            END Test;
            """,
            RefHB: "3.5.3-12",
            Expected: new ClassDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 10),
                    new RangePosition(3, 4, 3, 8)
                },
                Content =
                {
                    {
                        "Attr",
                        new AttributeDef
                        {
                            Name = "Attr",
                            NameLocations = { new RangePosition(1, 4, 1, 8) },
                            TypeDef = new TextType { Length = 12, Cardinality = new Cardinality { Min = 1, Max = 1 }, SourceRange = new RangePosition(1, 21, 1, 28) }
                        }
                    },
                    {
                        "Other",
                        new AttributeDef
                        {
                            Name = "Other",
                            NameLocations = { new RangePosition(2, 4, 2, 9) },
                            TypeDef = new DecimalType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 12, 2, 20) }
                        }
                    },
                }
            }));

        yield return Rule(new(
            "Class with ATTRIBUTE keyword",
            """
            CLASS ClassName =
            ATTRIBUTE
                Attr : TEXT*20;
            END ClassName;
            """,
            RefHB: "3.5.3-12",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(3, 4, 3, 13)
                },
                Content =
                {
                    {
                        "Attr",
                        new AttributeDef
                        {
                            Name = "Attr",
                            NameLocations = { new RangePosition(2, 4, 2, 8) },
                            TypeDef = new TextType { Length = 20, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 11, 2, 18) }
                        }
                    },
                }
            }));

        yield return Rule(new(
            "Class with parameter",
            """
            CLASS ClassName =
                PARAMETER
                    Scale : 1 .. 1000;
            END ClassName;
            """,
            RefHB: "3.5.3-12",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(3, 4, 3, 13)
                },
                Content =
                {
                    {
                        "Scale",
                        new ParameterDef
                        {
                            Name = "Scale",
                            NameLocations = { new RangePosition(2, 8, 2, 13) },
                            TypeDef = new DecimalType { Min = 1, Max = 1000, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 16, 2, 25) },
                        }
                    },
                }
            }));

        yield return Rule(new(
            "Class with multiple parameters",
            """
            CLASS ClassName =
                PARAMETER
                    ParamA : 0 .. 100;
                    ParamB : 0 .. 100;
            END ClassName;
            """,
            RefHB: "3.5.3-12",
            Expected: new ClassDef
            {
                Name = "ClassName",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 15),
                    new RangePosition(4, 4, 4, 13)
                },
                Content =
                {
                    {
                        "ParamA",
                        new ParameterDef
                        {
                            Name = "ParamA",
                            NameLocations = { new RangePosition(2, 8, 2, 14) },
                            TypeDef = new DecimalType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(2, 17, 2, 25) },
                        }
                    },
                    {
                        "ParamB",
                        new ParameterDef
                        {
                            Name = "ParamB",
                            NameLocations = { new RangePosition(3, 8, 3, 14) },
                            TypeDef = new DecimalType { Min = 0, Max = 100, Precision = 0, Cardinality = new Cardinality { Min = 0, Max = 1 }, SourceRange = new RangePosition(3, 17, 3, 25) },
                        }
                    },
                }
            }));

        yield return Rule(new(
            "Class extends qualified class reference",
            """
            CLASS SubClass EXTENDS Other.Topic.Base =
            END SubClass;
            """,
            RefHB: "3.5.3-13",
            Expected: new ClassDef
            {
                Name = "SubClass",
                NameLocations = { new RangePosition(0, 6, 0, 14), new RangePosition(1, 4, 1, 12) },
                Extends = new Reference<ClassDef> { Path = { "Other", "Topic", "Base" }, SourceRange = new RangePosition(0, 23, 0, 39) },
            }));

        yield return Rule(new(
            "Structure extends qualified structure reference",
            """
            STRUCTURE Sub EXTENDS Other.Topic.Base =
            END Sub;
            """,
            RefHB: "3.5.3-14",
            Expected: new ClassDef
            {
                Name = "Sub",
                NameLocations = { new RangePosition(0, 10, 0, 13), new RangePosition(1, 4, 1, 7) },
                Extends = new Reference<ClassDef> { Path = { "Other", "Topic", "Base" }, SourceRange = new RangePosition(0, 22, 0, 38) },
                IsStructure = true,
            }));

        yield return Rule(new(
            "Class extends nonexistent class",
            """
            CLASS SubClass EXTENDS NonExistent =
            END SubClass;
            """,
            RefHB: "3.5.3-15",
            Expected: new ClassDef
            {
                Name = "SubClass",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 14),
                    new RangePosition(1, 4, 1, 12)
                },
                Extends = new Reference<ClassDef> { Path = { "NonExistent" }, SourceRange = new RangePosition(0, 23, 0, 34) },
            }));

        yield return FullFile(new(
            "Class and structure extending themselves",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE S EXTENDS S =
                END S;
                TOPIC Topic =
                    CLASS A EXTENDS A =
                    END A;
                END Topic;
            END Model.
            """,
            Description: """
                A circular EXTENDS chain makes the definition transitively its own base — it extends nothing and
                every walk along its base chain would loop (ili2c rejects the models too: its single-pass name
                resolution can not even resolve the self-reference).
                """,
            RefHB: "3.5.3-13",
            ExpectedLog:
            [
                "Type check error in 'Model.S' at 3:4-4:10: the structure transitively EXTENDS itself.",
                "Type check error in 'Model.Topic.A' at 6:8-7:14: the class transitively EXTENDS itself.",
            ],
            AssertOutput: false));


        yield return FullFile(new(
            "OID definitions concretized along the predefined ladder",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS A =
                        NO OID;
                    END A;
                    CLASS B (ABSTRACT) EXTENDS A =
                        OID AS INTERLIS.ANYOID;
                    END B;
                    CLASS C EXTENDS B =
                        OID AS INTERLIS.STANDARDOID;
                    END C;
                    CLASS D EXTENDS C =
                        OID AS INTERLIS.STANDARDOID;
                    END D;
                END Topic;
            END Model.
            """,
            Description: """
                RefHB 3.5.3-2: an inherited NO OID may be replaced by ANYOID and an inherited ANYOID by a concrete
                definition — the ladder is the predefined domain hierarchy NOOID <- ANYOID <- STANDARDOID
                (RefHB 3.8.9-6/-8), and repeating an inherited definition unchanged is no replacement at all.
                """,
            RefHB: "3.5.3-2",
            AssertOutput: false));

        yield return FullFile(new(
            "OID definitions replaced against the predefined ladder",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS C =
                        OID AS INTERLIS.STANDARDOID;
                    END C;
                    CLASS D EXTENDS C =
                        OID AS INTERLIS.I32OID;
                    END D;
                END Topic;
            END Model.
            """,
            Description: """
                A concrete OID definition can not be replaced by a different concrete one — I32OID does not extend
                the inherited STANDARDOID.
                """,
            RefHB: "3.5.3-2",
            ExpectedLog: ["Type check error in 'Model.Topic.D' at 7:8-9:14: the inherited OID definition 'STANDARDOID' is concrete and can not be changed."],
            Ili2cDivergenceReason: """
                ili2c does not check OID redefinitions at all; RefHB 3.5.3-2 and 3.8.9-13 only allow replacing an
                inherited definition by an extension of it, so we reject.
                """,
            AssertOutput: false));


        yield return FullFile(new(
            "Concrete class with an open OID definition",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                    CLASS A =
                        OID AS INTERLIS.ANYOID;
                    END A;
                END Topic;
            END Model.
            """,
            Description: """
                An ANYOID assignment declares that identifications are expected while their exact definition is
                still open — only an abstract class can leave that undecided (RefHB 3.8.9-14). NO OID needs no
                abstractness: it declares the identification unstable, not undecided.
                """,
            RefHB: "3.8.9-14",
            ExpectedLog: ["Type check error in 'Model.Topic.A' at 4:8-6:14: must be declared ABSTRACT because its OID definition 'ANYOID' is still open."],
            Ili2cDivergenceReason: """
                ili2c accepts ANYOID assignments on concrete classes and topics; RefHB 3.8.9-14 reserves ANYOID
                for abstract topics and classes (otherwise it is only usable as an attribute value range), so we
                reject.
                """,
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
        => ComparisonRows(nameof(ClassTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadClassDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitClassDef(p.classDef()));
    }
}
