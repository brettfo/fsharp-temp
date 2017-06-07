﻿//----------------------------------------------------------------------------
// Copyright (c) 2002-2012 Microsoft Corporation. 
//
// This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
// copy of the license can be found in the License.html file at the root of this distribution. 
// By using this source code in any fashion, you are agreeing to be bound 
// by the terms of the Apache License, Version 2.0.
//
// You must not remove this notice, or any other, from this software.
//----------------------------------------------------------------------------

namespace Microsoft.FSharp.Compiler.SourceCodeServices

open System.Collections.Generic
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.AccessibilityLogic
open Microsoft.FSharp.Compiler.CompileOps
open Microsoft.FSharp.Compiler.Range
open Microsoft.FSharp.Compiler.Tast
open Microsoft.FSharp.Compiler.TcGlobals
open Microsoft.FSharp.Compiler.NameResolution

// Implementation details used by other code in the compiler    
module internal Impl = 
    type internal cenv = 
        new : TcGlobals * thisCcu:CcuThunk * tcImports: TcImports -> cenv
        member amap: Import.ImportMap
        member g: TcGlobals

/// Indicates the accessibility of a symbol, as seen by the F# language
#if COMPILER_PUBLIC_API
type FSharpAccessibility = 
#else
type internal FSharpAccessibility = 
#endif
    internal new: Accessibility * ?isProtected: bool -> FSharpAccessibility

    /// Indicates the symbol has public accessibility
    member IsPublic : bool

    /// Indicates the symbol has private accessibility
    member IsPrivate : bool

    /// Indicates the symbol has internal accessibility
    member IsInternal : bool

    /// The underlying Accessibility
    member internal Contents : Accessibility


/// Represents the information needed to format types and other information in a style
/// suitable for use in F# source text at a particular source location.
///
/// Acquired via GetDisplayEnvAtLocationAlternate and similar methods. May be passed 
/// to the Format method on FSharpType and other methods.
#if COMPILER_PUBLIC_API
type [<Class>] FSharpDisplayContext = 
#else
type [<Class>] internal FSharpDisplayContext = 
#endif
    internal new : denv: (TcGlobals -> Tastops.DisplayEnv) -> FSharpDisplayContext
    static member Empty: FSharpDisplayContext

/// Represents a symbol in checked F# source code or a compiled .NET component. 
///
/// The subtype of the symbol may reveal further information and can be one of FSharpEntity, FSharpUnionCase
/// FSharpField, FSharpGenericParameter, FSharpStaticParameter, FSharpMemberOrFunctionOrValue, FSharpParameter,
/// or FSharpActivePatternCase.
#if COMPILER_PUBLIC_API
type [<Class>] FSharpSymbol = 
#else
type [<Class>] internal FSharpSymbol = 
#endif
    /// Internal use only. 
    static member internal Create : g:TcGlobals * thisCcu: CcuThunk * tcImports: TcImports * item:NameResolution.Item -> FSharpSymbol

    /// Computes if the symbol is accessible for the given accessibility rights
    member IsAccessible: FSharpAccessibilityRights -> bool
        
    member internal Item: NameResolution.Item
        
    /// Get the assembly declaring this symbol
    member Assembly: FSharpAssembly 

    /// Get a textual representation of the full name of the symbol. The text returned for some symbols
    /// may not be a valid identifier path in F# code, but rather a human-readable representation of the symbol.
    member FullName: string

    /// Get the declaration location for the symbol
    member DeclarationLocation: range option

    /// Gets the short display name for the symbol
    member DisplayName: string

    /// Get the implementation location for the symbol if it was declared in a signature that has an implementation
    member ImplementationLocation: range option

    /// Get the signature location for the symbol if it was declared in an implementation
    member SignatureLocation: range option

    /// Return true if two symbols are effectively the same when referred to in F# source code text.  
    /// This sees through signatures (a symbol in a signature will be considered effectively the same as 
    /// the matching symbol in an implementation).  In addition, other equivalances are applied
    /// when the same F# source text implies the same declaration name - for example, constructors 
    /// are considered to be effectively the same symbol as the corresponding type definition.
    ///
    /// This is the relation used by GetUsesOfSymbol and GetUsesOfSymbolInFile.
    member IsEffectivelySameAs : other: FSharpSymbol -> bool
    member IsExplicitlySuppressed : bool
    static member GetAccessibility : FSharpSymbol -> FSharpAccessibility option

/// Represents an assembly as seen by the F# language
#if COMPILER_PUBLIC_API
and [<Class>] FSharpAssembly = 
#else
and [<Class>] internal FSharpAssembly = 
#endif

    internal new : tcGlobals: TcGlobals * tcImports: TcImports * ccu: CcuThunk -> FSharpAssembly

    /// The qualified name of the assembly
    member QualifiedName: string 
    
    [<System.Obsolete("This item is obsolete, it is not useful")>]
    member CodeLocation: string 
      
    /// The contents of the this assembly 
    member Contents:  FSharpAssemblySignature

    /// The file name for the assembly, if any
    member FileName : string option

    /// The simple name for the assembly
    member SimpleName : string 
