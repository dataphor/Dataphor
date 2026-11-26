select 
	"T1"."ID" as "EmployeeID", "T1"."Name" as "EmployeeName", "T1"."Nickname" as "Nickname", "T1"."Salary" as "Salary", "T1"."HireDate" as "HireDate", "T1"."Department_ID" as "Department_ID"
	from "Employee" as "T1"
order by "EmployeeID" asc;
