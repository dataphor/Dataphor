EXECUTE create table "Department"
(
	"ID" integer not null, 
	"Name" varchar(20) not null
);
EXECUTE create unique clustered index "UIDX_Department_ID" on "Department"("ID");
