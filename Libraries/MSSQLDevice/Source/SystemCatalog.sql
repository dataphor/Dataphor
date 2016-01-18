/*
	System Supported Operators
	
	© Copyright 2007 Alphora
*/

-- DAE_Trunc
if exists (select * from sysobjects where id = Object_ID('DAE_Trunc'))
	drop function DAE_Trunc
go

create function DAE_Trunc(@Value decimal(28,8))
returns decimal(28,8)
begin
	return Round(@Value,0,1)
end
go

grant execute on DAE_Trunc to public
go

-- DAE_Frac
if exists (select * from sysobjects where id = Object_ID('DAE_Frac'))
	drop function DAE_Frac
go

create function DAE_Frac(@Value decimal(28,8))
returns decimal(28,8)
begin
	return (@Value - Round(@Value,0,1))
end
go

grant execute on DAE_Frac to public
go

-- DAE_LogB
if exists (select * from sysobjects where id = Object_ID('DAE_LogB'))
	drop function DAE_LogB
go

create function DAE_LogB(@Value decimal(28,8), @Base decimal(28,8))
returns decimal(28,8)
begin
	return (Log(@Value) / Log(@Base))
end
go

grant execute on DAE_LogB to public
go

-- DAE_Factorial
if exists (select * from sysobjects where id = Object_ID('DAE_Factorial'))
	drop function DAE_Factorial
go

create function DAE_Factorial(@Value int)
returns int
begin
	declare @LReturnVal int;
	declare @i int;
	set @LReturnVal= 1;
	set @i = 1;
	while (@i <= @Value)
	begin
		set @LReturnVal= @LReturnVal * @i;
		set @i = @i + 1;
	end;
	return @LReturnVal;
end
go

grant execute on DAE_Factorial to public
go

-- DAE_TSReadMillisecond 
if exists (select * from sysobjects where id = Object_ID('DAE_TSReadMillisecond'))
	drop function DAE_TSReadMillisecond
go

create function DAE_TSReadMillisecond(@ATimeSpan bigint)
returns integer
begin
	declare @value decimal(28, 16), @fraction decimal(28, 28)
	set @value = @ATimeSpan / (10000.0 * 1000)
	set @fraction = @value - Round(@value,0,1) 
	return dbo.DAE_Trunc(@fraction * 1000);
end
go

grant execute on DAE_TSReadMillisecond to public
go

-- DAE_TSReadSecond 
if exists (select * from sysobjects where id = Object_ID('DAE_TSReadSecond'))
	drop function DAE_TSReadSecond
go

create function DAE_TSReadSecond(@ATimeSpan bigint)
returns integer
begin
	declare @value decimal(28, 17), @fraction decimal(28, 28)
	set @value = @ATimeSpan / (10000000.0 * 60)
	set @fraction = @value - Round(@value,0,1) 
	return dbo.DAE_Trunc(@fraction * 60);
end
go

grant execute on DAE_TSReadSecond to public
go

-- DAE_TSReadMinute
if exists (select * from sysobjects where id = Object_ID('DAE_TSReadMinute'))
	drop function DAE_TSReadMinute
go

create function DAE_TSReadMinute(@ATimeSpan bigint)
returns integer
begin
	declare @value decimal(28, 18), @fraction decimal(28, 28)
	set @value = @ATimeSpan / (600000000.0 * 60)
	set @fraction = @value - Round(@value,0,1) 
	return dbo.DAE_Trunc(@fraction * 60);
end
go

grant execute on DAE_TSReadMinute to public
go

-- DAE_TSReadHour
if exists (select * from sysobjects where id = Object_ID('DAE_TSReadHour'))
	drop function DAE_TSReadHour
go

create function DAE_TSReadHour(@ATimeSpan bigint)
returns integer
begin
	declare @value decimal(28, 20), @fraction decimal(28, 28)
	set @value = @ATimeSpan / (36000000000.0 * 24)
	set @fraction = @value - Round(@value,0,1) 
	return dbo.DAE_Trunc(@fraction * 24);
end
go

