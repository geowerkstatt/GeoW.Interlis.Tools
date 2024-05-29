parser grammar Interlis24Parser;

options {
    tokenVocab=Interlis24Lexer;
}

interlis: INTERLIS numeric SEMICOLON modelDef* EOF;

modelDef:
    metaAttributes* CONTRACTED? (TYPE | REFSYSTEM | SYMBOLOGY)? MODEL name=IDENTIFIER (
        '(' language=IDENTIFIER ')'
    )? NOINCREMENTALTRANSFER? AT uri=string VERSION modelVersion=string EXPLANATION? (
        TRANSLATION OF translationOf=IDENTIFIER '[' translationOfVersion=string ']'
    )? EQUAL_SIGN (CHARSET charsetName=string SEMICOLON)? (
        XMLNS xmlns=string SEMICOLON
    )? (IMPORTS UNQUALIFIED? IDENTIFIER ( ',' UNQUALIFIED? IDENTIFIER)*)* modelContents* END endName=IDENTIFIER '.';

modelContents:
    metaDataBasketDef
    | unitDef
    | functionDef
    | lineFormTypeDef
    | domainDef
    | contextDef
    | runTimeParameterDef
    | classDef
    | structureDef
    | topicDef;

topicDef:
    metaAttributes* VIEW? TOPIC name=IDENTIFIER properties? (EXTENDS topicRef)? EQUAL_SIGN (
        BASKET OID AS basketOid=definitionRef SEMICOLON
    )? (OID AS oid=definitionRef SEMICOLON)? (
        DEPENDS ON topicRef ( ',' topicRef)* SEMICOLON
    )* (DEFERRED GENERICS definitionRef ( ',' definitionRef)* SEMICOLON)? topicContents* END endName=IDENTIFIER
      SEMICOLON;

topicContents:
    metaDataBasketDef
    | unitDef
    | functionDef
    | domainDef
    | classDef
    | structureDef
    | associationDef
    | constraintsDef
    | viewDef
    | graphicDef;

topicRef: (model=IDENTIFIER '.')? topic=IDENTIFIER;

definitionRef: (model=IDENTIFIER '.' ( topic=IDENTIFIER '.')?)? name=IDENTIFIER;

classDef:
    metaAttributes* CLASS name=IDENTIFIER properties? (
        EXTENDS extends=definitionRef
    )? EQUAL_SIGN (( OID AS oid=definitionRef | NO OID) SEMICOLON)? classOrStructureDef END endName=IDENTIFIER SEMICOLON
      ;

structureDef:
    metaAttributes* STRUCTURE name=IDENTIFIER properties? (
        EXTENDS definitionRef
    )? EQUAL_SIGN classOrStructureDef END endName=IDENTIFIER SEMICOLON;

classOrStructureDef:
    ATTRIBUTE? attributeDef* constraintDef* (PARAMETER parameterDef)?;

attributeDef:
    metaAttributes* (CONTINUOUS? SUBDIVISION)? name=IDENTIFIER properties? ':' attrTypeDef (
        ':=' factor ( ',' factor)*
    )? SEMICOLON;

attrTypeDef: ( MANDATORY? | (BAG | LIST) cardinality? OF) attrType;

attrType: type | definitionRef | referenceAttr | restrictedStructureRef;

referenceAttr: REFERENCE TO properties? restrictedClassOrAssRef;

restrictedClassOrAssRef: (definitionRef | ANYCLASS) (
        RESTRICTION '(' definitionRef (SEMICOLON definitionRef)* ')'
    )?;

restrictedStructureRef: (definitionRef | ANYSTRUCTURE) (
        RESTRICTION '(' definitionRef (SEMICOLON definitionRef)* ')'
    )?;

associationDef:
    ASSOCIATION name=IDENTIFIER properties? (EXTENDS definitionRef)? (
        DERIVED FROM renamedViewableRef
    )? EQUAL_SIGN (( OID AS oid=definitionRef | NO OID) SEMICOLON)? roleDef* ATTRIBUTE? attributeDef* (
        CARDINALITY EQUAL_SIGN cardinality SEMICOLON
    )? constraintDef* END endName=IDENTIFIER SEMICOLON;

