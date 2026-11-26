select 
	"T1"."ID" as "ID", "T1"."Name" as "Name", "T1"."Nickname" as "Nickname", "T1"."Salary" as "Salary", "T1"."HireDate" as "HireDate", "T1"."Department_ID" as "Department_ID", "T3"."DepartmentName" as "DepartmentName"
	from "Employee" as "T1"
		inner join (select 
			"T2"."ID" as "Department_ID", "T2"."Name" as "DepartmentName"
			from "Department" as "T2") as "T3" on ("T1"."Department_ID" = "T3"."Department_ID")
order by "ID" asc;