grant execute on DAE_TSReadHour to public
go

-- DAE_TSReadDay
if exists (select * from sysobjects where id = Object_ID('DAE_TSReadDay'))
	drop function DAE_TSReadDay
go

create function DAE_TSReadDay(@ATimeSpan bigint)
returns integer
begin
	return dbo.DAE_Trunc(@ATimeSpan / 864000000000.0);
end
go

grant execute on DAE_TSReadDay to public
go

-- DAE_TSWriteMillisecond
if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteMillisecond'))
	drop function DAE_TSWriteMillisecond
go

create function DAE_TSWriteMillisecond(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.DAE_TSReadMillisecond(@ATimeSpan)) * 10000;
end
go

grant execute on DAE_TSWriteMillisecond to public
go

-- DAE_TSWriteSecond
if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteSecond'))
	drop function DAE_TSWriteSecond
go

create function DAE_TSWriteSecond(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.DAE_TSReadSecond(@ATimeSpan) ) * 10000000;
end
go

grant execute on DAE_TSWriteSecond to public
go

-- DAE_TSWriteMinute
if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteMinute'))
	drop function DAE_TSWriteMinute
go

create function DAE_TSWriteMinute(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.DAE_TSReadMinute(@ATimeSpan) ) * cast(600000000 as BigInt);
end
go

grant execute on DAE_TSWriteMinute to public
go

-- DAE_TSWriteHour
if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteHour'))
	drop function DAE_TSWriteHour
go

create function DAE_TSWriteHour(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.DAE_TSReadHour(@ATimeSpan) ) * 36000000000;
end
go

grant execute on DAE_TSWriteHour to public
go

-- DAE_TSWriteDay
if exists (select * from sysobjects where id = Object_ID('DAE_TSWriteDay'))
	drop function DAE_TSWriteDay
go

create function DAE_TSWriteDay(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.DAE_TSReadDay(@ATimeSpan) ) * 864000000000;
end
go

grant execute on DAE_TSWriteDay to public
go

-- DAE_AddMonths
if exists (select * from sysobjects where id = Object_ID('DAE_AddMonths'))
	drop function DAE_AddMonths
go

create function DAE_AddMonths(@ADate datetime, @AMonths int)
returns datetime
begin
	return DateAdd(mm, @AMonths, @ADate);
end
go

grant execute on DAE_AddMonths to public
go

-- DAE_AddYears
if exists (select * from sysobjects where id = Object_ID('DAE_AddYears'))
	drop function DAE_AddYears
go

create function DAE_AddYears(@ADate datetime, @AYears int)
returns datetime
begin
	return DateAdd(yyyy, @AYears, @ADate);
end
go

grant execute on DAE_AddYears to public
go

-- DAE_DayOfWeek
if exists (select * from sysobjects where id = Object_ID('DAE_DayOfWeek'))
	drop function DAE_DayOfWeek
go

create function DAE_DayOfWeek(@ADate datetime)
returns int
begin
	return DatePart(dw, @ADate);
end
go

grant execute on DAE_DayOfWeek to public
go

-- DAE_DayOfYear
if exists (select * from sysobjects where id = Object_ID('DAE_DayOfYear'))
	drop function DAE_DayOfYear
go

create function DAE_DayOfYear(@ADate datetime)
returns int
begin
	return DatePart(dy, @ADate);
end
go

grant execute on DAE_DayOfYear to public
go

-- DAE_DaysInMonth
if exists (select * from sysobjects where id = Object_ID('DAE_DaysInMonth'))
	drop function DAE_DaysInMonth
go

create function DAE_DaysInMonth(@Year int, @Month int)
returns int
begin
	declare @Date datetime
	set @Date = Convert(DateTime, Convert(VarChar, @Year) + '-' + Convert(VarChar, @Month) + '-01', 121)
	return DateDiff(dd, @Date, DateAdd(mm, 1, @Date))
end
go

grant execute on DAE_DaysInMonth to public
go

-- DAE_IsLeapYear
if exists (select * from sysobjects where id = Object_ID('DAE_IsLeapYear'))
	drop function DAE_IsLeapYear