#if EXTENSIONTYPING
    /// Indicates if the assembly was generated by a type provider and is due for static linking
    member IsProviderGenerated : bool
#endif

/// Represents an inferred signature of part of an assembly as seen by the F# language
#if COMPILER_PUBLIC_API
and [<Class>] FSharpAssemblySignature = 
#else
and [<Class>] internal FSharpAssemblySignature = 
#endif

    internal new : tcGlobals: TcGlobals * thisCcu: CcuThunk * tcImports: TcImports * topAttribs: TypeChecker.TopAttribs option * contents: ModuleOrNamespaceType -> FSharpAssemblySignature

    /// The (non-nested) module and type definitions in this signature
    member Entities:  IList<FSharpEntity>

    /// Get the declared attributes for the assembly.
    /// Only available when parsing an entire project. 
    member Attributes: IList<FSharpAttribute>

    /// Find entity using compiled names
    member FindEntityByPath: string list -> FSharpEntity option


/// A subtype of FSharpSymbol that represents a type definition or module as seen by the F# language
#if COMPILER_PUBLIC_API
and [<Class>] FSharpEntity = 
#else
and [<Class>] internal FSharpEntity = 
#endif
    inherit FSharpSymbol
    
    internal new : Impl.cenv * EntityRef -> FSharpEntity

    //   /// Return the FSharpEntity corresponding to a .NET type
    // static member FromType : System.Type -> FSharpEntity

    /// Get the name of the type or module, possibly with `n mangling  
    member LogicalName: string

    /// Get the compiled name of the type or module, possibly with `n mangling. This is identical to LogicalName
    /// unless the CompiledName attribute is used.
    member CompiledName: string

    /// Get the name of the type or module as displayed in F# code
    member DisplayName: string

    /// Get the path used to address the entity (e.g. "Namespace.Module1.NestedModule2"). Gives
    /// "global" for items not in a namespace.
    member AccessPath: string 

    /// Get the namespace containing the type or module, if any. Use 'None' for item not in a namespace.
    member Namespace: string option

    /// Get the fully qualified name of the type or module
    member QualifiedName: string 

    /// Get the full name of the type or module
    member FullName: string 

    /// Get the full name of the type or module if it is available
    member TryFullName: string option

    /// Get the declaration location for the type constructor 
    member DeclarationLocation: range 

    /// Indicates if the entity is a measure, type or exception abbreviation
    member IsFSharpAbbreviation   : bool

    /// Indicates if the entity is record type
    member IsFSharpRecord   : bool

    /// Indicates if the entity is union type
    member IsFSharpUnion   : bool

    /// Indicates if the entity is a struct or enum
    member IsValueType : bool

    /// Indicates if the entity is an array type
    member IsArrayType : bool

    /// Get the rank of an array type
    member ArrayRank : int
#if EXTENSIONTYPING
    /// Indicates if the entity is a 'fake' symbol related to a static instantiation of a type provider
    member IsStaticInstantiation : bool

    /// Indicates if the entity is a provided type
    member IsProvided : bool

    /// Indicates if the entity is an erased provided type
    member IsProvidedAndErased : bool

    /// Indicates if the entity is a generated provided type
    member IsProvidedAndGenerated : bool
#endif
    /// Indicates if the entity is an F# module definition
    member IsFSharpModule: bool 

    /// Get the generic parameters, possibly including unit-of-measure parameters
    member GenericParameters: IList<FSharpGenericParameter>
#if EXTENSIONTYPING
    /// Get the static parameters for a provided type
    member StaticParameters: IList<FSharpStaticParameter>
