using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public interface ICursor : IDataValue
	{
		new Schema.ICursorType DataType { get; }
		int ID { get; }
	}
}