go

create function DAE_IsLeapYear(@Year int)
returns int
begin
	declare @Date1 datetime;
	declare @Date2 datetime;
	set @Date1 = '2/28/1980';
	set @Date1 = dbo.DAE_DTWriteYear(@Date1, @Year);
	set @Date2 = '3/1/1980';
	set @Date2 = dbo.DAE_DTWriteYear(@Date2, @Year);
	return DateDiff(dd, @Date1, @Date2) - 1;
end
go

grant execute on DAE_IsLeapYear to public
go

-- DAE_DTReadHour
if exists (select * from sysobjects where id = Object_ID('DAE_DTReadHour'))
	drop function DAE_DTReadHour
go

create function DAE_DTReadHour(@ADate datetime)
returns int
begin
	return DatePart(hh, @ADate);
end
go

grant execute on DAE_DTReadHour to public
go

-- DAE_DTReadMinute
if exists (select * from sysobjects where id = Object_ID('DAE_DTReadMinute'))
	drop function DAE_DTReadMinute
go

create function DAE_DTReadMinute(@ADate datetime)
returns int
begin
	return DatePart(mi, @ADate);
end
go

grant execute on DAE_DTReadMinute to public
go

-- DAE_DTReadSecond
if exists (select * from sysobjects where id = Object_ID('DAE_DTReadSecond'))
	drop function DAE_DTReadSecond
go

create function DAE_DTReadSecond(@ADate datetime)
returns int
begin
	return DatePart(ss, @ADate);
end
go

grant execute on DAE_DTReadSecond to public
go

-- DAE_DTReadMillisecond
if exists (select * from sysobjects where id = Object_ID('DAE_DTReadMillisecond'))
	drop function DAE_DTReadMillisecond
go

create function DAE_DTReadMillisecond(@ADate datetime)
returns int
begin
	return DatePart(ms, @ADate);
end
go

grant execute on DAE_DTReadMillisecond to public
go

-- DAE_DTWriteMillisecond
if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteMillisecond'))
	drop function DAE_DTWriteMillisecond
go

create function DAE_DTWriteMillisecond(@ADate datetime, @APart int)
returns datetime
begin
	return DateAdd(ms,@APart - DatePart(ms,@ADate),@ADate);
end
go

grant execute on DAE_DTWriteMillisecond to public
go

-- DAE_DTWriteSecond
if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteSecond'))
	drop function DAE_DTWriteSecond
go

create function DAE_DTWriteSecond(@ADate datetime, @APart int)
returns datetime
begin
	return DateAdd(ss,@APart - DatePart(ss,@ADate),@ADate);
end
go

grant execute on DAE_DTWriteSecond to public
go

-- DAE_DTWriteMinute
if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteMinute'))
	drop function DAE_DTWriteMinute
go

create function DAE_DTWriteMinute(@ADate datetime, @APart int)
returns datetime
begin
	return DateAdd(mi,@APart - DatePart(mi,@ADate),@ADate);
end
go

grant execute on DAE_DTWriteMinute to public
go

-- DAE_DTWriteHour
if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteHour'))
	drop function DAE_DTWriteHour
go

create function DAE_DTWriteHour(@ADate datetime, @APart int)
returns datetime
begin
	return DateAdd(hh,@APart - DatePart(hh,@ADate),@ADate);
end
go

grant execute on DAE_DTWriteHour to public
go

-- DAE_DTWriteDay
if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteDay'))
	drop function DAE_DTWriteDay
go

create function DAE_DTWriteDay(@ADate datetime, @APart int)
returns datetime
begin
	return DateAdd(dd,@APart - DatePart(dd,@ADate),@ADate);
end
go

grant execute on DAE_DTWriteDay to public
go

-- DAE_DTWriteMonth
if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteMonth'))
	drop function DAE_DTWriteMonth
go

create function DAE_DTWriteMonth(@ADate datetime, @APart int)
returns datetime
begin
	return DateAdd(mm,@APart - DatePart(mm,@ADate),@ADate);
end
go

