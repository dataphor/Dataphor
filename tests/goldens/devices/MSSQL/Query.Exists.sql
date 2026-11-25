select 
	"T2"."D__ID" as "D__ID", "T2"."D__Name" as "D__Name"
	from (select 
		"T1"."ID" as "D__ID", "T1"."Name" as "D__Name"
		from "Department" as "T1" ) as "T2"
	where exists((select 
		*
		from "Employee" as "T3" 
		where ("T3"."Department_ID" = "T2"."D__ID")))
order by "D__ID" asc
option (fast 1);