roleDef:
    metaAttributes* name=IDENTIFIER properties? ('--' | '-<>' | '-<#>') cardinality? restrictedClassOrAssRef (
        OR restrictedClassOrAssRef
    )* (':=' role=factor)? SEMICOLON;

cardinality:
    '{' (from=( '*' | POS_NUMBER) ( '..' to=( POS_NUMBER | '*'))?) '}';

domainDef:
    DOMAIN (
        metaAttributes* name=IDENTIFIER properties? (EXTENDS definitionRef)? EQUAL_SIGN MANDATORY? type (
            CONSTRAINTS IDENTIFIER ':' expression (
                ',' IDENTIFIER ':' expression
            )*
        )? SEMICOLON
    )*;

type: baseType | lineType;

baseType:
    textType
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
    | attributePathType;

constant:
    UNDEFINED
    | numericConst
    | string
    | enumerationConst
    | classConst
    | attributePathConst;

textType: (MTEXT | TEXT) ('*' maxLength=POS_NUMBER)? | NAME | URI;

enumerationType: enumeration (ORDERED | CIRCULAR)?;

enumTreeValueType: ALL OF definitionRef;

enumeration: '(' (enumElement ( ',' enumElement)* ( ':' FINAL)? | FINAL) ')';

enumElement: metaAttributes* IDENTIFIER ('.' IDENTIFIER)* enumeration?;

enumerationConst: '#' (IDENTIFIER ( '.' IDENTIFIER)* ( '.' OTHERS)? | OTHERS);

alignmentType: HALIGNMENT | VALIGNMENT;

booleanType: BOOLEAN;

numericType: (numeric '..' numeric | NUMERIC) CIRCULAR? ('[' definitionRef ']')? (
        CLOCKWISE
        | COUNTERCLOCKWISE
        | refSys
    )?;

refSys:
    '{' metaObjectRef ('[' axis=POS_NUMBER ']')? '}'
    | '<' coord=definitionRef ('[' axis=POS_NUMBER ']')? '>';

decConst: numeric | PI | LNBASE;

numericConst: decConst ('[' definitionRef ']')?;

formattedType:
    FORMAT (
        BASED ON definitionRef formatDef (min=string '..' max=string)?
        | definitionRef min=string '..' max=string
    )
    | min=string '..' max=string;

formatDef:
    '(' INHERITANCE? nonNum=string? (baseAttrRef nonNum=string)* baseAttrRef nonNum=string? ')';

baseAttrRef:
    numericAttribute=IDENTIFIER ('/' intPos=POS_NUMBER)?
    | structureAttribute=IDENTIFIER '/' formatted=definitionRef;

dateTimeType: DATE | TIMEOFDAY | DATETIME;

coordinateType: (COORD | MULTICOORD) numericType (
        ',' numericType (',' numericType)? (',' rotationDef)? (
            REFSYS name=string
        )?
    )?;

rotationDef: ROTATION nullAxis=POS_NUMBER '->' piHalfAxis=POS_NUMBER;

contextDef:
    CONTEXT (
        name=IDENTIFIER EQUAL_SIGN (
            genericCoordDef=definitionRef EQUAL_SIGN concrete=definitionRef (
                OR concrete=definitionRef
            )* SEMICOLON
        )*
    )*;

oidType: OID (ANY | numericType | textType);

blackboxType: BLACKBOX (XML | BINARY);

classType:
    (CLASS | STRUCTURE) (
        RESTRICTION '(' definitionRef (SEMICOLON definitionRef)* ')'
    )?;

attributePathType:
    ATTRIBUTE OF (objectOrAttributePath | '@' argumentName=IDENTIFIER)? (
        RESTRICTION '(' attrTypeDef (SEMICOLON attrTypeDef)* ')'
    )?;

classConst: '>' definitionRef;

attributePathConst: '>>' (definitionRef '->')? attribute=IDENTIFIER;

lineType: (
        DIRECTED? POLYLINE
        | SURFACE
        | AREA
        | DIRECTED? MULTIPOLYLINE
        | MULTISURFACE
        | MULTIAREA
    ) lineForm? controlPoints? intersectionDef?;

lineForm: WITH '(' lineFormType (',' lineFormType)* ')';

