/*
	System Supported Operators

	© Copyright 2000-2011 Alphora
*/
create or replace function DAE_Frac (AValue IN NUMERIC)
  RETURNS NUMERIC
  AS $$ declare
    LReturnVal NUMERIC(28, 8);
  BEGIN
    LReturnVal := AValue - TRUNC(AValue, 0);
    RETURN(LReturnVal);
  END;
  $$ LANGUAGE plpgsql;
\
create or replace function DAE_LogB (AValue in NUMERIC, ABase in NUMERIC)
  RETURNS NUMERIC
  AS $$ declare
    LReturnVal NUMERIC(28, 8);
  BEGIN
    LReturnVal := LN(AValue) / LN(ABase);
    RETURN (LReturnVal);
  END;
  $$ LANGUAGE plpgsql;
\
create or replace function DAE_Random()
  RETURNS NUMERIC
  AS $$ declare
    LReturnVal NUMERIC(28,8);
  BEGIN
    LReturnVal := 0;
    --LReturnVal := DAE_Frac(ABS(DBMS_RANDOM.RANDOM) / 100000000);
    --DBMS_RANDOM.TERMINATE;
    RETURN LReturnVal;
  END;
  $$ LANGUAGE plpgsql;
\
create or replace function DAE_Factorial(AValue in int)
  RETURNS NUMERIC
  AS $$ declare 
    LReturnVal NUMERIC(28,8);
  begin
    LReturnVal := 1;
    for i in 1..AValue loop
      LReturnVal := LReturnVal * i;
    end loop;
    RETURN LReturnVal;
  end;
  $$ LANGUAGE plpgsql;