grant execute on DAE_DTWriteMonth to public
go

-- DAE_DTWriteYear
if exists (select * from sysobjects where id = Object_ID('DAE_DTWriteYear'))
	drop function DAE_DTWriteYear
go

create function DAE_DTWriteYear(@ADate datetime, @APart int)
returns datetime
begin
	return DateAdd(yyyy,@APart - DatePart(yyyy,@ADate),@ADate);
end
go

grant execute on DAE_DTWriteYear to public
go

-- DAE_DateTimeSelector
if exists (select * from sysobjects where id = Object_ID('DAE_DateTimeSelector'))
	drop function DAE_DateTimeSelector
go

create function DAE_DateTimeSelector(@Year int, @Month int = 0, @Day int = 0, @Hour int = 0, @Minute int = 0, @Second int = 0, @Millisecond int = 0)
returns datetime
begin
	return DateAdd(ms, @Millisecond, DateAdd(ss, @Second, DateAdd(mi, @Minute, DateAdd(hh, @Hour, dbo.DAE_DTWriteDay(dbo.DAE_DTWriteMonth(dbo.DAE_DTWriteYear('1/1/1900', @Year), @Month), @Day)))))
end
go

grant execute on DAE_DateTimeSelector to public
go

-- DAE_DTDatePart
if exists (select * from sysobjects where id = Object_ID('DAE_DTDatePart'))
	drop function DAE_DTDatePart
go

create function DAE_DTDatePart(@ADateTime datetime)
returns datetime
begin
	return Convert( DateTime, Floor ( Convert( Float, @ADateTime ) ) );
end
go

grant execute on DAE_DTDatePart to public
go

-- DAE_DTTimePart
if exists (select * from sysobjects where id = Object_ID('DAE_DTTimePart'))
	drop function DAE_DTTimePart
go

create function DAE_DTTimePart(@ADateTime datetime)
returns
 datetime
begin
 return
  Convert
   (
	DateTime,
	'1900-01-01 ' +
	cast(datepart(hh, @ADateTime) as varchar(2)) + ':' +
	cast(datepart(n, @ADateTime) as varchar(2)) + ':' +
	cast(datepart(s, @ADateTime) as varchar(2)) + '.' +
	cast(datepart(ms, @ADateTime) as varchar(3))
   );
end
go

grant execute on DAE_DTTimePart to public
go

-- DAE_DTTimeSpan
if exists (select * from sysobjects where id = Object_ID('DAE_DTTimeSpan'))
	drop function DAE_DTTimeSpan
go

create function DAE_DTTimeSpan (@ADateTime datetime)
returns bigint
begin
	declare @LRefDate datetime;
	set @LRefDate = '01/01/2000';
	return 10000 * (1000 * (DateDiff(ss, @LRefDate, @ADateTime) + 63082281600) + DatePart(ms, @ADateTime));
end
go

grant execute on DAE_DTTimeSpan to public
go

-- DAE_TSDateTime
if exists (select * from sysobjects where id = Object_ID('DAE_TSDateTime'))
	drop function DAE_TSDateTime
go

create function DAE_TSDateTime (@ATimeSpan bigint)
returns datetime
begin
	declare @TempTime bigint;
	set @TempTime = (@ATimeSpan - 630822816000000000) / 10000000;
	declare @TempTime2 bigint;
	set @TempTime2 = dbo.DAE_Frac((@ATimeSpan - 630822816000000000) / 10000000.0) * 1000;
	return DateAdd(ms, @TempTime2, DateAdd(ss, @TempTime ,'1/01/2000'));
end
go

grant execute on DAE_TSDateTime to public
go

-- DAE_PadLeft
if exists (select * from sysobjects where name = 'DAE_PadLeft')
	drop function DAE_PadLeft
go

create function DAE_PadLeft(@AString varchar(8000), @ALength int, @AChar char(1)) returns varchar(8000)
begin
	if DataLength(@AString) < @ALength
		set @AString = Replicate(@AChar, @ALength - DataLength(@AString)) + @AString
	return @AString
end
go

grant execute on DAE_PadLeft to public
go

