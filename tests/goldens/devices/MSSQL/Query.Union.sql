select 
	"T4"."ID" as "ID"
	from (select 
		"T1"."ID" as "ID"
		from "Employee" as "T1" 
	union
	select 
		"T2"."ID" as "ID"
		from "Department" as "T2" ) as "T4"
order by "ID" asc
option (fast 1);
