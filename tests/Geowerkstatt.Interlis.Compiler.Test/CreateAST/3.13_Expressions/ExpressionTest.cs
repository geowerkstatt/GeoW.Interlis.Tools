using Geowerkstatt.Interlis.Compiler.AST;
using Geowerkstatt.Interlis.Compiler.AST.Expression;
using Geowerkstatt.Interlis.Compiler.AST.Types;
using static Geowerkstatt.Interlis.Compiler.CompilationTestCase;

namespace Geowerkstatt.Interlis.Compiler;

public class ExpressionTest
{
    private static IEnumerable<CompilationTestCase> GetCases()
    {
        yield return Rule(new(
            "Numeric constant",
            "123",
            RefHB: "3.13-1",
            Expected: new NumericConstant { Value = 123 }));

        yield return Rule(new(
            "Numeric constant PI",
            "PI",
            RefHB: "3.13-1",
            Expected: new NumericConstant { Value = NumericConstant.PredefinedConstant.Pi }));

        yield return Rule(new(
            "Text constant",
            """ "Text Constant \uD83D\uDE0A!" """,
            RefHB: "3.13-1",
            Expected: new TextConstant { Value = "Text Constant \U0001F60A!" }));

        yield return Rule(new(
            "Enumeration constant",
            "#red.yellow.lightYellow.OTHERS",
            Description: """
                The trailing OTHERS is not an element name but stands for the potential further values an extension
                may still add below the named node (RefHB 3.8.3), so it is carried as a flag, not a path element.
                """,
            RefHB: "3.13-1",
            Expected: new EnumerationConstant { Path = { new("red"), new("yellow"), new("lightYellow") }, IsOthers = true }));

        yield return Rule(new(
            "Enumeration constant bare OTHERS",
            "#OTHERS",
            Description: "A bare #OTHERS (RefHB 3.13-1) carries no element path at all — only the OTHERS flag.",
            RefHB: "3.13-1",
            Expected: new EnumerationConstant { IsOthers = true }));

        yield return Rule(new(
            "Attribute path",
            "THIS->PARENT->class->bag[FIRST]->list[5]",
            RefHB: "3.13-1",
            Expected: new PathExpression
            {
                Reference = new Reference<IInterlisDefinition> { Path =
                {
                    new KeywordPathSegment(PathKeyword.This),
                    new KeywordPathSegment(PathKeyword.Parent),
                    new("class"),
                    new IndexedPathSegment { Name = "bag", Index = IndexKeyword.First },
                    new IndexedPathSegment { Name = "list", Index = 5 },
                } },
            },
            Ili2cDivergenceReason: "we accept this attribute path expression, ili2c rejects it"));

        yield return Rule(new(
            "Class constant",
            ">Model.Topic.Class",
            RefHB: "3.13-1",
            Expected: new ClassConstant
            {
                Viewable = new Reference<IInterlisDefinition> { Path = { new("Model"), new("Topic"), new("Class") } },
            }));

        yield return Rule(new(
            "Attribute path constant",
            ">>Model.Topic.Class->Attribute",
            RefHB: "3.13-1",
            Expected: new AttributePathConstant
            {
                Attribute = new Reference<AttributeDef> { Path = { new("Model"), new("Topic"), new("Class"), new("Attribute") } },
            }));

        yield return Rule(new(
            "Numeric: Addition",
            "1 + 2",
            RefHB: "3.13-1",
            Expected: new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Addition, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Numeric: Subtraction",
            "1 - 2",
            RefHB: "3.13-1",
            Expected: new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Subtraction, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Numeric: Multiplication",
            "1 * 2",
            RefHB: "3.13-1",
            Expected: new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Multiplication, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Numeric: Division",
            "1 / 2",
            RefHB: "3.13-1",
            Expected: new ArithmeticExpression { Operator = ArithmeticExpression.ArithmeticOperator.Division, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Boolean: Equals",
            "1 == 2",
            RefHB: "3.13-1",
            Expected: new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Boolean: Not equals",
            "1 != 2",
            RefHB: "3.13-1",
            Expected: new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.NotEqual, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Boolean: Not equals SQL style",
            "1 <> 2",
            RefHB: "3.13-1",
            Expected: new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.NotEqual, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Boolean: Greater than",
            "1 > 2",
            RefHB: "3.13-1",
            Expected: new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Greater, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Boolean: Smaller than",
            "1 < 2",
            RefHB: "3.13-1",
            Expected: new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Less, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Boolean: Greater than or equal",
            "1 >= 2",
            RefHB: "3.13-1",
            Expected: new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.GreaterOrEqual, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Boolean: Smaller than or equal",
            "1 <= 2",
            RefHB: "3.13-1",
            Expected: new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.LessOrEqual, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } }));