-- DAE_VersionNumberSelector
if exists (select * from sysobjects where name = 'DAE_VersionNumberSelector')
	drop function DAE_VersionNumberSelector
go

create function DAE_VersionNumberSelector(@Major int, @Minor int, @Revision int, @Build int) returns char(40)
begin
	if ((@Major = -1) and (@Minor <> -1))
		set @Major = Convert(int, 'Major version number must be set first.');
	if ((@Minor = -1) and (@Revision <> -1))
		set @Minor = Convert(int, 'Minor version number must be set first.');
	if ((@Revision = -1) and (@Build <> -1))
		set @Revision = Convert(int, 'Revision number must be set first.');
	return 
		case when @Major = -1 then '**********'	else dbo.DAE_PadLeft(LTrim(Str(@Major)), 10, '0') end +
		case when @Minor = -1 then '**********' else dbo.DAE_PadLeft(LTrim(Str(@Minor)), 10, '0') end +
		case when @Revision = -1 then '**********' else dbo.DAE_PadLeft(LTrim(Str(@Revision)), 10, '0') end +
		case when @Build = -1 then '**********' else dbo.DAE_PadLeft(LTrim(Str(@Build)), 10, '0') end
end
go

grant execute on DAE_VersionNumberSelector to public
go

-- DAE_VersionNumberMajorSelector
if exists (select * from sysobjects where name = 'DAE_VersionNumberMajorSelector')
	drop function DAE_VersionNumberMajorSelector
go

create function DAE_VersionNumberMajorSelector(@Major int) returns varchar(40)
begin
	return dbo.DAE_VersionNumberSelector(@Major, -1, -1, -1)
end
go

grant execute on DAE_VersionNumberMajorSelector to public
go

-- DAE_VersionNumbberMinorSelector
if exists (select * from sysobjects where name = 'DAE_VersionNumberMinorSelector')
	drop function DAE_VersionNumberMinorSelector
go

create function DAE_VersionNumberMinorSelector(@Major int, @Minor int) returns varchar(40)
begin
	return dbo.DAE_VersionNumberSelector(@Major, @Minor, -1, -1)
end
go

grant execute on DAE_VersionNumberMinorSelector to public
go

-- DAE_VersionNumberRevisionSelector
if exists (select * from sysobjects where name = 'DAE_VersionNumberRevisionSelector')
	drop function DAE_VersionNumberRevisionSelector
go

create function DAE_VersionNumberRevisionSelector(@Major int, @Minor int, @Revision int) returns varchar(40)
begin
	return dbo.DAE_VersionNumberSelector(@Major, @Minor, @Revision, -1)
end
go

grant execute on DAE_VersionNumberRevisionSelector to public
go

-- DAE_VersionNumberBuildSelector
if exists (select * from sysobjects where name = 'DAE_VersionNumberBuildSelector')
	drop function DAE_VersionNumberBuildSelector
go

create function DAE_VersionNumberBuildSelector(@Major int, @Minor int, @Revision int, @Build int) returns varchar(40)
begin
	return dbo.DAE_VersionNumberSelector(@Major, @Minor, @Revision, @Build)
end
go

grant execute on DAE_VersionNumberBuildSelector to public
go

-- DAE_VersionNumberMajorReadAccessor
if exists (select * from sysobjects where name = 'DAE_VersionNumberMajorReadAccessor')
	drop function DAE_VersionNumberMajorReadAccessor
go

create function DAE_VersionNumberMajorReadAccessor(@Value varchar(40)) returns int
begin
	return case when Substring(@Value, 1, 10) = '**********' then -1 else Convert(int, Substring(@Value, 1, 10)) end
end
go

grant execute on DAE_VersionNumberMajorReadAccessor to public
go

-- DAE_VersionNumberMinorReadAccessor
if exists (select * from sysobjects where name = 'DAE_VersionNumberMinorReadAccessor')
	drop function DAE_VersionNumberMinorReadAccessor
go

