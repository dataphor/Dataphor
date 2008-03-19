/*
	DB2 SQL Implementation Script for the DAE_Frac & trunc2 function that is mapped to the DB2 Device
*/	

create function DAE_Frac(AValue float)
returns float
return AValue - Trunc(AValue, 0)

create function DAE_Trunc(AValue decimal(28,8))
returns decimal
return Trunc(AValue, 0)

/*
	DB2 SQL Implementation Scripts for TimeSpan functions that are mapped to the DB2 Device
*/

create function DAE_TSReadMillisecond(AValue bigint)
returns bigint
return Trunc(DAE_Frac(AValue / (10000.0 * 1000)) * 1000,0) 

create function DAE_TSReadSecond(AValue bigint) 
returns integer
return Trunc(DAE_Frac(AValue / (10000000.0 * 60)) * 60,0)

create function DAE_TSReadMinute(AValue bigint)
returns bigint
return Trunc(DAE_Frac(AValue/ (600000000.0 * 60)) * 60,0)

create function DAE_TSReadHour(AValue bigint)
returns bigint
return Trunc(DAE_Frac(AValue / (3600000000.0 * 24)) * 24,0) 

create function DAE_TSReadDay(AValue bigint) 
returns bigint
return Trunc(AValue / 86400000000.0, 0)

create function DAE_TSWriteMillisecond(AValue bigint, APart bigint) 
returns bigint
return AValue + (APart - DAE_TSReadMillisecond(AValue)) * 10000

create function DAE_TSWriteSecond(AValue bigint, APart bigint) 
returns bigint
return AValue + (APart - DAE_TSReadSecond(AValue)) * 10000000

create function DAE_TSWriteMinute(AValue bigint, APart bigint)  
returns bigint
return AValue + (APart - DAE_TSReadMinute(AValue)) * 600000000

create function DAE_TSWriteHour(AValue bigint, APart bigint) 
returns bigint
return AValue + (APart - DAE_TSReadHour(AValue)) * 36000000000

create function DAE_TSWriteDay(AValue bigint, APart bigint)
returns bigint
return AValue + (APart - DAE_TSReadDay(AValue)) * 864000000000

create function DAE_TimeSpan(timestamp ADateTime)
returns bigint
return Days(ADateTime) + Hour(ADateTime) * 36000000000 + Minute(ADateTime) * 600000000 + Second(ADateTime) * 10000000 + Microsecond(ADateTime) * 10000

/*
	datetime functions
*/

create function DAE_AddMonths(x timestamp, y integer)
returns timestamp
return x + (y months)

create function DAE_AddYears(x timestamp, y integer)
returns timestamp
return x + (y years)

create function DAE_DayOfMonth(x timestamp)
returns integer
return day(x)

create function DAE_DayOfYear(x timestamp)
returns integer
return DayOfYear(x)

create function DAE_DTReadMonth(x timestamp)
returns integer
return month(x)

create function DAE_DTReadYear(x timestamp)
returns integer
return year(x)

create function DAE_DTReadDay(x timestamp)
returns integer
return day(x)

create function DAE_DTReadHour(x timestamp)
returns integer
return hour(x)

create function DAE_DTReadMinute(x timestamp)
returns integer
return minute(x)

create function DAE_DTReadSecond(x timestamp)
returns integer
return second(x)

create function DAE_DTReadMillisecond(x timestamp)
returns integer
return microsecond(x) * 1000

create function DAE_DatePart(x timestamp)
returns date
return date(x)

create function DAE_TimePart(x timestamp)
returns time
return time(x)

create function DAE_DayOfWeek(x timestamp)
returns integer
return dayofweek(x)

create function DAE_Now()
returns time
return current time

create function DAE_Today()
returns date
return current date

create function DAE_DTWriteMillisecond(x timestamp, y integer)
returns timestamp
return (x - (microsecond(x) microseconds)) + ((y*1000) microseconds)

create function DAE_DTWriteSecond(x timestamp, y integer)
returns timestamp
return (x - (second(x) seconds)) + (y seconds)

create function DAE_DTWriteMinute(x timestamp, y integer)
returns timestamp
return (x - (minute(x) minutes)) + (y minutes)

create function DAE_DTWriteHour(x timestamp, y integer)
returns timestamp
return (x - (hour(x) hour)) + (y hours)

create function DAE_DTWriteDay(x timestamp, y integer)
returns timestamp
return (x - (day(x) day)) + (y days)

create function DAE_DTWriteMonth(x timestamp, y integer)
returns timestamp
return (x - (month(x) month)) + (y months)

create function DAE_DTWriteYear(x timestamp, y integer)
returns timestamp
return (x - ((year(x) -1) years)) + ((y - 1) years)   

create function DAE_DaysInMonth (year integer, month integer) 
returns integer
return day(DAE_AddMonths(DAE_DTWriteDay(DAE_DTWriteMonth(DAE_DTWriteYear(current timestamp, year), month), 1), 1) - 1 day ) 

