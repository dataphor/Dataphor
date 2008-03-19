procedure frac(in AValue double) result double
declare
	var Li int;
code
	call trunc(AValue,0) into Li;
	Li := 1;
	return (AValue - Li);
end;
