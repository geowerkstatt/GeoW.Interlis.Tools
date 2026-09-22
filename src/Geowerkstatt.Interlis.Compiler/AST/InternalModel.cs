using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;

namespace Geowerkstatt.Interlis.Compiler.AST;

/// <summary>
/// Internal INTERLIS model (RefHB Annex A). The definitions follow the order of the annex.
/// </summary>
public static class InternalModel
{
    public static readonly ModelDef Interlis;

    static InternalModel()
    {
        Interlis = new ModelDef
        {
            Name = "INTERLIS",
            Type = ModelDef.ModelType.Type,
            Language = "en",
            URI = "http://www.interlis.ch/",
            Version = "2014-07-09",
        };
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

            // Predefined OID value domains (RefHB 3.8.9).
            () => new DomainDef
            {
                Name = "NOOID",
                TypeDef = new OidType { Value = new OidType.NoOid() },
            },
            () => new DomainDef
            {
                Name = "ANYOID",
                Properties = { Property.Abstract },
                TypeDef = new OidType
                {
                    Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["NOOID"] },
                    Value = new OidType.AnyOid(),
                },
            },
            () => new DomainDef
            {
                Name = "I32OID",
                TypeDef = new OidType
                {
                    Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["ANYOID"] },
                    Value = new OidType.ValueRange { Type = new DecimalType { Min = 0, Max = 2147483647, Precision = 0 } },
                },
            },
            () => new DomainDef
            {
                Name = "STANDARDOID",
                TypeDef = new OidType
                {
                    Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["ANYOID"] },
                    Value = new OidType.ValueRange { Type = new TextType { Length = 16 } },
                },
            },
            () => new DomainDef
            {
                Name = "UUIDOID",
                TypeDef = new OidType
                {
                    Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["ANYOID"] },
                    Value = new OidType.ValueRange { Type = new TextType { Length = 36 } },
                },
            },

            // Abstract coordinate domain that the predefined geometry helper structures build on.
            () => new DomainDef
            {
                Name = "LineCoord",
                Properties = { Property.Abstract },
                TypeDef = new CoordType
                {
                    Axis = { new NumericType(), new NumericType() },
                },
            },

            // Predefined standard functions (RefHB 3.14 / Annex C). Argument types reuse the same lossy
            // representations the parser produces (e.g. OBJECT(S) OF and ENUMVAL/ENUMTREEVAL collapse to
            // ObjectType/EnumerationType); the return type is what expressions read via FunctionCall.ReturnType.
            () => new FunctionDef
            {
                Name = "myClass",
                ReturnType = new ClassType { IsStructure = true },
                Arguments =
                {
                    new FunctionArgument { Name = "Object", Type = new ObjectType { Cardinality = new Cardinality { Min = 1, Max = 1 } } },
                },
            },
            () => new FunctionDef
            {
                Name = "isSubClass",
                ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "potSubClass", Type = new ClassType { IsStructure = true } },
                    new FunctionArgument { Name = "potSuperClass", Type = new ClassType { IsStructure = true } },
                },
            },
            () => new FunctionDef
            {
                Name = "isOfClass",
                ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "Object", Type = new ObjectType { Cardinality = new Cardinality { Min = 1, Max = 1 } } },
                    new FunctionArgument { Name = "Class", Type = new ClassType { IsStructure = true } },
                },
            },
            () => new FunctionDef
            {
                Name = "elementCount",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    // bag: BAG OF ANYSTRUCTURE
                    new FunctionArgument
                    {
                        Name = "bag",
                        Type = new UnresolvedNamedType
                        {
                            Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                            Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                        },
                    },
                },
            },
            () => new FunctionDef
            {
                Name = "objectCount",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "Objects", Type = new ObjectType { Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }], Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } } },
                },
            },
            () => new FunctionDef
            {
                Name = "len",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "TextVal", Type = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                },
            },
            () => new FunctionDef
            {
                Name = "lenM",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "TextVal", Type = new TextType { IsMText = true, Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                },
            },
            () => new FunctionDef
            {
                Name = "trim",
                ReturnType = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "TextVal", Type = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                },
            },
            () => new FunctionDef
            {
                Name = "trimM",
                ReturnType = new TextType { IsMText = true, Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "TextVal", Type = new TextType { IsMText = true, Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                },
            },
            () => new FunctionDef
            {
                Name = "isEnumSubVal",
                ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "SubVal", Type = new EnumerationValuesType { LeafsOnly = false } },
                    new FunctionArgument { Name = "NodeVal", Type = new EnumerationValuesType { LeafsOnly = false } },
                },
            },
            () => new FunctionDef
            {
                Name = "inEnumRange",
                ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "Enum", Type = new EnumerationValuesType { LeafsOnly = true } },
                    new FunctionArgument { Name = "MinVal", Type = new EnumerationValuesType { LeafsOnly = false } },
                    new FunctionArgument { Name = "MaxVal", Type = new EnumerationValuesType { LeafsOnly = false } },
                },
            },
            () => new FunctionDef
            {
                Name = "convertUnit",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "from", Type = new NumericType() },
                },
            },
            () => new FunctionDef
            {
                Name = "length",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "geom", Type = new PolyLineType() },
                },
            },
            () => new FunctionDef
            {
                Name = "multilength",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "geom", Type = new PolyLineType { IsMultiGeometry = true } },
                },
            },
            () => new FunctionDef
            {
                Name = "surface",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "geom", Type = new SurfaceType() },
                },
            },
            () => new FunctionDef
            {
                Name = "multisurface",
                ReturnType = new NumericType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "geom", Type = new SurfaceType { IsMultiGeometry = true } },
                },
            },
            () => new FunctionDef
            {
                Name = "areAreas",
                ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "Objects", Type = new ObjectType { Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }], Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } } },
                    // SurfaceBag: ATTRIBUTE OF @ Objects RESTRICTION (BAG OF ANYSTRUCTURE)
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
                                    Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                                    Target = new RestrictedRef { Value = RestrictedRef.AnyKind.Structure },
                                },
                            },
                        },
                    },
                    // SurfaceAttr: ATTRIBUTE OF @ SurfaceBag RESTRICTION (SURFACE)
                    new FunctionArgument
                    {
                        Name = "SurfaceAttr",
                        Type = new AttributePathType
                        {
                            ArgumentName = "SurfaceBag",
                            Restrictions = { new SurfaceType() },
                        },
                    },
                },
            },
            () => new FunctionDef
            {
                Name = "areAreas2",
                ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "Object", Type = new ObjectType { Cardinality = new Cardinality { Min = 1, Max = 1 } } },
                    new FunctionArgument { Name = "SurfaceBag", Type = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                    new FunctionArgument { Name = "SurfaceAttr", Type = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                },
            },
            () => new FunctionDef
            {
                Name = "areAreas3",
                ReturnType = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                Arguments =
                {
                    new FunctionArgument { Name = "Objects", Type = new ObjectType { Targets = [new RestrictedRef { Value = RestrictedRef.AnyKind.Class }], Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound } } },
                    new FunctionArgument { Name = "SurfaceBag", Type = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                    new FunctionArgument { Name = "SurfaceAttr", Type = new TextType { Cardinality = new Cardinality { Min = 0, Max = 1 } } },
                },
            },

            // Meta-object base classes (RefHB 3.10). METAOBJECT is the abstract root of every meta object
            // (reference-system objects and symbols); user symbology/refsystem models extend the classes below.
            () => CreateMetaObject(),
            () => CreateMetaObjectTranslation(),
            () => new ClassDef
            {
                Name = "AXIS",
                IsStructure = true,
                Content =
                {
                    {
                        "Unit",
                        new ParameterDef
                        {
                            Name = "Unit",
                            TypeDef = new NumericType
                            {
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["ANYUNIT"] },
                            },
                        }
                    },
                },
            },
            () => new ClassDef
            {
                Name = "REFSYSTEM",
                Properties = { Property.Abstract },
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["METAOBJECT"] },
            },
            () => new ClassDef
            {
                Name = "COORDSYSTEM",
                Properties = { Property.Abstract },
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["REFSYSTEM"] },
                Content =
                {
                    {
                        "Axis",
                        new AttributeDef
                        {
                            Name = "Axis",
                            TypeDef = new ObjectType
                            {
                                Cardinality = new Cardinality { Min = 1, Max = 3, Ordered = true },
                                Targets = [new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = Interlis.Content["AXIS"] } }],
                            },
                        }
                    },
                },
            },
            () => new ClassDef
            {
                Name = "SCALSYSTEM",
                Properties = { Property.Abstract },
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["REFSYSTEM"] },
                Content =
                {
                    {
                        "Unit",
                        new ParameterDef
                        {
                            Name = "Unit",
                            TypeDef = new NumericType
                            {
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["ANYUNIT"] },
                            },
                        }
                    },
                },
            },
            () => new ClassDef
            {
                Name = "SIGN",
                Properties = { Property.Abstract },
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["METAOBJECT"] },
                Content =
                {
                    // A plain METAOBJECT parameter references the signature class it is defined in (RefHB 3.10.2.2),
                    // i.e. SIGN itself; left with no explicit target to avoid a self-reference during construction.
                    {
                        "Sign",
                        new ParameterDef
                        {
                            Name = "Sign",
                            IsMetaObject = true,
                        }
                    },
                },
            },

            // The predefined time reference systems (RefHB 3.10.3).
            () => CreateTimeSystemsTopic(),

            () => new UnitDef
            {
                Name = "min",
                Term = "Minute",
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new NumericConstant { Value = 60 },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["s"] } }
                },
            },
            () => new UnitDef
            {
                Name = "h",
                Term = "Hour",
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new NumericConstant { Value = 60 },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["min"] } }
                },
            },
            () => new UnitDef
            {
                Name = "d",
                Term = "Day",
                Expression = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new NumericConstant { Value = 24 },
                    SecondOperand = new UnitReferenceExpression { Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["h"] } }
                },
            },
            () => new UnitDef { Name = "M", Term = "Month", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["TIME"] } },
            () => new UnitDef { Name = "Y", Term = "Year", Extends = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["TIME"] } },

            // REFSYSTEM BASKET BaseTimeSystems ~ TIMESYSTEMS: declares the meta objects the time domains
            // below refer to via {GregorianCalendar} / {UTC}.
            () => new MetaDataBasketDef
            {
                Name = "BaseTimeSystems",
                Kind = MetaDataBasketDef.BasketKind.Refsystem,
                Topic = new Reference<TopicDef> { Target = (TopicDef)Interlis.Content["TIMESYSTEMS"] },
                Objects =
                {
                    new MetaObjectsClause { Class = new Reference<ClassDef> { Path = { new("CALENDAR") } }, MetaObjects = { new MetaObjectDeclaration { Name = "GregorianCalendar" } } },
                    new MetaObjectsClause { Class = new Reference<ClassDef> { Path = { new("TIMEOFDAYSYS") } }, MetaObjects = { new MetaObjectDeclaration { Name = "UTC" } } },
                },
            },

            () => new ClassDef
            {
                Name = "TimeOfDay",
                IsStructure = true,
                Properties = { Property.Abstract },
                Content =
                {
                    {
                        "Hours",
                        new AttributeDef
                        {
                            Name = "Hours",
                            TypeDef = new DecimalType
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
                            Subdivision = AttributeDef.SubdivisionKind.ContinuousSubdivision,
                            TypeDef = new DecimalType
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
                            Subdivision = AttributeDef.SubdivisionKind.ContinuousSubdivision,
                            TypeDef = new DecimalType
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
                            TypeDef = new DecimalType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 23,
                                Precision = 0,
                                RefSystem = new RefSys { Value = new RefSys.MetaObjectRef { MetaObject = new Reference<MetaObjectDeclaration> { Path = { new("UTC") } } } },
                            },
                        }
                    },
                }
            },

            () => new DomainDef
            {
                Name = "GregorianYear",
                TypeDef = new DecimalType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Min = 1582,
                    Max = 2999,
                    Precision = 0,
                    Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["Y"] },
                    RefSystem = new RefSys { Value = new RefSys.MetaObjectRef { MetaObject = new Reference<MetaObjectDeclaration> { Path = { new("GregorianCalendar") } } } },
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
                            TypeDef = new TypeRef
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["GregorianYear"] },
                            }
                        }
                    },
                    {
                        "Month",
                        new AttributeDef
                        {
                            Name = "Month",
                            Subdivision = AttributeDef.SubdivisionKind.Subdivision,
                            TypeDef = new DecimalType
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
                            Subdivision = AttributeDef.SubdivisionKind.Subdivision,
                            TypeDef = new DecimalType
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
                            Subdivision = AttributeDef.SubdivisionKind.Subdivision,
                            TypeDef = new DecimalType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Min = 0,
                                Max = 23,
                                Precision = 0,
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["h"] },
                                Circular = true,
                                RefSystem = new RefSys { Value = new RefSys.MetaObjectRef { MetaObject = new Reference<MetaObjectDeclaration> { Path = { new("UTC") } } } },
                            },
                        }
                    },
                    {
                        "Minutes",
                        new AttributeDef
                        {
                            Name = "Minutes",
                            Subdivision = AttributeDef.SubdivisionKind.ContinuousSubdivision,
                            TypeDef = new DecimalType
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
                            Subdivision = AttributeDef.SubdivisionKind.ContinuousSubdivision,
                            TypeDef = new DecimalType
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
                    Format = new FormatDef
                    {
                        Components =
                        {
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Hours") } }, Position = 2 },
                            new FormatSeparator { Value = ":" },
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Minutes") } }, Position = 2 },
                            new FormatSeparator { Value = ":" },
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Seconds") } }, Position = 2 },
                        },
                    },
                }
            },
            () => new DomainDef
            {
                Name = "XMLDate",
                TypeDef = new FormattedType
                {
                    BasedOn = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["GregorianDate"] },
                    Format = new FormatDef
                    {
                        Components =
                        {
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Year") } }, Position = 4 },
                            new FormatSeparator { Value = "-" },
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Month") } }, Position = 2 },
                            new FormatSeparator { Value = "-" },
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Day") } }, Position = 2 },
                        },
                    },
                }
            },
            () => new DomainDef
            {
                Name = "XMLDateTime",
                TypeDef = new FormattedType
                {
                    Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["XMLDate"] },
                    BasedOn = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["GregorianDateTime"] },
                    Format = new FormatDef
                    {
                        Inheritance = true,
                        Components =
                        {
                            new FormatSeparator { Value = "T" },
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Hours") } }, Position = 2 },
                            new FormatSeparator { Value = ":" },
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Minutes") } }, Position = 2 },
                            new FormatSeparator { Value = ":" },
                            new FormatBaseAttribute { Attribute = new Reference<AttributeDef> { Path = { new("Seconds") } }, Position = 2 },
                        },
                    },
                }
            },

            // Geometry helper structures used by the predefined line and surface types.
            () => new ClassDef
            {
                Name = "LineSegment",
                IsStructure = true,
                Properties = { Property.Abstract },
                Content =
                {
                    {
                        "SegmentEndPoint",
                        new AttributeDef
                        {
                            Name = "SegmentEndPoint",
                            TypeDef = new TypeRef
                            {
                                Cardinality = new Cardinality { Min = 1, Max = 1 },
                                Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["LineCoord"] },
                            },
                        }
                    },
                },
            },
            () => new ClassDef
            {
                Name = "StartSegment",
                IsStructure = true,
                Properties = { Property.Final },
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["LineSegment"] },
            },
            () => new ClassDef
            {
                Name = "StraightSegment",
                IsStructure = true,
                Properties = { Property.Final },
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["LineSegment"] },
            },
            () => new ClassDef
            {
                Name = "ArcSegment",
                IsStructure = true,
                Properties = { Property.Final },
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["LineSegment"] },
                Content =
                {
                    {
                        "ArcPoint",
                        new AttributeDef
                        {
                            Name = "ArcPoint",
                            TypeDef = new TypeRef
                            {
                                Cardinality = new Cardinality { Min = 1, Max = 1 },
                                Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["LineCoord"] },
                            },
                        }
                    },
                    {
                        "Radius",
                        new AttributeDef
                        {
                            Name = "Radius",
                            TypeDef = new NumericType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = 1 },
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["LENGTH"] },
                            },
                        }
                    },
                },
            },
            // Predefined line forms (RefHB 3.8.12.1). The STRAIGHTS/ARCS keywords in a line type's WITH (...)
            // list become references to these definitions, uniform with custom line forms (RefHB 3.8.12.3).
            () => new LineFormTypeDef
            {
                Name = "STRAIGHTS",
                Structure = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["StraightSegment"], Path = { new("INTERLIS"), new("StraightSegment") } },
            },
            () => new LineFormTypeDef
            {
                Name = "ARCS",
                Structure = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["ArcSegment"], Path = { new("INTERLIS"), new("ArcSegment") } },
            },

            () => new ClassDef
            {
                Name = "SurfaceEdge",
                IsStructure = true,
                Content =
                {
                    {
                        "Geometry",
                        new AttributeDef
                        {
                            Name = "Geometry",
                            TypeDef = new PolyLineType
                            {
                                Cardinality = new Cardinality { Min = 1, Max = 1 },
                                IsDirected = true,
                            },
                        }
                    },
                },
            },
            () => new ClassDef
            {
                Name = "SurfaceBoundary",
                IsStructure = true,
                Content =
                {
                    {
                        "Lines",
                        new AttributeDef
                        {
                            Name = "Lines",
                            TypeDef = new ObjectType
                            {
                                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound, Ordered = true },
                                Targets = [new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = Interlis.Content["SurfaceEdge"] } }],
                            },
                        }
                    },
                },
            },
            () => CreateLineGeometry(),
        ]);
    }

    /// <summary>
    /// A one-step object path naming <paramref name="target"/>, for the hand-built constraints below. It carries the
    /// name and the already known target, but no span: the predefined model has no source to point at, and the path
    /// resolver skips it.
    /// </summary>
    private static PathExpression PathTo(IInterlisDefinition target) => new()
    {
        Reference = new Reference<IInterlisDefinition> { Path = { new PathSegment { Name = target.Name, Target = target } }, Target = target, Resolution = ReferenceResolution.ObjectPath },
    };

    /// <summary>
    /// <c>CLASS METAOBJECT (ABSTRACT) = Name: MANDATORY NAME; UNIQUE Name; END METAOBJECT;</c> (RefHB 3.10.2.1).
    /// </summary>
    private static ClassDef CreateMetaObject()
    {
        var name = new AttributeDef
        {
            Name = "Name",
            TypeDef = new TypeRef
            {
                Cardinality = new Cardinality { Min = 1, Max = 1 },
                Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["NAME"] },
            },
        };
        var metaObject = new ClassDef
        {
            Name = "METAOBJECT",
            Properties = { Property.Abstract },
            Content = { { name.Name, name } },
        };
        metaObject.Constraints.Add(new UniquenessConstraint
        {
            NameIndex = 1,
            GlobalUnique = { PathTo(name) },
        });
        return metaObject;
    }

    /// <summary>
    /// <c>CLASS METAOBJECT_TRANSLATION</c> with unique <c>Name</c> and <c>NameInBaseLanguage</c> (RefHB 3.10.2.1).
    /// </summary>
    private static ClassDef CreateMetaObjectTranslation()
    {
        var name = new AttributeDef
        {
            Name = "Name",
            TypeDef = new TypeRef
            {
                Cardinality = new Cardinality { Min = 1, Max = 1 },
                Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["NAME"] },
            },
        };
        var nameInBaseLanguage = new AttributeDef
        {
            Name = "NameInBaseLanguage",
            TypeDef = new TypeRef
            {
                Cardinality = new Cardinality { Min = 1, Max = 1 },
                Extends = new Reference<DomainDef> { Target = (DomainDef)Interlis.Content["NAME"] },
            },
        };
        var translation = new ClassDef
        {
            Name = "METAOBJECT_TRANSLATION",
            Content =
            {
                { name.Name, name },
                { nameInBaseLanguage.Name, nameInBaseLanguage },
            },
        };
        translation.Constraints.Add(new UniquenessConstraint
        {
            NameIndex = 1,
            GlobalUnique = { PathTo(name) },
        });
        translation.Constraints.Add(new UniquenessConstraint
        {
            NameIndex = 2,
            GlobalUnique = { PathTo(nameInBaseLanguage) },
        });
        return translation;
    }

    /// <summary>
    /// <c>TOPIC TIMESYSTEMS</c> with the <c>CALENDAR</c> and <c>TIMEOFDAYSYS</c> scale-system classes (RefHB 3.10.3).
    /// </summary>
    private static TopicDef CreateTimeSystemsTopic()
    {
        var topic = new TopicDef { Name = "TIMESYSTEMS" };
        topic.AddContent([
            () => new ClassDef
            {
                Name = "CALENDAR",
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["SCALSYSTEM"] },
                Content =
                {
                    {
                        "Unit",
                        new ParameterDef
                        {
                            Name = "Unit",
                            Properties = { Property.Extended },
                            TypeDef = new NumericType
                            {
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["TIME"] },
                            },
                        }
                    },
                },
            },
            () => new ClassDef
            {
                Name = "TIMEOFDAYSYS",
                Extends = new Reference<ClassDef> { Target = (ClassDef)Interlis.Content["SCALSYSTEM"] },
                Content =
                {
                    {
                        "Unit",
                        new ParameterDef
                        {
                            Name = "Unit",
                            Properties = { Property.Extended },
                            TypeDef = new NumericType
                            {
                                Unit = new Reference<UnitDef> { Target = (UnitDef)Interlis.Content["TIME"] },
                            },
                        }
                    },
                },
            },
        ]);
        return topic;
    }

    /// <summary>
    /// <c>STRUCTURE LineGeometry</c> with its <c>MANDATORY CONSTRAINT isOfClass (Segments[FIRST], StartSegment);</c>.
    /// </summary>
    private static ClassDef CreateLineGeometry()
    {
        var segments = new AttributeDef
        {
            Name = "Segments",
            TypeDef = new ObjectType
            {
                Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound, Ordered = true },
                Targets = [new RestrictedRef { Value = new Reference<IInterlisDefinition> { Target = Interlis.Content["LineSegment"] } }],
            },
        };
        var lineGeometry = new ClassDef
        {
            Name = "LineGeometry",
            IsStructure = true,
            Content = { { segments.Name, segments } },
        };
        lineGeometry.Constraints.Add(new MandatoryConstraint
        {
            NameIndex = 1,
            Condition = new FunctionCall
            {
                FunctionDef = new Reference<FunctionDef> { Target = (FunctionDef)Interlis.Content["isOfClass"], Path = { new("isOfClass") } },
                Arguments =
                {
                    new PathExpression
                    {
                        Reference = new Reference<IInterlisDefinition>
                        {
                            Path = { new IndexedPathSegment { Name = segments.Name, Index = IndexKeyword.First, Target = segments } },
                            Target = segments,
                            Resolution = ReferenceResolution.ObjectPath,
                        },
                    },
                    PathTo(Interlis.Content["StartSegment"]),
                },
            },
        });
        return lineGeometry;
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
