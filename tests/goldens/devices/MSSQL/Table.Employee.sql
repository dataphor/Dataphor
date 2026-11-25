EXECUTE create table "Employee"
(
	"ID" integer not null, 
	"Name" varchar(40) not null, 
	"Nickname" varchar(20) not null, 
	"Salary" money not null, 
	"HireDate" datetime not null, 
	"Department_ID" integer not null
);
EXECUTE create unique clustered index "UIDX_Employee_ID" on "Employee"("ID");
