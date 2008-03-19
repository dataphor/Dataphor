/*
	TSQL Implementation Script for the user defined functions that need to be mapped to the MSSQL Device.
	These are seperate functions.  They must be run in seperate query batches.
	If a function already exists, it can be kept if it has the same functionality,
	but Dataphor needs functions that are named these names.
*/	

CREATE FUNCTION TRUNC(@Value decimal(28,8)
RETURNS decimal(28,8)
BEGIN
	RETURN ROUND(@Value,0,1)
END


CREATE FUNCTION FRAC(@Value decimal(28,8))
RETURNS decimal(28,8)
BEGIN
	RETURN (@Value - ROUND(@Value,0,1))
END

CREATE FUNCTION LOGB(@Value decimal(28,8), @Base decimal(28,8))
RETURNS decimal(28,8)
BEGIN
	RETURN (LOG(@Value) / LOG(@Base))
END


create function factorial(@Value int)
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

// timespan functions

create function ReadMillisecondsPart(@ATimeSpan bigint)
returns integer
begin
	return dbo.Trunc(dbo.Frac(@ATimeSpan / (10000.0 * 1000)) * 1000);
end

create function ReadSecondsPart(@ATimeSpan bigint)
returns integer
begin
	return dbo.Trunc(dbo.Frac(@ATimeSpan / (10000000.0 * 60)) * 60);
end

create function ReadMinutesPart(@ATimeSpan bigint)
returns integer
begin
	return dbo.Trunc(dbo.Frac(@ATimeSpan / (600000000.0 * 60)) * 60);
end

create function ReadHoursPart(@ATimeSpan bigint)
returns integer
begin
	return dbo.Trunc(dbo.Frac(@ATimeSpan / (36000000000.0 * 24)) * 24);
end

create function ReadDaysPart(@ATimeSpan bigint)
returns integer
begin
	return dbo.Trunc(@ATimeSpan / 864000000000.0);
end

create function WriteMillisecondsPart(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.ReadMillisecondsPart(@ATimeSpan)) * 10000;
end

create function WriteSecondsPart(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.ReadSecondsPart(@ATimeSpan) ) * 10000000;
end

create function WriteMinutesPart(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.ReadMinutesPart(@ATimeSpan) ) * 600000000;
end

create function WriteHoursPart(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.ReadHoursPart(@ATimeSpan) ) * 36000000000;
end

create function WriteDaysPart(@ATimeSpan bigint, @APart int)
returns bigint
begin
	return @ATimeSpan + (@APart - dbo.ReadDaysPart(@ATimeSpan) ) * 864000000000;
end

/*
//date/time functions

create function AddMonths(@ADate datetime, @AMonths int)
returns datetime
begin
	return DATEADD(mm, @AMonths, @ADate);
end

create function AddYears(@ADate datetime, @AYears int)
returns datetime
begin
	return DATEADD(yyyy, @AYears, @ADate);
end
*/