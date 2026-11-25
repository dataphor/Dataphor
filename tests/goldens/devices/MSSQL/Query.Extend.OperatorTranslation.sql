select 
	"T1"."ID" as "ID", "T1"."Name" as "Name", "T1"."Nickname" as "Nickname", "T1"."Salary" as "Salary", "T1"."HireDate" as "HireDate", "T1"."Department_ID" as "Department_ID", DataLength("T1"."Name") as "NameLength", Upper("T1"."Name") as "UpperName"
	from "Employee" as "T1" 
order by "ID" asc
option (fast 1);
