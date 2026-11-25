OPEN select 
	case when not(exists((select 
		*
		from "Ledger" as "T1" (updlock)
		where ("T1"."ID" = @P1)))) then 1 else 0 end as "dummy1"
	from (select 
		0 as "dummy1") as "dummy1";
PARAMETERS @P1 : Integer(4)
EXECUTE insert into "Ledger"("ID", "Note", "Amount")
	values(@P0, @P1, @P2);
PARAMETERS @P0 : Integer(4), @P1 : String(30), @P2 : Money
