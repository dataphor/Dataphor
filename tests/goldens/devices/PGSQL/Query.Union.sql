select 
	distinct "T4"."ID" as "ID"
	from (select 
		"T1"."ID" as "ID"
		from "Employee" as "T1"
	union all
	select 
		"T2"."ID" as "ID"
		from "Department" as "T2") as "T4"
order by "ID" asc;
