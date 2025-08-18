// $antlr-format alignTrailingComments true, columnLimit 150, minEmptyLines 1, maxEmptyLinesToKeep 1, reflowComments false, useTab false
// $antlr-format allowShortRulesOnASingleLine false, allowShortBlocksOnASingleLine true, alignSemicolons hanging, alignColons hanging

parser grammar Interlis24Parser;

options {
    tokenVocab=Interlis24Lexer;
}

interlis
    : INTERLIS numeric SEMICOLON modelDef* EOF
    ;

modelDef
    : CONTRACTED? (TYPE | REFSYSTEM | SYMBOLOGY)? MODEL name=IDENTIFIER (
        '(' language=IDENTIFIER ')'
    )? NOINCREMENTALTRANSFER? AT uri=string VERSION modelVersion=string EXPLANATION? (
        TRANSLATION OF translationOf=IDENTIFIER '[' translationOfVersion=string ']'
    )? EQUAL_SIGN (CHARSET charsetName=string SEMICOLON)? (XMLNS xmlns=string SEMICOLON)? (
        IMPORTS modelImport (',' modelImport)* SEMICOLON
    )* modelContents* END endName=IDENTIFIER '.'
    ;

modelImport
    : UNQUALIFIED? name=(IDENTIFIER | INTERLIS)
    ;

modelContents
    : metaDataBasketDef
    | unitDef
    | functionDef
    | lineFormTypeDef
    | domainDef
    | contextDef
    | runTimeParameterDef
    | classDef
    | topicDef
    ;

topicDef
    : VIEW? TOPIC name=IDENTIFIER properties? /* ABSTRACT, FINAL */ (
        EXTENDS extends=definitionRef
    )? EQUAL_SIGN (BASKET OID AS basketOid=definitionRef SEMICOLON)? (
        OID AS oid=definitionRef SEMICOLON
    )? (DEPENDS ON dependsOn+=definitionRef ( ',' dependsOn+=definitionRef)* SEMICOLON)* (
        DEFERRED GENERICS generics+=definitionRef (',' generics+=definitionRef)* SEMICOLON
    )? topicContents* END endName=IDENTIFIER SEMICOLON
    ;

topicContents
    : metaDataBasketDef
    | unitDef
    | functionDef
    | domainDef
    | classDef
    | associationDef
    | constraintsDef
    | viewDef
    | graphicDef
    ;

definitionRef
    : (model=(IDENTIFIER | INTERLIS) '.' ( topic=IDENTIFIER '.')?)? name=(
        IDENTIFIER
        | URI
        | NAME
        | HALIGNMENT
        | VALIGNMENT
        | METAOBJECT
        | REFSYSTEM
    )
    ;

classDef
    : (CLASS | STRUCTURE) name=IDENTIFIER properties? /* ABSTRACT,EXTENDED,FINAL */ (
        EXTENDS extends=definitionRef
    )? EQUAL_SIGN ((OID AS oid=definitionRef | NO noOid=OID) SEMICOLON)? ATTRIBUTE? attributeDef* constraintDef* (
        PARAMETER parameterDef
    )? END endName=IDENTIFIER SEMICOLON
    ;

attributeDef
    : (CONTINUOUS? SUBDIVISION)? name=IDENTIFIER properties? /* ABSTRACT, EXTENDED, FINAL, TRANSIENT */ ':' attrTypeDef (
        ':=' factor ( ',' factor)*
    )? SEMICOLON
    ;

attrTypeDef
    : (MANDATORY? | (BAG | LIST) cardinality? OF) attrType
    ;

attrType
    : type
    | referenceAttr
    | restrictedDefinitionRef /* RestrictedStructureRef */ /* DomainRef */
    ;

referenceAttr
    : REFERENCE TO properties? /* EXTERNAL */ restrictedDefinitionRef /* RestrictedClassOrAssRef */
    ;

restrictedDefinitionRef
    : (ref=definitionRef | ANYCLASS | ANYSTRUCTURE) (
        RESTRICTION '(' restrictions+=definitionRef (SEMICOLON restrictions+=definitionRef)* ')'
    )?
    ;

associationDef
    : ASSOCIATION name=IDENTIFIER? properties? /* ABSTRACT, EXTENDED, FINAL, OID */ (
        EXTENDS extends=definitionRef
    )? (DERIVED FROM renamedViewableRef)? EQUAL_SIGN (
        ( OID AS oid=definitionRef | NO noOid=OID) SEMICOLON
    )? roleDef* ATTRIBUTE? attributeDef* (CARDINALITY EQUAL_SIGN cardinality SEMICOLON)? constraintDef* END endName=IDENTIFIER? SEMICOLON
    ;