lineFormType: STRAIGHTS | ARCS | (model=IDENTIFIER '.')? name=IDENTIFIER;

controlPoints: VERTEX coordType=definitionRef;

intersectionDef: WITHOUT OVERLAPS ('>' numeric)?;

lineFormTypeDef:
    LINE FORM (
        metaAttributes* lineFormTypeName=IDENTIFIER ':' lineStructureName=IDENTIFIER SEMICOLON
    )*;

unitDef:
    UNIT (
        metaAttributes* unitName=IDENTIFIER (
            '(' ABSTRACT ')'
            | '[' unitShortName=IDENTIFIER ']'
        )? (EXTENDS abstractUnitRef=definitionRef)? (
            EQUAL_SIGN ( derivedUnit | composedUnit)
        )? SEMICOLON
    )*;

derivedUnit: (decConst ( ( '*' | '/') decConst)* | FUNCTION EXPLANATION)? '[' definitionRef ']';

composedUnit: '(' definitionRef (( '*' | '/') definitionRef)* ')';

metaDataBasketDef:
    metaAttributes* (SIGN | REFSYSTEM) BASKET basketName=IDENTIFIER properties? (
        EXTENDS definitionRef
    )? '~' topicRef (
        OBJECTS OF className=IDENTIFIER ':' metaAttributes* metaObjectName=IDENTIFIER (
            ',' metaAttributes* metaObjectName=IDENTIFIER
        )*
    )* SEMICOLON;

metaObjectRef: (definitionRef '.')? metaObjectName=IDENTIFIER;

parameterDef:
    metaAttributes* arameter=IDENTIFIER properties? ':' (
        attrTypeDef
        | METAOBJECT (OF metaObject=definitionRef)?
    ) SEMICOLON;

runTimeParameterDef:
    PARAMETER (
        metaAttributes* runTimeParameterName=IDENTIFIER ':' attrTypeDef SEMICOLON
    )*;

constraintDef:
    metaAttributes* (
        mandatoryConstraint
        | plausibilityConstraint
        | existenceConstraint
        | uniquenessConstraint
        | setConstraint
    );

mandatoryConstraint:
    MANDATORY CONSTRAINT (name=IDENTIFIER ':')? logical=expression SEMICOLON;

plausibilityConstraint:
    CONSTRAINT (name=IDENTIFIER ':')? ('<=' | '>=') percentage=numeric '%' logical=expression SEMICOLON;

existenceConstraint:
    EXISTENCE CONSTRAINT (name=IDENTIFIER ':')? objectOrAttributePath REQUIRED IN definitionRef ':'
      objectOrAttributePath (OR definitionRef ':' objectOrAttributePath)* SEMICOLON;

uniquenessConstraint:
    UNIQUE ('(' BASKET ')')? (name=IDENTIFIER ':')? (WHERE expression)? (
        globalUniqueness
        | localUniqueness
    ) SEMICOLON;

globalUniqueness: uniqueEl;

uniqueEl: objectOrAttributePath (',' objectOrAttributePath)*;

localUniqueness:
    '(' LOCAL ')' structureAttribute=IDENTIFIER (
        '->' structureAttribute=IDENTIFIER
    )* ':' attributeName=IDENTIFIER (',' attributeName=IDENTIFIER);

setConstraint:
    SET CONSTRAINT ('(' BASKET ')')? (name=IDENTIFIER ':')? (WHERE expression)? expression SEMICOLON;

constraintsDef:
    CONSTRAINTS OF definitionRef EQUAL_SIGN (constraintDef)* END SEMICOLON;

expression:
    expression ('==' | NOT_EQUAL | '<=' | '>=' | '<' | '>') expression
    | expression (OR | '*' | '/') expression
    | expression (AND | '+' | '-') expression
    | expression '=>' expression
    | factor
    | NOT '(' expression ')'
    | (DEFINED '(' factor ')');

factor:
    objectOrAttributePath
    | (inspection | INSPECTION definitionRef) (OF objectOrAttributePath)?
    | functionCall
    | PARAMETER (model=IDENTIFIER '.') runTimeParameter=IDENTIFIER?
    | constant;

objectOrAttributePath: pathEl ('->' pathEl)*;