#endif
    /// Indicates that a module is compiled to a class with the given mangled name. The mangling is reversed during lookup 
    member HasFSharpModuleSuffix : bool

    /// Indicates if the entity is a measure definition
    member IsMeasure: bool

    /// Indicates an F# exception declaration
    member IsFSharpExceptionDeclaration: bool 

    /// Indicates if this is a reference to something in an F#-compiled assembly
    member IsFSharp : bool

    /// Indicates if the entity is in an unresolved assembly 
    member IsUnresolved : bool

    /// Indicates if the entity is a class type definition
    member IsClass : bool

    /// Indicates if is the 'byref<_>' type definition used for byref types in F#-compiled assemblies
    member IsByRef : bool

    /// Indicates if the entity is a type definition for a reference type where the implementation details are hidden by a signature
    member IsOpaque : bool

    /// Indicates if the entity is an enum type definition
    member IsEnum : bool

    /// Indicates if the entity is a delegate type definition
    member IsDelegate : bool

    /// Indicates if the entity is an interface type definition
    member IsInterface : bool

    /// Indicates if the entity is a part of a namespace path
    member IsNamespace : bool

    /// Get the in-memory XML documentation for the entity, used when code is checked in-memory
    member XmlDoc: IList<string>

      /// Get the XML documentation signature for the entity, used for .xml file lookup for compiled code
    member XmlDocSig: string

    /// Indicates if the type is implemented through a mapping to IL assembly code. This is only
    /// true for types in FSharp.Core.dll
    member HasAssemblyCodeRepresentation: bool 

    /// Indicates if the type prefers the "tycon<a,b>" syntax for display etc. 
    member UsesPrefixDisplay: bool                   

    /// Get the declared attributes for the type 
    member Attributes: IList<FSharpAttribute>     

    /// Get the declared interface implementations
    member DeclaredInterfaces : IList<FSharpType>  

    /// Get all the interface implementations, by walking the type hierarchy
    member AllInterfaces : IList<FSharpType>  

    /// Check if the entity inherits from System.Attribute in its type hierarchy
    member IsAttributeType : bool

    /// Get the base type, if any 
    member BaseType : FSharpType option

    /// Get the properties, events and methods of a type definitions, or the functions and values of a module
    member MembersFunctionsAndValues : IList<FSharpMemberOrFunctionOrValue>

    [<System.Obsolete("Renamed to MembersFunctionsAndValues")>]
    member MembersOrValues : IList<FSharpMemberOrFunctionOrValue>

    /// Get the modules and types defined in a module, or the nested types of a type
    member NestedEntities : IList<FSharpEntity>

    /// Get the fields of a record, class, struct or enum from the perspective of the F# language.
    /// This includes static fields, the 'val' bindings in classes and structs, and the value definitions in enums.
    /// For classes, the list may include compiler generated fields implied by the use of primary constructors.
    member FSharpFields : IList<FSharpField>

    [<System.Obsolete("Renamed to FSharpFields")>]
    member RecordFields : IList<FSharpField>

    /// Get the type abbreviated by an F# type abbreviation
    member AbbreviatedType   : FSharpType 

    /// Get the cases of a union type
    member UnionCases : IList<FSharpUnionCase>


    /// Indicates if the type is a delegate with the given Invoke signature 
    member FSharpDelegateSignature : FSharpDelegateSignature

    /// Get the declared accessibility of the type
    member Accessibility: FSharpAccessibility 

    /// Get the declared accessibility of the representation, not taking signatures into account 
    member RepresentationAccessibility: FSharpAccessibility

    /// Get all compilation paths, taking `Module` suffixes into account.
    member AllCompilationPaths : string list

/// Represents a delegate signature in an F# symbol
#if COMPILER_PUBLIC_API
and [<Class>] FSharpDelegateSignature =
#else
and [<Class>] internal FSharpDelegateSignature =
#endif
    /// Get the argument types of the delegate signature
    member DelegateArguments : IList<string option * FSharpType>

    /// Get the return type of the delegate signature
    member DelegateReturnType : FSharpType

/// Represents a parameter in an abstract method of a class or interface
#if COMPILER_PUBLIC_API
and [<Class>] FSharpAbstractParameter =
#else
and [<Class>] internal FSharpAbstractParameter =
#endif

    /// The optional name of the parameter
    member Name : string option
    
    /// The declared or inferred type of the parameter
    member Type : FSharpType

    /// Indicate this is an in argument
    member IsInArg : bool

    /// Indicate this is an out argument
    member IsOutArg : bool

    /// Indicate this is an optional argument
    member IsOptionalArg : bool

    /// The declared attributes of the parameter 
    member Attributes : IList<FSharpAttribute>     

/// Represents the signature of an abstract slot of a class or interface 
#if COMPILER_PUBLIC_API
and [<Class>] FSharpAbstractSignature =
#else
and [<Class>] internal FSharpAbstractSignature =
#endif
    internal new : Impl.cenv * SlotSig -> FSharpAbstractSignature

    /// Get the arguments of the abstract slot
    member AbstractArguments : IList<IList<FSharpAbstractParameter>>

    /// Get the return type of the abstract slot
    member AbstractReturnType : FSharpType

    /// Get the generic arguments of the type defining the abstract slot
    member DeclaringTypeGenericParameters : IList<FSharpGenericParameter>
        
    /// Get the generic arguments of the abstract slot
    member MethodGenericParameters : IList<FSharpGenericParameter>

    /// Get the name of the abstract slot
    member Name : string
    
    /// Get the declaring type of the abstract slot
    member DeclaringType : FSharpType