create function DAE_VersionNumberMinorReadAccessor(@Value varchar(40)) returns int
begin
	return case when Substring(@Value, 11, 10) = '**********' then -1 else Convert(int, Substring(@Value, 11, 10)) end
end
go

grant execute on DAE_VersionNumberMinorReadAccessor to public
go

-- DAE_VersionNumberRevisionReadAccessor
if exists (select * from sysobjects where name = 'DAE_VersionNumberRevisionReadAccessor')
	drop function DAE_VersionNumberRevisionReadAccessor
go

create function DAE_VersionNumberRevisionReadAccessor(@Value varchar(40)) returns int
begin
	return case when Substring(@Value, 21, 10) = '**********' then -1 else Convert(int, Substring(@Value, 21, 10)) end
end
go

grant execute on DAE_VersionNumberRevisionReadAccessor to public
go

-- DAE_VersionNumberBuildReadAccessor
if exists (select * from sysobjects where name = 'DAE_VersionNumberBuildReadAccessor')
	drop function DAE_VersionNumberBuildReadAccessor
go

create function DAE_VersionNumberBuildReadAccessor(@Value varchar(40)) returns int
begin
	return case when Substring(@Value, 31, 10) = '**********' then -1 else Convert(int, Substring(@Value, 31, 10)) end
end
go

grant execute on DAE_VersionNumberBuildReadAccessor to public
go

-- DAE_VersionNumberMajorWriteAccessor
if exists (select * from sysobjects where name = 'DAE_VersionNumberMajorWriteAccessor')
	drop function DAE_VersionNumberMajorWriteAccessor
go

create function DAE_VersionNumberMajorWriteAccessor(@VersionNumber varchar(40), @Value int) returns varchar(40)
begin
	return dbo.DAE_VersionNumberSelector
	(
		@Value, 
		dbo.DAE_VersionNumberMinorReadAccessor(@VersionNumber),
		dbo.DAE_VersionNumberRevisionReadAccessor(@VersionNumber),
		dbo.DAE_VersionNumberBuildReadAccessor(@VersionNumber)
	)
end
go

grant execute on DAE_VersionNumberMajorWriteAccessor to public
go

-- DAE_VersionNumberMinorWriteAccessor
if exists (select * from sysobjects where name = 'DAE_VersionNumberMinorWriteAccessor')
	drop function DAE_VersionNumberMinorWriteAccessor
go

create function DAE_VersionNumberMinorWriteAccessor(@VersionNumber varchar(40), @Value int) returns varchar(40)
begin
	return dbo.DAE_VersionNumberSelector
	(
		dbo.DAE_VersionNumberMajorReadAccessor(@VersionNumber),
		@Value, 
		dbo.DAE_VersionNumberRevisionReadAccessor(@VersionNumber),
		dbo.DAE_VersionNumberBuildReadAccessor(@VersionNumber)
	)
end
go

grant execute on DAE_VersionNumberMinorWriteAccessor to public
go

-- DAE_VersionNumberRevisionWriteAccessor
if exists (select * from sysobjects where name = 'DAE_VersionNumberRevisionWriteAccessor')
	drop function DAE_VersionNumberRevisionWriteAccessor
go

create function DAE_VersionNumberRevisionWriteAccessor(@VersionNumber varchar(40), @Value int) returns varchar(40)
begin
	return dbo.DAE_VersionNumberSelector
	(
		dbo.DAE_VersionNumberMajorReadAccessor(@VersionNumber),
		dbo.DAE_VersionNumberMinorReadAccessor(@VersionNumber),
		@Value, 
		dbo.DAE_VersionNumberBuildReadAccessor(@VersionNumber)
	)
end
go

grant execute on DAE_VersionNumberRevisionWriteAccessor to public
go

-- DAE_VersionNumberBuildWriteAccessor
if exists (select * from sysobjects where name = 'DAE_VersionNumberBuildWriteAccessor')
	drop function DAE_VersionNumberBuildWriteAccessor
go