roleDef
    : name=IDENTIFIER properties? /* ABSTRACT, EXTENDED, FINAL, HIDING, ORDERED, EXTERNAL */ referenceType=(
        '--'
        | '-<>'
        | '-<#>'
    ) cardinality? restrictedDefinitionRef (OR restrictedDefinitionRef)* (':=' role=factor)? SEMICOLON
    ;

cardinality
    : '{' from=('*' | POS_NUMBER) ('..' to=( POS_NUMBER | '*'))? '}'
    ;

domainDef
    : DOMAIN domainTypeDef*
    ;

domainTypeDef
    : name=IDENTIFIER properties? /* ABSTRACT, GENERIC, FINAL */ (EXTENDS extends=definitionRef)? EQUAL_SIGN (
        MANDATORY type?
        | type
    ) (CONSTRAINTS domainConstraint (',' domainConstraint)*)? SEMICOLON
    ;

domainConstraint
    : IDENTIFIER ':' expression
    ;

type
    : baseType
    | lineType
    ;

baseType
    : textType
    | enumerationType
    | enumTreeValueType
    | alignmentType
    | booleanType
    | numericType
    | formattedType
    | dateTimeType
    | coordinateType
    | oidType
    | blackboxType
    | classType
    | attributePathType
    ;

constant
    : UNDEFINED
    | numericConst
    | string
    | enumerationConst
    | classConst
    | attributePathConst
    ;

textType
    : (MTEXT | TEXT) ('*' maxLength=POS_NUMBER)?
    | NAME
    | URI
    ;

enumerationType
    : enumeration sequencing=(ORDERED | CIRCULAR)?
    ;

enumTreeValueType
    : ALL OF definitionRef
    ;

enumeration
    : '(' (enumElement ( ',' enumElement)* ( ':' FINAL)? | FINAL) ')'
    ;

enumElement
    : IDENTIFIER ('.' IDENTIFIER)* enumeration?
    ;

enumerationConst
    : '#' (IDENTIFIER ( '.' IDENTIFIER)* ( '.' OTHERS)? | OTHERS)
    ;

alignmentType
    : HALIGNMENT
    | VALIGNMENT
    ;

booleanType
    : (INTERLIS '.')? BOOLEAN
    ;

numericType
    : (min=numeric '..' max=numeric | NUMERIC) CIRCULAR? ('[' unit=definitionRef ']')? (
        CLOCKWISE
        | COUNTERCLOCKWISE
        | refSys
    )?
    ;

refSys
    : '{' metaObjectRef ('[' axis=POS_NUMBER ']')? '}'
    | '<' coord=definitionRef ('[' axis=POS_NUMBER ']')? '>'
    ;

decConst
    : numeric
    | PI
    | LNBASE
    ;

numericConst
    : decConst ('[' definitionRef ']')?
    ;

formattedType
    : FORMAT (
        BASED ON basedOn=definitionRef formatDef (min=string '..' max=string)?
        | domainRef=definitionRef min=string '..' max=string
    )
    | min=string '..' max=string
    ;

formatDef
    : '(' INHERITANCE? nonNum=string? (baseAttrRef nonNum=string)* baseAttrRef nonNum=string? ')'
    ;

baseAttrRef
    : numericAttribute=IDENTIFIER ('/' intPos=POS_NUMBER)?
    | structureAttribute=IDENTIFIER '/' formatted=definitionRef
    ;

dateTimeType
    : kind=(DATE | TIMEOFDAY | DATETIME)
    ;

coordinateType
    : (COORD | MULTICOORD) axis+=numericType (
        ',' axis+=numericType (',' axis+=numericType)? (',' rotationDef)? (REFSYS refsys=string)?
    )?
    ;

rotationDef
    : ROTATION nullAxis=POS_NUMBER '->' piHalfAxis=POS_NUMBER
    ;

contextDef
    : CONTEXT (
        name=IDENTIFIER EQUAL_SIGN (
            genericCoordDef=definitionRef EQUAL_SIGN concrete=definitionRef (
                OR concrete=definitionRef
            )* SEMICOLON
        )*
    )*
    ;

oidType
    : OID (ANY | numericType | textType)
    ;

blackboxType
    : BLACKBOX (XML | BINARY)
    ;

classType
    : (CLASS | STRUCTURE) (RESTRICTION '(' definitionRef (SEMICOLON definitionRef)* ')')?
    ;

attributePathType
    : ATTRIBUTE (OF objectOrAttributePath | '@' argumentName=IDENTIFIER)? (
        RESTRICTION '(' attrTypeDef (SEMICOLON attrTypeDef)* ')'
    )?
    ;

classConst
    : '>' definitionRef
    ;

attributePathConst
    : '>>' (definitionRef '->')? attribute=IDENTIFIER
    ;

