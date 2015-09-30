using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.DAE.Client.Controls
{
	public static class DateTimeExtension
	{
		public static DateTime AddMeridian(this DateTime dateTime, int step)
		{
			if (step > 0 && dateTime.TimeOfDay.Hours < 12)
			{
				return dateTime.AddHours(12);
			}
			else if (step < 0 && dateTime.TimeOfDay.Hours >= 12)
			{
				return dateTime.AddHours(-12);
			}

			return dateTime;
		}
	}
}
