/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Schema = Alphora.Dataphor.DAE.Schema;
using Hl7.Fhir.Model;

namespace Alphora.Dataphor.FHIR.Core
{
	public class GenerateTypesNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			Initialize();

			// Generate types for all descendents of Fhir.Base
			Type[] types = typeof(Base).Assembly.GetTypes();

			foreach (Type type in types)
			{
				if (type.Equals(typeof(Base)) || type.IsSubclassOf(typeof(Base)))
					if (!type.IsGenericTypeDefinition)
						GenerateType(program, type);
			}

			return new D4TextEmitter().Emit(_statements);
		}

		private Dictionary<String, String> _typeNameMap;
		private Block _statements;

		private void Initialize()
		{
			_typeNameMap = new Dictionary<String, String>();
			_statements = new Block();

			MapPrimitiveTypeNames();
		}

		private void MapPrimitiveTypeNames()
		{
			_typeNameMap.Add("System.String", "String");
			_typeNameMap.Add("System.Int32", "Integer");
			_typeNameMap.Add("System.Boolean", "Boolean");
			_typeNameMap.Add("System.DateTime", "DateTime");
			_typeNameMap.Add("System.DateTimeOffset", "DateTime");
			_typeNameMap.Add("System.Decimal", "Decimal");
			_typeNameMap.Add("System.Uri", "String"); // Map Uri to string
			_typeNameMap.Add("System.Byte[]", "Binary");
		}

		private TypeSpecifier GenerateType(Program program, Type type)
		{
			if (type.IsEnum)
				return GenerateType(program, typeof(String)); // Expose as a String, would be nice to restrict in D4, but...

			if (type.IsGenericType && type.BaseType == typeof(Primitive))
				return GenerateType(program, type.BaseType.BaseType); // Skip Primitive<T> and Primitive, they are unnecessary in D4

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				return GenerateType(program, type.GetGenericArguments()[0]); // Skip Nullable<T>, it is unnecessary in D4

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Code<>))
				return GenerateType(program, typeof(Code)); // Expose as a Code. Would be nice to restrict in D4, but....

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
				return new ListTypeSpecifier(GenerateType(program, type.GetGenericArguments()[0]));

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				return new ListTypeSpecifier(GenerateType(program, type.GetGenericArguments()[0]));

			String d4TypeName;
			if (!_typeNameMap.TryGetValue(type.FullName, out d4TypeName))
			{
				d4TypeName = GetD4TypeName(type.FullName);
				_typeNameMap.Add(type.FullName, d4TypeName);

				if (type.Equals(typeof(Base)))
				{
					GenerateBaseType(program, type, d4TypeName);
				}
				else if (type.Equals(typeof(Element)))
				{
					GenerateElementType(program, type, d4TypeName);
				}
				else if (type.Equals(typeof(Extension)))
				{
					GenerateExtensionType(program, type, d4TypeName);
				}
				//else if (type.Equals(typeof(Resource)))
				//{
				//	GenerateResourceType(program, type, d4TypeName);
				//}
				//else if (type.IsSubclassOf(typeof(Element)))
				//{
				//	GenerateElementSubclassType(program, type, d4TypeName);
				//}
				//else if (type.IsSubclassOf(typeof(Resource)))
				//{
				//	GenerateResourceSubclassType(program, type, d4TypeName);
				//}
				else
				{
					GenerateSubclassType(program, type, d4TypeName);
				}
				//	throw new InvalidOperationException(String.Format("Invalid type encountered: {0}", type.FullName));
			}

			return new ScalarTypeSpecifier(d4TypeName);
		}

		public static string GetD4TypeName(string typeName)
		{
			switch (Schema.Object.Unqualify(typeName))
			{
				case "FhirString" : return "FHIRString";
				case "FhirBoolean" : return "FHIRBoolean";
				case "Integer" : return "FHIRInteger";
				case "Time" : return "FHIRTime";
				case "Date" : return "FHIRDate";
				case "FhirDecimal" : return "FHIRDecimal";
				case "FhirDateTime" : return "FHIRDateTime";
				case "FhirUri" : return "FHIRUri";
				default : return Schema.Object.Unqualify(typeName).Replace("+", ".");
			}
		}

		private void GenerateExtensionType(Program program, Type type, string d4TypeName)
		{
			// Use the manual type for now
		}

		private void GenerateResourceType(Program program, Type type, string d4TypeName)
		{
			// Use the manual declaration for now
		}

		private void GenerateElementType(Program program, Type type, string d4TypeName)
		{
			// Use the manual declaration for now
		}

		private void GenerateBaseType(Program program, Type type, string d4TypeName)
		{
			// Use the manual declaration for now
		}

		private void GenerateSubclassType(Program program, Type type, string d4TypeName)
		{
			// create type <d4 type name> from class <native type name> is { <parent d4 type name> } { representation <d4 type name> { <properties> } };
			// foreach property:
				// <property name> : <d4 type name>
			var statement = new CreateScalarTypeStatement();
			statement.ScalarTypeName = d4TypeName;
			statement.FromClassDefinition = new ClassDefinition(type.FullName);
			if (type.BaseType != null && !type.BaseType.Equals(typeof(System.Object)))
			{
				var scalarTypeSpecifier = GenerateType(program, type.BaseType) as ScalarTypeSpecifier;
				if (scalarTypeSpecifier == null)
					throw new InvalidOperationException(String.Format("Scalar type specifier expected: {0}.", type.BaseType.Name));
				statement.ParentScalarTypes.Add(new ScalarTypeNameDefinition(scalarTypeSpecifier.ScalarTypeName));
			}

			_statements.Statements.Add(statement);

			var defaultRepresentation = new RepresentationDefinition(d4TypeName);
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
				if 
				(
					property.CanRead 
						&& property.CanWrite 
						&& property.GetGetMethod() != null
						&& property.GetGetMethod().GetBaseDefinition().DeclaringType == type 
						&& property.GetSetMethod() != null
						&& property.GetSetMethod().GetBaseDefinition().DeclaringType == type
				)
					defaultRepresentation.Properties.Add(new PropertyDefinition(property.Name, GenerateType(program, property.PropertyType)));

			if (defaultRepresentation.Properties.Count > 0)
			{
				var alterStatement = new AlterScalarTypeStatement();
				alterStatement.ScalarTypeName = d4TypeName;
				alterStatement.CreateRepresentations.Add(defaultRepresentation);
				_statements.Statements.Add(alterStatement);
			}
		}
	}
}