lineType
    : ((SURFACE | AREA | MULTISURFACE | MULTIAREA) | (DIRECTED? (POLYLINE | MULTIPOLYLINE))) lineForm? (
        VERTEX vertexType=definitionRef
    )? (WITHOUT OVERLAPS ('>' numeric)?)?
    ;

lineForm
    : WITH '(' lineFormType (',' lineFormType)* ')'
    ;

lineFormType
    : STRAIGHTS
    | ARCS
    | definitionRef
    ;

lineFormTypeDef
    : LINE FORM (lineFormTypeName=IDENTIFIER ':' lineStructureName=IDENTIFIER SEMICOLON)*
    ;

unitDef
    : UNIT unitTypeDef*
    ;

unitTypeDef
    : unitTerm=IDENTIFIER ('(' ABSTRACT ')' | '[' unitShortName=IDENTIFIER ']')? (
        EXTENDS extends=definitionRef
    )? (EQUAL_SIGN (derivedUnit | composedUnit))? SEMICOLON
    ;

derivedUnit
    : (decConst ( op+=( '*' | '/') decConst)* | FUNCTION EXPLANATION)? '[' definitionRef ']'
    ;

composedUnit
    : '(' definitionRef (op+=( '*' | '/') definitionRef)* ')'
    ;

metaDataBasketDef
    : (SIGN | REFSYSTEM) BASKET basketName=IDENTIFIER properties? /* FINAL */ (
        EXTENDS definitionRef
    )? '~' topic=definitionRef (
        OBJECTS OF className=IDENTIFIER ':' metaObjectName=IDENTIFIER (
            ',' metaObjectName=IDENTIFIER
        )*
    )* SEMICOLON
    ;

metaObjectRef
    : (definitionRef '.')? metaObjectName=IDENTIFIER
    ;

parameterDef
    : parameter=IDENTIFIER properties? /* ABSTRACT, EXTENDED, FINAL */ ':' (
        attrTypeDef
        | METAOBJECT (OF metaObject=definitionRef)?
    ) SEMICOLON
    ;

runTimeParameterDef
    : PARAMETER (runTimeParameterName=IDENTIFIER ':' attrTypeDef SEMICOLON)*
    ;

constraintDef
    : (
        mandatoryConstraint
        | plausibilityConstraint
        | existenceConstraint
        | uniquenessConstraint
        | setConstraint
    )
    ;

mandatoryConstraint
    : MANDATORY CONSTRAINT (name=IDENTIFIER ':')? logical=expression SEMICOLON
    ;

plausibilityConstraint
    : CONSTRAINT (name=IDENTIFIER ':')? ('<=' | '>=') percentage=numeric '%' logical=expression SEMICOLON
    ;

existenceConstraint
    : EXISTENCE CONSTRAINT (name=IDENTIFIER ':')? objectOrAttributePath REQUIRED IN definitionRef ':' objectOrAttributePath (
        OR definitionRef ':' objectOrAttributePath
    )* SEMICOLON
    ;

uniquenessConstraint
    : UNIQUE ('(' BASKET ')')? (name=IDENTIFIER ':')? (WHERE expression)? (
        globalUniqueness
        | localUniqueness
    ) SEMICOLON
    ;

globalUniqueness
    : uniqueEl
    ;

uniqueEl
    : objectOrAttributePath (',' objectOrAttributePath)*
    ;

localUniqueness
    : '(' LOCAL ')' structureAttribute=IDENTIFIER ('->' structureAttribute=IDENTIFIER)* ':' attributeName=IDENTIFIER (
        ',' attributeName=IDENTIFIER
    )*
    ;

setConstraint
    : SET CONSTRAINT ('(' BASKET ')')? (name=IDENTIFIER ':')? (WHERE expression)? expression SEMICOLON
    ;

constraintsDef
    : CONSTRAINTS OF definitionRef EQUAL_SIGN (constraintDef)* END SEMICOLON
    ;

expression
    : expression binOp=('==' | NOT_EQUAL | '<=' | '>=' | '<' | '>') expression # binaryExpression
    | expression binOp=(OR | '*' | '/') expression                             # binaryExpression
    | expression binOp=(AND | '+' | '-') expression                            # binaryExpression
    | expression binOp='=>' expression                                         # binaryExpression
    | factor                                                                   # factorExpression
    | NOT? '(' expression ')'                                                  # notExpression
    | (DEFINED '(' factor ')')                                                 # definedExpression
    ;

factor
    : functionCall
    | objectOrAttributePath
    | (inspection | INSPECTION definitionRef) (OF objectOrAttributePath)?
    | PARAMETER definitionRef
    | constant
    ;

objectOrAttributePath
    : pathEl ('->' pathEl)*
    ;