pathEl:
    THIS
    | THISAREA
    | THATAREA
    | PARENT
    | IDENTIFIER ('[' IDENTIFIER ']')?
    | associationPath
    | attributeRef;

associationPath: BACKSLASH? IDENTIFIER;

attributeRef:
    attribute=IDENTIFIER ('[' ( FIRST | LAST | axisListIndex=POS_NUMBER) ']')?
    | AGGREGATES;

functionCall: definitionRef '(' (argument ( ',' argument)*)? ')';

argument:
    expression
    | ALL ('(' (restrictedClassOrAssRef | definitionRef) ')')?;

functionDef:
    metaAttributes* FUNCTION name=IDENTIFIER '(' (
        argumentName=IDENTIFIER ':' argumentType (
            SEMICOLON argumentName=IDENTIFIER ':' IDENTIFIER
        )*
    )? ')' ':' argumentType EXPLANATION? SEMICOLON;

argumentType:
    attrTypeDef
    | (OBJECT | OBJECTS) OF (restrictedClassOrAssRef | definitionRef)
    | ENUMVAL
    | ENUMTREEVAL;

viewDef:
    metaAttributes* VIEW name=IDENTIFIER properties? (
        formationDef
        | EXTENDS definitionRef
    )? (baseExtensionDef)* (selection)* EQUAL_SIGN (viewAttributes)? (
        constraintDef
    )* END endName=IDENTIFIER SEMICOLON;

formationDef: (projection | join | union | aggregation | inspection) SEMICOLON;

projection: PROJECTION OF renamedViewableRef;

join: JOIN OF renamedViewableRef (',' renamedViewableRef ( '(' OR NULL ')')?)+;

union: UNION OF renamedViewableRef (',' renamedViewableRef)+;

aggregation: AGGREGATION OF renamedViewableRef (ALL | EQUAL '(' uniqueEl ')');

inspection:
    AREA? INSPECTION OF renamedViewableRef '->' IDENTIFIER ('->' IDENTIFIER)*;

renamedViewableRef: (base=IDENTIFIER '~')? definitionRef;

baseExtensionDef:
    BASE base=IDENTIFIER EXTENDED BY renamedViewableRef (
        ',' renamedViewableRef
    )*;

selection: WHERE expression SEMICOLON;

viewAttributes:
    ATTRIBUTE? (
        ALL OF base=IDENTIFIER SEMICOLON
        | attributeDef
        | attribute=IDENTIFIER properties? ':=' factor SEMICOLON
    )+;

graphicDef:
    metaAttributes* GRAPHIC name=IDENTIFIER (EXTENDS definitionRef)? (
        BASED ON definitionRef
    )? EQUAL_SIGN (drawingRule)* END endName=IDENTIFIER SEMICOLON;

drawingRule:
    name=IDENTIFIER properties? (OF sign=definitionRef)? ':' condSignParamAssignment (
        ',' condSignParamAssignment
    )* SEMICOLON;

condSignParamAssignment: (WHERE expression)? '(' signParamAssignment (
        SEMICOLON signParamAssignment
    )* ')';

signParamAssignment:
    IDENTIFIER ':=' (
        '{' metaObjectRef '}'
        | factor
        | ACCORDING objectOrAttributePath '(' enumAssignment (
            ',' enumAssignment
        )* ')'
    );

enumAssignment: ('{' metaObjectRef '}' | constant) WHEN IN enumRange;

enumRange: enumerationConst ('..' enumerationConst)?;

properties: '(' property (',' property)* ')';

property:
    ABSTRACT
    | EXTENDED
    | GENERIC
    | FINAL
    | TRANSIENT
    | EXTERNAL
    | OID
    | HIDING
    | ORDERED;

numeric: EXP_NUMBER | DECIMAL_NUMBER | SIGNED_NUMBER | POS_NUMBER;

string:
    DOUBLE_QUOTE_OPEN (LITERAL_TEXT | BACKSLASH | DOUBLE_QUOTE | UNICODE)* DOUBLE_QUOTE_CLOSE;

metaAttributes:
    META_COMMENT_OPEN metaAttribute (SEMICOLON metaAttribute)* META_COMMENT_CLOSE;

metaAttribute: META_ATTR_NAME EQUAL_SIGN (META_ATTR_NAME | string);