        yield return Rule(new(
            "Boolean: And",
            "(1 == 2) AND (3 == 4)",
            RefHB: "3.13-1",
            Expected: new LogicalExpression
            {
                Operator = LogicalExpression.LogicalOperator.And,
                FirstOperand = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } },
                SecondOperand = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = new NumericConstant { Value = 3 }, SecondOperand = new NumericConstant { Value = 4 } },
            }));

        yield return Rule(new(
            "Boolean: Or",
            "(1 == 2) OR (3 == 4)",
            RefHB: "3.13-1",
            Expected: new LogicalExpression
            {
                Operator = LogicalExpression.LogicalOperator.Or,
                FirstOperand = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } },
                SecondOperand = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = new NumericConstant { Value = 3 }, SecondOperand = new NumericConstant { Value = 4 } },
            }));

        yield return Rule(new(
            "Boolean: Implication",
            "(1 == 2) => (3 == 4)",
            RefHB: "3.13-1",
            Expected: new LogicalExpression
            {
                Operator = LogicalExpression.LogicalOperator.Implication,
                FirstOperand = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } },
                SecondOperand = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = new NumericConstant { Value = 3 }, SecondOperand = new NumericConstant { Value = 4 } },
            }));

        yield return Rule(new(
            "Boolean: Not",
            "NOT (1 == 2)",
            RefHB: "3.13-1",
            Expected: new NotExpression { Operand = new ComparisonExpression { Operator = ComparisonExpression.ComparisonOperator.Equal, FirstOperand = new NumericConstant { Value = 1 }, SecondOperand = new NumericConstant { Value = 2 } } }));

        yield return Rule(new(
            "Boolean: Defined",
            "DEFINED (UNDEFINED)",
            RefHB: "3.13-1",
            Expected: new DefinedExpression { Operand = new UndefinedConstant() }));

        yield return Rule(new(
            "Function call",
            "len(textAttr)",
            RefHB: "3.13-1",
            Expected: new FunctionCall
            {
                FunctionDef = new Reference<FunctionDef> { Path = { new("len") } },
                Arguments = { new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new("textAttr") } } } },
            }));

        yield return Rule(new(
            "Numeric: Arithmetic operator precedence (* binds before +)",
            "1 + 2 * 3",
            RefHB: "3.13-10",
            Expected: new ArithmeticExpression
            {
                Operator = ArithmeticExpression.ArithmeticOperator.Addition,
                FirstOperand = new NumericConstant
                {
                    Value = 1,
                },
                SecondOperand = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new NumericConstant
                    {
                        Value = 2,
                    },
                    SecondOperand = new NumericConstant
                    {
                        Value = 3,
                    },
                },
            }));

        yield return Rule(new(
            "Boolean: Logical operator precedence (relation before AND before OR)",
            "1 == 2 AND 3 == 4 OR 5 == 6",
            RefHB: "3.13-10",
            Expected: new LogicalExpression
            {
                Operator = LogicalExpression.LogicalOperator.Or,
                FirstOperand = new LogicalExpression
                {
                    Operator = LogicalExpression.LogicalOperator.And,
                    FirstOperand = new ComparisonExpression
                    {
                        Operator = ComparisonExpression.ComparisonOperator.Equal,
                        FirstOperand = new NumericConstant
                        {
                            Value = 1,
                        },
                        SecondOperand = new NumericConstant
                        {
                            Value = 2,
                        },
                    },
                    SecondOperand = new ComparisonExpression
                    {

                        Operator = ComparisonExpression.ComparisonOperator.Equal,
                        FirstOperand = new NumericConstant
                        {
                            Value = 3,
                        },
                        SecondOperand = new NumericConstant
                        {
                            Value = 4,
                        },
                    },
                },
                SecondOperand = new ComparisonExpression
                {
                    Operator = ComparisonExpression.ComparisonOperator.Equal,
                    FirstOperand = new NumericConstant
                    {
                        Value = 5,
                    },
                    SecondOperand = new NumericConstant
                    {
                        Value = 6,
                    },
                },
            }));

        yield return Rule(new(
            "Implication of a comparison and DEFINED",
            "(Status == #gueltig) => DEFINED(Geometrie)",
            RefHB: "3.13-20",
            Expected: new LogicalExpression
            {
                Operator = LogicalExpression.LogicalOperator.Implication,
                FirstOperand = new ComparisonExpression
                {
                    Operator = ComparisonExpression.ComparisonOperator.Equal,
                    FirstOperand = new PathExpression
                    {
                        Reference = new Reference<IInterlisDefinition> { Path = { new("Status") } },
                    },
                    SecondOperand = new EnumerationConstant
                    {
                        Path = { new("gueltig") },
                    },
                },
                SecondOperand = new DefinedExpression
                {
                    Operand = new PathExpression
                    {
                        Reference = new Reference<IInterlisDefinition> { Path = { new("Geometrie") } },
                    },
                },
            }));

        yield return Rule(new(
            "Boolean: Object equality with UNDEFINED",
            "THIS == UNDEFINED",
            RefHB: "3.13-16",
            Expected: new ComparisonExpression
            {
                Operator = ComparisonExpression.ComparisonOperator.Equal,
                FirstOperand = new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new KeywordPathSegment(PathKeyword.This) } } },
                SecondOperand = new UndefinedConstant(),
            }));

        yield return Rule(new(
            "Path element THISAREA",
            "THISAREA",
            RefHB: "3.13-28",
            Expected: new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new KeywordPathSegment(PathKeyword.ThisArea) } } }));

        yield return Rule(new(
            "Path element THATAREA",
            "THATAREA",
            RefHB: "3.13-28",
            Expected: new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new KeywordPathSegment(PathKeyword.ThatArea) } } }));

        yield return Rule(new(
            "Role with association qualifier",
            "role[Assoc]",
            RefHB: "3.13-28",
            Expected: new PathExpression
            {
                Reference = new Reference<IInterlisDefinition> { Path =
                {
                    new RolePathSegment { Name = "role", Association = new("Assoc") },
                } },
            }));

        yield return Rule(new(
            "Association path with leading backslash",
            "\\Assoc",
            ExpectedLog: ["Compile error at 1:0-1:1 extraneous input '\\' expecting {'(', '>', '>>', '#', 'AGGREGATES', 'AREA', 'DEFINED', 'HALIGNMENT', 'INSPECTION', 'INTERLIS', 'LNBASE', 'METAOBJECT', 'NAME', 'NOT', 'PARAMETER', 'PARENT', 'PI', 'REFSYSTEM', 'SIGN', 'THATAREA', 'THIS', 'THISAREA', 'UNDEFINED', 'URI', 'VALIGNMENT', EXP_NUMBER, DECIMAL_NUMBER, SIGNED_NUMBER, POS_NUMBER, IDENTIFIER, DOUBLE_QUOTE_OPEN, '\\\\'}."],
            RefHB: "3.13-29",
            Expected: new PathExpression
            {
                Reference = new Reference<IInterlisDefinition> { Path = { new("Assoc") } },
            }));

        yield return Rule(new(
            "Attribute ref index LAST",
            "list[LAST]",
            RefHB: "3.13-30",
            Expected: new PathExpression
            {
                Reference = new Reference<IInterlisDefinition> { Path =
                {
                    new IndexedPathSegment { Name = "list", Index = IndexKeyword.Last },
                } },
            }));

        yield return Rule(new(
            "Path element AGGREGATES",
            "AGGREGATES",
            RefHB: "3.13-30",
            Expected: new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new KeywordPathSegment(PathKeyword.Aggregates) } } }));

        yield return Rule(new(
            "Qualified function call",
            "Model.Topic.func(THIS)",
            RefHB: "3.13-31",
            Expected: new FunctionCall
            {
                FunctionDef = new Reference<FunctionDef> { Path = { new("Model"), new("Topic"), new("func") } },
                Arguments = { new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new KeywordPathSegment(PathKeyword.This) } } } },
            }));

        yield return Rule(new(
            "Function call without arguments",
            "now()",
            RefHB: "3.13-31",
            Expected: new FunctionCall
            {
                FunctionDef = new Reference<FunctionDef> { Path = { new("now") } },
            }));

        yield return Rule(new(
            "Function call with multiple arguments",
            "inside(a, b)",
            RefHB: "3.13-31",
            Expected: new FunctionCall
            {
                FunctionDef = new Reference<FunctionDef> { Path = { new("inside") } },
                Arguments =
                {
                    new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new("a") } } },
                    new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new("b") } } },
                },
            }));

        yield return Rule(new(
            "Function call with ALL argument",
            "count(ALL)",
            RefHB: "3.13-32",
            Expected: new FunctionCall
            {
                FunctionDef = new Reference<FunctionDef> { Path = { new("count") } },
                Arguments = { new AllExpression() },
            }));

        yield return Rule(new(
            "Function call with restricted ALL argument",
            "count(ALL (Model.Topic.ClassA))",
            RefHB: "3.13-32",
            Expected: new FunctionCall
            {
                FunctionDef = new Reference<FunctionDef> { Path = { new("count") } },
                Arguments =
                {
                    new AllExpression(new RestrictedRef
                    {
                        Value = new Reference<IInterlisDefinition> { Path = { new("Model"), new("Topic"), new("ClassA") } },
                    }),
                },
            }));

        yield return Rule(new(
            "Attribute ref index FIRST",
            "list[FIRST]",
            RefHB: "3.13-45",
            Expected: new PathExpression
            {
                Reference = new Reference<IInterlisDefinition> { Path =
                {
                    new IndexedPathSegment { Name = "list", Index = IndexKeyword.First },
                } },
            }));

        yield return Rule(new(
            "Inline inspection",
            "INSPECTION OF Class -> Attr",
            Description: """
                An inspection as a factor (RefHB 3.13-25/-48): the set of structure elements the inspection yields,
                written inline or referencing a named inspection view, optionally restricted with 'OF path'.
                """,
            RefHB: "3.13-48",
            Expected: new InspectionExpression
            {
                Source = new InspectionView
                {
                    Source = new BaseView { Name = "Class", NameLocations = { new RangePosition(0, 14, 0, 19) }, Viewable = new Reference<IInterlisDefinition> { Path = { new("Class") } } },
                    Path = new Reference<AttributeDef> { Path = { new("Attr") } },
                },
            }));

        yield return Rule(new(
            "Inline area inspection with renamed base",
            "AREA INSPECTION OF b~Class -> Attr",
            RefHB: "3.13-48",
            Expected: new InspectionExpression
            {
                Source = new InspectionView
                {
                    IsArea = true,
                    Source = new BaseView { Name = "b", NameLocations = { new RangePosition(0, 19, 0, 20) }, IsRenamed = true, Viewable = new Reference<IInterlisDefinition> { Path = { new("Class") } } },
                    Path = new Reference<AttributeDef> { Path = { new("Attr") } },
                },
            }));

        yield return Rule(new(
            "Inline inspection restricted with OF",
            "INSPECTION OF Class -> Attr OF Container",
            RefHB: "3.13-48",
            Expected: new InspectionExpression
            {
                Source = new InspectionView
                {
                    Source = new BaseView { Name = "Class", NameLocations = { new RangePosition(0, 14, 0, 19) }, Viewable = new Reference<IInterlisDefinition> { Path = { new("Class") } } },
                    Path = new Reference<AttributeDef> { Path = { new("Attr") } },
                },
                Of = new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new("Container") } } },
            }));

        yield return Rule(new(
            "Named inspection view",
            "INSPECTION SubStructures",
            RefHB: "3.13-48",
            Expected: new InspectionExpression
            {
                Source = new Reference<IInterlisDefinition> { Path = { new("SubStructures") } },
            }));

        yield return Rule(new(
            "Named inspection view restricted with OF",
            "INSPECTION V OF Attr",
            RefHB: "3.13-48",
            Expected: new InspectionExpression
            {
                Source = new Reference<IInterlisDefinition> { Path = { new("V") } },
                Of = new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new("Attr") } } },
            }));

        yield return Rule(new(
            "Runtime parameter reference",
            "PARAMETER Scale",
            Description: """
                A run-time parameter as a factor (RefHB 3.13-25): the value the runtime system supplies, e.g. the
                current map scale in a graphic definition (RefHB 3.13-53, 3.16).
                """,
            RefHB: "3.13-25",
            Expected: new ParameterRefExpression
            {
                Parameter = new Reference<ParameterDef> { Path = { new("Scale") } },
            }));

        yield return Rule(new(
            "Qualified runtime parameter reference",
            "PARAMETER Model.Scale",
            RefHB: "3.13-25",
            Expected: new ParameterRefExpression
            {
                Parameter = new Reference<ParameterDef> { Path = { new("Model"), new("Scale") } },
            }));

        yield return Rule(new(
            "Function call as comparison value",
            "len(name) == 5",
            RefHB: "3.13-53",
            Expected: new ComparisonExpression
            {
                Operator = ComparisonExpression.ComparisonOperator.Equal,
                FirstOperand = new FunctionCall
                {
                    FunctionDef = new Reference<FunctionDef> { Path = { new("len") } },
                    Arguments =
                    {
                        new PathExpression
                        {
                            Reference = new Reference<IInterlisDefinition> { Path = { new("name") } },
                        },
                    },
                },
                SecondOperand = new NumericConstant
                {
                    Value = 5,
                },
            }));

        yield return Rule(new(
            "Chained comparison is rejected",
            "a < b < c",
            Description: """
                A relation is non-associative (Term2 = Predicate [ Relation Predicate ]), so at most one comparison may
                appear without parentheses.
                """,
            ExpectedLog: ["Compile error at 1:6-1:7 comparison operators can not be chained (use parentheses)."],
            RefHB: "3.13-6",
            AssertOutput: false));

        yield return Rule(new(
            "Parenthesised nested comparison is accepted",
            "a < (b < c)",
            Description: "Parenthesising the nested comparison makes it a Predicate again, which is valid.",
            RefHB: "3.13-6",
            AssertOutput: false));

        yield return Rule(new(
            "Parenthesised left comparison operand is accepted",
            "(a < b) < c",
            Description: """
                A parenthesised left operand is a single Predicate, so this is one relation, not a chain. Its AST is
                identical to `a < b < c`, so only the parse tree (not the AST) can tell them apart.
                """,
            RefHB: "3.13-6",
            AssertOutput: false));

        yield return Rule(new(
            "Chained implication is rejected",
            "a => b => c",
            Description: """
                An implication is non-associative (Term = Term0 [ '=>' Term0 ]), so at most one '=>' may appear without
                parentheses.
                """,
            ExpectedLog: ["Compile error at 1:7-1:9 the implication operator '=>' can not be chained (use parentheses)."],
            RefHB: "3.13-3",
            AssertOutput: false));

        yield return Rule(new(
            "Parenthesised nested implication is accepted",
            "a => (b => c)",
            Description: "Parenthesising the nested implication makes it a Term0 again, which is valid.",
            RefHB: "3.13-3",
            AssertOutput: false));

        yield return Rule(new(
            "Numeric: Chained addition is accepted (left-associative)",
            "1 + 2 + 3",
            Description: """
                Unlike relations and implication, the additive operators (Term0 = Term1 { ('OR'|'+'|'-') Term1 }) are
                associative and chain freely; `1 + 2 + 3` groups left as `(1 + 2) + 3`.
                """,
            RefHB: "3.13-4",
            Expected: new ArithmeticExpression
            {
                Operator = ArithmeticExpression.ArithmeticOperator.Addition,
                FirstOperand = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Addition,
                    FirstOperand = new NumericConstant { Value = 1 },
                    SecondOperand = new NumericConstant { Value = 2 },
                },
                SecondOperand = new NumericConstant { Value = 3 },
            }));

        yield return Rule(new(
            "Numeric: Chained multiplication is accepted (left-associative)",
            "1 * 2 * 3",
            Description: """
                The multiplicative operators (Term1 = Term2 { ('AND'|'*'|'/') Term2 }) chain freely too; `1 * 2 * 3`
                groups left as `(1 * 2) * 3`.
                """,
            RefHB: "3.13-5",
            Expected: new ArithmeticExpression
            {
                Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                FirstOperand = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Multiplication,
                    FirstOperand = new NumericConstant { Value = 1 },
                    SecondOperand = new NumericConstant { Value = 2 },
                },
                SecondOperand = new NumericConstant { Value = 3 },
            }));

        yield return Rule(new(
            "Numeric: Parenthesised left division operand is accepted",
            "(8 / 4) / 2",
            Description: """
                Our grammar allows parentheses around ANY expression (notExpression = NOT? '(' expression ')'), not only a
                Logical-Expression as the handbook's Predicate does — so a parenthesised numeric sub-expression parses.
                `(8 / 4) / 2` groups the parenthesised division on the left.
                """,
            RefHB: "3.13-5",
            Expected: new ArithmeticExpression
            {
                Operator = ArithmeticExpression.ArithmeticOperator.Division,
                FirstOperand = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Division,
                    FirstOperand = new NumericConstant { Value = 8 },
                    SecondOperand = new NumericConstant { Value = 4 },
                },
                SecondOperand = new NumericConstant { Value = 2 },
            }));

        yield return Rule(new(
            "Numeric: Parenthesised right division operand is accepted",
            "8 / (4 / 2)",
            Description: "`8 / (4 / 2)` nests the parenthesised division on the right.",
            RefHB: "3.13-5",
            Expected: new ArithmeticExpression
            {
                Operator = ArithmeticExpression.ArithmeticOperator.Division,
                FirstOperand = new NumericConstant { Value = 8 },
                SecondOperand = new ArithmeticExpression
                {
                    Operator = ArithmeticExpression.ArithmeticOperator.Division,
                    FirstOperand = new NumericConstant { Value = 4 },
                    SecondOperand = new NumericConstant { Value = 2 },
                },
            }));

        yield return Rule(new(
            "Surplus closing parenthesis is rejected",
            "8 / 4 / 2)",
            Description: """
                The surplus ')' is a syntax error: a rule-level parse requires the whole input to be consumed (EOF), so
                trailing tokens after the valid prefix `8 / 4 / 2` are reported instead of silently ignored.
                """,
            ExpectedLog: ["Compile error at 1:9-1:10 extraneous input ')' expecting <EOF>."],
            RefHB: "3.13-5",
            AssertOutput: false));

        yield return FullFile(new(
            "Attribute path constant resolves to the referenced attribute",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS C =
                        flag : BOOLEAN;
                    MANDATORY CONSTRAINT Check: DEFINED(>> C -> flag);
                    END C;
                END T;
            END M.
            """,
            Description: """
                The single Reference<AttributeDef> of an attribute-path constant must resolve to the referenced
                attribute, including the unqualified viewable form '>> C -> flag' (viewable 'C' relative, attribute
                descended). The resolved Target in the expected AST (the same 'flag' instance declared in class C) is
                what the deep-comparison asserts. DEFINED(...) wraps the constant into a boolean expression so the
                constraint is valid for both compilers.
                """,
            RefHB: "3.13-1",
            Expected: TestTools.Build(() => {
                var flag = new AttributeDef
                {
                    Name = "flag",
                    TypeDef = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                };
                return Environment(("C", new ClassDef
                {
                    Name = "C",
                    Content = { { "flag", flag } },
                    Constraints =
                    {
                        new MandatoryConstraint
                        {
                            Name = "Check",
                            Condition = new DefinedExpression
                            {
                                Operand = new AttributePathConstant
                                {
                                    Attribute = new Reference<AttributeDef> { Path = { new("C"), new("flag") }, Target = flag },
                                },
                            },
                        },
                    },
                }));
            })));

        yield return FullFile(new(
            "Object path resolves to a boolean attribute (bare and via THIS)",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS C =
                        flag : BOOLEAN;
                        MANDATORY CONSTRAINT DEFINED(flag);
                        MANDATORY CONSTRAINT DEFINED(THIS -> flag);
                    END C;
                END T;
            END M.
            """,
            Description: """
                Object/attribute path resolution (RefHB 3.13). Resolution runs only for full files, so the resolved
                PathExpression.Target (go-to-definition) — and the ReturnType derived from it — are asserted via FullFile cases.
                """,
            RefHB: "3.13-33",
            Expected: TestTools.Build(() =>
            {
                var flag = new AttributeDef
                {
                    Name = "flag",
                    TypeDef = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                };
                return Environment(("C", new ClassDef
                {
                    Name = "C",
                    Content = { { "flag", flag } },
                    Constraints =
                    {
                        new MandatoryConstraint
                        {
                            NameIndex = 1,
                            Condition = new DefinedExpression
                            {
                                Operand = new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new("flag") }, Target = flag }},
                            },
                        },
                        new MandatoryConstraint
                        {
                            NameIndex = 2,
                            Condition = new DefinedExpression
                            {
                                Operand = new PathExpression
                                {
                                    Reference = new Reference<IInterlisDefinition> { Path = { new KeywordPathSegment(PathKeyword.This), new("flag") },
                                    Target = flag },
                                },
                            },
                        },
                    },
                }));
            })));

        yield return FullFile(new(
            "Object path descends through a reference attribute",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS Target =
                        flag : BOOLEAN;
                    END Target;
                    CLASS C =
                        link : REFERENCE TO Target;
                        MANDATORY CONSTRAINT DEFINED(link -> flag);
                    END C;
                END T;
            END M.
            """,
            RefHB: "3.13-37",
            Expected: TestTools.Build(() =>
            {
                var targetFlag = new AttributeDef
                {
                    Name = "flag",
                    TypeDef = new BooleanType { Cardinality = new Cardinality { Min = 0, Max = 1 } },
                };
                var target = new ClassDef { Name = "Target", Content = { { "flag", targetFlag } } };
                var link = new AttributeDef
                {
                    Name = "link",
                    TypeDef = new ReferenceType
                    {
                        Target = new RestrictedRef { Value = new Reference<IInterlisDefinition> { Path = { new("Target") }, Target = target } },                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                    },
                };
                return Environment(
                    ("Target", target),
                    ("C", new ClassDef
                    {
                        Name = "C",
                        Content = { { "link", link } },
                        Constraints =
                        {
                            new MandatoryConstraint
                            {
                                NameIndex = 1,
                                Condition = new DefinedExpression
                                {
                                    Operand = new PathExpression
                                    {
                                        Reference = new Reference<IInterlisDefinition> { Path = { new("link"), new("flag") },
                                        Target = targetFlag },
                                    },
                                },
                            },
                        },
                    }));
            })));

        yield return FullFile(new(
            "Indexed coordinate path resolves to its more specific axis type",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS C =
                        pos : COORD 0.000 .. 100.000, 0.000 .. 100.000;
                        MANDATORY CONSTRAINT DEFINED(pos[1]);
                    END C;
                END T;
            END M.
            """,
            RefHB: "3.13-42",
            Expected: TestTools.Build(() =>
            {
                var coord = new CoordType
                {
                    Cardinality = new Cardinality { Min = 0, Max = 1 },
                    Axis =
                    {
                        new DecimalType { Min = 0, Max = 100, Precision = -3 },
                        new DecimalType { Min = 0, Max = 100, Precision = -3 },
                    },
                };
                var pos = new AttributeDef { Name = "pos", TypeDef = coord };
                return Environment(("C", new ClassDef
                {
                    Name = "C",
                    Content = { { "pos", pos } },
                    Constraints =
                    {
                        new MandatoryConstraint
                        {
                            NameIndex = 1,
                            Condition = new DefinedExpression
                            {
                                // Target is the whole coordinate attribute; ReturnType narrows to the indexed (first) axis.
                                Operand = new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new IndexedPathSegment { Name = "pos", Index = 1 } }, Target = pos }},
                            },
                        },
                    },
                }));
            })));

        yield return FullFile(new(
            "Indexed coordinate path through a domain alias resolves to its axis type",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    DOMAIN
                        point = COORD 0.000 .. 100.000, 0.000 .. 100.000;
                    CLASS C =
                        pos : point;
                        MANDATORY CONSTRAINT DEFINED(pos[1]);
                    END C;
                END T;
            END M.
            """,
            Description: "The same narrowing applies when the coordinate is declared via a named domain: the alias is transparent.",
            RefHB: "3.13-42",
            Expected: TestTools.Build(() =>
            {
                var pointDomain = new DomainDef
                {
                    Name = "point",
                    TypeDef = new CoordType
                    {
                        Cardinality = new Cardinality { Min = 0, Max = Cardinality.Unbound },
                        Axis =
                        {
                            new DecimalType { Min = 0, Max = 100, Precision = -3 },
                            new DecimalType { Min = 0, Max = 100, Precision = -3 },
                        },
                    },
                };
                var pos = new AttributeDef
                {
                    Name = "pos",
                    TypeDef = new TypeRef
                    {
                        Cardinality = new Cardinality { Min = 0, Max = 1 },
                        Extends = new Reference<DomainDef> { Target = pointDomain, Path = { new("point") } },
                    },
                };
                return Environment(
                    ("point", pointDomain),
                    ("C", new ClassDef
                    {
                        Name = "C",
                        Content = { { "pos", pos } },
                        Constraints =
                        {
                            new MandatoryConstraint
                            {
                                NameIndex = 1,
                                Condition = new DefinedExpression
                                {
                                    // ReturnType narrows to the indexed (first) axis through the alias.
                                    Operand = new PathExpression { Reference = new Reference<IInterlisDefinition> { Path = { new IndexedPathSegment { Name = "pos", Index = 1 } }, Target = pos }},
                                },
                            },
                        },
                    }));
            })));

        yield return FullFile(new(
            "Unknown attribute in a constraint path",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS C =
                        attr : TEXT*10;
                        MANDATORY CONSTRAINT DEFINED(unknown);
                    END C;
                END T;
            END M.
            """,
            Description: "Compile-time attribute access: a path element that names no member of a known viewable is an error.",
            ExpectedLog: ["Could not resolve 'unknown' in 'M.T.C' at 6:41-6:48"],
            RefHB: "3.13",
            AssertOutput: false));

        yield return FullFile(new(
            "Unknown attribute behind a reference attribute",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS Target =
                        name : TEXT*10;
                    END Target;
                    CLASS C =
                        aRef : REFERENCE TO Target;
                        MANDATORY CONSTRAINT DEFINED(aRef->unknown);
                    END C;
                END T;
            END M.
            """,
            Description: "Compile-time attribute access: a path element that names no member of a known viewable is an error.",
            ExpectedLog: ["Could not resolve 'unknown' in 'M.T.Target' at 9:41-9:54"],
            RefHB: "3.13",
            AssertOutput: false));

        yield return FullFile(new(
            "Unknown attribute behind a multi-target role",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS A =
                    END A;
                    CLASS B =
                    END B;
                    ASSOCIATION Assoc =
                        r1 -- {0..1} A OR B;
                        r2 -- {0..*} A;
                        MANDATORY CONSTRAINT DEFINED(r1->nonexistent);
                    END Assoc;
                END T;
            END M.
            """,
            Description: """
                An object behind a multi-target role is an instance of ONE of the targets, so a member is only valid if
                every target has it (the intersection of the targets' members). Missing from all targets:
                """,
            ExpectedLog: ["Could not resolve 'nonexistent' in 'M.T.A', 'M.T.B' at 11:41-11:56"],
            RefHB: "3.13",
            AssertOutput: false));

        yield return FullFile(new(
            "Attribute in only one target of a multi-target role",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS A =
                        onlyInA : TEXT*10;
                    END A;
                    CLASS B =
                    END B;
                    ASSOCIATION Assoc =
                        r1 -- {0..1} A OR B;
                        r2 -- {0..*} A;
                        MANDATORY CONSTRAINT DEFINED(r1->onlyInA);
                    END Assoc;
                END T;
            END M.
            """,
            Description: "Missing from only one target: reported for exactly the target that lacks it.",
            ExpectedLog: ["Could not resolve 'onlyInA' in 'M.T.B' at 12:41-12:52"],
            RefHB: "3.13",
            Ili2cDivergenceReason: "ili2c types a multi-target role (A OR B) by its FIRST target only: an attribute existing only in the first target passes while one existing only in the second is rejected as not applicable to the first (verified with mirrored inputs), and a member missing in a later branch's substructure is never seen; we require a path member behind a multi-target role to exist in EVERY target since the object may be of any of them, so we reject what ili2c's first-target view accepts.",
            AssertOutput: false));

        yield return FullFile(new(
            "Attribute from a common base resolves behind a multi-target role",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    CLASS Target =
                    END Target;
                    CLASS Base (ABSTRACT) =
                        cRef : REFERENCE TO Target;
                    END Base;
                    CLASS A EXTENDS Base =
                    END A;
                    CLASS B EXTENDS Base =
                    END B;
                    ASSOCIATION Assoc =
                        r1 -- {0..1} A OR B;
                        r2 -- {0..*} Target;
                        MANDATORY CONSTRAINT DEFINED(r1->cRef->bogus);
                    END Assoc;
                END T;
            END M.
            """,
            Description: """
                An attribute inherited from a common base resolves through a multi-target role and navigation continues —
                proven by the error behind it, which is only found if the multi-target descent worked.
                """,
            ExpectedLog: ["Could not resolve 'bogus' in 'M.T.Target' at 16:41-16:56"],
            RefHB: "3.13",
            AssertOutput: false));

        yield return FullFile(new(
            "Member checked in every branch behind a multi-target role",
            """
            INTERLIS 2.4;
            MODEL M AT "http://example.com" VERSION "1.0.0" =
                TOPIC T =
                    STRUCTURE S =
                        y : TEXT*10;
                    END S;
                    STRUCTURE U =
                    END U;
                    CLASS A =
                        x : S;
                    END A;
                    CLASS B =
                        x : U;
                    END B;
                    ASSOCIATION Assoc =
                        r1 -- {0..1} A OR B;
                        r2 -- {0..*} A;
                        MANDATORY CONSTRAINT DEFINED(r1->x->y);
                    END Assoc;
                END T;
            END M.
            """,
            Description: """
                Same-named attributes that are DIFFERENT definitions per target: the next element must exist in the union
                of the viewables they navigate into (here it is missing from one branch's structure).
                """,
            ExpectedLog: ["Could not resolve 'y' in 'M.T.U' at 18:41-18:49"],
            RefHB: "3.13",
            Ili2cDivergenceReason: "ili2c types a multi-target role (A OR B) by its FIRST target only: an attribute existing only in the first target passes while one existing only in the second is rejected as not applicable to the first (verified with mirrored inputs), and a member missing in a later branch's substructure is never seen; we require a path member behind a multi-target role to exist in EVERY target since the object may be of any of them, so we reject what ili2c's first-target view accepts.",
            AssertOutput: false));

        yield return FullFile(new(
            "Inline inspection as a function argument",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Part =
                    Name : TEXT;
                END Part;
                FUNCTION hasParts (parts: BAG OF ANYSTRUCTURE): BOOLEAN;
                TOPIC Topic =
                    CLASS ClassA =
                        Parts : BAG OF Part;
                    SET CONSTRAINT hasParts(INSPECTION OF ClassA -> Parts);
                    END ClassA;
                END Topic;
            END Model.
            """,
            Description: """
                An inline inspection as a function argument: the inspection denotes the structure elements of ALL ClassA
                objects — a class-wide set, so it belongs in a SET CONSTRAINT (per-class), not a per-object MANDATORY
                CONSTRAINT. The inspected viewable reference resolves like a view formation's.
                """,
            RefHB: "3.13-48",
            AssertOutput: false));

        yield return FullFile(new(
            "Named inspection view as a function argument",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Part =
                    Name : TEXT;
                END Part;
                FUNCTION hasParts (parts: BAG OF ANYSTRUCTURE): BOOLEAN;
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

                    CONSTRAINTS OF ClassA =
                        SET CONSTRAINT hasParts(INSPECTION PartsView);
                    END;
                END Topic;
            END Model.
            """,
            Description: "A named inspection factor (INSPECTION ViewableRef) referencing an inspection view.",
            RefHB: "3.13-25",
            AssertOutput: false));

        yield return FullFile(new(
            "Named inspection factor referencing a non-inspection view is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Part =
                    Name : TEXT;
                END Part;
                FUNCTION hasParts (parts: BAG OF ANYSTRUCTURE): BOOLEAN;
                TOPIC Topic =
                    CLASS ClassA =
                        Parts : BAG OF Part;
                    END ClassA;

                    VIEW ProjView
                        PROJECTION OF ClassA;
                        =
                        ATTRIBUTE
                            ALL OF ClassA;
                    END ProjView;

                    CONSTRAINTS OF ClassA =
                        SET CONSTRAINT hasParts(INSPECTION ProjView);
                    END;
                END Topic;
            END Model.
            """,
            Description: "The viewable referenced by a named inspection factor must be an inspection view.",
            ExpectedLog: ["'Model.Topic.ProjView' at 20:47-20:55 can not be used as an INSPECTION factor because it is not an inspection view"],
            RefHB: "3.13-25",
            AssertOutput: false));

        yield return FullFile(new(
            "Unknown member in an inspection OF restriction path is reported",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Part =
                    Name : TEXT;
                END Part;
                FUNCTION hasParts (parts: BAG OF ANYSTRUCTURE): BOOLEAN;
                TOPIC Topic =
                    CLASS ClassA =
                        Parts : BAG OF Part;
                    SET CONSTRAINT hasParts(INSPECTION OF ClassA -> Parts OF Missing);
                    END ClassA;
                END Topic;
            END Model.
            """,
            Description: """
                The OF restriction path of an inspection factor is rooted at the context viewable and checked like any
                other path, so an unknown member in it is reported.
                """,
            ExpectedLog: ["Could not resolve 'Missing' in 'Model.Topic.ClassA' at 10:65-10:72"],
            RefHB: "3.13-48",
            AssertOutput: false));

        yield return FullFile(new(
            "Unknown attribute in an inline inspection path is reported",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                STRUCTURE Part =
                    Name : TEXT;
                END Part;
                FUNCTION hasParts (parts: BAG OF ANYSTRUCTURE): BOOLEAN;
                TOPIC Topic =
                    CLASS ClassA =
                        Parts : BAG OF Part;
                    SET CONSTRAINT hasParts(INSPECTION OF ClassA -> Nope);
                    END ClassA;
                END Topic;
            END Model.
            """,
            Description: """
                An inline inspection factor carries the same formation a view declares, so its attribute path is
                resolved and checked the same way — rooted at its own source viewable.
                """,
            ExpectedLog: ["Could not resolve 'Nope' in 'Model.Topic.ClassA' at 10:56-10:60"],
            RefHB: "3.13-48",
            AssertOutput: false));

        yield return FullFile(new(
            "Inline inspection of a scalar attribute is rejected",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                FUNCTION hasParts (parts: BAG OF ANYSTRUCTURE): BOOLEAN;
                TOPIC Topic =
                    CLASS ClassA =
                        Label : TEXT*10;
                    SET CONSTRAINT hasParts(INSPECTION OF ClassA -> Label);
                    END ClassA;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["'Model.Topic.ClassA -> Label' at 7:56-7:61 can not be inspected because its type is not a substructure or a single polyline, surface or area"],
            RefHB: "3.13-48",
            AssertOutput: false));

        yield return FullFile(new(
            "Runtime parameter as a comparison value",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                PARAMETER
                    MaxLength : 0 .. 100;
                TOPIC Topic =
                    CLASS ClassA =
                        Length : 0 .. 100;
                    MANDATORY CONSTRAINT Length <= PARAMETER MaxLength;
                    END ClassA;
                END Topic;
            END Model.
            """,
            Description: """
                A run-time parameter as a comparison value (RefHB 3.13-53): the model-level PARAMETER declaration
                (RefHB 3.11) lives in the model's content, so the reference resolves like any other definition.
                """,
            RefHB: "3.13-53",
            AssertOutput: false));

        yield return FullFile(new(
            "Unresolved runtime parameter is reported",
            """
            INTERLIS 2.4;
            MODEL Model AT "http://example.com" VERSION "1.0.0" =
                PARAMETER
                    MaxLength : 0 .. 100;
                TOPIC Topic =
                    CLASS ClassA =
                        Length : 0 .. 100;
                    MANDATORY CONSTRAINT Length <= PARAMETER Missing;
                    END ClassA;
                END Topic;
            END Model.
            """,
            ExpectedLog: ["Could not resolve 'reference 'Missing' from Model.Topic.ClassA' at 8:49-8:56"],
            RefHB: "3.13-53",
            AssertOutput: false));
    }

    /// <summary>Wraps the given topic members into the standard single-model, single-topic environment (<c>MODEL M … TOPIC T</c>).</summary>
    private static InterlisEnvironment Environment(params (string Name, IInterlisDefinition Definition)[] topicContent)
    {
        var topic = new TopicDef { Name = "T" };
        foreach (var (name, definition) in topicContent)
        {
            topic.Content.Add(name, definition);
        }

        return new InterlisEnvironment
        {
            Version = 2.4,
            Content =
            {
                { InternalModel.Interlis.Name, InternalModel.Interlis },
                {
                    "M",
                    new ModelDef
                    {
                        Name = "M",
                        URI = "http://example.com",
                        Version = "1.0.0",
                        Imports = { { InternalModel.Interlis.Name, (false, new Reference<ModelDef> { Target = InternalModel.Interlis, Path = { new(InternalModel.Interlis.Name) } }) } },
                        Content = { { "T", topic } },
                    }
                },
            }
        };
    }

    // A fragment is only accepted by BOTH compilers in a context that fits its kind, so route by a case-name prefix:
    //  "Boolean:" -> the fragment IS a boolean predicate: use it as a constraint body.
    //  "Numeric:" -> the fragment is a numeric value: compare it to 0 so the constraint body is boolean.
    //  otherwise  -> the fragment is a single value (factor): use it as a derived attribute value.
    // Without this, every binary-expression fragment is a non-factor that both compilers trivially reject.
    private static string Wrap(string fragment, string name)
        => name.StartsWith("Boolean:", StringComparison.Ordinal) ? ConstraintWrap(fragment)
        : name.StartsWith("Numeric:", StringComparison.Ordinal) ? ConstraintWrap($"({fragment}) == 0")
        : FactorWrap(fragment);

    private static string FactorWrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                CLASS Dummy = END Dummy;
                VIEW ExpressionView PROJECTION OF Dummy; =
                    Attr := {fragment};
                END ExpressionView;
            END Topic;
        END Model.
        """;

    private static string ConstraintWrap(string fragment) => $"""
        INTERLIS 2.4;
        MODEL Model AT "http://example.com" VERSION "1.0.0" =
            TOPIC Topic =
                CLASS C =
                    a : 0 .. 10;
                MANDATORY CONSTRAINT {fragment};
                END C;
            END Topic;
        END Model.
        """;

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetTestCases()
        => RuleRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullFileTestCases()
        => FullFileRows(GetCases());

    public static IEnumerable<TestDataRow<CompilationTestCase>> GetFullTestCases()
        => ComparisonRows(nameof(ExpressionTest), GetCases(), Wrap);

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task ReadExpression(CompilationTestCase data)
    {
        await TestTools.AssertReadRule(data, (p, v) => (IExpression?)v.Visit(p.expression()));
    }

    [Test]
    [MethodDataSource(nameof(GetFullFileTestCases))]
    public async Task ReadFullFile(CompilationTestCase data)
    {
        await TestTools.AssertReadFile(data);
    }
}