pathEl
    : keyword=(THIS | THISAREA | THATAREA | PARENT | AGGREGATES)
    | BACKSLASH name=IDENTIFIER
    | name=IDENTIFIER ('[' detail=( FIRST | LAST | POS_NUMBER | IDENTIFIER) ']')?
    ;

functionCall
    : definitionRef '(' (argument ( ',' argument)*)? ')'
    ;

argument
    : expression
    | ALL ('(' restrictedDefinitionRef ')')?
    ;

functionDef
    : FUNCTION name=IDENTIFIER '(' (
        argumentName=IDENTIFIER ':' argumentType (
            SEMICOLON argumentName=IDENTIFIER ':' argumentType
        )*
    )? ')' ':' returnType=argumentType EXPLANATION? SEMICOLON
    ;

argumentType
    : attrTypeDef
    | (OBJECT | OBJECTS) OF (restrictedDefinitionRef | definitionRef)
    | (ENUMVAL | ENUMTREEVAL)
    ;

viewDef
    : VIEW name=IDENTIFIER properties? /* ABSTRACT, EXTENDED, FINAL, TRANSIENT */ (
        formationDef
        | EXTENDS definitionRef
    )? (baseExtensionDef)* (selection)* EQUAL_SIGN viewAttributes (constraintDef)* END endName=IDENTIFIER SEMICOLON
    ;

formationDef
    : (projection | join | union | aggregation | inspection) SEMICOLON
    ;

projection
    : PROJECTION OF renamedViewableRef
    ;

join
    : JOIN OF renamedViewableRef (',' renamedViewableRef ( '(' OR NULL ')')?)+
    ;

union
    : UNION OF renamedViewableRef (',' renamedViewableRef)+
    ;

aggregation
    : AGGREGATION OF renamedViewableRef (ALL | EQUAL '(' uniqueEl ')')
    ;

inspection
    : AREA? INSPECTION OF renamedViewableRef '->' IDENTIFIER ('->' IDENTIFIER)*
    ;

renamedViewableRef
    : (base=IDENTIFIER '~')? definitionRef
    ;

baseExtensionDef
    : BASE base=IDENTIFIER EXTENDED BY renamedViewableRef (',' renamedViewableRef)*
    ;

selection
    : WHERE expression SEMICOLON
    ;

viewAttributes
    : ATTRIBUTE? (
        ALL OF base=IDENTIFIER SEMICOLON
        | attributeDef
        | attribute+=IDENTIFIER properties? /* ABSTRACT, EXTENDED, FINAL, TRANSIENT */ ':=' factor SEMICOLON
    )*
    ;

graphicDef
    : GRAPHIC name=IDENTIFIER (EXTENDS definitionRef)? (BASED ON definitionRef)? EQUAL_SIGN (
        drawingRule
    )* END endName=IDENTIFIER SEMICOLON
    ;

drawingRule
    : name=IDENTIFIER properties? /* ABSTRACT, EXTENDED, FINAL */ (OF sign=definitionRef)? ':' condSignParamAssignment (
        ',' condSignParamAssignment
    )* SEMICOLON
    ;

condSignParamAssignment
    : (WHERE expression)? '(' signParamAssignment (SEMICOLON signParamAssignment)* ')'
    ;

signParamAssignment
    : IDENTIFIER ':=' (
        '{' metaObjectRef '}'
        | factor
        | ACCORDING objectOrAttributePath '(' enumAssignment (',' enumAssignment)* ')'
    )
    ;

enumAssignment
    : ('{' metaObjectRef '}' | constant) WHEN IN enumRange
    ;

enumRange
    : enumerationConst ('..' enumerationConst)?
    ;

properties
    : '(' property (',' property)* ')'
    ;

property
    : ABSTRACT
    | EXTENDED
    | GENERIC
    | FINAL
    | TRANSIENT
    | EXTERNAL
    | OID
    | HIDING
    | ORDERED
    ;

numeric
    : EXP_NUMBER     # expNumber
    | DECIMAL_NUMBER # decimalNumber
    | SIGNED_NUMBER  # signedNumber
    | POS_NUMBER     # posNumber
    ;

string
    : DOUBLE_QUOTE_OPEN (LITERAL_TEXT | BACKSLASH | DOUBLE_QUOTE | UNICODE)* DOUBLE_QUOTE_CLOSE
    ;

/**
 * Meta comments (on channel META_COMMENT)
 * This rules are in the main grammar to easily reuse the string rule.
 */
metaComments
    : metaComment+ EOF
    ;

metaComment
    : META_COMMENT_OPEN metaAttribute (SEMICOLON metaAttribute)* SEMICOLON? META_COMMENT_CLOSE
    ;

metaAttribute
    : META_ATTR_NAME EQUAL_SIGN (META_ATTR_NAME | string)
    ;