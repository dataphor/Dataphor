select 
	"T1"."ID" as "ID", "T1"."Name" as "Name", "T1"."Nickname" as "Nickname", "T1"."Salary" as "Salary", "T1"."HireDate" as "HireDate", "T1"."Department_ID" as "Department_ID"
	from "Employee" as "T1"
	where (("T1"."ID" = 1) or (("T1"."ID" = 2) and ("T1"."Department_ID" = 3)))
order by "ID" asc;
