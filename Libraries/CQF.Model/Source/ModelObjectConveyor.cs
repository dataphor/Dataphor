/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Alphora.Dataphor.DAE.Streams;
using CQF.Model;

namespace Alphora.Dataphor.CQF.Model
{
	public class ModelObjectConveyor : Conveyor
	{
		public ModelObjectConveyor() : base() {}
		
		public override bool IsStreaming { get { return base.IsStreaming; } }

		// This "solution" sucks, but I'm tired of dinking around with Xml Serialization.
		private static XmlSerializer ModelInfoSerializer = new XmlSerializer(typeof(ModelInfo), new Type[] { typeof(ModelSpecifier), typeof(TypeInfo), typeof(TypeSpecifier), typeof(ConversionInfo), typeof(ClassInfoElement), typeof(TupleTypeInfoElement) });
		public static XmlSerializer ModelSpecifierSerializer = new XmlSerializer(typeof(ModelSpecifier), new Type[] { typeof(ModelInfo), typeof(TypeInfo), typeof(TypeSpecifier), typeof(ConversionInfo), typeof(ClassInfoElement), typeof(TupleTypeInfoElement) });
		public static XmlSerializer TypeInfoSerializer = new XmlSerializer(typeof(TypeInfo), new Type[] { typeof(ModelSpecifier), typeof(ModelInfo), typeof(TypeSpecifier), typeof(ConversionInfo), typeof(ClassInfoElement), typeof(TupleTypeInfoElement) });
		public static XmlSerializer TypeSpecifierSerializer = new XmlSerializer(typeof(TypeSpecifier), new Type[] { typeof(ModelSpecifier), typeof(TypeInfo), typeof(ModelInfo), typeof(ConversionInfo), typeof(ClassInfoElement), typeof(TupleTypeInfoElement) });
		public static XmlSerializer ConversionInfoSerializer = new XmlSerializer(typeof(ConversionInfo), new Type[] { typeof(ModelSpecifier), typeof(TypeInfo), typeof(TypeSpecifier), typeof(ModelInfo), typeof(ClassInfoElement), typeof(TupleTypeInfoElement) });
		public static XmlSerializer ClassInfoElementSerializer = new XmlSerializer(typeof(ClassInfoElement), new Type[] { typeof(ModelSpecifier), typeof(TypeInfo), typeof(TypeSpecifier), typeof(ConversionInfo), typeof(ModelInfo), typeof(TupleTypeInfoElement) });
		public static XmlSerializer TupleTypeInfoElementSerializer = new XmlSerializer(typeof(TupleTypeInfoElement), new Type[] { typeof(ModelSpecifier), typeof(TypeInfo), typeof(TypeSpecifier), typeof(ConversionInfo), typeof(ClassInfoElement), typeof(ModelInfo) });

		public static XmlSerializer GetSerializerForType(Type type)
		{
			if (type.Equals(typeof(ModelInfo))) return ModelInfoSerializer;
			if (type.Equals(typeof(ModelSpecifier))) return ModelSpecifierSerializer;
			if (type.Equals(typeof(TypeInfo)) || type.IsSubclassOf(typeof(TypeInfo))) return TypeInfoSerializer;
			if (type.Equals(typeof(TypeSpecifier)) || type.IsSubclassOf(typeof(TypeInfo))) return TypeSpecifierSerializer;
			if (type.Equals(typeof(ConversionInfo))) return ConversionInfoSerializer;
			if (type.Equals(typeof(ClassInfoElement))) return ClassInfoElementSerializer;
			if (type.Equals(typeof(TupleTypeInfoElement))) return TupleTypeInfoElementSerializer;

			throw new ArgumentException("Could not determine serializer for type.");
		}

		public static object Deserialize(TextReader reader)
		{
			using (var xmlReader = XmlReader.Create(reader))
			{
				if (ModelInfoSerializer.CanDeserialize(xmlReader))
					return ModelInfoSerializer.Deserialize(xmlReader);

				if (ModelSpecifierSerializer.CanDeserialize(xmlReader))
					return ModelSpecifierSerializer.Deserialize(xmlReader);

				if (TypeInfoSerializer.CanDeserialize(xmlReader))
					return TypeInfoSerializer.Deserialize(xmlReader);

				if (TypeSpecifierSerializer.CanDeserialize(xmlReader))
					return TypeSpecifierSerializer.Deserialize(xmlReader);

				if (ConversionInfoSerializer.CanDeserialize(xmlReader))
					return ConversionInfoSerializer.Deserialize(xmlReader);

				if (ClassInfoElementSerializer.CanDeserialize(xmlReader))
					return ClassInfoElementSerializer.Deserialize(xmlReader);

				if (TupleTypeInfoElementSerializer.CanDeserialize(xmlReader))
					return TupleTypeInfoElementSerializer.Deserialize(xmlReader);

				throw new Exception("Could not deserialize model info component.");
			}
		}

		public static object Deserialize(String value)
		{
			using (var reader = new StringReader(value))
			{
				return Deserialize(reader);
			}
		}

		public static string Serialize(Object value)
		{
			using (var stream = new StringWriter())
			{
				if (value != null)
				{
					GetSerializerForType(value.GetType()).Serialize(stream, value);
				}
				return stream.ToString();
			}
		}
		
		public override object Read(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				return Deserialize(reader);
			}
		}
		
		public override void Write(object tempValue, Stream stream)
		{
			if (tempValue != null)
			{
				GetSerializerForType(tempValue.GetType()).Serialize(stream, tempValue);
			}
		}
	}
}