create function DAE_IsLeapYear(year integer)
returns integer
return DaysInMonth(year, 2) - 28

/*
	Date Time Selectors
*/

create function DAE_DateTimeSelector1(year integer)
returns timestamp
return DAE_DTWriteYear(timestamp('0001-01-01-00.00.00.000000'), year)

create function DAE_DateTimeSelector2(year integer, month integer)
returns timestamp
return DAE_DTWriteMonth(DAE_DTWriteYear(timestamp('0001-01-01-00.00.00.000000'), year), month)

create function DAE_DateTimeSelector3(year integer, month integer, day integer)
returns timestamp
return DAE_DTWriteDay(DAE_DTWriteMonth(DAE_DTWriteYear(timestamp('0001-01-01-00.00.00.000000'), year), month), day)

create function DAE_DateTimeSelector4(year integer, month integer, day integer, hour integer)
returns timestamp
return DAE_DTWriteHour(DAE_DTWriteDay(DAE_DTWriteMonth(DAE_DTWriteYear(timestamp('0001-01-01-00.00.00.000000'), year), month), day), hour)

create function DAE_DateTimeSelector5(year integer, month integer, day integer, hour integer, minute integer)
returns timestamp
return DAE_DTWriteMinute(DAE_DTWriteHour(DAE_DTWriteDay(DAE_DTWriteMonth(DAE_DTWriteYear(timestamp('0001-01-01-00.00.00.000000'), year), month), day), hour), minute)

create function DAE_DateTimeSelector6(year integer, month integer, day integer, hour integer, minute integer, second integer)
returns timestamp
return DAE_DTWriteSecond(DAE_DTWriteMinute(DAE_DTWriteHour(DAE_DTWriteDay(DAE_DTWriteMonth(DAE_DTWriteYear(timestamp('0001-01-01-00.00.00.000000'), year), month), day), hour), minute), second)

create function DAE_DateTimeSelector7(year integer, month integer, day integer, hour integer, minute integer, second integer, ms integer)
returns timestamp
return DAE_DTWriteMillisecond(DAE_DTWriteSecond(DAE_DTWriteMinute(DAE_DTWriteHour(DAE_DTWriteDay(DAE_DTWriteMonth(DAE_DTWriteYear(timestamp('0001-01-01-00.00.00.000000'), year), month), day), hour), minute), second), ms)

create function DAE_DateTime(ts bigint)
returns timestamp
return timestamp('0001-01-01-00.00.00') + 63082281600 seconds + (ts/10000000-63082281600) seconds + DAE_Frac (ts/10000000) microseconds


/*
	Date functions
*/


create function DAE_DateReadYear(x date)
returns integer
return year(x)
 
create function DAE_DateReadMonth(x date)
returns integer
return month(x)

create function DAE_DateReadDay(x date)
returns integer
return day(x)

create function DAE_DateWriteYear(x date, y integer)
returns date
return (x - ((year(x) -1) years)) + ((y - 1) years)  
       
create function DAE_DateWriteMonth(x date, y integer)
returns date
return (x - (month(x) month)) + (y months)

create function DAE_DateWriteDay(x date, y integer)
returns date
return (x - (day(x) day)) + (y days)

create function DAE_AddMonths(x date, y integer)
returns date
return x + (y months)

create function DAE_AddYears(x date, y integer)
returns date
return x + (y years)



/*
	Date Selectors
*/
create function DAE_DateSelector3(year integer, month integer, day integer)
returns date
return DAE_DateWriteDay(DAE_DateWriteMonth(DAE_DateWriteYear(date('0001-01-01'), year), month), day)


/*
	Time Functions
*/

create function DAE_TimeReadHour(x timestamp)
returns integer
return hour(x)

create function DAE_TimeReadMinute(x timestamp)
returns integer
return minute(x)

create function DAE_TimeReadSecond(x timestamp)
returns integer
return second(x)

create function DAE_TimeWriteSecond(x timestamp, y integer)
returns timestamp
return (x - (second(x) seconds)) + (y seconds)

create function DAE_TimeWriteMinute(x timestamp, y integer)
returns timestamp
return (x - (minute(x) minutes)) + (y minutes)

create function DAE_TimeWriteHour(x timestamp, y integer)
returns timestamp
return (x - (hour(x) hour)) + (y hours)

/*
    Time Selectors
*/

create function DAE_TimeSelector2(hour integer, minute integer, second integer)
returns Time
return DAE_TimeWriteSecond(DAE_TimeWriteMinute(DAE_TimeWriteHour(time('00.00.00.000000'), hour), minute), second)

create function DAE_TimeSelector3(hour integer, minute integer, second integer, ms integer)
returns Time
return DAE_TimeWriteMillisecond(DAE_TimeWriteSecond(DAE_TimeWriteMinute(DAE_TimeWriteHour(time('00.00.00.000000'), hour), minute), second), ms)