\
create or replace function DAE_TSReadMillisecond(ATimeSpan in NUMERIC)
	RETURNS NUMERIC
	AS $$
	begin
		RETURN (ATimeSpan - (FLOOR(ATimeSpan / 10000000) * 10000000)) / 10000.0;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSReadSecond(ATimeSpan in NUMERIC)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := floor(ATimeSpan / 10000000) % 60;
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSReadMinute(ATimeSpan in NUMERIC)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := TRUNC(DAE_Frac((ATimeSpan / 600000000) / 60) * 60);
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSReadHour(ATimeSpan in NUMERIC)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := TRUNC(DAE_Frac(ATimeSpan / (36000000000 * 24)) * 24);
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSReadDay(ATimeSpan in NUMERIC)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := TRUNC(ATimeSpan / 864000000000);
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSWriteMillisecond(ATimeSpan NUMERIC, APart NUMERIC)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadMillisecond(ATimeSpan)) * 10000;
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSWriteSecond(ATimeSpan in NUMERIC, APart int)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadSecond(ATimeSpan)) * 10000000;
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSWriteMinute(ATimeSpan in NUMERIC, APart int)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadMinute(ATimeSpan)) * 600000000;
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSWriteHour(ATimeSpan in NUMERIC, APart int)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadHour(ATimeSpan)) * 36000000000;
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSWriteDay(ATimeSpan in NUMERIC, APart int)
	RETURNS NUMERIC
	AS $$ declare
		LReturnVal NUMERIC(20,0);
	begin
		LReturnVal := ATimeSpan + (APart - DAE_TSReadDay(ATimeSpan)) * 864000000000;
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_AddMonths(ADateTime in timestamp, AMonths in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + (AMonths || ' month')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_AddYears(ADateTime in timestamp, AYears in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + (AYears || ' year')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_Today()
	RETURNS date
	AS $$ declare
		LReturnVal date;
	begin
		LReturnVal := now();
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DaysInMonth(AYear in integer, AMonth in int)
	RETURNS integer
	AS $$
	begin
		RETURN ((AYear || '-' || AMonth || '-1')::date + '1 month':: interval)::date - (AYear || '-' || AMonth || '-1')::date;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_IsLeapYear(AYear in int)
	RETURNS int
	AS $$
	begin
		RETURN (AYear || '-3-1')::date - (AYear || '-2-28')::date - 1;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTReadDay(ADateTime in date)
	RETURNS int
	AS $$ declare
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'dd');
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTReadMonth(ADateTime in date)
	RETURNS int
	AS $$ declare
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'mm');
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTReadYear(ADateTime in date)
	RETURNS int
	AS $$ declare
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'yyyy');
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTReadHour(ADateTime in timestamp)
	RETURNS int
	AS $$
	begin
		RETURN date_part('hour', ADateTime);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTReadMinute(ADateTime in timestamp)
	RETURNS int
	AS $$
	begin
		RETURN date_part('minute', ADateTime);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTReadSecond(ADateTime in timestamp)
	RETURNS int
	AS $$
	begin
		RETURN date_part('s', ADateTime);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTReadMillisecond(ADateTime in timestamp)
	RETURNS int
	AS $$
	begin
		RETURN cast(date_part('ms', ADateTime) as bigint) % 1000;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DayOfYear(ADateTime timestamp)
	RETURNS int
	AS $$ declare 
		LReturnVal int;
	begin
		LReturnVal := To_Char(ADateTime, 'ddd');
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DayOfWeek(ADateTime timestamp)
	RETURNS int
	AS $$
	begin
		RETURN To_Char(ADateTime, 'd')::int - 1;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTWriteMillisecond(ADateTime in timestamp, APart in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + ((APart - DAE_DTReadMillisecond(ADateTime)) || ' ms')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTWriteSecond(ADateTime in timestamp, APart in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + ((APart - date_part('s', ADateTime)) || ' s')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTWriteMinute(ADateTime in timestamp, APart in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + ((APart - date_part('m', ADateTime)) || ' m')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTWriteHour(ADateTime in timestamp, APart in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + ((APart - date_part('h', ADateTime)) || ' h')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTWriteDay(ADateTime in timestamp, APart in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + ((APart - date_part('d', ADateTime)) || ' d')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTWriteMonth(ADateTime in timestamp, APart in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + ((APart - date_part('month', ADateTime)) || ' month')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTWriteYear(ADateTime in timestamp, APart in int)
	RETURNS timestamp
	AS $$
	begin
		RETURN ADateTime + ((APart - date_part('y', ADateTime)) || ' y')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_TSDateTime(ATimeSpan in NUMERIC)
	RETURNS timestamp
	AS $$ declare 
		TempTime bigint;
	begin
		TempTime := floor((ATimeSpan - 630822816000000000) / 10000);
		RETURN timestamp '2000-01-01' + (floor(TempTime / 1000) || ' s')::interval + ((TempTime % 1000) || ' ms')::interval;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTTimeSpan(ADateTime in timestamp)
	RETURNS NUMERIC
	AS $$
	begin
		RETURN 10000000 * (extract(epoch from (ADateTime - '2000-1-1'::timestamp)) + 63082281600);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DateTimeSelector(AYear in int, AMonth in int = 1, ADay in int = 1, AHour in int = 0, AMinute in int = 0, ASecond in int = 0, AMillisecond in int = 0)
	RETURNS timestamp
	AS $$
	begin
		RETURN AYear || '-' || AMonth || '-' || ADay || ' ' || AHour || ':' || AMinute || ':' || ASecond || '.' || AMillisecond;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTDatePart(ADateTime timestamp)
	RETURNS date
	AS $$
	begin
		RETURN ADateTime;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_DTTimePart(ADateTime timestamp)
	RETURNS timestamp
	AS $$
	begin
		RETURN ('1900-1-1 ' || date_part('h', ADateTime) || ':' ||
			date_part('m', ADateTime) || ':' ||
			date_part('s', ADateTime))::timestamp;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberSelector(AMajor int, AMinor int, ARevision int, ABuild int)
	RETURNS char(40)
	AS $$
	begin
		if ((AMajor = -1) and (AMinor <> -1)) then
			raise 'Major version number must be set first.';
		end if;
		if ((AMinor = -1) and (ARevision <> -1)) then
			raise 'Minor version number must be set first.';
		end if;
		if ((ARevision = -1) and (ABuild <> -1)) then
			raise 'Revision number must be set first.';
		end if;
		RETURN case when AMajor = -1 then '**********'	else lpad(AMajor::varchar, 10, '0') end ||
			case when AMinor = -1 then '**********' else lpad(AMinor::varchar, 10, '0') end ||
			case when ARevision = -1 then '**********' else lpad(ARevision::varchar, 10, '0') end ||
			case when ABuild = -1 then '**********' else lpad(ABuild::varchar, 10, '0') end;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberMajorSelector(AMajor int)
	RETURNS char(40)
	AS $$
	begin
		RETURN DAE_VersionNumberSelector(AMajor, -1, -1, -1);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberMinorSelector(AMajor int, AMinor int)
	RETURNS char(40)
	AS $$
	begin
		RETURN DAE_VersionNumberSelector(AMajor, AMinor, -1, -1);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberRevisionSelector(AMajor int, AMinor int, ARevision int)
	RETURNS char(40)
	AS $$
	begin
		RETURN DAE_VersionNumberSelector(AMajor, AMinor, ARevision, -1);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberBuildSelector(AMajor int, AMinor int, ARevision int, ABuild int)
	RETURNS char(40)
	AS $$
	begin
		RETURN DAE_VersionNumberSelector(AMajor, AMinor, ARevision, ABuild);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberMajorReadAccessor(AValue varchar(40))
	RETURNS int
	AS $$
	begin
		RETURN case when Substring(AValue, 1, 10) = '**********' then -1 else Substring(AValue, 1, 10)::int end;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberMinorReadAccessor(AValue varchar(40))
	RETURNS int
	AS $$
	begin
		RETURN case when Substring(AValue, 11, 10) = '**********' then -1 else Substring(AValue, 11, 10)::int end;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberRevisionReadAccessor(AValue varchar(40))
	RETURNS int
	AS $$
	begin
		RETURN case when Substring(AValue, 21, 10) = '**********' then -1 else Substring(AValue, 21, 10)::int end;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberBuildReadAccessor(AValue varchar(40))
	RETURNS int
	AS $$
	begin
		RETURN case when Substring(AValue, 31, 10) = '**********' then -1 else Substring(AValue, 31, 10)::int end;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberMajorWriteAccessor(AVersionNumber varchar(40), AValue int)
	RETURNS varchar(40)
	AS $$
	begin
		RETURN DAE_VersionNumberSelector
		(
			AValue,
			DAE_VersionNumberMinorReadAccessor(AVersionNumber),
			DAE_VersionNumberRevisionReadAccessor(AVersionNumber),
			DAE_VersionNumberBuildReadAccessor(AVersionNumber)
		);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberMinorWriteAccessor(AVersionNumber varchar(40), AValue int)
	RETURNS varchar(40)
	AS $$
	begin
		RETURN DAE_VersionNumberSelector
		(
			DAE_VersionNumberMajorReadAccessor(AVersionNumber),
			AValue,
			DAE_VersionNumberRevisionReadAccessor(AVersionNumber),
			DAE_VersionNumberBuildReadAccessor(AVersionNumber)
		);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberRevisionWriteAccessor(AVersionNumber varchar(40), AValue int)
	RETURNS varchar(40)
	AS $$
	begin
		RETURN DAE_VersionNumberSelector
		(
			DAE_VersionNumberMajorReadAccessor(AVersionNumber),
			DAE_VersionNumberMinorReadAccessor(AVersionNumber),
			AValue,
			DAE_VersionNumberBuildReadAccessor(AVersionNumber)
		);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberBuildWriteAccessor(AVersionNumber varchar(40), AValue int)
	RETURNS varchar(40)
	AS $$
	begin
		RETURN DAE_VersionNumberSelector
		(
			DAE_VersionNumberMajorReadAccessor(AVersionNumber),
			DAE_VersionNumberMinorReadAccessor(AVersionNumber),
			DAE_VersionNumberRevisionReadAccessor(AVersionNumber),
			AValue
		);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_VersionNumberToString(AVersionNumber varchar(40))
	RETURNS varchar(43)
	AS $$
	begin
		RETURN
			case
				when SubString(AVersionNumber, 1, 10) = '**********' then '*' 
				else
					SubString(AVersionNumber, 1, 10)::int || '.' ||
					case
						when SubString(AVersionNumber, 11, 10) = '**********' then '*'
						else
							SubString(AVersionNumber, 11, 10)::int || '.' ||
							case 
								when SubString(AVersionNumber, 21, 10) = '**********' then '*'
								else
									SubString(AVersionNumber, 21, 10)::int || '.' ||
									case
										when SubString(AVersionNumber, 31, 10) = '**********' then '*'
										else SubString(AVersionNumber, 31, 10)::int::varchar
									end
							end
					end
			end;
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_StringToVersionNumber(AString varchar(43))
	RETURNS varchar(40)
	AS $$ declare
		AMajor int; AMinor int; ARevision int; ABuild int; ACounter int;
	begin
		AMajor := -1;
		AMinor := -1;
		ARevision := -1;
		ABuild := -1;
		ACounter := 0;
		while strpos(AString, '.') > 0 loop
			ACounter := ACounter + 1;
			if (ACounter = 1) then
				AMajor := 
					case 
						when substring(AString, 1, strpos(AString, '.') - 1) like '*%' then -1 
						else substring(AString, 1, strpos(AString, '.') - 1)::int 
					end;
			elseif (ACounter = 2) then
				AMinor := 
					case
						when substring(AString, 1, strpos(AString, '.') - 1) like '*%' then -1
						else substring(AString, 1, strpos(AString, '.') - 1)::int
					end;
			elseif (ACounter = 3) then
				ARevision := 
					case
						when substring(AString, 1, strpos(AString, '.') - 1) like '*%' then -1
						else substring(AString, 1, strpos(AString, '.') - 1)::int
					end;
			else
				raise 'Could not convert ""%"" to a version number.', AString;
			end if;
			AString := substring(AString, strpos(AString, '.') + 1, char_length(AString) - strpos(AString, '.'));
		end loop;
		if (ACounter = 0) then
			AMajor :=
				case
					when AString like '*%' then -1
					else AString::int
				end;
		elseif (ACounter = 1) then
			AMinor :=
				case
					when AString like '*%' then -1
					else AString::int
				end;
		elseif (ACounter = 2) then
			ARevision :=
				case
					when AString like '*%' then -1
					else AString::int
				end;
		else
			ABuild =
				case
					when AString like '*%' then -1
					else AString::int
				end;
		end if;
		RETURN DAE_VersionNumberSelector(AMajor, AMinor, ARevision, ABuild);
	end;
	$$ LANGUAGE plpgsql;
\
create or replace function DAE_Reverse(AString text)
	RETURNS text
	AS $$ declare
		LReturnVal text := '';
		LChar varchar;
		LPos integer;
	begin
		SELECT Length(AString) INTO LPos;
		LOOP
			EXIT WHEN LPos < 1;
			SELECT substring(AString FROM LPos FOR 1) INTO LChar;
			LReturnVal := LReturnVal || LChar;
			LPos := LPos - 1;
		END LOOP;
		RETURN LReturnVal;
	end;
	$$ LANGUAGE plpgsql;
