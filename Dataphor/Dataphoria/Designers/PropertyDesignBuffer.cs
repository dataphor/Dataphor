using System;
using System.ComponentModel;
using System.IO;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public class PropertyDesignBuffer : DesignBuffer
	{
		public PropertyDesignBuffer(IDataphoria dataphoria, object instance, PropertyDescriptor descriptor)
			: base(dataphoria, null)
		{
			_instance = instance;
			_descriptor = descriptor;
			_iD = Guid.NewGuid();
		}

		// Instance

		private object _instance;
		public object Instance
		{
			get { return _instance; }
		}

		// Descriptor

		private PropertyDescriptor _descriptor;
		public PropertyDescriptor Descriptor
		{
			get { return _descriptor; }
		}

		// ID

		private Guid _iD;
		public Guid ID
		{
			get { return _iD; }
		}

		// DesignBuffer

		public override string GetDescription()
		{
			Frontend.Client.INode node = _instance as Frontend.Client.INode;
			return (node != null ? node.Name + "." : String.Empty) + _descriptor.Name;
		}

		public override bool Equals(object objectValue)
		{
			PropertyDesignBuffer buffer = objectValue as PropertyDesignBuffer;
			if ((buffer != null) && Object.ReferenceEquals(_instance, buffer.Instance) && _descriptor.Equals(buffer.Descriptor))
				return true;
			else
				return base.Equals(objectValue);
		}

		public override int GetHashCode()
		{
			return _instance.GetHashCode() ^ _descriptor.GetHashCode();
		}

		// Data

		public override void SaveData(string data)
		{
			_descriptor.SetValue(_instance, data);
		}

		public override void SaveBinaryData(Stream data)
		{
			Error.Fail("SaveBinaryData is not supported for PropertyDesignBuffer");
		}

		public override string LoadData()
		{
			return (string)_descriptor.GetValue(_instance);
		}

		public override void LoadData(Stream data)
		{
			Error.Fail("LoadData(Stream) is not supported for PropertyDesignBuffer");
		}

		public override bool LocatorNameMatches(string name)
		{
			return false;
		}
	}
}
