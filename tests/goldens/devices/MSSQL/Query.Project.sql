select 
	"T1"."ID" as "ID", "T1"."Name" as "Name"
	from "Employee" as "T1" 
order by "ID" asc
option (fast 1);
