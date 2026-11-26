select 
	"T1"."Department_ID" as "Department_ID", Sum("T1"."Salary") as "TotalSalary", Count(*) as "Headcount"
	from "Employee" as "T1"
	group by "T1"."Department_ID"
order by "Department_ID" asc;
