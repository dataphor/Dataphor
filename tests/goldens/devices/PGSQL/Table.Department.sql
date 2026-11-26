EXECUTE create table "Department"
(
	"ID" integer not null, 
	"Name" character varying(20) not null
);
EXECUTE create unique index "UIDX_Department_ID" on "Department"("ID");
