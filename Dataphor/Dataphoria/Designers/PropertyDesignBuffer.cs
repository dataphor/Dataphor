using System;
using System.ComponentModel;
using System.IO;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public class PropertyDesignBuffer : DesignBuffer
	{
		public PropertyDesignBuffer(IDataphoria ADataphoria, object AInstance, PropertyDescriptor ADescriptor)
			: base(ADataphoria)
		{
			FInstance = AInstance;
			FDescriptor = ADescriptor;
			FID = Guid.NewGuid();
		}

		// Instance

		private object FInstance;
		public object Instance
		{
			get { return FInstance; }
		}

		// Descriptor

		private PropertyDescriptor FDescriptor;
		public PropertyDescriptor Descriptor
		{
			get { return FDescriptor; }
		}

		// ID

		private Guid FID;
		public Guid ID
		{
			get { return FID; }
		}

		// DesignBuffer

		public override string GetDescription()
		{
			Frontend.Client.INode LNode = FInstance as Frontend.Client.INode;
			return (LNode != null ? LNode.Name + "." : String.Empty) + FDescriptor.Name;
		}

		public override bool Equals(object AObject)
		{
			PropertyDesignBuffer LBuffer = AObject as PropertyDesignBuffer;
			if ((LBuffer != null) && Object.ReferenceEquals(FInstance, LBuffer.Instance) && FDescriptor.Equals(LBuffer.Descriptor))
				return true;
			else
				return base.Equals(AObject);
		}

		public override int GetHashCode()
		{
			return FInstance.GetHashCode() ^ FDescriptor.GetHashCode();
		}

		// Data

		public override void SaveData(string AData)
		{
			FDescriptor.SetValue(FInstance, AData);
		}

		public override void SaveBinaryData(Stream AData)
		{
			Error.Fail("SaveBinaryData is not supported for PropertyDesignBuffer");
		}

		public override string LoadData()
		{
			return (string)FDescriptor.GetValue(FInstance);
		}

		public override void LoadData(Stream AData)
		{
			Error.Fail("LoadData(Stream) is not supported for PropertyDesignBuffer");
		}

		public override string GetLocatorName()
		{
			return null;
		}

		public override bool LocatorNameMatches(string AName)
		{
			return false;
		}
	}
}
