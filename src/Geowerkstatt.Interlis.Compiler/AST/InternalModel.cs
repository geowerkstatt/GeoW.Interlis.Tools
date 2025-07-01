using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Internal INTERLIS model.
/// </summary>
public static class InternalModel
{
    public static readonly ModelDef Interlis;

    static InternalModel()
    {
        Interlis = new ModelDef { Name = "INTERLIS" };
        Interlis.AddContent([
            () => new UnitDef { Name = "ANYUNIT", Term = "ANYUNIT", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "DIMENSIONLESS", Term = "DIMENSIONLESS", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "LENGTH", Term = "LENGTH", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "MASS", Term = "MASS", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "TIME", Term = "TIME", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "ELECTRIC_CURRENT", Term = "ELECTRIC_CURRENT", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "TEMPERATURE", Term = "TEMPERATURE", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "AMOUNT_OF_MATTER", Term = "AMOUNT_OF_MATTER", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "ANGLE", Term = "ANGLE", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "SOLID_ANGLE", Term = "SOLID_ANGLE", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "LUMINOUS_INTENSITY", Term = "LUMINOUS_INTENSITY", Properties = { Property.Abstract } },
            () => new UnitDef { Name = "MONEY", Term = "MONEY", Properties = { Property.Abstract } },

            () => new UnitDef { Name = "m", Term = "METER", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["LENGTH"] } },
            () => new UnitDef { Name = "kg", Term = "KILOGRAM", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["MASS"] } },
            () => new UnitDef { Name = "s", Term = "SECOND", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["TIME"] } },
            () => new UnitDef { Name = "A", Term = "AMPERE", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["ELECTRIC_CURRENT"] } },
            () => new UnitDef { Name = "K", Term = "DEGREE_KELVIN", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["TEMPERATURE"] } },
            () => new UnitDef { Name = "mol", Term = "MOLE", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["AMOUNT_OF_MATTER"] } },
            () => new UnitDef { Name = "rad", Term = "RADIAN", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["ANGLE"] } },
            () => new UnitDef { Name = "sr", Term = "STERADIAN", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["SOLID_ANGLE"] } },
            () => new UnitDef { Name = "cd", Term = "CANDELA", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["LUMINOUS_INTENSITY"] } },

            () => new DomainDef
            {
                Name = "URI",
                Properties = { Property.Final },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Length = 1023,
                },
            },
            () => new DomainDef
            {
                Name = "NAME",
                Properties = { Property.Final },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Length = 255,
                },
            },
            () => new DomainDef
            {
                Name = "INTERLIS_1_DATE",
                Properties = { Property.Final },
                TypeDef = new TextType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Length = 8,
                },
            },
            () => new DomainDef
            {
                Name = "BOOLEAN",
                Properties = { Property.Final },
                TypeDef = new EnumerationType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Sequencing = EnumerationType.Sequencings.Ordered,
                    Values =
                    {
                        new EnumerationTreeNode { Name = "false" },
                        new EnumerationTreeNode { Name = "true" },
                    },
                },
            },
            () => new DomainDef
            {
                Name = "HALIGNMENT",
                Properties = { Property.Final },
                TypeDef = new EnumerationType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Sequencing = EnumerationType.Sequencings.Ordered,
                    Values =
                    {
                        new EnumerationTreeNode { Name = "Left" },
                        new EnumerationTreeNode { Name = "Center" },
                        new EnumerationTreeNode { Name = "Right" },
                    },
                },
            },
            () => new DomainDef
            {
                Name = "VALIGNMENT",
                Properties = { Property.Final },
                TypeDef = new EnumerationType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Sequencing = EnumerationType.Sequencings.Ordered,
                    Values =
                    {
                        new EnumerationTreeNode { Name = "Top" },
                        new EnumerationTreeNode { Name = "Cap" },
                        new EnumerationTreeNode { Name = "Half" },
                        new EnumerationTreeNode { Name = "Base" },
                        new EnumerationTreeNode { Name = "Bottom" },
                    },
                },
            },

            () => new UnitDef
            {
                Name = "min",
                Term = "Minute",
                Expression = new Multiplication
                {
                    FirstOperand = new NumericConstant { Value = 60 },
                    SecondOperand = new PathExpression { Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Target = (UnitDef)Interlis.Content["s"] } } } }
                },
            },
            () => new UnitDef
            {
                Name = "h",
                Term = "Hour",
                Expression = new Multiplication
                {
                    FirstOperand = new NumericConstant { Value = 60 },
                    SecondOperand = new PathExpression { Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Target = (UnitDef)Interlis.Content["min"] } } } }
                },
            },
            () => new UnitDef
            {
                Name = "d",
                Term = "Day",
                Expression = new Multiplication
                {
                    FirstOperand = new NumericConstant { Value = 24 },
                    SecondOperand = new PathExpression { Path = { new ReferencePathElement { Value = new Reference<IInterlisDefinition> { Target = (UnitDef)Interlis.Content["h"] } } } }
                },
            },
            () => new UnitDef { Name = "M", Term = "Month", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["TIME"] } },
            () => new UnitDef { Name = "Y", Term = "Year", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["TIME"] } },

            () => new ClassDef
            {
                Name = "TimeOfDay",
                IsStructure = true,
                Content =
                {
                    {
                        "Hours",
                        new AttributeDef
                        {
                            Name = "Hours",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 23,
                                Precision = 0,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["h"] },
                                Circular = true,
                            },
                        }
                    },
                    {
                        "Minutes",
                        new AttributeDef
                        {
                            Name = "Minutes",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 59,
                                Precision = 0,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["min"] },
                                Circular = true,
                            },
                        }
                    },
                    {
                        "Seconds",
                        new AttributeDef
                        {
                            Name = "Seconds",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 59.999,
                                Precision = -3,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["s"] },
                                Circular = true,
                            },
                        }
                    },
                }
            },

            () => new ClassDef
            {
                Name = "UTC",
                IsStructure = true,
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["TimeOfDay"] },
                Content =
                {
                    {
                        "Hours",
                        new AttributeDef
                        {
                            Name = "Hours",
                            Properties = { Property.Extended },
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 23,
                                Precision = 0,
                            },
                        }
                    },
                }
            },

            () => new DomainDef
            {
                Name = "GregorianYear",
                TypeDef = new NumericType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Min = 1582,
                    Max = 2999,
                    Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["Y"] },
                },
            },
            () => new ClassDef
            {
                Name = "GregorianDate",
                IsStructure = true,
                Content =
                {
                    {
                        "Year",
                        new AttributeDef
                        {
                            Name = "Year",
                            TypeDef = new ReferenceType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = Interlis.Content["GregorianYear"] } },
                            }
                        }
                    },
                    {
                        "Month",
                        new AttributeDef
                        {
                            Name = "Month",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 1,
                                Max = 12,
                                Precision = 0,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["M"] },
                            }
                        }
                    },
                    {
                        "Day",
                        new AttributeDef
                        {
                            Name = "Day",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 1,
                                Max = 31,
                                Precision = 0,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["d"] },
                            }
                        }
                    },
                }
            },

            () => new ClassDef
            {
                Name = "GregorianDateTime",
                IsStructure = true,
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["GregorianDate"] },
                Content =
                {
                    {
                        "Hours",
                        new AttributeDef
                        {
                            Name = "Hours",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 23,
                                Precision = 0,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["h"] },
                                Circular = true,
                            },
                        }
                    },
                    {
                        "Minutes",
                        new AttributeDef
                        {
                            Name = "Minutes",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 59,
                                Precision = 0,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["min"] },
                                Circular = true,
                            },
                        }
                    },
                    {
                        "Seconds",
                        new AttributeDef
                        {
                            Name = "Seconds",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 59.999,
                                Precision = -3,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["s"] },
                                Circular = true,
                            },
                        }
                    },
                }
            },

            () => new DomainDef
            {
                Name = "XMLTime",
                TypeDef = new FormattedType
                {
                    BasedOn = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["UTC"] },
                }
            },
            () => new DomainDef
            {
                Name = "XMLDate",
                TypeDef = new FormattedType
                {
                    BasedOn = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["GregorianDate"] },
                }
            },
            () => new DomainDef
            {
                Name = "XMLDateTime",
                TypeDef = new FormattedType
                {
                    Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["XMLDate"] },
                    BasedOn = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["GregorianDateTime"] },
                }
            },
        ]);
    }

    private static void AddContent(this IInterlisDefinitionContainer container, IEnumerable<Func<IInterlisDefinition>> elements)
    {
        foreach (var elementSupplier in elements)
        {
            var element = elementSupplier();
            element.Parent = container;
            container.Content.Add(element.Name, element);
        }
    }
}