/// A subtype of FSharpSymbol that represents a union case as seen by the F# language
#if COMPILER_PUBLIC_API
and [<Class>] FSharpUnionCase =
#else 
and [<Class>] internal FSharpUnionCase =
#endif
    inherit FSharpSymbol
    internal new : Impl.cenv * UnionCaseRef -> FSharpUnionCase

    /// Get the name of the union case 
    member Name: string 

    /// Get the range of the name of the case 
    member DeclarationLocation : range

    /// Get the data carried by the case. 
    member UnionCaseFields: IList<FSharpField>

    /// Get the type constructed by the case. Normally exactly the type of the enclosing type, sometimes an abbreviation of it 
    member ReturnType: FSharpType

    /// Get the name of the case in generated IL code 
    member CompiledName: string

    /// Get the in-memory XML documentation for the union case, used when code is checked in-memory
    member XmlDoc: IList<string>

    /// Get the XML documentation signature for .xml file lookup for the union case, used for .xml file lookup for compiled code 
    member XmlDocSig: string

    ///  Indicates if the declared visibility of the union constructor, not taking signatures into account 
    member Accessibility: FSharpAccessibility 

    /// Get the attributes for the case, attached to the generated static method to make instances of the case 
    member Attributes: IList<FSharpAttribute>

    /// Indicates if the union case is for a type in an unresolved assembly 
    member IsUnresolved : bool



/// A subtype of FSharpSymbol that represents a record or union case field as seen by the F# language
#if COMPILER_PUBLIC_API
and [<Class>] FSharpField =
#else
and [<Class>] internal FSharpField =
#endif

    inherit FSharpSymbol
    internal new : Impl.cenv * RecdFieldRef -> FSharpField
    internal new : Impl.cenv * UnionCaseRef * int -> FSharpField

    /// Get the declaring entity of this field
    member DeclaringEntity: FSharpEntity

    /// Indicates if the field is declared 'static'
    member IsMutable: bool

    /// Indicates if the field has a literal value
    member IsLiteral: bool

    /// Indicates if the field is declared volatile 
    member IsVolatile: bool

    /// Indicates if the field declared is declared 'DefaultValue' 
    member IsDefaultValue: bool

    /// Indicates a static field 
    member IsStatic: bool

    /// Indicates a compiler generated field, not visible to Intellisense or name resolution 
    member IsCompilerGenerated: bool

    /// Get the in-memory XML documentation for the field, used when code is checked in-memory
    member XmlDoc: IList<string>

    /// Get the XML documentation signature for .xml file lookup for the field, used for .xml file lookup for compiled code
    member XmlDocSig: string

    /// Get the type of the field, w.r.t. the generic parameters of the enclosing type constructor 
    member FieldType: FSharpType

    /// Get the declaration location of the field 
    member DeclarationLocation: range

    /// Get the attributes attached to generated property 
    member PropertyAttributes: IList<FSharpAttribute> 

    /// Get the attributes attached to generated field 
    member FieldAttributes: IList<FSharpAttribute> 

    /// Get the name of the field 
    member Name : string

      /// Get the default initialization info, for static literals 
    member LiteralValue: obj option

      ///  Indicates if the declared visibility of the field, not taking signatures into account 
    member Accessibility: FSharpAccessibility 

    /// Indicates if the record field is for a type in an unresolved assembly 
    member IsUnresolved : bool

/// Represents the rights of a compilation to access symbols
#if COMPILER_PUBLIC_API
and [<Class>] FSharpAccessibilityRights =
#else
and [<Class>] internal FSharpAccessibilityRights =
#endif
    internal new : CcuThunk * AccessorDomain -> FSharpAccessibilityRights
    member internal Contents : AccessorDomain

/// A subtype of FSharpSymbol that represents a generic parameter for an FSharpSymbol
#if COMPILER_PUBLIC_API
and [<Class>] FSharpGenericParameter = 
#else
and [<Class>] internal FSharpGenericParameter = 
#endif

    inherit FSharpSymbol
    internal new : Impl.cenv * Typar -> FSharpGenericParameter

    /// Get the name of the generic parameter 
    member Name: string

    /// Get the range of the generic parameter 
    member DeclarationLocation : range 
       
    /// Indicates if this is a measure variable
    member IsMeasure : bool

    /// Get the in-memory XML documentation for the type parameter, used when code is checked in-memory
    member XmlDoc : IList<string>
       
    /// Indicates if this is a statically resolved type variable
    member IsSolveAtCompileTime : bool 

    /// Indicates if this is a compiler generated type parameter
    member IsCompilerGenerated : bool 

    /// Get the declared attributes of the type parameter. 
    member Attributes: IList<FSharpAttribute>                      
       
    /// Get the declared or inferred constraints for the type parameter
    member Constraints: IList<FSharpGenericParameterConstraint> 


#if EXTENSIONTYPING
/// A subtype of FSharpSymbol that represents a static parameter to an F# type provider
#if COMPILER_PUBLIC_API
and [<Class>] FSharpStaticParameter = 
#else
and [<Class>] internal FSharpStaticParameter = 
#endif

    inherit FSharpSymbol

    /// Get the name of the static parameter 
    member Name: string

    /// Get the declaration location of the static parameter 
    member DeclarationLocation : range 
       
    /// Get the kind of the static parameter
    member Kind : FSharpType

    /// Get the default value for the static parameter
    member DefaultValue : obj

    /// Indicates if the static parameter is optional
    member IsOptional : bool

    [<System.ObsoleteAttribute("This member is no longer used, use IsOptional instead")>]
    member HasDefaultValue : bool
