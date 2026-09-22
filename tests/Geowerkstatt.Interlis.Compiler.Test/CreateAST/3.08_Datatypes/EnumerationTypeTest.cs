using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class EnumerationTypeTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Enumeration attribute",
            "Attr : MANDATORY (red (lightRed, darkRed), green, blue (lightBlue, darkBlue : FINAL)) ORDERED;",
            RefHB: "3.8.2-1",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationType
                {
                    Cardinality = new Cardinality { Min = 1, Max = 1 },
                    Sequencing = EnumerationType.Sequencings.Ordered,
                    SourceRange = new RangePosition(0, 17, 0, 93),
                    Values =
                    {
                        new EnumerationTreeNode
                        {
                            Name = "red",
                            SubValues =
                            {
                                new EnumerationTreeNode { Name = "lightRed" },
                                new EnumerationTreeNode { Name = "darkRed" },
                            }
                        },
                        new EnumerationTreeNode { Name = "green" },
                        new EnumerationTreeNode
                        {
                            Name = "blue",
                            SubValues =
                            {
                                new EnumerationValuesList(isFinal : true)
                                {
                                    new EnumerationTreeNode { Name = "lightBlue" },
                                    new EnumerationTreeNode { Name = "darkBlue" },
                                }
                            }
                        },
                    },
                },
            }));

        yield return Rule(new(
            "Farben enumeration worked example",
            "Attr : (rot (dunkelrot, orange, karmin), gelb, gruen (hellgruen, dunkelgruen));",
            RefHB: "3.8.2-2",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationType
                {
                    Values =
                    {
                        new EnumerationTreeNode
                        {
                            Name = "rot",
                            SubValues =
                            {
                                new EnumerationTreeNode { Name = "dunkelrot" },
                                new EnumerationTreeNode { Name = "orange" },
                                new EnumerationTreeNode { Name = "karmin" },
                            },
                        },
                        new EnumerationTreeNode { Name = "gelb" },
                        new EnumerationTreeNode
                        {
                            Name = "gruen",
                            SubValues =
                            {
                                new EnumerationTreeNode { Name = "hellgruen" },
                                new EnumerationTreeNode { Name = "dunkelgruen" },
                            },
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 78),
                },
            }));

        yield return Rule(new(
            "Enumeration with arbitrary nesting depth",
            "Attr : (a (a1 (a1x, a1y), a2), b);",
            RefHB: "3.8.2-5",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationType
                {
                    Values =
                    {
                        new EnumerationTreeNode
                        {
                            Name = "a",
                            SubValues =
                            {
                                new EnumerationTreeNode
                                {
                                    Name = "a1",
                                    SubValues =
                                    {
                                        new EnumerationTreeNode { Name = "a1x" },
                                        new EnumerationTreeNode { Name = "a1y" },
                                    },
                                },
                                new EnumerationTreeNode { Name = "a2" },
                            },
                        },
                        new EnumerationTreeNode { Name = "b" },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 33),
                },
            }));

        yield return Rule(new(
            "Circular enumeration",
            "Attr : (Werktage (Montag, Dienstag), Sonntag) CIRCULAR;",
            RefHB: "3.8.2-6",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationType
                {
                    Sequencing = EnumerationType.Sequencings.Circular,
                    Values =
                    {
                        new EnumerationTreeNode
                        {
                            Name = "Werktage",
                            SubValues =
                            {
                                new EnumerationTreeNode { Name = "Montag" },
                                new EnumerationTreeNode { Name = "Dienstag" },
                            },
                        },
                        new EnumerationTreeNode { Name = "Sonntag" },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 54),
                },
            }));

        yield return FullFile(new(
            "All of enumeration worked example",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Lage = (unten, mitte, oben) ORDERED;
                Wochentage = (Werktage (Montag, Dienstag, Mittwoch,
                              Donnerstag, Freitag, Samstag),
                              Sonntag) CIRCULAR;
                WochentagsWerte = ALL OF Wochentage;
            END Model.
            """,
            RefHB: "3.8.2-10",
            AssertOutput: false));

        yield return Rule(new(
            "Ordered enumeration",
            "Attr : (unten, mitte, oben) ORDERED;",
            RefHB: "3.8.2-12",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationType
                {
                    Sequencing = EnumerationType.Sequencings.Ordered,
                    Values =
                    {
                        new EnumerationTreeNode { Name = "unten" },
                        new EnumerationTreeNode { Name = "mitte" },
                        new EnumerationTreeNode { Name = "oben" },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 35),
                },
            }));

        yield return Rule(new(
            "All of enumeration type",
            "Attr : ALL OF Wochentage;",
            RefHB: "3.8.2-13",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationValuesType
                {
                    TargetEnumeration = new Reference<DomainDef> { Path = { new("Wochentage") } },
                    LeafsOnly = false,
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 24),
                },
            }));

        yield return Rule(new(
            "Enumeration with final marker on whole enumeration",
            "Attr : (red, green, blue : FINAL);",
            RefHB: "3.8.2-14",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationType
                {
                    Values =
                    {
                        new EnumerationTreeNode { Name = "red" },
                        new EnumerationTreeNode { Name = "green" },
                        new EnumerationTreeNode { Name = "blue" },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 33),
                },
            }));

        yield return FullFile(new(
            "Enumeration extension worked example",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot,
                         gelb,
                         gruen);
                FarbePlus EXTENDS Farbe = (rot (dunkelrot, orange, karmin),
                                           gruen (hellgruen, dunkelgruen: FINAL),
                                           blau);
                FarbePlusPlus EXTENDS FarbePlus = (rot (FINAL),
                                                   blau (hellblau, dunkelblau));
            END Model.
            """,
            RefHB: "3.8.2-22",
            AssertOutput: false));

        yield return Rule(new(
            "Dotted enumeration element",
            "Attr : (rot.dunkelrot (hell, dunkel));",
            Description: """
                A dotted element name builds the same nested tree as authored nesting, with every name after the
                first flagged as dotted; the type checker uses the flag to reject dotted names outside of
                enumeration extensions, as this one is (both compilers reject the full file).
                """,
            RefHB: "3.8.2-17",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new EnumerationType
                {
                    Values =
                    {
                        new EnumerationTreeNode
                        {
                            Name = "rot",
                            SubValues =
                            {
                                new EnumerationTreeNode
                                {
                                    Name = "dunkelrot",
                                    FromDottedName = true,
                                    SubValues =
                                    {
                                        new EnumerationTreeNode { Name = "hell" },
                                        new EnumerationTreeNode { Name = "dunkel" },
                                    },
                                },
                            },
                        },
                    },
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    SourceRange = new RangePosition(0, 7, 0, 37),
                },
            }));

        yield return FullFile(new(
            "Dotted element name in a primary definition",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot.dunkelrot (hell, dunkel));
            END Model.
            """,
            Description: """
                A dotted name only identifies an inherited element for an extension to refine, so a primary
                definition can not use one.
                """,
            RefHB: "3.8.2-17",
            ExpectedLog: ["Type check error in 'Model.Farbe' at 4:4-4:43: the dotted element name '#rot.dunkelrot' is only allowed in an extension of an enumeration."],
            AssertOutput: false));

        yield return FullFile(new(
            "Extension refines inherited elements via dotted names",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot (dunkelrot (tief), orange), gelb);
                FarbePlus EXTENDS Farbe = (rot.dunkelrot.tief (hell, dunkel), gelb (mais));
            END Model.
            """,
            Description: """
                A dotted name walks down the inherited tree; the identified leaf becomes a node with the defined
                sub-enumeration, exactly like refining via authored nesting (gelb).
                """,
            RefHB: "3.8.2-18",
            AssertOutput: false));

        yield return FullFile(new(
            "Dotted names must identify inherited elements",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot (dunkelrot), gelb);
                FarbePlus EXTENDS Farbe = (violett.hell (x, y), rot.blass (a, b));
            END Model.
            """,
            Description: """
                Neither 'violett.hell' nor 'blass' under the inherited 'rot' name an inherited element; new elements
                are added with plain names and authored nesting.
                """,
            RefHB: "3.8.2-17",
            ExpectedLog:
            [
                "Type check error in 'Model.FarbePlus' at 5:4-5:70: the dotted element name '#violett.hell' must identify an element of the inherited enumeration.",
                "Type check error in 'Model.FarbePlus' at 5:4-5:70: the dotted element name '#rot.blass' must identify an element of the inherited enumeration.",
            ],
            Ili2cDivergenceReason: """
                ili2c accepts dotted names that match no inherited element and nests them as new elements; RefHB
                3.8.2-17 allows multiple names only to identify a bisheriges (inherited) element, so we reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Dotted element name without a sub-enumeration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot (dunkelrot), gelb);
                FarbePlus EXTENDS Farbe = (rot.dunkelrot);
            END Model.
            """,
            Description: """
                A dotted name identifies an inherited element in order to refine it, so it must define the sub-
                enumeration; without one it merely re-lists the inherited element (ili2c reports both too).
                """,
            RefHB: "3.8.2-17",
            ExpectedLog:
            [
                "Type check error in 'Model.FarbePlus' at 5:4-5:46: the dotted element name '#rot.dunkelrot' must define a sub-enumeration.",
                "Type check error in 'Model.FarbePlus' at 5:4-5:46: the element '#rot.dunkelrot' is already defined by the inherited enumeration.",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Re-listing inherited elements without extending them",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot (dunkelrot), gelb);
                FarbePlus EXTENDS Farbe = (gelb, rot (dunkelrot));
            END Model.
            """,
            Description: """
                An extension element naming an inherited element must refine it with a sub-enumeration; repeating
                the leaf 'gelb' or the nested leaf 'dunkelrot' unchanged duplicates it.
                """,
            RefHB: "3.8.2-17",
            ExpectedLog:
            [
                "Type check error in 'Model.FarbePlus' at 5:4-5:54: the element '#gelb' is already defined by the inherited enumeration.",
                "Type check error in 'Model.FarbePlus' at 5:4-5:54: the element '#rot.dunkelrot' is already defined by the inherited enumeration.",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Duplicate elements in a primary definition",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot (a, a), rot (b));
            END Model.
            """,
            RefHB: "3.8.2-5",
            ExpectedLog:
            [
                "Type check error in 'Model.Farbe' at 4:4-4:34: duplicate enumeration element '#rot.a'.",
                "Type check error in 'Model.Farbe' at 4:4-4:34: duplicate enumeration element '#rot'.",
            ],
            AssertOutput: false));

        yield return FullFile(new(
            "Duplicate element within one extension",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot);
                FarbePlus EXTENDS Farbe = (neu, neu);
            END Model.
            """,
            RefHB: "3.8.2-5",
            ExpectedLog: ["Type check error in 'Model.FarbePlus' at 5:4-5:41: duplicate enumeration element '#neu'."],
            Ili2cDivergenceReason: """
                ili2c only checks an extension's elements against the inherited enumeration and accepts re-defining
                an element the same extension introduced; RefHB 3.8.2-5 requires element names to be unique within
                each nesting, so we reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Same inherited leaf refined twice in one extension",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot, gelb);
                FarbePlus EXTENDS Farbe = (gelb (hell), gelb (dunkel));
            END Model.
            """,
            Description: """
                Authored element names are compared by their full dotted names, so two deltas for different
                inherited elements ('rot.a' and 'rot.b') coexist — but listing the same name twice is a duplicate
                even though each occurrence legally refines the inherited 'gelb'.
                """,
            RefHB: "3.8.2-5",
            ExpectedLog: ["Type check error in 'Model.FarbePlus' at 5:4-5:59: duplicate enumeration element '#gelb'."],
            Ili2cDivergenceReason: """
                ili2c merges repeated deltas for the same inherited element and accepts; RefHB 3.8.2-5 requires
                element names to be unique within each nesting, so we reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Extension adds to a FINAL enumeration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot, gelb : FINAL);
                FarbePlus EXTENDS Farbe = (blau);
            END Model.
            """,
            RefHB: "3.8.2-19",
            ExpectedLog: ["Type check error in 'Model.FarbePlus' at 5:4-5:37: can not add the element '#blau' because the inherited enumeration is FINAL."],
            AssertOutput: false));

        yield return FullFile(new(
            "Extension adds to a FINAL sub-enumeration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot (dunkelrot, orange : FINAL), gelb);
                FarbePlus EXTENDS Farbe = (rot (karmin));
            END Model.
            """,
            RefHB: "3.8.2-27",
            ExpectedLog: ["Type check error in 'Model.FarbePlus' at 5:4-5:45: can not add the element '#rot.karmin' because the inherited sub-enumeration 'rot' is FINAL."],
            AssertOutput: false));

        yield return FullFile(new(
            "FINAL marker without new elements finalizes the enumeration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot, gelb);
                FarbePlus EXTENDS Farbe = (FINAL);
                FarbePlusPlus EXTENDS FarbePlus = (blau);
            END Model.
            """,
            Description: """
                RefHB 3.8.2-19: an extension may declare FINAL without adding elements; the marker is inherited, so
                the further extension can not add elements any more.
                """,
            RefHB: "3.8.2-19",
            ExpectedLog: ["Type check error in 'Model.FarbePlusPlus' at 6:4-6:45: can not add the element '#blau' because the inherited enumeration is FINAL."],
            AssertOutput: false));

        yield return FullFile(new(
            "FINAL marker on an element without a sub-enumeration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot, gelb, blau, transparent (FINAL));
            END Model.
            """,
            Description: """
                RefHB 3.8.2-19 foresees the bare (FINAL) form only for freezing an existing sub-enumeration of an
                extended enumeration; here it creates an empty FINAL sub-enumeration instead, which silently turns
                'transparent' into a node that is no valid value (only leaves are values, RefHB 3.8.2-1) and never
                can be.
                """,
            RefHB: "3.8.2-19",
            ExpectedLog: ["Type check error in 'Model.Farbe' at 4:4-4:51: the element '#transparent' has no sub-enumeration to declare FINAL."],
            Ili2cDivergenceReason: """
                ili2c accepts the bare (FINAL) on an element without a sub-enumeration and silently drops the
                element from the value set; RefHB 3.8.2-19 only foresees the form for freezing an existing
                sub-enumeration, so we reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "FINAL marker on an inherited leaf",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot (dunkelrot), gelb);
                FarbePlus EXTENDS Farbe = (gelb (FINAL), neu (FINAL));
            END Model.
            """,
            Description: """
                The bare (FINAL) freezes an existing sub-enumeration ('rot (FINAL)' in the RefHB 3.8.2-22 worked
                example freezes the refined 'rot'); the inherited 'gelb' is a leaf and 'neu' a new element, so
                there is nothing to freeze — both would only lose their value to an empty FINAL sub-enumeration.
                """,
            RefHB: "3.8.2-19",
            ExpectedLog:
            [
                "Type check error in 'Model.FarbePlus' at 5:4-5:58: the element '#gelb' has no sub-enumeration to declare FINAL.",
                "Type check error in 'Model.FarbePlus' at 5:4-5:58: the element '#neu' has no sub-enumeration to declare FINAL.",
            ],
            Ili2cDivergenceReason: """
                ili2c accepts the bare (FINAL) on an element without a sub-enumeration and silently drops the
                element from the value set; RefHB 3.8.2-19 only foresees the form for freezing an existing
                sub-enumeration, so we reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Refining a leaf inside a FINAL enumeration stays legal",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot, gelb : FINAL);
                FarbePlus EXTENDS Farbe = (rot (hell, dunkel));
            END Model.
            """,
            Description: """
                RefHB 3.8.2 defines two extension mechanisms — giving leaves a sub-enumeration (3.8.2-18) and adding
                elements to a Teilaufzählung (3.8.2-19) — and introduces FINAL inside -19 as the counter to that
                mechanism only ('zusätzliche Aufzählelemente anfügen ... unterbunden ... indem die Teilaufzählung
                als abschliessend erklärt wird'). FINAL therefore binds the marked Teilaufzählung against additions,
                not the subtree against refinement: turning the inherited leaf 'rot' into a node defines a new,
                unmarked sub-enumeration (ili2c agrees).
                """,
            RefHB: "3.8.2-19",
            AssertOutput: false));

        yield return FullFile(new(
            "FINAL transfers through a dotted refinement",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot (dunkelrot), gelb);
                FarbePlus EXTENDS Farbe = (rot.dunkelrot (hell : FINAL));
                FarbePlusPlus EXTENDS FarbePlus = (rot.dunkelrot (neu));
            END Model.
            """,
            RefHB: "3.8.2-19",
            ExpectedLog: ["Type check error in 'Model.FarbePlusPlus' at 6:4-6:60: can not add the element '#rot.dunkelrot.neu' because the inherited sub-enumeration 'rot.dunkelrot' is FINAL."],
            AssertOutput: false));

        yield return FullFile(new(
            "Element added by an intermediate extension is inherited",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot);
                FarbePlus EXTENDS Farbe = (blau);
                FarbePlusPlus EXTENDS FarbePlus = (blau);
            END Model.
            """,
            RefHB: "3.8.2-19",
            ExpectedLog: ["Type check error in 'Model.FarbePlusPlus' at 6:4-6:45: the element '#blau' is already defined by the inherited enumeration."],
            AssertOutput: false));

        yield return FullFile(new(
            "Extending a CIRCULAR enumeration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Wochentage = (Werktage, Sonntag) CIRCULAR;
                WochentagePlus EXTENDS Wochentage = (Feiertag);
                WochentagePlusPlus EXTENDS WochentagePlus = (Brueckentag);
            END Model.
            """,
            Description: """
                The second extension declares no sequencing of its own but inherits CIRCULAR through the chain, so
                both extensions are rejected.
                """,
            RefHB: "3.8.2-20",
            ExpectedLog:
            [
                "Type check error in 'Model.WochentagePlus' at 5:4-5:51: a CIRCULAR enumeration can not be extended.",
                "Type check error in 'Model.WochentagePlusPlus' at 6:4-6:62: a CIRCULAR enumeration can not be extended.",
            ],
            Ili2cDivergenceReason: """
                ili2c accepts extensions of circular enumerations; RefHB 3.8.2-20 states circular enumerations can
                not be extended, so we reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Extension of a CIRCULAR enumeration that only refines a leaf",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Wochentage = (Werktage, Sonntag) CIRCULAR;
                WochentagePlus EXTENDS Wochentage = (Werktage (Mo, Di, Mi, Do, Fr));
            END Model.
            """,
            Description: """
                RefHB 3.8.2-20 forbids extending circular enumerations categorically, without an exception for
                extensions that add no elements to the circular level — refining 'Werktage' still replaces its value
                with the new leaves.
                """,
            RefHB: "3.8.2-20",
            ExpectedLog: ["Type check error in 'Model.WochentagePlus' at 5:4-5:72: a CIRCULAR enumeration can not be extended."],
            Ili2cDivergenceReason: """
                ili2c accepts extensions of circular enumerations; RefHB 3.8.2-20 states circular enumerations can
                not be extended, so we reject.
                """,
            AssertOutput: false));

        yield return FullFile(new(
            "Enumeration extending an ALL OF domain",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              DOMAIN
                Farbe = (rot, gelb);
                Werte = ALL OF Farbe;
                WertePlus EXTENDS Werte = (neu);
            END Model.
            """,
            Description: """
                An ALL OF domain is a different kind of value range than an enumeration definition, so an
                enumeration can not extend it (ili2c: enumeration types can only extend other enumeration types).
                """,
            RefHB: "3.8.2-13",
            ExpectedLog: ["Type check error in 'Model.WertePlus' at 6:4-6:36: the domain must be of the same kind as its base 'Werte'."],
            AssertOutput: false));

        yield return FullFile(new(
            "Extended attribute refines the inherited enumeration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
                CLASS ClassA =
                  attr : (rot, gelb);
                END ClassA;
                CLASS ClassB EXTENDS ClassA =
                  attr (EXTENDED) : (gelb (hell, dunkel), neu);
                END ClassB;
              END Topic;
            END Model.
            """,
            Description: """
                An inline enumeration on an EXTENDED attribute is a delta on the inherited attribute's enumeration,
                following the same rules as an enumeration domain extension.
                """,
            RefHB: "3.8.2-18",
            AssertOutput: false));

        yield return FullFile(new(
            "Extended attribute adds to a FINAL inherited enumeration",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
                CLASS ClassA =
                  attr : (rot, gelb : FINAL);
                END ClassA;
                CLASS ClassB EXTENDS ClassA =
                  attr (EXTENDED) : (blau);
                END ClassB;
              END Topic;
            END Model.
            """,
            RefHB: "3.8.2-19",
            ExpectedLog: ["Type check error in 'Model.Topic.ClassB -> attr' at 8:6-8:31: can not add the element '#blau' because the inherited enumeration is FINAL."],
            AssertOutput: false));

        yield return FullFile(new(
            "Extended attribute inherits the delta of an intermediate extension",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              TOPIC Topic =
                CLASS ClassA =
                  attr : (rot);
                END ClassA;
                CLASS ClassB EXTENDS ClassA =
                  attr (EXTENDED) : (blau);
                END ClassB;
                CLASS ClassC EXTENDS ClassB =
                  attr (EXTENDED) : (blau);
                END ClassC;
              END Topic;
            END Model.
            """,
            Description: """
                The delta of ClassB is part of ClassC's inherited enumeration, so re-listing 'blau' without a sub-
                enumeration duplicates it.
                """,
            RefHB: "3.8.2-19",
            ExpectedLog: ["Type check error in 'Model.Topic.ClassC -> attr' at 11:6-11:31: the element '#blau' is already defined by the inherited enumeration."],
            AssertOutput: false));

        yield return Rule(new(
            "Horizontal alignment type",
            "Attr : HALIGNMENT;",
            RefHB: "3.8.3-6",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Extends = new Reference<DomainDef> { Path = { new("INTERLIS"), new("HALIGNMENT") } },
                },
            }));

        yield return Rule(new(
            "Vertical alignment type",
            "Attr : VALIGNMENT;",
            RefHB: "3.8.3-6",
            Expected: new AttributeDef
            {
                Name = "Attr",
                NameLocations = { new RangePosition(0, 0, 0, 4) },
                TypeDef = new TypeRef
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Extends = new Reference<DomainDef> { Path = { new("INTERLIS"), new("VALIGNMENT") } },
                },
            }));
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
        => ComparisonRows(nameof(EnumerationTypeTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadAttributeDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitAttributeDef(p.attributeDef()));
    }
}
