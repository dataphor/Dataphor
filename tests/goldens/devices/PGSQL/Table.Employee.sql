EXECUTE create table "Employee"
(
	"ID" integer not null, 
	"Name" character varying(40) not null, 
	"Nickname" character varying(20) not null, 
	"Salary" money not null, 
	"HireDate" timestamp not null, 
	"Department_ID" integer not null
);
EXECUTE create unique index "UIDX_Employee_ID" on "Employee"("ID");