#endif

/// Represents further information about a member constraint on a generic type parameter
#if COMPILER_PUBLIC_API
and [<Class; NoEquality; NoComparison>] FSharpGenericParameterMemberConstraint = 
#else
and [<Class; NoEquality; NoComparison>] internal FSharpGenericParameterMemberConstraint = 
#endif

    /// Get the types that may be used to satisfy the constraint
    member MemberSources : IList<FSharpType>

    /// Get the name of the method required by the constraint
    member MemberName : string 

    /// Indicates if the the method required by the constraint must be static
    member MemberIsStatic : bool

    /// Get the argument types of the method required by the constraint
    member MemberArgumentTypes : IList<FSharpType>

    /// Get the return type of the method required by the constraint
    member MemberReturnType : FSharpType 

/// Represents further information about a delegate constraint on a generic type parameter
#if COMPILER_PUBLIC_API
and [<Class; NoEquality; NoComparison>] FSharpGenericParameterDelegateConstraint = 
#else
and [<Class; NoEquality; NoComparison>] internal FSharpGenericParameterDelegateConstraint = 
#endif

    /// Get the tupled argument type required by the constraint
    member DelegateTupledArgumentType : FSharpType

    /// Get the return type required by the constraint
    member DelegateReturnType : FSharpType 

/// Represents further information about a 'defaults to' constraint on a generic type parameter
#if COMPILER_PUBLIC_API
and [<Class; NoEquality; NoComparison>] FSharpGenericParameterDefaultsToConstraint = 
#else
and [<Class; NoEquality; NoComparison>] internal FSharpGenericParameterDefaultsToConstraint = 
#endif

    /// Get the priority off the 'defaults to' constraint
    member DefaultsToPriority : int

    /// Get the default type associated with the 'defaults to' constraint
    member DefaultsToTarget : FSharpType

/// Represents a constraint on a generic type parameter
#if COMPILER_PUBLIC_API
and [<Class; NoEquality; NoComparison>] FSharpGenericParameterConstraint = 
#else
and [<Class; NoEquality; NoComparison>] internal FSharpGenericParameterConstraint = 
#endif
    /// Indicates a constraint that a type is a subtype of the given type 
    member IsCoercesToConstraint : bool

    /// Gets further information about a coerces-to constraint
    member CoercesToTarget : FSharpType 

    /// Indicates a default value for an inference type variable should it be neither generalized nor solved 
    member IsDefaultsToConstraint : bool

    /// Gets further information about a defaults-to constraint
    member DefaultsToConstraintData : FSharpGenericParameterDefaultsToConstraint

    /// Indicates a constraint that a type has a 'null' value 
    member IsSupportsNullConstraint  : bool

    /// Indicates a constraint that a type supports F# generic comparison
    member IsComparisonConstraint  : bool

    /// Indicates a constraint that a type supports F# generic equality
    member IsEqualityConstraint  : bool

    /// Indicates a constraint that a type is an unmanaged type
    member IsUnmanagedConstraint  : bool

    /// Indicates a constraint that a type has a member with the given signature 
    member IsMemberConstraint : bool

    /// Gets further information about a member constraint
    member MemberConstraintData : FSharpGenericParameterMemberConstraint

    /// Indicates a constraint that a type is a non-Nullable value type 
    member IsNonNullableValueTypeConstraint : bool
    
    /// Indicates a constraint that a type is a reference type 
    member IsReferenceTypeConstraint  : bool

    /// Indicates a constraint that is a type is a simple choice between one of the given ground types. Used by printf format strings.
    member IsSimpleChoiceConstraint : bool

    /// Gets further information about a choice constraint
    member SimpleChoices : IList<FSharpType>

    /// Indicates a constraint that a type has a parameterless constructor 
    member IsRequiresDefaultConstructorConstraint  : bool

    /// Indicates a constraint that a type is an enum with the given underlying 
    member IsEnumConstraint : bool

    /// Gets further information about an enumeration constraint
    member EnumConstraintTarget : FSharpType 
    
    /// Indicates a constraint that a type is a delegate from the given tuple of args to the given return type 
    member IsDelegateConstraint : bool

    /// Gets further information about a delegate constraint
    member DelegateConstraintData : FSharpGenericParameterDelegateConstraint


#if COMPILER_PUBLIC_API
and [<RequireQualifiedAccess>] FSharpInlineAnnotation = 
#else
and [<RequireQualifiedAccess>] internal FSharpInlineAnnotation = 
#endif
   /// Indictes the value is inlined and compiled code for the function does not exist
   | PseudoValue
   /// Indictes the value is inlined but compiled code for the function still exists, e.g. to satisfy interfaces on objects, but that it is also always inlined 
   | AlwaysInline 
   /// Indictes the value is optionally inlined 
   | OptionalInline 
   /// Indictes the value is never inlined 
   | NeverInline 