create function DAE_VersionNumberBuildWriteAccessor(@VersionNumber varchar(40), @Value int) returns varchar(40)
begin
	return dbo.DAE_VersionNumberSelector
	(
		dbo.DAE_VersionNumberMajorReadAccessor(@VersionNumber),
		dbo.DAE_VersionNumberMinorReadAccessor(@VersionNumber),
		dbo.DAE_VersionNumberRevisionReadAccessor(@VersionNumber),
		@Value 
	)
end
go

grant execute on DAE_VersionNumberBuildWriteAccessor to public
go

-- DAE_VersionNumberToString
if exists (select * from sysobjects where name = 'DAE_VersionNumberToString')
	drop function DAE_VersionNumberToString
go

create function DAE_VersionNumberToString(@VersionNumber varchar(40)) returns varchar(43)
begin
	return
		case
			when SubString(@VersionNumber, 1, 10) = '**********' then '*' 
			else
				LTrim(Str(Convert(Int, SubString(@VersionNumber, 1, 10)))) + '.' +
				case
					when SubString(@VersionNumber, 11, 10) = '**********' then '*'
					else
						LTrim(Str(Convert(Int, SubString(@VersionNumber, 11, 10)))) + '.' +
						case 
							when SubString(@VersionNumber, 21, 10) = '**********' then '*'
							else
								LTrim(Str(Convert(Int, SubString(@VersionNumber, 21, 10)))) + '.' +
								case
									when SubString(@VersionNumber, 31, 10) = '**********' then '*'
									else LTrim(Str(Convert(Int, SubString(@VersionNumber, 31, 10))))
								end
						end
				end
		end
end
go

grant execute on DAE_VersionNumberToString to public
go

-- DAE_StringToVersionNumber
if exists (select * from sysobjects where name = 'DAE_StringToVersionNumber')
	drop function DAE_StringToVersionNumber
go

create function DAE_StringToVersionNumber(@String varchar(8000)) returns varchar(40)
begin
	declare @Major int, @Minor int, @Revision int, @Build int, @Counter int
	select @Major = -1, @Minor = -1, @Revision = -1, @Build = -1, @Counter = 0
	while charindex('.', @String) > 0
	begin
		set @Counter = @Counter + 1
		if (@Counter = 1)
			set @Major = 
				case 
					when substring(@String, 1, charindex('.', @String) - 1) like '*%' then -1 
					else Convert(int, substring(@String, 1, charindex('.', @String) - 1)) 
				end
		else if (@Counter = 2)
			set @Minor = 
				case
					when substring(@String, 1, charindex('.', @String) - 1) like '*%' then -1
					else Convert(int, substring(@String, 1, charindex('.', @String) - 1))
				end
		else if (@Counter = 3)
			set @Revision = 
				case
					when substring(@String, 1, charindex('.', @String) - 1) like '*%' then -1
					else Convert(int, substring(@String, 1, charindex('.', @String) - 1))
				end
		else
		begin
			set @String = Convert(int, 'Could not convert ""' + @String + '"" to a version number.')
			break;
		end
		set @String = substring(@String, charindex('.', @String) + 1, len(@String) - charindex('.', @String))
	end
	if (@Counter = 0)
		set @Major =
			case
				when @String like '*%' then -1
				else convert(int, @String)
			end
	else if (@Counter = 1)
		set @Minor =
			case 
				when @String like '*%' then -1
				else convert(int, @String)
			end
	else if (@Counter = 2)
		set @Revision =
			case 
				when @String like '*%' then -1
				else convert(int, @String)
			end
	else
		set @Build =
			case 
				when @String like '*%' then -1
				else convert(int, @String)
			end
	return dbo.DAE_VersionNumberSelector(@Major, @Minor, @Revision, @Build)
end
go

grant execute on DAE_StringToVersionNumber to public
go

-- DAE_TooManyRows
if exists (select * from sysobjects where name = 'DAE_TooManyRows')
	drop function DAE_TooManyRows
go

create function DAE_TooManyRows(@dummy int) returns int as
begin
	return cast('Row extractor expression must reference a table expression with at most one row.  Use a restriction or quota query to limit the number of rows in the source table expression.' as int);
end;
go

grant execute on DAE_TooManyRows to public
go
