select 
	"T1"."ID" as "ID", "T1"."Name" as "Name", "T1"."Nickname" as "Nickname", "T1"."Salary" as "Salary", "T1"."HireDate" as "HireDate", "T1"."Department_ID" as "Department_ID"
	from "Employee" as "T1"
	where ("T1"."Salary" > 1000.0)
order by "ID" asc;