/// A subtype of F# symbol that represents an F# method, property, event, function or value, including extension members.
#if COMPILER_PUBLIC_API
and [<Class>] FSharpMemberOrFunctionOrValue = 
#else
and [<Class>] internal FSharpMemberOrFunctionOrValue = 
#endif

    inherit FSharpSymbol
    internal new : Impl.cenv * ValRef -> FSharpMemberOrFunctionOrValue
    internal new : Impl.cenv * Infos.MethInfo -> FSharpMemberOrFunctionOrValue

    /// Indicates if the member, function or value is in an unresolved assembly 
    member IsUnresolved : bool

    /// Get the enclosing entity for the definition
    member EnclosingEntity : FSharpEntity
    
    /// Get the declaration location of the member, function or value
    member DeclarationLocation: range
    
    /// Get the typars of the member, function or value
    member GenericParameters: IList<FSharpGenericParameter>

    /// Get the full type of the member, function or value when used as a first class value
    member FullType: FSharpType

    /// Indicates if this is a compiler generated value
    member IsCompilerGenerated : bool

    /// Get a result indicating if this is a must-inline value
    member InlineAnnotation : FSharpInlineAnnotation

    /// Indicates if this is a mutable value
    member IsMutable : bool

    /// Indicates if this is a module or member value
    member IsModuleValueOrMember : bool

    /// Indicates if this is an extension member?
    member IsExtensionMember : bool

    [<System.Obsolete("Renamed to IsOverrideOrExplicitInterfaceImplementation")>]
    member IsOverrideOrExplicitMember : bool

    /// Indicates if this is an 'override', 'default' or an explicit implementation of an interface member
    member IsOverrideOrExplicitInterfaceImplementation : bool

    /// Indicates if this is an explicit implementation of an interface member
    member IsExplicitInterfaceImplementation : bool

    /// Gets the list of the abstract slot signatures implemented by the member
    member ImplementedAbstractSignatures : IList<FSharpAbstractSignature>

    /// Indicates if this is a member, including extension members?
    member IsMember : bool

    /// Indicates if this is a property member
    member IsProperty : bool

    /// Indicates if this is a property and there exists an associated getter method
    member HasGetterMethod : bool

    /// Get an associated getter method of the property
    member GetterMethod : FSharpMemberOrFunctionOrValue

    /// Indicates if this is a property and there exists an associated setter method
    member HasSetterMethod : bool

    /// Get an associated setter method of the property
    member SetterMethod : FSharpMemberOrFunctionOrValue

    /// Get an associated add method of an event
    member EventAddMethod : FSharpMemberOrFunctionOrValue

    /// Get an associated remove method of an event
    member EventRemoveMethod : FSharpMemberOrFunctionOrValue

    /// Get an associated delegate type of an event
    member EventDelegateType : FSharpType

    /// Indicate if an event can be considered to be a property for the F# type system of type IEvent or IDelegateEvent. 
    /// In this case ReturnParameter will have a type corresponding to the property type.  For 
    /// non-standard events, ReturnParameter will have a type corresponding to the delegate type.
    member EventIsStandard: bool

    /// Indicates if this is an event member
    member IsEvent : bool

    /// Gets the event symbol implied by the use of a property,
    /// for the case where the property is actually an F#-declared CLIEvent.
    /// 
    /// Uses of F#-declared events are considered to be properties as far as the language specification 
    /// and this API are concerned.  
    member EventForFSharpProperty : FSharpMemberOrFunctionOrValue option

    /// Indicates if this is an abstract member?
    member IsDispatchSlot : bool

    /// Indicates if this is a getter method for a property, or a use of a property in getter mode
    [<System.Obsolete("Renamed to IsPropertyGetterMethod, which returns 'true' only for method symbols, not for property symbols")>]
    member IsGetterMethod: bool 

    /// Indicates if this is a setter method for a property, or a use of a property in setter mode
    [<System.Obsolete("Renamed to IsPropertySetterMethod, which returns 'true' only for method symbols, not for property symbols")>]
    member IsSetterMethod: bool 

    /// Indicates if this is a getter method for a property, or a use of a property in getter mode
    member IsPropertyGetterMethod: bool 

    /// Indicates if this is a setter method for a property, or a use of a property in setter mode
    member IsPropertySetterMethod: bool 

    /// Indicates if this is an add method for an event
    member IsEventAddMethod: bool 

    /// Indicates if this is a remove method for an event
    member IsEventRemoveMethod: bool 

    /// Indicates if this is an instance member, when seen from F#?
    member IsInstanceMember : bool 
    
    /// Indicates if this is an instance member in compiled code. 
    ///
    /// Explanatory note: some members such as IsNone and IsSome on types with UseNullAsTrueValue appear 
    /// as instance members in F# code but are compiled as static members.
    member IsInstanceMemberInCompiledCode : bool 
    
    /// Indicates if this is an implicit constructor?
    member IsImplicitConstructor : bool
    
    /// Indicates if this is an F# type function
    member IsTypeFunction : bool

    /// Indicates if this value or member is an F# active pattern
    member IsActivePattern : bool
      
    /// Get the member name in compiled code
    member CompiledName: string

    /// Get the logical name of the member
    member LogicalName: string

    /// Get the logical enclosing entity, which for an extension member is type being extended
    member LogicalEnclosingEntity: FSharpEntity

    /// Get the name as presented in F# error messages and documentation
    member DisplayName : string

    member CurriedParameterGroups : IList<IList<FSharpParameter>>

    /// Gets the overloads for the current method
    /// matchParameterNumber indicates whether to filter the overloads to match the number of parameters in the current symbol
    member Overloads : bool -> IList<FSharpMemberOrFunctionOrValue> option

    member ReturnParameter : FSharpParameter

    /// Custom attributes attached to the value. These contain references to other values (i.e. constructors in types). Mutable to fixup  
    /// these value references after copying a collection of values. 
    member Attributes: IList<FSharpAttribute>

    /// Get the in-memory XML documentation for the value, used when code is checked in-memory
    member XmlDoc: IList<string>

    /// XML documentation signature for the value, used for .xml file lookup for compiled code
    member XmlDocSig: string

    /// Indicates if this is "base" in "base.M(...)"
    member IsBaseValue : bool

    /// Indicates if this is the "x" in "type C() as x = ..."
    member IsConstructorThisValue : bool

    /// Indicates if this is the "x" in "member x.M = ..."
    member IsMemberThisValue : bool

    /// Indicates if this is a [<Literal>] value, and if so what value? (may be null)
    member LiteralValue : obj option

    /// Get the accessibility information for the member, function or value
    member Accessibility : FSharpAccessibility

    /// Indicated if this is a value compiled to a method
    member IsValCompiledAsMethod : bool

    /// Indicates if this is a constructor.
    member IsConstructor : bool


