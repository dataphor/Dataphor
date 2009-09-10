using System;

namespace Alphora.Dataphor.BOP
{
	/// <summary> Events to customize object when serialzed and deserialized. </summary>
	// Specifily implemeted to handle circular containership.
	public interface IBOPSerializationEvents
	{
		void BeforeSerialize(Serializer ASender);
		void AfterSerialize(Serializer ASender);
		void AfterDeserialize(Deserializer ASender);
	}

	public delegate object FindReferenceHandler(string AString);

	public delegate void DeserializedObjectHandler(object AObject);
}
