using Geowerkstatt.Interlis.Compiler.AST;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class ModelTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Model with explanation",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" // Explanation */!!/* // =
            END Model.
            """,
            RefHB: "3.2.6-1",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                URI = "http://example.com",
                Version = "1.0.0",
                Explanation = " Explanation */!!/* ",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Model definition",
            """
            MODEL Test AT "http://foo.test" VERSION "123" =
            END Test.
            """,
            RefHB: "3.5.1-1",
            Expected: new ModelDef
            {
                Name = "Test",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 10),
                    new RangePosition(1, 4, 1, 8)
                },
                URI = "http://foo.test",
                Version = "123",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Type model with topic as invalid content",
            """
            TYPE MODEL Model AT "http://example.com" VERSION "1.0.0" =
                TOPIC Topic =
                END Topic;
            END Model.
            """,
            RefHB: "3.5.1-2",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 11, 0, 16),
                    new RangePosition(3, 4, 3, 9)
                },
                Type = ModelDef.ModelType.Type,
                URI = "http://example.com",
                Version = "1.0.0",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
                Content =
                {
                    {
                        "Topic",
                        new TopicDef
                        {
                            Name = "Topic",
                            NameLocations =
                            {
                                new RangePosition(1, 10, 1, 15),
                                new RangePosition(2, 8, 2, 13)
                            },
                        }
                    }
                },
            }));

        yield return Rule(new(
            "Refsystem model",
            """
            REFSYSTEM MODEL Model AT "http://example.com" VERSION "1.0.0" =
            END Model.
            """,
            RefHB: "3.5.1-3",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 16, 0, 21),
                    new RangePosition(1, 4, 1, 9)
                },
                Type = ModelDef.ModelType.Refsystem,
                URI = "http://example.com",
                Version = "1.0.0",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Symbology model",
            """
            SYMBOLOGY MODEL Model AT "http://example.com" VERSION "1.0.0" =
            END Model.
            """,
            RefHB: "3.5.1-4",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 16, 0, 21),
                    new RangePosition(1, 4, 1, 9)
                },
                Type = ModelDef.ModelType.Symbology,
                URI = "http://example.com",
                Version = "1.0.0",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Model language",
            """
            MODEL Model (de_CH) AT "http://example.com" VERSION "1.0.0" =
            END Model.
            """,
            RefHB: "3.5.1-5",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                Language = "de_CH",
                URI = "http://example.com",
                Version = "1.0.0",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Model with no incremental transfer",
            """
            MODEL Model NOINCREMENTALTRANSFER AT "http://example.com" VERSION "1.0.0" =
            END Model.
            """,
            RefHB: "3.5.1-6",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(1, 4, 1, 9) },
                Imports =
                {
                    { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) },
                },
                URI = "http://example.com",
                Version = "1.0.0",
                NoIncrementalTransfer = true,
            }));

        yield return Rule(new(
            "Invalid URI",
            """
            MODEL Model AT "not a URI!" VERSION "1.0.0" =
            END Model.
            """,
            ExpectedLog: ["Compile error at 1:15-1:16 Model URI 'not a URI!' is not a valid URI."],
            RefHB: "3.5.1-7",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                URI = "not a URI!",
                Version = "1.0.0",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Model definition complete",
            """
            /** A model with all optional fields set */
            !!@ EPSG=2056
            CONTRACTED TYPE MODEL Test_A (en) NOINCREMENTALTRANSFER AT "http://foo.test" VERSION "123" // Version Explanation //
            TRANSLATION OF Test_B ["12"] =
                CHARSET "UTF-32";
                XMLNS "http://www.interlis.test";
                IMPORTS UNQUALIFIED Test_C;
            END Test_A.
            """,
            RefHB: "3.5.1-9",
            Ech0117: "5-3",
            Expected: new ModelDef
            {
                Name = "Test_A",
                NameLocations =
                {
                    new RangePosition(2, 22, 2, 28),
                    new RangePosition(7, 4, 7, 10)
                },
                DocComments = { "/** A model with all optional fields set */" },
                MetaAttributes = { { "EPSG", "2056" } },
                Type = ModelDef.ModelType.Type,
                NoIncrementalTransfer = true,
                Language = "en",
                URI = "http://foo.test",
                Version = "123",
                Explanation = " Version Explanation ",
                TranslationOf = new Reference<IInterlisDefinition> { Path = { "Test_B" }, SourceRange = new RangePosition(3, 15, 3, 21) },
                TranslationOfVersion = "12",
                Charset = "UTF-32",
                Xmlns = "http://www.interlis.test",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name }, SourceRange = null }) }, // Implicit import
                    { "Test_C", (true, new Reference<ModelDef> { Path = { "Test_C" }, SourceRange = new RangePosition(6, 24, 6, 30) }) }
                },
            }));

        yield return Rule(new(
            "Missing version",
            """
            MODEL Model AT "http://example.com" =
            END Model.
            """,
            ExpectedLog: ["Compile error at 1:36-1:37 mismatched input '=' expecting 'VERSION'."],
            RefHB: "3.5.1-9",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11)
                },
                URI = "http://example.com",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Missing name",
            """
            MODEL AT "http://example.com" VERSION "1.0.0" =
            END Model.
            """,
            ExpectedLog: ["Compile error at 1:6-1:8 missing IDENTIFIER at 'AT'."],
            RefHB: "3.5.1-9",
            Expected: null));

        yield return Rule(new(
            "Missing URL",
            """
            MODEL Model VERSION "1.0.0" =
            END Model.
            """,
            ExpectedLog: ["Compile error at 1:12-1:19 mismatched input 'VERSION' expecting {'(', 'AT', 'NOINCREMENTALTRANSFER'}."],
            RefHB: "3.5.1-9",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11)
                },
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return FullFile(new(
            "Model translation of",
            """
            INTERLIS 2.4;
            MODEL Deutsch (de_CH) AT "http://example.com" VERSION "1.0.0" = END Deutsch.
            MODEL English (en) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Deutsch [ "1.0.0" ] = END English.
            """,
            RefHB: "3.5.1-10",
            Expected: TestTools.Build(() =>
            {
                var deutsch = new ModelDef
                {
                    Name = "Deutsch",
                    Language = "de_CH",
                    URI = "http://example.com",
                    Version = "1.0.0",
                    Imports =
                    {
                        { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                    },
                };

                return new InterlisEnvironment
                {
                    Version = 2.4,
                    Content =
                    {
                        { InternalModel.Interlis.Name, InternalModel.Interlis },
                        { "Deutsch", deutsch },
                        {
                            "English",
                            new ModelDef
                            {
                                Name = "English",
                                Language = "en",
                                URI = "http://example.com",
                                Version = "1.0.0",
                                TranslationOf = new Reference<IInterlisDefinition> { Target = deutsch, Path = { "Deutsch" } },
                                TranslationOfVersion = "1.0.0",
                                Imports =
                                {
                                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                                },
                            }
                        },
                    }
                };
            })));

        yield return FullFile(new(
            "Model translation links the elements to the original",
            """
            INTERLIS 2.4;
            MODEL Deutsch (de) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Thema =
                    CLASS Klasse =
                    END Klasse;
                END Thema;
            END Deutsch.
            MODEL English (en) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Deutsch [ "1.0.0" ] =
                TOPIC Topic =
                    CLASS Item =
                    END Item;
                END Topic;
            END English.
            """,
            Description: """
                A translation may only change names, so its elements correspond to the original's elements by
                declaration position. The reference resolver writes these element-level links, giving every definition
                of a translated model a navigable reference to the definition it translates.
                """,
            RefHB: "3.5.1-10",
            Expected: TestTools.Build(() =>
            {
                var klasse = new ClassDef { Name = "Klasse" };
                var thema = new TopicDef
                {
                    Name = "Thema",
                    Content = { { "Klasse", klasse } },
                };
                var deutsch = new ModelDef
                {
                    Name = "Deutsch",
                    Language = "de",
                    URI = "http://example.com",
                    Version = "1.0.0",
                    Imports =
                    {
                        { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { InternalModel.Interlis.Name } }) }
                    },
                    Content = { { "Thema", thema } },
                };

                return new InterlisEnvironment
                {
                    Version = 2.4,
                    Content =
                    {
                        { InternalModel.Interlis.Name, InternalModel.Interlis },
                        { "Deutsch", deutsch },
                        {
                            "English",
                            new ModelDef
                            {
                                Name = "English",
                                Language = "en",
                                URI = "http://example.com",
                                Version = "1.0.0",
                                TranslationOf = new Reference<IInterlisDefinition> { Target = deutsch, Path = { "Deutsch" } },
                                TranslationOfVersion = "1.0.0",
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
                                            TranslationOf = new Reference<IInterlisDefinition> { Target = thema },
                                            Content =
                                            {
                                                {
                                                    "Item",
                                                    new ClassDef
                                                    {
                                                        Name = "Item",
                                                        TranslationOf = new Reference<IInterlisDefinition> { Target = klasse },
                                                    }
                                                }
                                            },
                                        }
                                    }
                                },
                            }
                        },
                    }
                };
            })));

        yield return FullFile(new(
            "Model translation of wrong version",
            """
            INTERLIS 2.4;
            MODEL Deutsch (de_CH) AT "http://example.com" VERSION "1.0.0" = END Deutsch.
            MODEL English (en) AT "http://example.com" VERSION "0.0.5" TRANSLATION OF Deutsch [ "0.0.5" ] = END English.
            """,
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Model translation of missing version",
            """
            INTERLIS 2.4;
            MODEL Deutsch (de_CH) AT "http://example.com" VERSION "1.0.0" = END Deutsch.
            MODEL English (en) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Deutsch = END English.
            """,
            ExpectedLog: ["Compile error at 3:82-3:83 mismatched input '=' expecting '['."],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated topic declares OID domains the original lacks",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Thema =
                    CLASS Klasse = END Klasse;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                TOPIC Theme =
                    BASKET OID AS INTERLIS.UUIDOID;
                    OID AS INTERLIS.UUIDOID;
                    CLASS Classe = END Classe;
                END Theme;
            END Translated.
            """,
            Description: """
                A translation may only change names, so a translated topic must declare the same BASKET OID / OID
                domains as the original topic (ili2c agrees: "The types of the BID domains do not match."). Domains are
                compared through translation chains, so a topic using a translated OID domain still matches the original
                topic using the original domain.
                """,
            ExpectedLog:
            [
                "Type check error in 'Translated.Theme' at 8:4-12:14: the BASKET OID domain does not match the original topic's.",
                "Type check error in 'Translated.Theme' at 8:4-12:14: the OID domain does not match the original topic's.",
            ],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated topic declares a different BASKET OID domain",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Thema =
                    BASKET OID AS INTERLIS.STANDARDOID;
                    OID AS INTERLIS.STANDARDOID;
                    CLASS Klasse = END Klasse;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                TOPIC Theme =
                    BASKET OID AS INTERLIS.UUIDOID;
                    OID AS INTERLIS.STANDARDOID;
                    CLASS Classe = END Classe;
                END Theme;
            END Translated.
            """,
            ExpectedLog: ["Type check error in 'Translated.Theme' at 10:4-14:14: the BASKET OID domain does not match the original topic's."],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated topic declares the matching OID domains",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Thema =
                    BASKET OID AS INTERLIS.UUIDOID;
                    OID AS INTERLIS.UUIDOID;
                    CLASS Klasse = END Klasse;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                TOPIC Theme =
                    BASKET OID AS INTERLIS.UUIDOID;
                    OID AS INTERLIS.UUIDOID;
                    CLASS Classe = END Classe;
                END Theme;
            END Translated.
            """,
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated topic uses the translated OID domain",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Kennung = OID TEXT*20;
                TOPIC Thema =
                    OID AS Kennung;
                    CLASS Klasse = END Klasse;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                DOMAIN
                    Identifiant = OID TEXT*20;
                TOPIC Theme =
                    OID AS Identifiant;
                    CLASS Classe = END Classe;
                END Theme;
            END Translated.
            """,
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated model imports a model the original does not",
            """
            INTERLIS 2.4;
            MODEL Extra (de) AT "http://example.com" VERSION "1.0.0" = END Extra.
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" = END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                IMPORTS Extra;
            END Translated.
            """,
            Description: """
                A translation may only change names, so the model must import the same models as the original (ili2c
                agrees: "The imported models do not match."). Imports are compared through translation chains and
                independently of their declaration order.
                """,
            ExpectedLog: ["Type check error in 'Translated' at 4:0-6:15: the imported models do not match the original model's."],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated model imports the translation of the model the original imports",
            """
            INTERLIS 2.4;
            MODEL Base (de) AT "http://example.com" VERSION "1.0.0" = END Base.
            MODEL BaseFr (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Base ["1.0.0"] = END BaseFr.
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                IMPORTS Base;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                IMPORTS BaseFr;
            END Translated.
            """,
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated model declares the original's imports in a different order",
            """
            INTERLIS 2.4;
            MODEL BaseA (de) AT "http://example.com" VERSION "1.0.0" = END BaseA.
            MODEL BaseB (de) AT "http://example.com" VERSION "1.0.0" = END BaseB.
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                IMPORTS BaseA, BaseB;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                IMPORTS BaseB, BaseA;
            END Translated.
            """,
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated topic declares fewer classes than the original",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Thema =
                    CLASS Klasse1 = END Klasse1;
                    CLASS Klasse2 = END Klasse2;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                TOPIC Theme =
                    CLASS Classe1 = END Classe1;
                END Theme;
            END Translated.
            """,
            Description: """
                The elements of a translated container correspond to the original's elements by declaration position, so
                the containers must declare the same number of elements and each pair must be the same kind of
                definition (ili2c agrees: "The number of elements in ... do not match." / "There is a mismatch between
                ... and ...").
                """,
            ExpectedLog: ["Type check error in 'Translated.Theme' at 9:4-11:14: the number of elements does not match the original 'Original.Thema'."],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated model declares a domain where the original declares a topic",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Art = (a, b);
                TOPIC Thema =
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                DOMAIN
                    Genre = (a, b);
                DOMAIN
                    Sorte = (a, b);
            END Translated.
            """,
            ExpectedLog: ["Type check error in 'Translated.Sorte' at 12:8-12:23: must be the same kind of definition as the original 'Original.Thema'."],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated model mirrors the original's structure",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                DOMAIN
                    Art = (a, b);
                TOPIC Thema =
                    CLASS Klasse =
                        Attribut : TEXT*10;
                    END Klasse;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                DOMAIN
                    Genre = (a, b);
                TOPIC Theme =
                    CLASS Classe =
                        Attribut : TEXT*10;
                    END Classe;
                END Theme;
            END Translated.
            """,
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated class declares more constraints than the original",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Thema =
                    CLASS Klasse =
                        Attribut : TEXT*10;
                        MANDATORY CONSTRAINT DEFINED (Attribut);
                    END Klasse;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                TOPIC Theme =
                    CLASS Classe =
                        Attribut : TEXT*10;
                        MANDATORY CONSTRAINT DEFINED (Attribut);
                        MANDATORY CONSTRAINT Attribut <> "";
                    END Classe;
                END Theme;
            END Translated.
            """,
            Description: """
                The constraints of a translated element correspond to the original's by position too (ili2c agrees: "The
                number of elements in ... do not match." — it counts constraints as container elements).
                """,
            ExpectedLog: ["Type check error in 'Translated.Theme.Classe' at 12:8-16:19: the number of constraints does not match the original 'Original.Thema.Klasse'."],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated class declares the original's constraints",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Thema =
                    CLASS Klasse =
                        Attribut : TEXT*10;
                        MANDATORY CONSTRAINT DEFINED (Attribut);
                        UNIQUE Attribut;
                    END Klasse;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                TOPIC Theme =
                    CLASS Classe =
                        Attribut : TEXT*10;
                        MANDATORY CONSTRAINT DEFINED (Attribut);
                        UNIQUE Attribut;
                    END Classe;
                END Theme;
            END Translated.
            """,
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated class declares a different kind of constraint than the original",
            """
            INTERLIS 2.4;
            MODEL Original (de) AT "http://example.com" VERSION "1.0.0" =
                TOPIC Thema =
                    CLASS Klasse =
                        Attribut : TEXT*10;
                        MANDATORY CONSTRAINT DEFINED (Attribut);
                    END Klasse;
                END Thema;
            END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] =
                TOPIC Theme =
                    CLASS Classe =
                        Attribut : TEXT*10;
                        UNIQUE Attribut;
                    END Classe;
                END Theme;
            END Translated.
            """,
            ExpectedLog: ["Type check error in 'Translated.Theme.Classe' at 12:8-15:19: the constraint 'Constraint1' must be the same kind of constraint as the original's 'Constraint1'."],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return FullFile(new(
            "Translated model is not CONTRACTED like the original",
            """
            INTERLIS 2.4;
            CONTRACTED MODEL Original (de) AT "http://example.com" VERSION "1.0.0" = END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] = END Translated.
            """,
            Description: """
                RefHB 3.5.1-10 / ili2c Model.checkTranslationOf: the model kind (TYPE, REFSYSTEM, SYMBOLOGY) is part
                of the mirrored structure.
                """,
            RefHB: "3.5.1-12",
            AssertOutput: false,
            Ili2cDivergenceReason: "ili2c requires a translation to repeat the original's CONTRACTED declaration — a leftover INTERLIS 2.3 contract rule; RefHB 3.5.1-12 states CONTRACTED has no function anymore and is kept only for compatibility, so we accept the mismatch."));

        yield return FullFile(new(
            "Translated model is not a TYPE model like the original",
            """
            INTERLIS 2.4;
            TYPE MODEL Original (de) AT "http://example.com" VERSION "1.0.0" = END Original.
            MODEL Translated (fr) AT "http://example.com" VERSION "1.0.0" TRANSLATION OF Original ["1.0.0"] = END Translated.
            """,
            ExpectedLog: ["Type check error in 'Translated' at 3:0-3:113: the model kind does not match the original model's."],
            RefHB: "3.5.1-10",
            AssertOutput: false));

        yield return Rule(new(
            "Contracted model",
            """
            CONTRACTED MODEL Model AT "http://example.com" VERSION "1.0.0" =
            END Model.
            """,
            RefHB: "3.5.1-12",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 17, 0, 22), new RangePosition(1, 4, 1, 9) },
                Imports =
                {
                    { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) },
                },
                URI = "http://example.com",
                Version = "1.0.0",
            }));

        yield return Rule(new(
            "Model with explicit charset",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" = CHARSET "UTF-8";
            END Model.
            """,
            RefHB: "3.5.1-13",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                URI = "http://example.com",
                Version = "1.0.0",
                Charset = "UTF-8",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Model with explicit XML namespace",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" = XMLNS "https://www.example.com/awesomenamespace";
            END Model.
            """,
            RefHB: "3.5.1-14",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                URI = "http://example.com",
                Version = "1.0.0",
                Xmlns = "https://www.example.com/awesomenamespace",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Extraneous '=' before XMLNS string",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" = XMLNS = "https://www.example.com/awesomenamespace";
            END Model.
            """,
            ExpectedLog: ["Compile error at 1:60-1:61 extraneous input '=' expecting DOUBLE_QUOTE_OPEN."],
            RefHB: "3.5.1-14",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations =
                {
                    new RangePosition(0, 6, 0, 11),
                    new RangePosition(1, 4, 1, 9)
                },
                URI = "http://example.com",
                Version = "1.0.0",
                Xmlns = "https://www.example.com/awesomenamespace",
                Imports =
                {
                    { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Path = { InternalModel.Interlis.Name } }) }
                },
            }));

        yield return Rule(new(
            "Model with qualified import",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              IMPORTS Other;
            END Model.
            """,
            RefHB: "3.5.1-15",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(2, 4, 2, 9) },
                Imports =
                {
                    { "Other", (false, new Reference<ModelDef> { Path = { "Other" }, SourceRange = new RangePosition(1, 10, 1, 15) }) },
                    { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) },
                },
                URI = "http://example.com",
                Version = "1.0.0",
            }));

        yield return Rule(new(
            "Model with multiple imports mixed qualification",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              IMPORTS OtherA, UNQUALIFIED OtherB;
            END Model.
            """,
            RefHB: "3.5.1-15",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(2, 4, 2, 9) },
                Imports =
                {
                    { "OtherA", (false, new Reference<ModelDef> { Path = { "OtherA" }, SourceRange = new RangePosition(1, 10, 1, 16) }) },
                    { "OtherB", (true, new Reference<ModelDef> { Path = { "OtherB" }, SourceRange = new RangePosition(1, 30, 1, 36) }) },
                    { "INTERLIS", (false, new Reference<ModelDef> { Path = { "INTERLIS" } }) },
                },
                URI = "http://example.com",
                Version = "1.0.0",
            }));

        yield return Rule(new(
            "Model with unqualified INTERLIS import",
            """
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
              IMPORTS UNQUALIFIED INTERLIS;
            END Model.
            """,
            RefHB: "3.5.1-16",
            Expected: new ModelDef
            {
                Name = "Model",
                NameLocations = { new RangePosition(0, 6, 0, 11), new RangePosition(2, 4, 2, 9) },
                Imports =
                {
                    { "INTERLIS", (true, new Reference<ModelDef> { Path = { "INTERLIS" }, SourceRange = new RangePosition(1, 22, 1, 30) }) },
                },
                URI = "http://example.com",
                Version = "1.0.0",
            }));
    }

    private static string Wrap(string fragment) => $"""
        INTERLIS 2.4;
        {fragment}
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(ModelTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadModelDef(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => v.VisitModelDef(p.modelDef()));
    }
}