/// A subtype of FSharpSymbol that represents a parameter 
#if COMPILER_PUBLIC_API
and [<Class>] FSharpParameter =
#else
and [<Class>] internal FSharpParameter =
#endif
    inherit FSharpSymbol

    /// The optional name of the parameter 
    member Name: string option

    /// The declaration location of the parameter 
    member DeclarationLocation : range 

    /// The declared or inferred type of the parameter 
    member Type : FSharpType 

    /// The declared attributes of the parameter 
    member Attributes: IList<FSharpAttribute>

    /// Indicate this is a param array argument
    member IsParamArrayArg: bool

    /// Indicate this is an out argument
    member IsOutArg: bool

    /// Indicate this is an optional argument
    member IsOptionalArg: bool


/// A subtype of FSharpSymbol that represents a single case within an active pattern
#if COMPILER_PUBLIC_API
and [<Class>] FSharpActivePatternCase =
#else
and [<Class>] internal FSharpActivePatternCase =
#endif

    inherit FSharpSymbol

    /// The name of the active pattern case 
    member Name: string 

    /// The location of declaration of the active pattern case 
    member DeclarationLocation : range 

    /// The group of active pattern cases this belongs to
    member Group : FSharpActivePatternGroup

    /// Get the in-memory XML documentation for the active pattern case, used when code is checked in-memory
    member XmlDoc: IList<string>

      /// XML documentation signature for the active pattern case, used for .xml file lookup for compiled code
    member XmlDocSig: string

/// Represents all cases within an active pattern
#if COMPILER_PUBLIC_API
and [<Class>] FSharpActivePatternGroup =
#else
and [<Class>] internal FSharpActivePatternGroup =
#endif
    /// The names of the active pattern cases
    member Names: IList<string> 

    /// Indicate this is a total active pattern
    member IsTotal : bool 

    /// Get the type indicating signature of the active pattern
    member OverallType : FSharpType

    /// Try to get the enclosing entity of the active pattern
    member EnclosingEntity : FSharpEntity option

#if COMPILER_PUBLIC_API
and [<Class>] FSharpType =
#else
and [<Class>] internal FSharpType =
#endif
    /// Internal use only. Create a ground type.
    internal new : g:TcGlobals * thisCcu: CcuThunk * tcImports: TcImports * typ:TType -> FSharpType
    internal new : Impl.cenv * typ:TType -> FSharpType

    /// Indicates this is a named type in an unresolved assembly 
    member IsUnresolved : bool

    /// Indicates this is an abbreviation for another type
    member IsAbbreviation : bool

    /// Get the type for which this is an abbreviation
    member AbbreviatedType : FSharpType

    /// Indicates if the type is constructed using a named entity, including array and byref types
    member HasTypeDefinition : bool

    /// Get the type definition for a type 
    member TypeDefinition : FSharpEntity 

    /// Get the generic arguments for a tuple type, a function type or a type constructed using a named entity
    member GenericArguments : IList<FSharpType>
    
    /// Indicates if the type is a tuple type (reference or struct). The GenericArguments property returns the elements of the tuple type.
    member IsTupleType : bool

    /// Indicates if the type is a struct tuple type. The GenericArguments property returns the elements of the tuple type.
    member IsStructTupleType : bool

    /// Indicates if the type is a function type. The GenericArguments property returns the domain and range of the function type.
    member IsFunctionType : bool

    /// Indicates if the type is a variable type, whether declared, generalized or an inference type parameter  
    member IsGenericParameter : bool

    /// Get the generic parameter data for a generic parameter type
    member GenericParameter : FSharpGenericParameter

    /// Format the type using the rules of the given display context
    member Format : context: FSharpDisplayContext -> string

    /// Instantiate generic type parameters in a type
    member Instantiate : (FSharpGenericParameter * FSharpType) list -> FSharpType

    /// Get all the interface implementations, by walking the type hierarchy, taking into account the instantiation of this type
    /// if it is an instantiation of a generic type.
    member AllInterfaces : IList<FSharpType>  

    /// Get the base type, if any, taking into account the instantiation of this type
    /// if it is an instantiation of a generic type.
    member BaseType : FSharpType option

    /// Adjust the type by removing any occurrences of type inference variables, replacing them
    /// systematically with lower-case type inference variables such as <c>'a</c>.
    static member Prettify : typ:FSharpType -> FSharpType

    /// Adjust a group of types by removing any occurrences of type inference variables, replacing them
    /// systematically with lower-case type inference variables such as <c>'a</c>.
    static member Prettify : types: IList<FSharpType> -> IList<FSharpType>

    /// Adjust the type in a single parameter by removing any occurrences of type inference variables, replacing them
    /// systematically with lower-case type inference variables such as <c>'a</c>.
    static member Prettify : parameter: FSharpParameter -> FSharpParameter 

    /// Adjust the types in a group of parameters by removing any occurrences of type inference variables, replacing them
    /// systematically with lower-case type inference variables such as <c>'a</c>.
    static member Prettify : parameters: IList<FSharpParameter> -> IList<FSharpParameter>

    /// Adjust the types in a group of curried parameters by removing any occurrences of type inference variables, replacing them
    /// systematically with lower-case type inference variables such as <c>'a</c>.
    static member Prettify : parameters: IList<IList<FSharpParameter>> -> IList<IList<FSharpParameter>>

    /// Adjust the types in a group of curried parameters and return type by removing any occurrences of type inference variables, replacing them
    /// systematically with lower-case type inference variables such as <c>'a</c>.
    static member Prettify : parameters: IList<IList<FSharpParameter>> * returnParameter: FSharpParameter -> IList<IList<FSharpParameter>> * FSharpParameter

    [<System.Obsolete("Renamed to HasTypeDefinition")>]
    member IsNamedType : bool

    [<System.Obsolete("Renamed to TypeDefinition")>]
    member NamedEntity : FSharpEntity 


/// Represents a custom attribute attached to F# source code or a compiler .NET component
#if COMPILER_PUBLIC_API
and [<Class>] FSharpAttribute = 
#else
and [<Class>] internal FSharpAttribute = 
#endif

    /// The type of the attribute
    member AttributeType : FSharpEntity

    /// The arguments to the constructor for the attribute
    member ConstructorArguments : IList<FSharpType * obj>

    /// The named arguments for the attribute
    member NamedArguments : IList<FSharpType * string * bool * obj>

    /// Indicates if the attribute type is in an unresolved assembly 
    member IsUnresolved : bool

    /// Format the attribute using the rules of the given display context
    member Format : context: FSharpDisplayContext -> string



/// Represents the use of an F# symbol from F# source code
[<Sealed>]
#if COMPILER_PUBLIC_API
type FSharpSymbolUse = 
#else
type internal FSharpSymbolUse = 
#endif
    // For internal use only
    internal new : g:TcGlobals * denv: Tastops.DisplayEnv * symbol:FSharpSymbol * itemOcc:ItemOccurence * range: range -> FSharpSymbolUse

    /// The symbol referenced
    member Symbol : FSharpSymbol 

    /// The display context active at the point where the symbol is used. Can be passed to FSharpType.Format
    /// and other methods to format items in a way that is suitable for a specific source code location.
    member DisplayContext : FSharpDisplayContext

    /// Indicates if the reference is a definition for the symbol, either in a signature or implementation
    member IsFromDefinition : bool

    /// Indicates if the reference is in a pattern
    member IsFromPattern : bool

    /// Indicates if the reference is in a syntactic type
    member IsFromType : bool

    /// Indicates if the reference is in an attribute
    member IsFromAttribute : bool

    /// Indicates if the reference is via the member being implemented in a class or object expression
    member IsFromDispatchSlotImplementation : bool

    /// Indicates if the reference is either a builder or a custom operation in a computation expression
    member IsFromComputationExpression : bool

    /// The file name the reference occurs in 
    member FileName: string 

    /// The range of text representing the reference to the symbol
    member RangeAlternate: range
